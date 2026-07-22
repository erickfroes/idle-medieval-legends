using System.Collections.Generic;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Content;

namespace IdleMedievalLegends.Editor.ContentCatalog
{
    public static class ContentCatalogDemoFactory
    {
        public static IdleMedievalLegends.Domain.Content.ContentCatalog Create()
        {
            return new IdleMedievalLegends.Domain.Content.ContentCatalog(
                CreateHeroes(),
                new List<ItemDefinition>(),
                CreateEquipment(),
                CreateMaterials(),
                CreateRecipes(),
                CreateProfessions(),
                CreateRarities(),
                CreateTiers());
        }

        private static IReadOnlyList<HeroDefinition> CreateHeroes()
        {
            return new[]
            {
                new HeroDefinition(
                    "hero_paladin_001", "Paladino",
                    "Defensor resiliente que protege o grupo na linha de frente.",
                    HeroArchetype.Tank, 1400, 70, 150, 85, Rarity.Common,
                    new[] { "hero", "melee", "heavy_armor", "holy" }),
                new HeroDefinition(
                    "hero_archer_001", "Arqueira",
                    "Atacante ágil especializada em dano físico à distância.",
                    HeroArchetype.Ranger, 900, 145, 65, 120, Rarity.Common,
                    new[] { "hero", "ranged", "medium_armor", "physical" }),
                new HeroDefinition(
                    "hero_mage_001", "Mago",
                    "Conjurador de alto ataque e baixa resistência física.",
                    HeroArchetype.Mage, 850, 140, 60, 100, Rarity.Common,
                    new[] { "hero", "ranged", "light_armor", "arcane" })
            };
        }

        private static IReadOnlyList<MaterialDefinition> CreateMaterials()
        {
            return new[]
            {
                new MaterialDefinition(
                    "material_iron_ore_t1", "Minério de Ferro T1",
                    "Minério bruto coletado em regiões iniciais.",
                    MaterialCategory.Ore, ContentTier.Tier1, Rarity.Common, true,
                    "material_iron_ingot_t1", new[] { "mine", "ore", "t1" },
                    true, 999, tags: new[] { "material", "raw", "metal" }),
                new MaterialDefinition(
                    "material_iron_ingot_t1", "Lingote de Ferro T1",
                    "Ferro refinado para armas, armaduras e componentes.",
                    MaterialCategory.Ingot, ContentTier.Tier1, Rarity.Common, false,
                    null, new[] { "blacksmith", "refined", "t1" },
                    true, 999, tags: new[] { "material", "refined", "metal" }),
                new MaterialDefinition(
                    "material_raw_hide_t1", "Couro Cru T1",
                    "Pele bruta obtida em expedições de coleta.",
                    MaterialCategory.Hide, ContentTier.Tier1, Rarity.Common, true,
                    "material_treated_leather_t1", new[] { "beast", "hide", "t1" },
                    true, 999, tags: new[] { "material", "raw", "leather" }),
                new MaterialDefinition(
                    "material_treated_leather_t1", "Couro Tratado T1",
                    "Couro simples preparado para vestimentas e empunhaduras.",
                    MaterialCategory.Leather, ContentTier.Tier1, Rarity.Common, false,
                    null, new[] { "gatherer", "treated", "t1" },
                    true, 999, tags: new[] { "material", "refined", "leather" }),
                new MaterialDefinition(
                    "material_arcane_essence_t1", "Essência Arcana T1",
                    "Essência opaca usada em runas e acessórios arcanos menores.",
                    MaterialCategory.Essence, ContentTier.Tier1, Rarity.Common, false,
                    null, new[] { "arcane", "enchanting", "t1" },
                    true, 999, tags: new[] { "material", "arcane", "essence" })
            };
        }

        private static IReadOnlyList<EquipmentDefinition> CreateEquipment()
        {
            return new[]
            {
                new EquipmentDefinition(
                    "item_iron_sword_t1", "Espada de Ferro T1",
                    "Arma física básica produzida por Ferreiro.",
                    ContentTier.Tier1, Rarity.Common, true, true, true,
                    EquipmentSlot.Weapon, 100, new[] { "affix_attack_flat" }, 1,
                    new[] { "melee" }, ProfessionType.Blacksmith, 3,
                    BindingRule.UnboundUntilEquipped,
                    tags: new[] { "equipment", "weapon", "sword", "metal" }),
                new EquipmentDefinition(
                    "item_leather_tunic_t1", "Túnica de Couro T1",
                    "Proteção leve de couro produzida por Costureiro.",
                    ContentTier.Tier1, Rarity.Common, true, true, true,
                    EquipmentSlot.Chest, 100, new[] { "affix_defense_flat" }, 1,
                    new[] { "light_armor", "medium_armor" }, ProfessionType.Tailor, 3,
                    BindingRule.UnboundUntilEquipped,
                    tags: new[] { "equipment", "chest", "leather" }),
                new EquipmentDefinition(
                    "item_arcane_ring_t1", "Anel Arcano T1",
                    "Acessório menor que concentra essência arcana.",
                    ContentTier.Tier1, Rarity.Common, true, true, true,
                    EquipmentSlot.Ring, 100, new[] { "affix_attack_flat" }, 1,
                    new string[0], ProfessionType.Enchanter, 3,
                    BindingRule.UnboundUntilEquipped,
                    tags: new[] { "equipment", "ring", "arcane" })
            };
        }

        private static IReadOnlyList<RecipeDefinition> CreateRecipes()
        {
            return new[]
            {
                new RecipeDefinition(
                    "recipe_iron_ingot_t1", "material_iron_ingot_t1", 1,
                    ProfessionType.Blacksmith, 1, ProfessionRank.Apprentice,
                    ContentTier.Tier1, ContentTier.Tier1, 30, 1, 5,
                    new[]
                    {
                        new RecipeIngredientDefinition(
                            "material_iron_ore_t1", 2, true, false, "ore_t1")
                    },
                    new RecipeIngredientDefinition[0], false, false),
                new RecipeDefinition(
                    "recipe_iron_sword_t1", "item_iron_sword_t1", 1,
                    ProfessionType.Blacksmith, 1, ProfessionRank.Apprentice,
                    ContentTier.Tier1, ContentTier.Tier1, 60, 1, 20,
                    new[]
                    {
                        new RecipeIngredientDefinition(
                            "material_iron_ingot_t1", 4, true, false, "metal_ingot_t1")
                    },
                    new RecipeIngredientDefinition[0], false, true),
                new RecipeDefinition(
                    "recipe_leather_tunic_t1", "item_leather_tunic_t1", 1,
                    ProfessionType.Tailor, 1, ProfessionRank.Apprentice,
                    ContentTier.Tier1, ContentTier.Tier1, 60, 1, 20,
                    new[]
                    {
                        new RecipeIngredientDefinition(
                            "material_treated_leather_t1", 3, true, false, "leather_t1")
                    },
                    new[]
                    {
                        new RecipeIngredientDefinition(
                            "material_raw_hide_t1", 1, true, true, "hide_t1")
                    },
                    false, true),
                new RecipeDefinition(
                    "recipe_arcane_ring_t1", "item_arcane_ring_t1", 1,
                    ProfessionType.Enchanter, 1, ProfessionRank.Apprentice,
                    ContentTier.Tier1, ContentTier.Tier1, 90, 1, 25,
                    new[]
                    {
                        new RecipeIngredientDefinition(
                            "material_arcane_essence_t1", 2, true, false, "arcane_essence_t1")
                    },
                    new[]
                    {
                        new RecipeIngredientDefinition(
                            "material_iron_ingot_t1", 1, true, true, "metal_ingot_t1")
                    },
                    false, true)
            };
        }

        private static IReadOnlyList<RarityDefinition> CreateRarities()
        {
            return new[]
            {
                CreateRarity("rarity_common", Rarity.Common, "Comum", 1.00m, 1.00m,
                    0, 3, new VisualColor(190, 190, 190), 0, 6000),
                CreateRarity("rarity_uncommon", Rarity.Uncommon, "Incomum", 1.08m, 1.06m,
                    1, 5, new VisualColor(75, 190, 90), 1, 2800),
                CreateRarity("rarity_rare", Rarity.Rare, "Raro", 1.18m, 1.14m,
                    2, 7, new VisualColor(70, 125, 230), 2, 1000),
                CreateRarity("rarity_epic", Rarity.Epic, "Épico", 1.31m, 1.24m,
                    3, 10, new VisualColor(165, 85, 220), 3, 200),
                CreateRarity("rarity_legendary", Rarity.Legendary, "Lendário", 1.47m, 1.36m,
                    4, 13, new VisualColor(235, 155, 45), 4, 0),
                CreateRarity("rarity_mythic", Rarity.Mythic, "Mítico", 1.66m, 1.50m,
                    5, 15, new VisualColor(230, 70, 85), 5, 0)
            };
        }

        private static RarityDefinition CreateRarity(
            string id,
            Rarity rarity,
            string name,
            decimal heroMultiplier,
            decimal equipmentMultiplier,
            int affixes,
            int enhancement,
            VisualColor color,
            int sortOrder,
            int marketWeightBasisPoints)
        {
            return new RarityDefinition(
                id, rarity, name, heroMultiplier, equipmentMultiplier, affixes,
                enhancement, color, sortOrder, marketWeightBasisPoints,
                new[] { "client_preview_only", "server_authoritative_economy" });
        }

        private static IReadOnlyList<TierDefinition> CreateTiers()
        {
            int[] levels = { 1, 10, 20, 30, 40, 52, 64, 76, 90 };
            decimal[] multipliers =
            {
                1.000m, 1.450m, 2.103m, 3.049m, 4.421m,
                6.410m, 9.294m, 13.476m, 19.541m
            };
            var result = new List<TierDefinition>(9);
            for (int i = 0; i < 9; i++)
            {
                ContentTier tier = (ContentTier)(i + 1);
                long budget = (long)(100m * multipliers[i]);
                long recommendedPower = (long)(1000m * multipliers[i]);
                result.Add(new TierDefinition(
                    $"tier_{i + 1}", tier, $"Tier {i + 1}", levels[i], budget,
                    multipliers[i], multipliers[i], multipliers[i], recommendedPower,
                    new[] { $"t{i + 1}", "content_tier" }));
            }
            return result;
        }

        private static IReadOnlyList<ProfessionDefinition> CreateProfessions()
        {
            return new[]
            {
                CreateProfession(
                    "profession_blacksmith", ProfessionType.Blacksmith, "Ferreiro", "Forja",
                    "Refina minérios e produz armas, escudos e armaduras pesadas."),
                CreateProfession(
                    "profession_tailor", ProfessionType.Tailor, "Costureiro", "Ateliê",
                    "Prepara tecidos e couros para equipamentos leves e médios."),
                CreateProfession(
                    "profession_enchanter", ProfessionType.Enchanter, "Encantador", "Mesa Arcana",
                    "Refina essências e cria runas, encantamentos e acessórios."),
                CreateProfession(
                    "profession_alchemist", ProfessionType.Alchemist, "Alquimista", "Laboratório",
                    "Produz extratos, poções, óleos e catalisadores."),
                CreateProfession(
                    "profession_gatherer", ProfessionType.Gatherer, "Coletador",
                    "Acampamento de Expedição",
                    "Obtém e prepara matérias-primas para todas as profissões.")
            };
        }

        private static ProfessionDefinition CreateProfession(
            string id,
            ProfessionType type,
            string displayName,
            string stationName,
            string description)
        {
            return new ProfessionDefinition(
                id, type, displayName, description, stationName, 100,
                new[]
                {
                    new ProfessionUnlockThreshold("profession_available", 1),
                    new ProfessionUnlockThreshold("mastery_available", 40)
                },
                new[]
                {
                    new ProfessionRankThreshold(ProfessionRank.Apprentice, 1),
                    new ProfessionRankThreshold(ProfessionRank.Proficient, 20),
                    new ProfessionRankThreshold(ProfessionRank.Master, 40),
                    new ProfessionRankThreshold(ProfessionRank.Grandmaster, 64),
                    new ProfessionRankThreshold(ProfessionRank.God, 90)
                },
                new[]
                {
                    new ProfessionTierUnlockThreshold(ContentTier.Tier1, 1),
                    new ProfessionTierUnlockThreshold(ContentTier.Tier2, 10),
                    new ProfessionTierUnlockThreshold(ContentTier.Tier3, 20),
                    new ProfessionTierUnlockThreshold(ContentTier.Tier4, 30),
                    new ProfessionTierUnlockThreshold(ContentTier.Tier5, 40),
                    new ProfessionTierUnlockThreshold(ContentTier.Tier6, 52),
                    new ProfessionTierUnlockThreshold(ContentTier.Tier7, 64),
                    new ProfessionTierUnlockThreshold(ContentTier.Tier8, 76),
                    new ProfessionTierUnlockThreshold(ContentTier.Tier9, 90)
                },
                new[]
                {
                    new MasteryBranchDefinition(
                        "efficiency", "Eficiência", "Tempo, lotes e economia de material comum."),
                    new MasteryBranchDefinition(
                        "excellence", "Excelência", "Qualidade, afixos e preservação."),
                    new MasteryBranchDefinition(
                        "commerce", "Comércio", "Comissões, reputação e listagem em ouro.")
                },
                new[]
                {
                    new SpecializationBonusDefinition(
                        "primary_profession_xp", "Bônus de XP da profissão principal.", 2000),
                    new SpecializationBonusDefinition(
                        "primary_profession_duration", "Redução de duração da profissão principal.",
                        -1500)
                });
        }
    }
}
