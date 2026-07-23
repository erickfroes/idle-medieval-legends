using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Inventory;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Crafting
{
    public enum CraftingOperationType
    {
        Refine = 0,
        CraftEquipment = 1,
        CraftConsumable = 2,
        Enchant = 3,
        GatherExpedition = 4,
        Salvage = 5
    }

    public enum CraftingJobStatus
    {
        Pending = 0,
        Running = 1,
        ReadyToClaim = 2,
        Completed = 3,
        Cancelled = 4,
        Failed = 5,

        [Obsolete("Use Pending.")]
        Queued = Pending,
        [Obsolete("Use Running.")]
        InProgress = Running,
        [Obsolete("Use ReadyToClaim.")]
        ReadyToFinalize = ReadyToClaim
    }

    public enum RecipeUnlockSource
    {
        Default = 0,
        Diagram = 1,
        ProfessionLevel = 2,
        Dungeon = 3,
        Reputation = 4,
        Seasonal = 5,
        AdminGrant = 6
    }

    [Serializable]
    public sealed class CraftingIngredientData
    {
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private long quantity = 1;
        [SerializeField] private ItemTier minimumTier = ItemTier.Tier1;
        [SerializeField] private GameRarity minimumRarity = GameRarity.Common;
        [SerializeField] private CraftingProfession suppliedByProfession = CraftingProfession.None;

        public string DefinitionId => definitionId;
        public long Quantity => quantity;
        public ItemTier MinimumTier => minimumTier;
        public GameRarity MinimumRarity => minimumRarity;
        public CraftingProfession SuppliedByProfession => suppliedByProfession;

        public CraftingIngredientData()
        {
        }

        public CraftingIngredientData(
            string definitionId,
            long quantity,
            ItemTier minimumTier,
            GameRarity minimumRarity,
            CraftingProfession suppliedByProfession)
        {
            this.definitionId = definitionId ?? throw new ArgumentNullException(nameof(definitionId));
            this.quantity = quantity;
            this.minimumTier = minimumTier;
            this.minimumRarity = minimumRarity;
            this.suppliedByProfession = suppliedByProfession;
            Validate();
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new InvalidOperationException("Ingrediente sem definitionId.");
            if (quantity <= 0)
                throw new InvalidOperationException($"Ingrediente {definitionId} com quantidade inválida.");
            if (!minimumTier.IsValid())
                throw new InvalidOperationException($"Ingrediente {definitionId} com Tier inválido.");
            if (!minimumRarity.IsValid())
                throw new InvalidOperationException($"Ingrediente {definitionId} com raridade inválida.");
            if (suppliedByProfession != CraftingProfession.None &&
                !suppliedByProfession.IsCraftingProfession())
            {
                throw new InvalidOperationException(
                    $"Ingrediente {definitionId} com profissão fornecedora inválida.");
            }
        }
    }

    /// <summary>
    /// Definição de catálogo. Em produção, o backend carrega a versão oficial;
    /// o cliente usa uma cópia somente para interface e previsão.
    /// </summary>
    [Serializable]
    public sealed class CraftingRecipeData
    {
        [SerializeField] private string recipeId = string.Empty;
        [SerializeField] private CraftingProfession profession;
        [SerializeField] private CraftingOperationType operationType;
        [SerializeField] private ItemTier tier = ItemTier.Tier1;
        [SerializeField] private ProfessionRank minimumRank = ProfessionRank.Apprentice;
        [SerializeField] private int minimumProfessionLevel = 1;
        [SerializeField] private ItemTier requiredStationTier = ItemTier.Tier1;
        [SerializeField] private string outputDefinitionId = string.Empty;
        [SerializeField] private InventoryItemKind outputKind;
        [SerializeField] private long outputQuantity = 1;
        [SerializeField] private int baseDurationSeconds = 30;
        [SerializeField] private int focusCost = 1;
        [SerializeField] private long baseProfessionExperience = 25;
        [SerializeField] private bool requiresUnlock;
        [SerializeField] private bool outputTradable = true;
        [SerializeField] private List<CraftingIngredientData> ingredients =
            new List<CraftingIngredientData>();

        public string RecipeId => recipeId;
        public CraftingProfession Profession => profession;
        public CraftingOperationType OperationType => operationType;
        public ItemTier Tier => tier;
        public ProfessionRank MinimumRank => minimumRank;
        public int MinimumProfessionLevel => minimumProfessionLevel;
        public ItemTier RequiredStationTier => requiredStationTier;
        public string OutputDefinitionId => outputDefinitionId;
        public InventoryItemKind OutputKind => outputKind;
        public long OutputQuantity => outputQuantity;
        public int BaseDurationSeconds => baseDurationSeconds;
        public int FocusCost => focusCost;
        public long BaseProfessionExperience => baseProfessionExperience;
        public bool RequiresUnlock => requiresUnlock;
        public bool OutputTradable => outputTradable;
        public IReadOnlyList<CraftingIngredientData> Ingredients
        {
            get
            {
                if (ingredients == null) ingredients = new List<CraftingIngredientData>();
                return ingredients;
            }
        }

        public CraftingRecipeData()
        {
        }

        public CraftingRecipeData(
            string recipeId,
            CraftingProfession profession,
            CraftingOperationType operationType,
            ItemTier tier,
            ProfessionRank minimumRank,
            int minimumProfessionLevel,
            ItemTier requiredStationTier,
            string outputDefinitionId,
            InventoryItemKind outputKind,
            long outputQuantity,
            int baseDurationSeconds,
            int focusCost,
            long baseProfessionExperience,
            bool requiresUnlock,
            bool outputTradable,
            List<CraftingIngredientData> ingredients = null)
        {
            this.recipeId = recipeId ?? throw new ArgumentNullException(nameof(recipeId));
            this.profession = profession;
            this.operationType = operationType;
            this.tier = tier;
            this.minimumRank = minimumRank;
            this.minimumProfessionLevel = minimumProfessionLevel;
            this.requiredStationTier = requiredStationTier;
            this.outputDefinitionId = outputDefinitionId ??
                throw new ArgumentNullException(nameof(outputDefinitionId));
            this.outputKind = outputKind;
            this.outputQuantity = outputQuantity;
            this.baseDurationSeconds = baseDurationSeconds;
            this.focusCost = focusCost;
            this.baseProfessionExperience = baseProfessionExperience;
            this.requiresUnlock = requiresUnlock;
            this.outputTradable = outputTradable;
            this.ingredients = ingredients ?? new List<CraftingIngredientData>();
            Validate();
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                throw new InvalidOperationException("Receita sem recipeId.");
            if (!profession.IsCraftingProfession())
                throw new InvalidOperationException($"Receita {recipeId} sem profissão válida.");
            if (!tier.IsValid() || !requiredStationTier.IsValid())
                throw new InvalidOperationException($"Receita {recipeId} com Tier inválido.");
            if (minimumProfessionLevel < 1 || minimumProfessionLevel > 100)
                throw new InvalidOperationException($"Receita {recipeId} com nível mínimo inválido.");
            if (outputQuantity <= 0)
                throw new InvalidOperationException($"Receita {recipeId} com saída inválida.");
            if (baseDurationSeconds < 0 || focusCost < 0 || baseProfessionExperience < 0)
                throw new InvalidOperationException($"Receita {recipeId} com custos inválidos.");
            if (string.IsNullOrWhiteSpace(outputDefinitionId))
                throw new InvalidOperationException($"Receita {recipeId} sem outputDefinitionId.");

            if (ingredients == null)
            {
                ingredients = new List<CraftingIngredientData>();
            }

            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] == null)
                    throw new InvalidOperationException($"Receita {recipeId} possui ingrediente nulo.");
                ingredients[i].Validate();
            }
        }
    }

    [Serializable]
    public sealed class RecipeUnlockData
    {
        [SerializeField] private string recipeId = string.Empty;
        [SerializeField] private RecipeUnlockSource source;
        [SerializeField] private long unlockedAtUnixMilliseconds;
        [SerializeField] private long serverVersion;

        public string RecipeId => recipeId;
        public RecipeUnlockSource Source => source;
        public long UnlockedAtUnixMilliseconds => unlockedAtUnixMilliseconds;
        public long ServerVersion => serverVersion;

        public RecipeUnlockData()
        {
        }

        public RecipeUnlockData(
            string recipeId,
            RecipeUnlockSource source,
            long unlockedAtUnixMilliseconds,
            long serverVersion)
        {
            this.recipeId = recipeId ?? throw new ArgumentNullException(nameof(recipeId));
            this.source = source;
            this.unlockedAtUnixMilliseconds = unlockedAtUnixMilliseconds;
            this.serverVersion = serverVersion;
        }
    }

    [Serializable]
    public sealed class ProfessionProgressData
    {
        private static readonly ProfessionProgressionTuning ValidationTuning =
            new ProfessionProgressionTuning();

        [SerializeField] private CraftingProfession profession;
        [SerializeField] private int level = 1;
        [SerializeField] private long totalExperience;
        [SerializeField] private ProfessionRank rank = ProfessionRank.Apprentice;
        [SerializeField] private ItemTier maxUnlockedTier = ItemTier.Tier1;
        [SerializeField] private ItemTier stationTier = ItemTier.Tier1;
        [SerializeField] private long craftsCompleted;
        [SerializeField] private int masteryPoints;
        [SerializeField] private int mythicPityCounter;
        [SerializeField] private long serverVersion;

        public CraftingProfession Profession => profession;
        public int Level => level;
        public long TotalExperience => totalExperience;
        public ProfessionRank Rank => rank;
        public ItemTier MaxUnlockedTier => maxUnlockedTier;
        public ItemTier StationTier => stationTier;
        public long CraftsCompleted => craftsCompleted;
        public int MasteryPoints => masteryPoints;
        public int MythicPityCounter => mythicPityCounter;
        public long ServerVersion => serverVersion;

        public ProfessionProgressData()
        {
        }

        public ProfessionProgressData(
            CraftingProfession profession,
            int level,
            long totalExperience,
            ProfessionRank rank,
            ItemTier maxUnlockedTier,
            ItemTier stationTier,
            long craftsCompleted,
            int masteryPoints,
            int mythicPityCounter,
            long serverVersion)
        {
            this.profession = profession;
            this.level = level;
            this.totalExperience = totalExperience;
            this.rank = rank;
            this.maxUnlockedTier = maxUnlockedTier;
            this.stationTier = stationTier;
            this.craftsCompleted = craftsCompleted;
            this.masteryPoints = masteryPoints;
            this.mythicPityCounter = mythicPityCounter;
            this.serverVersion = serverVersion;
            Validate();
        }

        internal void Validate()
        {
            if (!profession.IsCraftingProfession())
                throw new InvalidOperationException("Progresso com profissão inválida.");
            if (level < 1 || level > 100)
                throw new InvalidOperationException($"{profession}: nível fora de 1..100.");
            if (!Enum.IsDefined(typeof(ProfessionRank), rank))
                throw new InvalidOperationException($"{profession}: grau inválido.");
            if (totalExperience < 0 || craftsCompleted < 0 || masteryPoints < 0)
                throw new InvalidOperationException($"{profession}: progresso negativo.");
            if (!maxUnlockedTier.IsValid() || !stationTier.IsValid())
                throw new InvalidOperationException($"{profession}: Tier inválido.");
            ProfessionRank expectedRank =
                ProfessionProgression.GetRankForLevel(level, ValidationTuning);
            if (rank != expectedRank)
            {
                throw new InvalidOperationException(
                    $"{profession}: grau {rank} não corresponde ao nível {level}; " +
                    $"esperado {expectedRank}.");
            }
            ItemTier expectedTier =
                ProfessionProgression.GetMaximumUnlockedTier(level, ValidationTuning);
            if (maxUnlockedTier != expectedTier)
            {
                throw new InvalidOperationException(
                    $"{profession}: Tier liberado {maxUnlockedTier} não corresponde " +
                    $"ao nível {level}; esperado {expectedTier}.");
            }
            if (mythicPityCounter < 0)
                throw new InvalidOperationException($"{profession}: pity mítico negativo.");
        }

        public static ProfessionProgressData CreateNew(CraftingProfession profession)
        {
            return new ProfessionProgressData(
                profession,
                1,
                0,
                ProfessionRank.Apprentice,
                ItemTier.Tier1,
                ItemTier.Tier1,
                0,
                0,
                0,
                0);
        }
    }

    [Serializable]
    public sealed class CraftingJobData
    {
        [SerializeField] private string jobId = string.Empty;
        [SerializeField] private string recipeId = string.Empty;
        [SerializeField] private CraftingProfession profession;
        [SerializeField] private CraftingJobStatus status;
        [SerializeField] private int quantity = 1;
        [SerializeField] private long startedAtUnixMilliseconds;
        [SerializeField] private long completesAtUnixMilliseconds;
        [SerializeField] private string reservationId = string.Empty;
        [SerializeField] private List<string> outputInstanceIds = new List<string>();
        [SerializeField] private long serverVersion;

        public string JobId => jobId;
        public string RecipeId => recipeId;
        public CraftingProfession Profession => profession;
        public CraftingJobStatus Status => status;
        public int Quantity => quantity;
        public long StartedAtUnixMilliseconds => startedAtUnixMilliseconds;
        public long CompletesAtUnixMilliseconds => completesAtUnixMilliseconds;
        public string ReservationId => reservationId;
        public IReadOnlyList<string> OutputInstanceIds
        {
            get
            {
                if (outputInstanceIds == null) outputInstanceIds = new List<string>();
                return outputInstanceIds;
            }
        }
        public long ServerVersion => serverVersion;

        public CraftingJobData()
        {
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new InvalidOperationException("Job de crafting sem jobId.");
            if (string.IsNullOrWhiteSpace(recipeId))
                throw new InvalidOperationException($"Job {jobId} sem recipeId.");
            if (!profession.IsCraftingProfession())
                throw new InvalidOperationException($"Job {jobId} com profissão inválida.");
            if (!Enum.IsDefined(typeof(CraftingJobStatus), status))
                throw new InvalidOperationException($"Job {jobId} com status inválido.");
            if (quantity <= 0)
                throw new InvalidOperationException($"Job {jobId} com quantidade inválida.");
            if (completesAtUnixMilliseconds < startedAtUnixMilliseconds)
                throw new InvalidOperationException($"Job {jobId} com timestamps inválidos.");
            if ((status == CraftingJobStatus.Pending ||
                 status == CraftingJobStatus.Running ||
                 status == CraftingJobStatus.ReadyToClaim) &&
                string.IsNullOrWhiteSpace(reservationId))
            {
                throw new InvalidOperationException($"Job ativo {jobId} sem reservationId.");
            }
            if (outputInstanceIds == null)
                outputInstanceIds = new List<string>();
        }
    }

    [Serializable]
    public sealed class ProfessionSnapshotData
    {
        public const int CurrentSchemaVersion = 2;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string playerId = string.Empty;
        [SerializeField] private long serverRevision;
        [SerializeField] private long generatedAtUnixMilliseconds;
        [SerializeField] private CraftingProfession primaryProfession = CraftingProfession.None;
        [SerializeField] private int focusAvailable;
        [SerializeField] private int focusCap = 100;
        [SerializeField] private List<ProfessionProgressData> professions =
            new List<ProfessionProgressData>();
        [SerializeField] private List<RecipeUnlockData> recipeUnlocks =
            new List<RecipeUnlockData>();
        [SerializeField] private List<CraftingJobData> activeJobs =
            new List<CraftingJobData>();

        public int SchemaVersion => schemaVersion;
        public string PlayerId => playerId;
        public long ServerRevision => serverRevision;
        public long GeneratedAtUnixMilliseconds => generatedAtUnixMilliseconds;
        public CraftingProfession PrimaryProfession => primaryProfession;
        public int FocusAvailable => focusAvailable;
        public int FocusCap => focusCap;
        public IReadOnlyList<ProfessionProgressData> Professions
        {
            get
            {
                if (professions == null) professions = new List<ProfessionProgressData>();
                return professions;
            }
        }

        public IReadOnlyList<RecipeUnlockData> RecipeUnlocks
        {
            get
            {
                if (recipeUnlocks == null) recipeUnlocks = new List<RecipeUnlockData>();
                return recipeUnlocks;
            }
        }

        public IReadOnlyList<CraftingJobData> ActiveJobs
        {
            get
            {
                if (activeJobs == null) activeJobs = new List<CraftingJobData>();
                return activeJobs;
            }
        }

        public ProfessionSnapshotData()
        {
        }

        public ProfessionSnapshotData(
            int schemaVersion,
            string playerId,
            long serverRevision,
            long generatedAtUnixMilliseconds,
            CraftingProfession primaryProfession,
            int focusAvailable,
            int focusCap,
            List<ProfessionProgressData> professions,
            List<RecipeUnlockData> recipeUnlocks,
            List<CraftingJobData> activeJobs)
        {
            this.schemaVersion = schemaVersion;
            this.playerId = playerId ?? string.Empty;
            this.serverRevision = serverRevision;
            this.generatedAtUnixMilliseconds = generatedAtUnixMilliseconds;
            this.primaryProfession = primaryProfession;
            this.focusAvailable = focusAvailable;
            this.focusCap = focusCap;
            this.professions = professions ?? new List<ProfessionProgressData>();
            this.recipeUnlocks = recipeUnlocks ?? new List<RecipeUnlockData>();
            this.activeJobs = activeJobs ?? new List<CraftingJobData>();
        }

        internal void NormalizeAfterLoad(string fallbackPlayerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                playerId = fallbackPlayerId ?? string.Empty;
            if (professions == null) professions = new List<ProfessionProgressData>();
            if (recipeUnlocks == null) recipeUnlocks = new List<RecipeUnlockData>();
            if (activeJobs == null) activeJobs = new List<CraftingJobData>();

            if (professions.Count == 0)
            {
                professions.Add(ProfessionProgressData.CreateNew(CraftingProfession.Blacksmith));
                professions.Add(ProfessionProgressData.CreateNew(CraftingProfession.Tailor));
                professions.Add(ProfessionProgressData.CreateNew(CraftingProfession.Enchanter));
                professions.Add(ProfessionProgressData.CreateNew(CraftingProfession.Alchemist));
                professions.Add(ProfessionProgressData.CreateNew(CraftingProfession.Gatherer));
            }

            if (focusCap <= 0) focusCap = 100;
            if (focusAvailable < 0) focusAvailable = 0;
            if (focusAvailable > focusCap) focusAvailable = focusCap;
            schemaVersion = CurrentSchemaVersion;
        }

        public static ProfessionSnapshotData CreateEmpty(
            string playerId = "",
            long generatedAtUnixMilliseconds = 0)
        {
            var progress = new List<ProfessionProgressData>
            {
                ProfessionProgressData.CreateNew(CraftingProfession.Blacksmith),
                ProfessionProgressData.CreateNew(CraftingProfession.Tailor),
                ProfessionProgressData.CreateNew(CraftingProfession.Enchanter),
                ProfessionProgressData.CreateNew(CraftingProfession.Alchemist),
                ProfessionProgressData.CreateNew(CraftingProfession.Gatherer)
            };

            return new ProfessionSnapshotData(
                CurrentSchemaVersion,
                playerId ?? string.Empty,
                0,
                generatedAtUnixMilliseconds,
                CraftingProfession.None,
                100,
                100,
                progress,
                new List<RecipeUnlockData>(),
                new List<CraftingJobData>());
        }
    }
}
