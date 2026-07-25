using System;
using System.Collections.Generic;
using System.IO;
using IdleMedievalLegends.Presentation.MobileUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleMedievalLegends.Editor.MobileUI
{
    public static class MobileUiShellTools
    {
        private const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";
        private const string ThemeDirectory = "Assets/_Game/Data/Presentation";
        private const string ThemePath = ThemeDirectory + "/UiThemeConfig.asset";

        private static readonly HashSet<NavigationRoute> ShellRoutes =
            new HashSet<NavigationRoute>
            {
                NavigationRoute.Home,
                NavigationRoute.Heroes,
                NavigationRoute.Gacha,
                NavigationRoute.Market,
                NavigationRoute.Profile,
                NavigationRoute.Settings,
                NavigationRoute.More
            };

        [MenuItem("Tools/Idle Medieval Legends/UI/Generate or Update Mobile Shell")]
        public static void Generate()
        {
            if (!UnityEngine.Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                GenerateAndValidate();
            }
            finally
            {
                RestoreSceneSetupIfValid(previousSetup);
            }
        }

        private static void GenerateAndValidate()
        {
            EnsureFolder(ThemeDirectory);
            UiThemeConfig theme = AssetDatabase.LoadAssetAtPath<UiThemeConfig>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiThemeConfig>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }
            theme.Validate();
            EditorUtility.SetDirty(theme);

            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            GameObject app = FindRoot(scene, "App");
            if (app == null)
                throw new InvalidOperationException("A cena Bootstrap não possui o objeto raiz App.");

            MobileUiBootstrap[] bootstraps =
                app.GetComponentsInChildren<MobileUiBootstrap>(true);
            MobileUiBootstrap bootstrap;
            if (bootstraps.Length == 0)
            {
                bootstrap = Undo.AddComponent<MobileUiBootstrap>(app);
            }
            else
            {
                bootstrap = bootstraps[0];
                for (int i = 1; i < bootstraps.Length; i++)
                    Undo.DestroyObjectImmediate(bootstraps[i]);
            }
            bootstrap.Configure(theme);
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            IReadOnlyList<string> errors = ValidateProject(out IReadOnlyList<string> warnings);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

            Debug.Log(
                $"Mobile UI shell gerada e validada. Avisos: {warnings.Count}.",
                bootstrap);
        }

        [MenuItem("Tools/Idle Medieval Legends/UI/Validate Mobile Shell")]
        public static void ValidateMenu()
        {
            if (!UnityEngine.Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            IReadOnlyList<string> errors = ValidateProject(out IReadOnlyList<string> warnings);
            for (int i = 0; i < warnings.Count; i++)
                Debug.LogWarning(warnings[i]);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            Debug.Log($"Mobile UI shell válida. Avisos: {warnings.Count}.");
        }

        public static void GenerateFromCommandLine()
        {
            Generate();
        }

        public static void ValidateFromCommandLine()
        {
            ValidateMenu();
        }

        public static IReadOnlyList<string> ValidateProject(
            out IReadOnlyList<string> warnings)
        {
            var errors = new List<string>();
            var warningList = new List<string>();
            if (HasDirtyOpenScene())
            {
                errors.Add(
                    "Há cenas abertas com alterações não salvas. Salve ou descarte as " +
                    "alterações antes de validar a shell.");
                warnings = warningList;
                return errors;
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                ValidateRoutes(errors);
                ValidateBootstrap(errors);
                ValidateScenes(errors, warningList);
            }
            finally
            {
                RestoreSceneSetupIfValid(previousSetup);
            }

            warnings = warningList;
            return errors;
        }

        private static bool HasDirtyOpenScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                    return true;
            }
            return false;
        }

        private static void RestoreSceneSetupIfValid(SceneSetup[] setup)
        {
            if (setup == null)
                return;

            for (int i = 0; i < setup.Length; i++)
            {
                if (setup[i].isLoaded && setup[i].isActive)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                    return;
                }
            }
        }

        private static void ValidateRoutes(List<string> errors)
        {
            var routes = new HashSet<NavigationRoute>();
            IReadOnlyList<NavigationDestination> destinations =
                NavigationRouteRegistry.Destinations;
            foreach (NavigationDestination destination in destinations)
            {
                if (!routes.Add(destination.Route))
                    errors.Add($"Rota duplicada: {destination.Route}.");
                if (string.IsNullOrWhiteSpace(destination.TextKey))
                    errors.Add($"Botão/rota {destination.Route} sem chave de destino.");
                if (destination.UsesScene)
                {
                    string path = FindEnabledScenePath(destination.SceneName);
                    if (string.IsNullOrWhiteSpace(path))
                        errors.Add($"Rota {destination.Route} sem tela/cena habilitada.");
                }
                else if (!ShellRoutes.Contains(destination.Route))
                {
                    errors.Add($"Rota {destination.Route} sem ScreenView da shell.");
                }
            }

            foreach (NavigationRoute route in Enum.GetValues(typeof(NavigationRoute)))
            {
                if (!routes.Contains(route))
                    errors.Add($"Rota não registrada: {route}.");
            }

            ValidateButtons(MobileNavigationLayout.MainButtons, "barra principal", routes, errors);
            ValidateButtons(MobileNavigationLayout.MoreButtons, "menu Mais", routes, errors);
        }

        private static void ValidateButtons(
            IReadOnlyList<NavigationButtonDefinition> buttons,
            string group,
            HashSet<NavigationRoute> routes,
            List<string> errors)
        {
            var buttonIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < buttons.Count; i++)
            {
                NavigationButtonDefinition button = buttons[i];
                if (string.IsNullOrWhiteSpace(button.ButtonId))
                    errors.Add($"{group}: botão sem ID.");
                else if (!buttonIds.Add(button.ButtonId))
                    errors.Add($"{group}: botão duplicado {button.ButtonId}.");
                if (!button.TargetRoute.HasValue)
                    errors.Add($"{group}: botão {button.ButtonId} sem destino.");
                else if (!routes.Contains(button.TargetRoute.Value))
                    errors.Add($"{group}: botão {button.ButtonId} aponta para rota inválida.");
                if (string.IsNullOrWhiteSpace(button.TextKey))
                    errors.Add($"{group}: botão {button.ButtonId} sem chave de texto.");
            }
        }

        private static void ValidateBootstrap(List<string> errors)
        {
            if (!File.Exists(BootstrapScenePath))
            {
                errors.Add("Cena Bootstrap ausente.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            MobileUiBootstrap[] bootstraps = FindInScene<MobileUiBootstrap>(scene);
            if (bootstraps.Length != 1)
                errors.Add($"Bootstrap requer exatamente um MobileUiBootstrap; encontrado: {bootstraps.Length}.");
            else if (bootstraps[0].Theme == null)
                errors.Add("MobileUiBootstrap sem UiThemeConfig.");

            if (bootstraps.Length == 1)
            {
                try
                {
                    bootstraps[0].Theme?.Validate();
                }
                catch (Exception exception)
                {
                    errors.Add($"Tema inválido: {exception.Message}");
                }
            }

            if (FindInScene<SafeAreaController>(scene).Length > 0)
                errors.Add("SafeAreaController não deve ser duplicado na cena; a shell o cria em runtime.");
        }

        private static void ValidateScenes(
            List<string> errors,
            List<string> warnings)
        {
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < buildScenes.Length; i++)
            {
                if (!buildScenes[i].enabled ||
                    !buildScenes[i].path.StartsWith("Assets/_Game/Scenes/", StringComparison.Ordinal))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(
                    buildScenes[i].path,
                    OpenSceneMode.Single);
                EventSystem[] eventSystems = FindInScene<EventSystem>(scene);
                if (eventSystems.Length > 1)
                    errors.Add($"{scene.name}: EventSystem duplicado ({eventSystems.Length}).");

                Canvas[] canvases = FindInScene<Canvas>(scene);
                int overlayCount = 0;
                for (int canvasIndex = 0; canvasIndex < canvases.Length; canvasIndex++)
                {
                    if (canvases[canvasIndex].renderMode == RenderMode.ScreenSpaceOverlay)
                        overlayCount++;
                }
                if (overlayCount > 1)
                {
                    warnings.Add(
                        $"{scene.name}: {overlayCount} Canvas ScreenSpaceOverlay legados; " +
                        "a shell os mantém por compatibilidade e normaliza navegação em runtime.");
                }

                if (scene.name != "Bootstrap" &&
                    scene.name != "Battle" &&
                    !HasPresentationController(scene))
                {
                    warnings.Add($"{scene.name}: tela sem controller/presenter reconhecido.");
                }
            }
        }

        private static bool HasPresentationController(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] != null &&
                        behaviours[i].GetType().Name.EndsWith(
                            "PanelController",
                            StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static string FindEnabledScenePath(string sceneName)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].enabled &&
                    string.Equals(
                        Path.GetFileNameWithoutExtension(scenes[i].path),
                        sceneName,
                        StringComparison.Ordinal))
                {
                    return scenes[i].path;
                }
            }
            return string.Empty;
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component
        {
            var values = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                values.AddRange(root.GetComponentsInChildren<T>(true));
            return values.ToArray();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, name, StringComparison.Ordinal))
                    return root;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
