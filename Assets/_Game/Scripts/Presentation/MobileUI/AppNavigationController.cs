using System;
using System.Collections;
using System.Collections.Generic;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Campaign;
using IdleMedievalLegends.Domain.Crafting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.MobileUI
{
    public sealed class HomeScreenViewModel
    {
        public long TeamPower { get; set; }
        public int CurrentStageSequence { get; set; }
        public long Gold { get; set; }
        public int Energy { get; set; }
        public int MaximumEnergy { get; set; }
        public long OfflineMilliseconds { get; set; }
        public int ActiveCraftingJobs { get; set; }
    }

    public sealed class HomeScreenPresenter : ScreenPresenter
    {
        private readonly Func<GameManager> gameManagerProvider;

        public HomeScreenPresenter(Func<GameManager> provider)
            : base(NavigationRoute.Home)
        {
            gameManagerProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public HomeScreenViewModel ViewModel { get; private set; } = new HomeScreenViewModel();

        public override void Activate()
        {
            Refresh();
        }

        public void Refresh()
        {
            GameManager manager = gameManagerProvider();
            if (manager == null || !manager.IsReady)
            {
                SetState(ScreenState.Loading);
                return;
            }

            try
            {
                IdleProgressionService idle = manager.IdleProgression;
                DungeonService dungeons = manager.LocalDungeons;
                LocalCraftingService crafting = manager.LocalCrafting;
                OfflineRewardReport report = idle?.Progress?.PendingOfflineReport;

                ViewModel = new HomeScreenViewModel
                {
                    TeamPower = idle?.TeamPower ?? dungeons?.TeamPower ?? 0,
                    CurrentStageSequence = idle?.CurrentStage?.Sequence ?? 0,
                    Gold = manager.GoldWallet?.GoldBalance ?? 0,
                    Energy = dungeons?.Energy?.CurrentEnergy ?? 0,
                    MaximumEnergy = dungeons?.Energy?.MaximumEnergy ?? 0,
                    OfflineMilliseconds = report != null && !report.Collected
                        ? report.EligibleDurationMilliseconds
                        : 0,
                    ActiveCraftingJobs = CountActiveJobs(crafting)
                };
                SetState(ScreenState.Ready);
            }
            catch (Exception exception)
            {
                SetState(ScreenState.Error, exception.Message);
            }
        }

        private static int CountActiveJobs(LocalCraftingService crafting)
        {
            if (crafting == null)
                return 0;
            int count = 0;
            IReadOnlyList<CraftingJob> jobs = crafting.Queue.Jobs;
            for (int i = 0; i < jobs.Count; i++)
            {
                if (jobs[i].IsActive)
                    count++;
            }
            return count;
        }
    }

    public sealed class AppHeader : MonoBehaviour
    {
        private Text title;
        private CurrencyDisplay gold;
        private CurrencyDisplay energy;

        public void Build(UiThemeConfig theme, Font font, IUiTextService texts)
        {
            Image image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.color = theme.Surface;
            title = UiRuntimeFactory.CreateText(
                transform, "Title", string.Empty, theme.TitleFontSize, font, theme.TextPrimary);
            title.rectTransform.anchorMin = new Vector2(0.04f, 0f);
            title.rectTransform.anchorMax = new Vector2(0.48f, 1f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
            title.alignment = TextAnchor.MiddleLeft;

            GameObject goldObject = UiRuntimeFactory.CreateObject(
                transform, "Gold", typeof(RectTransform));
            RectTransform goldRect = goldObject.GetComponent<RectTransform>();
            goldRect.anchorMin = new Vector2(0.50f, 0.12f);
            goldRect.anchorMax = new Vector2(0.74f, 0.88f);
            goldRect.offsetMin = goldRect.offsetMax = Vector2.zero;
            gold = goldObject.AddComponent<CurrencyDisplay>();
            gold.Build(theme, font, texts.Get("currency.gold"));

            GameObject energyObject = UiRuntimeFactory.CreateObject(
                transform, "Energy", typeof(RectTransform));
            RectTransform energyRect = energyObject.GetComponent<RectTransform>();
            energyRect.anchorMin = new Vector2(0.74f, 0.12f);
            energyRect.anchorMax = new Vector2(0.98f, 0.88f);
            energyRect.offsetMin = energyRect.offsetMax = Vector2.zero;
            energy = energyObject.AddComponent<CurrencyDisplay>();
            energy.Build(theme, font, texts.Get("currency.energy"));
        }

        public void Bind(string titleText, long goldValue, int energyValue)
        {
            title.text = titleText ?? string.Empty;
            gold.SetValue(goldValue);
            energy.SetValue(energyValue);
        }
    }

    public class AppNavigationController : MonoBehaviour
    {
        [SerializeField] private UiThemeConfig theme;
        [SerializeField] private bool openHomeWhenReady = true;

        private readonly Dictionary<NavigationRoute, ScreenView> screenViews =
            new Dictionary<NavigationRoute, ScreenView>();
        private readonly AsyncActionGate navigationGate = new AsyncActionGate();
        private readonly ModalService modalService = new ModalService();
        private readonly ToastService toastService = new ToastService();
        private readonly Dictionary<Text, int> baseFontSizes =
            new Dictionary<Text, int>();
        private readonly Dictionary<Text, Color> baseFontColors =
            new Dictionary<Text, Color>();
        private NavigationHistory history;
        private IUiTextService texts;
        private UiPreferenceService preferenceService;
        private UiAccessibilitySettings accessibility;
        private Font font;
        private Canvas shellCanvas;
        private EventSystem ownedEventSystem;
        private RectTransform contentRoot;
        private AppHeader header;
        private LoadingOverlay loadingOverlay;
        private ConfirmationDialog confirmationDialog;
        private HomeScreenPresenter homePresenter;
        private Text homeSummary;
        private string pendingSceneName = string.Empty;
        private List<NavigationRoute> pendingHistorySnapshot;
        private Func<string, bool> sceneAvailability =
            sceneName => UnityEngine.Application.CanStreamedLevelBeLoaded(sceneName);
        private bool built;
        private float nextHomeRefresh;

        public static AppNavigationController Instance { get; private set; }
        public NavigationRoute CurrentRoute => history?.Current ?? NavigationRoute.Home;
        public NavigationHistory History => history;
        public ModalService Modals => modalService;
        public ToastService Toasts => toastService;
        public LoadingOverlay Loading => loadingOverlay;
        public UiAccessibilitySettings Accessibility => accessibility;
        public bool IsBuilt => built;
        public bool IsNavigating => navigationGate.IsBusy;

        public void Configure(UiThemeConfig uiTheme)
        {
            theme = uiTheme;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Build();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private IEnumerator Start()
        {
            if (!openHomeWhenReady)
                yield break;

            GameManager manager = GameManager.Instance;
            while (manager != null &&
                   !manager.IsReady &&
                   manager.State != GameLifecycleState.Faulted)
            {
                yield return null;
            }

            if (manager != null && manager.IsReady)
            {
                homePresenter.Refresh();
                UpdateHeader();
                Navigate(NavigationRoute.Home);
            }
        }

        private void Update()
        {
            if (CurrentRoute != NavigationRoute.Home || Time.unscaledTime < nextHomeRefresh)
                return;

            nextHomeRefresh = Time.unscaledTime + 1f;
            homePresenter.Refresh();
            RenderHome();
            UpdateHeader();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this)
                Instance = null;
        }

        public bool Navigate(NavigationRoute route)
        {
            NavigationDestination destination;
            try
            {
                destination = NavigationRouteRegistry.Get(route);
            }
            catch (ArgumentOutOfRangeException)
            {
                toastService.ShowError(texts.Get("error.invalid_route"));
                return false;
            }

            if (destination.UsesScene &&
                !IsSceneActive(destination.SceneName) &&
                !sceneAvailability(destination.SceneName))
            {
                toastService.ShowError(
                    string.Format(texts.Get("error.scene_unavailable"), destination.SceneName));
                return false;
            }

            if (!navigationGate.TryBegin())
                return false;

            CapturePendingHistory();
            history.Push(route);
            ApplyRoute(destination, true);
            return true;
        }

        public bool HandleBack()
        {
            if (modalService.IsOpen)
                return modalService.Close();
            NavigationRoute route;
            if (!history.TryPeekBack(out route))
            {
                if (CurrentRoute == NavigationRoute.Home)
                    return false;
                route = NavigationRoute.Home;
            }

            NavigationDestination destination = NavigationRouteRegistry.Get(route);
            if (destination.UsesScene &&
                !IsSceneActive(destination.SceneName) &&
                !sceneAvailability(destination.SceneName))
            {
                toastService.ShowError(
                    string.Format(texts.Get("error.scene_unavailable"), destination.SceneName));
                return false;
            }
            if (!navigationGate.TryBegin())
                return false;

            CapturePendingHistory();
            if (history.CanGoBack)
                history.TryBack(out route);
            else
                history.Reset(route);
            ApplyRoute(destination, true);
            return true;
        }

        public bool ShowConfirmation(
            string title,
            string message,
            Action confirm,
            Action cancel = null)
        {
            return confirmationDialog.Show(title, message, confirm, cancel);
        }

        public ScreenView GetScreenView(NavigationRoute route)
        {
            screenViews.TryGetValue(route, out ScreenView view);
            return view;
        }

        internal void SetSceneAvailabilityForTests(Func<string, bool> availability)
        {
            sceneAvailability = availability ??
                throw new ArgumentNullException(nameof(availability));
        }

        private void ApplyRoute(NavigationDestination destination, bool loadScene)
        {
            UpdateHeader();
            if (destination.UsesScene)
            {
                HideAllScreens();
                if (loadScene && IsSceneActive(destination.SceneName))
                {
                    pendingSceneName = string.Empty;
                    loadingOverlay.Hide();
                    ApplyScenePresentationSettings(destination.SceneName);
                    CompleteNavigation();
                    return;
                }

                loadingOverlay.Show(texts.Get("common.loading"));
                if (loadScene &&
                    sceneAvailability(destination.SceneName))
                {
                    pendingSceneName = destination.SceneName;
                    StartCoroutine(LoadRouteScene(destination));
                    return;
                }

                pendingSceneName = string.Empty;
                toastService.ShowError(
                    string.Format(texts.Get("error.scene_unavailable"), destination.SceneName));
                RestoreNavigationAfterFailure();
                return;
            }

            ShowShellScreen(destination.Route);
            loadingOverlay.Hide();
            CompleteNavigation();
        }

        private IEnumerator LoadRouteScene(NavigationDestination destination)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(
                destination.SceneName,
                LoadSceneMode.Single);
            if (operation == null)
            {
                pendingSceneName = string.Empty;
                toastService.ShowError(
                    string.Format(texts.Get("error.scene_load"), destination.SceneName));
                RestoreNavigationAfterFailure();
                yield break;
            }

            while (!operation.isDone)
                yield return null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            NormalizeEventSystems();
            if (!string.IsNullOrWhiteSpace(pendingSceneName) &&
                !string.Equals(pendingSceneName, scene.name, StringComparison.Ordinal))
            {
                return;
            }
            if (string.Equals(pendingSceneName, scene.name, StringComparison.Ordinal))
                pendingSceneName = string.Empty;

            if (NavigationRouteRegistry.TryGetByScene(scene.name, out NavigationDestination route) &&
                CurrentRoute != route.Route &&
                !(openHomeWhenReady &&
                  CurrentRoute == NavigationRoute.Home &&
                  route.Route == NavigationRoute.Battle))
            {
                history.Push(route.Route);
            }
            UpdateHeader();
            ApplyScenePresentationSettings(scene.name);
            loadingOverlay.Hide();
            CompleteNavigation();
        }

        private void ShowShellScreen(NavigationRoute route)
        {
            foreach (KeyValuePair<NavigationRoute, ScreenView> entry in screenViews)
            {
                if (entry.Key == route)
                    entry.Value.Show();
                else
                    entry.Value.Hide();
            }

            contentRoot.gameObject.SetActive(true);
            if (route == NavigationRoute.Home)
                RenderHome();
        }

        private void HideAllScreens()
        {
            foreach (ScreenView view in screenViews.Values)
                view.Hide();
            contentRoot.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (built)
                return;
            built = true;

            if (theme == null)
                theme = ScriptableObject.CreateInstance<UiThemeConfig>();
            theme.Validate();
            texts = new PortugueseUiTextService();
            preferenceService = new UiPreferenceService(new PlayerPrefsUiPreferenceStore());
            accessibility = preferenceService.Load();
            history = new NavigationHistory(NavigationRoute.Home);
            font = theme.PrimaryFont != null
                ? theme.PrimaryFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateEventSystem();
            CreateShellCanvas();
            CreateScreens();
            CreateHeader();
            CreateBottomNavigation();
            CreateOverlays();

            MobileInputController input = gameObject.AddComponent<MobileInputController>();
            input.Configure(this);
            UiSoundHooks soundHooks = gameObject.AddComponent<UiSoundHooks>();
            soundHooks.Configure(accessibility);
            CaptureBaseFontSizes();
            ApplyAccessibility();
            ShowShellScreen(NavigationRoute.Home);
            UpdateHeader();
        }

        private void CreateEventSystem()
        {
            GameObject eventObject = new GameObject(
                "MobileUiEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventObject.transform.SetParent(transform, false);
            ownedEventSystem = eventObject.GetComponent<EventSystem>();
        }

        private void CreateShellCanvas()
        {
            GameObject canvasObject = UiRuntimeFactory.CreateObject(
                transform,
                "MobileUiShellCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            shellCanvas = canvasObject.GetComponent<Canvas>();
            shellCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            shellCanvas.sortingOrder = 2000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject safeObject = UiRuntimeFactory.CreateObject(
                canvasObject.transform, "SafeArea", typeof(RectTransform));
            RectTransform safeRect = safeObject.GetComponent<RectTransform>();
            UiRuntimeFactory.Stretch(safeRect);
            SafeAreaController safeArea = safeObject.AddComponent<SafeAreaController>();
            safeArea.Configure(safeRect);

            GameObject contentObject = UiRuntimeFactory.CreateObject(
                safeRect, "ScreenContent", typeof(RectTransform));
            contentRoot = contentObject.GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 0.075f);
            contentRoot.anchorMax = new Vector2(1f, 0.94f);
            contentRoot.offsetMin = contentRoot.offsetMax = Vector2.zero;
            ResponsiveLayoutController responsive =
                safeObject.AddComponent<ResponsiveLayoutController>();
            responsive.Configure(contentRoot);
        }

        private void CreateHeader()
        {
            RectTransform safe = shellCanvas.transform.Find("SafeArea") as RectTransform;
            GameObject headerObject = UiRuntimeFactory.CreateObject(
                safe, "ReusableHeader", typeof(RectTransform), typeof(Image));
            RectTransform rect = headerObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.94f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            header = headerObject.AddComponent<AppHeader>();
            header.Build(theme, font, texts);
        }

        private void CreateBottomNavigation()
        {
            RectTransform safe = shellCanvas.transform.Find("SafeArea") as RectTransform;
            GameObject navigationObject = UiRuntimeFactory.CreateObject(
                safe, "MainTabBar", typeof(RectTransform), typeof(Image));
            RectTransform rect = navigationObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0.075f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            navigationObject.GetComponent<Image>().color = theme.Surface;
            var layout = navigationObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = theme.SpacingUnit;
            layout.padding = new RectOffset(
                Mathf.RoundToInt(theme.SpacingUnit),
                Mathf.RoundToInt(theme.SpacingUnit),
                Mathf.RoundToInt(theme.SpacingUnit),
                Mathf.RoundToInt(theme.SpacingUnit));
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            TabBar tabBar = navigationObject.AddComponent<TabBar>();
            NavigationRoute[] tabRoutes =
                new NavigationRoute[MobileNavigationLayout.MainButtons.Count];

            for (int i = 0; i < MobileNavigationLayout.MainButtons.Count; i++)
            {
                NavigationButtonDefinition definition =
                    MobileNavigationLayout.MainButtons[i];
                NavigationRoute route = definition.TargetRoute.Value;
                tabRoutes[i] = route;
                UiRuntimeFactory.CreateButton<IconButton>(
                    navigationObject.transform,
                    definition.ButtonId,
                    theme,
                    font,
                    texts.Get(definition.TextKey),
                    () => Navigate(route),
                    route);
            }
            tabBar.Configure(tabRoutes);
        }

        private void CreateScreens()
        {
            homePresenter = new HomeScreenPresenter(() => GameManager.Instance);
            ScreenView home = CreateScreen(NavigationRoute.Home, homePresenter);
            BuildHome(home.transform);

            BuildMessageScreen(
                NavigationRoute.Heroes,
                ScreenState.Empty,
                texts.Get("home.heroes_empty"));
            BuildMoreScreen();
            BuildMessageScreen(
                NavigationRoute.Gacha,
                ScreenState.Empty,
                texts.Get("gacha.unavailable"));
            BuildMessageScreen(
                NavigationRoute.Market,
                ScreenState.Locked,
                texts.Get("market.locked"));
            BuildMessageScreen(
                NavigationRoute.Profile,
                ScreenState.Empty,
                texts.Get("home.profile_empty"));
            BuildSettingsScreen();
        }

        private ScreenView CreateScreen(NavigationRoute route, ScreenPresenter presenter)
        {
            GameObject screenObject = UiRuntimeFactory.CreateObject(
                contentRoot, route + "Screen", typeof(RectTransform), typeof(Image));
            RectTransform rect = screenObject.GetComponent<RectTransform>();
            UiRuntimeFactory.Stretch(rect);
            screenObject.GetComponent<Image>().color = theme.Background;
            ScreenView view = screenObject.AddComponent<ScreenView>();
            view.Configure(route, presenter);
            screenViews.Add(route, view);
            return view;
        }

        private void BuildHome(Transform parent)
        {
            GameObject panelObject = UiRuntimeFactory.CreateObject(
                parent, "HomeSummary", typeof(RectTransform), typeof(Image));
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.04f, 0.08f);
            panel.anchorMax = new Vector2(0.96f, 0.94f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            panelObject.GetComponent<Image>().color = theme.Surface;

            homeSummary = UiRuntimeFactory.CreateText(
                panel,
                "Summary",
                string.Empty,
                theme.BodyFontSize,
                font,
                theme.TextPrimary);
            homeSummary.rectTransform.anchorMin = new Vector2(0.06f, 0.28f);
            homeSummary.rectTransform.anchorMax = new Vector2(0.94f, 0.88f);
            homeSummary.rectTransform.offsetMin = homeSummary.rectTransform.offsetMax = Vector2.zero;
            homeSummary.alignment = TextAnchor.UpperLeft;

            PrimaryButton battle = UiRuntimeFactory.CreateButton<PrimaryButton>(
                panel,
                "QuickBattle",
                theme,
                font,
                texts.Get("home.quick_battle"),
                () => Navigate(NavigationRoute.Battle),
                NavigationRoute.Battle);
            RectTransform battleRect = battle.GetComponent<RectTransform>();
            battleRect.anchorMin = new Vector2(0.06f, 0.08f);
            battleRect.anchorMax = new Vector2(0.48f, 0.21f);
            battleRect.offsetMin = battleRect.offsetMax = Vector2.zero;

            SecondaryButton rewards = UiRuntimeFactory.CreateButton<SecondaryButton>(
                panel,
                "QuickRewards",
                theme,
                font,
                texts.Get("home.quick_rewards"),
                () => Navigate(NavigationRoute.Campaign),
                NavigationRoute.Campaign);
            RectTransform rewardsRect = rewards.GetComponent<RectTransform>();
            rewardsRect.anchorMin = new Vector2(0.52f, 0.08f);
            rewardsRect.anchorMax = new Vector2(0.94f, 0.21f);
            rewardsRect.offsetMin = rewardsRect.offsetMax = Vector2.zero;
        }

        private void RenderHome()
        {
            HomeScreenViewModel model = homePresenter.ViewModel;
            if (homePresenter.State == ScreenState.Loading)
            {
                homeSummary.text = texts.Get("common.loading");
                return;
            }
            if (homePresenter.State == ScreenState.Error)
            {
                homeSummary.text =
                    $"{texts.Get("home.load_error")}\n{homePresenter.ErrorMessage}";
                return;
            }

            TimeSpan offline = TimeSpan.FromMilliseconds(model.OfflineMilliseconds);
            string stage = model.CurrentStageSequence <= 0
                ? "—"
                : string.Format(texts.Get("home.stage_value"), model.CurrentStageSequence);
            homeSummary.text =
                $"{texts.Get("home.team_power")}\n{CurrencyFormatter.Format(model.TeamPower)}\n\n" +
                $"{texts.Get("home.current_stage")}\n{stage}\n\n" +
                $"{texts.Get("home.gold")}\n{CurrencyFormatter.Format(model.Gold)}\n\n" +
                $"{texts.Get("home.energy")}\n{model.Energy}/{model.MaximumEnergy}\n\n" +
                $"{texts.Get("home.offline_progress")}\n{offline.Hours:00}h {offline.Minutes:00}min\n\n" +
                $"{texts.Get("home.crafting_active")}\n{model.ActiveCraftingJobs}";
        }

        private void BuildMessageScreen(
            NavigationRoute route,
            ScreenState state,
            string message)
        {
            ScreenView screen = CreateScreen(route, new StaticScreenPresenter(route, state));
            if (state == ScreenState.Locked)
            {
                LockedFeatureCard card = screen.gameObject.AddComponent<LockedFeatureCard>();
                card.Build(theme, font);
                card.SetMessage($"🔒 {texts.Get("common.coming_soon")}\n\n{message}");
            }
            else
            {
                EmptyState empty = screen.gameObject.AddComponent<EmptyState>();
                empty.Build(theme, font);
                empty.SetMessage(message);
            }
        }

        private void BuildMoreScreen()
        {
            ScreenView screen = CreateScreen(
                NavigationRoute.More,
                new StaticScreenPresenter(NavigationRoute.More));
            GameObject listObject = UiRuntimeFactory.CreateObject(
                screen.transform, "MoreMenu", typeof(RectTransform), typeof(VerticalLayoutGroup));
            RectTransform rect = listObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.08f);
            rect.anchorMax = new Vector2(0.92f, 0.92f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = listObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = theme.SpacingUnit * 1.5f;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            for (int i = 0; i < MobileNavigationLayout.MoreButtons.Count; i++)
            {
                NavigationButtonDefinition buttonDefinition =
                    MobileNavigationLayout.MoreButtons[i];
                NavigationRoute route = buttonDefinition.TargetRoute.Value;
                NavigationDestination destination = NavigationRouteRegistry.Get(route);
                string suffix = destination.Locked
                    ? " • " + texts.Get("common.coming_soon")
                    : string.Empty;
                UiRuntimeFactory.CreateButton<SecondaryButton>(
                    listObject.transform,
                    buttonDefinition.ButtonId,
                    theme,
                    font,
                    texts.Get(buttonDefinition.TextKey) + suffix,
                    () => Navigate(route),
                    route);
            }
        }

        private void BuildSettingsScreen()
        {
            ScreenView screen = CreateScreen(
                NavigationRoute.Settings,
                new StaticScreenPresenter(NavigationRoute.Settings));
            GameObject listObject = UiRuntimeFactory.CreateObject(
                screen.transform, "SettingsList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            RectTransform rect = listObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.08f);
            rect.anchorMax = new Vector2(0.92f, 0.92f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = listObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = theme.SpacingUnit;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            CreateSettingToggle(listObject.transform, texts.Get("settings.music"), () => accessibility.MusicEnabled,
                value => accessibility.MusicEnabled = value);
            CreateSettingToggle(listObject.transform, texts.Get("settings.effects"), () => accessibility.EffectsEnabled,
                value => accessibility.EffectsEnabled = value);
            CreateSettingToggle(listObject.transform, texts.Get("settings.vibration"), () => accessibility.VibrationEnabled,
                value => accessibility.VibrationEnabled = value);
            CreateSettingToggle(listObject.transform, texts.Get("settings.reduce_motion"), () => accessibility.ReduceMotion,
                value => accessibility.ReduceMotion = value);
            CreateSettingToggle(listObject.transform, texts.Get("settings.high_contrast"), () => accessibility.HighContrast,
                value => accessibility.HighContrast = value);
            CreateCycleSetting(
                listObject.transform,
                texts.Get("settings.text_scale"),
                () => accessibility.TextScale.ToString("0.0") + "x",
                () =>
                {
                    accessibility.TextScale = accessibility.TextScale >= 1.4f
                        ? 0.8f
                        : accessibility.TextScale + 0.2f;
                });
            CreateCycleSetting(
                listObject.transform,
                texts.Get("settings.battle_speed"),
                () => accessibility.BattleSpeed + "x",
                () => accessibility.BattleSpeed = accessibility.BattleSpeed >= 3
                    ? 1
                    : accessibility.BattleSpeed + 1);
            CreateCycleSetting(
                listObject.transform,
                texts.Get("settings.language"),
                () => texts.Get("settings.language_placeholder"),
                () => { });
            CreateCycleSetting(
                listObject.transform,
                texts.Get("settings.account"),
                () => texts.Get("settings.account_placeholder"),
                () => { });
        }

        private void CreateSettingToggle(
            Transform parent,
            string label,
            Func<bool> getter,
            Action<bool> setter)
        {
            SecondaryButton button = null;
            button = UiRuntimeFactory.CreateButton<SecondaryButton>(
                parent,
                label.Replace(" ", string.Empty),
                theme,
                font,
                string.Empty,
                () =>
                {
                    setter(!getter());
                    button.Label.text =
                        $"{label}: {texts.Get(getter() ? "common.on" : "common.off")}";
                    ApplyAccessibility();
                    preferenceService.Save(accessibility);
                    toastService.ShowSuccess(texts.Get("settings.saved"));
                });
            button.Label.text =
                $"{label}: {texts.Get(getter() ? "common.on" : "common.off")}";
        }

        private void CreateCycleSetting(
            Transform parent,
            string label,
            Func<string> value,
            Action cycle)
        {
            SecondaryButton button = null;
            button = UiRuntimeFactory.CreateButton<SecondaryButton>(
                parent,
                label.Replace(" ", string.Empty),
                theme,
                font,
                string.Empty,
                () =>
                {
                    cycle();
                    button.Label.text = $"{label}: {value()}";
                    ApplyAccessibility();
                    preferenceService.Save(accessibility);
                });
            button.Label.text = $"{label}: {value()}";
        }

        private void CreateOverlays()
        {
            GameObject loadingObject = UiRuntimeFactory.CreateObject(
                shellCanvas.transform, "LoadingOverlay", typeof(RectTransform), typeof(Image));
            UiRuntimeFactory.Stretch(loadingObject.GetComponent<RectTransform>());
            loadingOverlay = loadingObject.AddComponent<LoadingOverlay>();
            loadingOverlay.Build(theme, font, texts.Get("common.loading"));

            GameObject modalObject = UiRuntimeFactory.CreateObject(
                shellCanvas.transform, "Modal", typeof(RectTransform), typeof(Image));
            UiRuntimeFactory.Stretch(modalObject.GetComponent<RectTransform>());
            Modal modal = modalObject.AddComponent<Modal>();
            modal.Build(theme, font, modalService, texts);

            GameObject confirmationObject = UiRuntimeFactory.CreateObject(
                shellCanvas.transform, "ConfirmationDialog", typeof(RectTransform));
            confirmationDialog = confirmationObject.AddComponent<ConfirmationDialog>();
            confirmationDialog.Configure(modalService);

            GameObject toastObject = UiRuntimeFactory.CreateObject(
                shellCanvas.transform, "Toast", typeof(RectTransform), typeof(Image));
            RectTransform toastRect = toastObject.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.08f, 0.80f);
            toastRect.anchorMax = new Vector2(0.92f, 0.88f);
            toastRect.offsetMin = toastRect.offsetMax = Vector2.zero;
            Toast toast = toastObject.AddComponent<Toast>();
            toast.Build(theme, font, toastService);
        }

        private void UpdateHeader()
        {
            if (header == null)
                return;
            GameManager manager = GameManager.Instance;
            long gold = manager?.GoldWallet?.GoldBalance ?? 0;
            int energy = manager?.LocalDungeons?.Energy?.CurrentEnergy ?? 0;
            NavigationDestination destination = NavigationRouteRegistry.Get(CurrentRoute);
            header.Bind(texts.Get(destination.TextKey), gold, energy);
        }

        private void NormalizeEventSystems()
        {
            EventSystem[] systems = FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != ownedEventSystem)
                    systems[i].gameObject.SetActive(false);
            }
            ownedEventSystem.gameObject.SetActive(true);
        }

        private bool IsSceneActive(string sceneName)
        {
            return string.Equals(
                SceneManager.GetActiveScene().name,
                sceneName,
                StringComparison.Ordinal);
        }

        private void ApplyScenePresentationSettings(string sceneName)
        {
            if (!string.Equals(sceneName, "Battle", StringComparison.Ordinal))
                return;

            GameObject battlePresentation = GameObject.Find("BattlePresentation");
            battlePresentation?.SendMessage(
                "SetSpeed",
                accessibility.BattleSpeed,
                SendMessageOptions.DontRequireReceiver);
        }

        private void CapturePendingHistory()
        {
            pendingHistorySnapshot = new List<NavigationRoute>(history.Entries);
        }

        private void CompleteNavigation()
        {
            pendingHistorySnapshot = null;
            navigationGate.End();
        }

        private void RestoreNavigationAfterFailure()
        {
            if (pendingHistorySnapshot != null)
                history.Restore(pendingHistorySnapshot);
            pendingHistorySnapshot = null;

            NavigationDestination restored =
                NavigationRouteRegistry.Get(CurrentRoute);
            if (restored.UsesScene)
                HideAllScreens();
            else
                ShowShellScreen(restored.Route);

            UpdateHeader();
            loadingOverlay.Hide();
            navigationGate.End();
        }

        private void CaptureBaseFontSizes()
        {
            Text[] labels = shellCanvas.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                baseFontSizes[labels[i]] = labels[i].fontSize;
                baseFontColors[labels[i]] = labels[i].color;
            }
        }

        private void ApplyAccessibility()
        {
            foreach (KeyValuePair<Text, int> entry in baseFontSizes)
            {
                if (entry.Key == null)
                    continue;
                entry.Key.fontSize = Mathf.Max(
                    12,
                    Mathf.RoundToInt(entry.Value * accessibility.TextScale));
                entry.Key.color = accessibility.HighContrast
                    ? Color.white
                    : baseFontColors[entry.Key];
            }
        }
    }
}
