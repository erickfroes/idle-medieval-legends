using System;
using IdleMedievalLegends.Domain.Common;

namespace IdleMedievalLegends.Domain.Content
{
    /// <summary>
    /// Raridade persistida do catálogo v2. Os valores são parte do contrato de dados.
    /// </summary>
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5
    }

    /// <summary>
    /// Tier persistido do catálogo. Tier e raridade são eixos independentes.
    /// </summary>
    public enum ContentTier
    {
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
        Tier4 = 4,
        Tier5 = 5,
        Tier6 = 6,
        Tier7 = 7,
        Tier8 = 8,
        Tier9 = 9
    }

    /// <summary>
    /// Profissão persistida do catálogo. Mantém os valores 1..5 do contrato legado.
    /// </summary>
    public enum ProfessionType
    {
        Blacksmith = 1,
        Tailor = 2,
        Enchanter = 3,
        Alchemist = 4,
        Gatherer = 5
    }

    public enum HeroArchetype
    {
        None = 0,
        Tank = 1,
        Warrior = 2,
        Ranger = 3,
        Mage = 4,
        Support = 5,
        Assassin = 6
    }

    public enum ItemType
    {
        None = 0,
        Equipment = 1,
        Material = 2,
        Consumable = 3,
        Enchantment = 4,
        RecipeDiagram = 5,
        Tool = 6
    }

    public enum EquipmentSlot
    {
        None = 0,
        Weapon = 1,
        OffHand = 2,
        Head = 3,
        Chest = 4,
        Hands = 5,
        Legs = 6,
        Feet = 7,
        Neck = 8,
        Ring = 9
    }

    public enum MaterialCategory
    {
        None = 0,
        Ore = 1,
        Ingot = 2,
        Hide = 3,
        Leather = 4,
        Fiber = 5,
        Cloth = 6,
        Herb = 7,
        Essence = 8,
        Catalyst = 9
    }

    public enum BindingRule
    {
        None = 0,
        UnboundUntilEquipped = 1,
        AccountBoundOnAcquire = 2,
        AlwaysUnbound = 3
    }

    public enum ContentValidationSeverity
    {
        Warning = 0,
        Error = 1
    }

    /// <summary>
    /// Ponte explícita entre os tipos novos do catálogo e os tipos persistidos
    /// introduzidos antes da Task 003. Não existe conversão implícita.
    /// </summary>
    public static class LegacyProgressionTypeAdapter
    {
        public static Rarity ToCatalogRarity(this GameRarity value)
        {
            if (!value.IsValid())
                throw new ArgumentOutOfRangeException(nameof(value), value, "Raridade inválida.");

            return (Rarity)(int)value;
        }

        public static GameRarity ToLegacyRarity(this Rarity value)
        {
            if (!Enum.IsDefined(typeof(Rarity), value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Raridade inválida.");

            return (GameRarity)(int)value;
        }

        public static ContentTier ToContentTier(this ItemTier value)
        {
            if (!value.IsValid())
                throw new ArgumentOutOfRangeException(nameof(value), value, "Tier inválido.");

            return (ContentTier)(int)value;
        }

        public static ItemTier ToLegacyTier(this ContentTier value)
        {
            if (!Enum.IsDefined(typeof(ContentTier), value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Tier inválido.");

            return (ItemTier)(int)value;
        }

        public static ProfessionType ToProfessionType(this CraftingProfession value)
        {
            if (!value.IsCraftingProfession())
                throw new ArgumentOutOfRangeException(nameof(value), value, "Profissão inválida.");

            return (ProfessionType)(int)value;
        }

        public static CraftingProfession ToLegacyProfession(this ProfessionType value)
        {
            if (!Enum.IsDefined(typeof(ProfessionType), value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Profissão inválida.");

            return (CraftingProfession)(int)value;
        }
    }
}
