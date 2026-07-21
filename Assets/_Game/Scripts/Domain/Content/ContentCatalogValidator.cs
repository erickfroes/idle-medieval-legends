using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IdleMedievalLegends.Domain.Common;

namespace IdleMedievalLegends.Domain.Content
{
    public sealed class ContentValidationMessage
    {
        public ContentValidationMessage(
            ContentValidationSeverity severity,
            string definitionType,
            string definitionId,
            string reason)
        {
            Severity = severity;
            DefinitionType = definitionType;
            DefinitionId = definitionId;
            Reason = reason;
        }

        public ContentValidationSeverity Severity { get; }
        public string DefinitionType { get; }
        public string DefinitionId { get; }
        public string Reason { get; }

        public override string ToString()
        {
            return $"[{Severity}] {DefinitionType} '{DefinitionId ?? "<null>"}': {Reason}";
        }
    }

    public sealed class ContentCatalogSummary
    {
        public ContentCatalogSummary(
            int heroCount,
            int itemCount,
            int equipmentCount,
            int materialCount,
            int recipeCount,
            IReadOnlyDictionary<ContentTier, int> totalsByTier,
            IReadOnlyDictionary<Rarity, int> totalsByRarity,
            IReadOnlyDictionary<ProfessionType, int> totalsByProfession)
        {
            HeroCount = heroCount;
            ItemCount = itemCount;
            EquipmentCount = equipmentCount;
            MaterialCount = materialCount;
            RecipeCount = recipeCount;
            TotalsByTier = totalsByTier;
            TotalsByRarity = totalsByRarity;
            TotalsByProfession = totalsByProfession;
        }

        public int HeroCount { get; }
        public int ItemCount { get; }
        public int EquipmentCount { get; }
        public int MaterialCount { get; }
        public int RecipeCount { get; }
        public IReadOnlyDictionary<ContentTier, int> TotalsByTier { get; }
        public IReadOnlyDictionary<Rarity, int> TotalsByRarity { get; }
        public IReadOnlyDictionary<ProfessionType, int> TotalsByProfession { get; }
    }

    public sealed class ContentCatalogValidationReport
    {
        internal ContentCatalogValidationReport(
            IReadOnlyList<ContentValidationMessage> messages,
            ContentCatalogSummary summary)
        {
            Messages = messages;
            Summary = summary;

            int errors = 0;
            int warnings = 0;
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Severity == ContentValidationSeverity.Error)
                    errors++;
                else
                    warnings++;
            }

            ErrorCount = errors;
            WarningCount = warnings;
        }

        public IReadOnlyList<ContentValidationMessage> Messages { get; }
        public ContentCatalogSummary Summary { get; }
        public int ErrorCount { get; }
        public int WarningCount { get; }
        public bool IsValid => ErrorCount == 0;
    }

    public static class ContentCatalogValidator
    {
        public static ContentCatalogValidationReport Validate(ContentCatalog catalog)
        {
            var messages = new List<ContentValidationMessage>();
            if (catalog == null)
            {
                AddError(messages, nameof(ContentCatalog), null, "Catálogo nulo.");
                return BuildReport(messages, null);
            }

            ValidateDefinitions(catalog, messages);
            ValidateReferences(catalog, messages);
            return BuildReport(messages, catalog);
        }

        private static void ValidateDefinitions(
            ContentCatalog catalog,
            List<ContentValidationMessage> messages)
        {
            var allIds = new Dictionary<string, string>(StringComparer.Ordinal);

            ValidateHeroDefinitions(catalog, allIds, messages);
            ValidateItemDefinitions(catalog.Items, allIds, messages);
            ValidateEquipmentDefinitions(catalog.Equipment, allIds, messages);
            ValidateMaterialDefinitions(catalog.Materials, allIds, messages);
            ValidateRecipeDefinitions(catalog.Recipes, allIds, messages);
            ValidateProfessionDefinitions(catalog.Professions, allIds, messages);
            ValidateRarityDefinitions(catalog.Rarities, allIds, messages);
            ValidateTierDefinitions(catalog.Tiers, allIds, messages);
        }

        private static void ValidateHeroDefinitions(
            ContentCatalog catalog,
            Dictionary<string, string> allIds,
            List<ContentValidationMessage> messages)
        {
            for (int i = 0; i < catalog.Heroes.Count; i++)
            {
                HeroDefinition hero = catalog.Heroes[i];
                if (!Register(hero, nameof(HeroDefinition), allIds, messages))
                    continue;

                if (string.IsNullOrWhiteSpace(hero.DisplayName))
                    AddError(messages, nameof(HeroDefinition), hero.DefinitionId, "displayName vazio.");
                if (!Enum.IsDefined(typeof(HeroArchetype), hero.Archetype) ||
                    hero.Archetype == HeroArchetype.None)
                {
                    AddError(messages, nameof(HeroDefinition), hero.DefinitionId, "Arquétipo inválido.");
                }
                if (hero.BaseHealth <= 0 || hero.BaseAttack <= 0 || hero.BaseDefense < 0 ||
                    hero.BaseSpeed <= 0)
                {
                    AddError(messages, nameof(HeroDefinition), hero.DefinitionId,
                        "Atributos-base devem ser positivos; Defesa pode ser zero.");
                }
                ValidateRarityValue(hero.InitialRarity, nameof(HeroDefinition),
                    hero.DefinitionId, messages);
                ValidateStringList(hero.Tags, "tag", nameof(HeroDefinition),
                    hero.DefinitionId, messages);
                ValidateStringList(hero.AbilityDefinitionIds, "abilityDefinitionId",
                    nameof(HeroDefinition), hero.DefinitionId, messages);
            }
        }

        private static void ValidateItemDefinitions(
            IReadOnlyList<ItemDefinition> items,
            Dictionary<string, string> allIds,
            List<ContentValidationMessage> messages)
        {
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (!Register(item, nameof(ItemDefinition), allIds, messages))
                    continue;
                ValidateItem(item, nameof(ItemDefinition), messages);
                if (item.ItemType == ItemType.Equipment ||
                    item.ItemType == ItemType.Material ||
                    item is EquipmentDefinition ||
                    item is MaterialDefinition)
                {
                    AddError(messages, nameof(ItemDefinition), item.DefinitionId,
                        "Equipment e Material devem usar suas coleções especializadas.");
                }
            }
        }

        private static void ValidateEquipmentDefinitions(
            IReadOnlyList<EquipmentDefinition> equipment,
            Dictionary<string, string> allIds,
            List<ContentValidationMessage> messages)
        {
            for (int i = 0; i < equipment.Count; i++)
            {
                EquipmentDefinition item = equipment[i];
                if (!Register(item, nameof(EquipmentDefinition), allIds, messages))
                    continue;
                ValidateItem(item, nameof(EquipmentDefinition), messages);
                if (item.EquipmentSlot == EquipmentSlot.None ||
                    !Enum.IsDefined(typeof(EquipmentSlot), item.EquipmentSlot))
                {
                    AddError(messages, nameof(EquipmentDefinition), item.DefinitionId,
                        "Equipamento sem slot válido.");
                }
                if (item.StatBudget <= 0)
                    AddError(messages, nameof(EquipmentDefinition), item.DefinitionId,
                        "statBudget deve ser maior que zero.");
                if (item.RequiredLevel < 1)
                    AddError(messages, nameof(EquipmentDefinition), item.DefinitionId,
                        "requiredLevel deve ser maior que zero.");
                if (item.EnhancementLimit < 0)
                    AddError(messages, nameof(EquipmentDefinition), item.DefinitionId,
                        "enhancementLimit não pode ser negativo.");
                if (!Enum.IsDefined(typeof(ProfessionType), item.ProfessionSource))
                    AddError(messages, nameof(EquipmentDefinition), item.DefinitionId,
                        "professionSource inválida.");
                if (!Enum.IsDefined(typeof(BindingRule), item.BindingRule) ||
                    item.BindingRule == BindingRule.None)
                {
                    AddError(messages, nameof(EquipmentDefinition), item.DefinitionId,
                        "bindingRule inválida.");
                }
                ValidateStringList(item.AllowedAffixes, "allowedAffix",
                    nameof(EquipmentDefinition), item.DefinitionId, messages);
                ValidateStringList(item.RequiredHeroTags, "requiredHeroTag",
                    nameof(EquipmentDefinition), item.DefinitionId, messages);
            }
        }

        private static void ValidateMaterialDefinitions(
            IReadOnlyList<MaterialDefinition> materials,
            Dictionary<string, string> allIds,
            List<ContentValidationMessage> messages)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                MaterialDefinition material = materials[i];
                if (!Register(material, nameof(MaterialDefinition), allIds, messages))
                    continue;
                ValidateItem(material, nameof(MaterialDefinition), messages);
                if (material.MaterialCategory == MaterialCategory.None ||
                    !Enum.IsDefined(typeof(MaterialCategory), material.MaterialCategory))
                {
                    AddError(messages, nameof(MaterialDefinition), material.DefinitionId,
                        "materialCategory inválida.");
                }
                if (material.Refinable &&
                    string.IsNullOrWhiteSpace(material.RefinedOutputDefinitionId))
                {
                    AddError(messages, nameof(MaterialDefinition), material.DefinitionId,
                        "Material refinável sem refinedOutputDefinitionId.");
                }
                if (!material.Refinable &&
                    !string.IsNullOrWhiteSpace(material.RefinedOutputDefinitionId))
                {
                    AddWarning(messages, nameof(MaterialDefinition), material.DefinitionId,
                        "Material não refinável possui refinedOutputDefinitionId ignorado.");
                }
                ValidateStringList(material.SourceTags, "sourceTag",
                    nameof(MaterialDefinition), material.DefinitionId, messages);
            }
        }

        private static void ValidateItem(
            ItemDefinition item,
            string type,
            List<ContentValidationMessage> messages)
        {
            if (string.IsNullOrWhiteSpace(item.DisplayName))
                AddError(messages, type, item.DefinitionId, "displayName vazio.");
            if (item.ItemType == ItemType.None || !Enum.IsDefined(typeof(ItemType), item.ItemType))
                AddError(messages, type, item.DefinitionId, "itemType inválido.");
            ValidateTierValue(item.Tier, type, item.DefinitionId, messages);
            ValidateRarityValue(item.Rarity, type, item.DefinitionId, messages);
            if (item.MaxStackSize <= 0)
                AddError(messages, type, item.DefinitionId, "maxStackSize deve ser maior que zero.");
            if (!item.Stackable && item.MaxStackSize != 1)
                AddError(messages, type, item.DefinitionId,
                    "Item não empilhável deve possuir maxStackSize igual a 1.");
            ValidateStringList(item.Tags, "tag", type, item.DefinitionId, messages);
        }

        private static void ValidateRecipeDefinitions(
            IReadOnlyList<RecipeDefinition> recipes,
            Dictionary<string, string> allIds,
            List<ContentValidationMessage> messages)
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (!Register(recipe, nameof(RecipeDefinition), allIds, messages))
                    continue;

                if (string.IsNullOrWhiteSpace(recipe.OutputDefinitionId))
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId, "Receita sem saída.");
                if (recipe.OutputQuantity <= 0)
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        "outputQuantity deve ser maior que zero.");
                if (!Enum.IsDefined(typeof(ProfessionType), recipe.Profession))
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        "Profissão inválida.");
                if (recipe.RequiredProfessionLevel < 1 || recipe.RequiredProfessionLevel > 100)
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        "requiredProfessionLevel deve estar entre 1 e 100.");
                if (!Enum.IsDefined(typeof(ProfessionRank), recipe.RequiredProfessionRank))
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        "requiredProfessionRank inválido.");
                ValidateTierValue(recipe.RequiredTier, nameof(RecipeDefinition),
                    recipe.RecipeId, messages);
                ValidateTierValue(recipe.RequiredStationTier, nameof(RecipeDefinition),
                    recipe.RecipeId, messages);
                if (recipe.DurationSeconds < 0 || recipe.FocusCost < 0 || recipe.GoldCost < 0)
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        "Duração, Foco e ouro não podem ser negativos.");
                if (recipe.Ingredients.Count == 0 && !recipe.ExplicitlyFree)
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        "Receita sem ingredientes deve ser explicitamente gratuita.");

                ValidateIngredients(recipe, recipe.Ingredients, "ingrediente", messages);
                ValidateIngredients(recipe, recipe.OptionalCatalysts, "catalisador", messages);
            }
        }

        private static void ValidateIngredients(
            RecipeDefinition recipe,
            IReadOnlyList<RecipeIngredientDefinition> ingredients,
            string kind,
            List<ContentValidationMessage> messages)
        {
            for (int i = 0; i < ingredients.Count; i++)
            {
                RecipeIngredientDefinition ingredient = ingredients[i];
                if (ingredient == null)
                {
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        $"Lista de {kind} contém entrada nula.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(ingredient.ItemDefinitionId))
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        $"{kind} sem itemDefinitionId.");
                if (ingredient.Quantity <= 0)
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        $"{kind} '{ingredient.ItemDefinitionId}' com quantidade inválida.");
            }
        }

        private static void ValidateProfessionDefinitions(
            IReadOnlyList<ProfessionDefinition> professions,
            Dictionary<string, string> allIds,
            List<ContentValidationMessage> messages)
        {
            var types = new HashSet<ProfessionType>();
            for (int i = 0; i < professions.Count; i++)
            {
                ProfessionDefinition profession = professions[i];
                if (!Register(profession, nameof(ProfessionDefinition), allIds, messages))
                    continue;
                if (!Enum.IsDefined(typeof(ProfessionType), profession.ProfessionType))
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        "professionType inválida.");
                else if (!types.Add(profession.ProfessionType))
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        $"professionType duplicada: {profession.ProfessionType}.");
                if (string.IsNullOrWhiteSpace(profession.DisplayName) ||
                    string.IsNullOrWhiteSpace(profession.StationName))
                {
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        "displayName e stationName são obrigatórios.");
                }
                if (profession.MaxLevel < 1)
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        "maxLevel deve ser maior que zero.");

                ValidateUnlockThresholds(profession, messages);
                ValidateRankThresholds(profession, messages);
                ValidateTierThresholds(profession, messages);
                ValidateMasteryAndBonuses(profession, messages);
            }

            foreach (ProfessionType value in Enum.GetValues(typeof(ProfessionType)))
            {
                if (!types.Contains(value))
                {
                    AddError(messages, nameof(ProfessionDefinition), value.ToString(),
                        "Definição de profissão persistida ausente.");
                }
            }
        }

        private static void ValidateUnlockThresholds(
            ProfessionDefinition profession,
            List<ContentValidationMessage> messages)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < profession.UnlockThresholds.Count; i++)
            {
                ProfessionUnlockThreshold threshold = profession.UnlockThresholds[i];
                if (threshold == null || string.IsNullOrWhiteSpace(threshold.UnlockId))
                {
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        "unlockThreshold nulo ou sem unlockId.");
                    continue;
                }
                if (!ids.Add(threshold.UnlockId))
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        $"unlockThreshold duplicado: {threshold.UnlockId}.");
                if (threshold.RequiredLevel < 1 ||
                    threshold.RequiredLevel > profession.MaxLevel)
                {
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        $"unlockThreshold '{threshold.UnlockId}' fora de 1..maxLevel.");
                }
            }
        }

        private static void ValidateRankThresholds(
            ProfessionDefinition profession,
            List<ContentValidationMessage> messages)
        {
            var thresholds = new Dictionary<ProfessionRank, int>();
            for (int i = 0; i < profession.RankThresholds.Count; i++)
            {
                ProfessionRankThreshold threshold = profession.RankThresholds[i];
                if (threshold == null || !Enum.IsDefined(typeof(ProfessionRank), threshold.Rank))
                {
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        "rankThreshold nulo ou com rank inválido.");
                    continue;
                }
                if (threshold.MinimumLevel < 1 || threshold.MinimumLevel > profession.MaxLevel)
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        $"Threshold de {threshold.Rank} fora de 1..maxLevel.");
                if (thresholds.ContainsKey(threshold.Rank))
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        $"Threshold duplicado para {threshold.Rank}.");
                else
                    thresholds.Add(threshold.Rank, threshold.MinimumLevel);
            }

            int previous = 0;
            foreach (ProfessionRank rank in Enum.GetValues(typeof(ProfessionRank)))
            {
                if (!thresholds.TryGetValue(rank, out int level))
                {
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        $"Threshold ausente para {rank}.");
                    continue;
                }
                if (level <= previous)
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        "rankThresholds devem crescer estritamente por grau.");
                previous = level;
            }
        }

        private static void ValidateTierThresholds(
            ProfessionDefinition profession,
            List<ContentValidationMessage> messages)
        {
            var thresholds = new Dictionary<ContentTier, int>();
            for (int i = 0; i < profession.TierUnlockThresholds.Count; i++)
            {
                ProfessionTierUnlockThreshold threshold = profession.TierUnlockThresholds[i];
                if (threshold == null || !Enum.IsDefined(typeof(ContentTier), threshold.Tier))
                {
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        "tierUnlockThreshold nulo ou com Tier inválido.");
                    continue;
                }
                if (threshold.MinimumLevel < 1 || threshold.MinimumLevel > profession.MaxLevel)
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        $"Threshold de {threshold.Tier} fora de 1..maxLevel.");
                if (thresholds.ContainsKey(threshold.Tier))
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        $"Threshold duplicado para {threshold.Tier}.");
                else
                    thresholds.Add(threshold.Tier, threshold.MinimumLevel);
            }

            int previous = 0;
            foreach (ContentTier tier in Enum.GetValues(typeof(ContentTier)))
            {
                if (!thresholds.TryGetValue(tier, out int level))
                {
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        $"Threshold ausente para {tier}.");
                    continue;
                }
                if (level <= previous)
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        "tierUnlockThresholds devem crescer estritamente por Tier.");
                previous = level;
            }
        }

        private static void ValidateMasteryAndBonuses(
            ProfessionDefinition profession,
            List<ContentValidationMessage> messages)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < profession.MasteryBranches.Count; i++)
            {
                MasteryBranchDefinition branch = profession.MasteryBranches[i];
                if (branch == null || string.IsNullOrWhiteSpace(branch.BranchId) ||
                    !ids.Add(branch.BranchId))
                {
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        "masteryBranch nulo, vazio ou duplicado.");
                }
            }

            ids.Clear();
            for (int i = 0; i < profession.SpecializationBonuses.Count; i++)
            {
                SpecializationBonusDefinition bonus = profession.SpecializationBonuses[i];
                if (bonus == null || string.IsNullOrWhiteSpace(bonus.BonusId) ||
                    !ids.Add(bonus.BonusId))
                {
                    AddError(messages, nameof(ProfessionDefinition), profession.DefinitionId,
                        "specializationBonus nulo, vazio ou duplicado.");
                }
            }
        }

        private static void ValidateRarityDefinitions(
            IReadOnlyList<RarityDefinition> rarities,
            Dictionary<string, string> allIds,
            List<ContentValidationMessage> messages)
        {
            var values = new HashSet<Rarity>();
            var sortOrders = new HashSet<int>();
            for (int i = 0; i < rarities.Count; i++)
            {
                RarityDefinition rarity = rarities[i];
                if (!Register(rarity, nameof(RarityDefinition), allIds, messages))
                    continue;
                ValidateRarityValue(rarity.Rarity, nameof(RarityDefinition),
                    rarity.DefinitionId, messages);
                if (Enum.IsDefined(typeof(Rarity), rarity.Rarity) && !values.Add(rarity.Rarity))
                    AddError(messages, nameof(RarityDefinition), rarity.DefinitionId,
                        $"Raridade duplicada: {rarity.Rarity}.");
                if (!sortOrders.Add(rarity.SortOrder))
                    AddError(messages, nameof(RarityDefinition), rarity.DefinitionId,
                        $"sortOrder duplicado: {rarity.SortOrder}.");
                if (rarity.HeroMultiplier <= 0 || rarity.EquipmentMultiplier <= 0)
                    AddError(messages, nameof(RarityDefinition), rarity.DefinitionId,
                        "Multiplicadores devem ser maiores que zero.");
                if (rarity.MaximumAffixCount < 0 || rarity.MaximumEnhancementLevel < 0 ||
                    rarity.MarketWeightBasisPoints < 0)
                {
                    AddError(messages, nameof(RarityDefinition), rarity.DefinitionId,
                        "Limites e metadata econômica não podem ser negativos.");
                }
            }

            foreach (Rarity value in Enum.GetValues(typeof(Rarity)))
            {
                if (!values.Contains(value))
                    AddError(messages, nameof(RarityDefinition), value.ToString(),
                        "Definição de raridade ausente.");
            }
        }

        private static void ValidateTierDefinitions(
            IReadOnlyList<TierDefinition> tiers,
            Dictionary<string, string> allIds,
            List<ContentValidationMessage> messages)
        {
            var values = new HashSet<ContentTier>();
            for (int i = 0; i < tiers.Count; i++)
            {
                TierDefinition tier = tiers[i];
                if (!Register(tier, nameof(TierDefinition), allIds, messages))
                    continue;
                ValidateTierValue(tier.Tier, nameof(TierDefinition), tier.DefinitionId, messages);
                if (Enum.IsDefined(typeof(ContentTier), tier.Tier) && !values.Add(tier.Tier))
                    AddError(messages, nameof(TierDefinition), tier.DefinitionId,
                        $"Tier duplicado: {tier.Tier}.");
                if (tier.MinimumProfessionLevel < 1 || tier.MinimumProfessionLevel > 100 ||
                    tier.BaseEquipmentBudget <= 0 || tier.DefaultCraftDurationMultiplier <= 0 ||
                    tier.DefaultGoldMultiplier <= 0 || tier.DefaultMaterialMultiplier <= 0 ||
                    tier.RecommendedPlayerPower < 0)
                {
                    AddError(messages, nameof(TierDefinition), tier.DefinitionId,
                        "Níveis, orçamento e multiplicadores do Tier são inválidos.");
                }
            }

            foreach (ContentTier value in Enum.GetValues(typeof(ContentTier)))
            {
                if (!values.Contains(value))
                    AddError(messages, nameof(TierDefinition), value.ToString(),
                        "Definição de Tier ausente.");
            }
        }

        private static void ValidateReferences(
            ContentCatalog catalog,
            List<ContentValidationMessage> messages)
        {
            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < catalog.Items.Count; i++)
                if (catalog.Items[i] != null) itemIds.Add(catalog.Items[i].DefinitionId);
            for (int i = 0; i < catalog.Equipment.Count; i++)
                if (catalog.Equipment[i] != null) itemIds.Add(catalog.Equipment[i].DefinitionId);
            for (int i = 0; i < catalog.Materials.Count; i++)
                if (catalog.Materials[i] != null) itemIds.Add(catalog.Materials[i].DefinitionId);

            var professions = new HashSet<ProfessionType>();
            var professionDefinitions = new Dictionary<ProfessionType, ProfessionDefinition>();
            for (int i = 0; i < catalog.Professions.Count; i++)
                if (catalog.Professions[i] != null)
                {
                    professions.Add(catalog.Professions[i].ProfessionType);
                    if (!professionDefinitions.ContainsKey(catalog.Professions[i].ProfessionType))
                    {
                        professionDefinitions.Add(
                            catalog.Professions[i].ProfessionType,
                            catalog.Professions[i]);
                    }
                }

            var rarityValues = new HashSet<Rarity>();
            var rarityDefinitions = new Dictionary<Rarity, RarityDefinition>();
            for (int i = 0; i < catalog.Rarities.Count; i++)
            {
                if (catalog.Rarities[i] == null)
                    continue;
                rarityValues.Add(catalog.Rarities[i].Rarity);
                if (!rarityDefinitions.ContainsKey(catalog.Rarities[i].Rarity))
                    rarityDefinitions.Add(catalog.Rarities[i].Rarity, catalog.Rarities[i]);
            }

            var tierValues = new HashSet<ContentTier>();
            for (int i = 0; i < catalog.Tiers.Count; i++)
                if (catalog.Tiers[i] != null) tierValues.Add(catalog.Tiers[i].Tier);

            ValidateItemMetadataReferences(
                catalog,
                professions,
                rarityValues,
                rarityDefinitions,
                tierValues,
                messages);

            for (int i = 0; i < catalog.Materials.Count; i++)
            {
                MaterialDefinition material = catalog.Materials[i];
                if (material != null && material.Refinable &&
                    !string.IsNullOrWhiteSpace(material.RefinedOutputDefinitionId) &&
                    !itemIds.Contains(material.RefinedOutputDefinitionId))
                {
                    AddError(messages, nameof(MaterialDefinition), material.DefinitionId,
                        $"refinedOutputDefinitionId inexistente: {material.RefinedOutputDefinitionId}.");
                }
            }

            for (int i = 0; i < catalog.Recipes.Count; i++)
            {
                RecipeDefinition recipe = catalog.Recipes[i];
                if (recipe == null)
                    continue;
                if (!string.IsNullOrWhiteSpace(recipe.OutputDefinitionId) &&
                    !itemIds.Contains(recipe.OutputDefinitionId))
                {
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        $"outputDefinitionId inexistente: {recipe.OutputDefinitionId}.");
                }
                if (!professions.Contains(recipe.Profession))
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        $"ProfessionDefinition inexistente para {recipe.Profession}.");
                else if (professionDefinitions.TryGetValue(
                    recipe.Profession,
                    out ProfessionDefinition professionDefinition))
                {
                    ValidateRecipeThresholds(recipe, professionDefinition, messages);
                }
                if (!tierValues.Contains(recipe.RequiredTier) ||
                    !tierValues.Contains(recipe.RequiredStationTier))
                {
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        "TierDefinition inexistente para Tier requerido ou estação.");
                }
                ValidateIngredientReferences(recipe, recipe.Ingredients, itemIds, messages);
                ValidateIngredientReferences(recipe, recipe.OptionalCatalysts, itemIds, messages);
                if (recipe.EligibleForMythicCrafting &&
                    (recipe.RequiredTier != ContentTier.Tier9 ||
                     recipe.RequiredProfessionRank != ProfessionRank.God))
                {
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        "Receita elegível a Mítico deve exigir Tier9 e grau Deus.");
                }
            }
        }

        private static void ValidateItemMetadataReferences(
            ContentCatalog catalog,
            HashSet<ProfessionType> professions,
            HashSet<Rarity> rarities,
            Dictionary<Rarity, RarityDefinition> rarityDefinitions,
            HashSet<ContentTier> tiers,
            List<ContentValidationMessage> messages)
        {
            var items = new List<ItemDefinition>();
            items.AddRange(catalog.Items);
            items.AddRange(catalog.Equipment);
            items.AddRange(catalog.Materials);
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (item == null)
                    continue;
                if (!rarities.Contains(item.Rarity))
                    AddError(messages, item.GetType().Name, item.DefinitionId,
                        $"RarityDefinition inexistente para {item.Rarity}.");
                else if (item is EquipmentDefinition equipment &&
                    rarityDefinitions.TryGetValue(item.Rarity, out RarityDefinition rarity) &&
                    equipment.EnhancementLimit > rarity.MaximumEnhancementLevel)
                {
                    AddError(messages, nameof(EquipmentDefinition), item.DefinitionId,
                        $"enhancementLimit excede o máximo de {item.Rarity}.");
                }
                if (!tiers.Contains(item.Tier))
                    AddError(messages, item.GetType().Name, item.DefinitionId,
                        $"TierDefinition inexistente para {item.Tier}.");
            }

            for (int i = 0; i < catalog.Heroes.Count; i++)
            {
                HeroDefinition hero = catalog.Heroes[i];
                if (hero != null && !rarities.Contains(hero.InitialRarity))
                    AddError(messages, nameof(HeroDefinition), hero.DefinitionId,
                        $"RarityDefinition inexistente para {hero.InitialRarity}.");
            }

            for (int i = 0; i < catalog.Equipment.Count; i++)
            {
                EquipmentDefinition equipment = catalog.Equipment[i];
                if (equipment != null && !professions.Contains(equipment.ProfessionSource))
                    AddError(messages, nameof(EquipmentDefinition), equipment.DefinitionId,
                        $"ProfessionDefinition inexistente para {equipment.ProfessionSource}.");
            }
        }

        private static void ValidateIngredientReferences(
            RecipeDefinition recipe,
            IReadOnlyList<RecipeIngredientDefinition> ingredients,
            HashSet<string> itemIds,
            List<ContentValidationMessage> messages)
        {
            for (int i = 0; i < ingredients.Count; i++)
            {
                RecipeIngredientDefinition ingredient = ingredients[i];
                if (ingredient != null &&
                    !string.IsNullOrWhiteSpace(ingredient.ItemDefinitionId) &&
                    !itemIds.Contains(ingredient.ItemDefinitionId))
                {
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        $"Ingrediente inexistente: {ingredient.ItemDefinitionId}.");
                }
            }
        }

        private static void ValidateRecipeThresholds(
            RecipeDefinition recipe,
            ProfessionDefinition profession,
            List<ContentValidationMessage> messages)
        {
            for (int i = 0; i < profession.RankThresholds.Count; i++)
            {
                ProfessionRankThreshold threshold = profession.RankThresholds[i];
                if (threshold != null && threshold.Rank == recipe.RequiredProfessionRank &&
                    recipe.RequiredProfessionLevel < threshold.MinimumLevel)
                {
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        $"requiredProfessionLevel é incompatível com {threshold.Rank}.");
                }
            }

            for (int i = 0; i < profession.TierUnlockThresholds.Count; i++)
            {
                ProfessionTierUnlockThreshold threshold = profession.TierUnlockThresholds[i];
                if (threshold != null && threshold.Tier == recipe.RequiredTier &&
                    recipe.RequiredProfessionLevel < threshold.MinimumLevel)
                {
                    AddError(messages, nameof(RecipeDefinition), recipe.RecipeId,
                        $"requiredProfessionLevel é incompatível com {threshold.Tier}.");
                }
            }
        }

        private static bool Register(
            ContentDefinition definition,
            string type,
            Dictionary<string, string> allIds,
            List<ContentValidationMessage> messages)
        {
            if (definition == null)
            {
                AddError(messages, type, null, "Definição nula.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(definition.DefinitionId))
            {
                AddError(messages, type, definition.DefinitionId, "ID nulo, vazio ou whitespace.");
                return false;
            }
            if (allIds.TryGetValue(definition.DefinitionId, out string previousType))
            {
                AddError(messages, type, definition.DefinitionId,
                    $"ID duplicado; já usado por {previousType}.");
                return false;
            }

            if (!IsStableTextId(definition.DefinitionId))
            {
                AddError(messages, type, definition.DefinitionId,
                    "ID deve usar somente letras minúsculas ASCII, números e underscore.");
            }

            allIds.Add(definition.DefinitionId, type);
            return true;
        }

        private static bool IsStableTextId(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                bool lowercaseAscii = character >= 'a' && character <= 'z';
                bool digit = character >= '0' && character <= '9';
                if (!lowercaseAscii && !digit && character != '_')
                    return false;
            }

            return true;
        }

        private static void ValidateRarityValue(
            Rarity rarity,
            string type,
            string id,
            List<ContentValidationMessage> messages)
        {
            if (!Enum.IsDefined(typeof(Rarity), rarity))
                AddError(messages, type, id, $"Raridade desconhecida: {(int)rarity}.");
        }

        private static void ValidateTierValue(
            ContentTier tier,
            string type,
            string id,
            List<ContentValidationMessage> messages)
        {
            if (!Enum.IsDefined(typeof(ContentTier), tier))
                AddError(messages, type, id, $"Tier inválido: {(int)tier}.");
        }

        private static void ValidateStringList(
            IReadOnlyList<string> values,
            string valueName,
            string type,
            string id,
            List<ContentValidationMessage> messages)
        {
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < values.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(values[i]))
                    AddError(messages, type, id, $"{valueName} vazio.");
                else if (!unique.Add(values[i]))
                    AddWarning(messages, type, id, $"{valueName} duplicado: {values[i]}.");
            }
        }

        private static ContentCatalogValidationReport BuildReport(
            List<ContentValidationMessage> messages,
            ContentCatalog catalog)
        {
            ContentCatalogSummary summary = BuildSummary(catalog);
            return new ContentCatalogValidationReport(
                new ReadOnlyCollection<ContentValidationMessage>(messages),
                summary);
        }

        private static ContentCatalogSummary BuildSummary(ContentCatalog catalog)
        {
            var byTier = InitializeEnumCounts<ContentTier>();
            var byRarity = InitializeEnumCounts<Rarity>();
            var byProfession = InitializeEnumCounts<ProfessionType>();

            if (catalog == null)
            {
                return new ContentCatalogSummary(0, 0, 0, 0, 0,
                    AsReadOnly(byTier), AsReadOnly(byRarity), AsReadOnly(byProfession));
            }

            var items = new List<ItemDefinition>();
            items.AddRange(catalog.Items);
            items.AddRange(catalog.Equipment);
            items.AddRange(catalog.Materials);
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (item == null)
                    continue;
                IncrementKnown(byTier, item.Tier);
                IncrementKnown(byRarity, item.Rarity);
            }
            for (int i = 0; i < catalog.Heroes.Count; i++)
            {
                if (catalog.Heroes[i] != null)
                    IncrementKnown(byRarity, catalog.Heroes[i].InitialRarity);
            }
            for (int i = 0; i < catalog.Recipes.Count; i++)
            {
                if (catalog.Recipes[i] == null)
                    continue;
                IncrementKnown(byTier, catalog.Recipes[i].RequiredTier);
                IncrementKnown(byProfession, catalog.Recipes[i].Profession);
            }

            return new ContentCatalogSummary(
                catalog.Heroes.Count,
                items.Count,
                catalog.Equipment.Count,
                catalog.Materials.Count,
                catalog.Recipes.Count,
                AsReadOnly(byTier),
                AsReadOnly(byRarity),
                AsReadOnly(byProfession));
        }

        private static Dictionary<T, int> InitializeEnumCounts<T>()
        {
            var counts = new Dictionary<T, int>();
            foreach (T value in Enum.GetValues(typeof(T)))
                counts.Add(value, 0);
            return counts;
        }

        private static void IncrementKnown<T>(Dictionary<T, int> counts, T key)
        {
            if (counts.TryGetValue(key, out int count))
                counts[key] = count + 1;
        }

        private static IReadOnlyDictionary<T, int> AsReadOnly<T>(Dictionary<T, int> values)
        {
            return new ReadOnlyDictionary<T, int>(values);
        }

        private static void AddError(
            List<ContentValidationMessage> messages,
            string type,
            string id,
            string reason)
        {
            messages.Add(new ContentValidationMessage(
                ContentValidationSeverity.Error, type, id, reason));
        }

        private static void AddWarning(
            List<ContentValidationMessage> messages,
            string type,
            string id,
            string reason)
        {
            messages.Add(new ContentValidationMessage(
                ContentValidationSeverity.Warning, type, id, reason));
        }
    }
}
