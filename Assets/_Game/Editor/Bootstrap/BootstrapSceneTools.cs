using System;
using System.Collections.Generic;
using System.IO;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Editor.ContentCatalog;
using IdleMedievalLegends.Infrastructure.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleMedievalLegends.Editor.Bootstrap
{
    public static class BootstrapSceneTools
    {
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";
        public const string CombatConfigPath =
            "Assets/_Game/Data/Balance/CombatBalanceConfig.asset";
        public const string CraftingConfigPath =
            "Assets/_Game/Data/Balance/CraftingBalanceConfig.asset";
        public const string ContentCatalogPath =
            ContentCatalogEditorTools.CatalogAssetPath;

        [MenuItem(
            "Tools/Idle Medieval Legends/Bootstrap/Generate or Update Bootstrap",
            priority = 110)]
        public static void GenerateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            GenerateOrUpdate();
        }

        [MenuItem(
            "Tools/Idle Medieval Legends/Bootstrap/Validate Bootstrap",
            priority = 111)]
        public static void ValidateFromMenu()
        {
            BootstrapValidationReport report = Validate();
            Log(report);
        }

        // Entry point para automação com -executeMethod.
        public static void GenerateFromCommandLine()
        {
            GenerateOrUpdate();
        }

        // Entry point para automação com -executeMethod.
        public static void ValidateFromCommandLine()
        {
            BootstrapValidationReport report = Validate();
            Log(report);

            if (report.Errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Bootstrap inválido: {report.Errors.Count} erro(s).");
            }
        }

        private static void GenerateOrUpdate()
        {
            EnsureFolder("Assets/_Game/Scenes");
            EnsureFolder("Assets/_Game/Data/Balance");

            CombatBalanceConfigAsset combatConfig =
                LoadOrCreateAsset<CombatBalanceConfigAsset>(CombatConfigPath);
            CraftingBalanceConfigAsset craftingConfig =
                LoadOrCreateAsset<CraftingBalanceConfigAsset>(CraftingConfigPath);
            ContentCatalogAsset contentCatalog =
                ContentCatalogEditorTools.LoadOrCreateDemoCatalog();

            // Assets recém-criados precisam ter GUID/import estáveis antes de
            // serem serializados como referências da cena no mesmo comando.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            combatConfig = AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(
                CombatConfigPath);
            craftingConfig = AssetDatabase.LoadAssetAtPath<CraftingBalanceConfigAsset>(
                CraftingConfigPath);
            contentCatalog = AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(
                ContentCatalogPath);

            if (combatConfig == null || craftingConfig == null || contentCatalog == null)
            {
                throw new InvalidOperationException(
                    "Os assets de balanceamento não puderam ser recarregados.");
            }

            combatConfig.EnsureInitialized();
            craftingConfig.EnsureInitialized();
            EditorUtility.SetDirty(combatConfig);
            EditorUtility.SetDirty(craftingConfig);
            AssetDatabase.SaveAssets();
            combatConfig = AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(
                CombatConfigPath);
            craftingConfig = AssetDatabase.LoadAssetAtPath<CraftingBalanceConfigAsset>(
                CraftingConfigPath);

            Scene scene = File.Exists(BootstrapScenePath)
                ? EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // OpenScene pode descarregar objetos que ainda não pertencem à cena.
            // Recarregar aqui evita serializar referências Unity já invalidadas.
            combatConfig = AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(
                CombatConfigPath);
            craftingConfig = AssetDatabase.LoadAssetAtPath<CraftingBalanceConfigAsset>(
                CraftingConfigPath);
            contentCatalog = AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(
                ContentCatalogPath);

            if (combatConfig == null || craftingConfig == null || contentCatalog == null)
            {
                throw new InvalidOperationException(
                    "Os assets de balanceamento ficaram indisponíveis ao abrir a cena.");
            }

            GameObject app = FindSingleRootAppForGeneration(scene);
            if (app == null)
                app = new GameObject("App");

            LocalJsonPlayerStateRepository repository =
                GetOrAddComponent<LocalJsonPlayerStateRepository>(app);
            GameManager gameManager = GetOrAddComponent<GameManager>(app);
            BootstrapDiagnostics diagnostics = GetOrAddComponent<BootstrapDiagnostics>(app);

            repository.enabled = true;
            gameManager.enabled = true;
            diagnostics.enabled = true;
            gameManager.ConfigureBootstrapDependencies(
                repository,
                combatConfig,
                craftingConfig,
                contentCatalog);
            diagnostics.Configure(gameManager);
            app.SetActive(true);
            EditorUtility.SetDirty(app);
            EditorUtility.SetDirty(repository);
            EditorUtility.SetDirty(gameManager);
            EditorUtility.SetDirty(diagnostics);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, BootstrapScenePath))
                throw new InvalidOperationException("Não foi possível salvar a cena Bootstrap.");

            PutBootstrapFirstInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BootstrapValidationReport report = Validate();
            Log(report);
            if (report.Errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Bootstrap gerado com {report.Errors.Count} erro(s) de validação.");
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
        }

        private static BootstrapValidationReport Validate()
        {
            var report = new BootstrapValidationReport();

            CombatBalanceConfigAsset combatConfig =
                AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(CombatConfigPath);
            CraftingBalanceConfigAsset craftingConfig =
                AssetDatabase.LoadAssetAtPath<CraftingBalanceConfigAsset>(CraftingConfigPath);
            ContentCatalogAsset contentCatalog =
                AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(ContentCatalogPath);

            if (combatConfig == null)
                report.Errors.Add($"Asset ausente ou inválido: {CombatConfigPath}");
            if (craftingConfig == null)
                report.Errors.Add($"Asset ausente ou inválido: {CraftingConfigPath}");
            if (contentCatalog == null)
                report.Errors.Add($"Asset ausente ou inválido: {ContentCatalogPath}");
            else
            {
                ContentCatalogValidationReport catalogReport =
                    contentCatalog.ValidateCatalog();
                if (!catalogReport.IsValid)
                {
                    report.Errors.Add(
                        $"ContentCatalog.asset inválido: {catalogReport.ErrorCount} erro(s).");
                }
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (sceneAsset == null)
            {
                report.Errors.Add($"Cena ausente: {BootstrapScenePath}");
            }
            else
            {
                ValidateSceneContents(combatConfig, craftingConfig, contentCatalog, report);
            }

            ValidateBuildSettings(report);
            return report;
        }

        private static void ValidateSceneContents(
            CombatBalanceConfigAsset expectedCombatConfig,
            CraftingBalanceConfigAsset expectedCraftingConfig,
            ContentCatalogAsset expectedContentCatalog,
            BootstrapValidationReport report)
        {
            Scene loadedScene = SceneManager.GetSceneByPath(BootstrapScenePath);
            bool closeAfterValidation = !loadedScene.IsValid() || !loadedScene.isLoaded;
            Scene scene = closeAfterValidation
                ? EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive)
                : loadedScene;

            try
            {
                ValidateLoadedSceneContents(
                    scene,
                    expectedCombatConfig,
                    expectedCraftingConfig,
                    expectedContentCatalog,
                    report);
            }
            finally
            {
                if (closeAfterValidation)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static IReadOnlyList<string> GetSceneValidationErrors(
            Scene scene,
            CombatBalanceConfigAsset expectedCombatConfig,
            CraftingBalanceConfigAsset expectedCraftingConfig,
            ContentCatalogAsset expectedContentCatalog)
        {
            var report = new BootstrapValidationReport();
            ValidateLoadedSceneContents(
                scene,
                expectedCombatConfig,
                expectedCraftingConfig,
                expectedContentCatalog,
                report);
            return new List<string>(report.Errors);
        }

        private static void ValidateLoadedSceneContents(
            Scene scene,
            CombatBalanceConfigAsset expectedCombatConfig,
            CraftingBalanceConfigAsset expectedCraftingConfig,
            ContentCatalogAsset expectedContentCatalog,
            BootstrapValidationReport report)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                report.Errors.Add("A cena Bootstrap precisa estar válida e carregada.");
                return;
            }

            GameObject app = FindSingleRootApp(scene, report);
            if (app == null)
                return;

            List<LocalJsonPlayerStateRepository> repositories =
                GetSceneComponents<LocalJsonPlayerStateRepository>(scene);
            List<GameManager> managers = GetSceneComponents<GameManager>(scene);
            List<BootstrapDiagnostics> diagnostics =
                GetSceneComponents<BootstrapDiagnostics>(scene);

            RequireSingleSceneComponent(
                repositories.Count,
                nameof(LocalJsonPlayerStateRepository),
                report);
            RequireSingleSceneComponent(managers.Count, nameof(GameManager), report);
            RequireSingleSceneComponent(
                diagnostics.Count,
                nameof(BootstrapDiagnostics),
                report);

            ValidateComponentPlacement(repositories, app, report);
            ValidateComponentPlacement(managers, app, report);
            ValidateComponentPlacement(diagnostics, app, report);

            if (!app.activeSelf)
                report.Errors.Add("O objeto raiz App deve estar ativo.");

            if (repositories.Count == 1 && !repositories[0].enabled)
                report.Errors.Add("LocalJsonPlayerStateRepository deve estar habilitado.");
            if (managers.Count == 1 && !managers[0].enabled)
                report.Errors.Add("GameManager deve estar habilitado.");
            if (diagnostics.Count == 1 && !diagnostics[0].enabled)
                report.Errors.Add("BootstrapDiagnostics deve estar habilitado.");

            if (repositories.Count != 1 || managers.Count != 1)
                return;

            GameManager manager = managers[0];
            if (manager.CachedStateRepository != repositories[0])
            {
                report.Errors.Add(
                    "GameManager não referencia o LocalJsonPlayerStateRepository do App.");
            }
            if (manager.CombatBalanceConfig != expectedCombatConfig)
                report.Errors.Add("GameManager não referencia CombatBalanceConfig.asset.");
            if (manager.CraftingBalanceConfig != expectedCraftingConfig)
                report.Errors.Add("GameManager não referencia CraftingBalanceConfig.asset.");
            if (manager.ContentCatalogAsset != expectedContentCatalog)
                report.Errors.Add("GameManager não referencia ContentCatalog.asset.");
            if (string.IsNullOrWhiteSpace(repositories[0].LocalPlayerId))
                report.Errors.Add("LocalJsonPlayerStateRepository não possui playerId local.");

            try
            {
                GameBootstrapDependencies.Validate(
                    manager.CachedStateRepository,
                    manager.CombatBalanceConfig,
                    manager.CraftingBalanceConfig,
                    manager.ContentCatalogAsset);
            }
            catch (Exception exception)
            {
                report.Errors.Add($"Dependências do GameManager inválidas: {exception.Message}");
            }
        }

        private static void ValidateBuildSettings(BootstrapValidationReport report)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0 ||
                !scenes[0].enabled ||
                !string.Equals(
                    scenes[0].path,
                    BootstrapScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                report.Errors.Add(
                    "Bootstrap deve ser a primeira cena habilitada nos Build Settings.");
            }

            int occurrences = 0;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (string.Equals(
                    scenes[i].path,
                    BootstrapScenePath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    occurrences++;
                }
            }

            if (occurrences != 1)
                report.Errors.Add("Bootstrap deve aparecer exatamente uma vez nos Build Settings.");
        }

        private static GameObject FindSingleRootAppForGeneration(Scene scene)
        {
            GameObject result = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (!string.Equals(roots[i].name, "App", StringComparison.Ordinal))
                    continue;

                if (result != null)
                {
                    throw new InvalidOperationException(
                        "A cena contém mais de um objeto raiz chamado App.");
                }

                result = roots[i];
            }

            return result;
        }

        private static GameObject FindSingleRootApp(
            Scene scene,
            BootstrapValidationReport report)
        {
            GameObject result = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (!string.Equals(roots[i].name, "App", StringComparison.Ordinal))
                    continue;

                if (result != null)
                {
                    report.Errors.Add("A cena contém mais de um objeto raiz chamado App.");
                    return null;
                }

                result = roots[i];
            }

            if (result == null && File.Exists(BootstrapScenePath))
                report.Errors.Add("A cena não contém um objeto raiz chamado App.");

            return result;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"Já existe um asset de tipo incompatível em {path}.");
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void PutBootstrapFirstInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true)
            };

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (!string.Equals(
                    existing[i].path,
                    BootstrapScenePath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    scenes.Add(existing[i]);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static List<T> GetSceneComponents<T>(Scene scene) where T : Component
        {
            var components = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                components.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return components;
        }

        private static void ValidateComponentPlacement<T>(
            IReadOnlyList<T> components,
            GameObject app,
            BootstrapValidationReport report) where T : Component
        {
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].gameObject != app)
                {
                    report.Errors.Add(
                        $"{typeof(T).Name} deve existir somente no objeto raiz App.");
                    return;
                }
            }
        }

        private static void RequireSingleSceneComponent(
            int count,
            string componentName,
            BootstrapValidationReport report)
        {
            if (count != 1)
            {
                report.Errors.Add(
                    $"A cena Bootstrap deve possuir exatamente um {componentName}; " +
                    $"encontrados: {count}.");
            }
        }

        private static void Log(BootstrapValidationReport report)
        {
            for (int i = 0; i < report.Errors.Count; i++)
                Debug.LogError($"[BootstrapValidation] {report.Errors[i]}");

            if (report.Errors.Count == 0)
            {
                Debug.Log(
                    "[BootstrapValidation] Bootstrap válido: cena, dependências, " +
                    "balanceamento e Build Settings conferidos.");
            }
        }

        private sealed class BootstrapValidationReport
        {
            public List<string> Errors { get; } = new List<string>();
        }
    }
}
