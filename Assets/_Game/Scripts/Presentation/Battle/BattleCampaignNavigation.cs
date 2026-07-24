using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.Battle
{
    public sealed class BattleCampaignNavigation : MonoBehaviour
    {
        private void Start()
        {
            if (!UnityEngine.Application.CanStreamedLevelBeLoaded("Campaign")) return;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("CampaignNavigationCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var buttonObject = new GameObject("OpenCampaignButton", typeof(RectTransform),
                typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(220, 52);
            rect.anchoredPosition = new Vector2(-24, -148);
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.26f, 0.38f, 0.95f);
            buttonObject.GetComponent<Button>().onClick.AddListener(OpenCampaign);
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "Campanha";
        }

        public void OpenCampaign()
        {
            if (UnityEngine.Application.CanStreamedLevelBeLoaded("Campaign"))
                SceneManager.LoadSceneAsync("Campaign", LoadSceneMode.Single);
        }
    }
}
