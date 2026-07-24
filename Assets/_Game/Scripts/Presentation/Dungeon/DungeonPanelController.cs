using System;
using System.Collections;
using System.Text;
using System.Threading;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Dungeons;
using IdleMedievalLegends.Presentation.Battle;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.Dungeon
{
    public sealed class DungeonPanelController : MonoBehaviour
    {
        private GameManager gameManager;
        private DungeonService service;
        private Font font;
        private bool built;
        private int selectedDungeonIndex;
        private int selectedDifficultyIndex;
        private Text listText;
        private Text dungeonNameText;
        private Text professionText;
        private Text difficultyText;
        private Text powerText;
        private Text energyText;
        private Text rewardsText;
        private Text lockText;
        private Text statusText;
        private Text resultText;
        private Button previousDungeonButton;
        private Button nextDungeonButton;
        private Button difficultyButton;
        private Button enterButton;
        private Button backButton;

        public bool IsOpen => built && service != null && gameObject.activeInHierarchy;
        public DungeonDefinition SelectedDungeon =>
            service?.Dungeons.Dungeons[selectedDungeonIndex];
        public DungeonDifficultyDefinition SelectedDifficulty =>
            SelectedDungeon?.AvailableDifficulties[selectedDifficultyIndex];
        public string Status => statusText != null ? statusText.text : string.Empty;
        public string DisplayedEnergy =>
            energyText != null ? energyText.text : string.Empty;
        public string DisplayedResult =>
            resultText != null ? resultText.text : string.Empty;
        public Button EnterButton => enterButton;
        public Button DifficultyButton => difficultyButton;
        public DungeonRun LastStartedRun { get; private set; }

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
            if (!gameManager.IsReady || gameManager.LocalDungeons == null)
            {
                SetStatus("Serviço local de masmorras indisponível.");
                yield break;
            }
            Initialize(gameManager.LocalDungeons);
        }

        public void Initialize(DungeonService dungeonService)
        {
            BuildIfNeeded();
            service = dungeonService ??
                throw new ArgumentNullException(nameof(dungeonService));
            selectedDungeonIndex = Math.Min(
                selectedDungeonIndex,
                service.Dungeons.Dungeons.Count - 1);
            selectedDifficultyIndex = 0;
            Refresh();
            SetStatus(
                "Protótipo local: Energia, batalha e drops serão autoritativos no servidor.");
        }

        public void SelectDungeon(int index)
        {
            if (service == null)
                throw new InvalidOperationException("Masmorras ainda não inicializadas.");
            if (index < 0 || index >= service.Dungeons.Dungeons.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            selectedDungeonIndex = index;
            selectedDifficultyIndex = 0;
            Refresh();
        }

        public void SelectDifficulty(int index)
        {
            if (SelectedDungeon == null)
                throw new InvalidOperationException("Masmorra ainda não selecionada.");
            if (index < 0 || index >= SelectedDungeon.AvailableDifficulties.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            selectedDifficultyIndex = index;
            Refresh();
        }

        public DungeonRun EnterSelectedDungeon(
            string requestId = null,
            bool loadBattleScene = true)
        {
            if (service == null || gameManager == null)
                throw new InvalidOperationException("Masmorras ainda não inicializadas.");
            try
            {
                string configuredBattleScene =
                    SelectedDifficulty.Encounter.BattleSceneName;
                if (loadBattleScene &&
                    !UnityEngine.Application.CanStreamedLevelBeLoaded(
                        configuredBattleScene))
                {
                    throw new InvalidOperationException(
                        $"Cenário de batalha indisponível: {configuredBattleScene}.");
                }
                var request = new DungeonEntryRequest(
                    requestId ?? $"dungeon_entry_{Guid.NewGuid():N}",
                    SelectedDungeon.DungeonId,
                    SelectedDifficulty.DifficultyId,
                    DungeonService.StandardTeamIds,
                    playerLevel: 1);
                DungeonRun run = service.Enter(request);
                LastStartedRun = run;
                BattleResult battle = service.BeginBattle(run.RunId);
                var scenario = new BattleDebugScenario(
                    gameManager.ContentCatalog,
                    BuildRequestFromResult(run, battle),
                    battle);
                DungeonService stableService = service;
                GameManager stableManager = gameManager;
                string stableRunId = run.RunId;
                BattleScenarioBridge.Publish(
                    scenario,
                    completed =>
                    {
                        stableService.CompleteBattle(stableRunId, completed);
                        _ = stableManager.PersistLocalCacheAsync(CancellationToken.None);
                        if (UnityEngine.Application.CanStreamedLevelBeLoaded("Dungeon"))
                        {
                            SceneManager.LoadSceneAsync(
                                "Dungeon",
                                LoadSceneMode.Single);
                        }
                    });
                Refresh();
                SetStatus(
                    run.PowerBelowRecommended
                        ? "Aviso: Poder abaixo do recomendado. Entrada permitida."
                        : "Energia consumida uma vez. Carregando batalha...");
                if (loadBattleScene)
                {
                    SceneManager.LoadSceneAsync(
                        configuredBattleScene,
                        LoadSceneMode.Single);
                }
                return run;
            }
            catch (Exception exception)
            {
                SetStatus(ToPlayerFeedback(exception));
                Refresh();
                throw;
            }
        }

        public void Refresh()
        {
            if (service == null)
                return;
            service.RegenerateEnergy();
            DungeonDefinition dungeon = SelectedDungeon;
            DungeonDifficultyDefinition difficulty = SelectedDifficulty;
            var list = new StringBuilder();
            for (int i = 0; i < service.Dungeons.Dungeons.Count; i++)
            {
                DungeonDefinition entry = service.Dungeons.Dungeons[i];
                list.Append(i == selectedDungeonIndex ? "▶ " : "  ")
                    .Append(entry.DisplayName)
                    .Append(" • ")
                    .Append(ProfessionName(entry.AssociatedProfession))
                    .AppendLine();
            }
            listText.text = list.ToString();
            dungeonNameText.text =
                $"{dungeon.IconPlaceholder}\n{dungeon.DisplayName}\n{dungeon.Description}";
            professionText.text =
                $"Tipo: {string.Join(", ", dungeon.Tags)}\n" +
                $"Profissão: {ProfessionName(dungeon.AssociatedProfession)}";
            difficultyText.text =
                $"Dificuldade: {difficulty.DifficultyId} • " +
                $"Materiais T{(int)difficulty.MaterialTier}\n" +
                $"Duração estimada: {difficulty.DurationEstimateSeconds}s";
            powerText.text =
                $"Poder recomendado: {difficulty.RecommendedPower:N0}\n" +
                $"Poder da equipe: {service.TeamPower:N0}";
            energyText.text =
                $"Energia: {service.Energy.CurrentEnergy}/{service.Energy.MaximumEnergy}\n" +
                $"Custo: {difficulty.EnergyCost} • " +
                $"+1 a cada {service.EnergyRules.MinutesPerPoint} min";
            rewardsText.text = BuildRewardPreview(difficulty);
            int attempts = service.Progress.GetDailyAttempts(
                dungeon.DungeonId,
                service.Clock.UtcNowUnixMilliseconds);
            lockText.text = dungeon.DailyAttemptLimit.HasValue
                ? $"Tentativas hoje: {attempts}/{dungeon.DailyAttemptLimit.Value}"
                : "Tentativas diárias: sem limite";
            string blockReason = service.GetEntryBlockReason(
                dungeon,
                difficulty,
                playerLevel: 1);
            if (!string.IsNullOrWhiteSpace(blockReason))
                lockText.text += "\n⛔ " + blockReason;
            if (service.TeamPower < difficulty.RecommendedPower)
                lockText.text += "\n⚠ Poder abaixo do recomendado (aviso)";
            enterButton.interactable = string.IsNullOrWhiteSpace(blockReason);
            RenderLatestResult();
        }

        public void ReturnToBattle()
        {
            if (UnityEngine.Application.CanStreamedLevelBeLoaded("Battle"))
                SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
        }

        private static BattleRequest BuildRequestFromResult(
            DungeonRun run,
            BattleResult battle)
        {
            BattleRequest simulated = run.SimulatedBattle == battle
                ? run.BattleRequest
                : null;
            return simulated ?? throw new InvalidOperationException(
                "A run não possui requisição visual.");
        }

        private void RenderLatestResult()
        {
            DungeonRunResult latest = service.LatestResult;
            if (latest == null)
            {
                resultText.text = "Resultado: nenhuma run concluída nesta sessão.";
                return;
            }
            var builder = new StringBuilder();
            builder.Append(latest.Victory ? "VITÓRIA" : "DERROTA");
            if (latest.FirstClear)
                builder.Append(" • FIRST CLEAR");
            builder.AppendLine();
            if (!latest.Victory)
            {
                builder.Append("Nenhuma recompensa concedida.");
            }
            else
            {
                builder.Append("Ouro: ").Append(latest.GoldGranted);
                for (int i = 0; i < latest.Rewards.Count; i++)
                {
                    builder.AppendLine()
                        .Append(latest.Rewards[i].ItemDefinitionId)
                        .Append(" x")
                        .Append(latest.Rewards[i].Quantity);
                }
            }
            resultText.text = builder.ToString();
        }

        private void BuildIfNeeded()
        {
            if (built) return;
            built = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject(
                "DungeonCanvas",
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

            Image background = CreateImage(
                canvasObject.transform,
                "Background",
                Vector2.zero,
                new Vector2(1920f, 1080f),
                new Color(0.055f, 0.045f, 0.035f, 1f));
            CreateText(background.transform, "Title", new Vector2(0f, 475f),
                new Vector2(900f, 65f), 42, "MASMORRAS ATIVAS");
            listText = CreateText(background.transform, "DungeonList",
                new Vector2(-620f, 150f), new Vector2(560f, 550f), 23, string.Empty);
            listText.alignment = TextAnchor.UpperLeft;
            dungeonNameText = CreateText(background.transform, "DungeonName",
                new Vector2(0f, 300f), new Vector2(650f, 180f), 25, string.Empty);
            professionText = CreateText(background.transform, "Profession",
                new Vector2(0f, 160f), new Vector2(650f, 100f), 21, string.Empty);
            difficultyText = CreateText(background.transform, "Difficulty",
                new Vector2(0f, 55f), new Vector2(650f, 100f), 22, string.Empty);
            powerText = CreateText(background.transform, "Power",
                new Vector2(520f, 280f), new Vector2(560f, 100f), 23, string.Empty);
            energyText = CreateText(background.transform, "Energy",
                new Vector2(520f, 165f), new Vector2(560f, 100f), 25, string.Empty);
            rewardsText = CreateText(background.transform, "Rewards",
                new Vector2(520f, 0f), new Vector2(580f, 200f), 20, string.Empty);
            lockText = CreateText(background.transform, "Locks",
                new Vector2(0f, -100f), new Vector2(750f, 90f), 21, string.Empty);
            resultText = CreateText(background.transform, "Result",
                new Vector2(430f, -270f), new Vector2(760f, 220f), 20, string.Empty);
            resultText.color = new Color(1f, 0.83f, 0.35f);
            statusText = CreateText(background.transform, "Status",
                new Vector2(0f, -485f), new Vector2(1600f, 45f), 18, string.Empty);

            previousDungeonButton = CreateButton(background.transform, "PreviousDungeon",
                new Vector2(-160f, -205f), new Vector2(150f, 58f), "Anterior");
            nextDungeonButton = CreateButton(background.transform, "NextDungeon",
                new Vector2(0f, -205f), new Vector2(150f, 58f), "Próxima");
            difficultyButton = CreateButton(background.transform, "DifficultyButton",
                new Vector2(160f, -205f), new Vector2(180f, 58f), "Dificuldade");
            enterButton = CreateButton(background.transform, "EnterButton",
                new Vector2(0f, -300f), new Vector2(300f, 75f), "Entrar");
            backButton = CreateButton(background.transform, "BackButton",
                new Vector2(-540f, -390f), new Vector2(260f, 58f), "Voltar à batalha");
            previousDungeonButton.onClick.AddListener(SelectPreviousDungeon);
            nextDungeonButton.onClick.AddListener(SelectNextDungeon);
            difficultyButton.onClick.AddListener(SelectNextDifficulty);
            enterButton.onClick.AddListener(BeginEnter);
            backButton.onClick.AddListener(ReturnToBattle);
        }

        private void SelectPreviousDungeon()
        {
            int count = service.Dungeons.Dungeons.Count;
            SelectDungeon((selectedDungeonIndex - 1 + count) % count);
        }

        private void SelectNextDungeon()
        {
            SelectDungeon(
                (selectedDungeonIndex + 1) % service.Dungeons.Dungeons.Count);
        }

        private void SelectNextDifficulty()
        {
            SelectDifficulty(
                (selectedDifficultyIndex + 1) %
                SelectedDungeon.AvailableDifficulties.Count);
        }

        private void BeginEnter()
        {
            try
            {
                EnterSelectedDungeon();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Entrada em masmorra rejeitada: {exception.Message}", this);
            }
        }

        private static string BuildRewardPreview(
            DungeonDifficultyDefinition difficulty)
        {
            var builder = new StringBuilder("Recompensas possíveis:");
            for (int i = 0; i < difficulty.RewardTable.Entries.Count; i++)
            {
                DungeonRewardTableEntry entry = difficulty.RewardTable.Entries[i];
                builder.AppendLine()
                    .Append(entry.Guaranteed ? "✓ " : "? ")
                    .Append(entry.ItemDefinitionId)
                    .Append(" x")
                    .Append(entry.MinimumQuantity)
                    .Append("-")
                    .Append(entry.MaximumQuantity);
                if (!entry.Guaranteed)
                    builder.Append(" • ").Append(entry.ChanceBasisPoints).Append(" bp");
            }
            return builder.ToString();
        }

        private static string ProfessionName(
            IdleMedievalLegends.Domain.Common.CraftingProfession profession)
        {
            switch (profession)
            {
                case IdleMedievalLegends.Domain.Common.CraftingProfession.Blacksmith:
                    return "Ferreiro";
                case IdleMedievalLegends.Domain.Common.CraftingProfession.Tailor:
                    return "Costureiro";
                case IdleMedievalLegends.Domain.Common.CraftingProfession.Enchanter:
                    return "Encantador";
                case IdleMedievalLegends.Domain.Common.CraftingProfession.Alchemist:
                    return "Alquimista";
                case IdleMedievalLegends.Domain.Common.CraftingProfession.Gatherer:
                    return "Coletador";
                default:
                    return profession.ToString();
            }
        }

        private static string ToPlayerFeedback(Exception exception)
        {
            string message = exception.Message;
            if (message.IndexOf("Energia insuficiente", StringComparison.Ordinal) >= 0)
                return "Energia insuficiente.";
            if (message.IndexOf("bloqueada", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Masmorra bloqueada.";
            if (message.IndexOf("Tentativas diárias", StringComparison.Ordinal) >= 0)
                return "Tentativas esgotadas.";
            if (message.IndexOf("Poder", StringComparison.Ordinal) >= 0)
                return "Poder abaixo do recomendado.";
            return message;
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
                new Color(0.40f, 0.24f, 0.12f, 1f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(image.transform, "Label", Vector2.zero, size, 20, label);
            return button;
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value ?? string.Empty;
        }
    }
}
