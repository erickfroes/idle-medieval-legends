using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.Inventory
{
    public sealed class InventoryPanelController : MonoBehaviour
    {
        private PlayerInventory inventory;
        private ContentCatalogLookup catalog;
        private GameManager gameManager;
        private HeroEquipmentContext prototypeHero;
        private readonly InventoryFilter filter = new InventoryFilter();
        private InventorySortMode sortMode = InventorySortMode.TierDescending;
        private IReadOnlyList<InventoryViewEntry> visibleItems = Array.Empty<InventoryViewEntry>();
        private string selectedInstanceId = string.Empty;
        private RectTransform listContent;
        private ItemDetailView detailView;
        private InventoryConfirmationDialog confirmationDialog;
        private Text statusText;
        private Text categoryButtonText;
        private Font font;
        private long lastTimestamp;
        private bool built;

        public int VisibleItemCount => visibleItems.Count;
        public string SelectedInstanceId => selectedInstanceId;
        public InventoryCategoryFilter ActiveCategory => filter.Category;
        public bool IsOpen => built && gameObject.activeInHierarchy;

        private IEnumerator Start()
        {
            BuildIfNeeded();
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                SetStatus("GameManager não encontrado. Abra pelo fluxo Bootstrap → Battle.");
                yield break;
            }
            while (!gameManager.IsReady && gameManager.State != GameLifecycleState.Faulted)
                yield return null;
            if (!gameManager.IsReady)
            {
                SetStatus("Bootstrap falhou; inventário indisponível.");
                yield break;
            }
            Initialize(gameManager.Inventory, gameManager.ContentCatalog, gameManager.CurrentPlayerId);
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= Refresh;
        }

        public void Initialize(
            PlayerInventory playerInventory,
            ContentCatalogLookup contentCatalog,
            string playerId)
        {
            BuildIfNeeded();
            if (inventory != null) inventory.Changed -= Refresh;
            inventory = playerInventory ?? throw new ArgumentNullException(nameof(playerInventory));
            catalog = contentCatalog ?? throw new ArgumentNullException(nameof(contentCatalog));
            inventory.ConfigureDefinitionResolver(catalog);
            HeroDefinition hero = catalog.GetHero("hero_paladin_001");
            prototypeHero = new HeroEquipmentContext(
                "dev_hero_paladin",
                playerId,
                100,
                hero.Tags);
            lastTimestamp = Math.Max(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                inventory.CaptureSnapshotForCache().GeneratedAtUnixMilliseconds);
            inventory.Changed += Refresh;
            Refresh();
            SetStatus("Cache local de protótipo; backend continuará autoritativo.");
        }

        public void SetCategory(InventoryCategoryFilter category)
        {
            filter.Category = category;
            if (categoryButtonText != null)
                categoryButtonText.text = $"Filtro: {CategoryLabel(category)}";
            Refresh();
        }

        public void CycleCategory()
        {
            int next = ((int)filter.Category + 1) %
                       Enum.GetValues(typeof(InventoryCategoryFilter)).Length;
            SetCategory((InventoryCategoryFilter)next);
        }

        public void SetSortMode(InventorySortMode mode)
        {
            sortMode = mode;
            Refresh();
        }

        public void SetLockedOnly(bool enabled)
        {
            filter.LockedOnly = enabled;
            Refresh();
        }

        public bool SelectItem(string instanceId)
        {
            for (int i = 0; i < visibleItems.Count; i++)
            {
                if (!string.Equals(visibleItems[i].Item.InstanceId, instanceId,
                    StringComparison.Ordinal)) continue;
                selectedInstanceId = instanceId;
                BindDetails(visibleItems[i]);
                return true;
            }
            return false;
        }

        public void EquipSelected()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
            ExecuteSelected((item, definition) =>
            {
                if (!(definition is EquipmentDefinition equipment))
                    throw new InvalidOperationException("Item selecionado não é equipamento.");
                inventory.Equip(item.InstanceId, prototypeHero, equipment, NextTimestamp());
            }, "Item equipado.");
#else
            SetStatus("Equipar localmente é desabilitado em build de produção.");
#endif
        }

        public void UnequipSelected()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
            ExecuteSelected(
                (item, _) => inventory.Unequip(item.InstanceId, NextTimestamp()),
                "Item desequipado.");
#else
            SetStatus("Desequipar localmente é desabilitado em build de produção.");
#endif
        }

        public void LockSelected()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
            ExecuteSelected(
                (item, _) => inventory.Lock(item.InstanceId, NextTimestamp()),
                "Item bloqueado.");
#else
            SetStatus("Bloqueio local é desabilitado em build de produção.");
#endif
        }

        public void UnlockSelected()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
            ExecuteSelected(
                (item, _) => inventory.Unlock(item.InstanceId, NextTimestamp()),
                "Item desbloqueado.");
#else
            SetStatus("Desbloqueio local é desabilitado em build de produção.");
#endif
        }

        public void RequestDismantleSelected()
        {
            if (string.IsNullOrWhiteSpace(selectedInstanceId)) return;
            confirmationDialog.Show(
                "Desmontar destrói permanentemente a instância selecionada. Continuar?",
                DismantleSelectedConfirmed);
        }

        public void ConfirmDestructiveAction()
        {
            confirmationDialog.Confirm();
        }

        public void ReturnToBattle()
        {
            if (UnityEngine.Application.CanStreamedLevelBeLoaded("Battle"))
                SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
        }

        private void DismantleSelectedConfirmed()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
            try
            {
                DevelopmentInventorySeeder.Dismantle(
                    inventory,
                    catalog,
                    selectedInstanceId,
                    NextTimestamp());
                selectedInstanceId = string.Empty;
                SetStatus("Item desmontado; materiais locais de desenvolvimento gerados.");
                PersistCache();
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message);
            }
#else
            SetStatus("Desmontagem local é desabilitada em build de produção.");
#endif
        }

        private void ExecuteSelected(
            Action<ItemInstance, ItemDefinition> action,
            string successMessage)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(selectedInstanceId)) return;
            try
            {
                ItemInstance item = inventory.GetItem(selectedInstanceId);
                ItemDefinition definition = catalog.GetItem(item.DefinitionId);
                action(item, definition);
                SetStatus(successMessage);
                PersistCache();
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message);
            }
        }

        private async void PersistCache()
        {
            if (gameManager == null) return;
            try
            {
                await gameManager.PersistLocalCacheAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Falha ao persistir inventário: {exception.Message}", this);
            }
        }

        private void Refresh()
        {
            if (inventory == null || catalog == null) return;
            visibleItems = InventoryQuery.Execute(inventory.Items, catalog, filter, sortMode);
            RebuildRows();
            if (!string.IsNullOrWhiteSpace(selectedInstanceId))
            {
                if (!SelectItem(selectedInstanceId))
                {
                    selectedInstanceId = string.Empty;
                    detailView.Clear();
                }
            }
        }

        private void RebuildRows()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Destroy(listContent.GetChild(i).gameObject);
            for (int i = 0; i < visibleItems.Count; i++)
            {
                InventoryViewEntry entry = visibleItems[i];
                string itemId = entry.Item.InstanceId;
                Button row = CreateButton(listContent, FormatRow(entry), 54);
                row.onClick.AddListener(() => SelectItem(itemId));
            }
        }

        private void BindDetails(InventoryViewEntry entry)
        {
            detailView.Bind(
                entry,
                EquipSelected,
                UnequipSelected,
                LockSelected,
                UnlockSelected,
                RequestDismantleSelected);
        }

        private long NextTimestamp()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            lastTimestamp = Math.Max(now, checked(lastTimestamp + 1));
            return lastTimestamp;
        }

        private void BuildIfNeeded()
        {
            if (built) return;
            built = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();

            var canvasObject = new GameObject("InventoryCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            RectTransform root = CreatePanel(canvasObject.transform, "InventoryRoot",
                new Color(0.055f, 0.07f, 0.09f, 1f));
            Stretch(root);

            Text title = CreateText(root, "INVENTÁRIO LOCAL — PROTÓTIPO", 28, FontStyle.Bold);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 60);
            titleRect.anchoredPosition = new Vector2(0, -18);
            title.alignment = TextAnchor.MiddleCenter;

            Button back = CreateButton(root, "Voltar à Batalha", 44);
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = backRect.anchorMax = new Vector2(0, 1);
            backRect.pivot = new Vector2(0, 1);
            backRect.sizeDelta = new Vector2(190, 44);
            backRect.anchoredPosition = new Vector2(20, -20);
            back.onClick.AddListener(ReturnToBattle);

            Button category = CreateButton(root, "Filtro: Todos", 44);
            categoryButtonText = category.GetComponentInChildren<Text>();
            RectTransform categoryRect = category.GetComponent<RectTransform>();
            categoryRect.anchorMin = categoryRect.anchorMax = new Vector2(0, 1);
            categoryRect.pivot = new Vector2(0, 1);
            categoryRect.sizeDelta = new Vector2(240, 44);
            categoryRect.anchoredPosition = new Vector2(24, -90);
            category.onClick.AddListener(CycleCategory);

            RectTransform listPanel = CreatePanel(root, "ListPanel",
                new Color(0.09f, 0.11f, 0.14f, 1f));
            listPanel.anchorMin = new Vector2(0, 0);
            listPanel.anchorMax = new Vector2(0.58f, 1);
            listPanel.offsetMin = new Vector2(24, 70);
            listPanel.offsetMax = new Vector2(-8, -150);
            var scroll = listPanel.gameObject.AddComponent<ScrollRect>();
            RectTransform viewport = CreatePanel(listPanel, "Viewport", Color.clear);
            Stretch(viewport);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            listContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            listContent.SetParent(viewport, false);
            listContent.anchorMin = new Vector2(0, 1);
            listContent.anchorMax = new Vector2(1, 1);
            listContent.pivot = new Vector2(0.5f, 1);
            listContent.sizeDelta = Vector2.zero;
            var vertical = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 6;
            vertical.padding = new RectOffset(8, 8, 8, 8);
            vertical.childForceExpandHeight = false;
            listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = listContent;
            scroll.horizontal = false;

            RectTransform detailPanel = CreatePanel(root, "DetailPanel",
                new Color(0.09f, 0.11f, 0.14f, 1f));
            detailPanel.anchorMin = new Vector2(0.58f, 0);
            detailPanel.anchorMax = new Vector2(1, 1);
            detailPanel.offsetMin = new Vector2(8, 70);
            detailPanel.offsetMax = new Vector2(-24, -90);
            detailView = detailPanel.gameObject.AddComponent<ItemDetailView>();
            detailView.Build(font);

            statusText = CreateText(root, string.Empty, 15, FontStyle.Italic);
            statusText.rectTransform.anchorMin = new Vector2(0, 0);
            statusText.rectTransform.anchorMax = new Vector2(1, 0);
            statusText.rectTransform.pivot = new Vector2(0.5f, 0);
            statusText.rectTransform.sizeDelta = new Vector2(0, 50);
            statusText.rectTransform.anchoredPosition = new Vector2(0, 10);
            statusText.alignment = TextAnchor.MiddleCenter;

            RectTransform dialogRect = CreatePanel(canvasObject.transform, "ConfirmationDialog",
                Color.clear);
            dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(520, 320);
            confirmationDialog = dialogRect.gameObject.AddComponent<InventoryConfirmationDialog>();
            confirmationDialog.Build(font);
        }

        private Button CreateButton(Transform parent, string label, float height)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = new Color(0.17f, 0.22f, 0.28f, 1f);
            buttonObject.AddComponent<LayoutElement>().preferredHeight = height;
            Text text = CreateText(buttonObject.transform, label, 15, FontStyle.Normal);
            Stretch(text.rectTransform);
            text.alignment = TextAnchor.MiddleCenter;
            return buttonObject.GetComponent<Button>();
        }

        private Text CreateText(
            Transform parent,
            string value,
            int size,
            FontStyle style)
        {
            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            _ = new GameObject("EventSystem", typeof(EventSystem),
                typeof(InputSystemUIInputModule));
        }

        private static string FormatRow(InventoryViewEntry entry)
        {
            string flags = entry.Item.LockedByPlayer ? " 🔒" : string.Empty;
            return $"◆ {entry.Definition.DisplayName}  | T{(int)entry.Definition.Tier} " +
                   $"{entry.Item.Rarity} | x{entry.Item.Quantity} | " +
                   $"{entry.Item.State}{flags}";
        }

        private static string CategoryLabel(InventoryCategoryFilter category)
        {
            switch (category)
            {
                case InventoryCategoryFilter.All: return "Todos";
                case InventoryCategoryFilter.Equipment: return "Equipamentos";
                case InventoryCategoryFilter.Materials: return "Materiais";
                case InventoryCategoryFilter.Consumables: return "Consumíveis";
                default: return category.ToString();
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message ?? string.Empty;
        }
    }
}
