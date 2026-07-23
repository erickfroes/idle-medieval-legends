# TASK 007 — Vertical slice de inventário

Data: 2026-07-22

Branch: `feat/inventory-vertical-slice`

Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo

Entregar um inventário local funcional para prototipagem sem confundir catálogo,
instância, regra de domínio, cache e apresentação. O cliente continua sem
autoridade de produção sobre propriedade, criação, destruição, binding, mercado,
crafting, recompensas ou moedas.

## Arquitetura

- `ItemDefinition`, `EquipmentDefinition` e `MaterialDefinition` descrevem
  conteúdo estático. Rendimentos de desmontagem ficam em
  `DismantleYieldDefinition`.
- `ItemInstance` representa identidade e estado persistido. Coleções internas são
  expostas somente como leitura.
- `PlayerInventory` é o agregado que aplica snapshots e controla transições.
- `InventoryRules` contém equipamento, desmontagem, filtros, ordenação e o
  provider de modificadores de equipamento.
- `LocalJsonPlayerStateRepository` persiste somente cache descartável.
- `InventoryPanelController`, `ItemDetailView` e
  `InventoryConfirmationDialog` apresentam e encaminham intenções; não calculam
  Poder, rendimento ou elegibilidade econômica.
- `DevelopmentInventorySeeder` simula resultados autorizados exclusivamente em
  Editor, Development Build e testes.

## ItemInstance

O snapshot contém:

- `instanceId`, `definitionId`, `ownerPlayerId` e `quantity`;
- `state`, `binding`, `boundHeroInstanceId`, `equippedHeroInstanceId`,
  `marketListingId` e `reservationId`;
- `rollSeed`, `rollSeedHash`, `rolledStats` e `enhancementLevel`;
- `durability`/`maxDurability` quando aplicáveis e `lockedByPlayer`;
- `serverVersion`, `localVersion`, `createdAtUnixMilliseconds` e
  `updatedAtUnixMilliseconds`;
- `provenance`, além dos metadados de crafting já existentes no schema v2.

IDs de desenvolvimento usam GUID aleatório textual e não são identidade
definitiva. O backend deverá fornecer UUIDv7/ULID e versões oficiais.

## Estados e transições

Os valores numéricos persistidos foram preservados: `Owned=0`, `Equipped=1`,
`Escrow=2`, `ReservedByServer=3`, `Consumed=4` e `Destroyed=5`.

| Origem | Comando | Destino | Validações principais |
|---|---|---|---|
| snapshot | adicionar autorizado | Owned | ID único, definição/dono/quantidade válidos |
| Owned | equipar | Equipped | equipamento, herói elegível, slot livre, binding |
| Equipped | desequipar | Owned | item realmente equipado |
| Owned | reservar | ReservedByServer | reservationId obrigatório |
| ReservedByServer | liberar | Owned | reservationId correspondente |
| Owned | anunciar simulado | Escrow | anúncio, tradable e Unbound |
| Escrow | cancelar | Owned | listingId correspondente |
| Owned | consumir/remover tudo | Consumed | quantidade disponível |
| Owned | desmontar | Destroyed | regra e rendimento configurados |
| Owned | transferir autorizado | removido do agregado vendedor | Unbound, tradable, resultado auditável |

`Consumed` e `Destroyed` são terminais. Item reservado não pode ser vendido,
equipado, desmontado ou consumido. Escrow bloqueia as mesmas mutações. Item
bloqueado pelo jogador não pode ser desmontado. Equipamento único sempre possui
quantidade um enquanto ativo.

Split cria outra identidade e registra o pai na proveniência. Merge conserva a
quantidade ativa e encerra a pilha fonte como `Consumed`.

## Equipamento, atributos e Poder

`EquipmentRules` exige `Owned`, `EquipmentDefinition`, dono compatível, nível,
tags do herói e slot livre. Reequipar o mesmo item ou ocupar o mesmo slot gera
erro explícito. `UnboundUntilEquipped` torna o item `AccountBound`; `HeroBound`
somente equipa no herói vinculado.

`InventoryEquipmentModifierProvider` agrega `health`, `attack`, `defense` e
`speed` dos `rolledStats` de itens equipados no herói. Ele implementa
`IHeroEquipmentModifierProvider`, portanto `HeroPowerCalculator` continua sendo
o único local que aplica ordem, arredondamento, atributos finais e Poder. A UI
apenas mostra o breakdown já modelado.

## Filtros e ordenação

`InventoryQuery` é C# puro e combina:

- todos, equipamentos, materiais e consumíveis;
- Tier e raridade opcionais;
- somente equipados, bloqueados ou negociáveis;
- Tier decrescente, raridade decrescente, stat budget/Poder, nome, quantidade e
  aquisição.

Empates usam `instanceId`, mantendo resultado determinístico.

## Desmontagem de protótipo

`InventoryDismantleRules` valida estado, lock, definição e outputs configurados.
O catálogo demonstrativo devolve lingote, couro tratado ou essência conforme o
equipamento. Outputs de item vinculado também nascem vinculados, impedindo
lavagem econômica.

`DevelopmentInventorySeeder.Dismantle` materializa esses materiais localmente,
com proveniência `development_authority`. A UI exige confirmação antes de chamar
esse serviço. Não existe botão para adicionar item arbitrário.

## Seeder e segurança

O arquivo inteiro do seeder usa:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
```

O Bootstrap possui `allowDevelopmentInventorySeed: true` configurado
explicitamente pela ferramenta de cena. O seed só ocorre se o agregado estiver
vazio. Builds de produção não compilam o seeder nem executam mutações locais da
UI. Nenhuma Gema, compra, anúncio real ou transferência P2P foi implementada.

O backend substituirá:

- criação/remoção, split/merge e transições oficiais;
- IDs, timestamps, seeds, binding e versões;
- reservas, escrow, desmontagem e transferência;
- validação de dono, catálogo e concorrência;
- proveniência/ledger e snapshots de leitura.

## Serialização e recuperação

`GameSaveData` e `InventorySnapshotData` usam schema 4. O inventário persiste
`revision` local separada de `serverRevision`. `JsonUtility` serializa
listas, nunca `Dictionary`.

Mutações de protótipo incrementam apenas `revision`/`localVersion`; não avançam
`serverRevision` nem `serverVersion`. Assim, um snapshot autoritativo N+1 não é
rejeitado depois de ações locais sobre N. Na migração v3→v4, a revisão
misturada antiga nunca é promovida a revisão de servidor, pois cache não é
autoridade.

O loader trata:

- arquivo ausente: cria cache vazio;
- arquivo vazio ou JSON inválido: registra aviso e descarta;
- schema futuro incompatível: registra aviso e descarta;
- item duplicado, estado inválido, definição inexistente ou dono divergente: o
  agregado rejeita o snapshot inteiro;
- erro grave: o `GameManager` limpa uma vez, sem copiar conteúdo parcial; seed
  só é recriado quando a flag de desenvolvimento permite explicitamente.

A escrita permanece atômica por arquivo temporário + replace/backup.

## UI e navegação

`BattleSceneController` instala o botão **Inventário**. Ele abre
`Assets/_Game/Scenes/Inventory.unity`, habilitada nos Build Settings. A cena
oferece retorno à Batalha, filtro cíclico, lista, placeholder de ícone, nome,
Tier, raridade, quantidade, estado, lock, detalhes, stats e ações de equipar,
desequipar, bloquear, desbloquear e desmontar.

`ItemDetailView` é reutilizável e recebe um `InventoryViewEntry` com callbacks.
`InventoryConfirmationDialog` protege a ação destrutiva.

## Testes

EditMode cobre adição, ID duplicado, definição inexistente, split/merge,
definições diferentes, equipamento e slot duplicado, desequipamento, reserva,
escrow, binding, consumo, destruição terminal, bloqueio, desmontagem, transferência
autorizada, filtros, ordenação, modificadores, integração com Poder, round-trip de
JSON, estado inválido, migração, separação de revisões, atomicidade para timestamp
obsoleto e validação da cena.

PlayMode cobre abertura, listagem, seleção, equipar/desequipar, bloquear,
filtrar, confirmação/desmontagem, reinício do Bootstrap restaurando cache e o
retorno Inventory→Battle com um único `EventSystem`.

Resultados finais registrados após a última execução:

- compile/import: código 0, sem `error CS` nem `warning CS`;
- EditMode: 166 executados, 166 aprovados, zero falhas/ignorados;
- PlayMode: 10 executados, 10 aprovados, zero falhas/ignorados;
- geração/validação de catálogo e cena: código 0;
- validação estrutural: projeto válido, com o aviso preexistente de
  `DefaultCompany` em Player Settings;
- builds Android/iOS e dispositivo: não executados.

A primeira execução PlayMode da UI teve 3 falhas por uma segunda `Image` no
diálogo de confirmação. A composição passou a reutilizar o componente existente;
as execuções posteriores são as consideradas válidas.

## Limitações e riscos

- Não existe backend; qualquer edição do cache será substituída pelo estado
  oficial quando a integração autoritativa existir.
- O herói de equipamento da UI é o Paladino local `dev_hero_paladin`; coleção e
  seleção definitiva de heróis ainda não existem no `GameManager`.
- Arte, localização, safe area, acessibilidade, virtualização de lista e layout
  responsivo final permanecem pendentes.
- O catálogo ainda é monolítico e o seed usa apenas conteúdo T1 demonstrativo.
- `rollSeed` existe para o contrato pedido/cache de protótipo; produção deve
  expor somente hash/auditoria e manter a seed secreta no servidor.
- Não foram realizados mercado, P2P, Gemas, crafting completo, builds móveis ou
  smoke test em dispositivo.

## Ausência de commit

Nenhum commit foi criado automaticamente nesta task.
