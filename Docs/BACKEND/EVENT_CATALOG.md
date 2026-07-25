# Catálogo de eventos

## Envelope

Todo evento de domínio/outbox usa:

```json
{
  "eventId": "01K0EVENT00000000000000001",
  "type": "ItemCreated",
  "aggregateId": "01K0ITEM000000000000000001",
  "playerId": "01K0PLAYER0000000000000001",
  "occurredAt": "2026-07-25T15:31:23.102Z",
  "version": 8,
  "payload": {},
  "correlationId": "01K0CORRELATION00000000001",
  "causationId": "01K0COMMAND0000000000000001",
  "schemaVersion": 1
}
```

| Campo | Regra |
|---|---|
| `eventId` | único global; chave de dedup do consumidor |
| `type` | nome estável em PascalCase |
| `aggregateId` | agregado cuja versão avançou |
| `playerId` | jogador principal; eventos P2P incluem contraparte no payload |
| `occurredAt` | UTC do servidor dentro da transação |
| `version` | versão monotônica do agregado |
| `payload` | schema específico, sem secrets/PII/seed |
| `correlationId` | cadeia request → worker → integração |
| `causationId` | command/event que causou este evento |
| `schemaVersion` | versão do payload daquele tipo |

Outbox é publicada pelo menos uma vez. Consumidores deduplicam por `eventId`.
Eventos são fatos imutáveis; correções produzem eventos compensatórios. O
catálogo não implica event sourcing: PostgreSQL continua fonte do estado
materializado.

## Eventos v1

| Evento | Aggregate | Payload mínimo |
|---|---|---|
| `PlayerCreated` | player | `createdByProvider`, `profileRevision` |
| `HeroUnlocked` | hero | `heroInstanceId`, `heroDefinitionId`, `fragmentCost` |
| `HeroLeveledUp` | hero | `fromLevel`, `toLevel`, `goldCost`, `experienceUsed` |
| `HeroAscended` | hero | `fromAscension`, `toAscension`, `fragmentCost` |
| `ItemCreated` | item | `itemInstanceId`, `definitionId`, `quantity`, `originType`, `originTransactionId` |
| `ItemEquipped` | item | `itemInstanceId`, `heroInstanceId`, `slotId`, `bindingAfter` |
| `ItemSalvaged` | item | `itemInstanceId`, `outputs`, `salvageTransactionId` |
| `CraftingStarted` | crafting job | `jobId`, `recipeId`, `quantity`, `completesAt`, `catalogVersion` |
| `CraftingCancelled` | crafting job | `jobId`, `goldRefund`, `focusRefund`, `releasedInputs` |
| `CraftingCompleted` | crafting job | `jobId`, `outputs`, `professionXp`, `pityBefore`, `pityAfter` |
| `ProfessionLeveledUp` | profession | `professionId`, `fromLevel`, `toLevel`, `totalExperience` |
| `OfflineRewardClaimed` | offline report | `reportId`, `eligibleDurationSeconds`, `gold`, `items`, `experience` |
| `DungeonStarted` | dungeon run | `runId`, `dungeonId`, `difficultyId`, `energyCost` |
| `DungeonCompleted` | dungeon run | `runId`, `outcome`, `firstClear`, `claimRequired` |
| `GachaPulled` | gacha pull | `pullId`, `bannerId`, `quantity`, `cost`, `rewardSummaries`, `rulesVersion` |
| `MythicPityTriggered` | pity | `system`, `groupId`, `counterBefore`, `resultReferenceId` |
| `MarketListingCreated` | listing | `listingId`, `itemInstanceId`, `sellerPlayerId`, `priceGems`, `expiresAt` |
| `MarketListingCancelled` | listing | `listingId`, `itemInstanceId`, `reason` |
| `MarketItemSold` | listing | `listingId`, `transactionId`, `itemInstanceId`, `buyerPlayerId`, `sellerPlayerId`, `grossGems`, `feeGems`, `sellerNetGems` |
| `GemsBurned` | wallet transaction | `transactionId`, `amount`, `reason`, `listingId?`, `commissionId?` |
| `WalletChanged` | wallet | `transactionId`, `currencyId`, `delta`, `balanceAfter`, `reason`, `counterpartyId?` |

Valores monetários e quantidades são `int64`. Probabilidades/taxas são basis
points. Payloads não contêm saldo anterior fornecido pelo cliente.

## Ordenação e evolução

- Ordem global não é prometida. Por agregado, `version` detecta lacunas e
  reordenação.
- Um comando pode gerar múltiplos eventos; todos compartilham correlation e
  causation.
- Mudança aditiva mantém `schemaVersion`; remoção, mudança de tipo/semântica ou
  enum incompatível incrementa a versão.
- Consumidor desconhecendo versão não confirma processamento silenciosamente;
  envia para retry/dead-letter e alerta.
- Analytics pode projetar eventos, mas não corrige carteira/inventário.

## Privacidade e retenção

Eventos usam IDs opacos, sem nome, e-mail, token, receipt, IP bruto ou dados de
attestation. Retenção de outbox operacional pode ser menor que audit/ledger,
desde que publicação e evidência exigida sejam preservadas. A política final
depende de requisitos legais e operação de produção.

