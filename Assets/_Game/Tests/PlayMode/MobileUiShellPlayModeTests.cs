using System.Collections;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Presentation.MobileUI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdleMedievalLegends.Tests.PlayMode
{
    public sealed class MobileUiShellPlayModeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_Ready_OpensHomeWithSafeArea()
        {
            yield return LoadBootstrapAndWaitForShell();
            AppNavigationController navigation = AppNavigationController.Instance;

            Assert.That(navigation, Is.Not.Null);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(NavigationRoute.Home));
            Assert.That(navigation.GetScreenView(NavigationRoute.Home).IsVisible, Is.True);
            Assert.That(
                UnityEngine.Object.FindAnyObjectByType<SafeAreaController>(),
                Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Shell_ShellRoutesBackAndModal_Work()
        {
            yield return LoadBootstrapAndWaitForShell();
            AppNavigationController navigation = AppNavigationController.Instance;

            Assert.That(navigation.Navigate(NavigationRoute.Heroes), Is.True);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(NavigationRoute.Heroes));
            Assert.That(navigation.HandleBack(), Is.True);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(NavigationRoute.Home));

            bool confirmed = false;
            Assert.That(
                navigation.ShowConfirmation("Confirmar", "Continuar?", () => confirmed = true),
                Is.True);
            Assert.That(navigation.Modals.IsOpen, Is.True);
            Assert.That(navigation.Modals.Confirm(), Is.True);
            Assert.That(confirmed, Is.True);
            Assert.That(navigation.Modals.IsOpen, Is.False);

            Assert.That(
                navigation.ShowConfirmation("Fechar", "Cancelar este modal?", () => { }),
                Is.True);
            Assert.That(navigation.HandleBack(), Is.True);
            Assert.That(navigation.Modals.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator Shell_MarketRoute_ShowsLockedState()
        {
            yield return LoadBootstrapAndWaitForShell();
            AppNavigationController navigation = AppNavigationController.Instance;

            Assert.That(navigation.Navigate(NavigationRoute.Market), Is.True);
            ScreenView market = navigation.GetScreenView(NavigationRoute.Market);

            Assert.That(market, Is.Not.Null);
            Assert.That(market.IsVisible, Is.True);
            Assert.That(market.Presenter.State, Is.EqualTo(ScreenState.Locked));
        }

        [UnityTest]
        public IEnumerator Shell_ActiveBattleRoute_ReusesExistingScene()
        {
            yield return LoadBootstrapAndWaitForShell();
            AppNavigationController navigation = AppNavigationController.Instance;
            Scene battleScene = SceneManager.GetActiveScene();
            GameObject battlePresentation = GameObject.Find("BattlePresentation");
            Assert.That(battlePresentation, Is.Not.Null);

            Assert.That(navigation.Navigate(NavigationRoute.Battle), Is.True);
            yield return null;

            Assert.That(navigation.IsNavigating, Is.False);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(NavigationRoute.Battle));
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(battleScene.handle));
            Assert.That(
                GameObject.Find("BattlePresentation"),
                Is.SameAs(battlePresentation));
        }

        [UnityTest]
        public IEnumerator Shell_UnavailableSceneRoute_PreservesHistoryAndVisibleScreen()
        {
            yield return LoadBootstrapAndWaitForShell();
            AppNavigationController navigation = AppNavigationController.Instance;
            Assert.That(navigation.Navigate(NavigationRoute.Heroes), Is.True);
            int historyCount = navigation.History.Count;
            navigation.SetSceneAvailabilityForTests(_ => false);

            bool navigated = navigation.Navigate(NavigationRoute.Inventory);

            Assert.That(navigated, Is.False);
            Assert.That(navigation.IsNavigating, Is.False);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(NavigationRoute.Heroes));
            Assert.That(navigation.History.Count, Is.EqualTo(historyCount));
            Assert.That(navigation.GetScreenView(NavigationRoute.Heroes).IsVisible, Is.True);
            navigation.SetSceneAvailabilityForTests(
                UnityEngine.Application.CanStreamedLevelBeLoaded);
        }

        [UnityTest]
        public IEnumerator SafeArea_NotchAndAspectProfiles_ClampAnchors()
        {
            var rootObject = new GameObject("SafeAreaTest", typeof(RectTransform));
            var contentObject = new GameObject("ResponsiveContent", typeof(RectTransform));
            contentObject.transform.SetParent(rootObject.transform, false);
            SafeAreaController safeArea = rootObject.AddComponent<SafeAreaController>();
            RectTransform rect = rootObject.GetComponent<RectTransform>();
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            ResponsiveLayoutController responsive =
                rootObject.AddComponent<ResponsiveLayoutController>();
            safeArea.Configure(rect);
            responsive.Configure(contentRect);

            var profiles = new[]
            {
                new { Screen = new Vector2(1080, 1920), Safe = new Rect(0, 80, 1080, 1760) },
                new { Screen = new Vector2(1080, 1920), Safe = new Rect(0, 0, 1080, 1920) },
                new { Screen = new Vector2(1080, 2340), Safe = new Rect(0, 90, 1080, 2160) },
                new { Screen = new Vector2(1080, 2400), Safe = new Rect(0, 96, 1080, 2208) },
                new { Screen = new Vector2(1600, 2560), Safe = new Rect(0, 80, 1600, 2400) }
            };

            foreach (var profile in profiles)
            {
                safeArea.Apply(profile.Safe, profile.Screen);
                Assert.That(rect.anchorMin.x, Is.InRange(0f, 1f));
                Assert.That(rect.anchorMin.y, Is.InRange(0f, 1f));
                Assert.That(rect.anchorMax.x, Is.InRange(0f, 1f));
                Assert.That(rect.anchorMax.y, Is.InRange(0f, 1f));
                Assert.That(rect.anchorMax.x, Is.GreaterThan(rect.anchorMin.x));
                Assert.That(rect.anchorMax.y, Is.GreaterThan(rect.anchorMin.y));
                responsive.Apply(profile.Screen);
            }

            responsive.Apply(new Vector2(1080, 2400));
            Assert.That(contentRect.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
            responsive.Apply(new Vector2(1600, 2000));
            Assert.That(contentRect.anchorMin.x, Is.EqualTo(0.12f).Within(0.001f));

            UnityEngine.Object.Destroy(rootObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Shell_AllRoutes_AreReachableWithoutUnexpectedExceptions()
        {
            yield return LoadBootstrapAndWaitForShell();
            AppNavigationController navigation = AppNavigationController.Instance;

            NavigationRoute[] routes =
            {
                NavigationRoute.Home,
                NavigationRoute.Battle,
                NavigationRoute.Heroes,
                NavigationRoute.Inventory,
                NavigationRoute.Crafting,
                NavigationRoute.Dungeons,
                NavigationRoute.Gacha,
                NavigationRoute.Market,
                NavigationRoute.Profile,
                NavigationRoute.Settings,
                NavigationRoute.Campaign,
                NavigationRoute.More
            };

            for (int i = 0; i < routes.Length; i++)
            {
                Assert.That(navigation.Navigate(routes[i]), Is.True, routes[i].ToString());
                float timeout = Time.realtimeSinceStartup + 10f;
                while (navigation.IsNavigating && Time.realtimeSinceStartup < timeout)
                    yield return null;
                Assert.That(navigation.IsNavigating, Is.False, routes[i].ToString());
                Assert.That(navigation.CurrentRoute, Is.EqualTo(routes[i]));
            }
        }

        private static IEnumerator LoadBootstrapAndWaitForShell()
        {
            if (AppNavigationController.Instance != null)
            {
                UnityEngine.Object.Destroy(
                    AppNavigationController.Instance.transform.root.gameObject);
                yield return null;
            }

            AsyncOperation load = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            while (!load.isDone)
                yield return null;

            float timeout = Time.realtimeSinceStartup + 10f;
            while ((AppNavigationController.Instance == null ||
                    !AppNavigationController.Instance.IsBuilt ||
                    AppNavigationController.Instance.CurrentRoute != NavigationRoute.Home ||
                    GameManager.Instance == null ||
                    !GameManager.Instance.IsReady ||
                    SceneManager.GetActiveScene().name != "Battle") &&
                   Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(AppNavigationController.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance.IsReady, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Battle"));
            Assert.That(
                AppNavigationController.Instance.CurrentRoute,
                Is.EqualTo(NavigationRoute.Home));
        }
    }
}
