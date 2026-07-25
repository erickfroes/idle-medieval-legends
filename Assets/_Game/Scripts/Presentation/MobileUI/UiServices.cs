using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IdleMedievalLegends.Presentation.MobileUI
{
    public enum ToastSeverity
    {
        Success = 0,
        Warning = 1,
        Error = 2
    }

    public sealed class ToastMessage
    {
        public ToastMessage(string message, ToastSeverity severity)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A mensagem do toast é obrigatória.", nameof(message));

            Message = message;
            Severity = severity;
        }

        public string Message { get; }
        public ToastSeverity Severity { get; }
    }

    public sealed class ToastService
    {
        private readonly Queue<ToastMessage> queue = new Queue<ToastMessage>();

        public int PendingCount => queue.Count;
        public event Action Changed;

        public void ShowSuccess(string message) => Enqueue(message, ToastSeverity.Success);
        public void ShowWarning(string message) => Enqueue(message, ToastSeverity.Warning);
        public void ShowError(string message) => Enqueue(message, ToastSeverity.Error);

        public bool TryDequeue(out ToastMessage message)
        {
            if (queue.Count == 0)
            {
                message = null;
                return false;
            }

            message = queue.Dequeue();
            return true;
        }

        private void Enqueue(string message, ToastSeverity severity)
        {
            queue.Enqueue(new ToastMessage(message, severity));
            Changed?.Invoke();
        }
    }

    public sealed class ModalRequest
    {
        public ModalRequest(
            string title,
            string message,
            Action confirm = null,
            Action cancel = null,
            bool destructive = false)
        {
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            Confirm = confirm;
            Cancel = cancel;
            Destructive = destructive;
        }

        public string Title { get; }
        public string Message { get; }
        public Action Confirm { get; }
        public Action Cancel { get; }
        public bool Destructive { get; }
    }

    public sealed class ModalService
    {
        public bool IsOpen => Current != null;
        public ModalRequest Current { get; private set; }
        public event Action Changed;

        public bool TryOpen(ModalRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (IsOpen)
                return false;

            Current = request;
            Changed?.Invoke();
            return true;
        }

        public bool Confirm()
        {
            if (!IsOpen)
                return false;

            ModalRequest request = Current;
            Current = null;
            Changed?.Invoke();
            request.Confirm?.Invoke();
            return true;
        }

        public bool Close()
        {
            if (!IsOpen)
                return false;

            ModalRequest request = Current;
            Current = null;
            Changed?.Invoke();
            request.Cancel?.Invoke();
            return true;
        }
    }

    public sealed class SafeAreaController : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;

        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        public Rect LastAppliedSafeArea => lastSafeArea;

        public void Configure(RectTransform root)
        {
            safeAreaRoot = root ?? throw new ArgumentNullException(nameof(root));
            Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        }

        private void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (Screen.safeArea != lastSafeArea || size != lastScreenSize)
                Apply(Screen.safeArea, new Vector2(size.x, size.y));
        }

        public void Apply(Rect safeArea, Vector2 screenSize)
        {
            if (safeAreaRoot == null)
                throw new InvalidOperationException("SafeAreaRoot não configurado.");
            if (screenSize.x <= 0f || screenSize.y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(screenSize));

            Vector2 minimum = safeArea.position;
            Vector2 maximum = safeArea.position + safeArea.size;
            minimum.x = Mathf.Clamp01(minimum.x / screenSize.x);
            minimum.y = Mathf.Clamp01(minimum.y / screenSize.y);
            maximum.x = Mathf.Clamp01(maximum.x / screenSize.x);
            maximum.y = Mathf.Clamp01(maximum.y / screenSize.y);
            if (maximum.x < minimum.x || maximum.y < minimum.y)
                throw new ArgumentException("Safe area inválida.", nameof(safeArea));

            safeAreaRoot.anchorMin = minimum;
            safeAreaRoot.anchorMax = maximum;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(
                Mathf.RoundToInt(screenSize.x),
                Mathf.RoundToInt(screenSize.y));
        }
    }

    public sealed class ResponsiveLayoutController : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;

        private Vector2Int lastScreenSize;

        public void Configure(RectTransform target)
        {
            contentRoot = target ?? throw new ArgumentNullException(nameof(target));
            Apply(new Vector2(Screen.width, Screen.height));
        }

        private void Update()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size != lastScreenSize)
                Apply(new Vector2(size.x, size.y));
        }

        public void Apply(Vector2 screenSize)
        {
            if (contentRoot == null)
                throw new InvalidOperationException("ContentRoot não configurado.");
            if (screenSize.x <= 0f || screenSize.y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(screenSize));

            float aspect = screenSize.x / screenSize.y;
            float horizontalMargin = aspect >= 0.75f ? 0.12f : 0f;
            contentRoot.anchorMin = new Vector2(horizontalMargin, 0.075f);
            contentRoot.anchorMax = new Vector2(1f - horizontalMargin, 0.94f);
            contentRoot.offsetMin = contentRoot.offsetMax = Vector2.zero;
            lastScreenSize = new Vector2Int(
                Mathf.RoundToInt(screenSize.x),
                Mathf.RoundToInt(screenSize.y));
        }
    }

    public sealed class MobileInputController : MonoBehaviour
    {
        private AppNavigationController navigation;

        public void Configure(AppNavigationController controller)
        {
            navigation = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        private void Update()
        {
            if (navigation != null &&
                Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                navigation.HandleBack();
            }
        }
    }

    public sealed class UiSoundHooks : MonoBehaviour
    {
        private UiAccessibilitySettings settings;

        public void Configure(UiAccessibilitySettings accessibilitySettings)
        {
            settings = accessibilitySettings ??
                throw new ArgumentNullException(nameof(accessibilitySettings));
        }

        public void NotifyInteraction()
        {
            if (settings == null || !settings.EffectsEnabled)
                return;

            // Audio clips are intentionally deferred until authored assets exist.
        }

        public void NotifyDestructiveConfirmation()
        {
            NotifyInteraction();
#if UNITY_ANDROID || UNITY_IOS
            if (settings != null && settings.VibrationEnabled)
                Handheld.Vibrate();
#endif
        }
    }
}
