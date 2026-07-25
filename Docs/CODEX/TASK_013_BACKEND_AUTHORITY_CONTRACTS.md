# TASK 013 — Contratos de autoridade do backend

Data: 2026-07-25  
Branch: `feat/backend-authority-contracts`  
Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo

Definir a futura fronteira autoritativa entre o cliente Unity e o backend para
autenticação, bootstrap/snapshots, heróis, inventário, crafting, campanha/idle,
dungeons, gacha, mercado P2P e comissões. Esta task não implementa, hospeda ou
configura um backend de produção.

## Auditoria anterior às alterações

Foram lidos `AGENTS.md`, `Docs/Architecture_GDD.md` e os documentos das Tasks
003 a 012. Também foram inspecionados os DTOs existentes em
`Assets/_Game/Scripts/Infrastructure/Backend`, todos os schemas/pseudocódigos
em `Backend/` e o estado inicial do Git.

Estado inicial: branch `main`, um commit à frente de `origin/main`, sem arquivos
modificados. A branch própria desta task foi criada sem commit.

Operações econômicas simuladas localmente encontradas:

- heróis: XP, fragmentos, unlock, level-up, ascend, promote e referências de
  equipamento;
- inventário: seed autorizado de desenvolvimento, add/remove, split/merge,
  equip/unequip/binding, lock, reservas, consumo, destroy/salvage, escrow e
  transferência simulada;
- crafting: Foco, ouro local, reserva, start/cancel/claim, RNG de qualidade/
  raridade, outputs, XP, maestria, pity e especialização;
- campanha/idle: start/complete de estágio, first/repeat clear, relatório e
  claim offline de ouro/XP/materiais;
- dungeon: Energia, tentativa, run/seed, batalha, drops, ouro, first clear,
  cancelamento e falha técnica;
- gacha de desenvolvimento: moeda fictícia, pull, RNG, pity, history,
  fragmentos, unlock/ascend e duplicata;
- mercado: somente matemática, DTOs e transições-base de escrow/transferência;
  não existe mercado P2P local funcional.

## Arquivos

Documentação:

- `Docs/ADR/ADR-001-BACKEND-AUTHORITY-ARCHITECTURE.md`;
- `Docs/BACKEND/AUTHORITY_MODEL.md`;
- `Docs/BACKEND/API_CONTRACTS.md`;
- `Docs/BACKEND/DATABASE_SCHEMA.md`;
- `Docs/BACKEND/IDEMPOTENCY_AND_CONCURRENCY.md`;
- `Docs/BACKEND/SECURITY_MODEL.md`;
- `Docs/BACKEND/EVENT_CATALOG.md`;
- este handoff.

Referências de backend:

- `Backend/PostgreSQL/schema_reference.sql`;
- `Backend/OpenAPI/idle-medieval-legends-v1.yaml`.

Cliente/testes:

- `BackendContractDtos.cs`: envelopes, errors, snapshots, results e enums de
  rede versionados;
- `BackendRequestDtos.cs`, `CraftingCommandDtos.cs` e
  `MarketCommandDtos.cs`: payloads de intenção sem modelos Domain;
- `IGameBackendClient.cs`: porta correspondente aos contratos;
- `MockGameBackendClient.cs`: adapter em memória explicitamente não seguro,
  sem mutação econômica;
- `ContractSerializationTests.cs`: round-trip, validação e compatibilidade.

Unity gerou os `.meta` dos novos scripts/teste durante a importação canônica.

## ADR e arquitetura

A ADR recomenda:

```text
Unity → HTTPS API → ASP.NET Core → PostgreSQL → Workers → Outbox
                                              ↘ notifications/analytics
```

PostgreSQL concentra wallet, ledger, inventário, crafting, pity, claims e
settlement P2P. Uma arquitetura híbrida é permitida para identidade,
attestation, receipt validation, push e analytics, sem criar autoridade
econômica paralela.

Foram comparados ASP.NET Core/PostgreSQL, Firebase/Firestore, PlayFab, Unity
Gaming Services e híbrida por transação multi-entidade, P2P, ledger,
idempotência, escala, custo, complexidade, observabilidade, duplicação, Unity,
lock-in e operação. Nenhuma opção é descrita como capaz de impedir toda fraude.

## Contratos

- Envelope de comando: `commandId`, `requestId`, `clientVersion`, `platform`,
  `deviceSessionId`, `expectedRevision`, `sentAtClientTime`, `payload`,
  `idempotencyKey` e `correlationId`.
- Envelope de resposta: IDs, success, server time, revisão, result/error,
  patch opcional, retryable e rules version.
- Erro seguro e catálogo inicial de códigos estáveis.
- `playerId` é derivado exclusivamente da identidade autenticada.
- Bootstrap retorna profile, wallet, heroes, inventory, professions, campaign,
  energy, jobs, pity, catálogo, flags e revisão.
- Todos os endpoints pedidos foram incluídos no documento, OpenAPI e porta C#.
- `complete-stage`/dungeon aceitam evidência, não um winner confiável; a
  recomendação v1 é simulação integral no servidor.
- Gacha é totalmente resolvido no servidor.

## Banco e mercado

O documento relacional especifica PKs, FKs, campos, índices, constraints,
revisões, timestamps, soft delete e invariantes das tabelas obrigatórias. Foram
incluídas tabelas auxiliares para fragmentos, equipes, first clear, reports
offline, dungeon claims e pulls.

Listing usa estados explícitos `Draft=0` a `Failed=6`. Compra bloqueia listing,
item e wallets; valida escrow/saldo/autocompra; debita comprador, credita
vendedor, queima 10%, transfere item e grava ledger, transaction, audit, dedup
e outbox no mesmo commit.

```text
feeBps = 1000
fee = ceil(price × 1000 / 10000)
sellerNet = price - fee
```

Moedas usam `long`/`bigint`, nunca `float`/`double`.

## Idempotência e eventos

Mesmo `idempotencyKey` + mesmo payload canônico retorna a resposta armazenada.
Payload diferente retorna `IDEMPOTENCY_CONFLICT`. `expectedRevision` aplica
concorrência otimista; locks e constraints protegem recursos compartilhados.
Outbox e workers usam entrega ao menos uma vez e consumidores deduplicam por
`eventId`.

O catálogo cobre os 21 eventos mínimos, todos com `eventId`, type, aggregate,
player, time, version, payload, correlation, causation e schema version.

## Segurança

Foram cobertos autenticação/sessão, autorização, rate limit, attestation,
receipt validation futura, secrets, TLS, logs, PII, bans, fraude, chargeback,
replay, relógio, botting, colusão, alts e observabilidade. Attestation é um
sinal complementar e nunca substitui regras de negócio.

## Testes e validação

Validação executada em
`%TEMP%/IdleMedievalLegends-validation-task013` e
`%TEMP%/IdleMedievalLegends-validation-task013-review`:

- importação/compilação Unity: código 0, sem `error CS` ou `warning CS`;
- primeira suíte EditMode: 300 executados, 299 aprovados, uma falha de
  round-trip que revelou materialização de `result` vazio pelo `JsonUtility`;
- contrato ajustado para usar `success/error` como discriminador em falha;
- após a revisão dos contratos wire, `ContractSerializationTests`: 16
  executados e aprovados, cobrindo platform string, opcionais RFC 3339/ID,
  patches, evidence aninhada e `professionId`;
- suíte EditMode final pós-revisão: `Passed`, 307 executados, 307 aprovados,
  zero falhas, ignorados ou inconclusivos;
- suíte PlayMode de regressão: `Passed`, 21 executados, 21 aprovados, zero
  falhas, ignorados ou inconclusivos;
- YAML carregado com PyYAML: 42 paths, 44 operações, 328 referências internas
  resolvidas e todos os endpoints obrigatórios presentes;
- SQL validado estaticamente: 70 statements, 29 tabelas, delimitadores
  balanceados, FKs apontando para tabelas declaradas e invariantes-chave
  presentes, sem conexão com banco;
- Markdown: oito arquivos com H1, fences, hierarquia e links relativos válidos;
- revisão cruzada: 25 erros, 21 eventos, 22 tabelas obrigatórias e 21 classes
  de autoridade encontradas;
- `git diff --check`: aprovado.

O SQL não foi analisado pelo parser de um servidor PostgreSQL nem conectado a
um banco, conforme a restrição da task. Ele é referência inicial e ainda
precisa ser transformado em migrations/testes de integração numa task futura.
Builds Android/iOS e smoke test em dispositivo não foram executados.

## Decisões

- DTO de rede não reutiliza classes Domain; enums de contrato têm IDs
  explícitos.
- O mock é compilado somente em Editor/Development Build/testes, lê snapshots
  e retorna read models de desenvolvimento, mas recusa mutações com
  `MOCK_NOT_CONFIGURED`.
- `JsonUtility` usa `success/error` como discriminador porque pode materializar
  um objeto genérico vazio para campo ausente.
- Como `JsonUtility` transforma strings nulas em vazias no JSON, o cliente
  sempre materializa `sentAtClientTime` RFC 3339 e `correlationId` válidos.
- `platform` e IDs de profissão são strings wire canônicas; enums ficam como
  conveniência interna e não são serializados nesses comandos.
- Resposta idempotente já armazenada tem precedência sobre revisão atual.
- Regras estruturais ficam em constraints; elegibilidade de catálogo continua
  no domínio transacional.
- SQL e OpenAPI são referências versionáveis, não alegação de serviço pronto.

## Limitações e riscos

- Não há solução ASP.NET, servidor, migration, banco conectado, worker,
  autenticação real, HTTP adapter, IAP, deploy ou configuração de produção.
- O OpenAPI inicial reutiliza `payload`/`result` genéricos em vários endpoints;
  a implementação deverá expandi-los para schemas específicos antes de gerar
  SDK público.
- SQL requer revisão por backend/DBA, teste em PostgreSQL descartável e desenho
  de migrations/rollback.
- Política final de TTL, retenção, PII, ban, chargeback e attestation depende
  de operação, lojas, lei aplicável e análise de risco.
- Simulação de batalha cross-runtime precisa de golden replays por
  `rulesVersion`.
- Nenhum build mobile foi validado.

## Sequência sugerida

1. Backend 001 — Solution e infraestrutura.
2. Backend 002 — Autenticação e bootstrap.
3. Backend 003 — Wallet e ledger.
4. Backend 004 — Inventário.
5. Backend 005 — Crafting.
6. Backend 006 — Idle e campanha.
7. Backend 007 — Dungeons.
8. Backend 008 — Gacha.
9. Backend 009 — Mercado P2P.
10. Backend 010 — Comissões e antifraude.

Cada task deve adicionar migrations versionadas, testes de integração,
observabilidade e critérios de rollback/reconciliação sem ampliar autoridade do
cliente.

## Ausência de commit

Nenhum commit foi criado automaticamente nesta task.
