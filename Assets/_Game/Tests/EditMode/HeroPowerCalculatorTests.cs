using System;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;
using NUnit.Framework;
using UnityEngine;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class HeroPowerCalculatorTests
    {
        private static readonly CombatBalanceTuning Tuning = new CombatBalanceTuning();

        [TestCase(1, 1.00000d)]
        [TestCase(50, 5.02535d)]
        [TestCase(100, 10.86535d)]
        public void LevelMultiplier_KnownLevel_ReturnsGddValue(
            int level,
            double expected)
        {
            double result = HeroPowerCalculator.CalculateLevelMultiplier(level, Tuning);

            Assert.That(result, Is.EqualTo(expected).Within(0.0000001d));
        }

        [TestCase(Rarity.Common, 1.00d)]
        [TestCase(Rarity.Uncommon, 1.08d)]
        [TestCase(Rarity.Rare, 1.18d)]
        [TestCase(Rarity.Epic, 1.31d)]
        [TestCase(Rarity.Legendary, 1.47d)]
        [TestCase(Rarity.Mythic, 1.66d)]
        public void RarityMultiplier_EachPersistedRarity_ReturnsConfiguredValue(
            Rarity rarity,
            double expected)
        {
            Assert.That(
                HeroPowerCalculator.GetRarityMultiplier(rarity, Tuning),
                Is.EqualTo(expected));
        }

        [TestCase(0, 1.00d)]
        [TestCase(1, 1.08d)]
        [TestCase(2, 1.18d)]
        [TestCase(3, 1.30d)]
        [TestCase(4, 1.44d)]
        [TestCase(5, 1.60d)]
        public void AscensionMultiplier_EachLevel_ReturnsConfiguredValue(
            int ascension,
            double expected)
        {
            Assert.That(
                HeroPowerCalculator.GetAscensionMultiplier(ascension, Tuning),
                Is.EqualTo(expected));
        }

        [Test]
        public void FinalSpeed_BelowMinimum_IsClamped()
        {
            HeroBuildData build = CreateBaseWarriorBuild();
            build.baseStats = new HeroStatBlock(1100, 105, 100, 10d);

            HeroStatBlock result = HeroPowerCalculator.CalculateFinalStats(build, Tuning);

            Assert.That(result.Speed, Is.EqualTo(60d));
        }

        [Test]
        public void FinalSpeed_AboveMaximum_IsClamped()
        {
            HeroBuildData build = CreateBaseWarriorBuild();
            build.baseStats = new HeroStatBlock(1100, 105, 100, 1000d);

            HeroStatBlock result = HeroPowerCalculator.CalculateFinalStats(build, Tuning);

            Assert.That(result.Speed, Is.EqualTo(180d));
        }

        [Test]
        public void DamageReduction_ExtremeDefense_StopsAtConfiguredCap()
        {
            double result = HeroPowerCalculator.CalculateDamageReduction(
                long.MaxValue,
                100,
                Tuning);

            Assert.That(result, Is.EqualTo(0.75d));
        }

        [Test]
        public void FinalStats_ZeroBaseDefense_RemainsValid()
        {
            HeroStatBlock result = HeroPowerCalculator.CalculateFinalStats(
                new HeroBuildData
                {
                    baseStats = new HeroStatBlock(100, 10, 0, 100)
                },
                Tuning);

            Assert.That(result.Defense, Is.Zero);
            Assert.That(
                HeroPowerCalculator.CalculateDamageReduction(result.Defense, 1, Tuning),
                Is.Zero);
        }

        [Test]
        public void HeroPower_BaseWarrior_EqualsGddReference()
        {
            long result = HeroPowerCalculator.CalculateHeroPower(
                CreateBaseWarriorBuild(),
                Tuning);

            Assert.That(result, Is.EqualTo(1140));
        }

        [Test]
        public void HeroPower_EquipmentBonuses_AppliesFlatThenPercentAtFinalRounding()
        {
            HeroBuildData build = CreateBaseWarriorBuild();
            build.equipmentModifiers = new HeroStatModifiers(
                100, 0.10d,
                15, 0.20d,
                20, 0.10d,
                10d, 0.10d);

            HeroStatBlock stats = HeroPowerCalculator.CalculateFinalStats(build, Tuning);
            long power = HeroPowerCalculator.CalculateHeroPower(build, Tuning);

            Assert.That(stats.MaxHealth, Is.EqualTo(1320));
            Assert.That(stats.Attack, Is.EqualTo(144));
            Assert.That(stats.Defense, Is.EqualTo(132));
            Assert.That(stats.Speed, Is.EqualTo(121d).Within(0.000001d));
            Assert.That(power, Is.GreaterThan(1140));
        }

        [Test]
        public void HeroBuildData_JsonRoundTrip_PreservesStatDtos()
        {
            HeroBuildData source = CreateBaseWarriorBuild();
            source.equipmentModifiers = new HeroStatModifiers(
                100, 0.10d,
                15, 0.20d,
                20, 0.30d,
                5d, 0.40d);

            string json = JsonUtility.ToJson(source);
            HeroBuildData restored = JsonUtility.FromJson<HeroBuildData>(json);

            Assert.That(restored.baseStats.MaxHealth, Is.EqualTo(1100));
            Assert.That(restored.baseStats.Attack, Is.EqualTo(105));
            Assert.That(restored.baseStats.Defense, Is.EqualTo(100));
            Assert.That(restored.baseStats.Speed, Is.EqualTo(100d));
            Assert.That(restored.equipmentModifiers.FlatHealth, Is.EqualTo(100));
            Assert.That(restored.equipmentModifiers.PercentHealth, Is.EqualTo(0.10d));
            Assert.That(restored.equipmentModifiers.FlatAttack, Is.EqualTo(15));
            Assert.That(restored.equipmentModifiers.PercentAttack, Is.EqualTo(0.20d));
            Assert.That(restored.equipmentModifiers.FlatDefense, Is.EqualTo(20));
            Assert.That(restored.equipmentModifiers.PercentDefense, Is.EqualTo(0.30d));
            Assert.That(restored.equipmentModifiers.FlatSpeed, Is.EqualTo(5d));
            Assert.That(restored.equipmentModifiers.PercentSpeed, Is.EqualTo(0.40d));

            const string legacyJson =
                "{\"level\":2,\"rarity\":1,\"ascension\":1," +
                "\"baseStats\":{\"maxHealth\":1234,\"attack\":222," +
                "\"defense\":111,\"speed\":99.5}," +
                "\"equipmentBonuses\":{\"flatHealth\":25," +
                "\"healthPercent\":0.15,\"flatAttack\":7," +
                "\"attackPercent\":0.05,\"flatDefense\":8," +
                "\"defensePercent\":0.06,\"flatSpeed\":3.5," +
                "\"speedPercent\":0.07}}";
            HeroBuildData migrated = JsonUtility.FromJson<HeroBuildData>(legacyJson);

            Assert.That(migrated.ascensionLevel, Is.EqualTo(1));
            Assert.That(migrated.baseStats.MaxHealth, Is.EqualTo(1234));
            Assert.That(migrated.baseStats.Speed, Is.EqualTo(99.5d));
            Assert.That(migrated.equipmentModifiers.FlatHealth, Is.EqualTo(25));
            Assert.That(migrated.equipmentModifiers.PercentHealth, Is.EqualTo(0.15d));
            Assert.That(migrated.equipmentModifiers.FlatSpeed, Is.EqualTo(3.5d));
        }

        [Test]
        public void HeroPower_RarityBelowDefinitionInitialRarity_IsRejected()
        {
            var definition = new HeroDefinition(
                "hero_rare_test",
                "Herói Raro",
                "Herói que começa Raro.",
                HeroArchetype.Warrior,
                1100,
                105,
                100,
                100,
                Rarity.Rare);
            HeroInstance hero = CreateUnlockedHero(definition.DefinitionId);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                HeroPowerCalculator.CalculateBreakdown(
                    hero,
                    definition,
                    EmptyHeroEquipmentModifierProvider.Instance,
                    Tuning));

            Assert.That(exception.Message, Does.Contain("raridade abaixo da inicial"));
        }

        [Test]
        public void HeroPower_CachedValue_IsIgnoredAndCanOnlyBeRefreshedByCalculation()
        {
            HeroDefinition definition = CreateWarriorDefinition();
            HeroInstance hero = HeroInstance.Restore(
                "hero_instance_001",
                definition.DefinitionId,
                "player_001",
                1,
                0,
                Rarity.Common,
                0,
                0,
                null,
                true,
                1,
                7,
                null,
                999999,
                Tuning);

            HeroPowerBreakdown breakdown = HeroPowerCalculator.CalculateBreakdown(
                hero,
                definition,
                EmptyHeroEquipmentModifierProvider.Instance,
                Tuning);
            HeroInstance refreshed = HeroPowerCalculator.RefreshCalculatedPowerCache(
                hero,
                definition,
                EmptyHeroEquipmentModifierProvider.Instance,
                Tuning);

            Assert.That(breakdown.HeroPower.Value, Is.EqualTo(1140));
            Assert.That(refreshed.CalculatedPower, Is.EqualTo(1140));
            Assert.That(hero.CalculatedPower, Is.EqualTo(999999));
        }

        [Test]
        public void HeroPower_EquipmentProvider_ContributesModifiers()
        {
            HeroDefinition definition = CreateWarriorDefinition();
            HeroInstance hero = CreateUnlockedHero(definition.DefinitionId);
            var provider = new StubEquipmentModifierProvider(
                new HeroStatModifiers(100, 0d, 20, 0d, 30, 0d, 5d, 0d));

            HeroPowerBreakdown result = HeroPowerCalculator.CalculateBreakdown(
                hero,
                definition,
                provider,
                Tuning);

            Assert.That(result.EquipmentModifiers.FlatHealth, Is.EqualTo(100));
            Assert.That(result.FinalStats.Attack, Is.EqualTo(125));
            Assert.That(result.HeroPower.Value, Is.GreaterThan(1140));
        }

        private static HeroBuildData CreateBaseWarriorBuild()
        {
            return new HeroBuildData
            {
                level = 1,
                rarity = Rarity.Common,
                ascensionLevel = 0,
                baseStats = new HeroStatBlock(1100, 105, 100, 100d)
            };
        }

        private static HeroDefinition CreateWarriorDefinition()
        {
            return new HeroDefinition(
                "hero_warrior_test",
                "Guerreiro",
                "Herói de teste.",
                HeroArchetype.Warrior,
                1100,
                105,
                100,
                100,
                Rarity.Common);
        }

        private static HeroInstance CreateUnlockedHero(string definitionId)
        {
            return HeroInstance.Restore(
                "hero_instance_001",
                definitionId,
                "player_001",
                1,
                0,
                Rarity.Common,
                0,
                0,
                null,
                true,
                1,
                1,
                null,
                0,
                Tuning);
        }

        private sealed class StubEquipmentModifierProvider : IHeroEquipmentModifierProvider
        {
            private readonly HeroStatModifiers modifiers;

            public StubEquipmentModifierProvider(HeroStatModifiers modifiers)
            {
                this.modifiers = modifiers;
            }

            public HeroStatModifiers GetModifiers(HeroInstance hero)
            {
                return modifiers;
            }
        }
    }
}
