using System;
using IdleMedievalLegends.Domain.Common;

namespace IdleMedievalLegends.Domain.Equipment
{
    [Serializable]
    public sealed class EquipmentBalanceTuning
    {
        public int version = 1;
        public double tierGrowth = 1.45d;
        public double enhancementBudgetPerLevel = 0.035d;
        public double[] rarityBudgetMultipliers =
        {
            1.00d, 1.06d, 1.14d, 1.24d, 1.36d, 1.50d
        };
        public int[] maximumEnhancementLevels = { 3, 5, 7, 10, 13, 15 };
        public int[] affixCounts = { 0, 1, 2, 3, 4, 5 };
    }

    public static class EquipmentBudgetCalculator
    {
        public static long CalculateStatBudget(
            long baseSlotBudget,
            ItemTier tier,
            GameRarity rarity,
            int enhancementLevel,
            EquipmentBalanceTuning tuning)
        {
            Validate(tuning);
            if (baseSlotBudget < 0) throw new ArgumentOutOfRangeException(nameof(baseSlotBudget));
            if (!tier.IsValid()) throw new ArgumentOutOfRangeException(nameof(tier));
            if (!rarity.IsValid()) throw new ArgumentOutOfRangeException(nameof(rarity));

            int rarityIndex = (int)rarity;
            int clampedEnhancement = Math.Max(
                0,
                Math.Min(enhancementLevel, tuning.maximumEnhancementLevels[rarityIndex]));

            double tierMultiplier = Math.Pow(tuning.tierGrowth, tier.ToNumber() - 1);
            double enhancementMultiplier =
                1d + clampedEnhancement * tuning.enhancementBudgetPerLevel;

            double raw = baseSlotBudget *
                         tierMultiplier *
                         tuning.rarityBudgetMultipliers[rarityIndex] *
                         enhancementMultiplier;

            if (raw <= 0d) return 0;
            if (raw >= long.MaxValue) return long.MaxValue;
            return (long)Math.Round(raw, MidpointRounding.AwayFromZero);
        }

        public static int GetMaximumEnhancementLevel(
            GameRarity rarity,
            EquipmentBalanceTuning tuning)
        {
            Validate(tuning);
            if (!rarity.IsValid()) throw new ArgumentOutOfRangeException(nameof(rarity));
            return tuning.maximumEnhancementLevels[(int)rarity];
        }

        public static int GetAffixCount(
            GameRarity rarity,
            EquipmentBalanceTuning tuning)
        {
            Validate(tuning);
            if (!rarity.IsValid()) throw new ArgumentOutOfRangeException(nameof(rarity));
            return tuning.affixCounts[(int)rarity];
        }

        private static void Validate(EquipmentBalanceTuning tuning)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (tuning.tierGrowth <= 1d)
                throw new InvalidOperationException("tierGrowth deve ser maior que 1.");
            if (tuning.rarityBudgetMultipliers == null ||
                tuning.rarityBudgetMultipliers.Length != 6)
            {
                throw new InvalidOperationException("São necessários seis multiplicadores de raridade.");
            }
            if (tuning.maximumEnhancementLevels == null ||
                tuning.maximumEnhancementLevels.Length != 6)
            {
                throw new InvalidOperationException("São necessários seis limites de aprimoramento.");
            }
            if (tuning.affixCounts == null || tuning.affixCounts.Length != 6)
                throw new InvalidOperationException("São necessárias seis contagens de afixos.");
        }
    }
}
