using System;
using System.Collections.Generic;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Campaign;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Dungeons;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Editor.Dungeon;
using NUnit.Framework;
using UnityEditor;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class DungeonLoopTests
    {
        private const long StartTime = 1_800_000_000_000L;

        [Test]
        public void EnergyRegeneration_AggregatedIntervals_GrantsExpectedPoints()
        {
            var clock = new DevelopmentGameClock(StartTime);
            var wallet = new EnergyWallet(5, 20, StartTime);
            var rules = new EnergyRegenerationRules(5, 20);
            clock.SetForTest(StartTime + 16 * 60_000L);

            int granted = rules.Regenerate(wallet, clock);

            Assert.That(granted, Is.EqualTo(3));
            Assert.That(wallet.CurrentEnergy, Is.EqualTo(8));
            Assert.That(
                wallet.LastRegenerationTime,
                Is.EqualTo(StartTime + 15 * 60_000L));
        }

        [Test]
        public void EnergyRegeneration_AboveMaximum_StopsAtMaximum()
        {
            var clock = new DevelopmentGameClock(StartTime + 60 * 60_000L);
            var wallet = new EnergyWallet(19, 20, StartTime);

            int granted = new EnergyRegenerationRules(5, 20)
                .Regenerate(wallet, clock);

            Assert.That(granted, Is.EqualTo(1));
            Assert.That(wallet.CurrentEnergy, Is.EqualTo(20));
        }

        [Test]
        public void DungeonEntry_InsufficientEnergy_IsRejected()
        {
            Harness harness = CreateHarness(currentEnergy: 0);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => harness.Service.Enter(Request("energy_low")));

            Assert.That(exception.Message, Does.Contain("Energia insuficiente"));
            Assert.That(harness.Service.Energy.CurrentEnergy, Is.Zero);
        }

        [Test]
        public void DungeonEntry_FirstRequest_ConsumesEnergyOnce()
        {
            Harness harness = CreateHarness();
            int before = harness.Service.Energy.CurrentEnergy;

            harness.Service.Enter(Request("consume_once"));

            Assert.That(
                harness.Service.Energy.CurrentEnergy,
                Is.EqualTo(before - 10));
        }

        [Test]
        public void DungeonEntry_RepeatedRequest_ReturnsSameRunWithoutSecondConsumption()
        {
            Harness harness = CreateHarness();
            DungeonEntryRequest request = Request("same_request");
            DungeonRun first = harness.Service.Enter(request);
            int afterFirst = harness.Service.Energy.CurrentEnergy;

            DungeonRun second = harness.Service.Enter(request);

            Assert.That(second, Is.SameAs(first));
            Assert.That(harness.Service.Energy.CurrentEnergy, Is.EqualTo(afterFirst));
        }

        [Test]
        public void DungeonEntry_ReusedRequestWithDifferentPayload_IsRejected()
        {
            Harness harness = CreateHarness();
            DungeonEntryRequest original = Request("payload_collision");
            harness.Service.Enter(original);
            int energyAfterOriginal = harness.Service.Energy.CurrentEnergy;
            var reorderedTeam = new[]
            {
                "dev_hero_paladin",
                "dev_hero_mage",
                "dev_hero_archer"
            };
            var reordered = new DungeonEntryRequest(
                original.RequestId,
                original.DungeonId,
                original.DifficultyId,
                reorderedTeam,
                original.PlayerLevel);
            var differentLevel = new DungeonEntryRequest(
                original.RequestId,
                original.DungeonId,
                original.DifficultyId,
                DungeonService.StandardTeamIds,
                original.PlayerLevel + 1);

            Assert.Throws<InvalidOperationException>(
                () => harness.Service.Enter(reordered));
            Assert.Throws<InvalidOperationException>(
                () => harness.Service.Enter(differentLevel));
            Assert.That(
                harness.Service.Energy.CurrentEnergy,
                Is.EqualTo(energyAfterOriginal));
        }

        [Test]
        public void DungeonEntry_MissingDifficulty_IsRejected()
        {
            Harness harness = CreateHarness();
            var request = new DungeonEntryRequest(
                "missing_difficulty",
                "dungeon_ore_mine",
                "not_found",
                DungeonService.StandardTeamIds,
                1);

            Assert.Throws<KeyNotFoundException>(() => harness.Service.Enter(request));
        }

        [Test]
        public void DungeonEntry_LockedContent_IsRejected()
        {
            Harness harness = CreateHarness(unlocked: false);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => harness.Service.Enter(Request("locked")));

            Assert.That(exception.Message, Does.Contain("bloqueada"));
        }

        [Test]
        public void DungeonBattle_WeakEncounter_EndsInVictoryAndGrantsRewards()
        {
            Harness harness = CreateHarness();
            DungeonRun run = harness.Service.Enter(Request("victory"));
            BattleResult battle = harness.Service.BeginBattle(run.RunId);

            DungeonRunResult result =
                harness.Service.CompleteBattle(run.RunId, battle);

            Assert.That(result.Victory, Is.True);
            Assert.That(run.State, Is.EqualTo(DungeonRunState.RewardsGranted));
            Assert.That(result.Rewards, Is.Not.Empty);
        }

        [Test]
        public void DungeonBattle_OverwhelmingEncounter_EndsInDefeatWithoutRewards()
        {
            Harness harness = CreateHarness(
                dungeonCatalog: BuildSingleDungeon(
                    enemyMultiplier: 1_000_000,
                    dailyLimit: null));
            DungeonRun run = harness.Service.Enter(
                Request("defeat", "dungeon_test", "test"));
            BattleResult battle = harness.Service.BeginBattle(run.RunId);

            DungeonRunResult result =
                harness.Service.CompleteBattle(run.RunId, battle);

            Assert.That(result.Victory, Is.False);
            Assert.That(run.State, Is.EqualTo(DungeonRunState.Lost));
            Assert.That(result.Rewards, Is.Empty);
            Assert.That(result.GoldGranted, Is.Zero);
        }

        [Test]
        public void DungeonReward_FirstClear_IsGrantedOnlyOnce()
        {
            Harness harness = CreateHarness();
            DungeonRunResult first = ExecuteVictory(harness, "first_clear_1");
            DungeonRunResult second = ExecuteVictory(harness, "first_clear_2");

            Assert.That(first.FirstClear, Is.True);
            Assert.That(second.FirstClear, Is.False);
            Assert.That(
                harness.Service.Progress.FirstClears,
                Has.Count.EqualTo(1));
        }

        [Test]
        public void DungeonReward_CompleteSameRunTwice_DoesNotDuplicateInventory()
        {
            Harness harness = CreateHarness();
            DungeonRun run = harness.Service.Enter(Request("same_completion"));
            BattleResult battle = harness.Service.BeginBattle(run.RunId);
            DungeonRunResult first =
                harness.Service.CompleteBattle(run.RunId, battle);
            long afterFirst = MaterialQuantity(harness.Inventory);

            DungeonRunResult second =
                harness.Service.CompleteBattle(run.RunId, battle);

            Assert.That(second, Is.SameAs(first));
            Assert.That(MaterialQuantity(harness.Inventory), Is.EqualTo(afterFirst));
        }

        [Test]
        public void DungeonReward_ChanceZero_NeverGrants()
        {
            var resolver = new DungeonRewardResolver();
            var table = new DungeonRewardTable(new[]
            {
                new DungeonRewardTableEntry(
                    "material_iron_ore_t1", 1, 1, 0, false, false)
            });

            DungeonResolvedRewards result = resolver.Resolve(table, 10, false);

            Assert.That(result.ItemRewards, Is.Empty);
        }

        [Test]
        public void DungeonReward_ChanceTenThousand_AlwaysGrants()
        {
            var resolver = new DungeonRewardResolver();
            var table = new DungeonRewardTable(new[]
            {
                new DungeonRewardTableEntry(
                    "material_iron_ore_t1", 1, 1, 10000, false, false)
            });

            DungeonResolvedRewards result = resolver.Resolve(table, 10, false);

            Assert.That(result.ItemRewards, Has.Count.EqualTo(1));
        }

        [Test]
        public void DungeonReward_QuantityRoll_StaysWithinInclusiveBounds()
        {
            var resolver = new DungeonRewardResolver();
            var table = new DungeonRewardTable(new[]
            {
                new DungeonRewardTableEntry(
                    "material_iron_ore_t1", 3, 7, 10000, true, false)
            });

            for (long seed = 1; seed <= 50; seed++)
            {
                long quantity = resolver.Resolve(table, seed, false)
                    .ItemRewards[0].Quantity;
                Assert.That(quantity, Is.InRange(3L, 7L));
            }
        }

        [Test]
        public void DungeonReward_Victory_AddsMaterialToInventory()
        {
            Harness harness = CreateHarness();
            long before = MaterialQuantity(harness.Inventory);

            ExecuteVictory(harness, "inventory_reward");

            Assert.That(MaterialQuantity(harness.Inventory), Is.GreaterThan(before));
        }

        [Test]
        public void DungeonEntry_DailyLimitReached_IsRejected()
        {
            Harness harness = CreateHarness(
                dungeonCatalog: BuildSingleDungeon(5000, 1));
            DungeonRun first = harness.Service.Enter(
                Request("daily_1", "dungeon_test", "test"));
            BattleResult battle = harness.Service.BeginBattle(first.RunId);
            harness.Service.CompleteBattle(first.RunId, battle);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () => harness.Service.Enter(
                        Request("daily_2", "dungeon_test", "test")));

            Assert.That(exception.Message, Does.Contain("Tentativas diárias"));
        }

        [Test]
        public void DungeonCancellation_BeforeBattle_RefundsEnergy()
        {
            Harness harness = CreateHarness();
            int before = harness.Service.Energy.CurrentEnergy;
            DungeonRun run = harness.Service.Enter(Request("cancel_before"));

            harness.Service.Cancel(run.RunId);

            Assert.That(run.State, Is.EqualTo(DungeonRunState.Cancelled));
            Assert.That(harness.Service.Energy.CurrentEnergy, Is.EqualTo(before));
        }

        [Test]
        public void DungeonCancellation_AfterBattleStart_IsRejectedWithoutRefund()
        {
            Harness harness = CreateHarness();
            DungeonRun run = harness.Service.Enter(Request("cancel_after"));
            harness.Service.BeginBattle(run.RunId);
            int energy = harness.Service.Energy.CurrentEnergy;

            Assert.Throws<InvalidOperationException>(
                () => harness.Service.Cancel(run.RunId));
            Assert.That(harness.Service.Energy.CurrentEnergy, Is.EqualTo(energy));
            Assert.That(run.State, Is.EqualTo(DungeonRunState.InBattle));
        }

        [Test]
        public void DungeonFailure_RefundableTechnicalFailureAfterStart_RefundsEnergy()
        {
            Harness harness = CreateHarness();
            int before = harness.Service.Energy.CurrentEnergy;
            DungeonRun run = harness.Service.Enter(Request("technical_failure"));
            harness.Service.BeginBattle(run.RunId);

            harness.Service.Fail(
                run.RunId,
                DungeonFailureClassification.TechnicalRefundable);

            Assert.That(run.State, Is.EqualTo(DungeonRunState.Failed));
            Assert.That(harness.Service.Energy.CurrentEnergy, Is.EqualTo(before));
        }

        [Test]
        public void DungeonFailure_CompletedLoss_RemainsTerminalWithoutRefund()
        {
            Harness harness = CreateHarness(
                dungeonCatalog: BuildSingleDungeon(
                    enemyMultiplier: 1_000_000,
                    dailyLimit: null));
            DungeonRun run = harness.Service.Enter(
                Request("terminal_loss", "dungeon_test", "test"));
            BattleResult battle = harness.Service.BeginBattle(run.RunId);
            DungeonRunResult result =
                harness.Service.CompleteBattle(run.RunId, battle);
            int energyAfterLoss = harness.Service.Energy.CurrentEnergy;

            Assert.That(result.Victory, Is.False);
            Assert.Throws<InvalidOperationException>(
                () => harness.Service.Fail(
                    run.RunId,
                    DungeonFailureClassification.TechnicalRefundable));
            Assert.That(run.State, Is.EqualTo(DungeonRunState.Lost));
            Assert.That(
                harness.Service.Energy.CurrentEnergy,
                Is.EqualTo(energyAfterLoss));
            Assert.That(
                harness.Service.Progress.GetDailyAttempts(
                    run.Dungeon.DungeonId,
                    run.CreatedAtUnixMilliseconds),
                Is.EqualTo(1));
        }

        [Test]
        public void DungeonCancellation_UtcBoundary_ReleasesReservedDay()
        {
            long nextDay = checked(
                (StartTime / 86_400_000L + 1) * 86_400_000L);
            var clock = new SequenceGameClock(new[]
            {
                nextDay - 1000,
                nextDay - 500,
                nextDay - 1,
                nextDay
            });
            Harness harness = CreateHarness(clockOverride: clock);
            DungeonRun run = harness.Service.Enter(Request("utc_boundary"));

            Assert.DoesNotThrow(() => harness.Service.Cancel(run.RunId));

            Assert.That(
                harness.Service.Progress.GetDailyAttempts(
                    run.Dungeon.DungeonId,
                    nextDay - 1),
                Is.Zero);
            Assert.That(
                harness.Service.Progress.GetDailyAttempts(
                    run.Dungeon.DungeonId,
                    nextDay),
                Is.Zero);
        }

        [Test]
        public void DungeonConfig_SerializedBalance_IsUsedByCatalog()
        {
            DungeonConfigAsset config =
                UnityEngine.ScriptableObject.CreateInstance<DungeonConfigAsset>();
            try
            {
                config.EnsureValid();
                var serialized = new SerializedObject(config);
                SerializedProperty firstDifficulty = serialized
                    .FindProperty("dungeonDefinitions")
                    .GetArrayElementAtIndex(0)
                    .FindPropertyRelative("availableDifficulties")
                    .GetArrayElementAtIndex(0);
                firstDifficulty.FindPropertyRelative("recommendedPower")
                    .longValue = 9876;
                firstDifficulty.FindPropertyRelative("energyCost")
                    .intValue = 37;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                DungeonDifficultyDefinition built = config.BuildCatalog()
                    .GetDungeon("dungeon_ore_mine")
                    .GetDifficulty("mine_apprentice");

                Assert.That(built.RecommendedPower, Is.EqualTo(9876));
                Assert.That(built.EnergyCost, Is.EqualTo(37));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DungeonScene_CanonicalComposition_HasNoValidationErrors()
        {
            Assert.That(DungeonSceneTools.GetValidationErrors(), Is.Empty);
        }

        private static DungeonEntryRequest Request(
            string requestId,
            string dungeonId = "dungeon_ore_mine",
            string difficultyId = "mine_apprentice")
        {
            return new DungeonEntryRequest(
                requestId,
                dungeonId,
                difficultyId,
                DungeonService.StandardTeamIds,
                1);
        }

        private static DungeonRunResult ExecuteVictory(
            Harness harness,
            string requestId)
        {
            DungeonRun run = harness.Service.Enter(Request(requestId));
            BattleResult battle = harness.Service.BeginBattle(run.RunId);
            DungeonRunResult result =
                harness.Service.CompleteBattle(run.RunId, battle);
            Assert.That(result.Victory, Is.True);
            return result;
        }

        private static Harness CreateHarness(
            int currentEnergy = 100,
            bool unlocked = true,
            DungeonCatalog dungeonCatalog = null,
            IGameClock clockOverride = null)
        {
            ContentCatalogAsset catalogAsset =
                AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(
                    "Assets/_Game/Data/Content/ContentCatalog.asset");
            CombatBalanceConfigAsset combatAsset =
                AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(
                    "Assets/_Game/Data/Balance/CombatBalanceConfig.asset");
            DungeonConfigAsset dungeonConfig =
                AssetDatabase.LoadAssetAtPath<DungeonConfigAsset>(
                    DungeonSceneTools.DungeonConfigPath);
            Assert.That(catalogAsset, Is.Not.Null);
            Assert.That(combatAsset, Is.Not.Null);
            Assert.That(dungeonConfig, Is.Not.Null);
            ContentCatalogLookup catalog = catalogAsset.BuildValidatedLookup();
            var inventory = new PlayerInventory();
            inventory.ApplyServerSnapshot(
                new InventorySnapshotData(
                    InventorySnapshotData.CurrentSchemaVersion,
                    "test_player",
                    0,
                    0,
                    StartTime,
                    new List<ItemInstance>()),
                catalog);
            IGameClock clock = clockOverride ?? new DevelopmentGameClock(StartTime);
            var wallet = new LocalGoldEconomyService(0);
            var energy = new EnergyWallet(
                currentEnergy,
                dungeonConfig.MaximumEnergy,
                StartTime);
            var service = new DungeonService(
                "test_player",
                dungeonCatalog ?? dungeonConfig.BuildCatalog(),
                catalog,
                combatAsset.Tuning,
                new InventoryEquipmentModifierProvider(inventory),
                inventory,
                wallet,
                clock,
                energy,
                dungeonConfig.BuildEnergyRules(),
                _ => unlocked,
                seedSource: new FixedSeedSource(1010));
            return new Harness(service, inventory);
        }

        private static DungeonCatalog BuildSingleDungeon(
            int enemyMultiplier,
            int? dailyLimit)
        {
            var reward = new DungeonRewardTable(new[]
            {
                new DungeonRewardTableEntry(
                    "material_iron_ore_t1", 2, 2, 10000, true, false)
            });
            var empty = new DungeonRewardTable(
                Array.Empty<DungeonRewardTableEntry>());
            var encounter = new DungeonEncounterDefinition(
                "test_encounter",
                new StageEnemyFormation(new[]
                {
                    new StageEnemy(
                        "test_enemy",
                        "hero_paladin_001",
                        0,
                        1,
                        enemyMultiplier)
                }),
                "test_scenario");
            var difficulty = new DungeonDifficultyDefinition(
                "test",
                1,
                10,
                encounter,
                reward,
                empty,
                1,
                "stage_01",
                1,
                ItemTier.Tier1);
            return new DungeonCatalog(new[]
            {
                new DungeonDefinition(
                    "dungeon_test",
                    "Teste",
                    "Masmorra de teste.",
                    CraftingProfession.Blacksmith,
                    "stage_01",
                    new[] { difficulty },
                    dailyLimit,
                    new DungeonScheduleMetadata(),
                    "icon_test",
                    new[] { "test" })
            });
        }

        private static long MaterialQuantity(PlayerInventory inventory)
        {
            long result = 0;
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                if (!inventory.Items[i].IsTerminal)
                    result = checked(result + inventory.Items[i].Quantity);
            }
            return result;
        }

        private sealed class FixedSeedSource : IDungeonSeedSource
        {
            private readonly long seed;

            public FixedSeedSource(long seed)
            {
                this.seed = seed;
            }

            public long CreateServerSeed(DungeonEntryRequest request)
            {
                return seed;
            }
        }

        private sealed class SequenceGameClock : IGameClock
        {
            private readonly long[] values;
            private int index;

            public SequenceGameClock(long[] values)
            {
                this.values = values ??
                    throw new ArgumentNullException(nameof(values));
                if (values.Length == 0)
                    throw new ArgumentException("Sequência vazia.", nameof(values));
            }

            public long UtcNowUnixMilliseconds
            {
                get
                {
                    int current = Math.Min(index, values.Length - 1);
                    index++;
                    return values[current];
                }
            }

            public bool IsAuthoritative => false;
            public string Source => "test_sequence";
        }

        private sealed class Harness
        {
            public Harness(DungeonService service, PlayerInventory inventory)
            {
                Service = service;
                Inventory = inventory;
            }

            public DungeonService Service { get; }
            public PlayerInventory Inventory { get; }
        }
    }
}
