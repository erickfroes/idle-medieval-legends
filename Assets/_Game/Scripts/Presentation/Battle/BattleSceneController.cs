using System;
using IdleMedievalLegends.Domain.Combat;
using UnityEngine;

namespace IdleMedievalLegends.Presentation.Battle
{
    public sealed class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private BattleDebugScenarioProvider scenarioProvider;
        [SerializeField] private BattleEventPlayer eventPlayer;
        [SerializeField] private BattleTeamView playerTeamView;
        [SerializeField] private BattleTeamView enemyTeamView;
        [SerializeField] private BattleHudView hudView;
        [SerializeField] private bool playOnStart = true;

        private BattlePresenter presenter;
        private BattleDebugScenario scenario;

        public BattlePresentationState State => presenter?.State ??
            BattlePresentationState.Uninitialized;
        public BattleResult Result => scenario?.Result;
        public BattleDebugScenarioProvider ScenarioProvider => scenarioProvider;
        public BattleEventPlayer EventPlayer => eventPlayer;
        public BattleHudView HudView => hudView;
        public BattleTeamView PlayerTeamView => playerTeamView;
        public BattleTeamView EnemyTeamView => enemyTeamView;
        public int PlaybackSpeed => presenter?.SpeedController.Speed ?? 1;
        public bool IsConfigured => scenarioProvider != null && eventPlayer != null &&
            playerTeamView != null && enemyTeamView != null && hudView != null;

        public void Configure(
            BattleDebugScenarioProvider provider,
            BattleEventPlayer player,
            BattleTeamView playerTeam,
            BattleTeamView enemyTeam,
            BattleHudView hud,
            bool autoPlay = true)
        {
            scenarioProvider = provider != null
                ? provider
                : throw new ArgumentNullException(nameof(provider));
            eventPlayer = player != null
                ? player
                : throw new ArgumentNullException(nameof(player));
            playerTeamView = playerTeam != null
                ? playerTeam
                : throw new ArgumentNullException(nameof(playerTeam));
            enemyTeamView = enemyTeam != null
                ? enemyTeam
                : throw new ArgumentNullException(nameof(enemyTeam));
            hudView = hud != null ? hud : throw new ArgumentNullException(nameof(hud));
            playOnStart = autoPlay;
        }

        private void Awake()
        {
            presenter = new BattlePresenter();
            presenter.SpeedController.SpeedChanged += HandleSpeedChanged;
        }

        private void Start()
        {
            if (GetComponent<BattleInventoryNavigation>() == null)
                gameObject.AddComponent<BattleInventoryNavigation>();
            if (GetComponent<BattleCraftingNavigation>() == null)
                gameObject.AddComponent<BattleCraftingNavigation>();
            if (GetComponent<BattleCampaignNavigation>() == null)
                gameObject.AddComponent<BattleCampaignNavigation>();
            if (playOnStart)
                BeginScenario();
        }

        public void BeginScenario()
        {
            if (presenter == null)
            {
                presenter = new BattlePresenter();
                presenter.SpeedController.SpeedChanged += HandleSpeedChanged;
            }
            if (presenter.State != BattlePresentationState.Uninitialized)
                return;

            try
            {
                ValidateReferences();
                scenario = scenarioProvider.CreateScenario();
                playerTeamView.Bind(scenario.Request.Attacker, scenario.Catalog);
                enemyTeamView.Bind(scenario.Request.Defender, scenario.Catalog);
                presenter.Prepare(scenario.Result);
                hudView.Bind(
                    scenario.Result.Seed,
                    presenter.SpeedController,
                    CycleSpeed,
                    SkipBattle);
                eventPlayer.Begin(presenter);
            }
            catch (Exception exception)
            {
                presenter.Fault();
                hudView?.SetStatus("Falha ao preparar batalha");
                Debug.LogException(exception, this);
            }
        }

        public void CycleSpeed()
        {
            if (presenter == null ||
                presenter.State == BattlePresentationState.Completed ||
                presenter.State == BattlePresentationState.Faulted)
            {
                return;
            }

            presenter.SpeedController.Cycle();
        }

        public void SetSpeed(int speed)
        {
            if (presenter == null)
                throw new InvalidOperationException("Presenter ainda não inicializado.");
            presenter.SpeedController.Set(speed);
        }

        public void SkipBattle()
        {
            eventPlayer?.Skip();
        }

        public BattleUnitView FindUnitView(string unitId)
        {
            return playerTeamView?.FindUnit(unitId) ?? enemyTeamView?.FindUnit(unitId);
        }

        private void HandleSpeedChanged(int speed)
        {
            hudView?.SetSpeed(speed);
        }

        private void ValidateReferences()
        {
            if (scenarioProvider == null || eventPlayer == null ||
                playerTeamView == null || enemyTeamView == null || hudView == null)
            {
                throw new InvalidOperationException(
                    "BattleSceneController possui referências de composição ausentes.");
            }
        }

        private void OnDestroy()
        {
            if (eventPlayer != null)
                eventPlayer.CancelPlayback();
            if (presenter != null)
                presenter.SpeedController.SpeedChanged -= HandleSpeedChanged;
        }
    }
}
