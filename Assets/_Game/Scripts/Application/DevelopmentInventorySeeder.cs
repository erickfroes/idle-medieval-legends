#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Inventory;

namespace IdleMedievalLegends.Application
{
    /// <summary>
    /// Autoridade simulada estritamente para Editor, Development Build e testes.
    /// Não existe em builds de produção.
    /// </summary>
    public static class DevelopmentInventorySeeder
    {
        public static bool SeedIfEmpty(
            PlayerInventory inventory,
            ContentCatalogLookup catalog,
            string playerId,
            long timestamp)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (inventory.Items.Count != 0) return false;
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("playerId é obrigatório.", nameof(playerId));

            AddEquipment(
                inventory, catalog, playerId, "item_iron_sword_t1", timestamp,
                new RolledStatData("attack", 25));
            AddEquipment(
                inventory, catalog, playerId, "item_leather_tunic_t1", timestamp + 1,
                new RolledStatData("defense", 20),
                new RolledStatData("health", 100));
            AddEquipment(
                inventory, catalog, playerId, "item_arcane_ring_t1", timestamp + 2,
                new RolledStatData("attack", 12, 0.02d));
            AddStack(
                inventory, catalog, playerId, "material_iron_ore_t1", 20,
                timestamp + 3, ItemBinding.Unbound);
            AddStack(
                inventory, catalog, playerId, "material_iron_ingot_t1", 18,
                timestamp + 4, ItemBinding.Unbound);
            AddStack(
                inventory, catalog, playerId, "material_raw_hide_t1", 20,
                timestamp + 5, ItemBinding.Unbound);
            AddStack(
                inventory, catalog, playerId, "material_treated_leather_t1", 12,
                timestamp + 6, ItemBinding.Unbound);
            AddStack(
                inventory, catalog, playerId, "material_arcane_essence_t1", 7,
                timestamp + 7, ItemBinding.Unbound);
            if (catalog.TryGetItem("consumable_minor_tonic_t1", out _))
            {
                AddStack(
                    inventory, catalog, playerId, "consumable_minor_tonic_t1", 5,
                    timestamp + 8, ItemBinding.AccountBound);
            }
            return true;
        }

        public static IReadOnlyList<DismantleYield> Dismantle(
            PlayerInventory inventory,
            ContentCatalogLookup catalog,
            string instanceId,
            long timestamp)
        {
            ItemInstance source = inventory.GetItem(instanceId);
            ItemDefinition definition = catalog.GetItem(source.DefinitionId);
            IReadOnlyList<DismantleYield> outputs =
                InventoryDismantleRules.Evaluate(source, definition);
            inventory.Dismantle(instanceId, definition, timestamp);
            for (int i = 0; i < outputs.Count; i++)
            {
                DismantleYield output = outputs[i];
                AddStack(
                    inventory,
                    catalog,
                    source.OwnerPlayerId,
                    output.DefinitionId,
                    output.Quantity,
                    timestamp + i + 1,
                    output.Bound ? ItemBinding.AccountBound : ItemBinding.Unbound,
                    $"dismantle:{source.InstanceId}");
            }
            return outputs;
        }

        private static void AddEquipment(
            PlayerInventory inventory,
            ContentCatalogLookup catalog,
            string playerId,
            string definitionId,
            long timestamp,
            params RolledStatData[] stats)
        {
            EquipmentDefinition definition = catalog.GetEquipment(definitionId);
            var item = Create(
                playerId,
                definition,
                InventoryItemKind.Equipment,
                1,
                false,
                ItemBinding.Unbound,
                timestamp,
                stats,
                "development_seed");
            inventory.AddAuthorizedItem(item, definition, timestamp);
        }

        private static void AddStack(
            PlayerInventory inventory,
            ContentCatalogLookup catalog,
            string playerId,
            string definitionId,
            long quantity,
            long timestamp,
            ItemBinding binding,
            string sourceId = "task_007")
        {
            ItemDefinition definition = catalog.GetItem(definitionId);
            var item = Create(
                playerId,
                definition,
                ToKind(definition.ItemType),
                quantity,
                true,
                binding,
                timestamp,
                Array.Empty<RolledStatData>(),
                sourceId);
            inventory.AddStack(item, definition, timestamp);
        }

        private static ItemInstance Create(
            string playerId,
            ItemDefinition definition,
            InventoryItemKind kind,
            long quantity,
            bool stackable,
            ItemBinding binding,
            long timestamp,
            IEnumerable<RolledStatData> stats,
            string sourceId)
        {
            string instanceId = $"dev_{Guid.NewGuid():N}";
            return new ItemInstance(
                instanceId,
                definition.DefinitionId,
                playerId,
                kind,
                definition.Rarity.ToLegacyRarity(),
                definition.Tier.ToLegacyTier(),
                quantity,
                stackable,
                InventoryItemState.Owned,
                binding,
                string.Empty,
                string.Empty,
                string.Empty,
                instanceId.GetHashCode(),
                stats,
                0,
                kind == InventoryItemKind.Equipment ? 100 : -1,
                kind == InventoryItemKind.Equipment ? 100 : -1,
                false,
                0,
                timestamp,
                timestamp,
                new ItemProvenanceData(
                    "development_authority",
                    sourceId,
                    $"devtx:{Guid.NewGuid():N}"));
        }

        private static InventoryItemKind ToKind(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Material: return InventoryItemKind.Material;
                case ItemType.Consumable: return InventoryItemKind.Consumable;
                default: throw new InvalidOperationException(
                    $"Seeder não suporta ItemType {itemType}.");
            }
        }
    }
}
#endif
