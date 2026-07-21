using System;
using UnityEngine;

namespace IdleMedievalLegends.Application
{
    public static class BootstrapDiagnosticsFormatter
    {
        public static string Format(
            GameLifecycleState state,
            string playerId,
            long inventoryRevision,
            long professionRevision)
        {
            string displayedPlayerId = string.IsNullOrWhiteSpace(playerId)
                ? "<local-unassigned>"
                : playerId;

            return
                $"[BootstrapDiagnostics] state={state}; " +
                $"playerId={displayedPlayerId}; " +
                $"inventoryRevision={inventoryRevision}; " +
                $"professionRevision={professionRevision}";
        }
    }

    /// <summary>
    /// Apresentação temporária do bootstrap. Observa o GameManager e registra
    /// somente dados de diagnóstico; não altera estado de jogo.
    /// </summary>
    public sealed class BootstrapDiagnostics : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        public void Configure(GameManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            gameManager = manager;
        }

        private void OnEnable()
        {
            if (gameManager == null)
                gameManager = GetComponent<GameManager>();

            if (gameManager == null)
            {
                Debug.LogError(
                    "BootstrapDiagnostics requer um GameManager no mesmo objeto.",
                    this);
                return;
            }

            gameManager.StateChanged += HandleStateChanged;

            if (gameManager.IsReady)
                LogCurrentState();
        }

        private void OnDisable()
        {
            if (gameManager != null)
                gameManager.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameLifecycleState state)
        {
            if (state == GameLifecycleState.Ready)
                LogCurrentState();
        }

        private void LogCurrentState()
        {
            Debug.Log(
                BootstrapDiagnosticsFormatter.Format(
                    gameManager.State,
                    gameManager.CurrentPlayerId,
                    gameManager.Inventory.ServerRevision,
                    gameManager.Professions.ServerRevision),
                this);
        }
    }
}
