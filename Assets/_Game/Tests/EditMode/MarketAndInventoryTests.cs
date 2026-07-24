using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Domain.Market;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Infrastructure.Save;
using NUnit.Framework;
using UnityEngine;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class MarketAndInventoryTests
    {
        [TestCase(10, 1, 9)]
        [TestCase(99, 10, 89)]
        [TestCase(100, 10, 90)]
        [TestCase(1001, 101, 900)]
        public void MarketFee_UsesIntegerCeiling(
            long price,
            long expectedFee,
            long expectedNet)
        {
            MarketSettlement settlement = MarketMath.CalculateSettlement(price);

            Assert.That(settlement.FeeBurned, Is.EqualTo(expectedFee));
            Assert.That(settlement.SellerNet, Is.EqualTo(expectedNet));
        }

        [Test]
        public void Inventory_RejectsDuplicateInstanceIds()
        {
            var first = new ItemInstance(
                "same-id",
                "iron-sword",
                "player-1",
                InventoryItemKind.Equipment,
                1,
                false,
                123,
                1);

            var second = new ItemInstance(
                "same-id",
                "iron-shield",
                "player-1",
                InventoryItemKind.Equipment,
                1,
                false,
                456,
                1);

            var snapshot = new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                "player-1",
                1,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                new List<ItemInstance> { first, second });

            var inventory = new PlayerInventory();

            Assert.Throws<InvalidOperationException>(
                () => inventory.ApplyServerSnapshot(snapshot));
        }

        [Test]
        public void LegacyCache_IsUpgradedToSchemaTwo()
        {
            const string legacyJson =
                "{\"schemaVersion\":1," +
                "\"playerId\":\"player-1\"," +
                "\"inventory\":{" +
                "\"schemaVersion\":1," +
                "\"playerId\":\"player-1\"," +
                "\"serverRevision\":1," +
                "\"items\":[{" +
                "\"instanceId\":\"legacy-item\"," +
                "\"definitionId\":\"legacy-sword\"," +
                "\"ownerPlayerId\":\"player-1\"," +
                "\"kind\":1," +
                "\"quantity\":1," +
                "\"stackable\":false," +
                "\"state\":0," +
                "\"binding\":0}]}}";

            GameSaveData legacy = JsonUtility.FromJson<GameSaveData>(legacyJson);
            GameSaveData upgraded = GameSaveMigration.UpgradeToCurrent(legacy);

            Assert.That(upgraded.SchemaVersion, Is.EqualTo(GameSaveData.CurrentSchemaVersion));
            Assert.That(upgraded.Inventory.Items.Count, Is.EqualTo(1));
            Assert.That(upgraded.Inventory.Items[0].Tier, Is.EqualTo(ItemTier.Tier1));
            Assert.That(upgraded.Inventory.Items[0].Rarity, Is.EqualTo(GameRarity.Common));
            Assert.That(upgraded.Professions.Professions.Count, Is.EqualTo(5));
        }
    }
}
