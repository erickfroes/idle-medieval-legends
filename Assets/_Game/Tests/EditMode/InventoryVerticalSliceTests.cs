using System;
using System.Collections.Generic;
using System.Reflection;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Editor.ContentCatalog;
using NUnit.Framework;
using UnityEngine;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class InventoryVerticalSliceTests
    {
        private const string PlayerId = "player-task007";
        private ContentCatalogLookup catalog;
        private PlayerInventory inventory;
        private long time;

        [SetUp]
        public void SetUp()
        {
            catalog = new ContentCatalogLookup(ContentCatalogDemoFactory.Create());
            inventory = new PlayerInventory();
            inventory.ApplySnapshot(new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                PlayerId,
                10,
                1000,
                new List<ItemInstance>()), catalog);
            time = 1000;
        }

        [Test]
        public void Inventory_AddAuthorizedItem_AddsOwnedInstance()
        {
            Add("sword", "item_iron_sword_t1");

            Assert.That(inventory.Items.Count, Is.EqualTo(1));
            Assert.That(inventory.GetItem("sword").State, Is.EqualTo(InventoryItemState.Owned));
        }

        [Test]
        public void LocalMutations_DoNotAdvanceServerRevision()
        {
            Add("sword", "item_iron_sword_t1");
            long serverRevision = inventory.ServerRevision;
            long localRevision = inventory.Revision;
            long itemServerVersion = inventory.GetItem("sword").ServerVersion;

            inventory.Lock("sword", Next());
            inventory.Unlock("sword", Next());

            Assert.That(serverRevision, Is.EqualTo(10));
            Assert.That(inventory.ServerRevision, Is.EqualTo(serverRevision));
            Assert.That(inventory.Revision, Is.EqualTo(localRevision + 2));
            Assert.That(inventory.GetItem("sword").ServerVersion,
                Is.EqualTo(itemServerVersion));
            Assert.That(inventory.GetItem("sword").LocalVersion, Is.EqualTo(2));
        }

        [Test]
        public void RemoveQuantity_StaleTimestamp_DoesNotPartiallyMutateItem()
        {
            Add("ore", "material_iron_ingot_t1", 5);
            ItemInstance item = inventory.GetItem("ore");
            long revision = inventory.Revision;
            long version = item.ServerVersion;
            long localVersion = item.LocalVersion;

            Assert.Throws<InvalidOperationException>(
                () => inventory.RemoveQuantity("ore", 2, 999));

            Assert.That(item.Quantity, Is.EqualTo(5));
            Assert.That(item.ServerVersion, Is.EqualTo(version));
            Assert.That(item.LocalVersion, Is.EqualTo(localVersion));
            Assert.That(inventory.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void RemoveQuantity_TimestampOlderThanAggregate_DoesNotMutateOlderItem()
        {
            ItemDefinition definition = catalog.GetItem("material_iron_ingot_t1");
            ItemInstance item = Create("aggregate-stale", definition.DefinitionId, 5);
            var snapshot = new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                PlayerId,
                22,
                7,
                2000,
                new List<ItemInstance> { item });
            inventory.ApplyServerSnapshot(snapshot, catalog);

            Assert.Throws<InvalidOperationException>(() =>
                inventory.RemoveQuantity(item.InstanceId, 2, 1500));

            Assert.That(inventory.GetItem(item.InstanceId).Quantity, Is.EqualTo(5));
            Assert.That(inventory.GetItem(item.InstanceId).State,
                Is.EqualTo(InventoryItemState.Owned));
            Assert.That(inventory.Revision, Is.EqualTo(7));
            Assert.That(inventory.ServerRevision, Is.EqualTo(22));
        }

        [Test]
        public void AddAuthorizedItem_AccountBoundOnAcquireRejectsUnboundInstance()
        {
            var definition = new EquipmentDefinition(
                "account_bound_equipment", "Equipamento vinculado", "Teste",
                ContentTier.Tier1, Rarity.Common, false, false, true,
                EquipmentSlot.Head, 100, Array.Empty<string>(), 1,
                Array.Empty<string>(), ProfessionType.Blacksmith, 3,
                BindingRule.AccountBoundOnAcquire);
            var unbound = new ItemInstance(
                "bound-item", definition.DefinitionId, PlayerId,
                InventoryItemKind.Equipment,
                definition.Rarity.ToLegacyRarity(), definition.Tier.ToLegacyTier(),
                1, false, InventoryItemState.Owned, ItemBinding.Unbound,
                string.Empty, string.Empty, string.Empty, 0,
                Array.Empty<RolledStatData>(), 0, 100, 100, false, 0,
                1000, 1000,
                new ItemProvenanceData("test", "binding", "tx:binding"));

            Assert.Throws<InvalidOperationException>(
                () => inventory.AddAuthorizedItem(unbound, definition, Next()));
            Assert.That(inventory.Items, Is.Empty);
        }

        [Test]
        public void Inventory_DuplicateInstanceId_IsRejected()
        {
            Add("same", "item_iron_sword_t1");

            Assert.Throws<InvalidOperationException>(
                () => Add("same", "item_arcane_ring_t1"));
        }

        [Test]
        public void CombineStacks_SameDefinition_ConservesQuantity()
        {
            Add("a", "material_iron_ingot_t1", 4);
            Add("b", "material_iron_ingot_t1", 6);

            inventory.CombineStacks("a", "b", catalog.GetItem("material_iron_ingot_t1"), Next());

            Assert.That(inventory.GetItem("a").Quantity, Is.EqualTo(10));
            Assert.That(inventory.GetItem("b").State, Is.EqualTo(InventoryItemState.Consumed));
        }

        [Test]
        public void CombineStacks_DifferentDefinitions_IsRejected()
        {
            Add("a", "material_iron_ingot_t1", 4);
            Add("b", "material_arcane_essence_t1", 6);

            Assert.Throws<InvalidOperationException>(() => inventory.CombineStacks(
                "a", "b", catalog.GetItem("material_iron_ingot_t1"), Next()));
        }

        [Test]
        public void SplitStack_ValidQuantity_CreatesUniqueChild()
        {
            Add("source", "material_iron_ingot_t1", 10);

            ItemInstance child = inventory.SplitStack(
                "source", "child", 3, catalog.GetItem("material_iron_ingot_t1"), Next());

            Assert.That(inventory.GetItem("source").Quantity, Is.EqualTo(7));
            Assert.That(child.Quantity, Is.EqualTo(3));
            Assert.That(child.Provenance.ParentInstanceId, Is.EqualTo("source"));
        }

        [Test]
        public void Equip_EligibleHero_EquipsAndBindsItem()
        {
            Add("sword", "item_iron_sword_t1");

            inventory.Equip("sword", Paladin(), catalog.GetEquipment("item_iron_sword_t1"), Next());

            ItemInstance item = inventory.GetItem("sword");
            Assert.That(item.State, Is.EqualTo(InventoryItemState.Equipped));
            Assert.That(item.EquippedHeroInstanceId, Is.EqualTo("hero-instance"));
            Assert.That(item.Binding, Is.EqualTo(ItemBinding.AccountBound));
        }

        [Test]
        public void Equip_DuplicateSlot_IsRejected()
        {
            Add("sword-a", "item_iron_sword_t1");
            Add("sword-b", "item_iron_sword_t1");
            EquipmentDefinition definition = catalog.GetEquipment("item_iron_sword_t1");
            inventory.Equip("sword-a", Paladin(), definition, Next());

            Assert.Throws<InvalidOperationException>(
                () => inventory.Equip("sword-b", Paladin(), definition, Next()));
        }

        [Test]
        public void Unequip_EquippedItem_ReturnsOwned()
        {
            Add("sword", "item_iron_sword_t1");
            inventory.Equip("sword", Paladin(), catalog.GetEquipment("item_iron_sword_t1"), Next());

            inventory.Unequip("sword", Next());

            Assert.That(inventory.GetItem("sword").State, Is.EqualTo(InventoryItemState.Owned));
            Assert.That(inventory.GetItem("sword").EquippedHeroInstanceId, Is.Empty);
        }

        [Test]
        public void Equip_ReservedItem_IsRejected()
        {
            Add("sword", "item_iron_sword_t1");
            inventory.Reserve("sword", "reservation", Next());

            Assert.Throws<InvalidOperationException>(() => inventory.Equip(
                "sword", Paladin(), catalog.GetEquipment("item_iron_sword_t1"), Next()));
        }

        [Test]
        public void Dismantle_LockedItem_IsRejected()
        {
            Add("sword", "item_iron_sword_t1");
            inventory.Lock("sword", Next());

            Assert.Throws<InvalidOperationException>(() => inventory.Dismantle(
                "sword", catalog.GetEquipment("item_iron_sword_t1"), Next()));
        }

        [Test]
        public void Dismantle_EquippedItem_IsRejected()
        {
            Add("sword", "item_iron_sword_t1");
            EquipmentDefinition definition = catalog.GetEquipment("item_iron_sword_t1");
            inventory.Equip("sword", Paladin(), definition, Next());

            Assert.Throws<InvalidOperationException>(
                () => inventory.Dismantle("sword", definition, Next()));
        }

        [Test]
        public void Reservation_ReserveAndRelease_ReturnsOwned()
        {
            Add("ore", "material_iron_ore_t1", 3);

            inventory.Reserve("ore", "job-1", Next());
            inventory.ReleaseReservation("ore", "job-1", Next());

            Assert.That(inventory.GetItem("ore").State, Is.EqualTo(InventoryItemState.Owned));
        }

        [Test]
        public void ReservedItem_SellDismantleAndConsume_AreRejected()
        {
            Add("sword", "item_iron_sword_t1");
            EquipmentDefinition definition = catalog.GetEquipment("item_iron_sword_t1");
            inventory.Reserve("sword", "job-1", Next());

            Assert.Throws<InvalidOperationException>(
                () => inventory.MarkEscrow("sword", "listing", definition, Next()));
            Assert.Throws<InvalidOperationException>(
                () => inventory.Dismantle("sword", definition, Next()));
            Assert.Throws<InvalidOperationException>(
                () => inventory.Consume("sword", 1, Next()));
        }

        [Test]
        public void Escrow_MarkAndCancel_ReturnsOwned()
        {
            Add("sword", "item_iron_sword_t1");
            EquipmentDefinition definition = catalog.GetEquipment("item_iron_sword_t1");

            inventory.MarkEscrow("sword", "listing-1", definition, Next());
            Assert.That(inventory.GetItem("sword").State, Is.EqualTo(InventoryItemState.Escrow));
            inventory.CancelEscrow("sword", "listing-1", Next());

            Assert.That(inventory.GetItem("sword").State, Is.EqualTo(InventoryItemState.Owned));
        }

        [Test]
        public void AuthorizedTransfer_UnboundOwnedItem_ChangesOwnerAndProvenance()
        {
            Add("ore", "material_iron_ore_t1", 3);

            ItemInstance transferred = inventory.TransferOwnershipAuthorized(
                "ore", "player-two", catalog.GetItem("material_iron_ore_t1"),
                "transfer-tx", Next());

            Assert.That(transferred.OwnerPlayerId, Is.EqualTo("player-two"));
            Assert.That(transferred.Provenance.TransactionId, Is.EqualTo("transfer-tx"));
            Assert.That(inventory.TryGetItem("ore", out _), Is.False);
        }

        [Test]
        public void Consume_AllQuantity_TransitionsToConsumed()
        {
            Add("tonic", "consumable_minor_tonic_t1", 2, ItemBinding.AccountBound);

            inventory.Consume("tonic", 2, Next());

            Assert.That(inventory.GetItem("tonic").State, Is.EqualTo(InventoryItemState.Consumed));
            Assert.That(inventory.GetItem("tonic").Quantity, Is.Zero);
        }

        [Test]
        public void Dismantle_ValidItem_TransitionsToDestroyed()
        {
            Add("sword", "item_iron_sword_t1");

            IReadOnlyList<DismantleYield> outputs = inventory.Dismantle(
                "sword", catalog.GetEquipment("item_iron_sword_t1"), Next());

            Assert.That(inventory.GetItem("sword").State, Is.EqualTo(InventoryItemState.Destroyed));
            Assert.That(outputs[0].DefinitionId, Is.EqualTo("material_iron_ingot_t1"));
        }

        [Test]
        public void DestroyedItem_CannotReturnToOwned()
        {
            Add("sword", "item_iron_sword_t1");
            inventory.Dismantle("sword", catalog.GetEquipment("item_iron_sword_t1"), Next());

            Assert.Throws<InvalidOperationException>(
                () => inventory.Reserve("sword", "job", Next()));
        }

        [Test]
        public void InventoryQuery_CategoryAndFlags_FilterItems()
        {
            Add("sword", "item_iron_sword_t1");
            Add("ore", "material_iron_ore_t1", 3);
            inventory.Lock("sword", Next());
            var filter = new InventoryFilter
            {
                Category = InventoryCategoryFilter.Equipment,
                LockedOnly = true
            };

            IReadOnlyList<InventoryViewEntry> result = InventoryQuery.Execute(
                inventory.Items, catalog, filter, InventorySortMode.NameAscending);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Item.InstanceId, Is.EqualTo("sword"));
        }

        [Test]
        public void InventoryQuery_TierRarityAndQuantity_SortsDeterministically()
        {
            Add("ore", "material_iron_ore_t1", 2);
            Add("essence", "material_arcane_essence_t1", 8);

            IReadOnlyList<InventoryViewEntry> result = InventoryQuery.Execute(
                inventory.Items, catalog, new InventoryFilter(),
                InventorySortMode.QuantityDescending);

            Assert.That(result[0].Item.InstanceId, Is.EqualTo("essence"));
            Assert.That(result[1].Item.InstanceId, Is.EqualTo("ore"));
        }

        [Test]
        public void InventoryQuery_CraftedRarity_FiltersAndSortsByInstanceRarity()
        {
            Add("common-sword", "item_iron_sword_t1");
            ItemDefinition definition = catalog.GetItem("item_iron_sword_t1");
            long timestamp = Next();
            ItemInstance epic = ItemInstance.CreateCrafted(
                "epic-sword", definition.DefinitionId, PlayerId,
                InventoryItemKind.Equipment, GameRarity.Epic, ItemTier.Tier1,
                1, false, 8811, "seed", timestamp,
                CraftingProfession.Blacksmith, "recipe_iron_sword_t1", "job-epic", 7500);
            inventory.AddAuthorizedItem(epic, definition, timestamp);

            IReadOnlyList<InventoryViewEntry> filtered = InventoryQuery.Execute(
                inventory.Items, catalog,
                new InventoryFilter { Rarity = Rarity.Epic },
                InventorySortMode.NameAscending);
            IReadOnlyList<InventoryViewEntry> sorted = InventoryQuery.Execute(
                inventory.Items, catalog, new InventoryFilter(),
                InventorySortMode.RarityDescending);

            Assert.That(filtered.Count, Is.EqualTo(1));
            Assert.That(filtered[0].Item.InstanceId, Is.EqualTo("epic-sword"));
            Assert.That(sorted[0].Item.InstanceId, Is.EqualTo("epic-sword"));
        }

        [Test]
        public void EquipmentModifierProvider_EquippedItems_AggregatesStats()
        {
            Add("sword", "item_iron_sword_t1", stats: new[]
            {
                new RolledStatData("attack", 25, 0.05d)
            });
            inventory.Equip("sword", Paladin(), catalog.GetEquipment("item_iron_sword_t1"), Next());

            HeroStatModifiers modifiers =
                new InventoryEquipmentModifierProvider(inventory).GetModifiers("hero-instance");

            Assert.That(modifiers.FlatAttack, Is.EqualTo(25));
            Assert.That(modifiers.PercentAttack, Is.EqualTo(0.05d));
        }

        [Test]
        public void HeroPowerCalculator_EquippedItem_IncreasesPower()
        {
            Add("sword", "item_iron_sword_t1", stats: new[]
            {
                new RolledStatData("attack", 50)
            });
            inventory.Equip("sword", Paladin(), catalog.GetEquipment("item_iron_sword_t1"), Next());
            var tuning = new CombatBalanceTuning();
            HeroInstance hero = HeroInstance.Restore(
                "hero-instance", "hero_paladin_001", PlayerId, 1, 0, Rarity.Common,
                0, 0, new[] { "sword" }, true, 1000, 0, null, 0, tuning);
            HeroDefinition definition = catalog.GetHero("hero_paladin_001");

            long basePower = HeroPowerCalculator.CalculateBreakdown(
                hero, definition, HeroStatModifiers.None, HeroStatModifiers.None, tuning)
                .HeroPower.Value;
            long equippedPower = HeroPowerCalculator.CalculateBreakdown(
                hero, definition, new InventoryEquipmentModifierProvider(inventory), tuning)
                .HeroPower.Value;

            Assert.That(equippedPower, Is.GreaterThan(basePower));
        }

        [Test]
        public void InventorySnapshot_JsonRoundTrip_PreservesSchemaRevisionAndItems()
        {
            Add("ore", "material_iron_ore_t1", 4);
            InventorySnapshotData source = inventory.CaptureSnapshotForCache();

            string json = JsonUtility.ToJson(source);
            InventorySnapshotData restored = JsonUtility.FromJson<InventorySnapshotData>(json);
            var reloaded = new PlayerInventory();
            reloaded.ApplySnapshot(restored, catalog);

            Assert.That(reloaded.Revision, Is.EqualTo(source.Revision));
            Assert.That(reloaded.GetItem("ore").Quantity, Is.EqualTo(4));
        }

        [Test]
        public void InventorySnapshot_UnknownDefinition_IsRejected()
        {
            ItemInstance unknown = Create("unknown", "missing_definition", 1);
            var invalid = new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                PlayerId,
                11,
                1000,
                new List<ItemInstance> { unknown });

            Assert.Throws<InvalidOperationException>(
                () => inventory.ApplySnapshot(invalid, catalog));
        }

        [Test]
        public void InventorySnapshot_InvalidState_IsRejected()
        {
            ItemInstance valid = Create("ore", "material_iron_ore_t1", 1);
            string json = JsonUtility.ToJson(valid).Replace("\"state\":0", "\"state\":99");
            ItemInstance invalid = JsonUtility.FromJson<ItemInstance>(json);
            var snapshot = new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion, PlayerId, 11, 1000,
                new List<ItemInstance> { invalid });

            Assert.Throws<InvalidOperationException>(
                () => inventory.ApplySnapshot(snapshot, catalog));
        }

        [Test]
        public void InventorySnapshot_CurrentSchemaWithoutProvenance_IsRejected()
        {
            ItemInstance item = Create("missing-provenance", "material_iron_ore_t1", 1);
            FieldInfo provenanceField = typeof(ItemInstance).GetField(
                "provenance", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(provenanceField, Is.Not.Null);
            provenanceField.SetValue(item, null);
            var snapshot = new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                PlayerId,
                11,
                1000,
                new List<ItemInstance> { item });

            Assert.Throws<InvalidOperationException>(
                () => inventory.ApplySnapshot(snapshot, catalog));
        }

        [Test]
        public void InventorySnapshot_RefinedMaterialKind_IsPreserved()
        {
            ItemDefinition definition = catalog.GetItem("material_iron_ingot_t1");
            var refined = new ItemInstance(
                "legacy-refined", definition.DefinitionId, PlayerId,
                InventoryItemKind.RefinedMaterial, GameRarity.Common, ItemTier.Tier1,
                4, true, InventoryItemState.Owned, ItemBinding.Unbound,
                string.Empty, string.Empty, string.Empty, 0,
                Array.Empty<RolledStatData>(), 0, -1, -1, false, 3,
                1000, 1000,
                new ItemProvenanceData("legacy_cache", definition.DefinitionId,
                    "cache:legacy-refined"));
            var snapshot = new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                PlayerId,
                12,
                2000,
                new List<ItemInstance> { refined });

            Assert.DoesNotThrow(() => inventory.ApplySnapshot(snapshot, catalog));
            Assert.That(inventory.GetItem(refined.InstanceId).Kind,
                Is.EqualTo(InventoryItemKind.RefinedMaterial));
        }

        private ItemInstance Add(
            string instanceId,
            string definitionId,
            long quantity = 1,
            ItemBinding binding = ItemBinding.Unbound,
            IEnumerable<RolledStatData> stats = null)
        {
            ItemDefinition definition = catalog.GetItem(definitionId);
            ItemInstance item = Create(instanceId, definitionId, quantity, binding, stats);
            inventory.AddAuthorizedItem(item, definition, Next());
            return item;
        }

        private ItemInstance Create(
            string instanceId,
            string definitionId,
            long quantity,
            ItemBinding binding = ItemBinding.Unbound,
            IEnumerable<RolledStatData> stats = null)
        {
            bool found = catalog.TryGetItem(definitionId, out ItemDefinition definition);
            ItemType type = found ? definition.ItemType : ItemType.Material;
            bool stackable = found ? definition.Stackable : true;
            return new ItemInstance(
                instanceId, definitionId, PlayerId, ToKind(type),
                found ? definition.Rarity.ToLegacyRarity() :
                    IdleMedievalLegends.Domain.Common.GameRarity.Common,
                found ? definition.Tier.ToLegacyTier() :
                    IdleMedievalLegends.Domain.Common.ItemTier.Tier1,
                quantity, stackable, InventoryItemState.Owned, binding,
                string.Empty, string.Empty, string.Empty, 7007, stats, 0,
                type == ItemType.Equipment ? 100 : -1,
                type == ItemType.Equipment ? 100 : -1,
                false, 0, 1000, 1000,
                new ItemProvenanceData("test", "task_007", $"tx:{instanceId}"));
        }

        private HeroEquipmentContext Paladin()
        {
            return new HeroEquipmentContext(
                "hero-instance", PlayerId, 100, catalog.GetHero("hero_paladin_001").Tags);
        }

        private long Next()
        {
            return ++time;
        }

        private static InventoryItemKind ToKind(ItemType type)
        {
            switch (type)
            {
                case ItemType.Equipment: return InventoryItemKind.Equipment;
                case ItemType.Material: return InventoryItemKind.Material;
                case ItemType.Consumable: return InventoryItemKind.Consumable;
                default: throw new InvalidOperationException();
            }
        }
    }
}
