using System.Linq;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Editor.ContentCatalog;
using IdleMedievalLegends.Presentation.Battle;
using NUnit.Framework;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class BattlePresentationLogicTests
    {
        [TestCase(-10, 100, 0f)]
        [TestCase(0, 0, 0f)]
        [TestCase(50, 100, 0.5f)]
        [TestCase(150, 100, 1f)]
        public void HealthNormalization_InvalidOrBoundedValues_ClampsSafely(
            long current,
            long maximum,
            float expected)
        {
            Assert.That(
                BattlePresentationMath.NormalizeHealth(current, maximum),
                Is.EqualTo(expected));
        }

        [Test]
        public void BattleSpeedController_Cycle_UsesOneTwoThreeAndWraps()
        {
            var controller = new BattleSpeedController();

            Assert.That(controller.Speed, Is.EqualTo(1));
            Assert.That(controller.Cycle(), Is.EqualTo(2));
            Assert.That(controller.Cycle(), Is.EqualTo(3));
            Assert.That(controller.Cycle(), Is.EqualTo(1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => controller.Set(4));
        }

        [Test]
        public void BattlePresenter_Skip_TransitionsWithoutChangingResult()
        {
            BattleResult result = CreateTinyResult();
            var presenter = new BattlePresenter();

            presenter.Prepare(result);
            presenter.Play();
            bool beganSkip = presenter.BeginSkip();
            presenter.Complete();

            Assert.That(beganSkip, Is.True);
            Assert.That(presenter.State, Is.EqualTo(BattlePresentationState.Completed));
            Assert.That(presenter.Result, Is.SameAs(result));
            Assert.That(presenter.Result.DeterministicHash,
                Is.EqualTo(result.DeterministicHash));
        }

        [Test]
        public void DebugScenario_SameSeed_BuildsDeterministicThreeVersusThreeBattle()
        {
            var catalog = new ContentCatalogLookup(ContentCatalogDemoFactory.Create());
            var tuning = new CombatBalanceTuning();

            BattleDebugScenario first = BattleDebugScenarioFactory.Create(
                catalog,
                tuning,
                6006);
            BattleDebugScenario second = BattleDebugScenarioFactory.Create(
                catalog,
                tuning,
                6006);

            Assert.That(first.Request.Attacker.Units, Has.Count.EqualTo(3));
            Assert.That(first.Request.Defender.Units, Has.Count.EqualTo(3));
            Assert.That(first.Result.DeterministicHash,
                Is.EqualTo(second.Result.DeterministicHash));
            Assert.That(first.Request.Defender.Units.All(
                unit => unit.UnitId.StartsWith("enemy_debug_")), Is.True);
        }

        private static BattleResult CreateTinyResult()
        {
            var configuration = new BattleConfiguration(
                maximumActions: 1,
                minimumDamageVariation: 1d,
                maximumDamageVariation: 1d,
                defaultCriticalChance: 0d,
                defaultAccuracy: 1d,
                defaultEvasion: 0d,
                minimumHitChance: 1d,
                maximumHitChance: 1d);
            var attacker = new BattleUnit(
                "a", "hero_paladin_001", BattleSide.Attacker, 0, 1,
                100, 100, 20, 0, 100, 0, 1.5, 1, 0, 0, null);
            var defender = new BattleUnit(
                "d", "hero_mage_001", BattleSide.Defender, 0, 1,
                100, 100, 20, 0, 100, 0, 1.5, 1, 0, 0, null);
            var request = new BattleRequest(
                new BattleTeam(BattleSide.Attacker, new[] { attacker }),
                new BattleTeam(BattleSide.Defender, new[] { defender }),
                1,
                configuration,
                BattleDebugScenarioFactory.RulesVersion);
            return new BattleSimulator().Simulate(request);
        }
    }
}
