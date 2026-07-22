using System.Collections;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Presentation.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdleMedievalLegends.Tests.PlayMode
{
    public sealed class BattleScenePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameManager.Instance != null)
            {
                Object.Destroy(GameManager.Instance.gameObject);
                yield return null;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(
                "Battle",
                LoadSceneMode.Single);
            while (!operation.isDone)
                yield return null;
        }

        [UnityTest]
        public IEnumerator BattleScene_Load_StartsThreeVersusThreeScenario()
        {
            BattleSceneController controller = null;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (controller == null && Time.realtimeSinceStartup < deadline)
            {
                controller = Object.FindAnyObjectByType<BattleSceneController>();
                yield return null;
            }

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.Result, Is.Not.Null);
            Assert.That(
                controller.State,
                Is.EqualTo(BattlePresentationState.Playing)
                    .Or.EqualTo(BattlePresentationState.Completed));
            Assert.That(controller.PlayerTeamView.UnitViews, Has.Count.EqualTo(3));
            Assert.That(controller.EnemyTeamView.UnitViews, Has.Count.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator BattleHud_SpeedAndSkip_ApplyOnlyFinalPresentationState()
        {
            yield return null;
            BattleSceneController controller =
                Object.FindAnyObjectByType<BattleSceneController>();
            Assert.That(controller, Is.Not.Null);

            string originalHash = controller.Result.DeterministicHash;
            controller.HudView.SpeedButton.onClick.Invoke();
            Assert.That(controller.PlaybackSpeed, Is.EqualTo(2));
            controller.HudView.SpeedButton.onClick.Invoke();
            Assert.That(controller.PlaybackSpeed, Is.EqualTo(3));
            controller.HudView.SkipButton.onClick.Invoke();
            yield return null;

            Assert.That(controller.State, Is.EqualTo(BattlePresentationState.Completed));
            Assert.That(controller.Result.DeterministicHash, Is.EqualTo(originalHash));
            Assert.That(controller.HudView.DisplayedResult, Is.Not.Empty);
            AssertFinalHealthMatchesViews(controller);
        }

        [UnityTest]
        public IEnumerator BattlePlayback_AtThreeTimes_CompletesWithResultAndFinalHealth()
        {
            yield return null;
            BattleSceneController controller =
                Object.FindAnyObjectByType<BattleSceneController>();
            Assert.That(controller, Is.Not.Null);
            controller.SetSpeed(3);

            float deadline = Time.realtimeSinceStartup + 30f;
            while (controller.State != BattlePresentationState.Completed &&
                   controller.State != BattlePresentationState.Faulted &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(controller.State, Is.EqualTo(BattlePresentationState.Completed));
            Assert.That(controller.HudView.DisplayedResult, Is.Not.Empty);
            AssertFinalHealthMatchesViews(controller);
        }

        private static void AssertFinalHealthMatchesViews(
            BattleSceneController controller)
        {
            for (int i = 0; i < controller.Result.FinalSnapshots.Count; i++)
            {
                CombatSnapshot snapshot = controller.Result.FinalSnapshots[i];
                BattleUnitView view = controller.FindUnitView(snapshot.UnitId);
                Assert.That(view, Is.Not.Null, snapshot.UnitId);
                Assert.That(view.CurrentHealth, Is.EqualTo(snapshot.CurrentHealth));
                Assert.That(view.IsDefeated, Is.EqualTo(!snapshot.Alive));
            }
        }
    }
}
