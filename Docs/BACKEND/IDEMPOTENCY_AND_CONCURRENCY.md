# Idempotência e concorrência

## Identificadores

| Campo | Escopo | Uso |
|---|---|---|
| `commandId` | intenção lógica global | diagnóstico, causation e vínculo client/server |
| `requestId` | jogador + tipo de comando | unicidade da requisição e suporte |
| `idempotencyKey` | jogador + tipo de comando | deduplicação do efeito econômico |
| `correlationId` | cadeia distribuída | API, banco, worker, outbox e logs |

O servidor deriva `playerId` da autenticação e inclui `commandType` a partir da
rota. O hash de payload usa JSON canônico com rota, versão e todos os campos que
alteram semântica; exclui somente correlation/telemetria explicitamente
documentada.

## Algoritmo de deduplicação

Na mesma transação da mutação:

1. inserir `(player_id, command_type, idempotency_key)` com `payload_hash`,
   `request_id`, status `Processing` e expiração;
2. se a chave não existir, executar o comando;
3. ao concluir, armazenar status HTTP, envelope de resposta serializado,
   `result_revision` e hash;
4. confirmar mutação, ledger/outbox e dedup juntos.

Em colisão:

- mesma `idempotencyKey` + mesmo payload canônico → retornar a mesma resposta
  armazenada, inclusive IDs, RNG, custos e revisão;
- mesma `idempotencyKey` + payload diferente → `IDEMPOTENCY_CONFLICT`;
- mesmo `requestId` no mesmo jogador/comando com outra chave/payload →
  `IDEMPOTENCY_CONFLICT`.

O replay de uma resposta concluída não revalida `expectedRevision`, pois isso
mudaria o resultado de uma operação que já ocorreu. Uma linha `Processing`
recente faz a segunda requisição aguardar brevemente ou responder retryable sem
executar em paralelo. Linha antiga é investigada contra ledger/aggregate antes
de ser retomada; nunca é simplesmente apagada e refeita.

## Resposta armazenada e TTL

- Retenção mínima proposta para mutações econômicas: 30 dias.
- Gacha, compra P2P, claims, IAP futuro e operações contestáveis: retenção
  alinhada ao histórico/auditoria, potencialmente permanente em forma compacta.
- Payload bruto sensível não é necessário: guardar hash canônico e resposta
  sanitizada/criptografada conforme classificação.
- Expirar dedup não permite repetir uma chave lógica que também possui
  constraint permanente, como first clear, `reportId`, `runId` ou listing.
- O cliente não reutiliza IDs após TTL; IDs são únicos por instalação/conta.

TTL é tuning operacional, não promessa de exatamente-once. Exatamente-uma
consequência econômica vem da combinação de dedup, constraints e transação.

## Revisões e concorrência otimista

Cada jogador possui `global_revision bigint` monotônica. Agregados concorridos também
possuem `version` própria. Um comando mutável compara `expectedRevision`:

```sql
UPDATE players
SET global_revision = global_revision + 1, updated_at = now()
WHERE player_id = :player_id AND global_revision = :expected_revision;
```

Zero linhas atualizadas → rollback e `REVISION_CONFLICT`, com revisões esperada
e atual. Comandos que tocam dois jogadores, como compra P2P, validam a revisão
do comprador e usam locks/versionamento interno para vendedor/listing; o
cliente não precisa conhecer a revisão privada de outro jogador.

Patches declaram `baseRevision` e `newRevision`. O cliente que não possui a base
solicita snapshot completo.

## Transações e locks

- PostgreSQL é a fronteira de commit para carteira, ledger, inventário,
  progresso, dedup, audit e outbox.
- `SELECT ... FOR UPDATE` é usado em linhas de listing, item, wallet e job que
  participam de decisões read-modify-write.
- A ordem de lock é determinística: recursos globais/listing, item, wallets por
  `player_id` ordinal, depois progresso/dedup.
- Índices/constraints garantem unicidade mesmo se validações de aplicação
  falharem.
- Isolamento inicial: `READ COMMITTED` com locks explícitos; operações de alta
  contenção podem usar `SERIALIZABLE` após teste de carga.
- Deadlock/serialization failure causa rollback completo e retry interno
  limitado com jitter; nunca há retry infinito.

## Política de retry

| Falha | Quem repete | Regra |
|---|---|---|
| timeout antes de resposta | cliente | mesma chave/payload |
| 429/503/erro retryable | cliente | `Retry-After`, backoff exponencial e jitter |
| deadlock/serialization | servidor | retry transacional limitado |
| `REVISION_CONFLICT` | cliente | buscar snapshot, nova decisão e novos IDs |
| `IDEMPOTENCY_CONFLICT` | ninguém automaticamente | erro de cliente/integridade |
| validação econômica | ninguém | exibir erro seguro |

Um retry após revisão conflitante é uma nova intenção; não reaproveita chave da
intenção rejeitada se o payload/decisão mudar.

## Outbox e workers

O produtor insere `outbox_events` na mesma transação do estado. Worker:

1. busca lote pendente com `FOR UPDATE SKIP LOCKED`;
2. publica usando `eventId` como chave de dedup;
3. marca tentativa/publicação;
4. em falha, registra erro sanitizado e agenda retry com backoff;
5. após limite, move logicamente para dead-letter/alerta sem apagar o evento.

Entrega externa é pelo menos uma vez. Consumidores deduplicam por `eventId`.
Notificações e analytics podem duplicar tecnicamente, mas nunca repetem a
mutação econômica.

Workers de crafting, expiração e reconciliação usam chaves naturais:
`(jobId, outputIndex)`, `(listingId, expirationVersion)` e IDs de transação.

## Compensação e reconciliação

Transações locais não precisam de compensação: falha implica rollback. Uma saga
externa (recibo, provedor ou serviço gerenciado) registra estado durável,
passos, retries e ação compensatória.

Compensação:

- é uma nova operação auditada, nunca edição/remoção de ledger;
- conserva moeda/item e referencia transação original;
- é idempotente;
- não inventa o resultado quando o histórico é ambíguo; cria incidente manual.

Reconciliações mínimas:

- saldo materializado versus soma do ledger;
- item em escrow sem listing e listing ativo sem item correspondente;
- listing vendido sem uma única `market_transaction`;
- job completo sem todos os outputs únicos;
- pity/history divergentes;
- dedup `Processing` além do timeout;
- outbox não publicada além do SLA.

## Exemplos críticos

### Claim offline

Constraint única em `offline_reward_claims.report_id`. Retry do claim retorna a
resposta armazenada. Um novo request para report já coletado retorna o resultado
anterior ou `REWARD_ALREADY_CLAIMED`, sem novo crédito.

### Gacha

Lock de wallet e pity, dedup, débito, RNG, rewards, history, pity, ledger e
outbox compartilham transação. O seed/estado RNG não é retornado como controle
ao cliente.

### Compra P2P

Lock do listing impede dois vencedores. Constraint única de
`market_transactions.listing_id` e índice de item ativo protegem contra dupla
venda. Debit/credit/burn, item, listing, ledger, transação, dedup e outbox são
confirmados juntos.
