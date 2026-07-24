using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IdleMedievalLegends.Domain.Campaign;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Common;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Dungeons
{
    public enum DungeonRunState
    {
        Created = 0,
        EnergyReserved = 1,
        InBattle = 2,
        Won = 3,
        Lost = 4,
        RewardsGranted = 5,
        Cancelled = 6,
        Failed = 7
    }

    public enum DungeonFailureClassification
    {
        None = 0,
        PlayerError = 1,
        TechnicalNonRefundable = 2,
        TechnicalRefundable = 3
    }

    [Serializable]
    public sealed class DungeonScheduleMetadata
    {
        [SerializeField] private string scheduleId = string.Empty;
        [SerializeField] private string futureScheduleNotes = string.Empty;
        [SerializeField] private bool requiresServerSchedule;

        public DungeonScheduleMetadata(
            string scheduleId = "",
            string futureScheduleNotes = "",
            bool requiresServerSchedule = false)
        {
            this.scheduleId = scheduleId ?? string.Empty;
            this.futureScheduleNotes = futureScheduleNotes ?? string.Empty;
            this.requiresServerSchedule = requiresServerSchedule;
        }

        public string ScheduleId => scheduleId;
        public string FutureScheduleNotes => futureScheduleNotes;
        public bool RequiresServerSchedule => requiresServerSchedule;
    }

    [Serializable]
    public sealed class DungeonRewardTableEntry
    {
        [SerializeField] private string itemDefinitionId = string.Empty;
        [SerializeField] private long minimumQuantity;
        [SerializeField] private long maximumQuantity;
        [SerializeField] private int chanceBasisPoints;
        [SerializeField] private bool guaranteed;
        [SerializeField] private bool firstClearOnly;
        [SerializeField] private int difficultyMultiplier = 10000;

        public DungeonRewardTableEntry(
            string itemDefinitionId,
            long minimumQuantity,
            long maximumQuantity,
            int chanceBasisPoints,
            bool guaranteed,
            bool firstClearOnly,
            int difficultyMultiplier = 10000)
        {
            if (string.IsNullOrWhiteSpace(itemDefinitionId))
                throw new ArgumentException(
                    "itemDefinitionId é obrigatório.",
                    nameof(itemDefinitionId));
            if (minimumQuantity < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumQuantity));
            if (maximumQuantity < minimumQuantity)
                throw new ArgumentOutOfRangeException(nameof(maximumQuantity));
            if (chanceBasisPoints < 0 || chanceBasisPoints > 10000)
                throw new ArgumentOutOfRangeException(nameof(chanceBasisPoints));
            if (difficultyMultiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(difficultyMultiplier));
            this.itemDefinitionId = itemDefinitionId;
            this.minimumQuantity = minimumQuantity;
            this.maximumQuantity = maximumQuantity;
            this.chanceBasisPoints = chanceBasisPoints;
            this.guaranteed = guaranteed;
            this.firstClearOnly = firstClearOnly;
            this.difficultyMultiplier = difficultyMultiplier;
        }

        public string ItemDefinitionId => itemDefinitionId;
        public long MinimumQuantity => minimumQuantity;
        public long MaximumQuantity => maximumQuantity;
        public int ChanceBasisPoints => chanceBasisPoints;
        public bool Guaranteed => guaranteed;
        public bool FirstClearOnly => firstClearOnly;
        public int DifficultyMultiplier => difficultyMultiplier;
    }

    [Serializable]
    public sealed class DungeonRewardTable
    {
        [SerializeField] private long gold;
        [SerializeField] private List<DungeonRewardTableEntry> entries =
            new List<DungeonRewardTableEntry>();

        public DungeonRewardTable(
            IEnumerable<DungeonRewardTableEntry> entries,
            long gold = 0)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            this.gold = gold;
            this.entries = entries == null
                ? new List<DungeonRewardTableEntry>()
                : new List<DungeonRewardTableEntry>(entries);
            for (int i = 0; i < this.entries.Count; i++)
            {
                if (this.entries[i] == null)
                    throw new InvalidOperationException("Entrada de recompensa nula.");
            }
        }

        public long Gold => gold;
        public IReadOnlyList<DungeonRewardTableEntry> Entries =>
            new ReadOnlyCollection<DungeonRewardTableEntry>(entries);
    }

    [Serializable]
    public sealed class DungeonEncounterDefinition
    {
        [SerializeField] private string encounterId = string.Empty;
        [SerializeField] private StageEnemyFormation enemyFormation;
        [SerializeField] private string scenarioId = string.Empty;
        [SerializeField] private string battleSceneName = "Battle";

        public DungeonEncounterDefinition(
            string encounterId,
            StageEnemyFormation enemyFormation,
            string scenarioId,
            string battleSceneName = "Battle")
        {
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("encounterId é obrigatório.", nameof(encounterId));
            if (string.IsNullOrWhiteSpace(scenarioId))
                throw new ArgumentException("scenarioId é obrigatório.", nameof(scenarioId));
            if (string.IsNullOrWhiteSpace(battleSceneName))
                throw new ArgumentException(
                    "battleSceneName é obrigatório.",
                    nameof(battleSceneName));
            this.encounterId = encounterId;
            this.enemyFormation = enemyFormation ??
                throw new ArgumentNullException(nameof(enemyFormation));
            this.scenarioId = scenarioId;
            this.battleSceneName = battleSceneName;
        }

        public string EncounterId => encounterId;
        public StageEnemyFormation EnemyFormation => enemyFormation;
        public string ScenarioId => scenarioId;
        public string BattleSceneName => battleSceneName;
    }

    [Serializable]
    public sealed class DungeonDifficultyDefinition
    {
        [SerializeField] private string difficultyId = string.Empty;
        [SerializeField] private long recommendedPower;
        [SerializeField] private int energyCost;
        [SerializeField] private DungeonEncounterDefinition encounter;
        [SerializeField] private DungeonRewardTable rewardTable;
        [SerializeField] private DungeonRewardTable firstClearReward;
        [SerializeField] private int minimumPlayerLevel = 1;
        [SerializeField] private string requiredCampaignStage = string.Empty;
        [SerializeField] private int durationEstimateSeconds;
        [SerializeField] private ItemTier materialTier = ItemTier.Tier1;
        [SerializeField] private bool blockBelowRecommendedPower;

        public DungeonDifficultyDefinition(
            string difficultyId,
            long recommendedPower,
            int energyCost,
            DungeonEncounterDefinition encounter,
            DungeonRewardTable rewardTable,
            DungeonRewardTable firstClearReward,
            int minimumPlayerLevel,
            string requiredCampaignStage,
            int durationEstimateSeconds,
            ItemTier materialTier,
            bool blockBelowRecommendedPower = false)
        {
            if (string.IsNullOrWhiteSpace(difficultyId))
                throw new ArgumentException("difficultyId é obrigatório.", nameof(difficultyId));
            if (recommendedPower < 0)
                throw new ArgumentOutOfRangeException(nameof(recommendedPower));
            if (energyCost < 1)
                throw new ArgumentOutOfRangeException(nameof(energyCost));
            if (minimumPlayerLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(minimumPlayerLevel));
            if (string.IsNullOrWhiteSpace(requiredCampaignStage))
                throw new ArgumentException(
                    "requiredCampaignStage é obrigatório.",
                    nameof(requiredCampaignStage));
            if (durationEstimateSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(durationEstimateSeconds));
            if (!materialTier.IsValid())
                throw new ArgumentOutOfRangeException(nameof(materialTier));
            this.difficultyId = difficultyId;
            this.recommendedPower = recommendedPower;
            this.energyCost = energyCost;
            this.encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            this.rewardTable = rewardTable ?? throw new ArgumentNullException(nameof(rewardTable));
            this.firstClearReward = firstClearReward ??
                throw new ArgumentNullException(nameof(firstClearReward));
            this.minimumPlayerLevel = minimumPlayerLevel;
            this.requiredCampaignStage = requiredCampaignStage;
            this.durationEstimateSeconds = durationEstimateSeconds;
            this.materialTier = materialTier;
            this.blockBelowRecommendedPower = blockBelowRecommendedPower;
        }

        public string DifficultyId => difficultyId;
        public long RecommendedPower => recommendedPower;
        public int EnergyCost => energyCost;
        public DungeonEncounterDefinition Encounter => encounter;
        public StageEnemyFormation EnemyFormation => encounter.EnemyFormation;
        public DungeonRewardTable RewardTable => rewardTable;
        public DungeonRewardTable FirstClearReward => firstClearReward;
        public int MinimumPlayerLevel => minimumPlayerLevel;
        public string RequiredCampaignStage => requiredCampaignStage;
        public int DurationEstimateSeconds => durationEstimateSeconds;
        public ItemTier MaterialTier => materialTier;
        public bool BlockBelowRecommendedPower => blockBelowRecommendedPower;
    }

    [Serializable]
    public sealed class DungeonDefinition
    {
        [SerializeField] private string dungeonId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string description = string.Empty;
        [SerializeField] private CraftingProfession associatedProfession;
        [SerializeField] private string unlockStageId = string.Empty;
        [SerializeField] private List<DungeonDifficultyDefinition> availableDifficulties =
            new List<DungeonDifficultyDefinition>();
        [SerializeField] private int dailyAttemptLimit;
        [SerializeField] private DungeonScheduleMetadata scheduleMetadata;
        [SerializeField] private string iconPlaceholder = string.Empty;
        [SerializeField] private List<string> tags = new List<string>();

        public DungeonDefinition(
            string dungeonId,
            string displayName,
            string description,
            CraftingProfession associatedProfession,
            string unlockStageId,
            IEnumerable<DungeonDifficultyDefinition> availableDifficulties,
            int? dailyAttemptLimit,
            DungeonScheduleMetadata scheduleMetadata,
            string iconPlaceholder,
            IEnumerable<string> tags)
        {
            if (string.IsNullOrWhiteSpace(dungeonId))
                throw new ArgumentException("dungeonId é obrigatório.", nameof(dungeonId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("displayName é obrigatório.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("description é obrigatória.", nameof(description));
            if (!associatedProfession.IsCraftingProfession())
                throw new ArgumentOutOfRangeException(nameof(associatedProfession));
            if (string.IsNullOrWhiteSpace(unlockStageId))
                throw new ArgumentException("unlockStageId é obrigatório.", nameof(unlockStageId));
            if (dailyAttemptLimit.HasValue && dailyAttemptLimit.Value < 1)
                throw new ArgumentOutOfRangeException(nameof(dailyAttemptLimit));
            if (string.IsNullOrWhiteSpace(iconPlaceholder))
                throw new ArgumentException(
                    "iconPlaceholder é obrigatório.",
                    nameof(iconPlaceholder));

            this.dungeonId = dungeonId;
            this.displayName = displayName;
            this.description = description;
            this.associatedProfession = associatedProfession;
            this.unlockStageId = unlockStageId;
            this.availableDifficulties = availableDifficulties == null
                ? new List<DungeonDifficultyDefinition>()
                : new List<DungeonDifficultyDefinition>(availableDifficulties);
            if (this.availableDifficulties.Count == 0)
                throw new InvalidOperationException("Masmorra deve possuir dificuldade.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < this.availableDifficulties.Count; i++)
            {
                DungeonDifficultyDefinition difficulty =
                    this.availableDifficulties[i] ??
                    throw new InvalidOperationException("Dificuldade nula.");
                if (!ids.Add(difficulty.DifficultyId))
                    throw new InvalidOperationException(
                        $"difficultyId duplicado: {difficulty.DifficultyId}.");
            }
            this.dailyAttemptLimit = dailyAttemptLimit ?? 0;
            this.scheduleMetadata = scheduleMetadata ??
                new DungeonScheduleMetadata();
            this.iconPlaceholder = iconPlaceholder;
            this.tags = tags == null ? new List<string>() : new List<string>(tags);
        }

        public string DungeonId => dungeonId;
        public string DisplayName => displayName;
        public string Description => description;
        public CraftingProfession AssociatedProfession => associatedProfession;
        public string UnlockStageId => unlockStageId;
        public IReadOnlyList<DungeonDifficultyDefinition> AvailableDifficulties =>
            new ReadOnlyCollection<DungeonDifficultyDefinition>(availableDifficulties);
        public int? DailyAttemptLimit =>
            dailyAttemptLimit > 0 ? dailyAttemptLimit : (int?)null;
        public DungeonScheduleMetadata ScheduleMetadata => scheduleMetadata;
        public string IconPlaceholder => iconPlaceholder;
        public IReadOnlyList<string> Tags => new ReadOnlyCollection<string>(tags);

        public DungeonDifficultyDefinition GetDifficulty(string difficultyId)
        {
            for (int i = 0; i < availableDifficulties.Count; i++)
            {
                if (string.Equals(
                    availableDifficulties[i].DifficultyId,
                    difficultyId,
                    StringComparison.Ordinal))
                {
                    return availableDifficulties[i];
                }
            }
            throw new KeyNotFoundException($"Dificuldade inexistente: {difficultyId}.");
        }
    }

    public sealed class DungeonCatalog
    {
        private readonly List<DungeonDefinition> dungeons;
        private readonly Dictionary<string, DungeonDefinition> index;

        public DungeonCatalog(IEnumerable<DungeonDefinition> dungeons)
        {
            this.dungeons = dungeons == null
                ? new List<DungeonDefinition>()
                : new List<DungeonDefinition>(dungeons);
            if (this.dungeons.Count == 0)
                throw new InvalidOperationException("Catálogo de masmorras vazio.");
            index = new Dictionary<string, DungeonDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < this.dungeons.Count; i++)
            {
                DungeonDefinition dungeon = this.dungeons[i] ??
                    throw new InvalidOperationException("Masmorra nula.");
                if (!index.TryAdd(dungeon.DungeonId, dungeon))
                    throw new InvalidOperationException(
                        $"dungeonId duplicado: {dungeon.DungeonId}.");
            }
        }

        public IReadOnlyList<DungeonDefinition> Dungeons =>
            new ReadOnlyCollection<DungeonDefinition>(dungeons);

        public DungeonDefinition GetDungeon(string dungeonId)
        {
            if (string.IsNullOrWhiteSpace(dungeonId) ||
                !index.TryGetValue(dungeonId, out DungeonDefinition dungeon))
            {
                throw new KeyNotFoundException($"Masmorra inexistente: {dungeonId}.");
            }
            return dungeon;
        }
    }

    public sealed class DungeonEntryRequest
    {
        public DungeonEntryRequest(
            string requestId,
            string dungeonId,
            string difficultyId,
            IEnumerable<string> teamHeroInstanceIds,
            int playerLevel)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("requestId é obrigatório.", nameof(requestId));
            if (string.IsNullOrWhiteSpace(dungeonId))
                throw new ArgumentException("dungeonId é obrigatório.", nameof(dungeonId));
            if (string.IsNullOrWhiteSpace(difficultyId))
                throw new ArgumentException("difficultyId é obrigatório.", nameof(difficultyId));
            if (playerLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(playerLevel));
            var team = teamHeroInstanceIds == null
                ? new List<string>()
                : new List<string>(teamHeroInstanceIds);
            if (team.Count < 1 || team.Count > 5)
                throw new InvalidOperationException(
                    "Equipe deve possuir de um a cinco heróis.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < team.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(team[i]) || !ids.Add(team[i]))
                    throw new InvalidOperationException("Equipe possui ID inválido ou duplicado.");
            }
            RequestId = requestId;
            DungeonId = dungeonId;
            DifficultyId = difficultyId;
            TeamHeroInstanceIds = new ReadOnlyCollection<string>(team);
            PlayerLevel = playerLevel;
        }

        public string RequestId { get; }
        public string DungeonId { get; }
        public string DifficultyId { get; }
        public IReadOnlyList<string> TeamHeroInstanceIds { get; }
        public int PlayerLevel { get; }

        public bool HasSamePayload(DungeonEntryRequest other)
        {
            if (other == null ||
                !string.Equals(RequestId, other.RequestId, StringComparison.Ordinal) ||
                !string.Equals(DungeonId, other.DungeonId, StringComparison.Ordinal) ||
                !string.Equals(DifficultyId, other.DifficultyId, StringComparison.Ordinal) ||
                PlayerLevel != other.PlayerLevel ||
                TeamHeroInstanceIds.Count != other.TeamHeroInstanceIds.Count)
            {
                return false;
            }
            for (int i = 0; i < TeamHeroInstanceIds.Count; i++)
            {
                if (!string.Equals(
                    TeamHeroInstanceIds[i],
                    other.TeamHeroInstanceIds[i],
                    StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public sealed class DungeonRewardGrant
    {
        public DungeonRewardGrant(string itemDefinitionId, long quantity, int entryIndex)
        {
            if (string.IsNullOrWhiteSpace(itemDefinitionId))
                throw new ArgumentException(
                    "itemDefinitionId é obrigatório.",
                    nameof(itemDefinitionId));
            if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (entryIndex < 0) throw new ArgumentOutOfRangeException(nameof(entryIndex));
            ItemDefinitionId = itemDefinitionId;
            Quantity = quantity;
            EntryIndex = entryIndex;
        }

        public string ItemDefinitionId { get; }
        public long Quantity { get; }
        public int EntryIndex { get; }
    }

    public sealed class DungeonRun
    {
        private BattleResult simulatedBattle;
        private BattleRequest battleRequest;

        public DungeonRun(
            string runId,
            DungeonEntryRequest entryRequest,
            DungeonDefinition dungeon,
            DungeonDifficultyDefinition difficulty,
            long serverSeed,
            long createdAtUnixMilliseconds,
            bool powerBelowRecommended)
        {
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("runId é obrigatório.", nameof(runId));
            if (serverSeed <= 0) throw new ArgumentOutOfRangeException(nameof(serverSeed));
            if (createdAtUnixMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(createdAtUnixMilliseconds));
            RunId = runId;
            EntryRequest = entryRequest ?? throw new ArgumentNullException(nameof(entryRequest));
            Dungeon = dungeon ?? throw new ArgumentNullException(nameof(dungeon));
            Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
            ServerSeed = serverSeed;
            CreatedAtUnixMilliseconds = createdAtUnixMilliseconds;
            PowerBelowRecommended = powerBelowRecommended;
            State = DungeonRunState.Created;
        }

        public string RunId { get; }
        public DungeonEntryRequest EntryRequest { get; }
        public DungeonDefinition Dungeon { get; }
        public DungeonDifficultyDefinition Difficulty { get; }
        public long ServerSeed { get; }
        public long CreatedAtUnixMilliseconds { get; }
        public bool PowerBelowRecommended { get; }
        public DungeonRunState State { get; private set; }
        public DungeonFailureClassification FailureClassification { get; private set; }
        public BattleResult SimulatedBattle => simulatedBattle;
        public BattleRequest BattleRequest => battleRequest;

        internal void ReserveEnergy()
        {
            RequireState(DungeonRunState.Created);
            State = DungeonRunState.EnergyReserved;
        }

        internal void BeginBattle(BattleRequest request, BattleResult battle)
        {
            RequireState(DungeonRunState.EnergyReserved);
            battleRequest = request ?? throw new ArgumentNullException(nameof(request));
            simulatedBattle = battle ?? throw new ArgumentNullException(nameof(battle));
            State = DungeonRunState.InBattle;
        }

        internal void MarkBattleOutcome(bool won)
        {
            RequireState(DungeonRunState.InBattle);
            State = won ? DungeonRunState.Won : DungeonRunState.Lost;
        }

        internal void MarkRewardsGranted()
        {
            RequireState(DungeonRunState.Won);
            State = DungeonRunState.RewardsGranted;
        }

        internal void Cancel()
        {
            if (State != DungeonRunState.Created &&
                State != DungeonRunState.EnergyReserved)
            {
                throw new InvalidOperationException(
                    "Cancelamento voluntário só é permitido antes da batalha.");
            }
            State = DungeonRunState.Cancelled;
        }

        internal void Fail(DungeonFailureClassification classification)
        {
            if (classification == DungeonFailureClassification.None)
                throw new ArgumentOutOfRangeException(nameof(classification));
            if (State == DungeonRunState.Lost ||
                State == DungeonRunState.RewardsGranted ||
                State == DungeonRunState.Cancelled ||
                State == DungeonRunState.Failed)
            {
                throw new InvalidOperationException("Run já está em estado terminal.");
            }
            FailureClassification = classification;
            State = DungeonRunState.Failed;
        }

        private void RequireState(DungeonRunState expected)
        {
            if (State != expected)
                throw new InvalidOperationException(
                    $"Transição inválida de {State}; esperado {expected}.");
        }
    }

    public sealed class DungeonRunResult
    {
        public DungeonRunResult(
            DungeonRun run,
            BattleResult battle,
            bool firstClear,
            IReadOnlyList<DungeonRewardGrant> rewards,
            long goldGranted)
        {
            Run = run ?? throw new ArgumentNullException(nameof(run));
            Battle = battle ?? throw new ArgumentNullException(nameof(battle));
            FirstClear = firstClear;
            Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            if (goldGranted < 0)
                throw new ArgumentOutOfRangeException(nameof(goldGranted));
            GoldGranted = goldGranted;
        }

        public DungeonRun Run { get; }
        public BattleResult Battle { get; }
        public bool Victory =>
            Battle.Outcome == BattleOutcome.AttackerVictory &&
            Battle.WinningTeam == BattleSide.Attacker;
        public bool FirstClear { get; }
        public IReadOnlyList<DungeonRewardGrant> Rewards { get; }
        public long GoldGranted { get; }
    }
}
