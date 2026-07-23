using System;
using IdleMedievalLegends.Domain.Common;

namespace IdleMedievalLegends.Domain.Crafting
{
    [Serializable]
    public sealed class RarityChanceRow
    {
        public int common;
        public int uncommon;
        public int rare;
        public int epic;
        public int legendary;
        public int mythic;

        public RarityChanceRow()
            : this(6000, 2800, 1000, 200, 0, 0)
        {
        }

        public RarityChanceRow(
            int common,
            int uncommon,
            int rare,
            int epic,
            int legendary,
            int mythic)
        {
            this.common = common;
            this.uncommon = uncommon;
            this.rare = rare;
            this.epic = epic;
            this.legendary = legendary;
            this.mythic = mythic;
        }

        public int[] ToArray()
        {
            return new[] { common, uncommon, rare, epic, legendary, mythic };
        }
    }

    [Serializable]
    public sealed class TierRarityChanceTable
    {
        public ItemTier tier = ItemTier.Tier1;
        public GameRarity maximumRarity = GameRarity.Rare;
        public RarityChanceRow standard = new RarityChanceRow();
        public RarityChanceRow skilled = new RarityChanceRow(4000, 3400, 1900, 650, 50, 0);
        public RarityChanceRow expert = new RarityChanceRow(2200, 3000, 3000, 1550, 250, 0);
        public RarityChanceRow mastered = new RarityChanceRow(800, 1800, 3300, 3200, 850, 50);
        public RarityChanceRow divine = new RarityChanceRow(200, 800, 2600, 3500, 2800, 100);

        public RarityChanceRow Get(CraftingQualityBand band)
        {
            switch (band)
            {
                case CraftingQualityBand.Standard: return standard;
                case CraftingQualityBand.Skilled: return skilled;
                case CraftingQualityBand.Expert: return expert;
                case CraftingQualityBand.Mastered: return mastered;
                case CraftingQualityBand.Divine: return divine;
                default: throw new ArgumentOutOfRangeException(nameof(band));
            }
        }
    }

    [Serializable]
    public sealed class CraftingQueueTuning
    {
        public int baseSlots = 2;
        public int[] rankBonusSlots = { 0, 0, 0, 1, 1 };
        public int primaryMasterBonusSlots = 1;
        public int maximumBonusSlots = 4;
    }

    [Serializable]
    public sealed class CraftingCancellationTuning
    {
        public bool enabled = true;
        public int latestCancellationProgressBasisPoints = 5000;
        public bool refundFocus = false;
        public int goldRefundBasisPoints = 5000;
        public int goldPenaltyBasisPoints = 5000;
        public bool returnMaterials = true;
    }

    [Serializable]
    public sealed class CraftingExperienceTuning
    {
        public long[] baseExperienceByTier = { 20, 35, 60, 95, 145, 215, 310, 440, 620 };
        public long experiencePerDurationMinute = 2;
        public int successMultiplierBasisPoints = 10000;
        public int[] lowerTierMultipliersBasisPoints = { 10000, 6500, 2500, 500 };
        public long masteryExperiencePerTier = 10;
        public long masteryExperiencePerPoint = 1000;
    }

    [Serializable]
    public sealed class CraftingRuntimeTuning
    {
        public int rulesVersion = 1;
        public int primaryExperienceBonusBasisPoints = 2000;
        public int primaryDurationReductionBasisPoints = 1500;
        public int primaryQualityBonusPoints = 5;
        public long specializationChangeGoldCost = 10000;
        public long specializationCooldownSeconds = 604800;
        public int minimumFocusCostBasisPoints = 5000;
        public int baseFocusMaximum = 100;
        public int maximumRecipeQuantity = 99;
        public int levelMarginQualityPointsPerLevel = 1;
        public int stationSurplusQualityPointsPerTier = 8;
        public int skilledQualityThreshold = 5;
        public int expertQualityThreshold = 15;
        public int masteredQualityThreshold = 25;
        public int divineQualityThreshold = 35;
        public string divineCatalystDefinitionId = "material_divine_catalyst_t9";
        public TierRarityChanceTable[] rarityTables = CreateDefaultRarityTables();
        public CraftingQueueTuning queue = new CraftingQueueTuning();
        public CraftingCancellationTuning cancellation = new CraftingCancellationTuning();
        public CraftingExperienceTuning experience = new CraftingExperienceTuning();
        public CraftingPityTuning pity = new CraftingPityTuning();

        private static TierRarityChanceTable[] CreateDefaultRarityTables()
        {
            var tables = new TierRarityChanceTable[9];
            for (int i = 0; i < tables.Length; i++)
            {
                tables[i] = new TierRarityChanceTable
                {
                    tier = (ItemTier)(i + 1),
                    maximumRarity = i < 2
                        ? GameRarity.Rare
                        : i < 4 ? GameRarity.Epic : GameRarity.Legendary
                };
            }
            tables[8].maximumRarity = GameRarity.Mythic;
            return tables;
        }
    }
}
