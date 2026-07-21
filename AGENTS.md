# Repository Guidelines

## Project Structure & Source of Truth

Idle Medieval Legends is a Unity 6 (`6000.5.4f1`) C#/URP Idle RPG targeting Android and iOS. Game code lives in `Assets/_Game/Scripts`: pure rules in `Domain`, orchestration in `Application`, balance assets in `Config`, and persistence/integrations in `Infrastructure`. EditMode tests live in `Assets/_Game/Tests/EditMode`; scenes and URP settings are under `Assets/Scenes` and `Assets/Settings`.

`Docs/Architecture_GDD.md` is authoritative for economy, rarities, attributes, crafting, professions, and market behavior. `Backend/` contains reference schemas and pseudocode, not production services. `Examples/` contains sample data and is never production authority.

## Architecture and Coding Rules

- Prefer pure, deterministic, testable C# domain code, separated from `MonoBehaviour`. MonoBehaviours are adapters, composition roots, or presentation components; they must not concentrate business rules.
- Prefer `IdleMedievalLegends.*` namespaces. Use four spaces, braces on new lines, `PascalCase` for types/methods/properties, and `camelCase` for locals, parameters, and existing serialized fields.
- Use nullable annotations and explicit validation where the Unity/C# version supports them. Do not hide failures with empty `try/catch`, ignored tests, or unresolved `TODO` comments.
- Keep balance values configurable (for example, ScriptableObjects or versioned tuning objects); do not scatter magic numbers through gameplay code.
- Do not add external packages without documenting why existing Unity or project capabilities are insufficient.
- Do not build a giant system in one task. Split work into independently testable increments with clear boundaries.

## Economy, Persistence, and Security

- The client is never authoritative for balances, inventory, gacha, crafting, pity, market state, or premium currency. Every future economic operation must be validated by the server.
- Do not implement real P2P markets, purchases, advertisements, or gems without an authoritative backend.
- Use `long` for integer currencies; never use `float` or `double` for monetary values. Represent rates and fees in basis points (1 bp = 0.01%).
- Instance IDs must be unique and immutable. Persisted enums must never be reordered or renumbered without an explicit, versioned data migration.
- Never commit secrets, tokens, API keys, signing material, or environment credentials. Keep backend operations idempotent and server-validated.

## Generated Files and Assets

Never edit `Library/`, `Temp/`, `Logs/`, `obj/`, or Unity-generated `.csproj`/`.sln`/`.slnx` files. Do not commit these or personal `UserSettings/`. Move Unity assets together with their `.meta` files. Make package and project-setting changes through Unity or their canonical manifests.

## Build, Test, and Validation

Use the exact editor version from `ProjectSettings/ProjectVersion.txt`. From PowerShell, adjust the path if Unity Hub is installed elsewhere:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe'
$validationDir = Join-Path ([IO.Path]::GetTempPath()) 'IdleMedievalLegends-validation'
New-Item -ItemType Directory -Force -Path $validationDir | Out-Null

# Import and compile the project headlessly.
& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -logFile (Join-Path $validationDir 'compile.log')

# Run all NUnit EditMode tests.
& $unityEditor -batchmode -nographics -quit -projectPath (Get-Location) `
  -runTests -testPlatform EditMode `
  -testResults (Join-Path $validationDir 'EditMode-results.xml') `
  -logFile (Join-Path $validationDir 'EditMode-tests.log')
```

Check exit codes, inspect both logs for compiler/test errors, and confirm the XML reports zero failures. Tests use Unity Test Framework/NUnit; name files `*Tests.cs` and methods `Subject_Scenario_ExpectedResult`. Cover boundaries, validation, rounding, currency arithmetic, enum serialization, and migration behavior. Every change must compile in Unity and preserve all existing tests.

There is no repository-owned command-line player build yet. Validate Android and iOS through **File > Build Profiles**, confirming scenes, target platform, URP/quality settings, and Player Settings; smoke-test the resulting build on a device or appropriate simulator. Do not claim a mobile build was validated if only EditMode tests ran.

## Branches, Commits, and Pull Requests

Each task requires its own branch, a narrow scope, tests, and explicit acceptance criteria. The repository has no commit history yet; use concise imperative commits such as `test(crafting): cover tier eligibility`. Pull requests must explain behavior and balance impact, link issues, list acceptance criteria and validation performed, include screenshots for UI/scene changes, and highlight schema, save-data, or serialized-enum migrations.

Every handoff must report: files changed, decisions made, tests executed (or why not), risks, and genuine pending work. Never describe unexecuted validation as successful.
