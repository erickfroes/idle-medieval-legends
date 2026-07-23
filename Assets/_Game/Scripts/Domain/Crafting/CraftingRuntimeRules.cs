using System;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Content;

namespace IdleMedievalLegends.Domain.Crafting
{
    public sealed class CraftingCommandException : InvalidOperationException
    {
        public CraftingCommandException(CraftingEligibilityCode code, string message)
            : base(message)
        {
            Code = code;
        }

        public CraftingEligibilityCode Code { get; }
    }

    public static class CraftingRuntimeRules
    {
        public static int GetQueueSlotCount(
            ProfessionProgress progress,
            int configuredBonusSlots,
            CraftingRuntimeTuning tuning)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            CraftingRules.ValidateRuntimeTuning(tuning);
            int rankBonus = tuning.queue.rankBonusSlots[(int)progress.Rank];
            int specializationBonus = progress.Specialization == ProfessionSpecialization.Primary &&
                                      progress.Rank >= ProfessionRank.Master
                ? tuning.queue.primaryMasterBonusSlots
                : 0;
            int otherBonus = Math.Min(
                Math.Max(0, configuredBonusSlots), tuning.queue.maximumBonusSlots);
            return checked(tuning.queue.baseSlots + rankBonus + specializationBonus + otherBonus);
        }

        public static int CalculateFocusCost(
            RecipeDefinition recipe,
            int quantity,
            int reductionBasisPoints,
            CraftingRuntimeTuning tuning)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            int allowedReduction = Math.Min(
                Math.Max(0, reductionBasisPoints),
                10000 - tuning.minimumFocusCostBasisPoints);
            long baseCost = checked((long)recipe.FocusCost * quantity);
            return checked((int)((baseCost * (10000 - allowedReduction) + 9999) / 10000));
        }

        public static int CalculateDurationSeconds(
            RecipeDefinition recipe,
            ProfessionProgress progress,
            int additionalReductionBasisPoints,
            CraftingRuntimeTuning tuning)
        {
            int reduction = Math.Max(0, additionalReductionBasisPoints);
            if (progress.Specialization == ProfessionSpecialization.Primary)
                reduction = checked(reduction + tuning.primaryDurationReductionBasisPoints);
            reduction = Math.Min(reduction, 9000);
            long duration = checked((long)recipe.DurationSeconds * (10000 - reduction));
            return checked((int)Math.Max(0, (duration + 9999) / 10000));
        }

        public static int CalculateQualityScore(
            RecipeDefinition recipe,
            ProfessionProgress progress,
            int toolQualityPoints,
            int catalystQualityPoints,
            int masteryQualityPoints,
            CraftingRuntimeTuning tuning)
        {
            int levelMargin = Math.Max(0, progress.Level - recipe.RequiredProfessionLevel);
            int stationMargin = Math.Max(
                0,
                progress.StationTier.ToNumber() - (int)recipe.RequiredStationTier);
            long score = checked(
                (long)levelMargin * tuning.levelMarginQualityPointsPerLevel +
                (long)stationMargin * tuning.stationSurplusQualityPointsPerTier +
                Math.Max(0, toolQualityPoints) + Math.Max(0, catalystQualityPoints) +
                Math.Max(0, masteryQualityPoints));
            if (progress.Specialization == ProfessionSpecialization.Primary)
                score += tuning.primaryQualityBonusPoints;
            return (int)Math.Min(100L, score);
        }

        public static CraftingQualityBand GetQualityBand(
            int qualityScore,
            ProfessionRank rank,
            CraftingRuntimeTuning tuning)
        {
            if (qualityScore >= tuning.divineQualityThreshold && rank == ProfessionRank.God)
                return CraftingQualityBand.Divine;
            if (qualityScore >= tuning.masteredQualityThreshold)
                return CraftingQualityBand.Mastered;
            if (qualityScore >= tuning.expertQualityThreshold)
                return CraftingQualityBand.Expert;
            if (qualityScore >= tuning.skilledQualityThreshold)
                return CraftingQualityBand.Skilled;
            return CraftingQualityBand.Standard;
        }

        public static bool IsMythicEligible(
            RecipeDefinition recipe,
            ProfessionProgress progress,
            bool hasDivineCatalyst)
        {
            return recipe.RequiredTier == ContentTier.Tier9 &&
                   progress.Rank == ProfessionRank.God &&
                   recipe.EligibleForMythicCrafting &&
                   hasDivineCatalyst;
        }

        public static long CalculateExperience(
            RecipeDefinition recipe,
            ProfessionProgress progress,
            int quantity,
            CraftingRuntimeTuning tuning)
        {
            int tier = (int)recipe.RequiredTier;
            int tierGap = Math.Max(0, progress.MaximumUnlockedTier.ToNumber() - tier);
            int gapIndex = Math.Min(
                tierGap,
                tuning.experience.lowerTierMultipliersBasisPoints.Length - 1);
            long baseXp = checked(
                tuning.experience.baseExperienceByTier[tier - 1] +
                (long)(recipe.DurationSeconds / 60) *
                tuning.experience.experiencePerDurationMinute);
            long scaled = checked(baseXp * quantity);
            scaled = MultiplyBasisPoints(
                scaled,
                tuning.experience.lowerTierMultipliersBasisPoints[gapIndex]);
            scaled = MultiplyBasisPoints(scaled, tuning.experience.successMultiplierBasisPoints);
            if (progress.Specialization == ProfessionSpecialization.Primary)
                scaled = MultiplyBasisPoints(
                    scaled,
                    10000 + tuning.primaryExperienceBonusBasisPoints);
            return Math.Max(1, scaled);
        }

        public static long CalculateMasteryExperience(
            RecipeDefinition recipe,
            ProfessionProgress progress,
            int quantity,
            CraftingRuntimeTuning tuning)
        {
            if (progress.Rank < ProfessionRank.Master) return 0;
            int tierGap = progress.MaximumUnlockedTier.ToNumber() - (int)recipe.RequiredTier;
            if (tierGap > 1) return 0;
            return checked(tuning.experience.masteryExperiencePerTier *
                           (int)recipe.RequiredTier * quantity);
        }

        public static long MultiplyBasisPoints(long value, int basisPoints)
        {
            if (value < 0 || basisPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            return checked((value * basisPoints + 5000) / 10000);
        }
    }

    public sealed class DeterministicCraftingRandom
    {
        private ulong state;

        public DeterministicCraftingRandom(long seed)
        {
            state = unchecked((ulong)seed);
            if (state == 0) state = 0x9E3779B97F4A7C15UL;
        }

        public int NextBasisPoints()
        {
            ulong value = NextUInt64();
            return (int)(value % 10000UL);
        }

        private ulong NextUInt64()
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong value = state;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
