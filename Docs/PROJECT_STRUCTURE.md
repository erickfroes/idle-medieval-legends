# Estrutura do projeto

Idle Medieval Legends usa Unity 6 (`6000.5.4f1`), URP e assemblies explícitos para impedir que responsabilidades diferentes voltem a se misturar no assembly padrão. As regras econômicas e de progressão continuam definidas em `Docs/Architecture_GDD.md`.

## Diretórios principais

```text
Assets/
├── _Game/
│   ├── Editor/ProjectValidation/   # validações estruturais exclusivas do Editor
│   ├── Editor/Bootstrap/           # geração e validação repetível da cena inicial
│   ├── Editor/ContentCatalog/      # catálogo, tuning e visualização de Poder no Editor
│   ├── Editor/Combat/              # execução e diagnóstico de batalhas sem cena
│   ├── Editor/Battle/              # geração e validação do vertical slice visual
│   ├── Data/Balance/               # assets versionados de tuning do cliente
│   ├── Data/Content/               # definições locais, sem dados de jogador
│   ├── Data/Presentation/          # timing e materiais placeholder do cliente
│   ├── Scenes/                     # cenas Bootstrap e Battle mantidas por ferramentas
│   ├── Scripts/
│   │   ├── Domain/                 # regras determinísticas e modelos do domínio
│   │   ├── Application/            # ciclo de vida e orquestração do cliente
│   │   ├── Infrastructure/         # cache local e DTOs de integrações futuras
│   │   ├── Config/                 # ScriptableObjects e tuning versionado
│   │   └── Presentation/Battle/    # replay visual, views e HUD substituível
│   ├── Tests/EditMode/             # testes NUnit executados somente no Editor
│   └── Tests/PlayMode/             # smoke tests que carregam cenas no Editor
├── Scenes/                         # cenas mantidas pelo Unity Editor
└── Settings/                       # assets URP e perfis de renderização
Packages/                           # manifesto e lock de pacotes Unity
ProjectSettings/                    # configurações canônicas do projeto Unity
Docs/                               # arquitetura, decisões e registros de tarefas
Backend/                            # schemas/pseudocódigo de referência, não produção
Examples/                           # dados de exemplo, sem autoridade de produção
```

`Library/`, `Temp/`, `Logs/`, `obj/`, builds locais, arquivos de IDE e `UserSettings/` são gerados localmente e ficam fora do Git.

## Assemblies e dependências

```text
Domain
├── Config
├── Infrastructure
└── Application ──> Config + Infrastructure

Presentation Battle ──> Domain + Application + Config

ContentCatalog Editor ──> Domain + Config
Combat Editor ──> Domain + Config
Battle Editor ──> Presentation Battle + Domain + Application + Config
Bootstrap Editor ──> Domain + Application + Config + Infrastructure + ContentCatalog Editor
EditMode Tests ──> runtime + Bootstrap/Battle/ContentCatalog Editor
PlayMode Tests ──> Presentation Battle + Application + Domain
ProjectValidation (Editor only, independente dos assemblies de runtime)
```

| Assembly | Plataforma | Responsabilidade | Referências do projeto |
|---|---|---|---|
| `IdleMedievalLegends.Domain` | Runtime | Regras, tipos persistidos, cálculos e snapshots | nenhuma |
| `IdleMedievalLegends.Config` | Runtime | Assets de balanceamento configuráveis | Domain |
| `IdleMedievalLegends.Infrastructure` | Runtime | Persistência local e contratos de integração | Domain |
| `IdleMedievalLegends.Application` | Runtime | Orquestração e composição do cliente | Domain, Config, Infrastructure |
| `IdleMedievalLegends.Presentation.Battle` | Runtime | Replay visual do log, placeholders, HUD e transição de cena | Domain, Application, Config |
| `IdleMedievalLegends.Tests.EditMode` | Editor | Testes NUnit/EditMode | assemblies de runtime, Bootstrap/ContentCatalog Editor e `TestAssemblies` |
| `IdleMedievalLegends.Tests.PlayMode` | Editor/Play Mode | Smoke test da cena Bootstrap | Application, Domain e `TestAssemblies` |
| `IdleMedievalLegends.Editor.ProjectValidation` | Editor | Auditoria da fundação do projeto | nenhuma do runtime |
| `IdleMedievalLegends.Editor.ContentCatalog` | Editor | Geração do exemplo e validação contextual dos assets de catálogo | Domain, Config |
| `IdleMedievalLegends.Editor.Combat` | Editor | Runner resumido do simulador determinístico, sem cena | Domain, Config |
| `IdleMedievalLegends.Editor.Battle` | Editor | Geração/validação da cena Battle e de sua composição | Presentation Battle, Domain, Application, Config |
| `IdleMedievalLegends.Editor.Bootstrap` | Editor | Geração/validação da cena inicial, assets e Build Settings | Domain, Application, Config, Infrastructure, ContentCatalog Editor |

O fluxo de dependências é unidirecional. `Domain` não referencia Application, Infrastructure ou Config. MonoBehaviours permanecem nas bordas do sistema; regras de negócio devem continuar em classes determinísticas do domínio.

## Progressão e Poder dos heróis

`Domain/Heroes` contém o snapshot serializável `HeroInstance` e as transições
validadas de progressão. `Domain/Combat` contém o tuning puro, modificadores,
fórmulas de atributos/Poder e as métricas `HeroPower`, `TeamPower`,
`AccountPower`, `CompetitivePower` e `SeasonPeakPower`. O domínio não consulta
cenas, relógio, UI ou inventário; bônus de equipamentos chegam pela interface
`IHeroEquipmentModifierProvider`.

O asset em `Data/Balance` é migrado explicitamente para a versão corrente ao
ser carregado. No Editor, **Tools > Idle Medieval Legends > Balance > Upgrade
Combat Balance Assets** valida e persiste a migração. O componente opcional
`HeroPowerDebugComponent`, compilado apenas no assembly de Editor, apresenta o
breakdown completo sem participar do cálculo.

## Simulação determinística de batalha

O módulo puro em `Domain/Combat` recebe duas equipes de `BattleUnit`, cria
estados internos descartáveis e produz `BattleResult` com eventos e snapshots
finais. Ordem, alvo, acerto, crítico e variação são resolvidos sem relógio,
frame, cena, estado global ou RNG do Unity. O `HeroInstance` é consultado apenas
antes da batalha por `BattleUnitFactory`; a simulação usa exclusivamente o
snapshot calculado.

Use **Tools > Idle Medieval Legends > Combat > Run Deterministic Demo Battle**
para executar Paladino/Arqueira contra Mago e imprimir somente seed, vencedor,
turnos, ações, dano, derrotados e hash.

## Vertical slice visual de batalha

`Scripts/Presentation/Battle` consome o `BattleResult` pronto e reproduz seus
eventos com tempo visual independente. `BattleSceneController` compõe o cenário,
`BattleEventPlayer` controla deslocamento/impacto/morte e as views aplicam apenas
estado de apresentação. Alterar 1x/2x/3x ou pular nunca chama o simulador outra
vez nem modifica seed, dano, turnos, hash ou snapshots finais.

A cena `Assets/_Game/Scenes/Battle.unity` usa primitivas, barras de vida uGUI,
uma câmera e iluminação URP simples. Use **Tools > Idle Medieval Legends >
Scenes > Create or Repair Battle Scene** para gerar/validar a composição e
manter Bootstrap/Battle nas duas primeiras posições dos Build Settings.

## Bootstrap executável

A cena inicial fica em `Assets/_Game/Scenes/Bootstrap.unity` e contém um único
objeto raiz `App`, com `GameManager`, `LocalJsonPlayerStateRepository` e o
diagnóstico temporário do ciclo de vida. `GameManager` referencia os assets em
`Assets/_Game/Data/Balance`, o catálogo em `Assets/_Game/Data/Content` e trata o
JSON local somente como cache descartável. O catálogo é convertido em um
snapshot/lookup somente leitura durante a inicialização e não armazena estado de jogador.

Use **Tools > Idle Medieval Legends > Bootstrap > Generate or Update Bootstrap**
para criar ou atualizar a composição sem editar YAML, e **Validate Bootstrap**
para conferir componentes, referências, assets e Build Settings. O gerador é
idempotente, preserva outras cenas de build e mantém `Bootstrap` habilitada na
primeira posição.

## Catálogo de conteúdo

As definições imutáveis e o validador ficam em `Domain/Content`. O
`ContentCatalogAsset` em Config contém somente DTOs privados de autoria e produz
um `ContentCatalog` separado no runtime. IDs textuais, referências cruzadas,
Tiers, raridades, stacks, receitas, slots e thresholds profissionais são
validados antes da construção do `ContentCatalogLookup`.

Use **Tools > Idle Medieval Legends > Validate Content Catalog** para validar
todos os assets e imprimir mensagens contextuais e o resumo por Tier, raridade
e profissão. **Content > Generate or Reset Demo Catalog** recria deliberadamente
o exemplo mínimo; o gerador do Bootstrap apenas o cria quando estiver ausente e
não sobrescreve autoria existente.

## Validação estrutural

No Editor, execute **Tools > Idle Medieval Legends > Validate Project**. A validação informa no Console:

- documentos obrigatórios ausentes;
- arquivos `.asmdef` ausentes ou com nome divergente;
- versão incorreta do Unity;
- ausência do URP ou Unity Test Framework no manifesto;
- arquivos essenciais de `ProjectSettings`/URP ausentes;
- Render Pipeline Asset padrão não configurado;
- ausência de cena habilitada nos Build Settings.

`DefaultCompany` gera aviso, pois a identidade definitiva é necessária antes de distribuir builds móveis, mas não bloqueia esta fundação de código.

Para automação em batch mode:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
$validationDir = Join-Path ([IO.Path]::GetTempPath()) 'IdleMedievalLegends-validation'
New-Item -ItemType Directory -Force -Path $validationDir | Out-Null

& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -executeMethod IdleMedievalLegends.Editor.ProjectValidation.ProjectValidator.ValidateFromCommandLine `
  -logFile (Join-Path $validationDir 'project-validation.log')

& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -logFile (Join-Path $validationDir 'compile.log')

& $unityEditor -batchmode -nographics -projectPath (Get-Location) `
  -runTests -testPlatform EditMode `
  -testResults (Join-Path $validationDir 'EditMode-results.xml') `
  -logFile (Join-Path $validationDir 'EditMode-tests.log')
```

O Test Framework encerra o processo ao concluir a execução. Nesta versão, passar `-quit` junto de `-runTests` encerrou o Editor antes do runner e não produziu XML. Os códigos de saída, os logs e o XML devem ser inspecionados. Um teste só pode ser declarado aprovado quando o XML existir e reportar zero falhas. Android e iOS ainda exigem validação pelos Build Profiles e smoke test em dispositivo ou simulador apropriado.

## Git e assets

Arquivos textuais mantidos pelo projeto usam LF. As regras Git LFS existentes para fontes de arte 3D/2D, áudio e vídeo foram preservadas em `.gitattributes`. Assets Unity devem ser movidos junto com seus arquivos `.meta`; GUIDs de enums persistidos, assets ou referências não devem ser recriados sem necessidade.
