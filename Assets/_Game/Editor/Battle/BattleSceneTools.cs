using System;
using System.Collections.Generic;
using System.IO;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Presentation.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IdleMedievalLegends.Editor.Battle
{
    public static class BattleSceneTools
    {
        public const string BattleScenePath = "Assets/_Game/Scenes/Battle.unity";
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";
        public const string PresentationConfigPath =
            "Assets/_Game/Data/Presentation/BattlePresentationConfig.asset";
        public const string CatalogPath =
            "Assets/_Game/Data/Content/ContentCatalog.asset";
        public const string CombatBalancePath =
            "Assets/_Game/Data/Balance/CombatBalanceConfig.asset";

        private const string MaterialFolder = "Assets/_Game/Data/Presentation/Materials";
        private const long DefaultSeed = 6006;

        [MenuItem(
            "Tools/Idle Medieval Legends/Scenes/Create or Repair Battle Scene",
            priority = 130)]
        public static void CreateOrRepairFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            CreateOrRepair();
            EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleScenePath);
        }

        public static void GenerateFromCommandLine()
        {
            CreateOrRepair();
        }

        public static void ValidateFromCommandLine()
        {
            IReadOnlyList<string> errors = GetValidationErrors();
            LogValidation(errors);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Cena Battle inválida: {errors.Count} erro(s).");
            }
        }

        public static IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            BattlePresentationConfig config =
                AssetDatabase.LoadAssetAtPath<BattlePresentationConfig>(
                    PresentationConfigPath);
            if (config == null)
                errors.Add($"Configuração ausente: {PresentationConfigPath}.");

            if (AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(CatalogPath) == null)
                errors.Add($"Catálogo ausente: {CatalogPath}.");
            if (AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(CombatBalancePath) == null)
                errors.Add($"Balanceamento ausente: {CombatBalancePath}.");

            ValidateBattleScene(errors);
            ValidateBootstrapTransition(errors);
            ValidateBuildSettings(errors);
            return errors.AsReadOnly();
        }

        public static IReadOnlyList<string> GetSceneValidationErrors(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new ArgumentException("A cena deve estar carregada.", nameof(scene));

            var errors = new List<string>();
            ValidateLoadedBattleScene(scene, errors);
            return errors.AsReadOnly();
        }

        private static void CreateOrRepair()
        {
            EnsureFolder("Assets/_Game/Scenes");
            EnsureFolder("Assets/_Game/Data/Presentation");
            EnsureFolder(MaterialFolder);

            BattlePresentationConfig config =
                LoadOrCreateAsset<BattlePresentationConfig>(PresentationConfigPath);
            config.EnsureValid();
            EditorUtility.SetDirty(config);

            ContentCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(CatalogPath);
            CombatBalanceConfigAsset balance =
                AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(CombatBalancePath);
            if (catalog == null || balance == null)
            {
                throw new InvalidOperationException(
                    "A cena Battle requer os assets das Tasks 003 e 004.");
            }

            Material arenaMaterial = LoadOrCreateMaterial(
                MaterialFolder + "/BattleArena.mat",
                new Color(0.16f, 0.20f, 0.22f));
            Material playerMaterial = LoadOrCreateMaterial(
                MaterialFolder + "/BattlePlayer.mat",
                new Color(0.18f, 0.48f, 0.92f));
            Material enemyMaterial = LoadOrCreateMaterial(
                MaterialFolder + "/BattleEnemy.mat",
                new Color(0.82f, 0.20f, 0.18f));
            Material selectionMaterial = LoadOrCreateMaterial(
                MaterialFolder + "/BattleSelection.mat",
                new Color(1f, 0.72f, 0.08f));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            List<string> sceneErrors = GetBattleSceneErrors();
            if (!File.Exists(BattleScenePath) || sceneErrors.Count > 0)
            {
                CreateCanonicalBattleScene(
                    config,
                    catalog,
                    balance,
                    arenaMaterial,
                    playerMaterial,
                    enemyMaterial,
                    selectionMaterial);
            }

            EnsureBootstrapTransition();
            PutScenesInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            IReadOnlyList<string> errors = GetValidationErrors();
            LogValidation(errors);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Cena Battle gerada com {errors.Count} erro(s).");
            }
        }

        private static void CreateCanonicalBattleScene(
            BattlePresentationConfig config,
            ContentCatalogAsset catalog,
            CombatBalanceConfigAsset balance,
            Material arenaMaterial,
            Material playerMaterial,
            Material enemyMaterial,
            Material selectionMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            // A troca de cena pode descarregar objetos carregados apenas pelo
            // Editor. Recarregamos referências persistentes antes de compor.
            config = AssetDatabase.LoadAssetAtPath<BattlePresentationConfig>(
                PresentationConfigPath);
            catalog = AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(CatalogPath);
            balance = AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(
                CombatBalancePath);
            arenaMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialFolder + "/BattleArena.mat");
            playerMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialFolder + "/BattlePlayer.mat");
            enemyMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialFolder + "/BattleEnemy.mat");
            selectionMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialFolder + "/BattleSelection.mat");
            if (config == null || catalog == null || balance == null ||
                arenaMaterial == null || playerMaterial == null ||
                enemyMaterial == null || selectionMaterial == null)
            {
                throw new InvalidOperationException(
                    "Assets de apresentação ficaram indisponíveis ao criar Battle.");
            }

            Camera camera = CreateCamera();
            CreateLighting();
            CreateArena(arenaMaterial);

            BattleTeamView playerTeam = CreateTeam(
                "PlayerTeam",
                BattleSide.Attacker,
                -3.5f,
                playerMaterial,
                selectionMaterial,
                camera);
            BattleTeamView enemyTeam = CreateTeam(
                "EnemyTeam",
                BattleSide.Defender,
                3.5f,
                enemyMaterial,
                selectionMaterial,
                camera);
            BattleHudView hud = CreateHud();
            CreateEventSystem();

            var presentationRoot = new GameObject("BattlePresentation");
            BattleDebugScenarioProvider provider =
                presentationRoot.AddComponent<BattleDebugScenarioProvider>();
            BattleEventPlayer eventPlayer =
                presentationRoot.AddComponent<BattleEventPlayer>();
            BattleSceneController controller =
                presentationRoot.AddComponent<BattleSceneController>();

            provider.Configure(catalog, balance, DefaultSeed);
            eventPlayer.Configure(config, playerTeam, enemyTeam, hud);
            controller.Configure(provider, eventPlayer, playerTeam, enemyTeam, hud);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.32f, 0.38f);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, BattleScenePath))
                throw new InvalidOperationException("Não foi possível salvar a cena Battle.");
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.07f, 0.10f);
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 8.5f, -11.5f);
            camera.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 1.2f, 0f) - camera.transform.position);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Battle Key Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.82f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private static void CreateArena(Material material)
        {
            var environment = new GameObject("BattleEnvironment");
            GameObject arena = GameObject.CreatePrimitive(PrimitiveType.Plane);
            arena.name = "Arena";
            arena.transform.SetParent(environment.transform, false);
            arena.transform.localScale = new Vector3(1.2f, 1f, 0.7f);
            arena.GetComponent<Renderer>().sharedMaterial = material;

            CreateBoundary(environment.transform, new Vector3(0f, 0.25f, 3.4f));
            CreateBoundary(environment.transform, new Vector3(0f, 0.25f, -3.4f));
        }

        private static void CreateBoundary(Transform parent, Vector3 position)
        {
            GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundary.name = "Arena Boundary";
            boundary.transform.SetParent(parent, false);
            boundary.transform.localPosition = position;
            boundary.transform.localScale = new Vector3(12f, 0.5f, 0.15f);
            Object.DestroyImmediate(boundary.GetComponent<Collider>());
        }

        private static BattleTeamView CreateTeam(
            string name,
            BattleSide side,
            float x,
            Material bodyMaterial,
            Material selectionMaterial,
            Camera camera)
        {
            var teamObject = new GameObject(name);
            BattleTeamView teamView = teamObject.AddComponent<BattleTeamView>();
            var views = new List<BattleUnitView>(3);
            float[] zPositions = { -2.1f, 0f, 2.1f };
            for (int i = 0; i < zPositions.Length; i++)
            {
                views.Add(CreateUnitView(
                    teamObject.transform,
                    side,
                    i,
                    new Vector3(x, 0f, zPositions[i]),
                    bodyMaterial,
                    selectionMaterial,
                    camera));
            }
            teamView.Configure(side, views);
            return teamView;
        }

        private static BattleUnitView CreateUnitView(
            Transform parent,
            BattleSide side,
            int slot,
            Vector3 position,
            Material bodyMaterial,
            Material selectionMaterial,
            Camera camera)
        {
            var root = new GameObject($"{side} Slot {slot + 1}");
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            GameObject body = GameObject.CreatePrimitive(
                slot == 1 ? PrimitiveType.Cube : PrimitiveType.Capsule);
            body.name = "Placeholder Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = slot == 1
                ? new Vector3(1.15f, 1.8f, 1.15f)
                : new Vector3(0.78f, 1f, 0.78f);
            Renderer renderer = body.GetComponent<Renderer>();
            renderer.sharedMaterial = bodyMaterial;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "Selection Indicator";
            indicator.transform.SetParent(root.transform, false);
            indicator.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            indicator.transform.localScale = new Vector3(0.9f, 0.03f, 0.9f);
            indicator.GetComponent<Renderer>().sharedMaterial = selectionMaterial;
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            indicator.SetActive(false);

            HealthBarView healthBar;
            Text nameLabel;
            CreateWorldHealthBar(root.transform, camera, out healthBar, out nameLabel);

            BattleUnitView unitView = root.AddComponent<BattleUnitView>();
            unitView.Configure(
                slot,
                body.transform,
                renderer,
                indicator,
                healthBar,
                nameLabel);
            return unitView;
        }

        private static void CreateWorldHealthBar(
            Transform parent,
            Camera camera,
            out HealthBarView healthBar,
            out Text nameLabel)
        {
            var canvasObject = new GameObject(
                "Unit HUD",
                typeof(RectTransform),
                typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(180f, 58f);
            canvasRect.localPosition = new Vector3(0f, 2.75f, 0f);
            canvasRect.localScale = Vector3.one * 0.01f;
            canvasRect.rotation = camera.transform.rotation;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 5;

            nameLabel = CreateText(
                canvasObject.transform,
                "Unit Name",
                new Vector2(0f, 17f),
                new Vector2(180f, 24f),
                20,
                TextAnchor.MiddleCenter);
            nameLabel.color = Color.white;

            Image background = CreateImage(
                canvasObject.transform,
                "Health Background",
                new Vector2(0f, -10f),
                new Vector2(160f, 20f),
                new Color(0.08f, 0.08f, 0.08f, 0.95f));
            Image fill = CreateImage(
                background.transform,
                "Health Fill",
                Vector2.zero,
                new Vector2(154f, 14f),
                new Color(0.18f, 0.82f, 0.30f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;

            Text valueLabel = CreateText(
                background.transform,
                "Health Value",
                Vector2.zero,
                new Vector2(160f, 20f),
                14,
                TextAnchor.MiddleCenter);
            valueLabel.color = Color.white;
            healthBar = background.gameObject.AddComponent<HealthBarView>();
            healthBar.Configure(fill, valueLabel);
        }

        private static BattleHudView CreateHud()
        {
            var canvasObject = new GameObject(
                "BattleHud",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image topPanel = CreateStretchedPanel(
                canvasObject.transform,
                "Top Bar",
                true,
                96f);
            Text playerLabel = CreateText(
                topPanel.transform,
                "Player Team",
                new Vector2(-640f, 0f),
                new Vector2(420f, 70f),
                30,
                TextAnchor.MiddleLeft);
            Text enemyLabel = CreateText(
                topPanel.transform,
                "Enemy Team",
                new Vector2(640f, 0f),
                new Vector2(420f, 70f),
                30,
                TextAnchor.MiddleRight);
            Text seedLabel = CreateText(
                topPanel.transform,
                "Seed",
                Vector2.zero,
                new Vector2(300f, 35f),
                20,
                TextAnchor.UpperCenter);
            Text statusLabel = CreateText(
                topPanel.transform,
                "Status",
                new Vector2(0f, -25f),
                new Vector2(700f, 40f),
                22,
                TextAnchor.MiddleCenter);

            Text resultLabel = CreateText(
                canvasObject.transform,
                "Result",
                new Vector2(0f, 105f),
                new Vector2(800f, 120f),
                62,
                TextAnchor.MiddleCenter);
            resultLabel.color = new Color(1f, 0.82f, 0.25f);

            Image bottomPanel = CreateStretchedPanel(
                canvasObject.transform,
                "Bottom Bar",
                false,
                112f);
            Button speedButton = CreateButton(
                bottomPanel.transform,
                "Speed Button",
                new Vector2(-125f, 0f),
                new Vector2(190f, 70f),
                "VELOCIDADE");
            Text speedLabel = CreateText(
                speedButton.transform,
                "Speed Value",
                new Vector2(55f, 0f),
                new Vector2(70f, 60f),
                26,
                TextAnchor.MiddleCenter);
            Button skipButton = CreateButton(
                bottomPanel.transform,
                "Skip Button",
                new Vector2(125f, 0f),
                new Vector2(190f, 70f),
                "PULAR");

            BattleHudView hud = canvasObject.AddComponent<BattleHudView>();
            hud.Configure(
                playerLabel,
                enemyLabel,
                seedLabel,
                speedLabel,
                statusLabel,
                resultLabel,
                speedButton,
                skipButton);
            return hud;
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule module =
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static Image CreateStretchedPanel(
            Transform parent,
            string name,
            bool top,
            float height)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = top ? new Vector2(0f, 1f) : Vector2.zero;
            rect.anchorMax = top ? Vector2.one : new Vector2(1f, 0f);
            rect.pivot = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
            Image image = panelObject.GetComponent<Image>();
            image.color = new Color(0.03f, 0.045f, 0.07f, 0.92f);
            return image;
        }

        private static Image CreateImage(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            int fontSize,
            TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            string label)
        {
            Image image = CreateImage(
                parent,
                name,
                position,
                size,
                new Color(0.14f, 0.25f, 0.38f, 0.98f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text buttonLabel = CreateText(
                image.transform,
                "Label",
                new Vector2(-18f, 0f),
                size,
                24,
                TextAnchor.MiddleCenter);
            buttonLabel.text = label;
            return button;
        }

        private static void EnsureBootstrapTransition()
        {
            Scene scene = EditorSceneManager.OpenScene(
                BootstrapScenePath,
                OpenSceneMode.Single);
            GameObject app = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, "App", StringComparison.Ordinal))
                {
                    app = roots[i];
                    break;
                }
            }
            if (app == null)
                throw new InvalidOperationException("Bootstrap não contém o objeto App.");

            GameManager manager = app.GetComponent<GameManager>();
            if (manager == null)
                throw new InvalidOperationException("Bootstrap não contém GameManager.");

            BootstrapBattleLoader[] loaders =
                app.GetComponents<BootstrapBattleLoader>();
            BootstrapBattleLoader loader = loaders.Length > 0
                ? loaders[0]
                : app.AddComponent<BootstrapBattleLoader>();
            for (int i = 1; i < loaders.Length; i++)
                Object.DestroyImmediate(loaders[i]);

            bool changed = loaders.Length != 1 || loader.GameManager != manager ||
                !string.Equals(loader.BattleSceneName, "Battle", StringComparison.Ordinal) ||
                !loader.LoadWhenReady;
            if (changed)
            {
                loader.Configure(manager);
                EditorUtility.SetDirty(loader);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, BootstrapScenePath))
                {
                    throw new InvalidOperationException(
                        "Não foi possível salvar a transição do Bootstrap.");
                }
            }
        }

        private static void PutScenesInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(BattleScenePath, true)
            };
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                string path = existing[i].path;
                if (!string.Equals(path, BootstrapScenePath, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(path, BattleScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    scenes.Add(existing[i]);
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static List<string> GetBattleSceneErrors()
        {
            var errors = new List<string>();
            ValidateBattleScene(errors);
            return errors;
        }

        private static void ValidateBattleScene(List<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleScenePath) == null)
            {
                errors.Add($"Cena ausente: {BattleScenePath}.");
                return;
            }

            Scene loaded = SceneManager.GetSceneByPath(BattleScenePath);
            bool close = !loaded.IsValid() || !loaded.isLoaded;
            Scene scene = close
                ? EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive)
                : loaded;
            try
            {
                ValidateLoadedBattleScene(scene, errors);
            }
            finally
            {
                if (close)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ValidateLoadedBattleScene(
            Scene scene,
            List<string> errors)
        {
            RequireCount<Camera>(scene, 1, errors);
            RequireCount<Light>(scene, 1, errors);
            RequireCount<EventSystem>(scene, 1, errors);
            RequireCount<InputSystemUIInputModule>(scene, 1, errors);
            RequireCount<Canvas>(scene, 7, errors);
            RequireCount<BattleSceneController>(scene, 1, errors);
            RequireCount<BattleEventPlayer>(scene, 1, errors);
            RequireCount<BattleDebugScenarioProvider>(scene, 1, errors);
            RequireCount<BattleTeamView>(scene, 2, errors);
            RequireCount<BattleUnitView>(scene, 6, errors);
            RequireCount<HealthBarView>(scene, 6, errors);
            RequireCount<BattleHudView>(scene, 1, errors);

            RequireActiveAndEnabled<Camera>(scene, errors);
            RequireActiveAndEnabled<Light>(scene, errors);
            RequireActiveAndEnabled<EventSystem>(scene, errors);
            RequireActiveAndEnabled<InputSystemUIInputModule>(scene, errors);
            RequireActiveAndEnabled<Canvas>(scene, errors);
            RequireActiveAndEnabled<BattleSceneController>(scene, errors);
            RequireActiveAndEnabled<BattleEventPlayer>(scene, errors);
            RequireActiveAndEnabled<BattleDebugScenarioProvider>(scene, errors);
            RequireActiveAndEnabled<BattleTeamView>(scene, errors);
            RequireActiveAndEnabled<BattleUnitView>(scene, errors);
            RequireActiveAndEnabled<HealthBarView>(scene, errors);
            RequireActiveAndEnabled<BattleHudView>(scene, errors);

            ValidateComposedReferences(scene, errors);
        }

        private static void ValidateComposedReferences(
            Scene scene,
            List<string> errors)
        {
            List<BattleSceneController> controllers =
                GetSceneComponents<BattleSceneController>(scene);
            List<BattleEventPlayer> eventPlayers =
                GetSceneComponents<BattleEventPlayer>(scene);
            List<BattleDebugScenarioProvider> providers =
                GetSceneComponents<BattleDebugScenarioProvider>(scene);
            List<BattleHudView> huds = GetSceneComponents<BattleHudView>(scene);
            List<BattleTeamView> teams = GetSceneComponents<BattleTeamView>(scene);

            if (controllers.Count == 1 && !controllers[0].IsConfigured)
                errors.Add("BattleSceneController possui referências ausentes.");
            if (eventPlayers.Count == 1 && !eventPlayers[0].IsConfigured)
                errors.Add("BattleEventPlayer possui referências ausentes.");
            if (providers.Count == 1 && !providers[0].IsConfigured)
                errors.Add("BattleDebugScenarioProvider possui assets inválidos.");
            if (huds.Count == 1 && !huds[0].IsConfigured)
                errors.Add("BattleHudView possui referências ausentes.");

            bool hasAttacker = false;
            bool hasDefender = false;
            for (int i = 0; i < teams.Count; i++)
            {
                if (!teams[i].IsConfigured)
                    errors.Add($"BattleTeamView {teams[i].name} está incompleta.");
                hasAttacker |= teams[i].Side == BattleSide.Attacker;
                hasDefender |= teams[i].Side == BattleSide.Defender;
            }
            if (teams.Count == 2 && (!hasAttacker || !hasDefender))
                errors.Add("Battle requer uma view por lado atacante/defensor.");

            if (controllers.Count != 1 || eventPlayers.Count != 1 ||
                providers.Count != 1 || huds.Count != 1 || teams.Count != 2)
            {
                return;
            }

            BattleSceneController controller = controllers[0];
            BattleEventPlayer eventPlayer = eventPlayers[0];
            BattleDebugScenarioProvider provider = providers[0];
            BattleHudView hud = huds[0];

            if (controller.EventPlayer != eventPlayer)
                errors.Add("BattleSceneController referencia outro BattleEventPlayer.");
            if (controller.ScenarioProvider != provider)
                errors.Add("BattleSceneController referencia outro scenario provider.");
            if (controller.HudView != hud || eventPlayer.HudView != hud)
                errors.Add("Controller e event player devem compartilhar o HUD da cena.");
            if (controller.PlayerTeamView != eventPlayer.PlayerTeamView ||
                controller.EnemyTeamView != eventPlayer.EnemyTeamView)
            {
                errors.Add(
                    "Controller e event player devem compartilhar as mesmas equipes.");
            }

            if (!teams.Contains(controller.PlayerTeamView) ||
                !teams.Contains(controller.EnemyTeamView) ||
                controller.PlayerTeamView == controller.EnemyTeamView)
            {
                errors.Add("Controller deve referenciar as duas equipes distintas da cena.");
            }
            else if (controller.PlayerTeamView.Side != BattleSide.Attacker ||
                controller.EnemyTeamView.Side != BattleSide.Defender)
            {
                errors.Add(
                    "Controller deve mapear jogador para Attacker e inimigo para Defender.");
            }

            BattlePresentationConfig expectedConfig =
                AssetDatabase.LoadAssetAtPath<BattlePresentationConfig>(
                    PresentationConfigPath);
            if (eventPlayer.PresentationConfig != expectedConfig)
                errors.Add("BattleEventPlayer referencia outra configuração de apresentação.");

            ContentCatalogAsset expectedCatalog =
                AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(CatalogPath);
            CombatBalanceConfigAsset expectedBalance =
                AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(
                    CombatBalancePath);
            if (provider.ContentCatalog != expectedCatalog ||
                provider.CombatBalance != expectedBalance)
            {
                errors.Add("Scenario provider referencia assets diferentes dos canônicos.");
            }
        }

        private static void ValidateBootstrapTransition(List<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) == null)
            {
                errors.Add("Cena Bootstrap ausente.");
                return;
            }

            Scene loaded = SceneManager.GetSceneByPath(BootstrapScenePath);
            bool close = !loaded.IsValid() || !loaded.isLoaded;
            Scene scene = close
                ? EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive)
                : loaded;
            try
            {
                List<BootstrapBattleLoader> loaders =
                    GetSceneComponents<BootstrapBattleLoader>(scene);
                if (loaders.Count != 1)
                {
                    errors.Add(
                        "Bootstrap deve possuir exatamente um BootstrapBattleLoader.");
                }
                else if (!loaders[0].LoadWhenReady ||
                    !string.Equals(
                        loaders[0].BattleSceneName,
                        "Battle",
                        StringComparison.Ordinal))
                {
                    errors.Add("BootstrapBattleLoader possui configuração inválida.");
                }
            }
            finally
            {
                if (close)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ValidateBuildSettings(List<string> errors)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length < 2 || !scenes[0].enabled || !scenes[1].enabled ||
                !string.Equals(
                    scenes[0].path,
                    BootstrapScenePath,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    scenes[1].path,
                    BattleScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Build Settings devem iniciar com Bootstrap e Battle habilitadas.");
            }

            int bootstrapCount = 0;
            int battleCount = 0;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (string.Equals(
                    scenes[i].path,
                    BootstrapScenePath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    bootstrapCount++;
                }
                if (string.Equals(
                    scenes[i].path,
                    BattleScenePath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    battleCount++;
                }
            }
            if (bootstrapCount != 1 || battleCount != 1)
                errors.Add("Bootstrap e Battle devem aparecer uma única vez nos Build Settings.");
        }

        private static void RequireCount<T>(
            Scene scene,
            int expected,
            List<string> errors) where T : Component
        {
            int count = GetSceneComponents<T>(scene).Count;
            if (count != expected)
            {
                errors.Add(
                    $"Battle requer {expected} {typeof(T).Name}; encontrados: {count}.");
            }
        }

        private static void RequireActiveAndEnabled<T>(
            Scene scene,
            List<string> errors) where T : Behaviour
        {
            List<T> components = GetSceneComponents<T>(scene);
            for (int i = 0; i < components.Count; i++)
            {
                T component = components[i];
                if (!component.gameObject.activeInHierarchy)
                {
                    errors.Add(
                        $"{typeof(T).Name} em '{component.gameObject.name}' " +
                        "está em GameObject inativo.");
                }
                else if (!component.enabled)
                {
                    errors.Add(
                        $"{typeof(T).Name} em '{component.gameObject.name}' está desabilitado.");
                }
            }
        }

        private static List<T> GetSceneComponents<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                result.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return result;
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material LoadOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                    Shader.Find("Standard");
                if (shader == null)
                    throw new InvalidOperationException("Shader Lit não encontrado.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.color != color)
            {
                material.color = color;
                EditorUtility.SetDirty(material);
            }
            return material;
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

        private static void LogValidation(IReadOnlyList<string> errors)
        {
            for (int i = 0; i < errors.Count; i++)
                Debug.LogError($"[BattleSceneValidation] {errors[i]}");
            if (errors.Count == 0)
            {
                Debug.Log(
                    "[BattleSceneValidation] Battle válida: composição, Bootstrap " +
                    "e Build Settings conferidos.");
            }
        }
    }
}
