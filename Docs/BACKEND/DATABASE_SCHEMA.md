# Esquema de banco de dados

Alvo de referência: PostgreSQL 15+ (validar a versão suportada no início do
backend). O SQL inicial está em
[`Backend/PostgreSQL/schema_reference.sql`](../../Backend/PostgreSQL/schema_reference.sql).
Ele não é migration de produção.

## Convenções

- IDs são `uuid` gerados no servidor; IDs estáveis de catálogo são `text`.
- Moeda, XP, fragmentos e quantidades usam `bigint` (`long` no C#).
- Taxas usam `integer` em basis points.
- Datas usam `timestamptz` e `now()` do servidor.
- Entidades mutáveis possuem `revision` ou `version bigint >= 0`.
- `created_at`/`updated_at` existem em entidades mutáveis; registros imutáveis
  usam `created_at`/`occurred_at` apenas.
- Soft delete só existe onde retenção e referência exigem histórico. Ledger,
  history, transaction, audit e outbox nunca são soft-deleted pela aplicação.
- JSONB guarda payload versionado/snapshot, não moeda sem constraint nem
  relacionamentos essenciais.
- Enums persistidos usam `smallint` explícito ou texto estável com `CHECK`;
  nunca dependem da ordem implícita do C#.

## Núcleo de jogador e sessão

### `players`

- PK: `player_id uuid`.
- FKs: nenhuma; raiz de ownership.
- Campos: `status` (`Active=0`, `Restricted=1`, `Suspended=2`, `Banned=3`),
  `global_revision`, `created_at`, `updated_at`, `deleted_at`.
- Índices: status; `deleted_at` parcial para housekeeping.
- Invariantes/constraints: revisão não negativa; `deleted_at` é soft delete e
  não apaga ledger/audit. Todo comando trava/compara esta revisão.

### `player_profiles`

- PK/FK: `player_id → players`.
- Campos: nome seguro, locale, time zone, `account_power`,
  `season_peak_power`, `active_team_id`, `primary_profession_id`,
  `catalog_version`, `rules_version`, `revision`, timestamps.
- Índices: `account_power`, `season_peak_power`; nome normalizado apenas se
  unicidade pública for requisito.
- Invariantes: poderes/revisão não negativos; campos de Poder são Derived View
  recalculável. Sem soft delete próprio; acompanha `players`.

### `player_sessions`

- PK: `session_id uuid`; FK `player_id → players`.
- Campos: `device_session_id`, provider, hash do refresh token, família,
  plataforma, client version, attestation/risk, expirações, revoked timestamp,
  last seen, `revision`, timestamps.
- Índices/constraints: único `(player_id, device_session_id)` enquanto ativo;
  único hash de refresh; índices por expiry/player.
- Invariantes: token bruto não é persistido; revogação é terminal. Retenção
  após revogação é política de segurança, não soft delete econômico.

### `feature_flags`

- PK: `flag_key text`.
- Campos: tipo/valor JSONB, ambiente, targeting/rules, `enabled`, `version`,
  início/fim, timestamps.
- Índices: `(enabled, starts_at, ends_at)`.
- Invariantes: schema do valor validado na aplicação; flag não concede recurso
  sem comando econômico. Soft delete por `disabled/deleted_at` opcional.

## Carteira e inventário

### `wallets`

- PK/FK: `player_id → players`.
- Campos: `gold`, `gems_available`, `gems_held`, `revision`, timestamps.
- Índices: PK é suficiente para escrita; observabilidade usa ledger.
- Constraints: todos os saldos `bigint >= 0`; hold nunca negativo.
- Invariantes: alteração e legs do ledger na mesma transação; sem soft delete.

### `wallet_ledger`

- PK: `entry_id uuid`; FKs: `player_id → players` nullable somente para conta
  de sistema formalizada, `transaction_id` lógico.
- Campos: currency, bucket, delta assinado, `balance_after`, reason,
  counterparty, request/correlation, metadata versionada, `created_at`.
- Índices: `(player_id, created_at desc)`, `transaction_id`; unique
  `(transaction_id, leg_index)`.
- Invariantes: append-only; delta não zero; correção é nova entrada;
  `balance_after >= 0` para jogador. Sem update/delete/soft delete.

### `item_instances`

- PK: `item_instance_id uuid`; FK owner → players; self-FK parent; FKs
  opcionais para hero/listing/reservation por IDs controlados.
- Campos: definition, owner, kind, tier, rarity, quantity, state, binding,
  hero/listing/reservation, crafting/provenance, quality, enhancement, stats,
  `version`, timestamps, `deleted_at`.
- Índices: owner/state/kind/tier; reservation; listing; definition; escrow.
- Constraints: Tier 1..9; quantity positiva em estado ativo; equipamento único
  quantity=1; estados/bindings enumerados; estado equipado/escrow/reservado
  exige respectiva referência.
- Invariantes: ID único e imutável; um owner lógico; `Consumed/Destroyed`
  terminal. `deleted_at` só para retenção/privacidade após estado terminal,
  nunca para reutilizar ID.

### `hero_instances`

- PK: `hero_instance_id uuid`; FK owner → players.
- Campos: definition, level, XP, rarity, ascension, unlocked, computed power,
  `version`, timestamps, `deleted_at`.
- Índices: owner, definition, power.
- Constraints: nível 1..100 quando unlocked; ascensão em faixa versionada;
  valores não negativos; unique owner+definition se o catálogo permitir uma
  única instância.
- Invariantes: identidade imutável; equipamento é relacionado por
  `item_instances.equipped_hero_instance_id`. Soft delete apenas após política
  explícita; herói consumido não renasce com mesmo ID.

### `hero_fragments` (suporte)

- PK/FKs: `(player_id, hero_definition_id)`; player → players.
- Campos: balance `bigint`, `revision`, timestamps.
- Índices: PK.
- Constraints: balance/revision não negativos.
- Invariantes: crédito e consumo junto ao comando/history; sem soft delete.

### `player_teams` e `player_team_members` (suporte)

- PK: `team_id uuid`; FK player. Membros PK `(team_id, slot_index)`, FK hero.
- Campos: tipo, nome, `version`, timestamps; membro slot 0..4.
- Índices/constraints: unique `(team_id, hero_instance_id)`; no máximo cinco
  por constraint de slot.
- Invariantes: heróis pertencem ao player e estão unlocked; team power é visão
  derivada. Soft delete em team por `deleted_at`, nunca em membro histórico
  auditado.

## Crafting e profissões

### `profession_progress`

- PK/FK: `(player_id, profession_id)`; player → players.
- Campos: level, XP, rank/tier derivados, station tier, Foco/cap/update time,
  mastery, specialization, mythic pity, cooldown, `revision`, timestamps.
- Índices: player; specialization/cooldown para operação.
- Constraints: level 1..100, Tier/station 1..9, Foco entre 0 e cap, pity/XP
  não negativos, profession ID estável.
- Invariantes: uma linha por profissão; pity por profissão; sem soft delete.

### `crafting_jobs`

- PK: `job_id uuid`; FKs player/output owner → players.
- Campos: recipe/profession/quantity/status/reservation, tool/catalyst,
  catalog/rules versions, request, start/complete/claim times, result JSON,
  `version`, timestamps.
- Índices: `(player_id,status,completes_at)`, worker por status/time; unique
  reservation; unique `(player_id, request_id)`.
- Constraints: quantity positiva; completes >= starts; status versionado.
- Invariantes: claim/cancel terminal; job guarda versões usadas. Sem soft
  delete enquanto outputs/audit o referenciam.

### `crafting_job_inputs`

- PK: `(job_id, input_index)`; FKs job/item.
- Campos: quantity reserved/consumed, item version, role, timestamps.
- Índices: item, job.
- Constraints: quantidades positivas; unique `(job_id,item_id,role)` quando
  aplicável.
- Invariantes: snapshot imutável após start; reserva corresponde ao job. Sem
  revisão/soft delete.

### `crafting_job_outputs`

- PK: `(job_id, output_index)`; FKs job/item; item unique.
- Campos: item, `created_at`.
- Índices: unique item.
- Invariante crítica: output único por `(jobId, outputIndex)`, portanto retry
  de worker não duplica. Imutável, sem soft delete.

## Campanha, idle e Energia

### `campaign_progress`

- PK/FK: `player_id → players`.
- Campos: current/highest stage, last session/claim server time, accumulated
  XP, pending report ID, `revision`, timestamps.
- Índices: highest stage para analytics.
- Constraints: timestamps/revision/XP não negativos; estágio resolve catálogo.
- Invariantes: progresso não regride sem migração/compensação auditada.

### `campaign_first_clears` (suporte)

- PK: `(player_id, stage_id)`; FK player.
- Campos: battle ID, reward transaction ID, completed/claimed time.
- Índices: reward transaction unique.
- Invariantes: first clear único por estágio; registro imutável.

### `offline_reward_reports` (suporte)

- PK: `report_id uuid`; FK player.
- Campos: período, duração elegível, stage/rules/catalog, reward JSON, status,
  claim transaction ID, `version`, timestamps.
- Índices/constraints: um report pending por player via índice parcial; claim
  transaction unique; end >= start.
- Invariantes: recompensa offline única por `reportId`; claim não recalcula o
  report. Não soft-delete dentro da janela de auditoria.

### `energy_wallets`

- PK/FK: `player_id → players`.
- Campos: current, maximum, regeneration anchor/interval, `revision`,
  timestamps.
- Índices: nenhum adicional inicialmente.
- Constraints: 0 <= current <= maximum; interval/max positivos.
- Invariantes: relógio do servidor; consumo/reserva de run atômico.

## Dungeons

### `dungeon_runs`

- PK: `run_id uuid`; FK player/team.
- Campos: dungeon/difficulty, state, energy cost, attempt date, rules/catalog,
  battle reference/hash, outcome, first-clear flag, reward JSON, start/complete/
  claim times, `version`, timestamps.
- Índices: player/status; worker/expiry; unique `(player_id, request_id)`.
- Constraints: energia >=0; state enum explícito; datas coerentes.
- Invariantes: seed privada/criptografada não é controle do cliente; estado
  terminal não reabre.

### `dungeon_claims` (suporte)

- PK/FK: `run_id → dungeon_runs`; FK reward transaction.
- Campos: claimed_at, result JSON.
- Índices: transaction unique.
- Invariante crítica: um claim por run; imutável.

## Gacha

### `gacha_pity`

- PK/FK: `(player_id, pity_group_id, track_id)`; player → players.
- Campos: counters/guarantee/total pulls, `revision`, timestamps.
- Índices: player/group.
- Constraints: contadores/revisão não negativos.
- Invariantes: atualizado na mesma transação do pull e independente por trilho.

### `gacha_history`

- PK: `history_id uuid`; FKs player; referência a pull/request/banner.
- Campos: sequence, reward type/definition/rarity/quantity, pity before/after,
  rules/catalog, correlation, `created_at`.
- Índices: `(player_id, created_at desc)`, `(player_id,banner_id,created_at)`;
  unique `(pull_id, sequence)`.
- Constraints: sequence/quantity não negativos; rarity versionada.
- Invariantes: imutável; sem update/delete/soft delete; não persiste seed
  reutilizável.

## Mercado

### `market_listings`

- PK: `listing_id uuid`; FKs item/seller/buyer → entidades.
- Campos: snapshots pesquisáveis, `price_gems bigint`, fee bps, fee/net,
  status `Draft..Failed`, transaction ID, expiry/reservation, `version`,
  timestamps.
- Índices: browse por status/kind/tier/rarity/price; seller/status; expiry;
  índice único parcial por item em `Active/Reserved`.
- Constraints: preço positivo/dentro do teto configurado na aplicação;
  fee 0..10000; buyer != seller; sold exige buyer/transaction/fee/net;
  gross=fee+net na transaction.
- Invariantes: no máximo um listing ativo por item; estado terminal não volta a
  Active. Histórico retido; sem soft delete econômico.

### `market_transactions`

- PK: `market_transaction_id uuid`; FKs listing/item/buyer/seller.
- Campos: gross/fee/net, request/correlation, completed time.
- Índices/constraints: `listing_id` unique; transaction ID no ledger; buyer !=
  seller; gross=fee+net; valores não negativos.
- Invariante crítica: uma market transaction por listing. Imutável.

## Comandos, outbox e auditoria

### `command_deduplication`

- PK: `(player_id, command_type, idempotency_key)`; FK player.
- Campos: request/command IDs, payload hash, status, HTTP status, response JSON,
  result revision, expiry, timestamps.
- Índices/constraints: unique `(player_id,command_type,request_id)`; expiry;
  payload hash obrigatório.
- Invariantes: mesma chave/payload retorna resposta; payload diferente gera
  conflito. Revisão da linha aumenta durante takeover controlado; soft delete
  somente por TTL após políticas de retenção/constraints naturais.

### `outbox_events`

- PK: `event_id uuid`.
- Campos: type, aggregate type/id/version, player, payload/schema version,
  correlation/causation, occurred/available/published times, attempts,
  last error seguro.
- Índices: pendentes por available/occurred; aggregate/version unique.
- Constraints: attempts/schema/version não negativos.
- Invariantes: criado no mesmo commit do fato; append-only, apenas campos de
  entrega são atualizados. Sem soft delete antes da retenção operacional.

### `audit_events`

- PK: `audit_event_id uuid`; FK player opcional.
- Campos: actor type/id, action, target, result, reason, correlation, metadata
  sanitizada, `occurred_at`.
- Índices: target/time, actor/time, correlation.
- Invariantes: append-only; sem secrets/PII desnecessária; correções adicionam
  evento. Sem revisão/soft delete pela aplicação.

## Invariantes críticas consolidadas

1. `item_instance_id` é globalmente único e imutável.
2. Índice único parcial permite no máximo um listing ativo/reservado por item.
3. Wallet nunca fica negativa.
4. Ledger e gacha history são append-only/imutáveis.
5. Output de crafting é único por `(job_id, output_index)`.
6. `request_id` é único por jogador e comando.
7. Pity e history são atualizados na mesma transação do pull.
8. Recompensa offline é única por `report_id`.
9. First clear é único por jogador/estágio.
10. Dungeon claim é único por run.
11. Market transaction é única por listing.

Constraints protegem invariantes estruturais; regras dependentes de catálogo
continuam na camada de domínio e são revalidadas dentro da transação.

