using System;
using System.Text;
using IdleMedievalLegends.Domain.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.Inventory
{
    public sealed class ItemDetailView : MonoBehaviour
    {
        private Text title;
        private Text details;
        private Button equipButton;
        private Button unequipButton;
        private Button lockButton;
        private Button unlockButton;
        private Button dismantleButton;

        public void Build(Font font)
        {
            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(16, 16, 16, 16);
            title = CreateText("Title", font, 22, FontStyle.Bold);
            details = CreateText("Details", font, 15, FontStyle.Normal);
            details.horizontalOverflow = HorizontalWrapMode.Wrap;
            details.verticalOverflow = VerticalWrapMode.Overflow;
            equipButton = CreateButton("Equipar", font);
            unequipButton = CreateButton("Desequipar", font);
            lockButton = CreateButton("Bloquear", font);
            unlockButton = CreateButton("Desbloquear", font);
            dismantleButton = CreateButton("Desmontar (protótipo)", font);
            Clear();
        }

        public void Bind(
            InventoryViewEntry entry,
            Action equip,
            Action unequip,
            Action lockItem,
            Action unlockItem,
            Action dismantle)
        {
            if (entry == null)
            {
                Clear();
                return;
            }
            ItemInstance item = entry.Item;
            title.text = entry.Definition.DisplayName;
            var text = new StringBuilder();
            text.AppendLine($"Ícone: ◆ {entry.Definition.IconReference ?? "placeholder"}");
            text.AppendLine($"Tier: {(int)entry.Definition.Tier}");
            text.AppendLine($"Raridade: {entry.Item.Rarity}");
            text.AppendLine($"Quantidade: {item.Quantity}");
            text.AppendLine($"Estado: {item.State}");
            text.AppendLine($"Bloqueio: {(item.LockedByPlayer ? "sim" : "não")}");
            text.AppendLine($"Equipado em: {EmptyAsDash(item.EquippedHeroInstanceId)}");
            text.AppendLine($"Aprimoramento: +{item.EnhancementLevel}");
            if (item.HasDurability)
                text.AppendLine($"Durabilidade: {item.Durability}/{item.MaxDurability}");
            text.AppendLine();
            text.AppendLine(entry.Definition.Description);
            text.AppendLine();
            text.AppendLine("Stats:");
            if (item.RolledStats.Count == 0) text.AppendLine("—");
            for (int i = 0; i < item.RolledStats.Count; i++)
            {
                RolledStatData stat = item.RolledStats[i];
                text.AppendLine(
                    $"{stat.StatId}: +{stat.FlatValue} / {stat.PercentValue:P0}");
            }
            details.text = text.ToString();

            ConfigureButton(equipButton, item.State == InventoryItemState.Owned, equip);
            ConfigureButton(unequipButton, item.State == InventoryItemState.Equipped, unequip);
            ConfigureButton(lockButton, !item.LockedByPlayer && !item.IsTerminal, lockItem);
            ConfigureButton(unlockButton, item.LockedByPlayer && !item.IsTerminal, unlockItem);
            ConfigureButton(dismantleButton, !item.IsTerminal, dismantle);
        }

        public void Clear()
        {
            if (title == null) return;
            title.text = "Selecione um item";
            details.text = "Os detalhes e atributos aparecem aqui.";
            ConfigureButton(equipButton, false, null);
            ConfigureButton(unequipButton, false, null);
            ConfigureButton(lockButton, false, null);
            ConfigureButton(unlockButton, false, null);
            ConfigureButton(dismantleButton, false, null);
        }

        private Text CreateText(string objectName, Font font, int size, FontStyle style)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(transform, false);
            var text = child.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            return text;
        }

        private Button CreateButton(string label, Font font)
        {
            var child = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(transform, false);
            child.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.28f, 1f);
            var layout = child.AddComponent<LayoutElement>();
            layout.preferredHeight = 42;
            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(child.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 15;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            return child.GetComponent<Button>();
        }

        private static void ConfigureButton(Button button, bool active, Action action)
        {
            if (button == null) return;
            button.gameObject.SetActive(active);
            button.onClick.RemoveAllListeners();
            if (active && action != null) button.onClick.AddListener(() => action());
        }

        private static string EmptyAsDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value;
        }
    }
}
