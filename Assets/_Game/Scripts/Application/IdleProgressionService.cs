using System;
using System.Collections.Generic;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Campaign;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Economy;
using IdleMedievalLegends.Domain.Heroes;
using IdleMedievalLegends.Domain.Inventory;

namespace IdleMedievalLegends.Application
{
    public sealed class CampaignBattleResult
    {
        public CampaignBattleResult(
            CampaignStageDefinition stage,
            BattleResult battle,
            bool firstClear,
            bool advanced,
            StageRewardDefinition grantedReward)
        {
            Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            Battle = battle ?? throw new ArgumentNullException(nameof(battle));
            FirstClear = firstClear;
            Advanced = advanced;
            GrantedReward = grantedReward;
        }

        public CampaignStageDefinition Stage { get; }
        public BattleResult Battle { get; }
        public bool Victory =>
            Battle.Outcome == BattleOutcome.AttackerVictory &&
            Battle.WinningTeam == BattleSide.Attacker;
        public bool FirstClear { get; }
        public bool Advanced { get; }
        public StageRewardDefinition GrantedReward { get; }
    }

    /// <summary>
    /// Fronteira local para campanha, batalha e recompensas. Produção deve
    /// substituir este serviço por comandos idempotentes validados pelo servidor.
    /// </summary>
    public sealed class IdleProgressionService
    {
        private const string CombatRulesVersion = "combat_rules_v1";

        private readonly string playerId;
        private readonly CampaignDefinition campaign;
        private readonly CampaignConfigAsset config;
        private readonly ContentCatalogLookup catalog;
        private readonly CombatBalanceTuning combatTuning;
        private readonly IHeroEquipmentModifierProvider equipmentProvider;
        private readonly PlayerInventory inventory;
        private readonly IGoldEconomyService goldWallet;
        private readonly IGameClock clock;
        private readonly IdleRewardCalculator rewardCalculator;
        private readonly Action<string> warningSink;
        private readonly Dictionary<string, CampaignBattleResult> battleResults =
            new Dictionary<string, CampaignBattleResult>(StringComparer.Ordinal);
        private readonly Dictionary<string, OfflineRewardReport> reports =
            new Dictionary<string, OfflineRewardReport>(StringComparer.Ordinal);

        private PlayerCampaignProgress progress;

        public IdleProgressionService(
            string playerId,
            CampaignDefinition campaign,
            CampaignConfigAsset config,
            ContentCatalogLookup catalog,
            CombatBalanceTuning combatTuning,
            IHeroEquipmentModifierProvider equipmentProvider,
            PlayerInventory inventory,
            IGoldEconomyService goldWallet,
            IGameClock clock,
            PlayerCampaignProgress progress = null,
            IdleRewardCalculator rewardCalculator = null,
            Action<string> warningSink = null)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS)
            throw new PlatformNotSupportedException(
                "IdleProgressionService local existe somente para prototipagem.");
#else
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("playerId é obrigatório.", nameof(playerId));
            this.playerId = playerId;
            this.campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.combatTuning = combatTuning ?? throw new ArgumentNullException(nameof(combatTuning));
            this.equipmentProvider = equipmentProvider ??
                throw new ArgumentNullException(nameof(equipmentProvider));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.goldWallet = goldWallet ?? throw new ArgumentNullException(nameof(goldWallet));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.rewardCalculator = rewardCalculator ?? new IdleRewardCalculator();
            this.warningSink = warningSink;
            this.progress = progress?.Clone() ??
                PlayerCampaignProgress.CreateNew(campaign, clock.UtcNowUnixMilliseconds);
            this.progress.ValidateAgainst(campaign);
            if (this.progress.PendingOfflineReport != null)
            {
                reports[this.progress.PendingOfflineReport.RequestId] =
                    this.progress.PendingOfflineReport;
            }
            DeliverPendingFirstClearRewards();
#endif
        }

        public CampaignDefinition Campaign => campaign;
        public PlayerCampaignProgress Progress => progress;
        public IGameClock Clock => clock;
        public long GoldBalance => goldWallet.GoldBalance;
        public IReadOnlyList<GoldLedgerEntry> GoldLedger =>
            goldWallet.Ledger;
        public CampaignStageDefinition CurrentStage =>
            campaign.GetStage(progress.CurrentStageId);
        public CampaignStageDefinition HighestClearedStage =>
            string.IsNullOrWhiteSpace(progress.HighestClearedStageId)
                ? null
                : campaign.GetStage(progress.HighestClearedStageId);
        public long TeamPower => CalculateTeamPower();
        public int PlayerOfflineLimitHours => config.BasePlayerOfflineHours;

        public void ApplySnapshot(PlayerCampaignProgress snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            snapshot.ValidateAgainst(campaign);
            if (snapshot.Revision < progress.Revision)
                throw new InvalidOperationException("Snapshot de campanha possui revisão antiga.");
            progress = snapshot.Clone();
            reports.Clear();
            if (progress.PendingOfflineReport != null)
                reports.Add(progress.PendingOfflineReport.RequestId, progress.PendingOfflineReport);
            DeliverPendingFirstClearRewards();
        }

        public PlayerCampaignProgress CaptureSnapshot()
        {
            return progress.Clone();
        }

        public CampaignBattleResult BattleStage(
            string stageId,
            string requestId,
            long seed)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("requestId é obrigatório.", nameof(requestId));
            if (seed <= 0) throw new ArgumentOutOfRangeException(nameof(seed));
            if (battleResults.TryGetValue(requestId, out CampaignBattleResult cached))
                return cached;
            if (progress.HasProcessedBattle(requestId))
                throw new InvalidOperationException("Batalha já processada por este requestId.");

            CampaignStageDefinition stage = campaign.GetStage(stageId);
            EnsureStageUnlocked(stage);
            BattleRequest request = StartStageBattle(stageId, requestId, seed);
            BattleResult battle = new BattleSimulator().Simulate(request);
            return CompleteStageBattle(stageId, requestId, battle);
        }

        public BattleRequest StartStageBattle(
            string stageId,
            string requestId,
            long seed)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("requestId é obrigatório.", nameof(requestId));
            if (seed <= 0) throw new ArgumentOutOfRangeException(nameof(seed));
            CampaignStageDefinition stage = campaign.GetStage(stageId);
            EnsureStageUnlocked(stage);
            return BuildBattleRequest(stage, requestId, seed);
        }

        public CampaignBattleResult CompleteStageBattle(
            string stageId,
            string requestId,
            BattleResult battle)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("requestId é obrigatório.", nameof(requestId));
            if (battleResults.TryGetValue(requestId, out CampaignBattleResult cached))
                return cached;
            if (progress.HasProcessedBattle(requestId))
                throw new InvalidOperationException("Batalha já processada por este requestId.");
            CampaignStageDefinition stage = campaign.GetStage(stageId);
            EnsureStageUnlocked(stage);
            string expectedBattleId = $"campaign:{stage.StageId}:{requestId}";
            if (!string.Equals(battle.BattleId, expectedBattleId, StringComparison.Ordinal) ||
                !string.Equals(battle.RulesVersion, CombatRulesVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Resultado de batalha não corresponde ao estágio/request/regras.");
            }
            bool victory = battle.Outcome == BattleOutcome.AttackerVictory &&
                           battle.WinningTeam == BattleSide.Attacker;
            bool firstClear = victory && !progress.HasCleared(stage.StageId);
            bool highestAdvanced = victory &&
                (HighestClearedStage == null ||
                 stage.Sequence > HighestClearedStage.Sequence);
            CampaignStageDefinition next = victory ? campaign.GetNextStage(stage.StageId) : null;
            bool clearsCurrentStage = victory &&
                stage.Sequence == CurrentStage.Sequence;
            string nextCurrentStageId = clearsCurrentStage && next != null
                ? next.StageId
                : progress.CurrentStageId;
            if (clearsCurrentStage && next == null)
                nextCurrentStageId = stage.StageId;
            string nextHighestStageId = highestAdvanced
                ? stage.StageId
                : progress.HighestClearedStageId;

            progress.RecordBattle(
                stage.StageId,
                nextCurrentStageId,
                nextHighestStageId,
                requestId,
                victory,
                firstClear);

            StageRewardDefinition grantedReward = null;
            if (victory)
            {
                grantedReward = firstClear ? stage.FirstClearRewards : stage.RepeatRewards;
                string rewardRequestId = firstClear
                    ? $"first_clear:{stage.StageId}"
                    : $"repeat:{requestId}";
                ApplyReward(
                    grantedReward,
                    rewardRequestId,
                    firstClear ? "campaign_first_clear" : "campaign_repeat",
                    clock.UtcNowUnixMilliseconds);
                if (firstClear)
                    progress.MarkFirstClearDelivered(stage.StageId);
            }

            var result = new CampaignBattleResult(
                stage,
                battle,
                firstClear,
                victory && next != null &&
                string.Equals(progress.CurrentStageId, next.StageId, StringComparison.Ordinal),
                grantedReward);
            battleResults.Add(requestId, result);
            return result;
        }

        public OfflineRewardReport GenerateOfflineReport(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("requestId é obrigatório.", nameof(requestId));
            if (progress.PendingOfflineReport != null)
                return progress.PendingOfflineReport;
            if (reports.TryGetValue(requestId, out OfflineRewardReport existing))
                return existing;
            if (progress.HasCollectedOffline(requestId))
                throw new InvalidOperationException("requestId de coleta já utilizado.");

            CampaignStageDefinition stage = HighestClearedStage ?? CurrentStage;
            long now = clock.UtcNowUnixMilliseconds;
            long start = progress.LastClaimedServerTime;
            var session = new OfflineSession(
                start,
                now,
                stage.StageId,
                progress.Revision,
                campaign.RulesVersion,
                requestId);
            IdleProductionProfile profile = BuildProductionProfile(stage);
            TimeValidationResult validation = rewardCalculator.ValidateTime(
                start,
                now,
                HoursToMilliseconds(config.MaximumAbsoluteClockJumpHours),
                HoursToMilliseconds(config.SafeClockJumpHours));
            if (validation.HasWarning)
                warningSink?.Invoke(validation.Warning);
            if (validation.Code == TimeValidationCode.MissingTimestamp)
                progress.InitializeTimestamps(now);
            OfflineRewardReport report =
                rewardCalculator.Calculate(session, profile, validation);
            reports.Add(requestId, report);
            progress.StorePendingReport(report);
            return report;
        }

        public OfflineRewardReport CollectOfflineReport(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("requestId é obrigatório.", nameof(requestId));
            if (progress.HasCollectedOffline(requestId))
            {
                if (reports.TryGetValue(requestId, out OfflineRewardReport prior))
                    return prior;
                throw new InvalidOperationException("Relatório offline já foi coletado.");
            }
            OfflineRewardReport report = progress.PendingOfflineReport;
            if (report == null ||
                !string.Equals(report.RequestId, requestId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Relatório não existe ou não corresponde ao requestId.");
            }

            long now = clock.UtcNowUnixMilliseconds;
            goldWallet.Credit(
                report.Gold,
                "offline_reward",
                $"offline_gold:{report.RequestId}",
                now,
                "local_idle_simulation");
            ApplyMaterials(
                report.Materials,
                $"offline_materials:{report.RequestId}",
                "offline_reward",
                now);
            progress.AddAccountExperience(report.AccountExperience);
            report.MarkCollected();
            progress.MarkOfflineCollected(requestId, report.EndUnixMilliseconds);
            reports[requestId] = report;
            return report;
        }

        public IdleProductionProfile BuildCurrentProductionProfile()
        {
            return BuildProductionProfile(HighestClearedStage ?? CurrentStage);
        }

        private IdleProductionProfile BuildProductionProfile(
            CampaignStageDefinition stage)
        {
            bool eligible = HighestClearedStage != null && stage.IdleUnlocked;
            return new IdleProductionProfile(
                stage.StageId,
                eligible ? stage.BaseGoldPerMinute : 0,
                eligible ? stage.RewardMaterialTable : Array.Empty<CampaignMaterialReward>(),
                eligible ? stage.Sequence / 4 : 0,
                Math.Max(
                    0,
                    clock.UtcNowUnixMilliseconds - progress.LastClaimedServerTime),
                HoursToMilliseconds(config.BasePlayerOfflineHours),
                HoursToMilliseconds(stage.MaximumOfflineHours),
                new[] { new IdleRewardMultiplier("base", 10000) });
        }

        private BattleRequest BuildBattleRequest(
            CampaignStageDefinition stage,
            string requestId,
            long seed)
        {
            var configuration = new BattleConfiguration(
                maximumActions: 300,
                basicAttackMultiplier: 2.25d,
                defaultCriticalChance: 0.12d,
                defaultAccuracy: 0.97d,
                defaultEvasion: 0.03d,
                targetSelectionMode: TargetSelectionMode.Random);
            string[] heroIds =
            {
                "hero_paladin_001",
                "hero_archer_001",
                "hero_mage_001"
            };
            string[] instanceIds =
            {
                "dev_hero_paladin",
                "dev_hero_archer",
                "dev_hero_mage"
            };
            var attackers = new List<BattleUnit>(3);
            for (int i = 0; i < heroIds.Length; i++)
            {
                HeroDefinition definition = catalog.GetHero(heroIds[i]);
                HeroInstance hero = CreateHero(
                    instanceIds[i],
                    definition,
                    1,
                    "campaign_player");
                attackers.Add(BattleUnitFactory.FromHero(
                    hero,
                    definition,
                    equipmentProvider,
                    combatTuning,
                    configuration,
                    BattleSide.Attacker,
                    i));
            }

            var defenders = new List<BattleUnit>(stage.EnemyFormation.Enemies.Count);
            for (int i = 0; i < stage.EnemyFormation.Enemies.Count; i++)
            {
                StageEnemy enemy = stage.EnemyFormation.Enemies[i];
                HeroDefinition definition = catalog.GetHero(enemy.HeroDefinitionId);
                HeroInstance hero = CreateHero(
                    enemy.EnemyId,
                    definition,
                    enemy.Level,
                    "campaign_enemy");
                BattleUnit baseUnit = BattleUnitFactory.FromHero(
                    hero,
                    definition,
                    EmptyHeroEquipmentModifierProvider.Instance,
                    combatTuning,
                    configuration,
                    BattleSide.Defender,
                    enemy.Slot);
                defenders.Add(ScaleEnemy(baseUnit, enemy.StatMultiplierBasisPoints));
            }

            return new BattleRequest(
                new BattleTeam(BattleSide.Attacker, attackers),
                new BattleTeam(BattleSide.Defender, defenders),
                seed,
                configuration,
                CombatRulesVersion,
                $"campaign:{stage.StageId}:{requestId}");
        }

        private HeroInstance CreateHero(
            string instanceId,
            HeroDefinition definition,
            int level,
            string owner)
        {
            return HeroInstance.Restore(
                instanceId,
                definition.DefinitionId,
                owner,
                level,
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

        private static BattleUnit ScaleEnemy(BattleUnit unit, int basisPoints)
        {
            long health = Scale(unit.MaximumHealth, basisPoints, minimum: 1);
            return new BattleUnit(
                unit.UnitId,
                unit.DefinitionId,
                unit.Team,
                unit.Slot,
                unit.Level,
                health,
                health,
                Scale(unit.Attack, basisPoints, minimum: 1),
                Scale(unit.Defense, basisPoints, minimum: 0),
                unit.Speed,
                unit.CriticalChance,
                unit.CriticalMultiplier,
                unit.Accuracy,
                unit.Evasion,
                unit.ActionGauge,
                unit.Tags);
        }

        private long CalculateTeamPower()
        {
            string[] heroIds =
            {
                "hero_paladin_001",
                "hero_archer_001",
                "hero_mage_001"
            };
            string[] instanceIds =
            {
                "dev_hero_paladin",
                "dev_hero_archer",
                "dev_hero_mage"
            };
            long power = 0;
            for (int i = 0; i < heroIds.Length; i++)
            {
                HeroDefinition definition = catalog.GetHero(heroIds[i]);
                HeroInstance hero = CreateHero(
                    instanceIds[i],
                    definition,
                    1,
                    playerId);
                power = checked(power + HeroPowerCalculator.CalculateBreakdown(
                    hero,
                    definition,
                    equipmentProvider,
                    combatTuning).HeroPower.Value);
            }
            return power;
        }

        private void EnsureStageUnlocked(CampaignStageDefinition stage)
        {
            CampaignStageDefinition current = CurrentStage;
            if (stage.Sequence > current.Sequence)
                throw new InvalidOperationException("Estágio ainda não foi desbloqueado.");
        }

        private void DeliverPendingFirstClearRewards()
        {
            string[] pending = new string[progress.PendingFirstClearRewards.Count];
            for (int i = 0; i < pending.Length; i++)
                pending[i] = progress.PendingFirstClearRewards[i];
            for (int i = 0; i < pending.Length; i++)
            {
                CampaignStageDefinition stage = campaign.GetStage(pending[i]);
                ApplyReward(
                    stage.FirstClearRewards,
                    $"first_clear:{stage.StageId}",
                    "campaign_first_clear_recovery",
                    clock.UtcNowUnixMilliseconds);
                progress.MarkFirstClearDelivered(stage.StageId);
            }
        }

        private void ApplyReward(
            StageRewardDefinition reward,
            string requestId,
            string reason,
            long timestamp)
        {
            goldWallet.Credit(
                reward.Gold,
                reason,
                $"gold:{requestId}",
                timestamp,
                "local_campaign_simulation");
            ApplyMaterials(reward.Materials, $"materials:{requestId}", reason, timestamp);
            progress.AddAccountExperience(reward.AccountExperience);
        }

        private void ApplyMaterials(
            IReadOnlyList<CampaignMaterialReward> materials,
            string requestId,
            string reason,
            long timestamp)
        {
            long safeTimestamp = Math.Max(
                timestamp,
                inventory.CaptureSnapshotForCache().GeneratedAtUnixMilliseconds);
            for (int i = 0; i < materials.Count; i++)
            {
                CampaignMaterialReward reward = materials[i];
                MaterialDefinition definition = catalog.GetMaterial(
                    reward.MaterialDefinitionId);
                long remaining = reward.Quantity;
                int stackIndex = 0;
                while (remaining > 0)
                {
                    long quantity = Math.Min(remaining, definition.MaxStackSize);
                    string instanceId =
                        $"idle_{StableToken(requestId)}_{i:D2}_{stackIndex:D3}";
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
                            0,
                            null,
                            0,
                            -1,
                            -1,
                            false,
                            0,
                            safeTimestamp,
                            safeTimestamp,
                            new ItemProvenanceData(
                                "local_idle_simulation",
                                reason,
                                requestId));
                        inventory.AddAuthorizedItem(item, definition, safeTimestamp);
                    }
                    else if (!string.Equals(
                        existing.DefinitionId,
                        definition.DefinitionId,
                        StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Colisão de material para requestId {requestId}.");
                    }
                    remaining -= quantity;
                    stackIndex++;
                }
            }
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

        private static long Scale(long value, int basisPoints, long minimum)
        {
            long scaled = checked(value * basisPoints / 10000L);
            return Math.Max(minimum, scaled);
        }

        private static long HoursToMilliseconds(int hours)
        {
            return checked(hours * 60L * 60L * 1000L);
        }
    }
}
