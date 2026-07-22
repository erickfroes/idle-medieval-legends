using System;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;
using NUnit.Framework;
using UnityEngine;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class HeroProgressionRulesTests
    {
        private CombatBalanceTuning tuning;

        [SetUp]
        public void SetUp()
        {
            tuning = new CombatBalanceTuning();
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(101)]
        public void HeroInstance_InvalidLevel_IsRejected(int level)
        {
            Assert.Throws<InvalidOperationException>(() => RestoreUnlocked(level: level));
        }

        [Test]
        public void HeroInstance_NegativeFragments_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() =>
                RestoreUnlocked(ownedFragments: -1));
        }

        [Test]
        public void AddFragments_NegativeAmount_IsRejected()
        {
            HeroInstance hero = CreateLocked();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HeroProgressionRules.AddFragments(hero, -1, tuning));
        }

        [Test]
        public void Unlock_InsufficientFragments_IsRejected()
        {
            HeroInstance hero = HeroProgressionRules.AddFragments(
                CreateLocked(),
                19,
                tuning);

            Assert.Throws<InvalidOperationException>(() =>
                HeroProgressionRules.Unlock(hero, tuning));
        }

        [Test]
        public void Unlock_SufficientFragments_ConsumesCostAndUnlocks()
        {
            HeroInstance hero = HeroProgressionRules.AddFragments(
                CreateLocked(),
                20,
                tuning);

            HeroInstance result = HeroProgressionRules.Unlock(hero, tuning);

            Assert.That(result.Unlocked, Is.True);
            Assert.That(result.OwnedFragments, Is.Zero);
            Assert.That(result.InstanceId, Is.EqualTo(hero.InstanceId));
        }

        [Test]
        public void Ascend_SufficientFragments_IncrementsAndConsumesCost()
        {
            HeroInstance hero = RestoreUnlocked(ownedFragments: 20);

            Assert.That(HeroProgressionRules.CanAscend(hero, tuning), Is.True);
            HeroInstance result = HeroProgressionRules.Ascend(hero, tuning);

            Assert.That(result.AscensionLevel, Is.EqualTo(1));
            Assert.That(result.OwnedFragments, Is.Zero);
        }

        [Test]
        public void PromoteRarity_MaximumRarity_IsRejected()
        {
            HeroInstance hero = RestoreUnlocked(
                rarity: Rarity.Mythic,
                ownedFragments: 1000);

            Assert.Throws<InvalidOperationException>(() =>
                HeroProgressionRules.PromoteRarity(hero, tuning));
        }

        [Test]
        public void PromoteRarity_SufficientFragments_PromotesOneStep()
        {
            HeroInstance hero = RestoreUnlocked(ownedFragments: 30);

            HeroInstance result = HeroProgressionRules.PromoteRarity(hero, tuning);

            Assert.That(result.Rarity, Is.EqualTo(Rarity.Uncommon));
            Assert.That(result.OwnedFragments, Is.Zero);
        }

        [Test]
        public void ExperienceAndLevelUp_SufficientResources_ConsumesXpAndReportsGold()
        {
            HeroInstance hero = RestoreUnlocked();
            long xpCost = HeroProgressionRules.GetExperienceRequiredForNextLevel(1, tuning);
            long goldCost = HeroProgressionRules.GetGoldCostForNextLevel(1, tuning);
            hero = HeroProgressionRules.AddExperience(hero, xpCost + 7, tuning);

            HeroLevelUpResult result = HeroProgressionRules.LevelUp(
                hero,
                goldCost,
                tuning);

            Assert.That(result.Hero.Level, Is.EqualTo(2));
            Assert.That(result.Hero.Experience, Is.EqualTo(7));
            Assert.That(result.GoldCost, Is.EqualTo(goldCost));
        }

        [Test]
        public void ExperienceCurve_LevelOverride_ReplacesFormula()
        {
            tuning.levelProgressionOverrides.Add(new HeroLevelProgressionOverride
            {
                level = 10,
                experienceRequired = 12345,
                goldCost = 678
            });

            Assert.That(
                HeroProgressionRules.GetExperienceRequiredForNextLevel(10, tuning),
                Is.EqualTo(12345));
            Assert.That(
                HeroProgressionRules.GetGoldCostForNextLevel(10, tuning),
                Is.EqualTo(678));
        }

        [Test]
        public void EquipmentReference_Duplicate_IsRejectedAndUnequipRemovesIt()
        {
            HeroInstance hero = HeroProgressionRules.EquipItemReference(
                RestoreUnlocked(),
                "item_instance_001",
                tuning);

            Assert.Throws<InvalidOperationException>(() =>
                HeroProgressionRules.EquipItemReference(
                    hero,
                    "item_instance_001",
                    tuning));
            HeroInstance result = HeroProgressionRules.UnequipItemReference(
                hero,
                "item_instance_001",
                tuning);
            Assert.That(result.EquippedItemInstanceIds, Is.Empty);
        }

        [Test]
        public void UniqueInstanceIds_Duplicate_IsRejected()
        {
            HeroInstance first = RestoreUnlocked();
            HeroInstance duplicate = RestoreUnlocked(level: 2);

            Assert.Throws<InvalidOperationException>(() =>
                HeroProgressionRules.ValidateUniqueInstanceIds(
                    new[] { first, duplicate }));
        }

        [Test]
        public void HeroInstance_JsonRoundTrip_PreservesListBasedModifiersAndEquipment()
        {
            HeroInstance hero = HeroInstance.Restore(
                "hero_instance_001",
                "hero_paladin_001",
                "player_001",
                10,
                123,
                Rarity.Rare,
                2,
                77,
                new[] { "item_instance_001" },
                true,
                1,
                5,
                new[]
                {
                    new HeroPermanentModifier(
                        "achievement_001",
                        new HeroStatModifiers(10, 0.01d, 2, 0.02d, 3, 0.03d, 1d, 0.01d))
                },
                999,
                tuning);

            string json = JsonUtility.ToJson(hero);
            HeroInstance restored = JsonUtility.FromJson<HeroInstance>(json);
            restored.Validate(tuning);

            Assert.That(restored.InstanceId, Is.EqualTo(hero.InstanceId));
            Assert.That(restored.EquippedItemInstanceIds, Is.EqualTo(hero.EquippedItemInstanceIds));
            Assert.That(restored.PermanentModifiers, Has.Count.EqualTo(1));
            Assert.That(restored.PermanentModifiers[0].SourceId, Is.EqualTo("achievement_001"));
            Assert.That(restored.CalculatedPower, Is.EqualTo(999));
        }

        [Test]
        public void CombatBalanceTuningMigration_VersionTwo_UpgradesRequiredFields()
        {
            var legacy = new CombatBalanceTuning
            {
                version = 2,
                maxAscensionLevel = 0,
                unlockFragmentsByRarity = null,
                ascensionFragmentCosts = null,
                rarityPromotionFragmentCosts = null,
                levelProgressionOverrides = null,
                activeTeamSize = 0
            };

            CombatBalanceTuningMigration.UpgradeToCurrent(legacy);

            Assert.That(legacy.version, Is.EqualTo(CombatBalanceTuningMigration.CurrentVersion));
            Assert.That(legacy.maxAscensionLevel, Is.EqualTo(5));
            Assert.That(legacy.unlockFragmentsByRarity, Has.Length.EqualTo(6));
            Assert.That(legacy.ascensionFragmentCosts, Has.Length.EqualTo(5));
            Assert.That(legacy.rarityPromotionFragmentCosts, Has.Length.EqualTo(6));
            Assert.That(legacy.levelProgressionOverrides, Is.Empty);
            Assert.That(legacy.activeTeamSize, Is.EqualTo(5));
            Assert.DoesNotThrow(() => HeroBalanceTuningValidator.Validate(legacy));
        }

        [Test]
        public void CombatBalanceTuningMigration_VersionTwoCustomAscensionTable_PreservesIt()
        {
            double[] customMultipliers = { 1d, 1.12d, 1.29d, 1.55d };
            var legacy = new CombatBalanceTuning
            {
                version = 2,
                maxAscensionLevel = 0,
                ascensionMultipliers = customMultipliers,
                ascensionFragmentCosts = null
            };

            CombatBalanceTuningMigration.UpgradeToCurrent(legacy);

            Assert.That(legacy.ascensionMultipliers, Is.SameAs(customMultipliers));
            Assert.That(legacy.maxAscensionLevel, Is.EqualTo(3));
            Assert.That(legacy.ascensionFragmentCosts, Has.Length.EqualTo(3));
            Assert.DoesNotThrow(() => HeroBalanceTuningValidator.Validate(legacy));
        }

        [Test]
        public void CombatBalanceTuningMigration_VersionTwoExtendedAscensionTable_ExtendsCosts()
        {
            var legacy = new CombatBalanceTuning
            {
                version = 2,
                ascensionMultipliers = new[]
                {
                    1d, 1.05d, 1.10d, 1.16d, 1.23d, 1.31d, 1.40d, 1.50d
                },
                ascensionFragmentCosts = null
            };

            CombatBalanceTuningMigration.UpgradeToCurrent(legacy);

            Assert.That(legacy.maxAscensionLevel, Is.EqualTo(7));
            Assert.That(legacy.ascensionFragmentCosts,
                Is.EqualTo(new long[] { 20, 40, 80, 160, 320, 640, 1280 }));
            Assert.DoesNotThrow(() => HeroBalanceTuningValidator.Validate(legacy));
        }

        [Test]
        public void CombatBalanceTuningMigration_VersionTwoAscensionZeroOnly_RemainsValid()
        {
            double[] customMultipliers = { 1d };
            var legacy = new CombatBalanceTuning
            {
                version = 2,
                ascensionMultipliers = customMultipliers,
                ascensionFragmentCosts = null
            };

            CombatBalanceTuningMigration.UpgradeToCurrent(legacy);

            Assert.That(legacy.ascensionMultipliers, Is.SameAs(customMultipliers));
            Assert.That(legacy.maxAscensionLevel, Is.Zero);
            Assert.That(legacy.ascensionFragmentCosts, Is.Empty);
            Assert.DoesNotThrow(() => HeroBalanceTuningValidator.Validate(legacy));
        }

        private HeroInstance CreateLocked()
        {
            return HeroInstance.CreateLocked(
                "hero_instance_001",
                "hero_paladin_001",
                "player_001",
                Rarity.Common,
                1,
                tuning);
        }

        private HeroInstance RestoreUnlocked(
            int level = 1,
            Rarity rarity = Rarity.Common,
            long ownedFragments = 0)
        {
            return HeroInstance.Restore(
                "hero_instance_001",
                "hero_paladin_001",
                "player_001",
                level,
                0,
                rarity,
                0,
                ownedFragments,
                null,
                true,
                1,
                1,
                null,
                0,
                tuning);
        }
    }
}
