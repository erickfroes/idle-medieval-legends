using System;
using IdleMedievalLegends.Application;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleMedievalLegends.Presentation.Battle
{
    [DefaultExecutionOrder(-900)]
    public sealed class BootstrapBattleLoader : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private string battleSceneName = "Battle";
        [SerializeField] private bool loadWhenReady = true;

        private bool transitionStarted;

        public string BattleSceneName => battleSceneName;
        public bool LoadWhenReady => loadWhenReady;
        public GameManager GameManager => gameManager;

        public void Configure(
            GameManager manager,
            string sceneName = "Battle",
            bool autoLoad = true)
        {
            gameManager = manager != null
                ? manager
                : throw new ArgumentNullException(nameof(manager));
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("Nome da cena é obrigatório.", nameof(sceneName));

            battleSceneName = sceneName;
            loadWhenReady = autoLoad;
        }

        private void OnEnable()
        {
            if (gameManager == null)
                gameManager = GetComponent<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError(
                    "BootstrapBattleLoader requer GameManager no mesmo objeto.",
                    this);
                return;
            }

            gameManager.StateChanged += HandleStateChanged;
            if (gameManager.IsReady)
                TryLoadBattle();
        }

        private void OnDisable()
        {
            if (gameManager != null)
                gameManager.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameLifecycleState state)
        {
            if (state == GameLifecycleState.Ready)
                TryLoadBattle();
        }

        private void TryLoadBattle()
        {
            if (!loadWhenReady || transitionStarted ||
                SceneManager.GetActiveScene().name == battleSceneName)
            {
                return;
            }
            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(battleSceneName))
            {
                Debug.LogError(
                    $"Cena {battleSceneName} não está disponível nos Build Settings.",
                    this);
                return;
            }

            transitionStarted = true;
            SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Single);
        }
    }
}
