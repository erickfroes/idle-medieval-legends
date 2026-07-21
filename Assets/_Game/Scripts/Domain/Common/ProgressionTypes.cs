using System;

namespace IdleMedievalLegends.Domain.Common
{
    /// <summary>
    /// Raridade compartilhada por heróis, equipamentos, diagramas e catalisadores.
    /// Os IDs são persistidos; não reordene sem uma migração de schema.
    /// </summary>
    public enum GameRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5
    }

    /// <summary>
    /// Tier representa a geração tecnológica/material do conteúdo. Não confundir
    /// com raridade: um item T6 Comum e um T5 Lendário são eixos diferentes.
    /// </summary>
    public enum ItemTier
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

    public enum CraftingProfession
    {
        None = 0,
        Blacksmith = 1,
        Tailor = 2,
        Enchanter = 3,
        Alchemist = 4,
        Gatherer = 5
    }

    public enum ProfessionRank
    {
        Apprentice = 0,
        Proficient = 1,
        Master = 2,
        Grandmaster = 3,
        God = 4
    }

    public static class ProgressionTypes
    {
        public const int MinimumTier = 1;
        public const int MaximumTier = 9;

        public static int ToNumber(this ItemTier tier)
        {
            return (int)tier;
        }

        public static bool IsValid(this ItemTier tier)
        {
            int value = (int)tier;
            return value >= MinimumTier && value <= MaximumTier;
        }

        public static bool IsValid(this GameRarity rarity)
        {
            int value = (int)rarity;
            return value >= (int)GameRarity.Common && value <= (int)GameRarity.Mythic;
        }

        public static bool IsCraftingProfession(this CraftingProfession profession)
        {
            int value = (int)profession;
            return value >= (int)CraftingProfession.Blacksmith &&
                   value <= (int)CraftingProfession.Gatherer;
        }

        public static GameRarity FromLegacyV1HeroRarity(int legacyValue)
        {
            // v1: Common=0, Rare=1, Epic=2, Legendary=3.
            switch (legacyValue)
            {
                case 0: return GameRarity.Common;
                case 1: return GameRarity.Rare;
                case 2: return GameRarity.Epic;
                case 3: return GameRarity.Legendary;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(legacyValue),
                        "Raridade legada v1 desconhecida.");
            }
        }

        public static ItemTier FromTierNumber(int tier)
        {
            if (tier < MinimumTier || tier > MaximumTier)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tier),
                    $"Tier deve estar entre {MinimumTier} e {MaximumTier}.");
            }

            return (ItemTier)tier;
        }
    }
}
