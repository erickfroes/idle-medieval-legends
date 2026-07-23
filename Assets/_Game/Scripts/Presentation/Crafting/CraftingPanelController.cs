using System;
using System.Collections;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Crafting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.Crafting
{
    public sealed class CraftingPanelController : MonoBehaviour
    {
        private LocalCraftingService service;
        private ContentCatalogLookup catalog;
        private CraftingProfession selectedProfession = CraftingProfession.Blacksmith;
        private string selectedRecipeId = string.Empty;
        private string selectedJobId = string.Empty;
        private Text professionSummary;
        private Text recipeSummary;
        private Text queueSummary;
        private Text resultSummary;
        private Text statusText;
        private RectTransform professionButtons;
        private RectTransform recipeButtons;
        private RectTransform queueButtons;
        private Font font;
        private bool built;
        private float nextRefresh;

        public bool IsOpen => built && gameObject.activeInHierarchy;
        public CraftingProfession SelectedProfession => selectedProfession;
        public string SelectedRecipeId => selectedRecipeId;
        public int DisplayedJobCount => service?.Queue.Jobs.Count ?? 0;
        public CraftingResult LastResult { get; private set; }
        public string Status => statusText == null ? string.Empty : statusText.text;

        private IEnumerator Start()
        {
            BuildIfNeeded();
            GameManager manager = GameManager.Instance;
            if (manager == null)
            {
                SetStatus("GameManager não encontrado. Abra pelo Bootstrap.");
                yield break;
            }
            while (!manager.IsReady && manager.State != GameLifecycleState.Faulted)
                yield return null;
            if (!manager.IsReady)
            {
                SetStatus("Bootstrap falhou; crafting indisponível.");
                yield break;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
            Initialize(manager.LocalCrafting, manager.ContentCatalog);
#else
            SetStatus("Crafting requer o serviço autoritativo do servidor nesta build.");
#endif
        }

        private void Update()
        {
            if (service == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.25f;
            service.Refresh();
            RefreshQueue();
        }

        public void Initialize(LocalCraftingService craftingService, ContentCatalogLookup lookup)
        {
            BuildIfNeeded();
            service = craftingService ?? throw new ArgumentNullException(nameof(craftingService));
            catalog = lookup ?? throw new ArgumentNullException(nameof(lookup));
            SelectProfession(CraftingProfession.Blacksmith);
            SetStatus("Protótipo local; produção validará início e conclusão no servidor.");
        }

        public void SelectProfession(CraftingProfession profession)
        {
            if (service == null || !profession.IsCraftingProfession()) return;
            selectedProfession = profession;
            selectedRecipeId = string.Empty;
            RefreshAll();
        }

        public bool SelectRecipe(string recipeId)
        {
            if (catalog == null || !catalog.TryGetRecipe(recipeId, out RecipeDefinition recipe) ||
                recipe.Profession.ToLegacyProfession() != selectedProfession ||
                !recipe.EnabledForNormalGameplay)
                return false;
            selectedRecipeId = recipeId;
            RefreshRecipeSummary();
            return true;
        }

        public CraftingJob StartSelectedRecipe()
        {
            if (service == null)
            {
                SetStatus("Crafting local indisponível nesta build.");
                return null;
            }
            if (string.IsNullOrWhiteSpace(selectedRecipeId))
            {
                SetStatus("Selecione uma receita.");
                return null;
            }
            try
            {
                CraftingJob job = service.StartCraft(
                    selectedProfession, selectedRecipeId, 1);
                selectedJobId = job.JobId;
                SetStatus("Job iniciado; materiais reservados em ReservedByServer.");
                RefreshAll();
                return job;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message);
                return null;
            }
        }

        public CraftingCancellationResult CancelSelectedJob()
        {
            if (service == null)
            {
                SetStatus("Crafting local indisponível nesta build.");
                return null;
            }
            if (string.IsNullOrWhiteSpace(selectedJobId)) return null;
            try
            {
                CraftingCancellationResult result = service.CancelCraft(selectedJobId);
                SetStatus("Job cancelado conforme a política configurada.");
                RefreshAll();
                return result;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message);
                return null;
            }
        }

        public CraftingResult CompleteSelectedJob()
        {
            if (service == null)
            {
                SetStatus("Crafting local indisponível nesta build.");
                return null;
            }
            if (string.IsNullOrWhiteSpace(selectedJobId)) return null;
            try
            {
                LastResult = service.CompleteCraft(selectedJobId);
                SetStatus("Saída coletada no inventário.");
                RefreshAll();
                return LastResult;
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message);
                return null;
            }
        }

        public void SelectJob(string jobId)
        {
            selectedJobId = jobId ?? string.Empty;
            RefreshQueue();
        }

        public void ReturnToBattle()
        {
            if (UnityEngine.Application.CanStreamedLevelBeLoaded("Battle"))
                SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
        }

        private void RefreshAll()
        {
            RefreshProfessions();
            RefreshRecipes();
            RefreshRecipeSummary();
            RefreshQueue();
            RefreshResult();
        }

        private void RefreshProfessions()
        {
            ClearChildren(professionButtons);
            foreach (CraftingProfession profession in new[]
                     {
                         CraftingProfession.Blacksmith,
                         CraftingProfession.Tailor,
                         CraftingProfession.Enchanter,
                         CraftingProfession.Alchemist,
                         CraftingProfession.Gatherer
                     })
            {
                CraftingProfession captured = profession;
                ProfessionProgress progress = service.GetProgress(profession);
                Button button = CreateButton(
                    professionButtons,
                    $"{ProfessionName(profession)}  Nv.{progress.Level}  {RankName(progress.Rank)}");
                button.onClick.AddListener(() => SelectProfession(captured));
            }
            ProfessionProgress selected = service.GetProgress(selectedProfession);
            ProfessionDefinition definition = catalog.GetProfession(
                selectedProfession.ToProfessionType());
            professionSummary.text =
                $"{definition.DisplayName} | Nível {selected.Level} | {RankName(selected.Rank)} | " +
                $"T{selected.MaximumUnlockedTier.ToNumber()} | {definition.StationName} " +
                $"T{selected.StationTier.ToNumber()}\nFoco {selected.FocusCurrent}/{selected.FocusMaximum} | " +
                $"XP {selected.Experience} | Maestria {selected.MasteryExperience} / " +
                $"{selected.MasteryPoints} pts | Fila {service.Queue.ActiveCount}/" +
                service.GetQueueSlotCount(selectedProfession);
        }

        private void RefreshRecipes()
        {
            ClearChildren(recipeButtons);
            for (int i = 0; i < catalog.Catalog.Recipes.Count; i++)
            {
                RecipeDefinition recipe = catalog.Catalog.Recipes[i];
                if (!recipe.EnabledForNormalGameplay ||
                    recipe.Profession.ToLegacyProfession() != selectedProfession) continue;
                string captured = recipe.RecipeId;
                Button button = CreateButton(
                    recipeButtons,
                    $"{catalog.GetItem(recipe.OutputDefinitionId).DisplayName} | " +
                    $"T{(int)recipe.RequiredTier} | {recipe.DurationSeconds}s");
                button.onClick.AddListener(() => SelectRecipe(captured));
            }
        }

        private void RefreshRecipeSummary()
        {
            if (string.IsNullOrWhiteSpace(selectedRecipeId) ||
                !catalog.TryGetRecipe(selectedRecipeId, out RecipeDefinition recipe))
            {
                recipeSummary.text = "Selecione uma receita para ver requisitos.";
                return;
            }
            string materials = string.Empty;
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                RecipeIngredientDefinition ingredient = recipe.Ingredients[i];
                if (i > 0) materials += ", ";
                materials += $"{catalog.GetItem(ingredient.ItemDefinitionId).DisplayName} x{ingredient.Quantity}";
            }
            recipeSummary.text =
                $"Requisitos: Nv.{recipe.RequiredProfessionLevel}, {RankName(recipe.RequiredProfessionRank)}, " +
                $"T{(int)recipe.RequiredTier}, estação T{(int)recipe.RequiredStationTier}\n" +
                $"Materiais: {materials} | Duração: {recipe.DurationSeconds}s | " +
                $"Foco: {recipe.FocusCost} | Ouro: {recipe.GoldCost}";
        }

        private void RefreshQueue()
        {
            if (service == null) return;
            ClearChildren(queueButtons);
            string summary = "Fila:";
            for (int i = 0; i < service.Queue.Jobs.Count; i++)
            {
                CraftingJob job = service.Queue.Jobs[i];
                long remaining = Math.Max(
                    0,
                    job.CompletesAtServerTime - service.ServerTime);
                summary += $"\n{job.JobId} | {job.State} | {remaining / 1000}s";
                string captured = job.JobId;
                Button button = CreateButton(
                    queueButtons, $"{job.JobId}  {job.State}  {remaining / 1000}s");
                button.onClick.AddListener(() => SelectJob(captured));
            }
            queueSummary.text = summary;
        }

        private void RefreshResult()
        {
            if (LastResult == null)
            {
                resultSummary.text = "Resultado: aguardando conclusão.";
                return;
            }
            resultSummary.text =
                $"Resultado: {LastResult.Rarity} | Qualidade {LastResult.QualityScore} | " +
                $"XP +{LastResult.ExperienceGranted} | Saídas {LastResult.Outputs.Count} | " +
                $"Pity {LastResult.PityBefore}→{LastResult.PityAfter}";
        }

        private void BuildIfNeeded()
        {
            if (built) return;
            built = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            var canvasObject = new GameObject("CraftingCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            RectTransform root = canvasObject.GetComponent<RectTransform>();
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 8;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            Text title = CreateText(root, "Profissões e Crafting", 30, 42);
            title.alignment = TextAnchor.MiddleCenter;
            professionButtons = CreateRow(root, 52);
            professionSummary = CreateText(root, string.Empty, 18, 62);
            recipeButtons = CreateRow(root, 52);
            recipeSummary = CreateText(root, string.Empty, 17, 72);
            Button start = CreateButton(root, "Iniciar receita selecionada", 44);
            start.onClick.AddListener(() => StartSelectedRecipe());
            queueButtons = CreateRow(root, 48);
            queueSummary = CreateText(root, "Fila:", 17, 82);
            RectTransform actions = CreateRow(root, 46);
            Button cancel = CreateButton(actions, "Cancelar", 42);
            cancel.onClick.AddListener(() => CancelSelectedJob());
            Button claim = CreateButton(actions, "Concluir / Coletar", 42);
            claim.onClick.AddListener(() => CompleteSelectedJob());
            resultSummary = CreateText(root, "Resultado: aguardando conclusão.", 18, 42);
            statusText = CreateText(root, string.Empty, 16, 44);
            Button back = CreateButton(root, "Voltar à Batalha", 42);
            back.onClick.AddListener(ReturnToBattle);
        }

        private RectTransform CreateRow(Transform parent, float height)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = height;
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            return row.GetComponent<RectTransform>();
        }

        private Button CreateButton(Transform parent, string label, float height = 42)
        {
            var item = new GameObject(label, typeof(RectTransform), typeof(Image),
                typeof(Button), typeof(LayoutElement));
            item.transform.SetParent(parent, false);
            item.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.16f, 0.96f);
            item.GetComponent<LayoutElement>().preferredHeight = height;
            Text text = CreateText(item.transform, label, 15, height);
            text.alignment = TextAnchor.MiddleCenter;
            return item.GetComponent<Button>();
        }

        private Text CreateText(Transform parent, string value, int size, float height)
        {
            var item = new GameObject("Text", typeof(RectTransform), typeof(Text),
                typeof(LayoutElement));
            item.transform.SetParent(parent, false);
            Text text = item.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            item.GetComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private void SetStatus(string value)
        {
            if (statusText != null) statusText.text = value ?? string.Empty;
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private static string ProfessionName(CraftingProfession profession)
        {
            string[] names = { "", "Ferreiro", "Costureiro", "Encantador", "Alquimista", "Coletador" };
            return names[(int)profession];
        }

        private static string RankName(ProfessionRank rank)
        {
            string[] names = { "Aprendiz", "Proficiente", "Mestre", "Grão-Mestre", "Deus" };
            return names[(int)rank];
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }
    }
}
