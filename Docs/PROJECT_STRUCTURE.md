# Estrutura do projeto

Idle Medieval Legends usa Unity 6 (`6000.5.4f1`), URP e assemblies explícitos para impedir que responsabilidades diferentes voltem a se misturar no assembly padrão. As regras econômicas e de progressão continuam definidas em `Docs/Architecture_GDD.md`.

## Diretórios principais

```text
Assets/
├── _Game/
│   ├── Editor/ProjectValidation/   # validações estruturais exclusivas do Editor
│   ├── Scripts/
│   │   ├── Domain/                 # regras determinísticas e modelos do domínio
│   │   ├── Application/            # ciclo de vida e orquestração do cliente
│   │   ├── Infrastructure/         # cache local e DTOs de integrações futuras
│   │   └── Config/                 # ScriptableObjects e tuning versionado
│   └── Tests/EditMode/             # testes NUnit executados somente no Editor
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

EditMode Tests ──> Domain + Config + Infrastructure + Application
ProjectValidation (Editor only, independente dos assemblies de runtime)
```

| Assembly | Plataforma | Responsabilidade | Referências do projeto |
|---|---|---|---|
| `IdleMedievalLegends.Domain` | Runtime | Regras, tipos persistidos, cálculos e snapshots | nenhuma |
| `IdleMedievalLegends.Config` | Runtime | Assets de balanceamento configuráveis | Domain |
| `IdleMedievalLegends.Infrastructure` | Runtime | Persistência local e contratos de integração | Domain |
| `IdleMedievalLegends.Application` | Runtime | Orquestração e composição do cliente | Domain, Config, Infrastructure |
| `IdleMedievalLegends.Tests.EditMode` | Editor | Testes NUnit/EditMode | todos os assemblies de runtime e `TestAssemblies` |
| `IdleMedievalLegends.Editor.ProjectValidation` | Editor | Auditoria da fundação do projeto | nenhuma do runtime |

O fluxo de dependências é unidirecional. `Domain` não referencia Application, Infrastructure ou Config. MonoBehaviours permanecem nas bordas do sistema; regras de negócio devem continuar em classes determinísticas do domínio.

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
