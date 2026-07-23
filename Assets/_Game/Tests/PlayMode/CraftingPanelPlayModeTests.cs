using System;
using System.Collections;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Presentation.Crafting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdleMedievalLegends.Tests.PlayMode
{
    public sealed class CraftingPanelPlayModeTests
    {
        [UnityTest]
        public IEnumerator CraftingPanel_OpenSelectProfessionAndRecipe_ShowsSelection()
        {
            ManualServerClock clock = null;
            yield return OpenFreshCrafting(value => clock = value);
            CraftingPanelController panel = FindPanel();

            panel.SelectProfession(CraftingProfession.Tailor);
            bool selected = panel.SelectRecipe("recipe_treated_leather_t1");

            Assert.That(clock, Is.Not.Null);
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.SelectedProfession, Is.EqualTo(CraftingProfession.Tailor));
            Assert.That(selected, Is.True);
            Assert.That(panel.SelectedRecipeId, Is.EqualTo("recipe_treated_leather_t1"));
        }

        [UnityTest]
        public IEnumerator CraftingPanel_StartDisplayComplete_ReceivesInventoryOutput()
        {
            ManualServerClock clock = null;
            yield return OpenFreshCrafting(value => clock = value);
            CraftingPanelController panel = FindPanel();
            panel.SelectProfession(CraftingProfession.Blacksmith);
            Assert.That(panel.SelectRecipe("recipe_iron_sword_t1"), Is.True);

            CraftingJob job = panel.StartSelectedRecipe();
            yield return null;

            Assert.That(job, Is.Not.Null, panel.Status);
            Assert.That(panel.DisplayedJobCount, Is.EqualTo(1));
            Assert.That(job.State, Is.EqualTo(CraftingJobStatus.Running));

            clock.AdvanceMilliseconds(60000);
            yield return null;
            CraftingResult result = panel.CompleteSelectedJob();
            yield return null;

            Assert.That(result, Is.Not.Null, panel.Status);
            Assert.That(job.State, Is.EqualTo(CraftingJobStatus.Completed));
            Assert.That(panel.LastResult, Is.SameAs(result));
            Assert.That(
                GameManager.Instance.Inventory.GetItem(result.Outputs[0].InstanceId).DefinitionId,
                Is.EqualTo("item_iron_sword_t1"));
        }

        private static IEnumerator OpenFreshCrafting(Action<ManualServerClock> clockReady)
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
            var clock = new ManualServerClock(
                manager.Inventory.CaptureSnapshotForCache().GeneratedAtUnixMilliseconds);
            manager.ResetLocalCraftingPrototype(clock);
            clockReady(clock);

            AsyncOperation scene = SceneManager.LoadSceneAsync("Crafting", LoadSceneMode.Single);
            while (!scene.isDone) yield return null;
            timeout = Time.realtimeSinceStartup + 5f;
            while ((FindPanel() == null || !FindPanel().IsOpen) &&
                   Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(FindPanel(), Is.Not.Null);
            Assert.That(FindPanel().IsOpen, Is.True);
        }

        private static CraftingPanelController FindPanel()
        {
            return UnityEngine.Object.FindAnyObjectByType<CraftingPanelController>();
        }
    }
}
