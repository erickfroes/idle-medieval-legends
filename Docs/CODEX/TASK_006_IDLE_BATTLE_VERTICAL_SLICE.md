# TASK 006 — Primeiro vertical slice visual de batalha idle

Data: 2026-07-21

Branch: `feat/idle-battle-vertical-slice`

Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo

Entregar uma batalha visual mínima que reproduz o `BattleResult` determinístico
da Task 005 sem mover regras de combate para MonoBehaviours. A cena é um slice
substituível: usa primitivas, uGUI e animações pequenas por corrotina, sem arte,
VFX ou UI definitivos.

## Fluxo

`BootstrapBattleLoader` observa o `GameManager`. Quando o Bootstrap alcança
`Ready`, ele carrega `Battle` em modo Single. O objeto `App` continua vivo por
ser a composição persistente existente.

Na cena Battle, `BattleDebugScenarioProvider` resolve o catálogo e tuning pelas
referências serializadas, materializa Paladino, Arqueira e Mago em cada lado e
executa o simulador uma única vez. O resultado e a requisição inicial formam um
`BattleDebugScenario`:

```text
Bootstrap Ready
    -> BattleDebugScenarioProvider
    -> BattleRequest 3v3 + seed 6006
    -> BattleSimulator.Simulate (uma vez)
    -> BattleResult imutável
    -> BattleEventPlayer
    -> BattleUnitView / HealthBarView / BattleHudView
```

O lado inimigo reutiliza as mesmas definições estáticas com IDs de instância
`enemy_debug_*` e rótulo `[Debug]`. Isso deixa explícito que são variações de
demonstração, não novas definições do catálogo.

## Separação entre domínio e apresentação

O assembly `IdleMedievalLegends.Presentation.Battle` depende de Domain,
Application e Config. `IdleMedievalLegends.Domain` não recebeu referência à
apresentação ou a UnityEngine.

- `BattleSceneController`: composição e ciclo de vida da cena;
- `BattlePresenter`: máquina de estados e posse do resultado;
- `BattleEventPlayer`: percorre o log em ordem e anima o efeito correspondente;
- `BattleUnitView`: posição, seleção, cor, vida e derrota de um placeholder;
- `BattleTeamView`: associa slots/unidades às views de um lado;
- `HealthBarView`: normaliza e exibe vida, sem calcular dano;
- `BattleHudView`: seed, equipes, status, resultado e botões;
- `BattleSpeedController`: contrato fechado de 1x, 2x e 3x;
- `BattlePresentationConfig`: tempos e distância de aproximação autoráveis;
- `BattleDebugScenarioProvider`: fronteira de catálogo/tuning do cenário local.

Os estados são `Uninitialized`, `Preparing`, `Playing`, `Paused`, `Skipping`,
`Completed` e `Faulted`. Desabilitar ou destruir o player interrompe a corrotina
ativa e retorna o atacante à posição inicial, evitando reprodução órfã.

## Reprodução, velocidade e skip

Os eventos `BattleStarted`, `UnitSelected`, `BasicAttackStarted`,
`AttackMissed`, `CriticalHit`, `DamageDealt`, `UnitDefeated`, `TurnEnded` e
`BattleEnded` são consumidos diretamente. Alvo, acerto, crítico, dano, morte e
vencedor nunca são recalculados na camada visual.

O tempo visual usa `Time.unscaledDeltaTime × velocidade`. Alterar velocidade
não usa `Time.timeScale` e não toca a simulação. Ao pular, a corrotina é
cancelada, todas as views voltam às posições iniciais, os snapshots finais do
resultado já existente são aplicados e o HUD mostra o vencedor. O simulador não
é chamado novamente.

`HealthBarView` trata máximo zero como 0%, limita o valor a 0..1 e exibe
`atual/máximo`. Ela não conhece Defesa, ataque ou fórmula de dano.

## Cena e placeholders

`Assets/_Game/Scenes/Battle.unity` contém:

- câmera principal e AudioListener;
- luz direcional e ambiente simples compatíveis com URP;
- arena, limites e materiais Lit;
- três slots `BattleUnitView` por lado;
- cápsulas/cubos azuis para heróis e vermelhos para inimigos debug;
- indicador de seleção, HUD world-space por unidade e HUD screen-space;
- `BattlePresentation` com provider, controller e event player;
- `EventSystem` com `InputSystemUIInputModule`.

Os modelos podem ser substituídos mantendo o objeto do slot e suas referências
de `BattleUnitView`. O corpo pode virar um prefab/Animator e os métodos de
seleção, dano e derrota podem encaminhar para animações definitivas, sem mudar o
contrato de eventos ou o domínio.

## Geração e validação

O menu **Tools > Idle Medieval Legends > Scenes > Create or Repair Battle
Scene** chama `BattleSceneTools`. A ferramenta:

- cria configuração e materiais ausentes;
- cria a cena canônica exclusivamente pelas APIs do Unity Editor;
- adiciona/configura `BootstrapBattleLoader` no objeto `App`;
- mantém Bootstrap e Battle habilitadas nas posições 0 e 1;
- preserva outras entradas de Build Settings;
- valida câmera, luz, Canvas, EventSystem, controller, provider, equipes,
  seis unidades, seis barras e transição;
- rejeita componentes essenciais desabilitados, hierarquias inativas e
  referências divergentes entre controller, event player, HUD, equipes,
  provider e assets canônicos.

Quando a cena já é válida, uma nova execução não a recria. Quando inválida, o
gerador recompõe a cena placeholder canônica; portanto, autoria definitiva deve
ser migrada para prefabs/geradores mais granulares antes de personalizações
manuais extensas nessa cena.

Automação:

```powershell
& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -executeMethod IdleMedievalLegends.Editor.Battle.BattleSceneTools.GenerateFromCommandLine

& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -executeMethod IdleMedievalLegends.Editor.Battle.BattleSceneTools.ValidateFromCommandLine
```

Nenhum YAML de cena foi editado manualmente. A cena, referências e `.meta`
foram produzidos pelo Unity.

## Testes e resultados automatizados

Os novos testes EditMode cobrem normalização/clamp de vida, ciclo 1x/2x/3x,
transições do presenter, skip sem troca do resultado, cenário determinístico
3v3, composição da cena e Build Settings. A validação de cena também cobre
componentes desabilitados, hierarquias inativas e referências compartilhadas
divergentes. A suíte completa foi executada no Unity e produziu
`136 total, 136 passed, 0 failed, 0 skipped`.

Os testes PlayMode cobrem:

- carregamento direto de Battle e início do cenário 3v3;
- transição Bootstrap → Battle após `Ready`;
- botões de velocidade até 3x;
- botão de pular;
- reprodução completa em 3x;
- HUD com resultado;
- vida/derrota de todas as views iguais aos snapshots finais.

A suíte PlayMode produziu `4 total, 4 passed, 0 failed, 0 skipped`, em
aproximadamente oito segundos. Exceções inesperadas falhariam os testes e não
foram registradas.

Uma repetição intermediária da suíte expôs uma `MissingReferenceException` na
ordem de destruição da cena: a view do atacante podia ser destruída antes do
cancelamento do player. O cancelamento passou a respeitar o null customizado de
`UnityEngine.Object`; os resultados 4/4 e 136/136 acima são das execuções finais
posteriores à correção.

A geração foi executada novamente após a criação. SHA-256 de Battle,
Bootstrap, `BattlePresentationConfig.asset` e `EditorBuildSettings.asset`
permaneceu idêntico, confirmando idempotência para a composição válida.

A validação visual foi executada no Unity Editor: Play iniciado pela cena
Bootstrap transitou para Battle; seis placeholders, nomes e barras foram
renderizados; dano, seleção, derrota e resultado ficaram visíveis; os controles
foram alternados de 1x para 2x e 3x; uma segunda execução foi pulada durante o
replay e mostrou imediatamente o estado final. O Console terminou com um log de
diagnóstico do Bootstrap, zero warnings e zero errors.

Logs e XMLs ficam em
`%TEMP%/IdleMedievalLegends-validation-task006`.

## Decisões

- usar o log completo como autoridade visual e nunca reconstruir combate;
- manter velocidade local ao player em vez de alterar o relógio global;
- construir cenário debug por referências explícitas de catálogo/tuning;
- usar uGUI e Input System já instalados, sem pacote de tween adicional;
- manter o gerador no Editor e a execução da cena sem busca global em loops;
- conservar `GameManager` como composição persistente, sem novo singleton.

## Limitações e riscos

- as animações são apenas deslocamento, flash e achatamento do placeholder;
- não há pooling, Animator definitivo, VFX, som ou câmera cinematográfica;
- a cena de reparo é canônica e pode substituir personalizações de uma cena
  inválida; prefabs separados devem preceder arte definitiva;
- o cenário é debug local e o cliente não é autoridade sobre combate/recompensa;
- a apresentação ainda materializa o log inteiro em memória;
- não foram executados builds Android/iOS ou smoke tests em dispositivo;
- acessibilidade, safe area, localização e múltiplas proporções de tela ainda
  precisam de uma task de UI dedicada.

## Não implementado

Habilidades, buffs, IA avançada, recompensas, campanha, inventário, progressão,
backend, rede, VFX/arte/UI finais, áudio, replay persistido e autoridade de
servidor permanecem fora do escopo.

## Próximos passos

A próxima task recomendada é extrair placeholders para prefabs e criar uma
camada de adaptadores de animação/efeitos orientada pelos mesmos eventos,
incluindo pooling e safe area, mantendo `BattleResult` como única entrada da
apresentação.
