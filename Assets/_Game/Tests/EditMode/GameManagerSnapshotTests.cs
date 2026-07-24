using System;
using System.Collections.Generic;
using System.Reflection;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Economy;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Editor.ContentCatalog;
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
            var foreignItem = new ItemInstance(
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
                new List<ItemInstance> { foreignItem });
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

        [Test]
        public void AtomicPlayerState_DifferentPlayer_RebuildsLocalCraftingState()
        {
            CraftingBalanceConfigAsset config =
                ScriptableObject.CreateInstance<CraftingBalanceConfigAsset>();
            try
            {
                config.EnsureInitialized();
                ContentCatalogLookup catalog = new ContentCatalogLookup(
                    ContentCatalogDemoFactory.Create());
                typeof(GameManager).GetProperty(nameof(GameManager.ContentCatalog))
                    ?.GetSetMethod(true)
                    ?.Invoke(gameManager, new object[] { catalog });
                typeof(GameManager).GetField(
                        "craftingBalanceConfig",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(gameManager, config);

                gameManager.ApplyAuthoritativePlayerState(
                    "player-a",
                    CreateInventorySnapshot("player-a", 10),
                    CreateProfessionSnapshot("player-a", 10));
                LocalCraftingService previous = gameManager.LocalCrafting;
                LocalGoldEconomyService previousWallet = gameManager.GoldWallet;
                Assert.That(previous, Is.Not.Null);
                Assert.That(previousWallet, Is.Not.Null);
                previousWallet.Credit(
                    777,
                    "player_a_reward",
                    "player_a_reward",
                    1,
                    "test");
                previous.StartCraft(
                    CraftingProfession.Gatherer,
                    "recipe_gather_iron_ore_t1",
                    1);
                Assert.That(previous.Queue.Jobs, Has.Count.EqualTo(1));

                gameManager.ApplyAuthoritativePlayerState(
                    "player-b",
                    CreateInventorySnapshot("player-b", 1),
                    CreateProfessionSnapshot("player-b", 1));

                Assert.That(gameManager.LocalCrafting, Is.Not.SameAs(previous));
                Assert.That(gameManager.LocalCrafting.PlayerId, Is.EqualTo("player-b"));
                Assert.That(gameManager.LocalCrafting.Queue.Jobs, Is.Empty);
                Assert.That(gameManager.GoldWallet, Is.Not.SameAs(previousWallet));
                Assert.That(gameManager.GoldWallet.GoldBalance, Is.EqualTo(50000));
                Assert.That(gameManager.LocalCrafting.GoldBalance, Is.EqualTo(50000));
                Assert.That(
                    gameManager.GoldWallet.Ledger,
                    Has.None.Matches<GoldLedgerEntry>(
                        entry => entry.RequestId == "player_a_reward"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void AuthoritativeInventory_LocalMutations_DoNotRejectNextServerRevision()
        {
            gameManager.ApplyAuthoritativeInventorySnapshot(
                "player-a",
                CreateInventorySnapshot("player-a", 10));
            var definition = new ItemDefinition(
                "material-local", "Material local", "Teste", ItemType.Material,
                ContentTier.Tier1, Rarity.Common, true, 99, true, true, true);
            var item = new ItemInstance(
                "local-item", definition.DefinitionId, "player-a",
                InventoryItemKind.Material, GameRarity.Common, ItemTier.Tier1,
                1, true, InventoryItemState.Owned, ItemBinding.Unbound,
                string.Empty, string.Empty, string.Empty, 0,
                Array.Empty<RolledStatData>(), 0, -1, -1, false, 0, 0, 0,
                new ItemProvenanceData("test", "local", "tx:local"));
            gameManager.Inventory.AddAuthorizedItem(item, definition, 1);
            gameManager.Inventory.Lock(item.InstanceId, 2);

            Assert.DoesNotThrow(() => gameManager.ApplyAuthoritativeInventorySnapshot(
                "player-a",
                CreateInventorySnapshot("player-a", 11)));

            Assert.That(gameManager.Inventory.ServerRevision, Is.EqualTo(11));
            Assert.That(gameManager.Inventory.Revision, Is.Zero);
            Assert.That(gameManager.Inventory.Items, Is.Empty);
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
                new List<ItemInstance>());
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
