using System;
using System.Collections.Generic;
using System.Linq;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;
using IdleMedievalLegends.Editor.ContentCatalog;
using NUnit.Framework;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class BattleSimulatorTests
    {
        private const string RulesVersion = "combat_rules_v1";

        [Test]
        public void Simulation_SameSeedAndInput_ProducesIdenticalResultAndLog()
        {
            BattleRequest request = RandomizedRequest(5005);

            BattleResult first = new BattleSimulator().Simulate(request);
            BattleResult second = new BattleSimulator().Simulate(request);

            Assert.That(second.DeterministicHash, Is.EqualTo(first.DeterministicHash));
            Assert.That(EventSignatures(second), Is.EqualTo(EventSignatures(first)));
            Assert.That(second.Outcome, Is.EqualTo(first.Outcome));
        }

        [Test]
        public void Simulation_DifferentSeeds_CanProduceDifferentDamageVariation()
        {
            BattleResult first = new BattleSimulator().Simulate(RandomizedRequest(1));
            BattleResult second = new BattleSimulator().Simulate(RandomizedRequest(2));

            Assert.That(DamageValues(second), Is.Not.EqualTo(DamageValues(first)));
        }

        [Test]
        public void BasicAttack_Hit_ReducesTargetHealth()
        {
            BattleResult result = Simulate(
                Unit("attacker", BattleSide.Attacker, 0, attack: 100),
                Unit("defender", BattleSide.Defender, 0, health: 500),
                Configuration(maximumActions: 1));

            CombatEvent damage = result.Events.Single(
                value => value.EventType == CombatEventType.DamageDealt);
            Assert.That(damage.TargetHealthAfter, Is.LessThan(damage.TargetHealthBefore));
            Assert.That(damage.Value, Is.EqualTo(100));
        }

        [Test]
        public void Damage_Critical_IncreasesDamageAndEmitsCriticalEvent()
        {
            BattleUnit normal = Unit(
                "normal", BattleSide.Attacker, 0, attack: 100, criticalChance: 0d);
            BattleUnit critical = Unit(
                "critical", BattleSide.Attacker, 0, attack: 100,
                criticalChance: 1d, criticalMultiplier: 2d);
            CombatSnapshot target = Unit(
                "target", BattleSide.Defender, 0, health: 1000).ToStateSnapshot();
            var calculator = new DamageCalculator();
            BattleConfiguration configuration = Configuration();

            DamageResult normalDamage = calculator.Calculate(
                normal.ToStateSnapshot(), target, configuration, new DeterministicRandom(10));
            DamageResult criticalDamage = calculator.Calculate(
                critical.ToStateSnapshot(), target, configuration, new DeterministicRandom(10));
            BattleResult battle = Simulate(
                critical,
                Unit("battle_target", BattleSide.Defender, 0, health: 1000),
                Configuration(maximumActions: 1));

            Assert.That(criticalDamage.Damage, Is.EqualTo(normalDamage.Damage * 2));
            Assert.That(battle.Events.Any(
                value => value.EventType == CombatEventType.CriticalHit), Is.True);
        }

        [Test]
        public void Damage_Defense_ReducesDamage()
        {
            CombatSnapshot source = Unit(
                "source", BattleSide.Attacker, 0, attack: 400).ToStateSnapshot();
            CombatSnapshot noDefense = Unit(
                "no_defense", BattleSide.Defender, 0, defense: 0).ToStateSnapshot();
            CombatSnapshot defended = Unit(
                "defended", BattleSide.Defender, 0, defense: 400).ToStateSnapshot();
            var calculator = new DamageCalculator();
            BattleConfiguration configuration = Configuration();

            DamageResult first = calculator.Calculate(
                source, noDefense, configuration, new DeterministicRandom(20));
            DamageResult second = calculator.Calculate(
                source, defended, configuration, new DeterministicRandom(20));

            Assert.That(first.Damage, Is.EqualTo(400));
            Assert.That(second.Damage, Is.EqualTo(200));
        }

        [Test]
        public void Damage_HighDefense_UsesConfiguredMinimumDamage()
        {
            BattleConfiguration configuration = Configuration(minimumDamage: 5);
            DamageResult result = new DamageCalculator().Calculate(
                Unit("source", BattleSide.Attacker, 0, attack: 1).ToStateSnapshot(),
                Unit(
                    "target", BattleSide.Defender, 0,
                    health: 100, defense: long.MaxValue).ToStateSnapshot(),
                configuration,
                new DeterministicRandom(30));

            Assert.That(result.Damage, Is.EqualTo(5));
        }

        [Test]
        public void Targeting_DefeatedUnit_IsRemovedFromLaterSelection()
        {
            BattleConfiguration configuration = Configuration(maximumActions: 2);
            var request = new BattleRequest(
                new BattleTeam(
                    BattleSide.Attacker,
                    new[]
                    {
                        Unit("attacker", BattleSide.Attacker, 0, attack: 1000, speed: 200)
                    }),
                new BattleTeam(
                    BattleSide.Defender,
                    new[]
                    {
                        Unit("defender_1", BattleSide.Defender, 0, health: 10),
                        Unit("defender_2", BattleSide.Defender, 1, health: 10)
                    }),
                40,
                configuration,
                RulesVersion);

            BattleResult result = new BattleSimulator().Simulate(request);
            string[] targets = result.Events
                .Where(value => value.EventType == CombatEventType.BasicAttackStarted)
                .Select(value => value.TargetUnitId)
                .ToArray();

            Assert.That(targets, Is.EqualTo(new[] { "defender_1", "defender_2" }));
        }

        [Test]
        public void Battle_TeamBecomesWithoutLivingUnits_Loses()
        {
            BattleResult result = Simulate(
                Unit("attacker", BattleSide.Attacker, 0, attack: 10000, speed: 200),
                Unit("defender", BattleSide.Defender, 0, health: 10),
                Configuration(maximumActions: 10));

            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.AttackerVictory));
            Assert.That(result.WinningTeam, Is.EqualTo(BattleSide.Attacker));
            Assert.That(result.EndReason, Is.EqualTo("defender_eliminated"));
        }

        [Test]
        public void Battle_ActionLimitReached_ReturnsDrawWithReason()
        {
            BattleResult result = Simulate(
                Unit("attacker", BattleSide.Attacker, 0, health: 10000, attack: 1),
                Unit("defender", BattleSide.Defender, 0, health: 10000, attack: 1),
                Configuration(maximumActions: 1));

            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Draw));
            Assert.That(result.WinningTeam, Is.Null);
            Assert.That(result.EndReason, Is.EqualTo("action_limit"));
            CombatEvent ended = result.Events.Last();
            Assert.That(ended.EventType, Is.EqualTo(CombatEventType.BattleEnded));
            Assert.That(ended.Metadata.Any(
                value => value.Key == "reason" && value.Value == "action_limit"), Is.True);
        }

        [Test]
        public void TurnOrder_EqualSpeed_UsesStableTeamSlotAndIdTieBreak()
        {
            BattleResult result = Simulate(
                Unit("z_attacker", BattleSide.Attacker, 0, speed: 100),
                Unit("a_defender", BattleSide.Defender, 0, speed: 100),
                Configuration(maximumActions: 1));

            CombatEvent selected = result.Events.First(
                value => value.EventType == CombatEventType.UnitSelected);
            Assert.That(selected.SourceUnitId, Is.EqualTo("z_attacker"));
        }

        [Test]
        public void TargetSelector_DeadCandidate_NeverReturnsDeadUnit()
        {
            CombatSnapshot source = Unit(
                "source", BattleSide.Attacker, 0).ToStateSnapshot();
            CombatSnapshot dead = Snapshot(
                "dead", BattleSide.Defender, 0, currentHealth: 0, alive: false);
            CombatSnapshot alive = Snapshot(
                "alive", BattleSide.Defender, 1, currentHealth: 100, alive: true);

            CombatSnapshot result = new TargetSelector(TargetSelectionMode.Random)
                .SelectTarget(
                    source,
                    new[] { dead, alive },
                    new DeterministicRandom(50));

            Assert.That(result.UnitId, Is.EqualTo("alive"));
        }

        [Test]
        public void BattleRequest_DuplicateUnitId_IsRejected()
        {
            BattleUnit attacker = Unit("duplicate", BattleSide.Attacker, 0);
            BattleUnit defender = Unit("duplicate", BattleSide.Defender, 0);

            Assert.Throws<InvalidOperationException>(() => Request(
                new[] { attacker }, new[] { defender }, Configuration()));
        }

        [Test]
        public void BattleTeam_EmptyUnits_IsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                new BattleTeam(BattleSide.Attacker, Array.Empty<BattleUnit>()));
        }

        [Test]
        public void BattleResult_EventsAreOrderedAndSequenceHasNoGaps()
        {
            BattleResult result = Simulate(
                Unit("attacker", BattleSide.Attacker, 0, attack: 500),
                Unit("defender", BattleSide.Defender, 0, health: 300),
                Configuration(maximumActions: 10));

            Assert.That(result.Events.First().EventType,
                Is.EqualTo(CombatEventType.BattleStarted));
            Assert.That(result.Events.Last().EventType,
                Is.EqualTo(CombatEventType.BattleEnded));
            for (int i = 0; i < result.Events.Count; i++)
            {
                Assert.That(result.Events[i].Sequence, Is.EqualTo(i));
                if (i > 0)
                {
                    Assert.That(result.Events[i].LogicalTick,
                        Is.GreaterThanOrEqualTo(result.Events[i - 1].LogicalTick));
                }
            }
        }

        [Test]
        public void BattleResult_SameBattle_HasIdenticalHash()
        {
            BattleRequest request = RandomizedRequest(60);

            string first = new BattleSimulator().Simulate(request).DeterministicHash;
            string second = new BattleSimulator().Simulate(request).DeterministicHash;

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Has.Length.EqualTo(64));
        }

        [Test]
        public void Simulator_Run_DoesNotModifyInputSnapshots()
        {
            BattleUnit attacker = Unit(
                "attacker", BattleSide.Attacker, 0, health: 321, actionGauge: 25d);
            BattleUnit defender = Unit(
                "defender", BattleSide.Defender, 0, health: 456, actionGauge: 50d);
            BattleRequest request = Request(
                new[] { attacker }, new[] { defender }, Configuration(maximumActions: 3));

            new BattleSimulator().Simulate(request);

            Assert.That(attacker.CurrentHealth, Is.EqualTo(321));
            Assert.That(attacker.ActionGauge, Is.EqualTo(25d));
            Assert.That(defender.CurrentHealth, Is.EqualTo(456));
            Assert.That(defender.ActionGauge, Is.EqualTo(50d));
        }

        [Test]
        public void DemoHeroes_PaladinSurvivesMoreBasicAttacksThanMage()
        {
            ContentCatalogLookup lookup = DemoLookup();
            BattleUnit paladin = DefinitionUnit(
                lookup.GetHero("hero_paladin_001"), BattleSide.Defender, 0, "paladin");
            BattleUnit mage = DefinitionUnit(
                lookup.GetHero("hero_mage_001"), BattleSide.Defender, 0, "mage");
            BattleUnit source = Unit(
                "source", BattleSide.Attacker, 0, attack: 200);

            int paladinHits = HitsToDefeat(source, paladin);
            int mageHits = HitsToDefeat(source, mage);

            Assert.That(paladinHits, Is.GreaterThan(mageHits));
        }

        [Test]
        public void DemoHeroes_ArcherActsBeforePaladinBecauseSpeedIsHigher()
        {
            ContentCatalogLookup lookup = DemoLookup();
            BattleUnit archer = DefinitionUnit(
                lookup.GetHero("hero_archer_001"), BattleSide.Attacker, 0, "archer");
            BattleUnit paladin = DefinitionUnit(
                lookup.GetHero("hero_paladin_001"), BattleSide.Defender, 0, "paladin");

            BattleResult result = Simulate(
                archer,
                paladin,
                Configuration(maximumActions: 1));

            CombatEvent selected = result.Events.First(
                value => value.EventType == CombatEventType.UnitSelected);
            Assert.That(selected.SourceUnitId, Is.EqualTo("archer"));
        }

        [Test]
        public void BattleRequest_InvalidSeed_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Request(
                    new[] { Unit("a", BattleSide.Attacker, 0) },
                    new[] { Unit("d", BattleSide.Defender, 0) },
                    Configuration(),
                    seed: 0));
        }

        [Test]
        public void BattleRequest_DuplicateSlotInTeam_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => Request(
                new[]
                {
                    Unit("a1", BattleSide.Attacker, 0),
                    Unit("a2", BattleSide.Attacker, 0)
                },
                new[] { Unit("d", BattleSide.Defender, 0) },
                Configuration()));
        }

        [Test]
        public void BattleRequest_SlotOutsideConfiguredTeamRange_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => Request(
                new[] { Unit("a", BattleSide.Attacker, 5) },
                new[] { Unit("d", BattleSide.Defender, 0) },
                Configuration()));
        }

        [Test]
        public void BattleRequest_TeamAboveConfiguredLimit_IsRejected()
        {
            BattleUnit[] attackers = Enumerable.Range(0, 6)
                .Select(index => Unit($"a{index}", BattleSide.Attacker, index))
                .ToArray();

            Assert.Throws<InvalidOperationException>(() => Request(
                attackers,
                new[] { Unit("d", BattleSide.Defender, 0) },
                Configuration()));
        }

        [TestCase(0, 100d)]
        [TestCase(100, 0d)]
        public void BattleUnit_InvalidInitialHealthOrSpeed_IsRejected(
            long health,
            double speed)
        {
            Assert.Throws<InvalidOperationException>(() => Unit(
                "invalid", BattleSide.Attacker, 0, health: health, speed: speed));
        }

        [Test]
        public void Attack_ZeroHitChance_EmitsMissWithoutDamage()
        {
            BattleConfiguration configuration = Configuration(
                maximumActions: 1,
                accuracy: 0d,
                minimumHitChance: 0d,
                maximumHitChance: 0d);
            BattleResult result = Simulate(
                Unit("attacker", BattleSide.Attacker, 0, accuracy: 0d),
                Unit("defender", BattleSide.Defender, 0),
                configuration);

            Assert.That(result.Events.Any(
                value => value.EventType == CombatEventType.AttackMissed), Is.True);
            Assert.That(result.Events.Any(
                value => value.EventType == CombatEventType.DamageDealt), Is.False);
        }

        [Test]
        public void BattleUnitFactory_UnlockedHero_UsesCalculatedFinalSnapshot()
        {
            ContentCatalogLookup lookup = DemoLookup();
            HeroDefinition definition = lookup.GetHero("hero_paladin_001");
            var tuning = new CombatBalanceTuning();
            HeroInstance hero = HeroInstance.Restore(
                "paladin_instance", definition.DefinitionId, "player", 1, 0,
                Rarity.Common, 0, 0, null, true, 1, 1, null, 0, tuning);

            BattleUnit result = BattleUnitFactory.FromHero(
                hero,
                definition,
                EmptyHeroEquipmentModifierProvider.Instance,
                tuning,
                Configuration(),
                BattleSide.Attacker,
                0);

            Assert.That(result.MaximumHealth, Is.EqualTo(1400));
            Assert.That(result.Attack, Is.EqualTo(70));
            Assert.That(result.Defense, Is.EqualTo(150));
            Assert.That(result.Speed, Is.EqualTo(85d));
        }

        private static BattleRequest RandomizedRequest(long seed)
        {
            BattleConfiguration configuration = Configuration(
                maximumActions: 20,
                minimumVariation: 0.80d,
                maximumVariation: 1.20d,
                criticalChance: 0.35d,
                targetMode: TargetSelectionMode.Random);
            return Request(
                new[]
                {
                    Unit(
                        "attacker_1", BattleSide.Attacker, 0,
                        health: 2500, attack: 170, speed: 120,
                        criticalChance: 0.35d),
                    Unit(
                        "attacker_2", BattleSide.Attacker, 1,
                        health: 2500, attack: 160, speed: 90,
                        criticalChance: 0.35d)
                },
                new[]
                {
                    Unit(
                        "defender_1", BattleSide.Defender, 0,
                        health: 2500, attack: 165, speed: 110,
                        criticalChance: 0.35d),
                    Unit(
                        "defender_2", BattleSide.Defender, 1,
                        health: 2500, attack: 155, speed: 95,
                        criticalChance: 0.35d)
                },
                configuration,
                seed);
        }

        private static BattleResult Simulate(
            BattleUnit attacker,
            BattleUnit defender,
            BattleConfiguration configuration)
        {
            return new BattleSimulator().Simulate(Request(
                new[] { attacker },
                new[] { defender },
                configuration));
        }

        private static BattleRequest Request(
            IEnumerable<BattleUnit> attackers,
            IEnumerable<BattleUnit> defenders,
            BattleConfiguration configuration,
            long seed = 5005)
        {
            return new BattleRequest(
                new BattleTeam(BattleSide.Attacker, attackers),
                new BattleTeam(BattleSide.Defender, defenders),
                seed,
                configuration,
                RulesVersion,
                "battle_test");
        }

        private static BattleConfiguration Configuration(
            int maximumActions = 100,
            long minimumDamage = 1,
            double minimumVariation = 1d,
            double maximumVariation = 1d,
            double criticalChance = 0d,
            double accuracy = 1d,
            double evasion = 0d,
            double minimumHitChance = 1d,
            double maximumHitChance = 1d,
            TargetSelectionMode targetMode = TargetSelectionMode.LowestSlot)
        {
            return new BattleConfiguration(
                maximumTeamSize: 5,
                maximumActions: maximumActions,
                actionGaugeThreshold: 1000d,
                basicAttackMultiplier: 1d,
                minimumDamageVariation: minimumVariation,
                maximumDamageVariation: maximumVariation,
                minimumDamage: minimumDamage,
                defaultCriticalChance: criticalChance,
                defaultCriticalMultiplier: 1.5d,
                defaultAccuracy: accuracy,
                defaultEvasion: evasion,
                minimumHitChance: minimumHitChance,
                maximumHitChance: maximumHitChance,
                targetSelectionMode: targetMode);
        }

        private static BattleUnit Unit(
            string id,
            BattleSide side,
            int slot,
            long health = 1000,
            long attack = 100,
            long defense = 0,
            double speed = 100d,
            double criticalChance = 0d,
            double criticalMultiplier = 1.5d,
            double accuracy = 1d,
            double evasion = 0d,
            double actionGauge = 0d)
        {
            return new BattleUnit(
                id,
                $"definition_{id}",
                side,
                slot,
                1,
                health,
                health,
                attack,
                defense,
                speed,
                criticalChance,
                criticalMultiplier,
                accuracy,
                evasion,
                actionGauge,
                new[] { "test" });
        }

        private static CombatSnapshot Snapshot(
            string id,
            BattleSide side,
            int slot,
            long currentHealth,
            bool alive)
        {
            return new CombatSnapshot(
                id, $"definition_{id}", side, slot, 1, 100,
                currentHealth, 10, 0, 100d, 0d, 1.5d, 1d, 0d,
                alive, 0d, new[] { "test" });
        }

        private static ContentCatalogLookup DemoLookup()
        {
            return new ContentCatalogLookup(ContentCatalogDemoFactory.Create());
        }

        private static BattleUnit DefinitionUnit(
            HeroDefinition definition,
            BattleSide side,
            int slot,
            string unitId)
        {
            return new BattleUnit(
                unitId,
                definition.DefinitionId,
                side,
                slot,
                1,
                definition.BaseHealth,
                definition.BaseHealth,
                definition.BaseAttack,
                definition.BaseDefense,
                definition.BaseSpeed,
                0d,
                1.5d,
                1d,
                0d,
                0d,
                definition.Tags);
        }

        private static int HitsToDefeat(BattleUnit source, BattleUnit target)
        {
            DamageResult damage = new DamageCalculator().Calculate(
                source.ToStateSnapshot(),
                target.ToStateSnapshot(),
                Configuration(),
                new DeterministicRandom(70));
            return checked((int)Math.Ceiling(target.MaximumHealth / (double)damage.Damage));
        }

        private static long[] DamageValues(BattleResult result)
        {
            return result.Events
                .Where(value => value.EventType == CombatEventType.DamageDealt)
                .Select(value => value.Value)
                .ToArray();
        }

        private static string[] EventSignatures(BattleResult result)
        {
            return result.Events.Select(value =>
                $"{value.Sequence}|{value.Turn}|{value.LogicalTick}|" +
                $"{value.EventType}|{value.SourceUnitId}|{value.TargetUnitId}|" +
                $"{value.Value}|{value.Critical}|{value.TargetHealthBefore}|" +
                $"{value.TargetHealthAfter}|" +
                string.Join(
                    ",",
                    value.Metadata.Select(item => $"{item.Key}={item.Value}")))
                .ToArray();
        }
    }

    internal static class BattleUnitTestExtensions
    {
        public static CombatSnapshot ToStateSnapshot(this BattleUnit unit)
        {
            return new BattleUnitState(unit).ToSnapshot();
        }
    }
}
