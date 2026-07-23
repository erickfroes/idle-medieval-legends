using System;
using System.Collections;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;

namespace IdleMedievalLegends.Tests.PlayMode
{
    public sealed class InventoryPanelPlayModeTests
    {
        [UnityTest]
        public IEnumerator InventoryPanel_OpenListAndSelect_ShowsSeededItems()
        {
            yield return OpenFreshInventory();
            InventoryPanelController panel = FindPanel();
            string swordId = FindByDefinition("item_iron_sword_t1").InstanceId;

            bool selected = panel.SelectItem(swordId);

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.VisibleItemCount, Is.GreaterThanOrEqualTo(5));
            Assert.That(selected, Is.True);
            Assert.That(panel.SelectedInstanceId, Is.EqualTo(swordId));
        }

        [UnityTest]
        public IEnumerator InventoryPanel_EquipAndUnequip_ChangesDomainState()
        {
            yield return OpenFreshInventory();
            InventoryPanelController panel = FindPanel();
            ItemInstance sword = FindByDefinition("item_iron_sword_t1");
            panel.SelectItem(sword.InstanceId);

            panel.EquipSelected();
            yield return null;
            Assert.That(sword.State, Is.EqualTo(InventoryItemState.Equipped));

            panel.UnequipSelected();
            yield return null;
            Assert.That(sword.State, Is.EqualTo(InventoryItemState.Owned));
        }

        [UnityTest]
        public IEnumerator InventoryPanel_LockAndFilter_UpdatesVisibleList()
        {
            yield return OpenFreshInventory();
            InventoryPanelController panel = FindPanel();
            ItemInstance sword = FindByDefinition("item_iron_sword_t1");
            panel.SelectItem(sword.InstanceId);

            panel.LockSelected();
            yield return null;
            panel.SetLockedOnly(true);
            yield return null;

            Assert.That(sword.LockedByPlayer, Is.True);
            Assert.That(panel.VisibleItemCount, Is.EqualTo(1));
            panel.SetCategory(InventoryCategoryFilter.Equipment);
            Assert.That(panel.VisibleItemCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InventoryCache_SaveAndRestart_RestoresLockedItem()
        {
            yield return OpenFreshInventory();
            InventoryPanelController panel = FindPanel();
            ItemInstance sword = FindByDefinition("item_iron_sword_t1");
            string instanceId = sword.InstanceId;
            panel.SelectItem(instanceId);
            panel.LockSelected();
            yield return null;

            GameManager manager = GameManager.Instance;
            var saveTask = manager.PersistLocalCacheAsync(default);
            while (!saveTask.IsCompleted) yield return null;
            Assert.That(saveTask.IsFaulted, Is.False);
            UnityEngine.Object.Destroy(manager.gameObject);
            yield return null;

            AsyncOperation bootstrap = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            while (!bootstrap.isDone) yield return null;
            float timeout = Time.realtimeSinceStartup + 10f;
            while ((GameManager.Instance == null || !GameManager.Instance.IsReady) &&
                   Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance.IsReady, Is.True);
            Assert.That(GameManager.Instance.Inventory.GetItem(instanceId).LockedByPlayer, Is.True);
        }

        [UnityTest]
        public IEnumerator InventoryPanel_DismantleConfirmation_DestroysAndCreatesMaterials()
        {
            yield return OpenFreshInventory();
            InventoryPanelController panel = FindPanel();
            ItemInstance sword = FindByDefinition("item_iron_sword_t1");
            long before = SumActiveQuantity("material_iron_ingot_t1");
            panel.SelectItem(sword.InstanceId);

            panel.RequestDismantleSelected();
            panel.ConfirmDestructiveAction();
            yield return null;

            Assert.That(sword.State, Is.EqualTo(InventoryItemState.Destroyed));
            Assert.That(SumActiveQuantity("material_iron_ingot_t1"), Is.EqualTo(before + 2));
        }

        [UnityTest]
        public IEnumerator InventoryToBattle_NavigationLeavesSingleEventSystem()
        {
            yield return OpenFreshInventory();
            InventoryPanelController panel = FindPanel();

            panel.ReturnToBattle();
            float timeout = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != "Battle" &&
                   Time.realtimeSinceStartup < timeout)
                yield return null;
            yield return null;

            EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Exclude);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Battle"));
            Assert.That(eventSystems, Has.Length.EqualTo(1));
        }

        private static IEnumerator OpenFreshInventory()
        {
            AsyncOperation bootstrap = SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            while (!bootstrap.isDone) yield return null;
            float timeout = Time.realtimeSinceStartup + 10f;
            while ((GameManager.Instance == null || !GameManager.Instance.IsReady) &&
                   Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance.IsReady, Is.True);
            GameManager manager = GameManager.Instance;
            manager.Inventory.Clear(manager.CurrentPlayerId);
            manager.Inventory.ConfigureDefinitionResolver(manager.ContentCatalog);
            long timestamp = Math.Max(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                manager.Inventory.CaptureSnapshotForCache().GeneratedAtUnixMilliseconds);
            DevelopmentInventorySeeder.SeedIfEmpty(
                manager.Inventory, manager.ContentCatalog, manager.CurrentPlayerId, timestamp);

            AsyncOperation inventoryScene =
                SceneManager.LoadSceneAsync("Inventory", LoadSceneMode.Single);
            while (!inventoryScene.isDone) yield return null;
            timeout = Time.realtimeSinceStartup + 5f;
            while ((FindPanel() == null || FindPanel().VisibleItemCount == 0) &&
                   Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(FindPanel(), Is.Not.Null);
            Assert.That(FindPanel().VisibleItemCount, Is.GreaterThan(0));
        }

        private static InventoryPanelController FindPanel()
        {
            return UnityEngine.Object.FindAnyObjectByType<InventoryPanelController>();
        }

        private static ItemInstance FindByDefinition(string definitionId)
        {
            foreach (ItemInstance item in GameManager.Instance.Inventory.Items)
            {
                if (item.DefinitionId == definitionId) return item;
            }
            throw new InvalidOperationException($"Item não encontrado: {definitionId}.");
        }

        private static long SumActiveQuantity(string definitionId)
        {
            long total = 0;
            foreach (ItemInstance item in GameManager.Instance.Inventory.Items)
            {
                if (item.DefinitionId == definitionId && !item.IsTerminal)
                    total += item.Quantity;
            }
            return total;
        }
    }
}
