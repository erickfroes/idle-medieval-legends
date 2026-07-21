using System.Collections.Generic;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Equipment;
using IdleMedievalLegends.Domain.Inventory;
using NUnit.Framework;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class CraftingProgressionTests
    {
        private static readonly ProfessionProgressionTuning Tuning =
            new ProfessionProgressionTuning();

        [TestCase(1, ProfessionRank.Apprentice, ItemTier.Tier1)]
        [TestCase(10, ProfessionRank.Apprentice, ItemTier.Tier2)]
        [TestCase(20, ProfessionRank.Proficient, ItemTier.Tier3)]
        [TestCase(40, ProfessionRank.Master, ItemTier.Tier5)]
        [TestCase(64, ProfessionRank.Grandmaster, ItemTier.Tier7)]
        [TestCase(90, ProfessionRank.God, ItemTier.Tier9)]
        public void Level_MapsToExpectedRankAndTier(
            int level,
            ProfessionRank expectedRank,
            ItemTier expectedTier)
        {
            Assert.That(
                ProfessionProgression.GetRankForLevel(level, Tuning),
                Is.EqualTo(expectedRank));
            Assert.That(
                ProfessionProgression.GetMaximumUnlockedTier(level, Tuning),
                Is.EqualTo(expectedTier));
        }

        [Test]
        public void MythicCrafting_RequiresTierNineGodAndDivineCatalyst()
        {
            Assert.That(
                CraftingRules.GetMaximumRarity(
                    ItemTier.Tier9,
                    ProfessionRank.God,
                    true),
                Is.EqualTo(GameRarity.Mythic));

            Assert.That(
                CraftingRules.GetMaximumRarity(
                    ItemTier.Tier9,
                    ProfessionRank.God,
                    false),
                Is.EqualTo(GameRarity.Legendary));

            Assert.That(
                CraftingRules.GetMaximumRarity(
                    ItemTier.Tier8,
                    ProfessionRank.God,
                    true),
                Is.EqualTo(GameRarity.Legendary));
        }

        [Test]
        public void RarityWeights_AlwaysSumToTenThousandAfterCap()
        {
            RarityWeightSet weights = CraftingRules.BuildRarityWeights(
                CraftingQualityBand.Divine,
                GameRarity.Epic);

            int total = 0;
            foreach (GameRarity rarity in new[]
                     {
                         GameRarity.Common,
                         GameRarity.Uncommon,
                         GameRarity.Rare,
                         GameRarity.Epic,
                         GameRarity.Legendary,
                         GameRarity.Mythic
                     })
            {
                total += weights.GetWeight(rarity);
            }

            Assert.That(total, Is.EqualTo(10000));
            Assert.That(weights.GetWeight(GameRarity.Legendary), Is.Zero);
            Assert.That(weights.GetWeight(GameRarity.Mythic), Is.Zero);
        }

        [Test]
        public void ValidBlacksmithRecipe_CanStart()
        {
            CraftingRecipeData recipe = CreateTierFiveBlacksmithRecipe();
            ProfessionProgressData progress = CreateTierFiveBlacksmithProgress();

            CraftingEligibilityResult result = CraftingRules.CanStartRecipe(
                recipe,
                progress,
                CraftingProfession.Blacksmith,
                true,
                20,
                1);

            Assert.That(result.IsAllowed, Is.True, result.Message);
        }

        [Test]
        public void PrimaryProfession_ReducesDurationButDoesNotBypassRequirements()
        {
            CraftingRecipeData recipe = CreateTierFiveBlacksmithRecipe();
            ProfessionProgressData progress = CreateTierFiveBlacksmithProgress();

            int primaryDuration = ProfessionProgression.CalculateCraftDurationSeconds(
                recipe,
                progress,
                CraftingProfession.Blacksmith,
                0,
                Tuning);

            int secondaryDuration = ProfessionProgression.CalculateCraftDurationSeconds(
                recipe,
                progress,
                CraftingProfession.Tailor,
                0,
                Tuning);

            Assert.That(primaryDuration, Is.EqualTo(3060));
            Assert.That(secondaryDuration, Is.EqualTo(3600));
            Assert.That(primaryDuration, Is.LessThan(secondaryDuration));
        }

        [Test]
        public void MythicPity_AddsSoftBonusAndGuaranteesHundredthEligibleCraft()
        {
            var pity = new CraftingPityTuning();

            RarityWeightSet beforeSoftPity =
                CraftingRules.BuildRarityWeightsWithMythicPity(
                    CraftingQualityBand.Divine,
                    GameRarity.Mythic,
                    49,
                    pity);

            RarityWeightSet firstSoftPityCraft =
                CraftingRules.BuildRarityWeightsWithMythicPity(
                    CraftingQualityBand.Divine,
                    GameRarity.Mythic,
                    50,
                    pity);

            RarityWeightSet hardPityCraft =
                CraftingRules.BuildRarityWeightsWithMythicPity(
                    CraftingQualityBand.Divine,
                    GameRarity.Mythic,
                    99,
                    pity);

            Assert.That(
                beforeSoftPity.GetWeight(GameRarity.Mythic),
                Is.EqualTo(100));
            Assert.That(
                firstSoftPityCraft.GetWeight(GameRarity.Mythic),
                Is.EqualTo(105));
            Assert.That(
                hardPityCraft.GetWeight(GameRarity.Mythic),
                Is.EqualTo(10000));
        }

        [Test]
        public void EquipmentBudget_UsesTierRarityAndEnhancement()
        {
            var tuning = new EquipmentBalanceTuning();

            long commonT1 = EquipmentBudgetCalculator.CalculateStatBudget(
                100,
                ItemTier.Tier1,
                GameRarity.Common,
                0,
                tuning);

            long mythicT9 = EquipmentBudgetCalculator.CalculateStatBudget(
                100,
                ItemTier.Tier9,
                GameRarity.Mythic,
                15,
                tuning);

            Assert.That(commonT1, Is.EqualTo(100));
            Assert.That(mythicT9, Is.GreaterThan(commonT1));
            Assert.That(
                EquipmentBudgetCalculator.GetAffixCount(GameRarity.Mythic, tuning),
                Is.EqualTo(5));
        }

        [TestCase(100, ProfessionRank.Apprentice, ItemTier.Tier1)]
        [TestCase(1, ProfessionRank.God, ItemTier.Tier9)]
        public void ProfessionProgress_InconsistentDerivedCaches_Throws(
            int level,
            ProfessionRank rank,
            ItemTier maxUnlockedTier)
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                new ProfessionProgressData(
                    CraftingProfession.Blacksmith,
                    level,
                    0,
                    rank,
                    maxUnlockedTier,
                    ItemTier.Tier1,
                    0,
                    0,
                    0,
                    1));
        }

        private static CraftingRecipeData CreateTierFiveBlacksmithRecipe()
        {
            return new CraftingRecipeData(
                "blacksmith_sword_t5",
                CraftingProfession.Blacksmith,
                CraftingOperationType.CraftEquipment,
                ItemTier.Tier5,
                ProfessionRank.Master,
                40,
                ItemTier.Tier5,
                "sword_t5",
                InventoryItemKind.Equipment,
                1,
                3600,
                8,
                500,
                true,
                true,
                new List<CraftingIngredientData>());
        }

        private static ProfessionProgressData CreateTierFiveBlacksmithProgress()
        {
            return new ProfessionProgressData(
                CraftingProfession.Blacksmith,
                40,
                100000,
                ProfessionRank.Master,
                ItemTier.Tier5,
                ItemTier.Tier5,
                100,
                0,
                0,
                10);
        }
    }
}
