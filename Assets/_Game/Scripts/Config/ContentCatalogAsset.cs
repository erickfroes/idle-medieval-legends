using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Content;
using UnityEngine;

namespace IdleMedievalLegends.Config
{
    [CreateAssetMenu(
        fileName = "ContentCatalog",
        menuName = "Idle Medieval Legends/Content/Content Catalog")]
    public sealed class ContentCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<HeroDefinitionAuthoring> heroes =
            new List<HeroDefinitionAuthoring>();
        [SerializeField] private List<ItemDefinitionAuthoring> items =
            new List<ItemDefinitionAuthoring>();
        [SerializeField] private List<EquipmentDefinitionAuthoring> equipment =
            new List<EquipmentDefinitionAuthoring>();
        [SerializeField] private List<MaterialDefinitionAuthoring> materials =
            new List<MaterialDefinitionAuthoring>();
        [SerializeField] private List<RecipeDefinitionAuthoring> recipes =
            new List<RecipeDefinitionAuthoring>();
        [SerializeField] private List<ProfessionDefinitionAuthoring> professions =
            new List<ProfessionDefinitionAuthoring>();
        [SerializeField] private List<RarityDefinitionAuthoring> rarities =
            new List<RarityDefinitionAuthoring>();
        [SerializeField] private List<TierDefinitionAuthoring> tiers =
            new List<TierDefinitionAuthoring>();

        public ContentCatalog BuildRuntimeCatalog()
        {
            EnsureInitialized();
            return new ContentCatalog(
                ConvertAll(heroes, value => value?.ToDomain()),
                ConvertAll(items, value => value?.ToDomain()),
                ConvertAll(equipment, value => value?.ToDomain()),
                ConvertAll(materials, value => value?.ToDomain()),
                ConvertAll(recipes, value => value?.ToDomain()),
                ConvertAll(professions, value => value?.ToDomain()),
                ConvertAll(rarities, value => value?.ToDomain()),
                ConvertAll(tiers, value => value?.ToDomain()));
        }

        public ContentCatalogLookup BuildValidatedLookup()
        {
            return new ContentCatalogLookup(BuildRuntimeCatalog());
        }

        public ContentCatalogValidationReport ValidateCatalog()
        {
            return ContentCatalogValidator.Validate(BuildRuntimeCatalog());
        }

        public void EnsureInitialized()
        {
            if (heroes == null) heroes = new List<HeroDefinitionAuthoring>();
            if (items == null) items = new List<ItemDefinitionAuthoring>();
            if (equipment == null) equipment = new List<EquipmentDefinitionAuthoring>();
            if (materials == null) materials = new List<MaterialDefinitionAuthoring>();
            if (recipes == null) recipes = new List<RecipeDefinitionAuthoring>();
            if (professions == null) professions = new List<ProfessionDefinitionAuthoring>();
            if (rarities == null) rarities = new List<RarityDefinitionAuthoring>();
            if (tiers == null) tiers = new List<TierDefinitionAuthoring>();
        }

#if UNITY_EDITOR
        /// <summary>
        /// API exclusiva do Editor para geradores/importadores. Não existe no player.
        /// </summary>
        public void ReplaceDefinitionsForEditor(ContentCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            heroes = ConvertAll(catalog.Heroes, value => new HeroDefinitionAuthoring(value));
            items = ConvertAll(catalog.Items, value => new ItemDefinitionAuthoring(value));
            equipment = ConvertAll(
                catalog.Equipment,
                value => new EquipmentDefinitionAuthoring(value));
            materials = ConvertAll(
                catalog.Materials,
                value => new MaterialDefinitionAuthoring(value));
            recipes = ConvertAll(catalog.Recipes, value => new RecipeDefinitionAuthoring(value));
            professions = ConvertAll(
                catalog.Professions,
                value => new ProfessionDefinitionAuthoring(value));
            rarities = ConvertAll(
                catalog.Rarities,
                value => new RarityDefinitionAuthoring(value));
            tiers = ConvertAll(catalog.Tiers, value => new TierDefinitionAuthoring(value));
        }
#endif

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private static List<TOutput> ConvertAll<TInput, TOutput>(
            IReadOnlyList<TInput> source,
            Func<TInput, TOutput> convert)
        {
            if (source == null)
                return new List<TOutput>();

            var result = new List<TOutput>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(convert(source[i]));
            return result;
        }
    }

    [Serializable]
    public sealed class HeroDefinitionAuthoring
    {
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private HeroArchetype archetype;
        [SerializeField] private long baseHealth;
        [SerializeField] private long baseAttack;
        [SerializeField] private long baseDefense;
        [SerializeField] private int baseSpeed;
        [SerializeField] private Rarity initialRarity;
        [SerializeField] private List<string> tags = new List<string>();
        [SerializeField] private List<string> abilityDefinitionIds = new List<string>();

        public HeroDefinitionAuthoring()
        {
        }

        public HeroDefinitionAuthoring(HeroDefinition definition)
        {
            definitionId = definition.DefinitionId;
            displayName = definition.DisplayName;
            description = definition.Description;
            archetype = definition.Archetype;
            baseHealth = definition.BaseHealth;
            baseAttack = definition.BaseAttack;
            baseDefense = definition.BaseDefense;
            baseSpeed = definition.BaseSpeed;
            initialRarity = definition.InitialRarity;
            tags = new List<string>(definition.Tags);
            abilityDefinitionIds = new List<string>(definition.AbilityDefinitionIds);
        }

        public HeroDefinition ToDomain()
        {
            return new HeroDefinition(
                definitionId, displayName, description, archetype, baseHealth, baseAttack,
                baseDefense, baseSpeed, initialRarity, tags, abilityDefinitionIds);
        }
    }

    [Serializable]
    public sealed class ItemDefinitionAuthoring
    {
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private ItemType itemType;
        [SerializeField] private ContentTier tier = ContentTier.Tier1;
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField] private bool stackable;
        [SerializeField] private long maxStackSize = 1;
        [SerializeField] private bool tradable = true;
        [SerializeField] private bool sellable = true;
        [SerializeField] private bool destroyable = true;
        [SerializeField] private string iconReference = string.Empty;
        [SerializeField] private List<string> tags = new List<string>();

        public ItemDefinitionAuthoring()
        {
        }

        public ItemDefinitionAuthoring(ItemDefinition definition)
        {
            definitionId = definition.DefinitionId;
            displayName = definition.DisplayName;
            description = definition.Description;
            itemType = definition.ItemType;
            tier = definition.Tier;
            rarity = definition.Rarity;
            stackable = definition.Stackable;
            maxStackSize = definition.MaxStackSize;
            tradable = definition.Tradable;
            sellable = definition.Sellable;
            destroyable = definition.Destroyable;
            iconReference = definition.IconReference;
            tags = new List<string>(definition.Tags);
        }

        public ItemDefinition ToDomain()
        {
            return new ItemDefinition(
                definitionId, displayName, description, itemType, tier, rarity, stackable,
                maxStackSize, tradable, sellable, destroyable, iconReference, tags);
        }
    }

    [Serializable]
    public sealed class EquipmentDefinitionAuthoring
    {
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private ContentTier tier = ContentTier.Tier1;
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField] private bool tradable = true;
        [SerializeField] private bool sellable = true;
        [SerializeField] private bool destroyable = true;
        [SerializeField] private EquipmentSlot equipmentSlot;
        [SerializeField] private long statBudget;
        [SerializeField] private List<string> allowedAffixes = new List<string>();
        [SerializeField] private int requiredLevel = 1;
        [SerializeField] private List<string> requiredHeroTags = new List<string>();
        [SerializeField] private ProfessionType professionSource;
        [SerializeField] private int enhancementLimit;
        [SerializeField] private BindingRule bindingRule;
        [SerializeField] private string iconReference = string.Empty;
        [SerializeField] private List<string> tags = new List<string>();

        public EquipmentDefinitionAuthoring()
        {
        }

        public EquipmentDefinitionAuthoring(EquipmentDefinition definition)
        {
            definitionId = definition.DefinitionId;
            displayName = definition.DisplayName;
            description = definition.Description;
            tier = definition.Tier;
            rarity = definition.Rarity;
            tradable = definition.Tradable;
            sellable = definition.Sellable;
            destroyable = definition.Destroyable;
            equipmentSlot = definition.EquipmentSlot;
            statBudget = definition.StatBudget;
            allowedAffixes = new List<string>(definition.AllowedAffixes);
            requiredLevel = definition.RequiredLevel;
            requiredHeroTags = new List<string>(definition.RequiredHeroTags);
            professionSource = definition.ProfessionSource;
            enhancementLimit = definition.EnhancementLimit;
            bindingRule = definition.BindingRule;
            iconReference = definition.IconReference;
            tags = new List<string>(definition.Tags);
        }

        public EquipmentDefinition ToDomain()
        {
            return new EquipmentDefinition(
                definitionId, displayName, description, tier, rarity, tradable, sellable,
                destroyable, equipmentSlot, statBudget, allowedAffixes, requiredLevel,
                requiredHeroTags, professionSource, enhancementLimit, bindingRule,
                iconReference, tags);
        }
    }

    [Serializable]
    public sealed class MaterialDefinitionAuthoring
    {
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private MaterialCategory materialCategory;
        [SerializeField] private ContentTier tier = ContentTier.Tier1;
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField] private bool refinable;
        [SerializeField] private string refinedOutputDefinitionId = string.Empty;
        [SerializeField] private List<string> sourceTags = new List<string>();
        [SerializeField] private bool tradable = true;
        [SerializeField] private long stackSize = 999;
        [SerializeField] private bool sellable = true;
        [SerializeField] private bool destroyable = true;
        [SerializeField] private string iconReference = string.Empty;
        [SerializeField] private List<string> tags = new List<string>();

        public MaterialDefinitionAuthoring()
        {
        }

        public MaterialDefinitionAuthoring(MaterialDefinition definition)
        {
            definitionId = definition.DefinitionId;
            displayName = definition.DisplayName;
            description = definition.Description;
            materialCategory = definition.MaterialCategory;
            tier = definition.Tier;
            rarity = definition.Rarity;
            refinable = definition.Refinable;
            refinedOutputDefinitionId = definition.RefinedOutputDefinitionId;
            sourceTags = new List<string>(definition.SourceTags);
            tradable = definition.Tradable;
            stackSize = definition.StackSize;
            sellable = definition.Sellable;
            destroyable = definition.Destroyable;
            iconReference = definition.IconReference;
            tags = new List<string>(definition.Tags);
        }

        public MaterialDefinition ToDomain()
        {
            return new MaterialDefinition(
                definitionId, displayName, description, materialCategory, tier, rarity,
                refinable, refinedOutputDefinitionId, sourceTags, tradable, stackSize,
                sellable, destroyable, iconReference, tags);
        }
    }

    [Serializable]
    public sealed class RecipeIngredientAuthoring
    {
        [SerializeField] private string itemDefinitionId = string.Empty;
        [SerializeField] private long quantity = 1;
        [SerializeField] private bool consumed = true;
        [SerializeField] private bool optional;
        [SerializeField] private string acceptedSubstitutionTag = string.Empty;

        public RecipeIngredientAuthoring()
        {
        }

        public RecipeIngredientAuthoring(RecipeIngredientDefinition definition)
        {
            itemDefinitionId = definition.ItemDefinitionId;
            quantity = definition.Quantity;
            consumed = definition.Consumed;
            optional = definition.Optional;
            acceptedSubstitutionTag = definition.AcceptedSubstitutionTag;
        }

        public RecipeIngredientDefinition ToDomain()
        {
            return new RecipeIngredientDefinition(
                itemDefinitionId, quantity, consumed, optional, acceptedSubstitutionTag);
        }
    }

    [Serializable]
    public sealed class RecipeDefinitionAuthoring
    {
        [SerializeField] private string recipeId = string.Empty;
        [SerializeField] private string outputDefinitionId = string.Empty;
        [SerializeField] private long outputQuantity = 1;
        [SerializeField] private ProfessionType profession;
        [SerializeField] private int requiredProfessionLevel = 1;
        [SerializeField] private ProfessionRank requiredProfessionRank = ProfessionRank.Apprentice;
        [SerializeField] private ContentTier requiredTier = ContentTier.Tier1;
        [SerializeField] private ContentTier requiredStationTier = ContentTier.Tier1;
        [SerializeField] private int durationSeconds = 30;
        [SerializeField] private int focusCost = 1;
        [SerializeField] private long goldCost;
        [SerializeField] private List<RecipeIngredientAuthoring> ingredients =
            new List<RecipeIngredientAuthoring>();
        [SerializeField] private List<RecipeIngredientAuthoring> optionalCatalysts =
            new List<RecipeIngredientAuthoring>();
        [SerializeField] private bool eligibleForMythicCrafting;
        [SerializeField] private bool mayBeUsedInCraftingCommissions;
        [SerializeField] private bool explicitlyFree;

        public RecipeDefinitionAuthoring()
        {
        }

        public RecipeDefinitionAuthoring(RecipeDefinition definition)
        {
            recipeId = definition.RecipeId;
            outputDefinitionId = definition.OutputDefinitionId;
            outputQuantity = definition.OutputQuantity;
            profession = definition.Profession;
            requiredProfessionLevel = definition.RequiredProfessionLevel;
            requiredProfessionRank = definition.RequiredProfessionRank;
            requiredTier = definition.RequiredTier;
            requiredStationTier = definition.RequiredStationTier;
            durationSeconds = definition.DurationSeconds;
            focusCost = definition.FocusCost;
            goldCost = definition.GoldCost;
            ingredients = ConvertIngredients(definition.Ingredients);
            optionalCatalysts = ConvertIngredients(definition.OptionalCatalysts);
            eligibleForMythicCrafting = definition.EligibleForMythicCrafting;
            mayBeUsedInCraftingCommissions = definition.MayBeUsedInCraftingCommissions;
            explicitlyFree = definition.ExplicitlyFree;
        }

        public RecipeDefinition ToDomain()
        {
            return new RecipeDefinition(
                recipeId, outputDefinitionId, outputQuantity, profession,
                requiredProfessionLevel, requiredProfessionRank, requiredTier,
                requiredStationTier, durationSeconds, focusCost, goldCost,
                ConvertIngredients(ingredients), ConvertIngredients(optionalCatalysts),
                eligibleForMythicCrafting, mayBeUsedInCraftingCommissions, explicitlyFree);
        }

        private static List<RecipeIngredientAuthoring> ConvertIngredients(
            IReadOnlyList<RecipeIngredientDefinition> source)
        {
            var result = new List<RecipeIngredientAuthoring>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(source[i] == null ? null : new RecipeIngredientAuthoring(source[i]));
            return result;
        }

        private static List<RecipeIngredientDefinition> ConvertIngredients(
            IReadOnlyList<RecipeIngredientAuthoring> source)
        {
            if (source == null)
                return new List<RecipeIngredientDefinition>();

            var result = new List<RecipeIngredientDefinition>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(source[i]?.ToDomain());
            return result;
        }
    }

    [Serializable]
    public sealed class ProfessionUnlockThresholdAuthoring
    {
        [SerializeField] private string unlockId = string.Empty;
        [SerializeField] private int requiredLevel = 1;

        public ProfessionUnlockThresholdAuthoring()
        {
        }

        public ProfessionUnlockThresholdAuthoring(ProfessionUnlockThreshold definition)
        {
            unlockId = definition.UnlockId;
            requiredLevel = definition.RequiredLevel;
        }

        public ProfessionUnlockThreshold ToDomain()
        {
            return new ProfessionUnlockThreshold(unlockId, requiredLevel);
        }
    }

    [Serializable]
    public sealed class ProfessionRankThresholdAuthoring
    {
        [SerializeField] private ProfessionRank rank;
        [SerializeField] private int minimumLevel = 1;

        public ProfessionRankThresholdAuthoring()
        {
        }

        public ProfessionRankThresholdAuthoring(ProfessionRankThreshold definition)
        {
            rank = definition.Rank;
            minimumLevel = definition.MinimumLevel;
        }

        public ProfessionRankThreshold ToDomain()
        {
            return new ProfessionRankThreshold(rank, minimumLevel);
        }
    }

    [Serializable]
    public sealed class ProfessionTierUnlockThresholdAuthoring
    {
        [SerializeField] private ContentTier tier = ContentTier.Tier1;
        [SerializeField] private int minimumLevel = 1;

        public ProfessionTierUnlockThresholdAuthoring()
        {
        }

        public ProfessionTierUnlockThresholdAuthoring(
            ProfessionTierUnlockThreshold definition)
        {
            tier = definition.Tier;
            minimumLevel = definition.MinimumLevel;
        }

        public ProfessionTierUnlockThreshold ToDomain()
        {
            return new ProfessionTierUnlockThreshold(tier, minimumLevel);
        }
    }

    [Serializable]
    public sealed class MasteryBranchAuthoring
    {
        [SerializeField] private string branchId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;

        public MasteryBranchAuthoring()
        {
        }

        public MasteryBranchAuthoring(MasteryBranchDefinition definition)
        {
            branchId = definition.BranchId;
            displayName = definition.DisplayName;
            description = definition.Description;
        }

        public MasteryBranchDefinition ToDomain()
        {
            return new MasteryBranchDefinition(branchId, displayName, description);
        }
    }

    [Serializable]
    public sealed class SpecializationBonusAuthoring
    {
        [SerializeField] private string bonusId = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private int magnitudeBasisPoints;

        public SpecializationBonusAuthoring()
        {
        }

        public SpecializationBonusAuthoring(SpecializationBonusDefinition definition)
        {
            bonusId = definition.BonusId;
            description = definition.Description;
            magnitudeBasisPoints = definition.MagnitudeBasisPoints;
        }

        public SpecializationBonusDefinition ToDomain()
        {
            return new SpecializationBonusDefinition(
                bonusId, description, magnitudeBasisPoints);
        }
    }

    [Serializable]
    public sealed class ProfessionDefinitionAuthoring
    {
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private ProfessionType professionType;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private string stationName = string.Empty;
        [SerializeField] private int maxLevel = 100;
        [SerializeField] private List<ProfessionUnlockThresholdAuthoring> unlockThresholds =
            new List<ProfessionUnlockThresholdAuthoring>();
        [SerializeField] private List<ProfessionRankThresholdAuthoring> rankThresholds =
            new List<ProfessionRankThresholdAuthoring>();
        [SerializeField] private List<ProfessionTierUnlockThresholdAuthoring>
            tierUnlockThresholds = new List<ProfessionTierUnlockThresholdAuthoring>();
        [SerializeField] private List<MasteryBranchAuthoring> masteryBranches =
            new List<MasteryBranchAuthoring>();
        [SerializeField] private List<SpecializationBonusAuthoring> specializationBonuses =
            new List<SpecializationBonusAuthoring>();

        public ProfessionDefinitionAuthoring()
        {
        }

        public ProfessionDefinitionAuthoring(ProfessionDefinition definition)
        {
            definitionId = definition.DefinitionId;
            professionType = definition.ProfessionType;
            displayName = definition.DisplayName;
            description = definition.Description;
            stationName = definition.StationName;
            maxLevel = definition.MaxLevel;
            unlockThresholds = ConvertAll(
                definition.UnlockThresholds,
                value => new ProfessionUnlockThresholdAuthoring(value));
            rankThresholds = ConvertAll(
                definition.RankThresholds,
                value => new ProfessionRankThresholdAuthoring(value));
            tierUnlockThresholds = ConvertAll(
                definition.TierUnlockThresholds,
                value => new ProfessionTierUnlockThresholdAuthoring(value));
            masteryBranches = ConvertAll(
                definition.MasteryBranches,
                value => new MasteryBranchAuthoring(value));
            specializationBonuses = ConvertAll(
                definition.SpecializationBonuses,
                value => new SpecializationBonusAuthoring(value));
        }

        public ProfessionDefinition ToDomain()
        {
            return new ProfessionDefinition(
                definitionId, professionType, displayName, description, stationName, maxLevel,
                ConvertAll(unlockThresholds, value => value?.ToDomain()),
                ConvertAll(rankThresholds, value => value?.ToDomain()),
                ConvertAll(tierUnlockThresholds, value => value?.ToDomain()),
                ConvertAll(masteryBranches, value => value?.ToDomain()),
                ConvertAll(specializationBonuses, value => value?.ToDomain()));
        }

        private static List<TOutput> ConvertAll<TInput, TOutput>(
            IReadOnlyList<TInput> source,
            Func<TInput, TOutput> convert)
        {
            if (source == null)
                return new List<TOutput>();

            var result = new List<TOutput>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(convert(source[i]));
            return result;
        }
    }

    [Serializable]
    public sealed class RarityDefinitionAuthoring
    {
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private Rarity rarity;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private int heroMultiplierBasisPoints = 10000;
        [SerializeField] private int equipmentMultiplierBasisPoints = 10000;
        [SerializeField] private int maximumAffixCount;
        [SerializeField] private int maximumEnhancementLevel;
        [SerializeField] private Color32 visualColor = Color.white;
        [SerializeField] private int sortOrder;
        [SerializeField] private int marketWeightBasisPoints;
        [SerializeField] private List<string> economicMetadata = new List<string>();

        public RarityDefinitionAuthoring()
        {
        }

        public RarityDefinitionAuthoring(RarityDefinition definition)
        {
            definitionId = definition.DefinitionId;
            rarity = definition.Rarity;
            displayName = definition.DisplayName;
            heroMultiplierBasisPoints = ToBasisPoints(definition.HeroMultiplier);
            equipmentMultiplierBasisPoints = ToBasisPoints(definition.EquipmentMultiplier);
            maximumAffixCount = definition.MaximumAffixCount;
            maximumEnhancementLevel = definition.MaximumEnhancementLevel;
            visualColor = new Color32(
                definition.VisualColor.Red,
                definition.VisualColor.Green,
                definition.VisualColor.Blue,
                definition.VisualColor.Alpha);
            sortOrder = definition.SortOrder;
            marketWeightBasisPoints = definition.MarketWeightBasisPoints;
            economicMetadata = new List<string>(definition.EconomicMetadata);
        }

        public RarityDefinition ToDomain()
        {
            return new RarityDefinition(
                definitionId, rarity, displayName,
                heroMultiplierBasisPoints / 10000m,
                equipmentMultiplierBasisPoints / 10000m,
                maximumAffixCount, maximumEnhancementLevel,
                new VisualColor(visualColor.r, visualColor.g, visualColor.b, visualColor.a),
                sortOrder, marketWeightBasisPoints, economicMetadata);
        }

        private static int ToBasisPoints(decimal multiplier)
        {
            return decimal.ToInt32(decimal.Round(
                multiplier * 10000m,
                0,
                MidpointRounding.AwayFromZero));
        }
    }

    [Serializable]
    public sealed class TierDefinitionAuthoring
    {
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private ContentTier tier = ContentTier.Tier1;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private int minimumProfessionLevel = 1;
        [SerializeField] private long baseEquipmentBudget;
        [SerializeField] private int defaultCraftDurationMultiplierBasisPoints = 10000;
        [SerializeField] private int defaultGoldMultiplierBasisPoints = 10000;
        [SerializeField] private int defaultMaterialMultiplierBasisPoints = 10000;
        [SerializeField] private long recommendedPlayerPower;
        [SerializeField] private List<string> tags = new List<string>();

        public TierDefinitionAuthoring()
        {
        }

        public TierDefinitionAuthoring(TierDefinition definition)
        {
            definitionId = definition.DefinitionId;
            tier = definition.Tier;
            displayName = definition.DisplayName;
            minimumProfessionLevel = definition.MinimumProfessionLevel;
            baseEquipmentBudget = definition.BaseEquipmentBudget;
            defaultCraftDurationMultiplierBasisPoints =
                ToBasisPoints(definition.DefaultCraftDurationMultiplier);
            defaultGoldMultiplierBasisPoints = ToBasisPoints(definition.DefaultGoldMultiplier);
            defaultMaterialMultiplierBasisPoints =
                ToBasisPoints(definition.DefaultMaterialMultiplier);
            recommendedPlayerPower = definition.RecommendedPlayerPower;
            tags = new List<string>(definition.Tags);
        }

        public TierDefinition ToDomain()
        {
            return new TierDefinition(
                definitionId, tier, displayName, minimumProfessionLevel, baseEquipmentBudget,
                defaultCraftDurationMultiplierBasisPoints / 10000m,
                defaultGoldMultiplierBasisPoints / 10000m,
                defaultMaterialMultiplierBasisPoints / 10000m,
                recommendedPlayerPower, tags);
        }

        private static int ToBasisPoints(decimal multiplier)
        {
            return decimal.ToInt32(decimal.Round(
                multiplier * 10000m,
                0,
                MidpointRounding.AwayFromZero));
        }
    }
}
