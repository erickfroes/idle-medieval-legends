using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IdleMedievalLegends.Domain.Campaign;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Dungeons;
using IdleMedievalLegends.Domain.Heroes;
using IdleMedievalLegends.Domain.Inventory;

namespace IdleMedievalLegends.Application
{
    public interface IDungeonSeedSource
    {
        long CreateServerSeed(DungeonEntryRequest request);
    }

    public sealed class LocalDungeonSeedSource : IDungeonSeedSource
    {
        public long CreateServerSeed(DungeonEntryRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            ulong hash = 14695981039346656037UL;
            string value =
                $"{request.RequestId}:{request.DungeonId}:{request.DifficultyId}";
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 1099511628211UL;
            }
            long result = (long)(hash & long.MaxValue);
            return result == 0 ? 1 : result;
        }
    }

    /// <summary>
    /// Simulação local da fronteira autoritativa de masmorras. Produção deve
    /// executar deduplicação, Energia, batalha e entrega em uma transação de servidor.
    /// </summary>
    public sealed class DungeonService
    {
        private const string CombatRulesVersion = "combat_rules_v1";

        private readonly string playerId;
        private readonly DungeonCatalog dungeons;
        private readonly ContentCatalogLookup contentCatalog;
        private readonly CombatBalanceTuning combatTuning;
        private readonly IHeroEquipmentModifierProvider equipmentProvider;
        private readonly PlayerInventory inventory;
        private readonly IGoldEconomyService goldWallet;
        private readonly IGameClock clock;
        private readonly Func<string, bool> campaignStageUnlocked;
        private readonly IDungeonSeedSource seedSource;
        private readonly DungeonRewardResolver rewardResolver;
        private readonly Dictionary<string, DungeonRun> runsByRequest =
            new Dictionary<string, DungeonRun>(StringComparer.Ordinal);
        private readonly Dictionary<string, DungeonRun> runsById =
            new Dictionary<string, DungeonRun>(StringComparer.Ordinal);
        private readonly Dictionary<string, DungeonRunResult> resultsByRun =
            new Dictionary<string, DungeonRunResult>(StringComparer.Ordinal);

        public DungeonService(
            string playerId,
            DungeonCatalog dungeons,
            ContentCatalogLookup contentCatalog,
            CombatBalanceTuning combatTuning,
            IHeroEquipmentModifierProvider equipmentProvider,
            PlayerInventory inventory,
            IGoldEconomyService goldWallet,
            IGameClock clock,
            EnergyWallet energy,
            EnergyRegenerationRules energyRules,
            Func<string, bool> campaignStageUnlocked,
            DungeonProgress progress = null,
            IDungeonSeedSource seedSource = null,
            DungeonRewardResolver rewardResolver = null)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS)
            throw new PlatformNotSupportedException(
                "DungeonService local existe somente para prototipagem.");
#else
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("playerId é obrigatório.", nameof(playerId));
            this.playerId = playerId;
            this.dungeons = dungeons ?? throw new ArgumentNullException(nameof(dungeons));
            this.contentCatalog = contentCatalog ??
                throw new ArgumentNullException(nameof(contentCatalog));
            this.combatTuning = combatTuning ??
                throw new ArgumentNullException(nameof(combatTuning));
            this.equipmentProvider = equipmentProvider ??
                throw new ArgumentNullException(nameof(equipmentProvider));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.goldWallet = goldWallet ?? throw new ArgumentNullException(nameof(goldWallet));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Energy = energy ?? throw new ArgumentNullException(nameof(energy));
            EnergyRules = energyRules ??
                throw new ArgumentNullException(nameof(energyRules));
            this.campaignStageUnlocked = campaignStageUnlocked ??
                throw new ArgumentNullException(nameof(campaignStageUnlocked));
            Progress = progress ?? new DungeonProgress();
            this.seedSource = seedSource ?? new LocalDungeonSeedSource();
            this.rewardResolver = rewardResolver ?? new DungeonRewardResolver();
#endif
        }

        public DungeonCatalog Dungeons => dungeons;
        public EnergyWallet Energy { get; }
        public EnergyRegenerationRules EnergyRules { get; }
        public DungeonProgress Progress { get; }
        public IGameClock Clock => clock;
        public IReadOnlyCollection<DungeonRun> Runs =>
            new ReadOnlyCollection<DungeonRun>(new List<DungeonRun>(runsById.Values));
        public DungeonRunResult LatestResult { get; private set; }
        public long TeamPower => CalculateTeamPower(StandardTeamIds);

        public static IReadOnlyList<string> StandardTeamIds { get; } =
            new ReadOnlyCollection<string>(new[]
            {
                "dev_hero_paladin",
                "dev_hero_archer",
                "dev_hero_mage"
            });

        public int RegenerateEnergy()
        {
            return EnergyRules.Regenerate(Energy, clock);
        }

        public string GetEntryBlockReason(
            DungeonDefinition dungeon,
            DungeonDifficultyDefinition difficulty,
            int playerLevel)
        {
            if (dungeon == null) throw new ArgumentNullException(nameof(dungeon));
            if (difficulty == null) throw new ArgumentNullException(nameof(difficulty));
            if (!campaignStageUnlocked(dungeon.UnlockStageId) ||
                !campaignStageUnlocked(difficulty.RequiredCampaignStage))
            {
                return "Masmorra bloqueada pela campanha.";
            }
            if (playerLevel < difficulty.MinimumPlayerLevel)
                return "Nível do jogador insuficiente.";
            int attempts = Progress.GetDailyAttempts(
                dungeon.DungeonId,
                clock.UtcNowUnixMilliseconds);
            if (dungeon.DailyAttemptLimit.HasValue &&
                attempts >= dungeon.DailyAttemptLimit.Value)
            {
                return "Tentativas diárias esgotadas.";
            }
            if (Energy.CurrentEnergy < difficulty.EnergyCost)
                return "Energia insuficiente.";
            return string.Empty;
        }

        public DungeonRun Enter(DungeonEntryRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (runsByRequest.TryGetValue(request.RequestId, out DungeonRun existing))
            {
                if (!existing.EntryRequest.HasSamePayload(request))
                {
                    throw new InvalidOperationException(
                        "requestId já foi usado para outro comando.");
                }
                return existing;
            }

            RegenerateEnergy();
            long entryTimestamp = clock.UtcNowUnixMilliseconds;
            DungeonDefinition dungeon = dungeons.GetDungeon(request.DungeonId);
            DungeonDifficultyDefinition difficulty =
                dungeon.GetDifficulty(request.DifficultyId);
            ValidateUnlocked(dungeon, difficulty, request.PlayerLevel);
            ValidateTeam(request.TeamHeroInstanceIds);
            EnsureNoConflictingRun();
            int attempts = Progress.GetDailyAttempts(
                dungeon.DungeonId,
                entryTimestamp);
            if (dungeon.DailyAttemptLimit.HasValue &&
                attempts >= dungeon.DailyAttemptLimit.Value)
            {
                throw new InvalidOperationException("Tentativas diárias esgotadas.");
            }
            if (Energy.CurrentEnergy < difficulty.EnergyCost)
                throw new InvalidOperationException("Energia insuficiente.");

            long teamPower = CalculateTeamPower(request.TeamHeroInstanceIds);
            if (teamPower < difficulty.RecommendedPower &&
                difficulty.BlockBelowRecommendedPower)
            {
                throw new InvalidOperationException(
                    "Poder da equipe abaixo do mínimo obrigatório.");
            }

            long seed = seedSource.CreateServerSeed(request);
            string runId = $"dungeon_{StableToken(request.RequestId)}";
            if (runsById.ContainsKey(runId))
                throw new InvalidOperationException("Colisão de runId.");
            var run = new DungeonRun(
                runId,
                request,
                dungeon,
                difficulty,
                seed,
                entryTimestamp,
                teamPower < difficulty.RecommendedPower);
            Energy.Consume(difficulty.EnergyCost);
            Progress.ReserveAttempt(dungeon.DungeonId, entryTimestamp);
            run.ReserveEnergy();
            runsByRequest.Add(request.RequestId, run);
            runsById.Add(run.RunId, run);
            return run;
        }

        public BattleResult BeginBattle(string runId)
        {
            DungeonRun run = GetRun(runId);
            if (run.State == DungeonRunState.InBattle)
                return run.SimulatedBattle;
            if (run.State != DungeonRunState.EnergyReserved)
                throw new InvalidOperationException("Run não pode iniciar batalha.");
            BattleRequest request = BuildBattleRequest(run);
            BattleResult battle = new BattleSimulator().Simulate(request);
            run.BeginBattle(request, battle);
            return battle;
        }

        public DungeonRunResult CompleteBattle(string runId, BattleResult battle)
        {
            DungeonRun run = GetRun(runId);
            if (resultsByRun.TryGetValue(runId, out DungeonRunResult cached))
                return cached;
            if (run.State != DungeonRunState.InBattle)
                throw new InvalidOperationException("Run não está em batalha.");
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (!ReferenceEquals(run.SimulatedBattle, battle) &&
                (!string.Equals(run.SimulatedBattle.BattleId, battle.BattleId,
                     StringComparison.Ordinal) ||
                 !string.Equals(run.SimulatedBattle.DeterministicHash,
                     battle.DeterministicHash,
                     StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Resultado não corresponde à batalha simulada da run.");
            }

            bool victory = battle.Outcome == BattleOutcome.AttackerVictory &&
                           battle.WinningTeam == BattleSide.Attacker;
            run.MarkBattleOutcome(victory);
            bool firstClear = victory && !Progress.HasFirstClear(
                run.Dungeon.DungeonId,
                run.Difficulty.DifficultyId);
            var grants = new List<DungeonRewardGrant>();
            long gold = 0;
            if (victory)
            {
                DungeonResolvedRewards baseRewards = rewardResolver.Resolve(
                    run.Difficulty.RewardTable,
                    run.ServerSeed,
                    firstClear);
                grants.AddRange(baseRewards.ItemRewards);
                gold = baseRewards.Gold;
                if (firstClear)
                {
                    DungeonResolvedRewards firstRewards = rewardResolver.Resolve(
                        run.Difficulty.FirstClearReward,
                        DeriveSeed(run.ServerSeed),
                        true,
                        run.Difficulty.RewardTable.Entries.Count);
                    grants.AddRange(firstRewards.ItemRewards);
                    gold = checked(gold + firstRewards.Gold);
                }

                GrantRewards(run, grants, gold);
                if (firstClear)
                {
                    Progress.MarkFirstClear(
                        run.Dungeon.DungeonId,
                        run.Difficulty.DifficultyId);
                }
                run.MarkRewardsGranted();
            }

            var result = new DungeonRunResult(
                run,
                battle,
                firstClear,
                new ReadOnlyCollection<DungeonRewardGrant>(grants),
                gold);
            resultsByRun.Add(runId, result);
            LatestResult = result;
            return result;
        }

        public DungeonRun Cancel(string runId)
        {
            DungeonRun run = GetRun(runId);
            if (run.State == DungeonRunState.Cancelled)
                return run;
            run.Cancel();
            RefundEntry(run);
            return run;
        }

        public DungeonRun Fail(
            string runId,
            DungeonFailureClassification classification)
        {
            DungeonRun run = GetRun(runId);
            bool refundable =
                classification == DungeonFailureClassification.TechnicalRefundable;
            run.Fail(classification);
            if (refundable)
                RefundEntry(run);
            return run;
        }

        public DungeonRun GetRun(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId) ||
                !runsById.TryGetValue(runId, out DungeonRun run))
            {
                throw new KeyNotFoundException($"Run inexistente: {runId}.");
            }
            return run;
        }

        private BattleRequest BuildBattleRequest(DungeonRun run)
        {
            var configuration = new BattleConfiguration(
                maximumActions: 300,
                basicAttackMultiplier: 2.25d,
                defaultCriticalChance: 0.12d,
                defaultAccuracy: 0.97d,
                defaultEvasion: 0.03d,
                targetSelectionMode: TargetSelectionMode.Random);
            var attackers = new List<BattleUnit>();
            for (int i = 0; i < run.EntryRequest.TeamHeroInstanceIds.Count; i++)
            {
                string instanceId = run.EntryRequest.TeamHeroInstanceIds[i];
                HeroDefinition definition = contentCatalog.GetHero(
                    DefinitionIdForInstance(instanceId));
                HeroInstance hero = CreateHero(instanceId, definition, playerId);
                attackers.Add(BattleUnitFactory.FromHero(
                    hero,
                    definition,
                    equipmentProvider,
                    combatTuning,
                    configuration,
                    BattleSide.Attacker,
                    i));
            }

            IReadOnlyList<StageEnemy> formation =
                run.Difficulty.EnemyFormation.Enemies;
            var defenders = new List<BattleUnit>(formation.Count);
            for (int i = 0; i < formation.Count; i++)
            {
                StageEnemy enemy = formation[i];
                HeroDefinition definition =
                    contentCatalog.GetHero(enemy.HeroDefinitionId);
                HeroInstance hero = CreateHero(enemy.EnemyId, definition, "dungeon_enemy");
                BattleUnit unit = BattleUnitFactory.FromHero(
                    hero,
                    definition,
                    EmptyHeroEquipmentModifierProvider.Instance,
                    combatTuning,
                    configuration,
                    BattleSide.Defender,
                    enemy.Slot);
                defenders.Add(ScaleEnemy(unit, enemy.StatMultiplierBasisPoints));
            }

            return new BattleRequest(
                new BattleTeam(BattleSide.Attacker, attackers),
                new BattleTeam(BattleSide.Defender, defenders),
                run.ServerSeed,
                configuration,
                CombatRulesVersion,
                $"dungeon:{run.RunId}");
        }

        private void GrantRewards(
            DungeonRun run,
            IReadOnlyList<DungeonRewardGrant> rewards,
            long gold)
        {
            long timestamp = Math.Max(
                clock.UtcNowUnixMilliseconds,
                inventory.CaptureSnapshotForCache().GeneratedAtUnixMilliseconds);
            if (gold > 0)
            {
                goldWallet.Credit(
                    gold,
                    "dungeon_reward",
                    $"dungeon_gold:{run.RunId}",
                    timestamp,
                    "local_dungeon_simulation");
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                DungeonRewardGrant reward = rewards[i];
                MaterialDefinition definition =
                    contentCatalog.GetMaterial(reward.ItemDefinitionId);
                long remaining = reward.Quantity;
                int stackIndex = 0;
                while (remaining > 0)
                {
                    long quantity = Math.Min(remaining, definition.MaxStackSize);
                    string instanceId =
                        $"dungeon_{StableToken(run.RunId)}_{reward.EntryIndex:D3}_{stackIndex:D3}";
                    if (!inventory.TryGetItem(instanceId, out ItemInstance existing))
                    {
                        var item = new ItemInstance(
                            instanceId,
                            definition.DefinitionId,
                            playerId,
                            InventoryItemKind.Material,
                            definition.Rarity.ToLegacyRarity(),
                            definition.Tier.ToLegacyTier(),
                            quantity,
                            true,
                            InventoryItemState.Owned,
                            ItemBinding.Unbound,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            run.ServerSeed,
                            null,
                            0,
                            -1,
                            -1,
                            false,
                            0,
                            timestamp,
                            timestamp,
                            new ItemProvenanceData(
                                "local_dungeon_simulation",
                                "dungeon_reward",
                                run.RunId));
                        inventory.AddAuthorizedItem(item, definition, timestamp);
                    }
                    else if (!string.Equals(
                        existing.DefinitionId,
                        definition.DefinitionId,
                        StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Colisão de recompensa da run {run.RunId}.");
                    }
                    remaining -= quantity;
                    stackIndex++;
                }
            }
        }

        private void RefundEntry(DungeonRun run)
        {
            Energy.Refund(run.Difficulty.EnergyCost);
            Progress.ReleaseAttempt(
                run.Dungeon.DungeonId,
                run.CreatedAtUnixMilliseconds);
        }

        private void ValidateUnlocked(
            DungeonDefinition dungeon,
            DungeonDifficultyDefinition difficulty,
            int playerLevel)
        {
            if (!campaignStageUnlocked(dungeon.UnlockStageId) ||
                !campaignStageUnlocked(difficulty.RequiredCampaignStage))
            {
                throw new InvalidOperationException("Masmorra bloqueada pela campanha.");
            }
            if (playerLevel < difficulty.MinimumPlayerLevel)
                throw new InvalidOperationException("Nível do jogador insuficiente.");
        }

        private void ValidateTeam(IReadOnlyList<string> teamIds)
        {
            for (int i = 0; i < teamIds.Count; i++)
                DefinitionIdForInstance(teamIds[i]);
        }

        private void EnsureNoConflictingRun()
        {
            foreach (DungeonRun run in runsById.Values)
            {
                if (run.State == DungeonRunState.Created ||
                    run.State == DungeonRunState.EnergyReserved ||
                    run.State == DungeonRunState.InBattle)
                {
                    throw new InvalidOperationException(
                        "Já existe uma run de masmorra conflitante.");
                }
            }
        }

        private long CalculateTeamPower(IReadOnlyList<string> teamIds)
        {
            long result = 0;
            for (int i = 0; i < teamIds.Count; i++)
            {
                HeroDefinition definition = contentCatalog.GetHero(
                    DefinitionIdForInstance(teamIds[i]));
                HeroInstance hero = CreateHero(teamIds[i], definition, playerId);
                result = checked(result + HeroPowerCalculator.CalculateBreakdown(
                    hero,
                    definition,
                    equipmentProvider,
                    combatTuning).HeroPower.Value);
            }
            return result;
        }

        private HeroInstance CreateHero(
            string instanceId,
            HeroDefinition definition,
            string owner)
        {
            return HeroInstance.Restore(
                instanceId,
                definition.DefinitionId,
                owner,
                1,
                0,
                definition.InitialRarity,
                0,
                0,
                null,
                true,
                0,
                0,
                null,
                0,
                combatTuning);
        }

        private static string DefinitionIdForInstance(string instanceId)
        {
            switch (instanceId)
            {
                case "dev_hero_paladin": return "hero_paladin_001";
                case "dev_hero_archer": return "hero_archer_001";
                case "dev_hero_mage": return "hero_mage_001";
                default:
                    throw new InvalidOperationException(
                        $"Herói de equipe inválido no protótipo: {instanceId}.");
            }
        }

        private static BattleUnit ScaleEnemy(BattleUnit unit, int basisPoints)
        {
            long health = Scale(unit.MaximumHealth, basisPoints, 1);
            return new BattleUnit(
                unit.UnitId,
                unit.DefinitionId,
                unit.Team,
                unit.Slot,
                unit.Level,
                health,
                health,
                Scale(unit.Attack, basisPoints, 1),
                Scale(unit.Defense, basisPoints, 0),
                unit.Speed,
                unit.CriticalChance,
                unit.CriticalMultiplier,
                unit.Accuracy,
                unit.Evasion,
                unit.ActionGauge,
                unit.Tags);
        }

        private static long Scale(long value, int basisPoints, long minimum)
        {
            return Math.Max(minimum, checked(value * basisPoints / 10000L));
        }

        private static long DeriveSeed(long seed)
        {
            return seed == long.MaxValue ? seed - 1 : seed + 1;
        }

        private static string StableToken(string value)
        {
            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 1099511628211UL;
            }
            return hash.ToString("X16");
        }
    }
}
