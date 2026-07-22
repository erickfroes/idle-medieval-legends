using System.Collections;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Presentation.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdleMedievalLegends.Tests.PlayMode
{
    public sealed class BootstrapSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapScene_PlayMode_ReachesReadyWithoutExceptions()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                "Bootstrap",
                LoadSceneMode.Single);
            Assert.That(loadOperation, Is.Not.Null);

            while (!loadOperation.isDone)
                yield return null;

            float deadline = Time.realtimeSinceStartup + 10f;
            while ((GameManager.Instance == null || !GameManager.Instance.IsReady) &&
                   Time.realtimeSinceStartup < deadline)
            {
                if (GameManager.Instance != null &&
                    GameManager.Instance.State == GameLifecycleState.Faulted)
                {
                    Assert.Fail("GameManager alcançou o estado Faulted.");
                }

                yield return null;
            }

            GameManager gameManager = GameManager.Instance;
            Assert.That(gameManager, Is.Not.Null);
            Assert.That(gameManager.State, Is.EqualTo(GameLifecycleState.Ready));
            Assert.That(gameManager.CurrentPlayerId, Is.Not.Empty);
            Assert.That(gameManager.Inventory.ServerRevision, Is.GreaterThanOrEqualTo(0));
            Assert.That(gameManager.Professions.ServerRevision, Is.GreaterThanOrEqualTo(0));

            float sceneDeadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != "Battle" &&
                   Time.realtimeSinceStartup < sceneDeadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Battle"));
            Assert.That(
                Object.FindAnyObjectByType<BattleSceneController>(),
                Is.Not.Null);

            Object.Destroy(gameManager.gameObject);
            yield return null;
        }
    }
}
