using System;
using System.Collections;
using IdleMedievalLegends.Domain.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.MobileUI
{
    public abstract class ThemedButton : MonoBehaviour
    {
        protected Button Button;
        public Text Label { get; private set; }

        public NavigationRoute? TargetRoute { get; private set; }

        public virtual void Build(
            UiThemeConfig theme,
            Font font,
            string label,
            UnityAction action,
            NavigationRoute? targetRoute = null)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            Button = gameObject.GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            Image image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            Button.targetGraphic = image;
            image.color = GetColor(theme);
            TargetRoute = targetRoute;

            Label = UiRuntimeFactory.CreateText(
                transform,
                "Label",
                label,
                theme.BodyFontSize,
                font,
                theme.TextPrimary);
            UiRuntimeFactory.Stretch(Label.rectTransform);
            Label.alignment = TextAnchor.MiddleCenter;

            var layout = gameObject.GetComponent<LayoutElement>() ??
                         gameObject.AddComponent<LayoutElement>();
            layout.minHeight = theme.MinimumTouchSize;
            if (action != null)
                Button.onClick.AddListener(action);
        }

        protected abstract Color GetColor(UiThemeConfig theme);
    }

    public sealed class PrimaryButton : ThemedButton
    {
        protected override Color GetColor(UiThemeConfig theme) => theme.Primary;
    }

    public sealed class SecondaryButton : ThemedButton
    {
        protected override Color GetColor(UiThemeConfig theme) => theme.Secondary;
    }

    public sealed class IconButton : ThemedButton
    {
        protected override Color GetColor(UiThemeConfig theme) => theme.ElevatedSurface;
    }

    public sealed class CurrencyDisplay : MonoBehaviour
    {
        private Text label;
        private string symbol;

        public long Value { get; private set; }

        public void Build(UiThemeConfig theme, Font font, string currencySymbol)
        {
            symbol = currencySymbol ?? string.Empty;
            label = UiRuntimeFactory.CreateText(
                transform,
                "CurrencyValue",
                string.Empty,
                theme.BodyFontSize,
                font,
                theme.TextPrimary);
            UiRuntimeFactory.Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
            SetValue(0);
        }

        public void SetValue(long value)
        {
            Value = value;
            if (label != null)
                label.text = $"{symbol} {CurrencyFormatter.Format(value)}";
        }
    }

    public sealed class ProgressBar : MonoBehaviour
    {
        private Image fill;
        private Text value;

        public float NormalizedValue { get; private set; }

        public void Build(UiThemeConfig theme, Font font)
        {
            Image background = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            background.color = theme.Secondary;
            fill = UiRuntimeFactory.CreateImage(transform, "Fill", theme.Success);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            value = UiRuntimeFactory.CreateText(
                transform, "Value", "0%", theme.BodyFontSize, font, theme.TextPrimary);
            UiRuntimeFactory.Stretch(value.rectTransform);
            value.alignment = TextAnchor.MiddleCenter;
        }

        public void SetValue(long current, long maximum)
        {
            NormalizedValue = maximum <= 0
                ? 0f
                : Mathf.Clamp01((float)((double)current / maximum));
            if (fill != null)
                fill.rectTransform.anchorMax = new Vector2(NormalizedValue, 1f);
            if (value != null)
                value.text = $"{Mathf.RoundToInt(NormalizedValue * 100f)}%";
        }
    }

    public sealed class RarityBadge : MonoBehaviour
    {
        private Image background;
        private Text label;

        public void Build(UiThemeConfig theme, Font font)
        {
            background = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            label = UiRuntimeFactory.CreateText(
                transform, "Label", string.Empty, theme.BodyFontSize, font, theme.TextPrimary);
            UiRuntimeFactory.Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
        }

        public void Bind(GameRarity rarity, UiThemeConfig theme)
        {
            background.color = theme.GetRarityColor(rarity);
            label.text = rarity.ToString();
        }
    }

    public sealed class TierBadge : MonoBehaviour
    {
        private Text label;

        public void Build(UiThemeConfig theme, Font font)
        {
            Image image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.color = theme.ElevatedSurface;
            label = UiRuntimeFactory.CreateText(
                transform, "Label", "T1", theme.BodyFontSize, font, theme.TextPrimary);
            UiRuntimeFactory.Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
        }

        public void Bind(ItemTier tier)
        {
            label.text = $"T{(int)tier}";
        }
    }

    public sealed class ItemCard : MonoBehaviour
    {
        private Text label;

        public void Build(UiThemeConfig theme, Font font)
        {
            Image image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.color = theme.Surface;
            label = UiRuntimeFactory.CreateText(
                transform, "Label", string.Empty, theme.BodyFontSize, font, theme.TextPrimary);
            UiRuntimeFactory.Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleLeft;
        }

        public void Bind(string displayName, string detail)
        {
            label.text = $"{displayName}\n{detail}";
        }
    }

    public sealed class HeroCard : MonoBehaviour
    {
        private Text label;

        public void Build(UiThemeConfig theme, Font font)
        {
            Image image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.color = theme.Surface;
            label = UiRuntimeFactory.CreateText(
                transform, "Label", string.Empty, theme.BodyFontSize, font, theme.TextPrimary);
            UiRuntimeFactory.Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleLeft;
        }

        public void Bind(string displayName, long power, string status)
        {
            label.text = $"{displayName}\nPoder {CurrencyFormatter.Format(power)} • {status}";
        }
    }

    public abstract class MessageStateView : MonoBehaviour
    {
        protected Text Label;

        protected void BuildBase(UiThemeConfig theme, Font font, Color color)
        {
            Label = UiRuntimeFactory.CreateText(
                transform, "Message", string.Empty, theme.BodyFontSize, font, color);
            UiRuntimeFactory.Stretch(Label.rectTransform);
            Label.alignment = TextAnchor.MiddleCenter;
        }

        public void SetMessage(string message)
        {
            Label.text = message ?? string.Empty;
        }
    }

    public sealed class EmptyState : MessageStateView
    {
        public void Build(UiThemeConfig theme, Font font) =>
            BuildBase(theme, font, theme.TextSecondary);
    }

    public sealed class ErrorState : MessageStateView
    {
        public void Build(UiThemeConfig theme, Font font) =>
            BuildBase(theme, font, theme.Error);
    }

    public sealed class LockedFeatureCard : MessageStateView
    {
        public void Build(UiThemeConfig theme, Font font) =>
            BuildBase(theme, font, theme.Locked);
    }

    public sealed class LoadingSpinner : MonoBehaviour
    {
        private UiAccessibilitySettings settings;

        public void Configure(UiAccessibilitySettings accessibilitySettings)
        {
            settings = accessibilitySettings;
        }

        private void Update()
        {
            if (settings == null || settings.ReduceMotion)
                return;
            transform.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime);
        }
    }

    public sealed class TabBar : MonoBehaviour
    {
        public NavigationRoute[] Routes { get; private set; } = Array.Empty<NavigationRoute>();

        public void Configure(params NavigationRoute[] routes)
        {
            Routes = routes ?? Array.Empty<NavigationRoute>();
        }
    }

    public sealed class Modal : MonoBehaviour
    {
        private ModalService service;
        private Text title;
        private Text message;

        public void Build(
            UiThemeConfig theme,
            Font font,
            ModalService modalService,
            IUiTextService texts)
        {
            service = modalService ?? throw new ArgumentNullException(nameof(modalService));
            Image backdrop = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            backdrop.color = theme.Overlay;
            title = UiRuntimeFactory.CreateText(
                transform, "Title", string.Empty, theme.TitleFontSize, font, theme.TextPrimary);
            message = UiRuntimeFactory.CreateText(
                transform, "Message", string.Empty, theme.BodyFontSize, font, theme.TextPrimary);
            title.rectTransform.anchorMin = new Vector2(0.1f, 0.62f);
            title.rectTransform.anchorMax = new Vector2(0.9f, 0.76f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
            title.alignment = TextAnchor.MiddleCenter;
            message.rectTransform.anchorMin = new Vector2(0.12f, 0.32f);
            message.rectTransform.anchorMax = new Vector2(0.88f, 0.62f);
            message.rectTransform.offsetMin = message.rectTransform.offsetMax = Vector2.zero;
            message.alignment = TextAnchor.MiddleCenter;

            PrimaryButton confirm = UiRuntimeFactory.CreateButton<PrimaryButton>(
                transform,
                "Confirm",
                theme,
                font,
                texts.Get("common.confirm"),
                () => service.Confirm());
            RectTransform confirmRect = confirm.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.52f, 0.15f);
            confirmRect.anchorMax = new Vector2(0.88f, 0.25f);
            confirmRect.offsetMin = confirmRect.offsetMax = Vector2.zero;
            SecondaryButton cancel = UiRuntimeFactory.CreateButton<SecondaryButton>(
                transform,
                "Cancel",
                theme,
                font,
                texts.Get("common.cancel"),
                () => service.Close());
            RectTransform cancelRect = cancel.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.12f, 0.15f);
            cancelRect.anchorMax = new Vector2(0.48f, 0.25f);
            cancelRect.offsetMin = cancelRect.offsetMax = Vector2.zero;

            service.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (service != null)
                service.Changed -= Refresh;
        }

        private void Refresh()
        {
            gameObject.SetActive(service.IsOpen);
            if (!service.IsOpen)
                return;
            title.text = service.Current.Title;
            message.text = service.Current.Message;
        }
    }

    public sealed class ConfirmationDialog : MonoBehaviour
    {
        private ModalService service;

        public void Configure(ModalService modalService)
        {
            service = modalService ?? throw new ArgumentNullException(nameof(modalService));
        }

        public bool Show(
            string title,
            string message,
            Action confirm,
            Action cancel = null)
        {
            return service.TryOpen(new ModalRequest(title, message, confirm, cancel, true));
        }
    }

    public sealed class Toast : MonoBehaviour
    {
        private ToastService service;
        private UiThemeConfig theme;
        private Text label;
        private Coroutine hideRoutine;

        public void Build(UiThemeConfig uiTheme, Font font, ToastService toastService)
        {
            theme = uiTheme;
            service = toastService;
            label = UiRuntimeFactory.CreateText(
                transform, "Message", string.Empty, theme.BodyFontSize, font, theme.TextPrimary);
            UiRuntimeFactory.Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
            service.Changed += TryShowNext;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (service != null)
                service.Changed -= TryShowNext;
        }

        private void TryShowNext()
        {
            if (gameObject.activeSelf || !service.TryDequeue(out ToastMessage message))
                return;

            label.text = message.Message;
            Image image = gameObject.GetComponent<Image>();
            image.color = message.Severity == ToastSeverity.Success
                ? theme.Success
                : message.Severity == ToastSeverity.Warning
                    ? theme.Warning
                    : theme.Error;
            gameObject.SetActive(true);
            if (hideRoutine != null)
                StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            gameObject.SetActive(false);
            hideRoutine = null;
            TryShowNext();
        }
    }

    public sealed class LoadingOverlay : MonoBehaviour
    {
        private Text label;
        public bool IsVisible => gameObject.activeSelf;

        public void Build(UiThemeConfig theme, Font font, string message)
        {
            Image image = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.color = theme.Overlay;
            label = UiRuntimeFactory.CreateText(
                transform, "Message", message, theme.TitleFontSize, font, theme.TextPrimary);
            UiRuntimeFactory.Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
            gameObject.SetActive(false);
        }

        public void Show(string message)
        {
            label.text = message ?? string.Empty;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }

    internal static class UiRuntimeFactory
    {
        public static GameObject CreateObject(Transform parent, string name, params Type[] components)
        {
            var value = new GameObject(name, components);
            value.transform.SetParent(parent, false);
            return value;
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject value = CreateObject(parent, name, typeof(RectTransform), typeof(Image));
            Image image = value.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string value,
            int size,
            Font font,
            Color color)
        {
            GameObject textObject = CreateObject(parent, name, typeof(RectTransform), typeof(Text));
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value ?? string.Empty;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        public static T CreateButton<T>(
            Transform parent,
            string name,
            UiThemeConfig theme,
            Font font,
            string label,
            UnityAction action,
            NavigationRoute? targetRoute = null)
            where T : ThemedButton
        {
            GameObject buttonObject = CreateObject(
                parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
            T button = buttonObject.AddComponent<T>();
            button.Build(theme, font, label, action, targetRoute);
            return button;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
