# TASK 009 — Loop idle e recompensas offline

Data: 2026-07-23

Branch: `feat/idle-progression-loop`

Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo e fronteira de autoridade

Esta task implementa uma simulação local do ciclo:

```text
campanha
    → batalha determinística
    → avanço/first clear
    → produção passiva agregada
    → retorno offline
    → relatório imutável
    → coleta em carteira/inventário
    → fortalecimento futuro da equipe
```

O cliente local não é autoridade de produção. O relógio do aparelho, o
resultado da batalha, o saldo, o inventário, a revisão, o estágio, o relatório
e a deduplicação deverão ser validados e persistidos pelo servidor. Nenhuma
sincronização online, Gema, anúncio, IAP ou mercado foi implementado.

## Arquitetura

- `CampaignDefinition`, `CampaignChapterDefinition` e
  `CampaignStageDefinition` modelam conteúdo estático e ordenado.
- `StageEnemyFormation` descreve placeholders materializados pelo simulador de
  batalha existente.
- `StageRewardDefinition` separa recompensa única e recompensa de repetição.
- `PlayerCampaignProgress` é o snapshot versionado de progresso, deduplicação,
  timestamps, XP e relatório pendente.
- `IdleProductionProfile` materializa as taxas e limites do maior estágio
  concluído que liberou produção idle.
- `IdleRewardCalculator` valida tempo e calcula períodos inteiros sem loop por
  frame ou segundo.
- `OfflineSession` é a entrada do cálculo; `OfflineRewardReport` é o resultado
  persistível que será aplicado sem novo cálculo.
- `IdleProgressionService` orquestra batalha, progresso, first clear, cálculo,
  coleta, inventário e carteira na simulação local.
- `IGameClock` é a porta para um relógio autoritativo futuro.
- `LocalGameClock` usa UTC local ancorado em `Stopwatch`, mas continua não
  autoritativo.
- `DevelopmentGameClock` permite avançar horas somente em Editor,
  `DEVELOPMENT_BUILD` ou testes.
- `LocalGoldEconomyService` agora mantém uma carteira exclusiva de ouro e um
  ledger local. Ela é compartilhada pelo crafting e pela campanha.

O cache local subiu para schema 5 e passou a armazenar campanha e carteira.
Cache ausente ou inválido é descartado; isso não transforma o JSON local em
prova de propriedade.

## Campanha demonstrativa

`CampaignConfigAsset` gera exatamente dez estágios distribuídos entre
`chapter_frontier` e `chapter_highlands`. Cada estágio contém:

- `stageId`, `chapterId` e sequência global;
- formação de um a três inimigos placeholder;
- Poder recomendado;
- ouro por minuto e tabela de materiais por minuto;
- first clear e recompensa de repetição;
- flags de idle, boss, limite offline e tags.

Os placeholders reutilizam Paladino, Arqueira e Mago. A equipe do jogador
também usa os três heróis existentes e consulta o provider de equipamentos do
inventário. Não foram adicionadas centenas de definições.

Sementes configuráveis atuais:

```text
PoderRecomendado(1) = 1.600
PoderRecomendado(n + 1) =
    PoderRecomendado(n)
    + floor(PoderRecomendado(n) × 1.800 / 10.000)

OuroPorMinuto(n) = 10 + 3 × (n - 1)

MultiplicadorDeStatsInimigosBp(n) =
    6.500 + 900 × (n - 1)
    + 1.000 quando boss
```

Os estágios 5 e 10 são bosses. Todos os números são sementes de protótipo
autoráveis no asset e ainda precisam de simulação de volume e telemetria.

## Avanço e recompensas de batalha

O serviço expõe início e conclusão separados:

```text
StartStageBattle(stageId, requestId, seed)
    → BattleRequest com três heróis e formação do estágio

CompleteStageBattle(stageId, requestId, BattleResult)
    → valida battleId + rulesVersion
    → derrota: mantém estágio
    → vitória do estágio atual: libera o próximo
    → replay anterior: não regride estágio atual nem maior estágio
```

O first clear usa a chave lógica `first_clear:{stageId}`. O progresso mantém
estágios concluídos, first clears pendentes/entregues e requests de batalha.
Assim, uma recuperação repete a mesma chave e não credita ouro nem cria
materiais novamente. Um replay usa `repeat:{battleRequestId}` e recebe somente
a recompensa de repetição.

## Produção e fórmula offline

Produção idle usa o maior estágio concluído com `idleUnlocked=true`. Antes da
primeira conclusão, o perfil existe para apresentação, mas suas taxas efetivas
são zero.

Para cada saída:

```text
Recompensa =
    floor(
        TaxaPorMinuto
        × DuraçãoElegívelEmMilissegundos
        / 60.000
    )
    × MultiplicadoresPermitidosEmBasisPoints
```

Cada multiplicador usa aritmética inteira:

```text
valor = floor(valor × multiplicadorBp / 10.000)
```

Ouro, quantidades, XP, saldos e deltas usam `long`. Não há distribuição por
frame e não existe loop por segundo; uma ausência de horas custa a mesma
quantidade constante de operações que uma ausência de minutos.

```text
DuraçãoElegível = min(
    TempoDecorridoValidado,
    LimiteOfflineDoJogador,
    LimiteDoEstágio
)

TempoDescartado = max(
    0,
    TempoReal - DuraçãoElegível
)
```

O limite inicial do jogador e dos estágios é 8 horas, configurável e preparado
para expansão futura. O perfil contém ouro/min, materiais/min, XP opcional,
duração acumulada e os dois limites.

## Tempo e manipulação local

O protótipo usa UTC, mas nunca afirma impedir trapaça. A validação atual é:

| Situação | Resultado local |
|---|---|
| timestamp ausente | inicializa a sessão e concede zero retroativo |
| fim anterior ao início | regressão; zero recompensa e aviso |
| salto até 720h | usa o tempo observado, sujeito aos limites offline |
| salto acima de 720h | registra aviso e aplica limite seguro de 8h |
| snapshot com revisão menor | rejeita antes de substituir o progresso |

`IGameClock.IsAuthoritative` e `Source` deixam explícito o contrato do adapter.
Uma implementação de produção deverá obter tempo/revisão do servidor, e não
apenas trocar a origem de `UtcNow`.

## Relatório e idempotência da coleta

`OfflineRewardReport` contém:

- início, fim, duração real, elegível e descartada;
- estágio usado;
- ouro, materiais e XP;
- multiplicadores;
- revisão, `requestId`, estado `collected`;
- resultado/aviso da validação temporal.

O relatório é gerado uma vez e armazenado em
`PlayerCampaignProgress.PendingOfflineReport`. O botão **Coletar** aplica esse
objeto; ele não consulta o relógio nem chama o calculador novamente.

O ouro usa `offline_gold:{requestId}` no ledger. Materiais usam IDs determinísticos
derivados de `offline_materials:{requestId}` e são divididos pelo limite de
stack do catálogo. Repetir a coleta devolve o relatório já coletado durante a
sessão ou rejeita a chave persistida após reinício, sem nova entrega.

## Carteira e ledger local

`LocalGoldEconomyService` continua sendo uma simulação de desenvolvimento, mas
agora registra entradas append-only com:

- `entryId`;
- `reason`;
- `delta`;
- `balanceAfter`;
- `requestId`;
- `timestamp`;
- `source`.

O saldo e cada delta são `long`; saldo negativo e overflow são rejeitados.
`GoldWalletSnapshot.Validate` reconcilia cada `balanceAfter`, o saldo final e
requests duplicados. A validação pós-desserialização também rejeita saldo ou
revisão negativos, metadados ausentes, timestamps inválidos, IDs de entrada
duplicados e revisão divergente do ledger. Ao substituir o jogador ativo, o
protótipo descarta a carteira anterior e recria crafting e progressão idle com
uma carteira vinculada à nova sessão local. O ledger local é referência
arquitetural e não oferece imutabilidade, autenticação, ACID, outbox ou
segurança contra edição do cache.

## Interface e integração

A cena `Campaign` possui:

- estágio atual e maior estágio;
- Poder recomendado e Poder da equipe;
- botão **Batalhar** e resultado;
- ouro por minuto, saldo e limite offline;
- botão de retorno à cena `Battle`;
- simulador `+8h`, oculto fora de builds de desenvolvimento;
- modal de retorno com duração, ouro, XP, materiais e **Coletar**.

A cena `Battle` recebeu navegação para Campanha. O `GameManager` compartilha
inventário, carteira e catálogo entre campanha e crafting. Materiais offline
entram no `PlayerInventory` com proveniência de simulação local. O cache é
salvo após batalha/coleta e em pausa. Um relatório recém-gerado é persistido
antes de ser atribuído ao modal; a coleta aplica esse mesmo relatório. A taxa
mostrada na interface vem do perfil produtivo do maior estágio concluído
válido, incluindo taxa zero antes da primeira conclusão.

## Testes e validação

Os testes EditMode específicos cobrem:

- zero, uma hora, oito horas e acima do limite;
- regressão, salto extremo e timestamp ausente;
- cálculo de ouro/materiais e valores acima de `int`;
- vitória, derrota, avanço e replay sem regressão;
- first clear, repetição e não duplicação;
- coleta com mesmo `requestId`;
- relatório não recalculado depois de avançar o relógio;
- consistência do ledger;
- rejeição de snapshots de carteira malformados após desserialização;
- isolamento da carteira ao trocar o jogador ativo;
- dez estágios e crescimento de Poder;
- revisão antiga;
- round-trip de campanha, relatório e carteira;
- composição da cena.

O teste PlayMode abre a campanha, batalha, conclui, avança, simula retorno,
confirma que o relatório foi persistido antes da abertura, valida a taxa do
perfil produtivo, coleta, verifica carteira/inventário e tenta coletar de novo.

Resultados originais e da correção de revisão em
`%TEMP%/IdleMedievalLegends-validation-task009` e
`%TEMP%/IdleMedievalLegends-validation-task009-review`:

- importação/compilação: código 0, sem `error CS` ou `warning CS`;
- EditMode: `225 total, 225 passed, 0 failed, 0 skipped`;
- PlayMode: `13 total, 13 passed, 0 failed, 0 skipped`;
- geração/validação de campanha: código 0, dez estágios válidos;
- segunda geração: hashes de Campaign, config, Bootstrap e Build Settings
  permaneceram idênticos;
- validação do projeto: código 0, projeto válido e somente o aviso preexistente
  de `DefaultCompany`;
- Console das suítes: sem exceções de gameplay; o teste de recuperação de JSON
  inválido registra intencionalmente seu warning, e o Unity em batchmode
  registrou mensagens de licença sem afetar as execuções.

Builds Android/iOS e testes em dispositivo não foram executados.

## Segurança futura

Produção deverá mover para o servidor:

- relógio, revisão e duração elegível;
- snapshot da equipe e resultado oficial da batalha;
- estágio desbloqueado e first clear;
- geração/assinatura do relatório;
- command deduplication por jogador e `requestId`;
- transação de carteira, inventário, progresso, ledger e outbox;
- IDs globais, proveniência, reconciliação e auditoria.

O cliente deverá enviar apenas intenção, por exemplo:

```text
StartCampaignBattle(stageId, requestId)
CompleteCampaignBattle(battleId, requestId)
ClaimOfflineRewards(requestId)
```

O relatório devolvido pelo servidor deverá ser aplicado como snapshot
autoritativo, nunca recalculado com o relógio do aparelho.

## Limitações

- Heróis da campanha permanecem snapshots locais nível 1; progressão visual de
  herói/equipamento continua em outras fatias.
- Inimigos são os três heróis existentes com escala placeholder, sem novo
  catálogo de monstros, habilidades ou IA avançada.
- A batalha da interface é resolvida de forma síncrona; a apresentação visual
  da Task 006 ainda não reproduz uma batalha de campanha.
- O relatório mantém uma tabela por minuto; chance por intervalo não foi
  necessária nesta primeira fatia.
- O ledger/cache local pode ser editado e não resiste a fraude.
- Não há rede, backend, anúncios, IAP, Gemas ou mercado.
- Não foram realizados build móvel, safe area, localização, acessibilidade,
  arte ou balanceamento de produção.

## Ausência de commit

Nenhum commit é criado automaticamente por esta task.
