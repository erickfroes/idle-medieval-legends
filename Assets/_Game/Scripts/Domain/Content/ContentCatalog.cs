using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IdleMedievalLegends.Domain.Content
{
    /// <summary>
    /// Snapshot imutável das definições estáticas usadas pelo cliente.
    /// Não contém instâncias, saldos ou progresso de jogador.
    /// </summary>
    public sealed class ContentCatalog
    {
        public ContentCatalog(
            IEnumerable<HeroDefinition> heroes,
            IEnumerable<ItemDefinition> items,
            IEnumerable<EquipmentDefinition> equipment,
            IEnumerable<MaterialDefinition> materials,
            IEnumerable<RecipeDefinition> recipes,
            IEnumerable<ProfessionDefinition> professions,
            IEnumerable<RarityDefinition> rarities,
            IEnumerable<TierDefinition> tiers)
        {
            Heroes = DefinitionCollection.Copy(heroes);
            Items = DefinitionCollection.Copy(items);
            Equipment = DefinitionCollection.Copy(equipment);
            Materials = DefinitionCollection.Copy(materials);
            Recipes = DefinitionCollection.Copy(recipes);
            Professions = DefinitionCollection.Copy(professions);
            Rarities = DefinitionCollection.Copy(rarities);
            Tiers = DefinitionCollection.Copy(tiers);
        }

        public IReadOnlyList<HeroDefinition> Heroes { get; }
        public IReadOnlyList<ItemDefinition> Items { get; }
        public IReadOnlyList<EquipmentDefinition> Equipment { get; }
        public IReadOnlyList<MaterialDefinition> Materials { get; }
        public IReadOnlyList<RecipeDefinition> Recipes { get; }
        public IReadOnlyList<ProfessionDefinition> Professions { get; }
        public IReadOnlyList<RarityDefinition> Rarities { get; }
        public IReadOnlyList<TierDefinition> Tiers { get; }
    }

    public sealed class ContentCatalogLookup
    {
        private readonly IReadOnlyDictionary<string, HeroDefinition> heroesById;
        private readonly IReadOnlyDictionary<string, ItemDefinition> itemsById;
        private readonly IReadOnlyDictionary<string, EquipmentDefinition> equipmentById;
        private readonly IReadOnlyDictionary<string, MaterialDefinition> materialsById;
        private readonly IReadOnlyDictionary<string, RecipeDefinition> recipesById;
        private readonly IReadOnlyDictionary<string, ProfessionDefinition> professionsById;
        private readonly IReadOnlyDictionary<ProfessionType, ProfessionDefinition>
            professionsByType;
        private readonly IReadOnlyDictionary<Rarity, RarityDefinition> raritiesByValue;
        private readonly IReadOnlyDictionary<ContentTier, TierDefinition> tiersByValue;

        public ContentCatalogLookup(ContentCatalog catalog)
        {
            ValidationReport = ContentCatalogValidator.Validate(catalog);
            if (!ValidationReport.IsValid)
                throw new ContentCatalogValidationException(ValidationReport);

            Catalog = catalog;
            heroesById = IndexById(catalog.Heroes);

            var allItems = new List<ItemDefinition>(
                catalog.Items.Count + catalog.Equipment.Count + catalog.Materials.Count);
            allItems.AddRange(catalog.Items);
            allItems.AddRange(catalog.Equipment);
            allItems.AddRange(catalog.Materials);
            itemsById = IndexById(allItems);

            equipmentById = IndexById(catalog.Equipment);
            materialsById = IndexById(catalog.Materials);
            recipesById = IndexById(catalog.Recipes);
            professionsById = IndexById(catalog.Professions);

            var professionIndex = new Dictionary<ProfessionType, ProfessionDefinition>();
            for (int i = 0; i < catalog.Professions.Count; i++)
                professionIndex.Add(catalog.Professions[i].ProfessionType, catalog.Professions[i]);
            professionsByType = new ReadOnlyDictionary<ProfessionType, ProfessionDefinition>(
                professionIndex);

            var rarityIndex = new Dictionary<Rarity, RarityDefinition>();
            for (int i = 0; i < catalog.Rarities.Count; i++)
                rarityIndex.Add(catalog.Rarities[i].Rarity, catalog.Rarities[i]);
            raritiesByValue = new ReadOnlyDictionary<Rarity, RarityDefinition>(rarityIndex);

            var tierIndex = new Dictionary<ContentTier, TierDefinition>();
            for (int i = 0; i < catalog.Tiers.Count; i++)
                tierIndex.Add(catalog.Tiers[i].Tier, catalog.Tiers[i]);
            tiersByValue = new ReadOnlyDictionary<ContentTier, TierDefinition>(tierIndex);
        }

        public ContentCatalog Catalog { get; }
        public ContentCatalogValidationReport ValidationReport { get; }

        public HeroDefinition GetHero(string definitionId)
        {
            return GetRequired(heroesById, definitionId, nameof(HeroDefinition));
        }

        public ItemDefinition GetItem(string definitionId)
        {
            return GetRequired(itemsById, definitionId, nameof(ItemDefinition));
        }

        public EquipmentDefinition GetEquipment(string definitionId)
        {
            return GetRequired(equipmentById, definitionId, nameof(EquipmentDefinition));
        }

        public MaterialDefinition GetMaterial(string definitionId)
        {
            return GetRequired(materialsById, definitionId, nameof(MaterialDefinition));
        }

        public RecipeDefinition GetRecipe(string recipeId)
        {
            return GetRequired(recipesById, recipeId, nameof(RecipeDefinition));
        }

        public ProfessionDefinition GetProfession(string definitionId)
        {
            return GetRequired(professionsById, definitionId, nameof(ProfessionDefinition));
        }

        public ProfessionDefinition GetProfession(ProfessionType professionType)
        {
            return GetRequired(professionsByType, professionType, nameof(ProfessionDefinition));
        }

        public RarityDefinition GetRarity(Rarity rarity)
        {
            return GetRequired(raritiesByValue, rarity, nameof(RarityDefinition));
        }

        public TierDefinition GetTier(ContentTier tier)
        {
            return GetRequired(tiersByValue, tier, nameof(TierDefinition));
        }

        public bool TryGetItem(string definitionId, out ItemDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(definitionId) &&
                   itemsById.TryGetValue(definitionId, out definition);
        }

        private static IReadOnlyDictionary<string, T> IndexById<T>(
            IReadOnlyList<T> definitions) where T : ContentDefinition
        {
            var index = new Dictionary<string, T>(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Count; i++)
                index.Add(definitions[i].DefinitionId, definitions[i]);
            return new ReadOnlyDictionary<string, T>(index);
        }

        private static T GetRequired<T>(
            IReadOnlyDictionary<string, T> index,
            string id,
            string definitionType)
        {
            if (string.IsNullOrWhiteSpace(id) || !index.TryGetValue(id, out T value))
            {
                throw new KeyNotFoundException(
                    $"{definitionType} não encontrada para o ID '{id ?? "<null>"}'.");
            }

            return value;
        }

        private static T GetRequired<TKey, T>(
            IReadOnlyDictionary<TKey, T> index,
            TKey key,
            string definitionType)
        {
            if (!index.TryGetValue(key, out T value))
            {
                throw new KeyNotFoundException(
                    $"{definitionType} não encontrada para a chave '{key}'.");
            }

            return value;
        }
    }

    public sealed class ContentCatalogValidationException : InvalidOperationException
    {
        public ContentCatalogValidationException(ContentCatalogValidationReport report)
            : base($"Catálogo inválido: {report.ErrorCount} erro(s), " +
                   $"{report.WarningCount} aviso(s).")
        {
            Report = report;
        }

        public ContentCatalogValidationReport Report { get; }
    }
}
