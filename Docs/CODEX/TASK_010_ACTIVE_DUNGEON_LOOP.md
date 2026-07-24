# TASK 010 — Loop de masmorras ativas

Data: 2026-07-24

Branch: `feat/active-dungeon-loop`

Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo e autoridade

Esta task implementa um protótipo local de masmorras ativas focadas em
materiais. A entrada consome Energia, a luta reutiliza o simulador determinístico
da Task 005, e uma vitória resolve drops por uma seed criada pela fronteira local
que representa o servidor.

O cliente continua não autoritativo. Em produção, Energia, relógio, seed,
formação, resultado, tentativas, first clear, inventário, ouro e deduplicação
serão validados e persistidos pelo servidor. Não foram implementados compra de
Energia, Gemas, anúncios, monetização ou eventos sazonais.

## Definições

- `DungeonDefinition`: ID, nome, descrição, profissão, estágio de desbloqueio,
  dificuldades, limite diário opcional, metadata futura de agenda, ícone
  placeholder e tags.
- `DungeonDifficultyDefinition`: Poder recomendado, custo de Energia, encontro,
  tabela normal, first clear, nível mínimo, estágio requerido, duração estimada,
  Tier material e bloqueio opcional por Poder.
- `DungeonEncounterDefinition`: formação inimiga, `scenarioId` e nome da cena
  Battle usada pelo cenário visual de dungeon.
- `DungeonRewardTable`: ouro opcional e entradas independentes.
- `DungeonEntryRequest`: `requestId`, masmorra, dificuldade, equipe e nível.
- `DungeonRun`: identidade, seed simulada de servidor, timestamps, configuração,
  requisição/batalha e estado.
- `DungeonRunResult`: resultado de combate, first clear, ouro e materiais.
- `DungeonProgress`: first clears, tentativas UTC por dia e revisão.

Conteúdo demonstrativo:

| Masmorra | Profissão | Dificuldades |
|---|---|---:|
| Mina de Minérios | Ferreiro | 3 |
| Floresta de Couros e Fibras | Costureiro | 1 |
| Ruínas Arcanas | Encantador | 1 |
| Laboratório Abandonado | Alquimista | 1 |
| Expedição de Coleta | Coletador | 1 |

Todo o conteúdo jogável permanece T1. Isso demonstra as regras sem produzir
uma matriz artificial de T1 a T9. Foram acrescentados somente Fibra de Linho,
Erva Selvagem e Reagente Alquímico T1 ao catálogo.

Energia e todo o balanceamento de masmorras ficam serializados em
`DungeonConfig.asset`: Poder recomendado, custos, limites diários, formações,
multiplicadores inimigos, duração, Tier, tabelas, quantidades, chances e ouro.
Os valores em código servem apenas para inicializar um asset novo; depois disso,
`BuildCatalog()` materializa exclusivamente os dados autorados no asset.

## Energia

`EnergyWallet` contém `currentEnergy`, `maximumEnergy`,
`lastRegenerationTime` UTC e `revision`, todos inteiros onde aplicável.
`EnergyRegenerationRules` configura minutos por ponto e máximo.

A regeneração é agregada:

```text
pontos = floor((agoraUtc - últimaRegeneraçãoUtc) / intervalo)
energia = min(máximo, energia + pontos)
```

Não existe loop por minuto ou frame. Frações de intervalo são preservadas
enquanto a carteira não chega ao máximo. Ao atingir o máximo, o timestamp é
ancorado no momento observado para impedir acumular Energia invisível acima do
cap.

`IGameClock` é a abstração de relógio já usada pela campanha.
`LocalGameClock` usa UTC local ancorado em `Stopwatch`; é adequado apenas para
protótipo. Testes usam `DevelopmentGameClock`.

## Entrada e estados da run

A entrada valida, antes do débito:

1. masmorra e dificuldade;
2. estágio de campanha e nível;
3. equipe de um a cinco heróis válidos;
4. ausência de run ativa conflitante;
5. tentativas diárias;
6. Energia;
7. Poder, somente quando a dificuldade configura bloqueio;
8. `requestId`.

Poder abaixo do recomendado é aviso por padrão. O protótipo usa uma equipe
demonstrativa de Paladino, Arqueira e Mago.

Estados persistíveis, com IDs explícitos:

```text
Created
  → EnergyReserved
      → InBattle
          → Won → RewardsGranted
          → Lost

Created/EnergyReserved → Cancelled
qualquer estado não terminal → Failed
```

Cancelamento voluntário só é permitido antes de `InBattle` e devolve Energia e
a tentativa reservada. Depois do início, não há devolução. A única exceção é
`TechnicalRefundable`, uma classificação explícita de falha técnica; erros do
jogador e falhas técnicas não reembolsáveis não devolvem Energia.
`Lost` é terminal: uma derrota já concluída não pode ser reclassificada como
falha técnica nem receber reembolso retroativo.

## Combate e apresentação

`DungeonService` materializa `BattleRequest` e chama `BattleSimulator` uma única
vez. Nenhuma fórmula de alvo, turno, acerto ou dano foi copiada.

`BattleScenarioBridge` entrega o `BattleRequest` e o `BattleResult` já
materializados à cena `Battle`. `BattleEventPlayer` continua apenas reproduzindo
o log. Ao terminar ou pular a reprodução, a ponte conclui a run e retorna à
cena `Dungeon`.

Cada encontro possui `scenarioId`, permitindo substituir arena, iluminação ou
arte no futuro sem alterar regras de combate ou drops.

## Drops e integração econômica

Cada entrada da tabela possui:

- `itemDefinitionId`;
- quantidade mínima e máxima inclusivas;
- `chanceBasisPoints` entre 0 e 10.000;
- `guaranteed`;
- `firstClearOnly`;
- `difficultyMultiplier` em basis points.

Entradas são roladas independentemente por `DungeonRewardResolver` e
`DeterministicRandom`. A UI mostra possibilidades, mas não recebe API para
escolher seed, roll ou saída.

Vitórias materializam itens no `PlayerInventory` com IDs derivados de
`runId + entryIndex + stackIndex` e proveniência `local_dungeon_simulation`.
Quantidades respeitam `MaxStackSize`. Ouro positivo usa
`LocalGoldEconomyService` com `requestId` lógico `dungeon_gold:{runId}`.
Derrotas não concedem recompensas.

## Idempotência e first clear

- `requestId` repetido retorna a mesma `DungeonRun` somente quando masmorra,
  dificuldade, nível e equipe ordenada são idênticos; qualquer divergência de
  payload é rejeitada;
- `runId` é derivado de forma estável do `requestId` no protótipo;
- a seed é criada pela fonte local de servidor e armazenada na run;
- `BeginBattle` repetido durante `InBattle` retorna a batalha já simulada;
- `CompleteBattle` repetido retorna o mesmo `DungeonRunResult`;
- uma run em `RewardsGranted` não pode cunhar itens nem ouro novamente;
- first clear usa a chave lógica `dungeonId:difficultyId` em `DungeonProgress`;
- a chave só é marcada depois da entrega local;
- a tabela normal e a tabela de first clear usam seeds determinísticas
  separadas.

Produção precisa garantir essas propriedades com command deduplication,
unicidade de saída, transação, ledger, outbox e reconciliação.

## Interface

A cena `Dungeon` apresenta:

- lista das cinco masmorras;
- tags/tipo e profissão;
- seletor de dificuldade;
- Poder recomendado e Poder da equipe;
- custo e Energia atual;
- regeneração;
- recompensas possíveis e chances em basis points;
- tentativas e bloqueios;
- aviso de Poder;
- botão Entrar;
- resultado, first clear, ouro e materiais obtidos.

A cena `Battle` recebeu navegação para Masmorras. A tela desabilita Entrar
quando detecta bloqueio, Energia insuficiente ou tentativas esgotadas e também
converte rejeições do serviço em feedback explícito.

## Testes e validação

Os testes EditMode adicionados cobrem:

- regeneração agregada e limite máximo;
- Energia insuficiente, consumo único e `requestId` repetido;
- dificuldade inexistente e conteúdo bloqueado;
- vitória e derrota;
- first clear e conclusão não duplicada;
- chances 0 e 10.000;
- quantidade mínima/máxima;
- inventário;
- limite diário;
- reserva e liberação de tentativa atravessando meia-noite UTC;
- cancelamento antes/depois da batalha;
- falha técnica reembolsável;
- derrota terminal sem reembolso retroativo;
- rejeição de payload divergente no mesmo `requestId`;
- materialização do balanceamento alterado via dados serializados;
- composição da cena.

O teste PlayMode cobre a jornada:

```text
abrir Dungeon
  → selecionar Mina
  → selecionar dificuldade
  → entrar e atualizar Energia
  → carregar Battle
  → reproduzir/pular conclusão
  → retornar a Dungeon
  → exibir recompensa
  → validar inventário
```

Resultados finais:

- importação/compilação: código 0, sem `error CS` ou `warning CS`;
- EditMode: `249 total, 249 passed, 0 failed, 0 skipped`;
- PlayMode: `14 total, 14 passed, 0 failed, 0 skipped`;
- validação do catálogo: 0 erros e 0 avisos;
- validação da cena Dungeon: cinco masmorras válidas;
- validação estrutural: projeto válido, somente com o aviso preexistente de
  `DefaultCompany`.

Logs e XMLs ficam em `%TEMP%/IdleMedievalLegends-validation-task010`.

## Segurança futura

O backend deverá:

- usar relógio UTC e revisão do servidor;
- regenerar e reservar Energia atomicamente;
- deduplicar por jogador, tipo de comando e `requestId`;
- criar `runId` e seed sem expor controle ao cliente;
- validar campanha, equipe, Poder, agenda e tentativas;
- executar ou verificar a batalha oficial;
- entregar first clear, ouro e itens em uma transação;
- impor unicidade por `runId + outputIndex`;
- registrar ledger, proveniência e outbox;
- reembolsar somente falhas técnicas classificadas pelo servidor.

O cliente deverá enviar apenas intenção, por exemplo:

```text
EnterDungeon(dungeonId, difficultyId, teamSnapshotId, requestId)
CompleteDungeonRun(runId, requestId)
CancelDungeonRun(runId, requestId)
```

## Limitações

- Energia, progresso e runs de dungeon permanecem em memória; ainda não foram
  adicionados ao cache local.
- O protótipo usa somente três heróis existentes e inimigos placeholder.
- Não há monstros, habilidades, chefes ou cenários artísticos específicos.
- Agenda existe apenas como metadata futura, sem eventos sazonais.
- A UI runtime usa uGUI e placeholders; faltam arte, localização, safe area,
  acessibilidade e layout final.
- A ponte visual é efêmera e não recupera uma cena interrompida após encerrar o
  processo.
- O serviço local não oferece transação ACID entre inventário, carteira,
  progresso e Energia.
- Builds Android/iOS e smoke tests em dispositivo não fazem parte da validação
  EditMode/PlayMode.

## Ausência de commit

Nenhum commit é criado automaticamente por esta task.
