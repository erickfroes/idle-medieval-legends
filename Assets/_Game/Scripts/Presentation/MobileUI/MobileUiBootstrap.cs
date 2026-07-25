using UnityEngine;

namespace IdleMedievalLegends.Presentation.MobileUI
{
    [DisallowMultipleComponent]
    public sealed class MobileUiBootstrap : MonoBehaviour
    {
        [SerializeField] private UiThemeConfig theme;

        public UiThemeConfig Theme => theme;

        public void Configure(UiThemeConfig uiTheme)
        {
            theme = uiTheme;
        }

        private void Awake()
        {
            AppNavigationController existing =
                GetComponentInChildren<AppNavigationController>(true);
            if (existing != null)
            {
                existing.Configure(theme);
                return;
            }

            var navigationObject = new GameObject("MobileUiShell");
            navigationObject.SetActive(false);
            navigationObject.transform.SetParent(transform, false);
            AppNavigationController navigation =
                navigationObject.AddComponent<AppNavigationController>();
            navigation.Configure(theme);
            navigationObject.SetActive(true);
        }
    }
}
