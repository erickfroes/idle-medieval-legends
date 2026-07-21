using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IdleMedievalLegends.Domain.Common;

namespace IdleMedievalLegends.Domain.Content
{
    public abstract class ContentDefinition
    {
        protected ContentDefinition(string definitionId)
        {
            DefinitionId = definitionId;
        }

        public string DefinitionId { get; }
    }

    public sealed class HeroDefinition : ContentDefinition
    {
        public HeroDefinition(
            string definitionId,
            string displayName,
            string description,
            HeroArchetype archetype,
            long baseHealth,
            long baseAttack,
            long baseDefense,
            int baseSpeed,
            Rarity initialRarity,
            IEnumerable<string> tags = null,
            IEnumerable<string> abilityDefinitionIds = null)
            : base(definitionId)
        {
            DisplayName = displayName;
            Description = description;
            Archetype = archetype;
            BaseHealth = baseHealth;
            BaseAttack = baseAttack;
            BaseDefense = baseDefense;
            BaseSpeed = baseSpeed;
            InitialRarity = initialRarity;
            Tags = DefinitionCollection.Copy(tags);
            AbilityDefinitionIds = DefinitionCollection.Copy(abilityDefinitionIds);
        }

        public string DisplayName { get; }
        public string Description { get; }
        public HeroArchetype Archetype { get; }
        public long BaseHealth { get; }
        public long BaseAttack { get; }
        public long BaseDefense { get; }
        public int BaseSpeed { get; }
        public Rarity InitialRarity { get; }
        public IReadOnlyList<string> Tags { get; }

        /// <summary>
        /// IDs reservados para um catálogo futuro de habilidades. Não carregam
        /// comportamento nem são resolvidos nesta tarefa.
        /// </summary>
        public IReadOnlyList<string> AbilityDefinitionIds { get; }
    }

    public class ItemDefinition : ContentDefinition
    {
        public ItemDefinition(
            string definitionId,
            string displayName,
            string description,
            ItemType itemType,
            ContentTier tier,
            Rarity rarity,
            bool stackable,
            long maxStackSize,
            bool tradable,
            bool sellable,
            bool destroyable,
            string iconReference = null,
            IEnumerable<string> tags = null)
            : base(definitionId)
        {
            DisplayName = displayName;
            Description = description;
            ItemType = itemType;
            Tier = tier;
            Rarity = rarity;
            Stackable = stackable;
            MaxStackSize = maxStackSize;
            Tradable = tradable;
            Sellable = sellable;
            Destroyable = destroyable;
            IconReference = iconReference;
            Tags = DefinitionCollection.Copy(tags);
        }

        public string DisplayName { get; }
        public string Description { get; }
        public ItemType ItemType { get; }
        public ContentTier Tier { get; }
        public Rarity Rarity { get; }
        public bool Stackable { get; }
        public long MaxStackSize { get; }
        public bool Tradable { get; }
        public bool Sellable { get; }
        public bool Destroyable { get; }
        public string IconReference { get; }
        public IReadOnlyList<string> Tags { get; }
    }

    public sealed class EquipmentDefinition : ItemDefinition
    {
        public EquipmentDefinition(
            string definitionId,
            string displayName,
            string description,
            ContentTier tier,
            Rarity rarity,
            bool tradable,
            bool sellable,
            bool destroyable,
            EquipmentSlot equipmentSlot,
            long statBudget,
            IEnumerable<string> allowedAffixes,
            int requiredLevel,
            IEnumerable<string> requiredHeroTags,
            ProfessionType professionSource,
            int enhancementLimit,
            BindingRule bindingRule,
            string iconReference = null,
            IEnumerable<string> tags = null)
            : base(
                definitionId,
                displayName,
                description,
                ItemType.Equipment,
                tier,
                rarity,
                false,
                1,
                tradable,
                sellable,
                destroyable,
                iconReference,
                tags)
        {
            EquipmentSlot = equipmentSlot;
            StatBudget = statBudget;
            AllowedAffixes = DefinitionCollection.Copy(allowedAffixes);
            RequiredLevel = requiredLevel;
            RequiredHeroTags = DefinitionCollection.Copy(requiredHeroTags);
            ProfessionSource = professionSource;
            EnhancementLimit = enhancementLimit;
            BindingRule = bindingRule;
        }

        public EquipmentSlot EquipmentSlot { get; }
        public long StatBudget { get; }
        public IReadOnlyList<string> AllowedAffixes { get; }
        public int RequiredLevel { get; }
        public IReadOnlyList<string> RequiredHeroTags { get; }
        public ProfessionType ProfessionSource { get; }
        public int EnhancementLimit { get; }
        public BindingRule BindingRule { get; }
    }

    public sealed class MaterialDefinition : ItemDefinition
    {
        public MaterialDefinition(
            string definitionId,
            string displayName,
            string description,
            MaterialCategory materialCategory,
            ContentTier tier,
            Rarity rarity,
            bool refinable,
            string refinedOutputDefinitionId,
            IEnumerable<string> sourceTags,
            bool tradable,
            long stackSize,
            bool sellable = true,
            bool destroyable = true,
            string iconReference = null,
            IEnumerable<string> tags = null)
            : base(
                definitionId,
                displayName,
                description,
                ItemType.Material,
                tier,
                rarity,
                true,
                stackSize,
                tradable,
                sellable,
                destroyable,
                iconReference,
                tags)
        {
            MaterialCategory = materialCategory;
            Refinable = refinable;
            RefinedOutputDefinitionId = refinedOutputDefinitionId;
            SourceTags = DefinitionCollection.Copy(sourceTags);
        }

        public MaterialCategory MaterialCategory { get; }
        public bool Refinable { get; }
        public string RefinedOutputDefinitionId { get; }
        public IReadOnlyList<string> SourceTags { get; }
        public long StackSize => MaxStackSize;
    }

    public sealed class RecipeIngredientDefinition
    {
        public RecipeIngredientDefinition(
            string itemDefinitionId,
            long quantity,
            bool consumed,
            bool optional,
            string acceptedSubstitutionTag = null)
        {
            ItemDefinitionId = itemDefinitionId;
            Quantity = quantity;
            Consumed = consumed;
            Optional = optional;
            AcceptedSubstitutionTag = acceptedSubstitutionTag;
        }

        public string ItemDefinitionId { get; }
        public long Quantity { get; }
        public bool Consumed { get; }
        public bool Optional { get; }
        public string AcceptedSubstitutionTag { get; }
    }

    public sealed class RecipeDefinition : ContentDefinition
    {
        public RecipeDefinition(
            string recipeId,
            string outputDefinitionId,
            long outputQuantity,
            ProfessionType profession,
            int requiredProfessionLevel,
            ProfessionRank requiredProfessionRank,
            ContentTier requiredTier,
            ContentTier requiredStationTier,
            int durationSeconds,
            int focusCost,
            long goldCost,
            IEnumerable<RecipeIngredientDefinition> ingredients,
            IEnumerable<RecipeIngredientDefinition> optionalCatalysts,
            bool eligibleForMythicCrafting,
            bool mayBeUsedInCraftingCommissions,
            bool explicitlyFree = false)
            : base(recipeId)
        {
            OutputDefinitionId = outputDefinitionId;
            OutputQuantity = outputQuantity;
            Profession = profession;
            RequiredProfessionLevel = requiredProfessionLevel;
            RequiredProfessionRank = requiredProfessionRank;
            RequiredTier = requiredTier;
            RequiredStationTier = requiredStationTier;
            DurationSeconds = durationSeconds;
            FocusCost = focusCost;
            GoldCost = goldCost;
            Ingredients = DefinitionCollection.Copy(ingredients);
            OptionalCatalysts = DefinitionCollection.Copy(optionalCatalysts);
            EligibleForMythicCrafting = eligibleForMythicCrafting;
            MayBeUsedInCraftingCommissions = mayBeUsedInCraftingCommissions;
            ExplicitlyFree = explicitlyFree;
        }

        public string RecipeId => DefinitionId;
        public string OutputDefinitionId { get; }
        public long OutputQuantity { get; }
        public ProfessionType Profession { get; }
        public int RequiredProfessionLevel { get; }
        public ProfessionRank RequiredProfessionRank { get; }
        public ContentTier RequiredTier { get; }
        public ContentTier RequiredStationTier { get; }
        public int DurationSeconds { get; }
        public int FocusCost { get; }
        public long GoldCost { get; }
        public IReadOnlyList<RecipeIngredientDefinition> Ingredients { get; }
        public IReadOnlyList<RecipeIngredientDefinition> OptionalCatalysts { get; }
        public bool EligibleForMythicCrafting { get; }
        public bool MayBeUsedInCraftingCommissions { get; }
        public bool ExplicitlyFree { get; }
    }

    public sealed class ProfessionUnlockThreshold
    {
        public ProfessionUnlockThreshold(string unlockId, int requiredLevel)
        {
            UnlockId = unlockId;
            RequiredLevel = requiredLevel;
        }

        public string UnlockId { get; }
        public int RequiredLevel { get; }
    }

    public sealed class ProfessionRankThreshold
    {
        public ProfessionRankThreshold(ProfessionRank rank, int minimumLevel)
        {
            Rank = rank;
            MinimumLevel = minimumLevel;
        }

        public ProfessionRank Rank { get; }
        public int MinimumLevel { get; }
    }

    public sealed class ProfessionTierUnlockThreshold
    {
        public ProfessionTierUnlockThreshold(ContentTier tier, int minimumLevel)
        {
            Tier = tier;
            MinimumLevel = minimumLevel;
        }

        public ContentTier Tier { get; }
        public int MinimumLevel { get; }
    }

    public sealed class MasteryBranchDefinition
    {
        public MasteryBranchDefinition(
            string branchId,
            string displayName,
            string description)
        {
            BranchId = branchId;
            DisplayName = displayName;
            Description = description;
        }

        public string BranchId { get; }
        public string DisplayName { get; }
        public string Description { get; }
    }

    public sealed class SpecializationBonusDefinition
    {
        public SpecializationBonusDefinition(
            string bonusId,
            string description,
            int magnitudeBasisPoints)
        {
            BonusId = bonusId;
            Description = description;
            MagnitudeBasisPoints = magnitudeBasisPoints;
        }

        public string BonusId { get; }
        public string Description { get; }
        public int MagnitudeBasisPoints { get; }
    }

    public sealed class ProfessionDefinition : ContentDefinition
    {
        public ProfessionDefinition(
            string definitionId,
            ProfessionType professionType,
            string displayName,
            string description,
            string stationName,
            int maxLevel,
            IEnumerable<ProfessionUnlockThreshold> unlockThresholds,
            IEnumerable<ProfessionRankThreshold> rankThresholds,
            IEnumerable<ProfessionTierUnlockThreshold> tierUnlockThresholds,
            IEnumerable<MasteryBranchDefinition> masteryBranches,
            IEnumerable<SpecializationBonusDefinition> specializationBonuses)
            : base(definitionId)
        {
            ProfessionType = professionType;
            DisplayName = displayName;
            Description = description;
            StationName = stationName;
            MaxLevel = maxLevel;
            UnlockThresholds = DefinitionCollection.Copy(unlockThresholds);
            RankThresholds = DefinitionCollection.Copy(rankThresholds);
            TierUnlockThresholds = DefinitionCollection.Copy(tierUnlockThresholds);
            MasteryBranches = DefinitionCollection.Copy(masteryBranches);
            SpecializationBonuses = DefinitionCollection.Copy(specializationBonuses);
        }

        public ProfessionType ProfessionType { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string StationName { get; }
        public int MaxLevel { get; }
        public IReadOnlyList<ProfessionUnlockThreshold> UnlockThresholds { get; }
        public IReadOnlyList<ProfessionRankThreshold> RankThresholds { get; }
        public IReadOnlyList<ProfessionTierUnlockThreshold> TierUnlockThresholds { get; }
        public IReadOnlyList<MasteryBranchDefinition> MasteryBranches { get; }
        public IReadOnlyList<SpecializationBonusDefinition> SpecializationBonuses { get; }
    }

    public readonly struct VisualColor : IEquatable<VisualColor>
    {
        public VisualColor(byte red, byte green, byte blue, byte alpha = byte.MaxValue)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
        public byte Alpha { get; }

        public string ToHex()
        {
            return $"#{Red:X2}{Green:X2}{Blue:X2}{Alpha:X2}";
        }

        public bool Equals(VisualColor other)
        {
            return Red == other.Red && Green == other.Green &&
                   Blue == other.Blue && Alpha == other.Alpha;
        }

        public override bool Equals(object obj)
        {
            return obj is VisualColor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Red;
                hash = (hash * 397) ^ Green;
                hash = (hash * 397) ^ Blue;
                return (hash * 397) ^ Alpha;
            }
        }
    }

    public sealed class RarityDefinition : ContentDefinition
    {
        public RarityDefinition(
            string definitionId,
            Rarity rarity,
            string displayName,
            decimal heroMultiplier,
            decimal equipmentMultiplier,
            int maximumAffixCount,
            int maximumEnhancementLevel,
            VisualColor visualColor,
            int sortOrder,
            int marketWeightBasisPoints,
            IEnumerable<string> economicMetadata = null)
            : base(definitionId)
        {
            Rarity = rarity;
            DisplayName = displayName;
            HeroMultiplier = heroMultiplier;
            EquipmentMultiplier = equipmentMultiplier;
            MaximumAffixCount = maximumAffixCount;
            MaximumEnhancementLevel = maximumEnhancementLevel;
            VisualColor = visualColor;
            SortOrder = sortOrder;
            MarketWeightBasisPoints = marketWeightBasisPoints;
            EconomicMetadata = DefinitionCollection.Copy(economicMetadata);
        }

        public Rarity Rarity { get; }
        public string DisplayName { get; }
        public decimal HeroMultiplier { get; }
        public decimal EquipmentMultiplier { get; }
        public int MaximumAffixCount { get; }
        public int MaximumEnhancementLevel { get; }
        public VisualColor VisualColor { get; }
        public int SortOrder { get; }
        public int MarketWeightBasisPoints { get; }
        public IReadOnlyList<string> EconomicMetadata { get; }
    }

    public sealed class TierDefinition : ContentDefinition
    {
        public TierDefinition(
            string definitionId,
            ContentTier tier,
            string displayName,
            int minimumProfessionLevel,
            long baseEquipmentBudget,
            decimal defaultCraftDurationMultiplier,
            decimal defaultGoldMultiplier,
            decimal defaultMaterialMultiplier,
            long recommendedPlayerPower,
            IEnumerable<string> tags = null)
            : base(definitionId)
        {
            Tier = tier;
            DisplayName = displayName;
            MinimumProfessionLevel = minimumProfessionLevel;
            BaseEquipmentBudget = baseEquipmentBudget;
            DefaultCraftDurationMultiplier = defaultCraftDurationMultiplier;
            DefaultGoldMultiplier = defaultGoldMultiplier;
            DefaultMaterialMultiplier = defaultMaterialMultiplier;
            RecommendedPlayerPower = recommendedPlayerPower;
            Tags = DefinitionCollection.Copy(tags);
        }

        public ContentTier Tier { get; }
        public string DisplayName { get; }
        public int MinimumProfessionLevel { get; }
        public long BaseEquipmentBudget { get; }
        public decimal DefaultCraftDurationMultiplier { get; }
        public decimal DefaultGoldMultiplier { get; }
        public decimal DefaultMaterialMultiplier { get; }
        public long RecommendedPlayerPower { get; }
        public IReadOnlyList<string> Tags { get; }
    }

    internal static class DefinitionCollection
    {
        public static IReadOnlyList<T> Copy<T>(IEnumerable<T> source)
        {
            var copy = source == null ? new List<T>() : new List<T>(source);
            return new ReadOnlyCollection<T>(copy);
        }
    }
}
