#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Content;

namespace IdleMedievalLegends.Domain.Gacha
{
    public sealed class GachaValidationReport
    {
        private readonly List<string> errors = new List<string>();

        public IReadOnlyList<string> Errors => errors.AsReadOnly();
        public bool IsValid => errors.Count == 0;

        internal void Add(string message)
        {
            errors.Add(message);
        }

        public void ThrowIfInvalid()
        {
            if (!IsValid)
                throw new InvalidOperationException(string.Join(" | ", errors));
        }
    }

    public static class GachaProbabilityValidator
    {
        public static GachaValidationReport Validate(
            GachaBannerDefinition banner,
            ISet<string> existingHeroDefinitionIds)
        {
            var report = new GachaValidationReport();
            if (banner == null)
            {
                report.Add("Banner não pode ser nulo.");
                return report;
            }

            if (string.IsNullOrWhiteSpace(banner.BannerId))
                report.Add("bannerId é obrigatório.");
            if (string.IsNullOrWhiteSpace(banner.DisplayName))
                report.Add("displayName é obrigatório.");
            if (string.IsNullOrWhiteSpace(banner.RulesVersion))
                report.Add("rulesVersion é obrigatório.");
            if (banner.CurrencyType != GachaCurrencyType.DevelopmentGachaCurrency)
                report.Add("Somente DevelopmentGachaCurrency é aceita nesta task.");
            if (banner.CostSingle <= 0 || banner.CostMulti <= 0)
                report.Add("Custos devem ser positivos.");
            if (banner.MultiPullCount <= 1)
                report.Add("multiPullCount deve ser maior que um.");
            if (banner.SoftPityStart <= 0 || banner.HardPityThreshold <= 0)
                report.Add("Pity deve usar thresholds positivos.");
            if (banner.HardPityThreshold < banner.SoftPityStart)
                report.Add("hard pity não pode ser menor que soft pity.");
            if (banner.SoftPityIncreaseBasisPoints < 0)
                report.Add("Aumento de soft pity não pode ser negativo.");
            if (!Enum.IsDefined(typeof(Rarity), banner.HighRarityThreshold))
                report.Add("Raridade qualificadora inválida.");
            if (banner.PityScope == GachaPityScope.Group &&
                string.IsNullOrWhiteSpace(banner.PityGroupId))
            {
                report.Add("pityGroupId é obrigatório para pity por grupo.");
            }
            if (banner.StartTime.HasValue && banner.EndTime.HasValue &&
                banner.EndTime.Value <= banner.StartTime.Value)
            {
                report.Add("endTime deve ser posterior a startTime.");
            }
            if (banner.PoolEntries.Count == 0)
            {
                report.Add("Pool vazio.");
                return report;
            }

            var entryIds = new HashSet<string>(StringComparer.Ordinal);
            var featuredIds = new HashSet<string>(
                banner.FeaturedEntryIds,
                StringComparer.Ordinal);
            long totalWeight = 0;
            bool hasHighRarity = false;
            bool hasFeaturedHighRarity = false;
            int fragmentEntries = 0;
            for (int i = 0; i < banner.PoolEntries.Count; i++)
            {
                GachaPoolEntry entry = banner.PoolEntries[i];
                if (entry == null)
                {
                    report.Add($"Entrada {i} é nula.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(entry.EntryId) ||
                    !entryIds.Add(entry.EntryId))
                {
                    report.Add($"entryId vazio ou duplicado na posição {i}.");
                }
                if (entry.Weight <= 0)
                    report.Add($"Peso inválido em {entry.EntryId}.");
                else
                    totalWeight = checked(totalWeight + entry.Weight);
                if (!Enum.IsDefined(typeof(Rarity), entry.Rarity))
                    report.Add($"Raridade inválida em {entry.EntryId}.");
                if (entry.MinimumPlayerProgress.HasValue &&
                    entry.MinimumPlayerProgress.Value < 0)
                {
                    report.Add($"minimumPlayerProgress inválido em {entry.EntryId}.");
                }
                bool validRewardType =
                    Enum.IsDefined(typeof(GachaRewardType), entry.RewardType);
                if (!validRewardType)
                {
                    report.Add($"rewardType inválido em {entry.EntryId}.");
                }
                else if (entry.RewardType == GachaRewardType.HeroFragments)
                {
                    fragmentEntries++;
                    if (entry.FragmentQuantity <= 0)
                        report.Add($"Quantidade de fragmentos inválida em {entry.EntryId}.");
                    ValidateHeroId(entry, existingHeroDefinitionIds, report);
                }
                else if (entry.RewardType == GachaRewardType.DirectHeroUnlock)
                {
                    if (entry.FragmentQuantity != 0)
                        report.Add($"Desbloqueio direto não deve carregar fragmentos em {entry.EntryId}.");
                    ValidateHeroId(entry, existingHeroDefinitionIds, report);
                }
                else if (entry.FragmentQuantity <= 0)
                {
                    report.Add($"Quantidade auxiliar inválida em {entry.EntryId}.");
                }
                if (!Enum.IsDefined(typeof(GachaDuplicateRule), entry.DuplicateRule))
                    report.Add($"duplicateRule inválida em {entry.EntryId}.");

                bool isFeatured = entry.Featured || featuredIds.Contains(entry.EntryId);
                if (validRewardType &&
                    entry.Rarity >= banner.HighRarityThreshold)
                {
                    hasHighRarity = true;
                    hasFeaturedHighRarity |= isFeatured;
                }
            }

            if (totalWeight <= 0)
                report.Add("Soma de pesos deve ser positiva.");
            if (!hasHighRarity)
                report.Add("Pool não possui recompensa para o hard pity.");
            if (fragmentEntries <= banner.PoolEntries.Count / 2)
                report.Add("Fragmentos devem ser a recompensa principal do pool.");
            if (banner.FeaturedGuaranteeEnabled && !hasFeaturedHighRarity)
                report.Add("Featured guarantee exige destaque de alta raridade.");

            foreach (string featuredId in featuredIds)
            {
                if (!entryIds.Contains(featuredId))
                    report.Add($"featured entry inexistente: {featuredId}.");
            }

            for (int i = 0; i < banner.DuplicateConversionRules.Count; i++)
            {
                GachaDuplicateConversionRule rule = banner.DuplicateConversionRules[i];
                if (rule == null || !Enum.IsDefined(typeof(Rarity), rule.Rarity) ||
                    rule.FragmentQuantity <= 0)
                {
                    report.Add("Regra de conversão de duplicata inválida.");
                }
            }
            return report;
        }

        public static void ValidateRequest(
            GachaBannerDefinition banner,
            GachaPullRequest request)
        {
            if (banner == null) throw new ArgumentNullException(nameof(banner));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.RequestId))
                throw new ArgumentException("requestId é obrigatório.", nameof(request));
            if (!string.Equals(
                    request.BannerId,
                    banner.BannerId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Request referencia outro banner.");
            }
            if (request.PullCount != 1 && request.PullCount != banner.MultiPullCount)
                throw new InvalidOperationException("Quantidade de pulls inválida.");
            if (!request.HasExplicitSeed)
                throw new InvalidOperationException(
                    "O simulador exige seed explícita. Produção deve gerar a seed no servidor.");
            if (request.PlayerProgress < 0 || request.LogicalTimestamp < 0)
                throw new InvalidOperationException("Request possui progresso ou timestamp inválido.");
            if (!banner.Enabled)
                throw new InvalidOperationException("Banner desabilitado.");
            if (banner.StartTime.HasValue &&
                request.LogicalTimestamp < banner.StartTime.Value)
            {
                throw new InvalidOperationException("Banner ainda não iniciou.");
            }
            if (banner.EndTime.HasValue &&
                request.LogicalTimestamp >= banner.EndTime.Value)
            {
                throw new InvalidOperationException("Banner expirado.");
            }
        }

        private static void ValidateHeroId(
            GachaPoolEntry entry,
            ISet<string> existingHeroDefinitionIds,
            GachaValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(entry.HeroDefinitionId))
            {
                report.Add($"heroDefinitionId obrigatório em {entry.EntryId}.");
                return;
            }
            if (existingHeroDefinitionIds != null &&
                !existingHeroDefinitionIds.Contains(entry.HeroDefinitionId))
            {
                report.Add($"ID de herói inexistente em {entry.EntryId}: " +
                           entry.HeroDefinitionId);
            }
        }
    }
}
#endif
