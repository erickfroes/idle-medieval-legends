using System;
using System.Collections.Generic;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Campaign;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Economy;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Editor.Campaign;
using IdleMedievalLegends.Infrastructure.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class IdleProgressionTests
    {
        private const long StartTime = 1_000_000;
        private readonly List<UnityEngine.Object> temporaryObjects =
            new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < temporaryObjects.Count; i++)
                UnityEngine.Object.DestroyImmediate(temporaryObjects[i]);
            temporaryObjects.Clear();
        }

        [Test]
        public void IdleReward_ZeroMinutes_ReturnsZero()
        {
            OfflineRewardReport report = CalculateDirect(0);

            Assert.That(report.Gold, Is.Zero);
            Assert.That(report.Materials, Is.Empty);
            Assert.That(report.EligibleDurationMilliseconds, Is.Zero);
        }

        [Test]
        public void IdleReward_OneHour_CalculatesGoldAndMaterials()
        {
            OfflineRewardReport report = CalculateDirect(60L * 60L * 1000L);

            Assert.That(report.Gold, Is.EqualTo(600));
            Assert.That(report.Materials[0].Quantity, Is.EqualTo(60));
        }

        [Test]
        public void IdleReward_EightHours_UsesEntireConfiguredLimit()
        {
            OfflineRewardReport report = CalculateDirect(8L * 60L * 60L * 1000L);

            Assert.That(report.Gold, Is.EqualTo(4800));
            Assert.That(report.EligibleDurationMilliseconds,
                Is.EqualTo(8L * 60L * 60L * 1000L));
        }

        [Test]
        public void IdleReward_AboveLimit_DiscardsExcess()
        {
            OfflineRewardReport report = CalculateDirect(12L * 60L * 60L * 1000L);

            Assert.That(report.Gold, Is.EqualTo(4800));
            Assert.That(report.DiscardedDurationMilliseconds,
                Is.EqualTo(4L * 60L * 60L * 1000L));
        }

        [Test]
        public void TimeValidation_ClockRegression_ReturnsZeroReward()
        {
            var calculator = new IdleRewardCalculator();
            TimeValidationResult validation = calculator.ValidateTime(
                StartTime,
                StartTime - 1,
                720L * 60L * 60L * 1000L,
                8L * 60L * 60L * 1000L);

            Assert.That(validation.Code, Is.EqualTo(TimeValidationCode.ClockRegression));
            Assert.That(validation.ValidatedElapsedMilliseconds, Is.Zero);
        }

        [Test]
        public void TimeValidation_ExtremeJump_AppliesSafeLimit()
        {
            var calculator = new IdleRewardCalculator();
            long jump = 1000L * 60L * 60L * 1000L;
            TimeValidationResult validation = calculator.ValidateTime(
                StartTime,
                StartTime + jump,
                720L * 60L * 60L * 1000L,
                8L * 60L * 60L * 1000L);

            Assert.That(validation.Code, Is.EqualTo(TimeValidationCode.ExtremeJumpLimited));
            Assert.That(validation.ValidatedElapsedMilliseconds,
                Is.EqualTo(8L * 60L * 60L * 1000L));
        }

        [Test]
        public void Campaign_FirstVictory_GrantsFirstClearReward()
        {
            Harness harness = CreateHarness();
            long before = harness.Service.GoldBalance;

            CampaignBattleResult result = Complete(
                harness.Service, "stage_01", "battle_first", true);

            Assert.That(result.FirstClear, Is.True);
            Assert.That(harness.Service.GoldBalance - before,
                Is.EqualTo(result.Stage.FirstClearRewards.Gold));
            Assert.That(MaterialQuantity(harness.Inventory, "material_iron_ore_t1"),
                Is.EqualTo(5));
        }

        [Test]
        public void Campaign_RepeatedVictory_DoesNotDuplicateFirstClear()
        {
            Harness harness = CreateHarness();
            Complete(harness.Service, "stage_01", "battle_first", true);
            long afterFirst = harness.Service.GoldBalance;

            CampaignBattleResult repeat = Complete(
                harness.Service, "stage_01", "battle_repeat", true);

            Assert.That(repeat.FirstClear, Is.False);
            Assert.That(harness.Service.GoldBalance - afterFirst,
                Is.EqualTo(repeat.Stage.RepeatRewards.Gold));
            Assert.That(
                harness.Service.Progress.ClaimedFirstClearStageIds,
                Is.EquivalentTo(new[] { "stage_01" }));
        }

        [Test]
        public void OfflineCollection_SameRequestId_IsIdempotent()
        {
            Harness harness = CreateHarness();
            Complete(harness.Service, "stage_01", "unlock_idle", true);
            harness.Clock.AdvanceHours(1);
            OfflineRewardReport report =
                harness.Service.GenerateOfflineReport("offline_same");
            harness.Service.CollectOfflineReport(report.RequestId);
            long afterFirst = harness.Service.GoldBalance;

            OfflineRewardReport second =
                harness.Service.CollectOfflineReport(report.RequestId);

            Assert.That(second, Is.SameAs(report));
            Assert.That(harness.Service.GoldBalance, Is.EqualTo(afterFirst));
            Assert.That(harness.Service.GoldLedger.FindAll(
                entry => entry.RequestId == "offline_gold:offline_same").Count,
                Is.EqualTo(1));
        }

        [Test]
        public void OfflineCollection_UsesCalculatedGoldAndMaterials()
        {
            Harness harness = CreateHarness();
            Complete(harness.Service, "stage_01", "unlock_idle", true);
            long goldBefore = harness.Service.GoldBalance;
            long materialBefore = MaterialQuantity(
                harness.Inventory, "material_iron_ore_t1");
            harness.Clock.AdvanceHours(1);
            OfflineRewardReport report =
                harness.Service.GenerateOfflineReport("offline_rewards");

            harness.Service.CollectOfflineReport(report.RequestId);

            Assert.That(report.Gold, Is.EqualTo(600));
            Assert.That(harness.Service.GoldBalance - goldBefore, Is.EqualTo(600));
            Assert.That(
                MaterialQuantity(harness.Inventory, "material_iron_ore_t1") -
                materialBefore,
                Is.EqualTo(60));
        }

        [Test]
        public void Campaign_Defeat_DoesNotAdvance()
        {
            Harness harness = CreateHarness();

            CampaignBattleResult result = Complete(
                harness.Service, "stage_01", "battle_defeat", false);

            Assert.That(result.Victory, Is.False);
            Assert.That(harness.Service.Progress.CurrentStageId, Is.EqualTo("stage_01"));
            Assert.That(harness.Service.Progress.HighestClearedStageId, Is.Empty);
        }

        [Test]
        public void Campaign_Victory_AdvancesToNextStage()
        {
            Harness harness = CreateHarness();

            CampaignBattleResult result = Complete(
                harness.Service, "stage_01", "battle_victory", true);

            Assert.That(result.Advanced, Is.True);
            Assert.That(harness.Service.Progress.CurrentStageId, Is.EqualTo("stage_02"));
            Assert.That(harness.Service.Progress.HighestClearedStageId, Is.EqualTo("stage_01"));
        }

        [Test]
        public void Campaign_ReplayingEarlierStage_HighestStageDoesNotDecrease()
        {
            Harness harness = CreateHarness();
            Complete(harness.Service, "stage_01", "clear_1", true);
            Complete(harness.Service, "stage_02", "clear_2", true);

            Complete(harness.Service, "stage_01", "replay_1", true);

            Assert.That(harness.Service.Progress.HighestClearedStageId, Is.EqualTo("stage_02"));
            Assert.That(harness.Service.Progress.CurrentStageId, Is.EqualTo("stage_03"));
        }

        [Test]
        public void IdleReward_LongRate_PreservesValuesAboveInt32()
        {
            long result = IdleRewardCalculator.CalculateRate(
                3_000_000_000L,
                60_000L,
                new[] { new IdleRewardMultiplier("base", 10000) });

            Assert.That(result, Is.EqualTo(3_000_000_000L));
            Assert.That(result, Is.GreaterThan(int.MaxValue));
        }

        [Test]
        public void OfflineCollection_TimeAdvancedAfterReport_DoesNotRecalculate()
        {
            Harness harness = CreateHarness();
            Complete(harness.Service, "stage_01", "unlock_idle", true);
            harness.Clock.AdvanceHours(1);
            OfflineRewardReport report =
                harness.Service.GenerateOfflineReport("fixed_report");
            long expected = report.Gold;
            harness.Clock.AdvanceHours(7);
            long before = harness.Service.GoldBalance;

            harness.Service.CollectOfflineReport(report.RequestId);

            Assert.That(harness.Service.GoldBalance - before, Is.EqualTo(expected));
            Assert.That(report.EligibleDurationMilliseconds,
                Is.EqualTo(60L * 60L * 1000L));
        }

        [Test]
        public void GoldLedger_AllEntriesReconcileToBalance()
        {
            Harness harness = CreateHarness(initialGold: 500);
            Complete(harness.Service, "stage_01", "ledger_clear", true);
            harness.Clock.AdvanceHours(1);
            harness.Service.CollectOfflineReport(
                harness.Service.GenerateOfflineReport("ledger_offline").RequestId);

            long running = 0;
            for (int i = 0; i < harness.Service.GoldLedger.Count; i++)
            {
                running = checked(running + harness.Service.GoldLedger[i].Delta);
                Assert.That(
                    harness.Service.GoldLedger[i].BalanceAfter,
                    Is.EqualTo(running));
            }
            Assert.That(running, Is.EqualTo(harness.Service.GoldBalance));
            Assert.DoesNotThrow(() => harness.Wallet.CaptureSnapshot().Validate());
        }

        [Test]
        public void GoldWalletSnapshot_DeserializedMalformedState_IsRejected()
        {
            string[] malformedSnapshots =
            {
                "{\"balance\":-5,\"revision\":1,\"ledger\":[{\"entryId\":\"e1\",\"reason\":\"seed\",\"delta\":-5,\"balanceAfter\":-5,\"requestId\":\"r1\",\"timestamp\":1,\"source\":\"test\"}]}",
                "{\"balance\":0,\"revision\":-1,\"ledger\":[]}",
                "{\"balance\":5,\"revision\":1,\"ledger\":[{\"entryId\":\"e1\",\"reason\":\"seed\",\"delta\":5,\"balanceAfter\":5,\"requestId\":\"\",\"timestamp\":-1,\"source\":\"\"}]}",
                "{\"balance\":10,\"revision\":2,\"ledger\":[{\"entryId\":\"e1\",\"reason\":\"seed\",\"delta\":5,\"balanceAfter\":5,\"requestId\":\"r1\",\"timestamp\":1,\"source\":\"test\"},{\"entryId\":\"e1\",\"reason\":\"reward\",\"delta\":5,\"balanceAfter\":10,\"requestId\":\"r2\",\"timestamp\":2,\"source\":\"test\"}]}",
                "{\"balance\":5,\"revision\":2,\"ledger\":[{\"entryId\":\"e1\",\"reason\":\"seed\",\"delta\":5,\"balanceAfter\":5,\"requestId\":\"r1\",\"timestamp\":1,\"source\":\"test\"}]}"
            };

            for (int i = 0; i < malformedSnapshots.Length; i++)
            {
                GoldWalletSnapshot snapshot =
                    JsonUtility.FromJson<GoldWalletSnapshot>(
                        malformedSnapshots[i]);

                Assert.Throws<InvalidOperationException>(
                    () => snapshot.Validate(),
                    $"Snapshot inválido no índice {i} foi aceito.");
            }
        }

        [Test]
        public void CampaignDefinition_Demo_HasTenConfigurableStages()
        {
            CampaignConfigAsset config = CreateConfig();
            CampaignDefinition campaign = config.BuildDefinition();

            Assert.That(campaign.Stages, Has.Count.EqualTo(10));
            Assert.That(campaign.Chapters, Has.Count.EqualTo(2));
            for (int i = 1; i < campaign.Stages.Count; i++)
            {
                Assert.That(
                    campaign.Stages[i].RecommendedPower,
                    Is.GreaterThan(campaign.Stages[i - 1].RecommendedPower));
            }
        }

        [Test]
        public void CampaignScene_CanonicalComposition_HasNoValidationErrors()
        {
            Assert.That(CampaignSceneTools.GetValidationErrors(), Is.Empty);
        }

        [Test]
        public void CampaignSnapshot_OlderRevision_IsRejected()
        {
            Harness harness = CreateHarness();
            PlayerCampaignProgress old = harness.Service.CaptureSnapshot();
            Complete(harness.Service, "stage_01", "newer_state", true);

            Assert.Throws<InvalidOperationException>(
                () => harness.Service.ApplySnapshot(old));
        }

        [Test]
        public void CampaignCache_RoundTrip_PreservesReportProgressAndGoldLedger()
        {
            Harness harness = CreateHarness(initialGold: 500);
            Complete(harness.Service, "stage_01", "cache_clear", true);
            harness.Clock.AdvanceHours(1);
            OfflineRewardReport report =
                harness.Service.GenerateOfflineReport("cache_report");
            var save = new GameSaveData(
                "test_player",
                harness.Inventory.CaptureSnapshotForCache(),
                ProfessionSnapshotData.CreateEmpty("test_player"),
                harness.Service.CaptureSnapshot(),
                harness.Wallet.CaptureSnapshot());

            string json = JsonUtility.ToJson(save);
            GameSaveData restored = GameSaveMigration.UpgradeToCurrent(
                JsonUtility.FromJson<GameSaveData>(json));

            Assert.That(restored.Campaign.CurrentStageId, Is.EqualTo("stage_02"));
            Assert.That(restored.Campaign.PendingOfflineReport.RequestId,
                Is.EqualTo(report.RequestId));
            Assert.That(restored.Campaign.PendingOfflineReport.Gold,
                Is.EqualTo(report.Gold));
            Assert.That(restored.GoldWallet.Balance,
                Is.EqualTo(harness.Service.GoldBalance));
            Assert.DoesNotThrow(() => restored.GoldWallet.Validate());
        }

        [Test]
        public void OfflineReport_MissingTimestamp_InitializesWithoutRetroactiveReward()
        {
            CampaignConfigAsset config = CreateConfig();
            CampaignDefinition campaign = config.BuildDefinition();
            var progress = new PlayerCampaignProgress(
                "stage_01",
                string.Empty,
                0,
                0,
                Array.Empty<string>(),
                0,
                campaign.RulesVersion);
            Harness harness = CreateHarness(progress: progress);

            OfflineRewardReport report =
                harness.Service.GenerateOfflineReport("missing_time");

            Assert.That(report.TimeValidationCode,
                Is.EqualTo(TimeValidationCode.MissingTimestamp));
            Assert.That(report.Gold, Is.Zero);
            Assert.That(report.EligibleDurationMilliseconds, Is.Zero);
        }

        private OfflineRewardReport CalculateDirect(long elapsed)
        {
            var calculator = new IdleRewardCalculator();
            var session = new OfflineSession(
                StartTime,
                StartTime + elapsed,
                "stage_01",
                1,
                CampaignConfigAsset.DefaultRulesVersion,
                "direct");
            var profile = new IdleProductionProfile(
                "stage_01",
                10,
                new[] { new CampaignMaterialReward("material_iron_ore_t1", 1) },
                0,
                0,
                8L * 60L * 60L * 1000L,
                8L * 60L * 60L * 1000L,
                new[] { new IdleRewardMultiplier("base", 10000) });
            TimeValidationResult validation = calculator.ValidateTime(
                session.StartUnixMilliseconds,
                session.EndUnixMilliseconds,
                720L * 60L * 60L * 1000L,
                8L * 60L * 60L * 1000L);
            return calculator.Calculate(session, profile, validation);
        }

        private Harness CreateHarness(
            long initialGold = 0,
            PlayerCampaignProgress progress = null)
        {
            ContentCatalogAsset catalogAsset =
                AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(
                    "Assets/_Game/Data/Content/ContentCatalog.asset");
            CombatBalanceConfigAsset combatAsset =
                AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(
                    "Assets/_Game/Data/Balance/CombatBalanceConfig.asset");
            Assert.That(catalogAsset, Is.Not.Null);
            Assert.That(combatAsset, Is.Not.Null);
            ContentCatalogLookup catalog = catalogAsset.BuildValidatedLookup();
            CampaignConfigAsset config = CreateConfig();
            CampaignDefinition campaign = config.BuildDefinition();
            var inventory = new PlayerInventory();
            inventory.ApplyServerSnapshot(new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                "test_player",
                0,
                0,
                StartTime,
                new List<ItemInstance>()), catalog);
            var clock = new DevelopmentGameClock(StartTime);
            var wallet = new LocalGoldEconomyService(initialGold);
            var service = new IdleProgressionService(
                "test_player",
                campaign,
                config,
                catalog,
                combatAsset.Tuning,
                new InventoryEquipmentModifierProvider(inventory),
                inventory,
                wallet,
                clock,
                progress);
            return new Harness(service, inventory, wallet, clock);
        }

        private CampaignConfigAsset CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<CampaignConfigAsset>();
            temporaryObjects.Add(config);
            return config;
        }

        private static CampaignBattleResult Complete(
            IdleProgressionService service,
            string stageId,
            string requestId,
            bool victory)
        {
            BattleOutcome outcome = victory
                ? BattleOutcome.AttackerVictory
                : BattleOutcome.DefenderVictory;
            BattleSide side = victory ? BattleSide.Attacker : BattleSide.Defender;
            var battle = new BattleResult(
                $"campaign:{stageId}:{requestId}",
                9,
                outcome,
                side,
                1,
                1,
                victory ? "attacker_eliminated_defender" : "defender_eliminated_attacker",
                "combat_rules_v1",
                Array.Empty<CombatEvent>(),
                Array.Empty<CombatSnapshot>());
            return service.CompleteStageBattle(stageId, requestId, battle);
        }

        private static long MaterialQuantity(PlayerInventory inventory, string definitionId)
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

        private sealed class Harness
        {
            public Harness(
                IdleProgressionService service,
                PlayerInventory inventory,
                LocalGoldEconomyService wallet,
                DevelopmentGameClock clock)
            {
                Service = service;
                Inventory = inventory;
                Wallet = wallet;
                Clock = clock;
            }

            public IdleProgressionService Service { get; }
            public PlayerInventory Inventory { get; }
            public LocalGoldEconomyService Wallet { get; }
            public DevelopmentGameClock Clock { get; }
        }
    }

    internal static class LedgerTestExtensions
    {
        public static List<T> FindAll<T>(
            this IReadOnlyList<T> source,
            Predicate<T> predicate)
        {
            var result = new List<T>();
            for (int i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                    result.Add(source[i]);
            }
            return result;
        }
    }
}
