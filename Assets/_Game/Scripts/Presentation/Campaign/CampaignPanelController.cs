using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Campaign;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.Campaign
{
    public sealed class CampaignPanelController : MonoBehaviour
    {
        private GameManager gameManager;
        private IdleProgressionService service;
        private Font font;
        private bool built;
        private Text currentStageText;
        private Text highestStageText;
        private Text recommendedPowerText;
        private Text teamPowerText;
        private Text resultText;
        private Text goldRateText;
        private Text offlineLimitText;
        private Text goldBalanceText;
        private Text statusText;
        private GameObject offlineModal;
        private Text offlineDurationText;
        private Text offlineRewardsText;
        private Text offlineMaterialsText;
        private Button battleButton;
        private Button collectButton;
        private Button simulateButton;
        private Button backButton;

        public bool IsOpen => built && service != null && gameObject.activeInHierarchy;
        public bool IsOfflineModalOpen => offlineModal != null && offlineModal.activeSelf;
        public string Status => statusText != null ? statusText.text : string.Empty;
        public string DisplayedGoldRate =>
            goldRateText != null ? goldRateText.text : string.Empty;
        public CampaignBattleResult LastBattle { get; private set; }
        public OfflineRewardReport CurrentReport { get; private set; }
        public Button BattleButton => battleButton;
        public Button CollectButton => collectButton;
        public Button SimulateButton => simulateButton;

        private IEnumerator Start()
        {
            BuildIfNeeded();
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                SetStatus("GameManager não encontrado. Abra pelo Bootstrap.");
                yield break;
            }
            while (!gameManager.IsReady && gameManager.State != GameLifecycleState.Faulted)
                yield return null;
            if (!gameManager.IsReady || gameManager.IdleProgression == null)
            {
                SetStatus("Progressão idle local indisponível.");
                yield break;
            }
            Initialize(gameManager.IdleProgression);
            Task returnReportTask = TryOpenReturnReportAsync();
            while (!returnReportTask.IsCompleted)
                yield return null;
            if (returnReportTask.IsFaulted)
            {
                Exception exception =
                    returnReportTask.Exception?.GetBaseException() ??
                    new InvalidOperationException(
                        "Falha desconhecida ao abrir relatório offline.");
                SetStatus("Falha ao persistir o relatório offline.");
                Debug.LogException(exception, this);
            }
        }

        public void Initialize(IdleProgressionService progressionService)
        {
            BuildIfNeeded();
            service = progressionService ??
                throw new ArgumentNullException(nameof(progressionService));
            Refresh();
            SetStatus(
                "Simulação local de desenvolvimento; produção usará tempo e recompensas do servidor.");
        }

        public CampaignBattleResult StartCurrentStage()
        {
            if (service == null)
                throw new InvalidOperationException("Campanha ainda não inicializada.");
            string requestId = $"campaign_battle_{Guid.NewGuid():N}";
            long seed = Math.Max(1, service.Clock.UtcNowUnixMilliseconds);
            LastBattle = service.BattleStage(
                service.Progress.CurrentStageId,
                requestId,
                seed);
            resultText.text = LastBattle.Victory
                ? LastBattle.FirstClear
                    ? "Vitória — primeira conclusão"
                    : "Vitória — recompensa de repetição"
                : "Derrota — estágio mantido";
            Refresh();
            PersistCache();
            return LastBattle;
        }

        public async Task<OfflineRewardReport> OpenOfflineReportAsync(
            string requestId = null)
        {
            if (service == null)
                throw new InvalidOperationException("Campanha ainda não inicializada.");
            if (gameManager == null)
                throw new InvalidOperationException(
                    "GameManager necessário para persistir o relatório.");

            OfflineRewardReport generatedReport = service.GenerateOfflineReport(
                requestId ?? $"offline_{Guid.NewGuid():N}");
            await gameManager.PersistLocalCacheAsync(CancellationToken.None);

            CurrentReport = generatedReport;
            RenderOfflineReport(generatedReport);
            offlineModal.SetActive(true);
            return generatedReport;
        }

        public OfflineRewardReport CollectCurrentReport()
        {
            if (CurrentReport == null)
                throw new InvalidOperationException("Nenhum relatório aberto.");
            OfflineRewardReport collected =
                service.CollectOfflineReport(CurrentReport.RequestId);
            CurrentReport = collected;
            offlineModal.SetActive(false);
            Refresh();
            SetStatus("Relatório coletado sem recalcular recompensas.");
            PersistCache();
            return collected;
        }

        public async Task SimulateReturnHoursAsync(long hours)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS)
            throw new PlatformNotSupportedException(
                "Simulação temporal disponível somente em development builds.");
#else
            if (gameManager == null || service == null)
                throw new InvalidOperationException("Campanha ainda não inicializada.");
            var clock = new DevelopmentGameClock(service.Clock.UtcNowUnixMilliseconds);
            clock.AdvanceHours(hours);
            gameManager.ResetLocalIdlePrototype(clock, service.CaptureSnapshot());
            service = gameManager.IdleProgression;
            Refresh();
            await OpenOfflineReportAsync();
#endif
        }

        public void ReturnToBattle()
        {
            if (UnityEngine.Application.CanStreamedLevelBeLoaded("Battle"))
                SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
        }

        private async Task TryOpenReturnReportAsync()
        {
            if (service.Progress.PendingOfflineReport != null)
            {
                CurrentReport = service.Progress.PendingOfflineReport;
                RenderOfflineReport(CurrentReport);
                offlineModal.SetActive(true);
                return;
            }
            if (service.HighestClearedStage == null)
                return;
            long elapsed = service.Clock.UtcNowUnixMilliseconds -
                           service.Progress.LastClaimedServerTime;
            if (elapsed >= 60_000L)
                await OpenOfflineReportAsync();
        }

        private void Refresh()
        {
            if (service == null)
                return;
            CampaignStageDefinition current = service.CurrentStage;
            currentStageText.text = $"Estágio atual: {current.StageId}";
            highestStageText.text = "Maior estágio: " +
                (service.HighestClearedStage?.StageId ?? "nenhum");
            recommendedPowerText.text = $"Poder recomendado: {current.RecommendedPower:N0}";
            teamPowerText.text = $"Poder da equipe: {service.TeamPower:N0}";
            IdleProductionProfile production =
                service.BuildCurrentProductionProfile();
            goldRateText.text =
                $"Produção: {production.GoldPerMinute:N0} ouro/min";
            offlineLimitText.text = $"Limite offline: {service.PlayerOfflineLimitHours}h";
            goldBalanceText.text = $"Ouro: {service.GoldBalance:N0}";
        }

        private void RenderOfflineReport(OfflineRewardReport report)
        {
            TimeSpan eligible = TimeSpan.FromMilliseconds(
                report.EligibleDurationMilliseconds);
            TimeSpan real = TimeSpan.FromMilliseconds(report.RealDurationMilliseconds);
            offlineDurationText.text =
                $"Ausência: {FormatDuration(real)}\nElegível: {FormatDuration(eligible)}";
            offlineRewardsText.text =
                $"Estágio {report.StageId}\nOuro: {report.Gold:N0}\nXP: {report.AccountExperience:N0}";
            var materialBuilder = new StringBuilder();
            for (int i = 0; i < report.Materials.Count; i++)
            {
                if (i > 0) materialBuilder.AppendLine();
                materialBuilder.Append(report.Materials[i].MaterialDefinitionId)
                    .Append(": ")
                    .Append(report.Materials[i].Quantity);
            }
            offlineMaterialsText.text = materialBuilder.Length == 0
                ? "Materiais: nenhum"
                : "Materiais:\n" + materialBuilder;
            collectButton.interactable = !report.Collected;
        }

        private void BuildIfNeeded()
        {
            if (built) return;
            built = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject(
                "CampaignCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage(
                canvasObject.transform,
                "Background",
                Vector2.zero,
                new Vector2(1920f, 1080f),
                new Color(0.045f, 0.06f, 0.085f, 1f));
            CreateText(background.transform, "Title", new Vector2(0f, 430f),
                new Vector2(900f, 80f), 48, "CAMPANHA");
            currentStageText = CreateText(background.transform, "CurrentStage",
                new Vector2(-400f, 280f), new Vector2(700f, 55f), 30, string.Empty);
            highestStageText = CreateText(background.transform, "HighestStage",
                new Vector2(-400f, 215f), new Vector2(700f, 55f), 26, string.Empty);
            recommendedPowerText = CreateText(background.transform, "RecommendedPower",
                new Vector2(-400f, 130f), new Vector2(700f, 55f), 26, string.Empty);
            teamPowerText = CreateText(background.transform, "TeamPower",
                new Vector2(-400f, 70f), new Vector2(700f, 55f), 26, string.Empty);
            goldRateText = CreateText(background.transform, "GoldRate",
                new Vector2(400f, 280f), new Vector2(650f, 55f), 28, string.Empty);
            offlineLimitText = CreateText(background.transform, "OfflineLimit",
                new Vector2(400f, 215f), new Vector2(650f, 55f), 26, string.Empty);
            goldBalanceText = CreateText(background.transform, "GoldBalance",
                new Vector2(400f, 130f), new Vector2(650f, 55f), 28, string.Empty);
            resultText = CreateText(background.transform, "Result",
                new Vector2(0f, -75f), new Vector2(1100f, 70f), 34, string.Empty);
            resultText.color = new Color(1f, 0.82f, 0.30f);
            statusText = CreateText(background.transform, "Status",
                new Vector2(0f, -420f), new Vector2(1500f, 55f), 20, string.Empty);

            battleButton = CreateButton(background.transform, "BattleButton",
                new Vector2(0f, 10f), new Vector2(330f, 75f), "Batalhar");
            battleButton.onClick.AddListener(() => StartCurrentStage());
            simulateButton = CreateButton(background.transform, "SimulateButton",
                new Vector2(230f, -190f), new Vector2(360f, 62f), "Simular retorno +8h");
            simulateButton.onClick.AddListener(BeginSimulateReturn);
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS)
            simulateButton.gameObject.SetActive(false);
#endif
            backButton = CreateButton(background.transform, "BackButton",
                new Vector2(-230f, -190f), new Vector2(300f, 62f), "Voltar à batalha");
            backButton.onClick.AddListener(ReturnToBattle);

            BuildOfflineModal(background.transform);
        }

        private void BuildOfflineModal(Transform parent)
        {
            offlineModal = new GameObject(
                "OfflineRewardModal",
                typeof(RectTransform),
                typeof(Image));
            offlineModal.transform.SetParent(parent, false);
            RectTransform rect = offlineModal.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(820f, 650f);
            offlineModal.GetComponent<Image>().color =
                new Color(0.075f, 0.10f, 0.15f, 0.99f);
            CreateText(offlineModal.transform, "ModalTitle", new Vector2(0f, 260f),
                new Vector2(700f, 60f), 38, "RETORNO OFFLINE");
            offlineDurationText = CreateText(offlineModal.transform, "Duration",
                new Vector2(0f, 155f), new Vector2(700f, 100f), 26, string.Empty);
            offlineRewardsText = CreateText(offlineModal.transform, "Rewards",
                new Vector2(-190f, 15f), new Vector2(360f, 150f), 24, string.Empty);
            offlineMaterialsText = CreateText(offlineModal.transform, "Materials",
                new Vector2(190f, 15f), new Vector2(360f, 180f), 22, string.Empty);
            collectButton = CreateButton(offlineModal.transform, "CollectButton",
                new Vector2(0f, -245f), new Vector2(320f, 70f), "Coletar");
            collectButton.onClick.AddListener(() => CollectCurrentReport());
            offlineModal.SetActive(false);
        }

        private Text CreateText(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            int fontSize,
            string value)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
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

        private Button CreateButton(
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
                new Color(0.18f, 0.34f, 0.48f, 1f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(image.transform, "Label", Vector2.zero, size, 24, label);
            return button;
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value ?? string.Empty;
        }

        private void PersistCache()
        {
            if (gameManager != null)
                _ = gameManager.PersistLocalCacheAsync(CancellationToken.None);
        }

        private async void BeginSimulateReturn()
        {
            try
            {
                await SimulateReturnHoursAsync(8);
            }
            catch (Exception exception)
            {
                SetStatus("Falha ao persistir o relatório offline.");
                Debug.LogException(exception, this);
            }
        }

        private static string FormatDuration(TimeSpan value)
        {
            return value.TotalHours >= 1d
                ? $"{(long)value.TotalHours}h {value.Minutes:D2}min"
                : $"{value.Minutes}min";
        }
    }
}
