using System;
using IdleMedievalLegends.Domain.Common;

namespace IdleMedievalLegends.Domain.Crafting
{
    [Serializable]
    public sealed class ProfessionProgressionTuning
    {
        public int version = 1;
        public int maximumLevel = 100;
        public double experienceBase = 100d;
        public double experienceExponent = 1.55d;
        public double primaryProfessionExperienceBonus = 0.20d;
        public double primaryProfessionDurationReduction = 0.15d;
        public double stationSurplusDurationReductionPerTier = 0.05d;
        public double minimumDurationMultiplier = 0.25d;
        public double firstCraftDiscoveryBonus = 1.00d;

        // T1..T9: 1, 10, 20, 30, 40, 52, 64, 76, 90.
        public int[] tierUnlockLevels = { 1, 10, 20, 30, 40, 52, 64, 76, 90 };

        // Aprendiz, Proficiente, Mestre, Grão-Mestre, Deus.
        public int[] rankStartLevels = { 1, 20, 40, 64, 90 };

        // Penalidade de XP ao repetir receitas muito abaixo do maior Tier liberado.
        public double[] obsoleteTierExperienceMultipliers = { 1.00d, 0.65d, 0.25d, 0.05d };
    }

    public static class ProfessionProgression
    {
        public static ProfessionRank GetRankForLevel(
            int level,
            ProfessionProgressionTuning tuning)
        {
            ValidateTuning(tuning);
            int clamped = Clamp(level, 1, tuning.maximumLevel);

            if (clamped >= tuning.rankStartLevels[4]) return ProfessionRank.God;
            if (clamped >= tuning.rankStartLevels[3]) return ProfessionRank.Grandmaster;
            if (clamped >= tuning.rankStartLevels[2]) return ProfessionRank.Master;
            if (clamped >= tuning.rankStartLevels[1]) return ProfessionRank.Proficient;
            return ProfessionRank.Apprentice;
        }

        public static ItemTier GetMaximumUnlockedTier(
            int level,
            ProfessionProgressionTuning tuning)
        {
            ValidateTuning(tuning);
            int clamped = Clamp(level, 1, tuning.maximumLevel);
            int unlocked = 1;

            for (int i = 0; i < tuning.tierUnlockLevels.Length; i++)
            {
                if (clamped < tuning.tierUnlockLevels[i])
                    break;

                unlocked = i + 1;
            }

            return ProgressionTypes.FromTierNumber(unlocked);
        }

        public static long GetExperienceRequiredForNextLevel(
            int currentLevel,
            ProfessionProgressionTuning tuning)
        {
            ValidateTuning(tuning);
            if (currentLevel >= tuning.maximumLevel)
                return 0;

            int clamped = Clamp(currentLevel, 1, tuning.maximumLevel - 1);
            double raw = tuning.experienceBase *
                         Math.Pow(clamped, tuning.experienceExponent);

            if (raw >= long.MaxValue)
                return long.MaxValue;

            return Math.Max(1L, (long)Math.Round(raw, MidpointRounding.AwayFromZero));
        }

        public static long CalculateCraftExperience(
            CraftingRecipeData recipe,
            ProfessionProgressData progress,
            ItemTier highestUnlockedTier,
            bool isPrimaryProfession,
            bool isFirstCraftOfRecipe,
            ProfessionProgressionTuning tuning)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            ValidateTuning(tuning);
            if (recipe.Profession != progress.Profession)
                throw new InvalidOperationException("Receita e progresso pertencem a profissões diferentes.");
            if (!highestUnlockedTier.IsValid())
                throw new ArgumentOutOfRangeException(nameof(highestUnlockedTier));

            int tierGap = Math.Max(0, highestUnlockedTier.ToNumber() - recipe.Tier.ToNumber());
            int multiplierIndex = Math.Min(
                tierGap,
                tuning.obsoleteTierExperienceMultipliers.Length - 1);

            double multiplier = tuning.obsoleteTierExperienceMultipliers[multiplierIndex];
            if (isPrimaryProfession)
                multiplier *= 1d + tuning.primaryProfessionExperienceBonus;
            if (isFirstCraftOfRecipe)
                multiplier *= 1d + tuning.firstCraftDiscoveryBonus;

            double raw = recipe.BaseProfessionExperience * multiplier;
            if (raw <= 0d) return 0;
            if (raw >= long.MaxValue) return long.MaxValue;

            return Math.Max(1L, (long)Math.Round(raw, MidpointRounding.AwayFromZero));
        }

        public static int CalculateCraftDurationSeconds(
            CraftingRecipeData recipe,
            ProfessionProgressData progress,
            CraftingProfession primaryProfession,
            int toolDurationReductionBasisPoints,
            ProfessionProgressionTuning tuning)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            ValidateTuning(tuning);
            if (recipe.Profession != progress.Profession)
                throw new InvalidOperationException("Receita e progresso pertencem a profissões diferentes.");

            int stationSurplus = Math.Max(
                0,
                progress.StationTier.ToNumber() - recipe.RequiredStationTier.ToNumber());

            double reduction = stationSurplus *
                               tuning.stationSurplusDurationReductionPerTier;
            if (progress.Profession == primaryProfession)
                reduction += tuning.primaryProfessionDurationReduction;
            reduction += Math.Max(0, toolDurationReductionBasisPoints) / 10000d;

            double multiplier = Math.Max(
                tuning.minimumDurationMultiplier,
                Math.Min(1d, 1d - reduction));

            double rawDuration = recipe.BaseDurationSeconds * multiplier;
            if (rawDuration >= int.MaxValue)
                return int.MaxValue;

            return Math.Max(0, (int)Math.Ceiling(rawDuration));
        }

        public static bool IsRankCompatibleWithTier(ProfessionRank rank, ItemTier tier)
        {
            int rankValue = (int)rank;

            switch (tier)
            {
                case ItemTier.Tier1:
                case ItemTier.Tier2:
                    return rankValue >= (int)ProfessionRank.Apprentice;
                case ItemTier.Tier3:
                case ItemTier.Tier4:
                    return rankValue >= (int)ProfessionRank.Proficient;
                case ItemTier.Tier5:
                case ItemTier.Tier6:
                    return rankValue >= (int)ProfessionRank.Master;
                case ItemTier.Tier7:
                case ItemTier.Tier8:
                    return rankValue >= (int)ProfessionRank.Grandmaster;
                case ItemTier.Tier9:
                    return rankValue >= (int)ProfessionRank.God;
                default:
                    return false;
            }
        }

        public static void ValidateTuning(ProfessionProgressionTuning tuning)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (tuning.maximumLevel < 1)
                throw new InvalidOperationException("maximumLevel deve ser positivo.");
            if (tuning.experienceBase <= 0d || tuning.experienceExponent <= 0d)
                throw new InvalidOperationException("Curva de XP deve ser positiva.");
            if (tuning.primaryProfessionExperienceBonus < 0d ||
                tuning.primaryProfessionDurationReduction < 0d ||
                tuning.stationSurplusDurationReductionPerTier < 0d)
            {
                throw new InvalidOperationException("Bônus de profissão não podem ser negativos.");
            }
            if (tuning.minimumDurationMultiplier <= 0d ||
                tuning.minimumDurationMultiplier > 1d)
            {
                throw new InvalidOperationException(
                    "minimumDurationMultiplier deve estar em (0, 1].");
            }
            if (tuning.tierUnlockLevels == null || tuning.tierUnlockLevels.Length != 9)
                throw new InvalidOperationException("tierUnlockLevels deve conter T1..T9.");
            if (tuning.rankStartLevels == null || tuning.rankStartLevels.Length != 5)
                throw new InvalidOperationException("rankStartLevels deve conter cinco graus.");
            if (tuning.obsoleteTierExperienceMultipliers == null ||
                tuning.obsoleteTierExperienceMultipliers.Length < 1)
            {
                throw new InvalidOperationException(
                    "obsoleteTierExperienceMultipliers não pode estar vazio.");
            }

            ValidateAscendingThresholds(
                tuning.tierUnlockLevels,
                tuning.maximumLevel,
                "tierUnlockLevels");
            ValidateAscendingThresholds(
                tuning.rankStartLevels,
                tuning.maximumLevel,
                "rankStartLevels");

            for (int i = 0; i < tuning.obsoleteTierExperienceMultipliers.Length; i++)
            {
                if (tuning.obsoleteTierExperienceMultipliers[i] < 0d)
                    throw new InvalidOperationException("Multiplicador de XP não pode ser negativo.");
            }
        }

        private static void ValidateAscendingThresholds(
            int[] thresholds,
            int maximumLevel,
            string name)
        {
            int previous = 0;
            for (int i = 0; i < thresholds.Length; i++)
            {
                int value = thresholds[i];
                if (value <= previous || value > maximumLevel)
                {
                    throw new InvalidOperationException(
                        $"{name} deve ser estritamente crescente e <= maximumLevel.");
                }
                previous = value;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
    }
}
