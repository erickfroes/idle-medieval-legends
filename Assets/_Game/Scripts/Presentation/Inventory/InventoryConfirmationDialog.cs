using System;
using UnityEngine;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.Inventory
{
    public sealed class InventoryConfirmationDialog : MonoBehaviour
    {
        private Text message;
        private Button confirmButton;
        private Action pendingAction;

        public void Build(Font font)
        {
            var image = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.color = new Color(0.05f, 0.06f, 0.08f, 0.98f);
            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            message = CreateText(font);
            confirmButton = CreateButton("Confirmar desmontagem", font);
            Button cancel = CreateButton("Cancelar", font);
            confirmButton.onClick.AddListener(Confirm);
            cancel.onClick.AddListener(Cancel);
            gameObject.SetActive(false);
        }

        public void Show(string text, Action action)
        {
            message.text = text;
            pendingAction = action ?? throw new ArgumentNullException(nameof(action));
            gameObject.SetActive(true);
        }

        public void Confirm()
        {
            Action action = pendingAction;
            pendingAction = null;
            gameObject.SetActive(false);
            action?.Invoke();
        }

        public void Cancel()
        {
            pendingAction = null;
            gameObject.SetActive(false);
        }

        private Text CreateText(Font font)
        {
            var child = new GameObject("Message", typeof(RectTransform));
            child.transform.SetParent(transform, false);
            var text = child.AddComponent<Text>();
            text.font = font;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            child.AddComponent<LayoutElement>().preferredHeight = 100;
            return text;
        }

        private Button CreateButton(string label, Font font)
        {
            var child = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(transform, false);
            child.GetComponent<Image>().color = new Color(0.28f, 0.20f, 0.16f, 1f);
            child.AddComponent<LayoutElement>().preferredHeight = 46;
            var text = CreateText(font);
            text.transform.SetParent(child.transform, false);
            text.text = label;
            text.fontSize = 15;
            return child.GetComponent<Button>();
        }
    }
}
