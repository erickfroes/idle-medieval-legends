# TASK 002 — Bootstrap executável do jogo

Data: 2026-07-20  
Branch: `feat/bootstrap-game-lifecycle`  
Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo

Disponibilizar uma cena inicial mínima e executável que componha o ciclo de
vida do cliente, o cache JSON local e os assets de balanceamento. Este
bootstrap não implementa sistemas de produto e não transforma o cliente em
autoridade econômica.

## Composição da cena

`Assets/_Game/Scenes/Bootstrap.unity` possui um único objeto raiz `App` com:

- `LocalJsonPlayerStateRepository`, usando `player_cache.json` como cache
  descartável e `local-player` como identidade local de desenvolvimento;
- `GameManager`, referenciando o repositório e os dois assets de balanceamento;
- `BootstrapDiagnostics`, que observa a chegada a `Ready` e registra no Console
  o estado, `playerId`, revisão do inventário e revisão das profissões.

Os assets `CombatBalanceConfig.asset` e `CraftingBalanceConfig.asset` ficam em
`Assets/_Game/Data/Balance`. Seus valores iniciais vêm dos tunings versionados
já definidos pelo domínio e pelo GDD.

## Autoridade e persistência

O JSON local continua sendo apenas uma otimização de carregamento. Cache
ausente, vazio, malformado ou sem identidade válida produz um estado local
vazio; ele não concede saldos, itens, XP, crafting, pity ou qualquer resultado
econômico. Snapshots posteriores do backend devem substituir esses dados pelo
fluxo autoritativo já exposto no `GameManager`.

O identificador `local-player` é apenas diagnóstico para esta etapa sem login.
Ele não representa autenticação, conta de produção ou identidade de backend.

## Geração e validação pelo Editor

O arquivo `BootstrapSceneTools.cs` concentra a composição canônica. Os menus
disponíveis são:

- **Tools > Idle Medieval Legends > Bootstrap > Generate or Update Bootstrap**;
- **Tools > Idle Medieval Legends > Bootstrap > Validate Bootstrap**.

O gerador cria pastas/assets ausentes, abre ou cria a cena, reutiliza o objeto
`App` e componentes existentes, corrige referências e mantém `Bootstrap` como
a primeira cena habilitada nos Build Settings. As demais cenas são preservadas
e `Bootstrap` não é duplicada.

Para automação:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'

& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -executeMethod IdleMedievalLegends.Editor.Bootstrap.BootstrapSceneTools.GenerateFromCommandLine

& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -executeMethod IdleMedievalLegends.Editor.Bootstrap.BootstrapSceneTools.ValidateFromCommandLine
```

O validador rejeita cena ausente, múltiplos `App`, componentes ausentes ou
duplicados em qualquer ponto da cena, componentes fora do objeto raiz `App`,
`App` inativo, componentes desabilitados, referências inválidas, identidade
local vazia e configuração incorreta nos Build Settings. O gerador reativa
`App` e habilita seus três componentes canônicos; duplicatas externas são
rejeitadas sem apagar objetos potencialmente pertencentes ao usuário.

## Decisões

- As configurações são referências explícitas do `GameManager`; assim, uma
  cena incompleta falha cedo em vez de alcançar `Ready` silenciosamente.
- A inicialização de campos internos dos assets é defensiva para suportar
  criação e reimportação repetíveis pelo Editor.
- O diagnóstico é somente apresentação e não altera estado.
- A validação percorre toda a hierarquia, inclusive objetos inativos, para que
  outro singleton não possa invalidar silenciosamente o `GameManager` do `App`.
- A cena e os `.asset` foram gerados pela API do Unity Editor; o YAML não foi
  editado manualmente.
- `SampleScene` foi preservada nos Build Settings após `Bootstrap`.

## Arquivos alterados

- runtime: `GameManager.cs`, `GameBootstrapDependencies.cs`,
  `BootstrapDiagnostics.cs`, `LocalJsonPlayerStateRepository.cs` e os dois
  tipos de asset de configuração;
- Editor: novo assembly/pasta `Assets/_Game/Editor/Bootstrap` e atualização do
  `ProjectValidator.cs`;
- assets: `Assets/_Game/Scenes/Bootstrap.unity`, os dois `.asset` em
  `Assets/_Game/Data/Balance` e respectivos `.meta`/metas de pasta;
- testes: `BootstrapLifecycleTests.cs`, ajuste de
  `LocalJsonPlayerStateRepositoryTests.cs` e novo assembly/pasta PlayMode;
- configurações: `ProjectSettings/EditorBuildSettings.asset` e o
  `SceneTemplateSettings.json` padrão criado pelo Unity 6 ao operar cenas;
- documentação: `Docs/PROJECT_STRUCTURE.md` e este registro.

## Testes e critérios cobertos

Os testes EditMode adicionados cobrem validação de dependências, transição do
`GameManager` até `Ready`, identidade local, revisões vazias e conteúdo da
mensagem de diagnóstico. O teste existente de cache malformado agora confirma
também o fallback para `local-player`.

Foi adicionado também um smoke test PlayMode que carrega `Bootstrap` a partir dos
Build Settings e aguarda o estado `Ready`.

## Validação executada

Todos os comandos abaixo usaram Unity `6000.5.4f1` e gravaram logs em
`%TEMP%/IdleMedievalLegends-validation-task002`:

- importação/compilação: código de saída `0`, sem `error CS` ou `warning CS`;
- geração final: código de saída `0` e mensagem `Bootstrap válido`;
- segunda geração: código de saída `0`; hashes SHA-256 da cena, dos dois assets
  e de `EditorBuildSettings.asset` permaneceram idênticos;
- validação específica do bootstrap: código de saída `0`;
- validação estrutural do projeto: código de saída `0`, com o aviso já conhecido
  sobre `DefaultCompany`;
- EditMode: XML `Passed`, 33 executados, 33 aprovados, zero falhas, ignorados ou
  inconclusivos;
- PlayMode: XML `Passed`, um executado e aprovado; o Console registrou
  `state=Ready`, `playerId=local-player`, `inventoryRevision=0` e
  `professionRevision=0` sem exceções.

Uma tentativa inicial do PlayMode terminou antes do runner por erro de referência
do novo assembly e outra descobriu zero testes por filtro de plataforma. Ambas
foram corrigidas e não foram contabilizadas como sucesso; os resultados finais
acima vêm dos XMLs efetivamente gerados.

Após a revisão, dois testes EditMode adicionais passaram a cobrir um
`GameManager` fora de `App` e a combinação `App` inativo/manager desabilitado.
A suíte final possui 33 testes executados e aprovados, sem falhas, ignorados ou
inconclusivos.

## Fora do escopo e riscos

Não foram implementados batalha, UI final, mercado, gacha, backend, login,
anúncios ou compras. O bootstrap ainda não autentica nem busca snapshots do
servidor; `Ready` nesta etapa significa somente que cache e dependências locais
foram carregados com segurança.

Builds Android/iOS e smoke tests em dispositivo/simulador continuam pendentes.
