# Contratos da API autoritativa v1

Base path: `/v1`. Transporte: HTTPS com JSON UTF-8. Datas são UTC em RFC 3339.
IDs são strings opacas; o cliente não extrai significado deles. Campos
monetários e quantidades persistentes são inteiros de 64 bits.

Este documento especifica contratos, não um backend funcional. O OpenAPI
inicial está em
[`Backend/OpenAPI/idle-medieval-legends-v1.yaml`](../../Backend/OpenAPI/idle-medieval-legends-v1.yaml).

## Identidade, autenticação e headers

- `Authorization: Bearer <token>` identifica a sessão. O `playerId` efetivo vem
  somente dos claims validados e do vínculo da sessão.
- Um `playerId` recebido no body, query ou path nunca substitui a identidade
  autenticada. Os endpoints de jogador desta API não o aceitam no payload.
- `X-Correlation-Id` pode ser enviado; o servidor valida tamanho/formato ou
  cria outro e sempre o devolve.
- `X-App-Attestation` pode transportar token/assertion de attestation. É sinal
  adicional, não autorização econômica.
- Tokens e refresh tokens nunca aparecem em logs, analytics ou erros.

## Envelope padrão de comando

Todos os `POST` mutáveis recebem:

```json
{
  "commandId": "01K0COMMAND0000000000000001",
  "requestId": "01K0REQUEST0000000000000001",
  "clientVersion": "0.13.0",
  "platform": "android",
  "deviceSessionId": "01K0SESSION0000000000000001",
  "expectedRevision": 481,
  "sentAtClientTime": "2026-07-25T15:31:22Z",
  "payload": {},
  "idempotencyKey": "01K0IDEMPOTENCY00000000001",
  "correlationId": "01K0CORRELATION00000000001"
}
```

| Campo | Regra |
|---|---|
| `commandId` | ID opaco da intenção; obrigatório e estável no retry exato |
| `requestId` | ID da requisição lógica; único no escopo jogador + tipo de comando e estável no retry |
| `clientVersion` | versão SemVer/build aceita pela política do servidor |
| `platform` | `android`, `ios` ou `editor` para ambientes permitidos |
| `deviceSessionId` | sessão registrada e vinculada ao token |
| `expectedRevision` | revisão global lida pelo cliente; `0` somente quando o contrato permite ausência de estado anterior |
| `sentAtClientTime` | opcional, diagnóstico; nunca decide energia, idle, expiração ou crafting |
| `payload` | intenção específica; não contém fatos econômicos calculados |
| `idempotencyKey` | chave obrigatória de deduplicação da operação lógica |
| `correlationId` | opcional; servidor cria/normaliza e propaga |

Retry exato preserva `commandId`, `requestId`, `idempotencyKey` e payload
canônico. Uma ação nova usa novos IDs. `expectedRevision` protege contra decisão
baseada em snapshot obsoleto; a resposta idempotente previamente armazenada tem
precedência sobre revalidar a revisão atual.

Embora o protocolo permita omitir `sentAtClientTime` e `correlationId`, o DTO
Unity sempre os materializa com timestamp RFC 3339 e ID opaco válidos, pois o
`JsonUtility` serializa strings nulas como `""`.

`POST /session/bootstrap` e `/session/refresh` usam o mesmo formato, mas
`expectedRevision=0` é permitido.

## Envelope padrão de resposta

```json
{
  "requestId": "01K0REQUEST0000000000000001",
  "correlationId": "01K0CORRELATION00000000001",
  "success": true,
  "serverTime": "2026-07-25T15:31:23.102Z",
  "newRevision": 482,
  "result": {},
  "error": null,
  "snapshotPatch": {
    "baseRevision": 481,
    "newRevision": 482,
    "operations": []
  },
  "retryable": false,
  "rulesVersion": "economy_rules_v1"
}
```

- Sucesso: `result` não nulo, `error=null`.
- Falha: `result` nulo/omitido, `error` obrigatório e `success=false`. O adapter
  Unity ignora qualquer objeto `result` vazio materializado pelo `JsonUtility`
  quando `success=false`.
- `snapshotPatch` é opcional e só pode ser aplicado quando
  `baseRevision` coincide com o cache.
- Cada operação do patch é `{ "op": "set|remove", "path": "/...",
  "valueJson": "..." }`; `valueJson` é obrigatório para `set` e ignorado
  quando vazio/omitido em `remove`. A lista usa o campo `operations`.
- `newRevision` não diminui. Falha sem mutação devolve a revisão atual.
- `retryable` no topo repete `error.retryable` para decisões de transporte.
- O retry automático só é permitido para falha marcada retryable e preserva a
  mesma chave/payload.

## Erro padrão

```json
{
  "code": "REVISION_CONFLICT",
  "messageKey": "backend.error.revision_conflict",
  "safeMessage": "O estado mudou. Atualize e tente novamente.",
  "details": {
    "resource": "player",
    "fieldErrors": []
  },
  "retryable": false,
  "expectedRevision": 481,
  "actualRevision": 483,
  "correlationId": "01K0CORRELATION00000000001"
}
```

`safeMessage` é seguro para exibição, mas a UI deve preferir `messageKey`.
`details` nunca inclui stack trace, SQL, token, seed, regra secreta ou PII. Os
códigos são estáveis; mensagens podem mudar.

### Catálogo inicial de erros

| Código | HTTP | Retry | Significado |
|---|---:|---:|---|
| `AUTH_REQUIRED` | 401 | não | bearer ausente |
| `AUTH_INVALID` | 401 | não | token/sessão inválido ou expirado |
| `REQUEST_INVALID` | 400 | não | envelope/payload inválido |
| `IDEMPOTENCY_CONFLICT` | 409 | não | mesma chave com hash de payload diferente |
| `REVISION_CONFLICT` | 409 | não | `expectedRevision` não corresponde |
| `CATALOG_VERSION_UNSUPPORTED` | 409 | não | cliente precisa atualizar catálogo |
| `ITEM_NOT_FOUND` | 404 | não | instância inexistente/indisponível |
| `ITEM_NOT_OWNED` | 403 | não | instância não pertence ao autenticado |
| `ITEM_STATE_INVALID` | 409 | não | estado impede a ação |
| `INSUFFICIENT_GOLD` | 409 | não | ouro insuficiente |
| `INSUFFICIENT_GEMS` | 409 | não | Gemas disponíveis insuficientes |
| `INSUFFICIENT_ENERGY` | 409 | não | Energia insuficiente |
| `INSUFFICIENT_FOCUS` | 409 | não | Foco insuficiente |
| `PROFESSION_LEVEL_REQUIRED` | 409 | não | nível profissional insuficiente |
| `PROFESSION_TIER_REQUIRED` | 409 | não | Tier/grau/estação insuficiente |
| `RECIPE_NOT_KNOWN` | 409 | não | receita não aprendida |
| `CRAFTING_QUEUE_FULL` | 409 | não | sem slot de fila |
| `LISTING_NOT_ACTIVE` | 409 | não | listing não está comprável/cancelável |
| `SELF_PURCHASE_FORBIDDEN` | 403 | não | comprador e vendedor coincidem |
| `PRICE_INVALID` | 400 | não | preço fora de limites inteiros |
| `GACHA_BANNER_INACTIVE` | 409 | não | banner fora da janela/flag |
| `DUNGEON_LOCKED` | 409 | não | progressão/agenda/tentativa bloqueia |
| `REWARD_ALREADY_CLAIMED` | 409 | não | claim lógico já concluído |
| `RATE_LIMITED` | 429 | sim | aguardar `Retry-After` |
| `INTERNAL_ERROR` | 500 | sim | falha segura e correlacionada |

O catálogo crescerá de forma aditiva. Um cliente desconhecendo um código usa
tratamento genérico e `safeMessage`.

## Autenticação e bootstrap

### `POST /v1/session/bootstrap`

Autentica ou vincula uma identidade externa/guest já comprovada no header.
Payload: `locale`, `timeZone`, `installId`, `attestationChallengeId` opcional e
`attestationAssertion` opcional. O body não escolhe `playerId`.

Resultado `SessionBootstrapResult`:

- `session`: access token curto, refresh token rotacionável, expirações e
  `deviceSessionId`;
- `serverTime`;
- `snapshot`, contendo `playerProfile`, `wallet`, `heroes`, `inventory`,
  `professions`, `campaign`, `energy`, `craftingJobs`, `pityStates`,
  `catalogVersion`, `featureFlags` e `revision`;
- `rulesVersion` e versões mínimas/suportadas do cliente.

Tokens devem usar armazenamento seguro da plataforma; o mock local não emite
credenciais reais.

### `POST /v1/session/refresh`

Payload: `refreshToken`, `deviceSessionId` e prova/attestation quando exigida.
Rotaciona tokens; replay de refresh token revogado encerra a família de sessão.
Resultado: novo par de tokens, expirações e `serverTime`.

### `GET /v1/player/snapshot`

Query opcional: `afterRevision`. Retorna snapshot completo ou patch suportado.
Se o patch não puder ser produzido, retorna snapshot completo. `ETag` pode
espelhar a revisão, sem substituir `expectedRevision`.

## Heróis e equipes

Todos retornam `CommandResult` com entidades alteradas, custos debitados e
patch, mas o cliente nunca envia esses resultados como fatos.

| Endpoint | Payload de intenção | Validações/resultados principais |
|---|---|---|
| `POST /v1/heroes/unlock` | `heroDefinitionId` | calcula fragmentos, cria/desbloqueia instância uma vez |
| `POST /v1/heroes/level-up` | `heroInstanceId`, `levels` | valida XP/ouro e máximo; debita ouro |
| `POST /v1/heroes/ascend` | `heroInstanceId` | calcula/debita fragmentos e ascensão |
| `POST /v1/heroes/promote-rarity` | `heroInstanceId` | valida raridade/custo/config |
| `POST /v1/heroes/equip-item` | `heroInstanceId`, `itemInstanceId`, `slotId` | valida dono, slot, binding e estados; recalcula Poder |
| `POST /v1/heroes/unequip-item` | `heroInstanceId`, `itemInstanceId` | valida vínculo atual |
| `POST /v1/teams/update` | `teamId`, `heroInstanceIds` ordenados | 1..5 heróis únicos, desbloqueados e pertencentes |

`levels` é limitado pelo servidor; não significa “defina o nível final”.

## Inventário

| Endpoint | Entrada | Resultado |
|---|---|---|
| `GET /v1/inventory` | query `cursor`, `limit`, filtros | página de `ItemSnapshot`, cursor e revisão |
| `POST /v1/inventory/lock` | `itemInstanceId` | item bloqueado pelo jogador |
| `POST /v1/inventory/unlock` | `itemInstanceId` | lock removido |
| `POST /v1/inventory/salvage` | `itemInstanceIds` | itens destruídos e outputs calculados pelo servidor |
| `POST /v1/inventory/split-stack` | `itemInstanceId`, `quantity` | nova instância e proveniência; soma conservada |
| `POST /v1/inventory/merge-stack` | `targetItemInstanceId`, `sourceItemInstanceId`, `quantity` opcional | target atualizado, source consumido/ajustado; soma conservada |

Nenhum endpoint aceita owner, binding, rarity, output ou novo instance ID
escolhido pelo cliente.

## Crafting e profissões

| Endpoint | Entrada | Regra |
|---|---|---|
| `POST /v1/crafting/jobs` | `recipeId`, `quantity`, `selectedToolInstanceId?`, `selectedCatalystInstanceId?` | reserva inputs, debita ouro/Foco e cria job |
| `POST /v1/crafting/jobs/{jobId}/cancel` | path `jobId`; payload vazio | política de janela/reembolso do catálogo |
| `POST /v1/crafting/jobs/{jobId}/claim` | path `jobId`; payload vazio | finaliza uma vez, resolve RNG/output/XP/pity |
| `GET /v1/crafting/jobs` | `status?`, `cursor?` | jobs e tempo do servidor |
| `POST /v1/professions/specialization` | `professionId` | cooldown e ouro; nunca Gemas |
| `POST /v1/professions/station-upgrade` | `professionId` | calcula custo e próximo Tier |

Worker pode tornar o job `ReadyToClaim`; apenas uma transação pode materializar
`(jobId, outputIndex)`. Ferramenta, catalisador e inputs são validados por ID e
estado, não por atributos enviados.

## Campanha e idle

| Endpoint | Entrada | Regra |
|---|---|---|
| `POST /v1/campaign/start-stage` | `stageId`, `teamId` | cria `battleId`, fixa rules/catalog/team snapshot e desafio |
| `POST /v1/campaign/complete-stage` | `battleId`, `completionEvidence { rulesVersion, eventLogHash, compactReplay }` | servidor simula ou verifica; concede first/repeat uma vez |
| `GET /v1/idle/report` | nenhuma | cria ou retorna report pendente usando relógio do servidor |
| `POST /v1/idle/claim` | `reportId` | entrega o report persistido uma vez |

### Resultado de batalha do cliente

`complete-stage` não aceita simplesmente `winner = player`. Alternativas:

1. **Simulação integral no servidor (recomendada para v1):** o servidor guarda
   snapshot, seed e rulesVersion e executa o simulador determinístico.
2. **Replay verificável:** o cliente envia log compacto/inputs; servidor repete
   a simulação e compara hash. Hash do cliente sozinho não é prova.
3. **Servidor de batalha dedicado:** útil para regras complexas/tempo real,
   com custo operacional maior.
4. **Validação por invariantes/amostragem:** somente para conteúdo de baixo
   risco; envia telemetria e o servidor verifica limites, podendo re-simular
   amostras. Não serve para recompensas críticas/PvP.

Em todas as opções, o cliente envia intenção e evidência mínima. Seed secreta,
recompensa e decisão final permanecem no servidor.

## Dungeons

| Endpoint | Entrada | Regra |
|---|---|---|
| `GET /v1/dungeons` | nenhuma | catálogo/agenda/unlocks/tentativas/Energia derivados |
| `POST /v1/dungeons/runs` | `dungeonId`, `difficultyId`, `teamId` | regenera e reserva Energia/tentativa, cria run/seed |
| `POST /v1/dungeons/runs/{runId}/complete` | `completionEvidence { rulesVersion, eventLogHash, compactReplay }`; `runId` somente no path | simula/verifica e fixa resultado |
| `POST /v1/dungeons/runs/{runId}/claim` | payload vazio | entrega normal/first clear uma vez |

Run possui estados `Created`, `EnergyReserved`, `InBattle`, `Won`, `Lost`,
`RewardsGranted`, `Cancelled` e `Failed`. Reembolso técnico exige classificação
do servidor; derrota não vira falha reembolsável.

## Gacha

| Endpoint | Entrada | Regra |
|---|---|---|
| `GET /v1/gacha/banners` | nenhuma | banners ativos, odds publicadas, custos e pity legível |
| `POST /v1/gacha/pull` | `bannerId`, `quantity` | debita moeda, gera RNG, resolve rewards, pity e history na mesma transação |
| `GET /v1/gacha/history` | `cursor`, `limit`, `bannerId?` | histórico imutável paginado |

O pull é integralmente resolvido no servidor. O cliente não envia seed,
raridade, fragmentos, recompensa escolhida, pity final ou saldo.

## Mercado P2P

Estados persistidos, com IDs que nunca serão reordenados:

| ID | Estado |
|---:|---|
| 0 | `Draft` |
| 1 | `Active` |
| 2 | `Reserved` |
| 3 | `Sold` |
| 4 | `Cancelled` |
| 5 | `Expired` |
| 6 | `Failed` |

| Endpoint | Entrada | Regra |
|---|---|---|
| `GET /v1/market/listings` | cursor/limit/filtros/ordenação | read model público autenticado |
| `GET /v1/market/listings/{listingId}` | path | detalhe/snapshot do item |
| `POST /v1/market/listings` | `itemInstanceId`, `priceGems` | cria listing e move item para escrow |
| `POST /v1/market/listings/{listingId}/cancel` | payload vazio | somente vendedor; devolve item no mesmo commit |
| `POST /v1/market/listings/{listingId}/buy` | payload vazio | settlement atômico |
| `GET /v1/market/my-listings` | cursor/status | listings do autenticado |
| `GET /v1/market/history` | cursor/role | compras/vendas do autenticado |

`priceGems`, taxa e líquido são `long`. A taxa inicial é `1.000` basis points:

```text
fee = ceil(price × 1000 / 10000)
sellerNet = price - fee
```

Para evitar overflow, a implementação usa divisão inteira verificada
equivalente a `price / 10000 * 1000` mais o resto, ou inteiro de precisão maior
no servidor. `float` e `double` não são usados para moeda.

### Compra atômica

Uma única transação PostgreSQL:

1. deduplica o comando;
2. valida comprador autenticado;
3. bloqueia o listing;
4. valida `Active`, validade e item correspondente em escrow;
5. impede autocompra;
6. bloqueia e valida carteiras/saldo;
7. calcula taxa;
8. debita comprador;
9. credita vendedor;
10. registra queima de 10%;
11. transfere item e limpa escrow;
12. conclui listing;
13. grava três pernas do ledger;
14. grava `market_transaction`;
15. grava audit/outbox e resposta de dedup;
16. confirma tudo ou nada.

Locks são adquiridos em ordem determinística para reduzir deadlocks. Notificação
e analytics ocorrem após commit via outbox.

## Comissões futuras

Os endpoints ficam reservados em v1 e podem retornar feature locked até a task
correspondente:

| Endpoint | Entrada |
|---|---|
| `POST /v1/crafting-commissions` | `recipeId`, `quantity`, `serviceFeeGems`, `expiresInSeconds` |
| `POST /v1/crafting-commissions/{commissionId}/accept` | `selectedToolInstanceId?`, `selectedCatalystInstanceId?` |
| `POST /v1/crafting-commissions/{commissionId}/cancel` | payload vazio |
| `POST /v1/crafting-commissions/{commissionId}/claim` | payload vazio |

Materiais do comprador e fee ficam em hold/escrow. Artesão não pode ser o
comprador. Conclusão transfere output ao comprador, líquido ao artesão e queima
10% na mesma transação ou em saga explicitamente visível e reconciliável.

## Paginação e compatibilidade

- `limit` padrão 50, máximo definido por endpoint; cursor é opaco e curto.
- Schemas têm `schemaVersion`; mudanças aditivas não reutilizam significado de
  campos.
- Remoção/renomeação ou mudança de enum exige `/v2` ou migração negociada.
- Campos desconhecidos são ignorados quando opcionais; campos obrigatórios
  ausentes falham com `REQUEST_INVALID`.
- Raridade v1 da API usa IDs textuais ou enum explícito
  `Common=0..Mythic=5`; nunca interpreta a enum v1 histórica do cache.
