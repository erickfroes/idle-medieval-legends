using System;
using System.Collections;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Campaign;
using IdleMedievalLegends.Domain.Dungeons;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Presentation.Battle;
using IdleMedievalLegends.Presentation.Dungeon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdleMedievalLegends.Tests.PlayMode
{
    public sealed class DungeonPanelPlayModeTests
    {
        [UnityTest]
        public IEnumerator Dungeon_OpenSelectEnterBattleComplete_UpdatesEnergyAndInventory()
        {
            yield return LoadBootstrap();
            GameManager manager = GameManager.Instance;
            Assert.That(manager.LocalDungeons, Is.Not.Null);
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var clock = new DevelopmentGameClock(now);
            PlayerCampaignProgress campaignProgress =
                PlayerCampaignProgress.CreateNew(
                    manager.IdleProgression.Campaign,
                    now);
            manager.ResetLocalIdlePrototype(clock, campaignProgress);
            CompleteFirstCampaignStage(manager.IdleProgression);
            manager.ResetLocalDungeonPrototype(
                clock,
                new EnergyWallet(
                    manager.DungeonConfig.MaximumEnergy,
                    manager.DungeonConfig.MaximumEnergy,
                    now),
                new DungeonProgress());
            int energyBefore = manager.LocalDungeons.Energy.CurrentEnergy;
            long inventoryBefore = MaterialQuantity(manager.Inventory);

            AsyncOperation load = SceneManager.LoadSceneAsync(
                "Dungeon",
                LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            DungeonPanelController panel = null;
            float timeout = Time.realtimeSinceStartup + 5f;
            while ((panel == null || !panel.IsOpen) &&
                   Time.realtimeSinceStartup < timeout)
            {
                panel = UnityEngine.Object.FindAnyObjectByType<DungeonPanelController>();
                yield return null;
            }
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(manager.LocalDungeons.Dungeons.Dungeons, Has.Count.EqualTo(5));

            panel.SelectDungeon(0);
            panel.SelectDifficulty(1);
            Assert.That(panel.SelectedDungeon.DisplayName, Is.EqualTo("Mina de Minérios"));
            Assert.That(
                panel.SelectedDifficulty.DifficultyId,
                Is.EqualTo("mine_journeyman"));

            DungeonRun run = panel.EnterSelectedDungeon("playmode_dungeon");
            Assert.That(run.State, Is.EqualTo(DungeonRunState.InBattle));
            Assert.That(
                manager.LocalDungeons.Energy.CurrentEnergy,
                Is.EqualTo(energyBefore - run.Difficulty.EnergyCost));

            BattleSceneController battleController = null;
            timeout = Time.realtimeSinceStartup + 8f;
            while ((battleController == null ||
                    battleController.State == BattlePresentationState.Uninitialized) &&
                   Time.realtimeSinceStartup < timeout)
            {
                battleController =
                    UnityEngine.Object.FindAnyObjectByType<BattleSceneController>();
                yield return null;
            }
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Battle"));
            Assert.That(battleController, Is.Not.Null);
            Assert.That(battleController.Result, Is.SameAs(run.SimulatedBattle));
            battleController.SkipBattle();

            DungeonPanelController returnedPanel = null;
            timeout = Time.realtimeSinceStartup + 8f;
            while ((SceneManager.GetActiveScene().name != "Dungeon" ||
                    returnedPanel == null ||
                    !returnedPanel.IsOpen) &&
                   Time.realtimeSinceStartup < timeout)
            {
                returnedPanel =
                    UnityEngine.Object.FindAnyObjectByType<DungeonPanelController>();
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Dungeon"));
            Assert.That(returnedPanel, Is.Not.Null);
            Assert.That(manager.LocalDungeons.LatestResult, Is.Not.Null);
            Assert.That(manager.LocalDungeons.LatestResult.Victory, Is.True);
            Assert.That(
                manager.LocalDungeons.LatestResult.Run.State,
                Is.EqualTo(DungeonRunState.RewardsGranted));
            Assert.That(returnedPanel.DisplayedResult, Does.Contain("VITÓRIA"));
            Assert.That(
                manager.LocalDungeons.Energy.CurrentEnergy,
                Is.EqualTo(energyBefore - run.Difficulty.EnergyCost));
            Assert.That(
                MaterialQuantity(manager.Inventory),
                Is.GreaterThan(inventoryBefore));
        }

        private static IEnumerator LoadBootstrap()
        {
            BattleScenarioBridge.Clear();
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

        private static void CompleteFirstCampaignStage(
            IdleProgressionService progression)
        {
            const string requestId = "playmode_unlock_stage_02";
            var battle = new IdleMedievalLegends.Domain.Combat.BattleResult(
                $"campaign:stage_01:{requestId}",
                1,
                IdleMedievalLegends.Domain.Combat.BattleOutcome.AttackerVictory,
                IdleMedievalLegends.Domain.Combat.BattleSide.Attacker,
                1,
                1,
                "attacker_eliminated_defender",
                "combat_rules_v1",
                Array.Empty<IdleMedievalLegends.Domain.Combat.CombatEvent>(),
                Array.Empty<IdleMedievalLegends.Domain.Combat.CombatSnapshot>());
            progression.CompleteStageBattle("stage_01", requestId, battle);
        }

        private static long MaterialQuantity(PlayerInventory inventory)
        {
            long result = 0;
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                ItemInstance item = inventory.Items[i];
                if (!item.IsTerminal)
                    result = checked(result + item.Quantity);
            }
            return result;
        }
    }
}
