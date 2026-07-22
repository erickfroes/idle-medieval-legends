using System;
using IdleMedievalLegends.Domain.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.Battle
{
    public sealed class BattleHudView : MonoBehaviour
    {
        [SerializeField] private Text playerTeamLabel;
        [SerializeField] private Text enemyTeamLabel;
        [SerializeField] private Text seedLabel;
        [SerializeField] private Text speedLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text resultLabel;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button skipButton;

        public Button SpeedButton => speedButton;
        public Button SkipButton => skipButton;
        public string DisplayedResult => resultLabel != null ? resultLabel.text : string.Empty;
        public bool IsConfigured => playerTeamLabel != null && enemyTeamLabel != null &&
            seedLabel != null && speedLabel != null && statusLabel != null &&
            resultLabel != null && speedButton != null && skipButton != null;

        public void Configure(
            Text playerLabel,
            Text enemyLabel,
            Text seedText,
            Text speedText,
            Text statusText,
            Text resultText,
            Button speedControl,
            Button skipControl)
        {
            playerTeamLabel = playerLabel;
            enemyTeamLabel = enemyLabel;
            seedLabel = seedText;
            speedLabel = speedText;
            statusLabel = statusText;
            resultLabel = resultText;
            speedButton = speedControl;
            skipButton = skipControl;
        }

        public void Bind(
            long seed,
            BattleSpeedController speedController,
            Action cycleSpeed,
            Action skip)
        {
            if (speedController == null)
                throw new ArgumentNullException(nameof(speedController));
            if (cycleSpeed == null) throw new ArgumentNullException(nameof(cycleSpeed));
            if (skip == null) throw new ArgumentNullException(nameof(skip));
            if (speedButton == null || skipButton == null)
                throw new InvalidOperationException("HUD requer os botões de velocidade e pular.");

            playerTeamLabel.text = "HERÓIS";
            enemyTeamLabel.text = "INIMIGOS DEBUG";
            seedLabel.text = $"Seed {seed}";
            resultLabel.text = string.Empty;
            statusLabel.text = "Preparando...";
            SetSpeed(speedController.Speed);

            speedButton.onClick.RemoveAllListeners();
            skipButton.onClick.RemoveAllListeners();
            speedButton.onClick.AddListener(() => cycleSpeed());
            skipButton.onClick.AddListener(() => skip());
        }

        public void SetSpeed(int speed)
        {
            if (speedLabel != null)
                speedLabel.text = $"{speed}x";
        }

        public void SetStatus(string status)
        {
            if (statusLabel != null)
                statusLabel.text = status ?? string.Empty;
        }

        public void ShowResult(BattleResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            string winner = result.WinningTeam == BattleSide.Attacker
                ? "Vitória"
                : result.WinningTeam == BattleSide.Defender
                    ? "Derrota"
                    : "Empate";
            resultLabel.text = winner;
            statusLabel.text =
                $"{result.TurnCount} turnos • {result.ActionCount} ações";
            speedButton.interactable = false;
            skipButton.interactable = false;
        }

        private void OnDisable()
        {
            speedButton?.onClick.RemoveAllListeners();
            skipButton?.onClick.RemoveAllListeners();
        }
    }
}
