using System;
using IdleMedievalLegends.Domain.Common;

namespace IdleMedievalLegends.Domain.Crafting
{
    public enum CraftingEligibilityCode
    {
        Allowed = 0,
        InvalidRecipe = 1,
        WrongProfession = 2,
        ProfessionLevelTooLow = 3,
        RankTooLow = 4,
        TierLocked = 5,
        StationTierTooLow = 6,
        RecipeLocked = 7,
        InsufficientFocus = 8,
        InvalidQuantity = 9
    }

    public readonly struct CraftingEligibilityResult
    {
        public bool IsAllowed { get; }
        public CraftingEligibilityCode Code { get; }
        public string Message { get; }

        public CraftingEligibilityResult(
            bool isAllowed,
            CraftingEligibilityCode code,
            string message)
        {
            IsAllowed = isAllowed;
            Code = code;
            Message = message ?? string.Empty;
        }

        public static CraftingEligibilityResult Allowed()
        {
            return new CraftingEligibilityResult(
                true,
                CraftingEligibilityCode.Allowed,
                string.Empty);
        }

        public static CraftingEligibilityResult Rejected(
            CraftingEligibilityCode code,
            string message)
        {
            return new CraftingEligibilityResult(false, code, message);
        }
    }

    public enum CraftingQualityBand
    {
        Standard = 0,
        Skilled = 1,
        Expert = 2,
        Mastered = 3,
        Divine = 4
    }

    [Serializable]
    public sealed class CraftingPityTuning
    {
        public int softPityFailures = 50;
        public int hardPityFailures = 100;
        public int mythicBonusBasisPointsPerFailure = 5;
    }

    public sealed class RarityWeightSet
    {
        private readonly int[] weights;

        public RarityWeightSet(int[] weights)
        {
            if (weights == null || weights.Length != 6)
                throw new ArgumentException("São necessários seis pesos.", nameof(weights));

            this.weights = (int[])weights.Clone();
            int total = 0;
            for (int i = 0; i < this.weights.Length; i++)
            {
                if (this.weights[i] < 0)
                    throw new ArgumentOutOfRangeException(nameof(weights));
                total = checked(total + this.weights[i]);
            }

            if (total != 10000)
                throw new ArgumentException("Os pesos devem somar 10000 basis points.", nameof(weights));
        }

        public int GetWeight(GameRarity rarity)
        {
            if (!rarity.IsValid()) throw new ArgumentOutOfRangeException(nameof(rarity));
            return weights[(int)rarity];
        }

        public int[] ToArray()
        {
            return (int[])weights.Clone();
        }
    }

    public static class CraftingRules
    {
        private static readonly int[][] QualityWeights =
        {
            new[] { 6000, 2800, 1000,  200,    0,   0 },
            new[] { 4000, 3400, 1900,  650,   50,   0 },
            new[] { 2200, 3000, 3000, 1550,  250,   0 },
            new[] {  800, 1800, 3300, 3200,  850,  50 },
            new[] {  200,  800, 2600, 3500, 2800, 100 }
        };

        public static CraftingEligibilityResult CanStartRecipe(
            CraftingRecipeData recipe,
            ProfessionProgressData progress,
            CraftingProfession primaryProfession,
            bool recipeIsUnlocked,
            int focusAvailable,
            int quantity)
        {
            if (recipe == null || progress == null)
            {
                return CraftingEligibilityResult.Rejected(
                    CraftingEligibilityCode.InvalidRecipe,
                    "Receita ou progresso ausente.");
            }

            try
            {
                recipe.Validate();
                progress.Validate();
            }
            catch (Exception exception)
            {
                return CraftingEligibilityResult.Rejected(
                    CraftingEligibilityCode.InvalidRecipe,
                    exception.Message);
            }

            if (quantity <= 0)
            {
                return CraftingEligibilityResult.Rejected(
                    CraftingEligibilityCode.InvalidQuantity,
                    "Quantidade deve ser positiva.");
            }

            if (recipe.Profession != progress.Profession)
            {
                return CraftingEligibilityResult.Rejected(
                    CraftingEligibilityCode.WrongProfession,
                    "A receita pertence a outra profissão.");
            }

            if (progress.Level < recipe.MinimumProfessionLevel)
            {
                return CraftingEligibilityResult.Rejected(
                    CraftingEligibilityCode.ProfessionLevelTooLow,
                    "Nível de profissão insuficiente.");
            }

            if ((int)progress.Rank < (int)recipe.MinimumRank ||
                !ProfessionProgression.IsRankCompatibleWithTier(progress.Rank, recipe.Tier))
            {
                return CraftingEligibilityResult.Rejected(
                    CraftingEligibilityCode.RankTooLow,
                    "Grau profissional insuficiente.");
            }

            if (progress.MaxUnlockedTier.ToNumber() < recipe.Tier.ToNumber())
            {
                return CraftingEligibilityResult.Rejected(
                    CraftingEligibilityCode.TierLocked,
                    "Tier da receita ainda não foi liberado.");
            }

            if (progress.StationTier.ToNumber() < recipe.RequiredStationTier.ToNumber())
            {
                return CraftingEligibilityResult.Rejected(
                    CraftingEligibilityCode.StationTierTooLow,
                    "Estação de crafting precisa ser melhorada.");
            }

            if (recipe.RequiresUnlock && !recipeIsUnlocked)
            {
                return CraftingEligibilityResult.Rejected(
                    CraftingEligibilityCode.RecipeLocked,
                    "Diagrama ou receita ainda não foi desbloqueado.");
            }

            long totalFocus = (long)recipe.FocusCost * quantity;
            if (totalFocus > focusAvailable)
            {
                return CraftingEligibilityResult.Rejected(
                    CraftingEligibilityCode.InsufficientFocus,
                    "Foco artesanal insuficiente.");
            }

            // A profissão primária melhora eficiência/qualidade, mas nunca ignora
            // nível, grau, Tier, diagrama ou estação.
            _ = primaryProfession;
            return CraftingEligibilityResult.Allowed();
        }

        public static CraftingQualityBand CalculateQualityBand(
            ProfessionProgressData progress,
            CraftingRecipeData recipe,
            CraftingProfession primaryProfession,
            int toolBonusLevels,
            int catalystQualityBasisPoints)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));

            int levelMargin = Math.Max(0, progress.Level - recipe.MinimumProfessionLevel);
            int stationSurplus = Math.Max(
                0,
                progress.StationTier.ToNumber() - recipe.RequiredStationTier.ToNumber());

            long score = levelMargin +
                         stationSurplus * 8L +
                         Math.Max(0, toolBonusLevels) +
                         Math.Max(0, catalystQualityBasisPoints) / 500L;

            if (progress.Profession == primaryProfession)
                score += 5;

            if (progress.Rank == ProfessionRank.God && score >= 35)
                return CraftingQualityBand.Divine;
            if (score >= 25) return CraftingQualityBand.Mastered;
            if (score >= 15) return CraftingQualityBand.Expert;
            if (score >= 5) return CraftingQualityBand.Skilled;
            return CraftingQualityBand.Standard;
        }

        public static GameRarity GetMaximumRarity(
            ItemTier tier,
            ProfessionRank rank,
            bool hasDivineCatalyst)
        {
            if (!tier.IsValid()) throw new ArgumentOutOfRangeException(nameof(tier));
            if (!Enum.IsDefined(typeof(ProfessionRank), rank))
                throw new ArgumentOutOfRangeException(nameof(rank));

            switch (tier)
            {
                case ItemTier.Tier1:
                case ItemTier.Tier2:
                    return GameRarity.Rare;
                case ItemTier.Tier3:
                case ItemTier.Tier4:
                    return GameRarity.Epic;
                case ItemTier.Tier5:
                case ItemTier.Tier6:
                case ItemTier.Tier7:
                case ItemTier.Tier8:
                    return GameRarity.Legendary;
                case ItemTier.Tier9:
                    return rank == ProfessionRank.God && hasDivineCatalyst
                        ? GameRarity.Mythic
                        : GameRarity.Legendary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        public static RarityWeightSet BuildRarityWeights(
            CraftingQualityBand band,
            GameRarity maximumRarity)
        {
            if (!maximumRarity.IsValid())
                throw new ArgumentOutOfRangeException(nameof(maximumRarity));

            int bandIndex = (int)band;
            if (bandIndex < 0 || bandIndex >= QualityWeights.Length)
                throw new ArgumentOutOfRangeException(nameof(band));

            int[] result = (int[])QualityWeights[bandIndex].Clone();
            int capIndex = (int)maximumRarity;

            for (int i = capIndex + 1; i < result.Length; i++)
            {
                result[capIndex] = checked(result[capIndex] + result[i]);
                result[i] = 0;
            }

            return new RarityWeightSet(result);
        }

        public static RarityWeightSet BuildRarityWeightsWithMythicPity(
            CraftingQualityBand band,
            GameRarity maximumRarity,
            int previousEligibleFailures,
            CraftingPityTuning tuning)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (previousEligibleFailures < 0)
                throw new ArgumentOutOfRangeException(nameof(previousEligibleFailures));
            if (tuning.softPityFailures < 0 ||
                tuning.hardPityFailures <= tuning.softPityFailures ||
                tuning.mythicBonusBasisPointsPerFailure < 0)
            {
                throw new InvalidOperationException("Configuração de pity inválida.");
            }

            RarityWeightSet baseWeights = BuildRarityWeights(band, maximumRarity);
            if (maximumRarity != GameRarity.Mythic)
                return baseWeights;

            // Com 99 falhas anteriores, a próxima é a 100ª tentativa elegível.
            if (previousEligibleFailures >= tuning.hardPityFailures - 1)
                return new RarityWeightSet(new[] { 0, 0, 0, 0, 0, 10000 });

            int failuresPastSoft = Math.Max(
                0,
                previousEligibleFailures - tuning.softPityFailures + 1);
            long rawBonus = (long)failuresPastSoft *
                            tuning.mythicBonusBasisPointsPerFailure;
            int bonus = (int)Math.Min(10000L, rawBonus);
            if (bonus <= 0)
                return baseWeights;

            int[] adjusted = baseWeights.ToArray();
            int transferable = Math.Min(
                bonus,
                10000 - adjusted[(int)GameRarity.Mythic]);

            for (int i = (int)GameRarity.Common;
                 i < (int)GameRarity.Mythic && transferable > 0;
                 i++)
            {
                int moved = Math.Min(adjusted[i], transferable);
                adjusted[i] -= moved;
                adjusted[(int)GameRarity.Mythic] += moved;
                transferable -= moved;
            }

            return new RarityWeightSet(adjusted);
        }
    }
}
