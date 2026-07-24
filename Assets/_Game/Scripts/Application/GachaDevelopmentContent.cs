#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Gacha;

namespace IdleMedievalLegends.Application
{
    /// <summary>
    /// Conteúdo demonstrativo da Task 011. Não é catálogo ou autoridade de produção.
    /// </summary>
    public static class GachaDevelopmentContent
    {
        public const string BannerId = "development_fragment_banner_001";
        public const string RulesVersion = "task_011_dev_rules_v1";

        public static GachaBannerDefinition CreateMainBanner()
        {
            return new GachaBannerDefinition(
                BannerId,
                "Invocação de Fragmentos — Desenvolvimento",
                "Banner sem Gemas, compras ou autoridade econômica.",
                null,
                null,
                true,
                GachaCurrencyType.DevelopmentGachaCurrency,
                100,
                900,
                10,
                new[]
                {
                    new GachaPoolEntry(
                        "paladin_epic_fragments",
                        GachaRewardType.HeroFragments,
                        "hero_paladin_001",
                        10,
                        Rarity.Epic,
                        1250,
                        true,
                        null,
                        GachaDuplicateRule.KeepAsFragments,
                        new[] { "hero", "fragment", "featured", "paladin" }),
                    new GachaPoolEntry(
                        "archer_rare_fragments",
                        GachaRewardType.HeroFragments,
                        "hero_archer_001",
                        8,
                        Rarity.Rare,
                        4000,
                        false,
                        null,
                        GachaDuplicateRule.KeepAsFragments,
                        new[] { "hero", "fragment", "archer" }),
                    new GachaPoolEntry(
                        "mage_epic_fragments",
                        GachaRewardType.HeroFragments,
                        "hero_mage_001",
                        10,
                        Rarity.Epic,
                        1250,
                        false,
                        null,
                        GachaDuplicateRule.KeepAsFragments,
                        new[] { "hero", "fragment", "mage" }),
                    new GachaPoolEntry(
                        "common_development_dust",
                        GachaRewardType.AuxiliaryDevelopmentReward,
                        string.Empty,
                        10,
                        Rarity.Common,
                        2000,
                        false,
                        null,
                        GachaDuplicateRule.KeepAsFragments,
                        new[] { "auxiliary", "development-only" }),
                    new GachaPoolEntry(
                        "uncommon_development_dust",
                        GachaRewardType.AuxiliaryDevelopmentReward,
                        string.Empty,
                        5,
                        Rarity.Uncommon,
                        1500,
                        false,
                        null,
                        GachaDuplicateRule.KeepAsFragments,
                        new[] { "auxiliary", "development-only" })
                },
                new[] { "paladin_epic_fragments" },
                21,
                1000,
                30,
                Rarity.Epic,
                GachaPityScope.Group,
                "development_epic_fragment_family",
                false,
                RulesVersion,
                new[]
                {
                    new GachaDuplicateConversionRule(Rarity.Common, 20),
                    new GachaDuplicateConversionRule(Rarity.Uncommon, 30),
                    new GachaDuplicateConversionRule(Rarity.Rare, 50),
                    new GachaDuplicateConversionRule(Rarity.Epic, 80),
                    new GachaDuplicateConversionRule(Rarity.Legendary, 120),
                    new GachaDuplicateConversionRule(Rarity.Mythic, 200)
                },
                new[] { "development-only", "fragment-primary", "task-011" });
        }

        /// <summary>
        /// Prova de que o modelo aceita desbloqueio direto excepcional. Esta entrada
        /// não faz parte do banner demonstrativo principal.
        /// </summary>
        public static GachaPoolEntry CreateDisabledDirectUnlockExample()
        {
            return new GachaPoolEntry(
                "disabled_direct_paladin_unlock",
                GachaRewardType.DirectHeroUnlock,
                "hero_paladin_001",
                0,
                Rarity.Epic,
                1,
                false,
                null,
                GachaDuplicateRule.ConvertDirectUnlockToFragments,
                new[] { "disabled-example", "direct-unlock" });
        }

        public static HashSet<string> GetDemoHeroIds()
        {
            return new HashSet<string>(StringComparer.Ordinal)
            {
                "hero_paladin_001",
                "hero_archer_001",
                "hero_mage_001"
            };
        }
    }
}
#endif
