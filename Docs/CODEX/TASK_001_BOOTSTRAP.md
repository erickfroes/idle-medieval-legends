# TASK 001 — Fundação Unity

Data: 2026-07-20  
Branch observada: `chore/bootstrap-unity-foundation`  
Unity: `6000.5.4f1 (d550df8bd089)`

## Objetivo

Transformar o pacote de arquitetura importado em uma fundação Unity compilável, com limites explícitos entre Domain, Application, Infrastructure, Config, código de Editor e testes EditMode. Esta tarefa não cria conteúdo nem sistemas de produto.

## Escopo executado

- criado `.gitignore` para artefatos gerados pelo Unity, IDEs e builds locais;
- revisado `.gitattributes`, com normalização dos arquivos textuais e preservação integral das regras Git LFS existentes;
- criados assemblies separados para Domain, Config, Infrastructure, Application e EditMode Tests;
- criado assembly exclusivo de Editor para validação do projeto;
- configurado o assembly EditMode como `TestAssemblies`, Editor-only e com referências aos assemblies de runtime;
- criada validação estrutural acessível pelo menu do Unity e por `-executeMethod`;
- documentada a estrutura e o fluxo de validação;
- corrigida uma única quebra de acesso causada pela nova fronteira de assembly, sem alterar lógica de domínio.

Nenhuma fórmula de balanceamento, ID numérico de enum, pacote, asset de configuração ou teste existente foi modificado.

## Decisões

O grafo de dependências ficou unidirecional: Config e Infrastructure dependem de Domain; Application depende de Domain, Config e Infrastructure. Domain não depende das demais camadas.

`InventorySnapshotData.NormalizeAfterLoad` e `ProfessionSnapshotData.NormalizeAfterLoad` já eram `internal`, mas eram chamados pelo migrador em Infrastructure. A separação expôs a quebra de acesso `CS1061`. Em vez de tornar operações de migração públicas, `AssemblyInfo.cs` declara apenas `IdleMedievalLegends.Infrastructure` como assembly amigo. A alteração preserva a API e o comportamento existentes.

O validador trata `DefaultCompany` como aviso, não erro estrutural. Identidade de publicação deve ser definida junto das decisões de distribuição; não pertence a esta tarefa de compilação.

## Arquivos alterados

- `.gitattributes`
- `.gitignore`
- `Assets/_Game/Scripts/Domain/AssemblyInfo.cs`
- `Assets/_Game/Scripts/Domain/AssemblyInfo.cs.meta`
- `Assets/_Game/Scripts/Domain/IdleMedievalLegends.Domain.asmdef`
- `Assets/_Game/Scripts/Domain/IdleMedievalLegends.Domain.asmdef.meta`
- `Assets/_Game/Scripts/Config/IdleMedievalLegends.Config.asmdef`
- `Assets/_Game/Scripts/Config/IdleMedievalLegends.Config.asmdef.meta`
- `Assets/_Game/Scripts/Infrastructure/IdleMedievalLegends.Infrastructure.asmdef`
- `Assets/_Game/Scripts/Infrastructure/IdleMedievalLegends.Infrastructure.asmdef.meta`
- `Assets/_Game/Scripts/Application/IdleMedievalLegends.Application.asmdef`
- `Assets/_Game/Scripts/Application/IdleMedievalLegends.Application.asmdef.meta`
- `Assets/_Game/Tests/EditMode/IdleMedievalLegends.Tests.EditMode.asmdef`
- `Assets/_Game/Tests/EditMode/IdleMedievalLegends.Tests.EditMode.asmdef.meta`
- `Assets/_Game/Editor.meta`
- `Assets/_Game/Editor/ProjectValidation.meta`
- `Assets/_Game/Editor/ProjectValidation/IdleMedievalLegends.Editor.ProjectValidation.asmdef`
- `Assets/_Game/Editor/ProjectValidation/IdleMedievalLegends.Editor.ProjectValidation.asmdef.meta`
- `Assets/_Game/Editor/ProjectValidation/ProjectValidator.cs`
- `Assets/_Game/Editor/ProjectValidation/ProjectValidator.cs.meta`
- `Docs/PROJECT_STRUCTURE.md`
- `Docs/CODEX/TASK_001_BOOTSTRAP.md`

## Comandos e validações executados

Arquivos e dependências foram auditados com `rg --files`, `Get-Content`, `git status`, `git lfs version`, `git lfs track`, `git check-ignore` e `git check-attr`. Todos os `.asmdef` também foram carregados com `ConvertFrom-Json`; as referências `GUID:` foram resolvidas contra os `.meta`, e todos os GUIDs em `Assets/` foram verificados quanto a duplicidade.

Os seguintes argumentos foram executados no Unity encontrado em `C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe`:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
$validationDir = Join-Path ([IO.Path]::GetTempPath()) 'IdleMedievalLegends-validation'

# Validação estrutural
& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -executeMethod IdleMedievalLegends.Editor.ProjectValidation.ProjectValidator.ValidateFromCommandLine `
  -logFile (Join-Path $validationDir 'project-validation.log')

# Import/compilação
& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -logFile (Join-Path $validationDir 'compile.log')

# Execução que efetivamente produziu o resultado de testes nesta versão
& $unityEditor -batchmode -nographics -projectPath (Get-Location) `
  -runTests -testPlatform editmode `
  -testResults (Join-Path $validationDir 'EditMode-results-task001-run2.xml') `
  -logFile (Join-Path $validationDir 'EditMode-tests-task001-run2.log')
```

### Resultados reais

- validação estática de `.asmdef`/GUID: 6 assemblies válidos, todas as referências resolvidas, zero GUIDs duplicados;
- validação estrutural Unity: código de saída 0, zero erros e um aviso sobre `DefaultCompany`;
- compilação Unity: código de saída 0 e nenhum `error CS`/`warning CS` no log final;
- EditMode: código de saída 0; XML `Passed`, 21 testes executados, 21 aprovados, 0 falhas, 0 ignorados e 0 inconclusivos;
- Git LFS: `filter`, `diff` e `merge` continuam `lfs`, com `text` desabilitado, para todos os padrões originalmente presentes;
- `.gitignore`: Library, Temp, Logs, UserSettings e arquivos Unity/IDE gerados foram confirmados como ignorados.

Uma primeira tentativa de EditMode incluiu `-quit`, encerrou com código 0, mas não iniciou o runner nem gerou XML. Esse resultado não foi considerado aprovação. A repetição sem `-quit` gerou o XML descrito acima. Durante o primeiro import após os `.asmdef`, o Unity também detectou a quebra `internal` mencionada nas decisões; o erro foi corrigido e todas as validações finais foram repetidas.

## Riscos e pendências genuínas

- `PlayerSettings.companyName` ainda é `DefaultCompany`, e os application identifiers permanecem os valores do template; precisam ser definidos antes de publicar builds.
- Não foi executado player build Android ou iOS, nem smoke test em dispositivo/simulador. Os testes realizados foram somente import/compilação e EditMode no Editor Windows.
- A validação estrutural verifica a fundação e referências essenciais; ela não substitui build móvel, análise de conteúdo, validação de backend ou telemetria.
- A amizade de assembly é intencional e limitada a Infrastructure. Novos acessos internos entre camadas devem ser evitados ou justificados explicitamente.

## Não implementado

- backend ou serviço autoritativo;
- Firebase, PlayFab ou Unity Services;
- gacha, mercado real, compras, anúncios ou gemas;
- combate visual ou sistema de batalha;
- UI;
- cenas ou conteúdo;
- builds Android/iOS;
- alterações de balanceamento, raridades, profissões, tiers ou estados persistidos.
