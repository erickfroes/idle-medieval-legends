using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace IdleMedievalLegends.Presentation.MobileUI
{
    public enum NavigationRoute
    {
        Home = 0,
        Battle = 1,
        Heroes = 2,
        Inventory = 3,
        Crafting = 4,
        Dungeons = 5,
        Gacha = 6,
        Market = 7,
        Profile = 8,
        Settings = 9,
        Campaign = 10,
        More = 11
    }

    public enum ScreenState
    {
        Loading = 0,
        Ready = 1,
        Empty = 2,
        Error = 3,
        Locked = 4
    }

    public sealed class NavigationDestination
    {
        public NavigationDestination(
            NavigationRoute route,
            string textKey,
            string sceneName = "",
            bool locked = false)
        {
            if (string.IsNullOrWhiteSpace(textKey))
                throw new ArgumentException("A chave de texto da rota é obrigatória.", nameof(textKey));

            Route = route;
            TextKey = textKey;
            SceneName = sceneName ?? string.Empty;
            Locked = locked;
        }

        public NavigationRoute Route { get; }
        public string TextKey { get; }
        public string SceneName { get; }
        public bool Locked { get; }
        public bool UsesScene => !string.IsNullOrWhiteSpace(SceneName);
    }

    public static class NavigationRouteRegistry
    {
        private static readonly IReadOnlyList<NavigationDestination> destinations =
            new ReadOnlyCollection<NavigationDestination>(
                new List<NavigationDestination>
                {
                    new NavigationDestination(NavigationRoute.Home, "route.home"),
                    new NavigationDestination(NavigationRoute.Battle, "route.battle", "Battle"),
                    new NavigationDestination(NavigationRoute.Heroes, "route.heroes"),
                    new NavigationDestination(NavigationRoute.Inventory, "route.inventory", "Inventory"),
                    new NavigationDestination(NavigationRoute.Crafting, "route.crafting", "Crafting"),
                    new NavigationDestination(NavigationRoute.Dungeons, "route.dungeons", "Dungeon"),
                    new NavigationDestination(NavigationRoute.Gacha, "route.gacha"),
                    new NavigationDestination(NavigationRoute.Market, "route.market", locked: true),
                    new NavigationDestination(NavigationRoute.Profile, "route.profile"),
                    new NavigationDestination(NavigationRoute.Settings, "route.settings"),
                    new NavigationDestination(NavigationRoute.Campaign, "route.campaign", "Campaign"),
                    new NavigationDestination(NavigationRoute.More, "route.more")
                });

        public static IReadOnlyList<NavigationDestination> Destinations => destinations;

        public static NavigationDestination Get(NavigationRoute route)
        {
            for (int i = 0; i < destinations.Count; i++)
            {
                if (destinations[i].Route == route)
                    return destinations[i];
            }

            throw new ArgumentOutOfRangeException(nameof(route), route, "Rota não registrada.");
        }

        public static bool TryGetByScene(string sceneName, out NavigationDestination destination)
        {
            for (int i = 0; i < destinations.Count; i++)
            {
                if (string.Equals(
                    destinations[i].SceneName,
                    sceneName,
                    StringComparison.Ordinal))
                {
                    destination = destinations[i];
                    return true;
                }
            }

            destination = null;
            return false;
        }
    }

    public sealed class NavigationButtonDefinition
    {
        public NavigationButtonDefinition(
            string buttonId,
            string textKey,
            NavigationRoute? targetRoute)
        {
            ButtonId = buttonId ?? string.Empty;
            TextKey = textKey ?? string.Empty;
            TargetRoute = targetRoute;
        }

        public string ButtonId { get; }
        public string TextKey { get; }
        public NavigationRoute? TargetRoute { get; }
    }

    public static class MobileNavigationLayout
    {
        private static readonly IReadOnlyList<NavigationButtonDefinition> mainButtons =
            new ReadOnlyCollection<NavigationButtonDefinition>(
                new List<NavigationButtonDefinition>
                {
                    Button("home_tab", NavigationRoute.Home),
                    Button("battle_tab", NavigationRoute.Battle),
                    Button("heroes_tab", NavigationRoute.Heroes),
                    Button("crafting_tab", NavigationRoute.Crafting),
                    Button("more_tab", NavigationRoute.More)
                });

        private static readonly IReadOnlyList<NavigationButtonDefinition> moreButtons =
            new ReadOnlyCollection<NavigationButtonDefinition>(
                new List<NavigationButtonDefinition>
                {
                    Button("inventory_more", NavigationRoute.Inventory),
                    Button("campaign_more", NavigationRoute.Campaign),
                    Button("dungeons_more", NavigationRoute.Dungeons),
                    Button("gacha_more", NavigationRoute.Gacha),
                    Button("market_more", NavigationRoute.Market),
                    Button("profile_more", NavigationRoute.Profile),
                    Button("settings_more", NavigationRoute.Settings)
                });

        public static IReadOnlyList<NavigationButtonDefinition> MainButtons => mainButtons;
        public static IReadOnlyList<NavigationButtonDefinition> MoreButtons => moreButtons;

        private static NavigationButtonDefinition Button(
            string buttonId,
            NavigationRoute route)
        {
            return new NavigationButtonDefinition(
                buttonId,
                NavigationRouteRegistry.Get(route).TextKey,
                route);
        }
    }

    public sealed class NavigationHistory
    {
        private readonly List<NavigationRoute> entries = new List<NavigationRoute>();

        public NavigationHistory(NavigationRoute initialRoute)
        {
            ValidateRoute(initialRoute);
            entries.Add(initialRoute);
        }

        public NavigationRoute Current => entries[entries.Count - 1];
        public int Count => entries.Count;
        public bool CanGoBack => entries.Count > 1;
        public IReadOnlyList<NavigationRoute> Entries =>
            new ReadOnlyCollection<NavigationRoute>(entries);

        public void Push(NavigationRoute route)
        {
            ValidateRoute(route);
            if (Current != route)
                entries.Add(route);
        }

        public bool TryBack(out NavigationRoute route)
        {
            if (!CanGoBack)
            {
                route = Current;
                return false;
            }

            entries.RemoveAt(entries.Count - 1);
            route = Current;
            return true;
        }

        public bool TryPeekBack(out NavigationRoute route)
        {
            if (!CanGoBack)
            {
                route = Current;
                return false;
            }

            route = entries[entries.Count - 2];
            return true;
        }

        public void Reset(NavigationRoute route)
        {
            ValidateRoute(route);
            entries.Clear();
            entries.Add(route);
        }

        public void Restore(IReadOnlyList<NavigationRoute> snapshot)
        {
            if (snapshot == null || snapshot.Count == 0)
                throw new ArgumentException("O snapshot do histórico é obrigatório.", nameof(snapshot));

            for (int i = 0; i < snapshot.Count; i++)
                ValidateRoute(snapshot[i]);

            entries.Clear();
            for (int i = 0; i < snapshot.Count; i++)
                entries.Add(snapshot[i]);
        }

        private static void ValidateRoute(NavigationRoute route)
        {
            _ = NavigationRouteRegistry.Get(route);
        }
    }

    public abstract class ScreenPresenter
    {
        protected ScreenPresenter(NavigationRoute route)
        {
            Route = route;
        }

        public NavigationRoute Route { get; }
        public ScreenState State { get; private set; } = ScreenState.Loading;
        public string ErrorMessage { get; private set; } = string.Empty;

        public event Action Changed;

        public virtual void Activate()
        {
            SetState(ScreenState.Ready);
        }

        public virtual void Deactivate()
        {
        }

        protected void SetState(ScreenState state, string errorMessage = "")
        {
            State = state;
            ErrorMessage = errorMessage ?? string.Empty;
            Changed?.Invoke();
        }
    }

    public sealed class StaticScreenPresenter : ScreenPresenter
    {
        private readonly ScreenState desiredState;

        public StaticScreenPresenter(NavigationRoute route, ScreenState state = ScreenState.Ready)
            : base(route)
        {
            desiredState = state;
        }

        public override void Activate()
        {
            SetState(desiredState);
        }
    }

    public sealed class ScreenView : MonoBehaviour
    {
        private ScreenPresenter presenter;

        public NavigationRoute Route { get; private set; }
        public ScreenPresenter Presenter => presenter;
        public bool IsVisible => gameObject.activeSelf;

        public void Configure(NavigationRoute route, ScreenPresenter screenPresenter)
        {
            presenter = screenPresenter ??
                throw new ArgumentNullException(nameof(screenPresenter));
            if (presenter.Route != route)
                throw new InvalidOperationException("Presenter e view devem usar a mesma rota.");

            Route = route;
            presenter.Changed += ApplyState;
            ApplyState();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            presenter.Activate();
            ApplyState();
        }

        public void Hide()
        {
            presenter?.Deactivate();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (presenter != null)
                presenter.Changed -= ApplyState;
        }

        private void ApplyState()
        {
            gameObject.name = $"{Route}Screen_{presenter.State}";
        }
    }

    public sealed class AsyncActionGate
    {
        public bool IsBusy { get; private set; }

        public bool TryBegin()
        {
            if (IsBusy)
                return false;

            IsBusy = true;
            return true;
        }

        public void End()
        {
            IsBusy = false;
        }
    }
}
