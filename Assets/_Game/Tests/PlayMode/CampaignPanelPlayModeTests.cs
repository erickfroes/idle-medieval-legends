using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Campaign;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Presentation.Campaign;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdleMedievalLegends.Tests.PlayMode
{
    public sealed class CampaignPanelPlayModeTests
    {
        [UnityTest]
        public IEnumerator Campaign_OpenBattleAdvanceReturnCollect_UpdatesWalletAndInventory()
        {
            yield return LoadBootstrap();
            GameManager manager = GameManager.Instance;
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.IdleProgression, Is.Not.Null);

            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var clock = new DevelopmentGameClock(start);
            PlayerCampaignProgress fresh = PlayerCampaignProgress.CreateNew(
                manager.IdleProgression.Campaign,
                start);
            manager.ResetLocalIdlePrototype(clock, fresh);

            AsyncOperation load = SceneManager.LoadSceneAsync(
                "Campaign",
                LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            CampaignPanelController panel = null;
            float timeout = Time.realtimeSinceStartup + 5f;
            while ((panel == null || !panel.IsOpen) &&
                   Time.realtimeSinceStartup < timeout)
            {
                panel = UnityEngine.Object.FindAnyObjectByType<CampaignPanelController>();
                yield return null;
            }
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.DisplayedGoldRate, Is.EqualTo("Produção: 0 ouro/min"));

            CampaignBattleResult battle = panel.StartCurrentStage();
            yield return null;

            Assert.That(battle, Is.Not.Null);
            Assert.That(battle.Victory, Is.True);
            Assert.That(battle.FirstClear, Is.True);
            Assert.That(manager.IdleProgression.Progress.CurrentStageId,
                Is.EqualTo("stage_02"));
            Assert.That(manager.IdleProgression.Progress.HighestClearedStageId,
                Is.EqualTo("stage_01"));
            Assert.That(panel.DisplayedGoldRate,
                Is.EqualTo("Produção: 10 ouro/min"));

            long goldBefore = manager.GoldWallet.GoldBalance;
            long materialsBefore = MaterialQuantity(
                manager.Inventory,
                "material_iron_ore_t1");
            Task simulation = panel.SimulateReturnHoursAsync(2);
            yield return WaitForTask(simulation);

            Assert.That(panel.IsOfflineModalOpen, Is.True);
            Assert.That(panel.CurrentReport, Is.Not.Null);
            Assert.That(panel.CurrentReport.EligibleDurationMilliseconds,
                Is.EqualTo(2L * 60L * 60L * 1000L));
            OfflineRewardReport generated = panel.CurrentReport;
            var loadPersistedReport =
                manager.CachedStateRepository.LoadAsync(CancellationToken.None);
            yield return WaitForTask(loadPersistedReport);
            Assert.That(
                loadPersistedReport.Result.Campaign.PendingOfflineReport.RequestId,
                Is.EqualTo(generated.RequestId));

            OfflineRewardReport collected = panel.CollectCurrentReport();
            yield return null;

            Assert.That(collected, Is.SameAs(generated));
            Assert.That(collected.Collected, Is.True);
            Assert.That(panel.IsOfflineModalOpen, Is.False);
            Assert.That(
                manager.GoldWallet.GoldBalance - goldBefore,
                Is.EqualTo(generated.Gold));
            Assert.That(
                MaterialQuantity(manager.Inventory, "material_iron_ore_t1") -
                materialsBefore,
                Is.EqualTo(generated.Materials[0].Quantity));

            long balanceAfterFirstCollection = manager.GoldWallet.GoldBalance;
            OfflineRewardReport repeated =
                manager.IdleProgression.CollectOfflineReport(generated.RequestId);
            Assert.That(repeated, Is.SameAs(generated));
            Assert.That(manager.GoldWallet.GoldBalance,
                Is.EqualTo(balanceAfterFirstCollection));
        }

        private static IEnumerator LoadBootstrap()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(
                "Bootstrap",
                LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            float timeout = Time.realtimeSinceStartup + 10f;
            while ((GameManager.Instance == null || !GameManager.Instance.IsReady) &&
                   Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance.IsReady, Is.True);
        }

        private static IEnumerator WaitForTask(Task task)
        {
            while (!task.IsCompleted)
                yield return null;
            if (task.IsFaulted)
                throw task.Exception?.GetBaseException() ??
                    new InvalidOperationException("Task assíncrona falhou.");
            if (task.IsCanceled)
                throw new OperationCanceledException();
        }

        private static long MaterialQuantity(
            PlayerInventory inventory,
            string definitionId)
        {
            long result = 0;
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                ItemInstance item = inventory.Items[i];
                if (item.DefinitionId == definitionId && !item.IsTerminal)
                    result = checked(result + item.Quantity);
            }
            return result;
        }
    }
}
