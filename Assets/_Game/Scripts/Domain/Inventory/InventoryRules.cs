using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;

namespace IdleMedievalLegends.Domain.Inventory
{
    public sealed class HeroEquipmentContext
    {
        public HeroEquipmentContext(
            string heroInstanceId,
            string ownerPlayerId,
            int level,
            IEnumerable<string> tags)
        {
            HeroInstanceId = heroInstanceId ?? throw new ArgumentNullException(nameof(heroInstanceId));
            OwnerPlayerId = ownerPlayerId ?? throw new ArgumentNullException(nameof(ownerPlayerId));
            Level = level;
            Tags = new ReadOnlyCollection<string>(
                tags == null ? new List<string>() : new List<string>(tags));
        }

        public string HeroInstanceId { get; }
        public string OwnerPlayerId { get; }
        public int Level { get; }
        public IReadOnlyList<string> Tags { get; }

        public bool HasAnyTag(IReadOnlyList<string> requiredTags)
        {
            if (requiredTags == null || requiredTags.Count == 0) return true;
            for (int i = 0; i < requiredTags.Count; i++)
            {
                for (int j = 0; j < Tags.Count; j++)
                {
                    if (string.Equals(requiredTags[i], Tags[j], StringComparison.Ordinal))
                        return true;
                }
            }
            return false;
        }
    }

    public static class EquipmentRules
    {
        public static void ValidateEquip(
            ItemInstance item,
            EquipmentDefinition definition,
            HeroEquipmentContext hero)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (item.State != InventoryItemState.Owned)
                throw new InvalidOperationException(
                    $"Item {item.InstanceId} deve estar Owned para equipar.");
            if (item.Kind != InventoryItemKind.Equipment || item.Quantity != 1)
                throw new InvalidOperationException("Somente equipamento único pode ser equipado.");
            if (!string.Equals(item.DefinitionId, definition.DefinitionId, StringComparison.Ordinal))
                throw new InvalidOperationException("Instância e definição de equipamento divergem.");
            if (!string.Equals(item.OwnerPlayerId, hero.OwnerPlayerId, StringComparison.Ordinal))
                throw new InvalidOperationException("Herói e item pertencem a jogadores diferentes.");
            if (hero.Level < definition.RequiredLevel)
                throw new InvalidOperationException("Herói não atende ao nível requerido.");
            if (!hero.HasAnyTag(definition.RequiredHeroTags))
                throw new InvalidOperationException("Herói não atende às restrições do equipamento.");
            if (item.Binding == ItemBinding.HeroBound &&
                !string.Equals(
                    item.BoundHeroInstanceId,
                    hero.HeroInstanceId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Item está vinculado a outro herói.");
            }
        }
    }

    public sealed class DismantleYield
    {
        public DismantleYield(string definitionId, long quantity, bool bound)
        {
            DefinitionId = definitionId;
            Quantity = quantity;
            Bound = bound;
        }

        public string DefinitionId { get; }
        public long Quantity { get; }
        public bool Bound { get; }
    }

    public static class InventoryDismantleRules
    {
        public static IReadOnlyList<DismantleYield> Evaluate(
            ItemInstance item,
            ItemDefinition definition)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!string.Equals(item.DefinitionId, definition.DefinitionId, StringComparison.Ordinal))
                throw new InvalidOperationException("Instância e definição divergem.");
            if (!definition.Destroyable || definition.DismantleYields.Count == 0)
                throw new InvalidOperationException("Item não é desmontável.");
            if (item.LockedByPlayer)
                throw new InvalidOperationException("Item bloqueado não pode ser desmontado.");
            if (item.State == InventoryItemState.Equipped)
                throw new InvalidOperationException("Item equipado não pode ser desmontado.");
            if (item.State == InventoryItemState.ReservedByServer)
                throw new InvalidOperationException("Item reservado não pode ser desmontado.");
            if (item.State == InventoryItemState.Escrow)
                throw new InvalidOperationException("Item em escrow não pode ser desmontado.");
            if (item.State == InventoryItemState.Destroyed)
                throw new InvalidOperationException("Item já foi destruído.");
            if (item.State == InventoryItemState.Consumed)
                throw new InvalidOperationException("Item já foi consumido.");
            if (item.State != InventoryItemState.Owned)
                throw new InvalidOperationException("Estado do item não permite desmontagem.");

            bool bound = item.Binding != ItemBinding.Unbound;
            var outputs = new List<DismantleYield>(definition.DismantleYields.Count);
            for (int i = 0; i < definition.DismantleYields.Count; i++)
            {
                DismantleYieldDefinition output = definition.DismantleYields[i];
                outputs.Add(new DismantleYield(
                    output.MaterialDefinitionId,
                    checked(output.Quantity * item.Quantity),
                    bound));
            }
            return new ReadOnlyCollection<DismantleYield>(outputs);
        }
    }

    public enum InventoryCategoryFilter
    {
        All = 0,
        Equipment = 1,
        Materials = 2,
        Consumables = 3
    }

    public enum InventorySortMode
    {
        TierDescending = 0,
        RarityDescending = 1,
        PowerOrStatBudgetDescending = 2,
        NameAscending = 3,
        QuantityDescending = 4,
        AcquisitionDateDescending = 5
    }

    public sealed class InventoryFilter
    {
        public InventoryCategoryFilter Category { get; set; } = InventoryCategoryFilter.All;
        public ContentTier? Tier { get; set; }
        public Rarity? Rarity { get; set; }
        public bool EquippedOnly { get; set; }
        public bool LockedOnly { get; set; }
        public bool TradableOnly { get; set; }
    }

    public sealed class InventoryViewEntry
    {
        public InventoryViewEntry(ItemInstance item, ItemDefinition definition)
        {
            Item = item;
            Definition = definition;
        }

        public ItemInstance Item { get; }
        public ItemDefinition Definition { get; }
        public long PowerOrStatBudget => Definition is EquipmentDefinition equipment
            ? equipment.StatBudget
            : 0;
    }

    public static class InventoryQuery
    {
        public static IReadOnlyList<InventoryViewEntry> Execute(
            IEnumerable<ItemInstance> items,
            ContentCatalogLookup catalog,
            InventoryFilter filter,
            InventorySortMode sortMode)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            filter ??= new InventoryFilter();
            var result = new List<InventoryViewEntry>();
            foreach (ItemInstance item in items)
            {
                if (item == null || item.IsTerminal) continue;
                if (!catalog.TryGetItem(item.DefinitionId, out ItemDefinition definition)) continue;
                if (!Matches(item, definition, filter)) continue;
                result.Add(new InventoryViewEntry(item, definition));
            }
            result.Sort((left, right) => Compare(left, right, sortMode));
            return new ReadOnlyCollection<InventoryViewEntry>(result);
        }

        private static bool Matches(
            ItemInstance item,
            ItemDefinition definition,
            InventoryFilter filter)
        {
            if (filter.Category == InventoryCategoryFilter.Equipment &&
                definition.ItemType != ItemType.Equipment) return false;
            if (filter.Category == InventoryCategoryFilter.Materials &&
                definition.ItemType != ItemType.Material) return false;
            if (filter.Category == InventoryCategoryFilter.Consumables &&
                definition.ItemType != ItemType.Consumable) return false;
            if (filter.Tier.HasValue && definition.Tier != filter.Tier.Value) return false;
            if (filter.Rarity.HasValue &&
                item.Rarity.ToCatalogRarity() != filter.Rarity.Value) return false;
            if (filter.EquippedOnly && item.State != InventoryItemState.Equipped) return false;
            if (filter.LockedOnly && !item.LockedByPlayer) return false;
            if (filter.TradableOnly && (!definition.Tradable || !item.IsTradable)) return false;
            return true;
        }

        private static int Compare(
            InventoryViewEntry left,
            InventoryViewEntry right,
            InventorySortMode sortMode)
        {
            int result;
            switch (sortMode)
            {
                case InventorySortMode.TierDescending:
                    result = ((int)right.Definition.Tier).CompareTo((int)left.Definition.Tier);
                    break;
                case InventorySortMode.RarityDescending:
                    result = ((int)right.Item.Rarity).CompareTo((int)left.Item.Rarity);
                    break;
                case InventorySortMode.PowerOrStatBudgetDescending:
                    result = right.PowerOrStatBudget.CompareTo(left.PowerOrStatBudget);
                    break;
                case InventorySortMode.NameAscending:
                    result = string.Compare(
                        left.Definition.DisplayName,
                        right.Definition.DisplayName,
                        StringComparison.OrdinalIgnoreCase);
                    break;
                case InventorySortMode.QuantityDescending:
                    result = right.Item.Quantity.CompareTo(left.Item.Quantity);
                    break;
                case InventorySortMode.AcquisitionDateDescending:
                    result = right.Item.CreatedAtUnixMilliseconds.CompareTo(
                        left.Item.CreatedAtUnixMilliseconds);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sortMode));
            }
            return result != 0
                ? result
                : string.Compare(left.Item.InstanceId, right.Item.InstanceId,
                    StringComparison.Ordinal);
        }
    }

    public sealed class InventoryEquipmentModifierProvider : IHeroEquipmentModifierProvider
    {
        private readonly PlayerInventory inventory;

        public InventoryEquipmentModifierProvider(PlayerInventory inventory)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public HeroStatModifiers GetModifiers(HeroInstance hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            return GetModifiers(hero.InstanceId);
        }

        public HeroStatModifiers GetModifiers(string heroInstanceId)
        {
            if (string.IsNullOrWhiteSpace(heroInstanceId))
                throw new ArgumentException("heroInstanceId é obrigatório.", nameof(heroInstanceId));
            HeroStatModifiers total = HeroStatModifiers.None;
            IReadOnlyList<ItemInstance> items = inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemInstance item = items[i];
                if (item.State != InventoryItemState.Equipped ||
                    !string.Equals(item.EquippedHeroInstanceId, heroInstanceId,
                        StringComparison.Ordinal)) continue;
                total = HeroStatModifiers.Combine(total, ToModifiers(item.RolledStats));
            }
            return total;
        }

        private static HeroStatModifiers ToModifiers(IReadOnlyList<RolledStatData> stats)
        {
            long health = 0;
            long attack = 0;
            long defense = 0;
            double speed = 0;
            double healthPercent = 0;
            double attackPercent = 0;
            double defensePercent = 0;
            double speedPercent = 0;
            for (int i = 0; i < stats.Count; i++)
            {
                RolledStatData stat = stats[i];
                switch (stat.StatId)
                {
                    case "health":
                        health = checked(health + stat.FlatValue);
                        healthPercent += stat.PercentValue;
                        break;
                    case "attack":
                        attack = checked(attack + stat.FlatValue);
                        attackPercent += stat.PercentValue;
                        break;
                    case "defense":
                        defense = checked(defense + stat.FlatValue);
                        defensePercent += stat.PercentValue;
                        break;
                    case "speed":
                        speed += stat.FlatValue;
                        speedPercent += stat.PercentValue;
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Stat de equipamento não suportado: {stat.StatId}.");
                }
            }
            var result = new HeroStatModifiers(
                health, healthPercent, attack, attackPercent, defense,
                defensePercent, speed, speedPercent);
            result.Validate();
            return result;
        }
    }
}
