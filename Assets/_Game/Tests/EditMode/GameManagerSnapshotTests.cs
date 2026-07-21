using System;
using System.Collections.Generic;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class GameManagerSnapshotTests
    {
        private GameObject gameObject;
        private GameManager gameManager;

        [SetUp]
        public void SetUp()
        {
            Assert.That(GameManager.Instance, Is.Null);
            gameObject = new GameObject("GameManagerSnapshotTests");
            gameManager = gameObject.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
                UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AuthoritativeSnapshots_MissingOwner_AreRejected()
        {
            var foreignItem = new InventoryItemData(
                "item-b",
                "iron-b",
                "player-b",
                InventoryItemKind.Material,
                1,
                true,
                0,
                1);
            var inventorySnapshot = new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                string.Empty,
                1,
                0,
                new List<InventoryItemData> { foreignItem });
            ProfessionSnapshotData professionSnapshot = CreateProfessionSnapshot(string.Empty, 1);

            Assert.Throws<InvalidOperationException>(
                () => gameManager.ApplyAuthoritativeInventorySnapshot(
                    "player-a",
                    inventorySnapshot));
            Assert.Throws<InvalidOperationException>(
                () => gameManager.ApplyAuthoritativeProfessionSnapshot(
                    "player-a",
                    professionSnapshot));
        }

        [Test]
        public void PartialSnapshots_DifferentPlayer_AreRejectedWithoutMutation()
        {
            gameManager.ApplyAuthoritativePlayerState(
                "player-a",
                CreateInventorySnapshot("player-a", 10),
                CreateProfessionSnapshot("player-a", 10));

            Assert.Throws<InvalidOperationException>(
                () => gameManager.ApplyAuthoritativeInventorySnapshot(
                    "player-b",
                    CreateInventorySnapshot("player-b", 1)));
            Assert.Throws<InvalidOperationException>(
                () => gameManager.ApplyAuthoritativeProfessionSnapshot(
                    "player-b",
                    CreateProfessionSnapshot("player-b", 1)));

            Assert.That(gameManager.CurrentPlayerId, Is.EqualTo("player-a"));
            Assert.That(gameManager.Inventory.PlayerId, Is.EqualTo("player-a"));
            Assert.That(gameManager.Professions.PlayerId, Is.EqualTo("player-a"));
        }

        [Test]
        public void AtomicPlayerState_DifferentPlayerWithLowerRevisions_ReplacesAllAggregates()
        {
            gameManager.ApplyAuthoritativePlayerState(
                "player-a",
                CreateInventorySnapshot("player-a", 10),
                CreateProfessionSnapshot("player-a", 10));

            gameManager.ApplyAuthoritativePlayerState(
                "player-b",
                CreateInventorySnapshot("player-b", 1),
                CreateProfessionSnapshot("player-b", 1));

            Assert.That(gameManager.CurrentPlayerId, Is.EqualTo("player-b"));
            Assert.That(gameManager.Inventory.PlayerId, Is.EqualTo("player-b"));
            Assert.That(gameManager.Professions.PlayerId, Is.EqualTo("player-b"));
        }

        private static InventorySnapshotData CreateInventorySnapshot(
            string playerId,
            long serverRevision)
        {
            return new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                playerId,
                serverRevision,
                0,
                new List<InventoryItemData>());
        }

        private static ProfessionSnapshotData CreateProfessionSnapshot(
            string playerId,
            long serverRevision)
        {
            ProfessionSnapshotData empty = ProfessionSnapshotData.CreateEmpty(playerId);
            return new ProfessionSnapshotData(
                ProfessionSnapshotData.CurrentSchemaVersion,
                playerId,
                serverRevision,
                0,
                empty.PrimaryProfession,
                empty.FocusAvailable,
                empty.FocusCap,
                new List<ProfessionProgressData>(empty.Professions),
                new List<RecipeUnlockData>(),
                new List<CraftingJobData>());
        }
    }
}
