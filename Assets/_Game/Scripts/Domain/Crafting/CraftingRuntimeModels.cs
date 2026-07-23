using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Inventory;

namespace IdleMedievalLegends.Domain.Crafting
{
    public enum ProfessionSpecialization
    {
        None = 0,
        Primary = 1
    }

    public enum CraftingStationType
    {
        Forge = 1,
        Atelier = 2,
        ArcaneTable = 3,
        Laboratory = 4,
        ExpeditionCamp = 5
    }

    public enum CraftingReservationRole
    {
        Material = 0,
        Tool = 1,
        Catalyst = 2
    }

    public sealed class ReservedItemReference
    {
        public ReservedItemReference(
            string itemInstanceId,
            long quantity,
            CraftingReservationRole role,
            bool consumedAtCompletion)
        {
            if (string.IsNullOrWhiteSpace(itemInstanceId))
                throw new ArgumentException("itemInstanceId é obrigatório.", nameof(itemInstanceId));
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            ItemInstanceId = itemInstanceId;
            Quantity = quantity;
            Role = role;
            ConsumedAtCompletion = consumedAtCompletion;
        }

        public string ItemInstanceId { get; }
        public long Quantity { get; }
        public CraftingReservationRole Role { get; }
        public bool ConsumedAtCompletion { get; }
    }

    public sealed class CraftingProvenance
    {
        public CraftingProvenance(
            string jobId,
            string recipeId,
            string ownerPlayerId,
            string seedHash,
            int rulesVersion)
        {
            JobId = jobId ?? string.Empty;
            RecipeId = recipeId ?? string.Empty;
            OwnerPlayerId = ownerPlayerId ?? string.Empty;
            SeedHash = seedHash ?? string.Empty;
            RulesVersion = rulesVersion;
        }

        public string JobId { get; }
        public string RecipeId { get; }
        public string OwnerPlayerId { get; }
        public string SeedHash { get; }
        public int RulesVersion { get; }
    }

    public sealed class CraftingOutput
    {
        public CraftingOutput(
            string instanceId,
            string definitionId,
            int outputIndex,
            long quantity,
            GameRarity rarity,
            int qualityScore)
        {
            InstanceId = instanceId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            OutputIndex = outputIndex;
            Quantity = quantity;
            Rarity = rarity;
            QualityScore = qualityScore;
        }

        public string InstanceId { get; }
        public string DefinitionId { get; }
        public int OutputIndex { get; }
        public long Quantity { get; }
        public GameRarity Rarity { get; }
        public int QualityScore { get; }
    }

    public sealed class MaterialRefund
    {
        public MaterialRefund(string itemInstanceId, long quantity)
        {
            ItemInstanceId = itemInstanceId ?? string.Empty;
            Quantity = quantity;
        }

        public string ItemInstanceId { get; }
        public long Quantity { get; }
    }

    public sealed class CraftingResult
    {
        public CraftingResult(
            IEnumerable<CraftingOutput> outputs,
            GameRarity rarity,
            int qualityScore,
            IEnumerable<string> affixes,
            long experienceGranted,
            long masteryExperienceGranted,
            int pityBefore,
            int pityAfter,
            bool mythicTriggered,
            IEnumerable<MaterialRefund> materialRefunds,
            CraftingProvenance provenance)
        {
            Outputs = new ReadOnlyCollection<CraftingOutput>(
                new List<CraftingOutput>(outputs ?? Array.Empty<CraftingOutput>()));
            Rarity = rarity;
            QualityScore = qualityScore;
            Affixes = new ReadOnlyCollection<string>(
                new List<string>(affixes ?? Array.Empty<string>()));
            ExperienceGranted = experienceGranted;
            MasteryExperienceGranted = masteryExperienceGranted;
            PityBefore = pityBefore;
            PityAfter = pityAfter;
            MythicTriggered = mythicTriggered;
            MaterialRefunds = new ReadOnlyCollection<MaterialRefund>(
                new List<MaterialRefund>(materialRefunds ?? Array.Empty<MaterialRefund>()));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
        }

        public IReadOnlyList<CraftingOutput> Outputs { get; }
        public GameRarity Rarity { get; }
        public int QualityScore { get; }
        public IReadOnlyList<string> Affixes { get; }
        public long ExperienceGranted { get; }
        public long MasteryExperienceGranted { get; }
        public int PityBefore { get; }
        public int PityAfter { get; }
        public bool MythicTriggered { get; }
        public IReadOnlyList<MaterialRefund> MaterialRefunds { get; }
        public CraftingProvenance Provenance { get; }
    }

    public sealed class ProfessionProgress
    {
        private readonly List<string> selectedMasteryNodes;
        private readonly HashSet<string> knownRecipeIds;

        public ProfessionProgress(
            CraftingProfession professionType,
            int level,
            long experience,
            ProfessionRank rank,
            ItemTier maximumUnlockedTier,
            long masteryExperience,
            int masteryPoints,
            IEnumerable<string> selectedMasteryNodes,
            ProfessionSpecialization specialization,
            ItemTier stationTier,
            int focusCurrent,
            int focusMaximum,
            int mythicPityCounter,
            IEnumerable<string> knownRecipeIds,
            long serverRevision,
            ProfessionProgressionTuning progressionTuning)
        {
            ProfessionType = professionType;
            Level = level;
            Experience = experience;
            Rank = rank;
            MaximumUnlockedTier = maximumUnlockedTier;
            MasteryExperience = masteryExperience;
            MasteryPoints = masteryPoints;
            this.selectedMasteryNodes = CopyUnique(selectedMasteryNodes, "node de maestria");
            Specialization = specialization;
            StationTier = stationTier;
            FocusCurrent = focusCurrent;
            FocusMaximum = focusMaximum;
            MythicPityCounter = mythicPityCounter;
            this.knownRecipeIds = new HashSet<string>(
                CopyUnique(knownRecipeIds, "receita"), StringComparer.Ordinal);
            ServerRevision = serverRevision;
            Validate(progressionTuning);
        }

        public CraftingProfession ProfessionType { get; }
        public int Level { get; private set; }
        public long Experience { get; private set; }
        public ProfessionRank Rank { get; private set; }
        public ItemTier MaximumUnlockedTier { get; private set; }
        public long MasteryExperience { get; private set; }
        public int MasteryPoints { get; private set; }
        public IReadOnlyList<string> SelectedMasteryNodes =>
            new ReadOnlyCollection<string>(selectedMasteryNodes);
        public ProfessionSpecialization Specialization { get; private set; }
        public ItemTier StationTier { get; private set; }
        public int FocusCurrent { get; private set; }
        public int FocusMaximum { get; private set; }
        public int MythicPityCounter { get; private set; }
        public IReadOnlyCollection<string> KnownRecipeIds => knownRecipeIds;
        public long ServerRevision { get; private set; }

        public bool KnowsRecipe(string recipeId)
        {
            return !string.IsNullOrWhiteSpace(recipeId) && knownRecipeIds.Contains(recipeId);
        }

        public bool HasMasteryNode(string nodeId)
        {
            return !string.IsNullOrWhiteSpace(nodeId) && selectedMasteryNodes.Contains(nodeId);
        }

        internal void ConsumeFocus(int amount)
        {
            if (amount < 0 || amount > FocusCurrent)
                throw new InvalidOperationException("Foco artesanal insuficiente.");
            FocusCurrent -= amount;
            AdvanceRevision();
        }

        internal void RestoreFocus(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            FocusCurrent = Math.Min(FocusMaximum, checked(FocusCurrent + amount));
            AdvanceRevision();
        }

        internal void SetSpecialization(ProfessionSpecialization value)
        {
            Specialization = value;
            AdvanceRevision();
        }

        internal void SetPityCounter(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            MythicPityCounter = value;
            AdvanceRevision();
        }

        internal void AddExperience(
            long amount,
            long masteryAmount,
            ProfessionProgressionTuning progressionTuning,
            CraftingExperienceTuning experienceTuning)
        {
            if (amount < 0 || masteryAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            Experience = checked(Experience + amount);
            while (Level < progressionTuning.maximumLevel)
            {
                long threshold = ProfessionProgression.GetCumulativeExperienceForLevel(
                    Level + 1, progressionTuning);
                if (Experience < threshold) break;
                Level++;
            }
            Rank = ProfessionProgression.GetRankForLevel(Level, progressionTuning);
            MaximumUnlockedTier = ProfessionProgression.GetMaximumUnlockedTier(
                Level, progressionTuning);

            if (Rank >= ProfessionRank.Master && masteryAmount > 0)
            {
                long previousPoints = MasteryExperience / experienceTuning.masteryExperiencePerPoint;
                MasteryExperience = checked(MasteryExperience + masteryAmount);
                long currentPoints = MasteryExperience / experienceTuning.masteryExperiencePerPoint;
                MasteryPoints = checked(MasteryPoints + (int)(currentPoints - previousPoints));
            }
            AdvanceRevision();
        }

        internal void LearnRecipe(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                throw new ArgumentException("recipeId é obrigatório.", nameof(recipeId));
            if (knownRecipeIds.Add(recipeId)) AdvanceRevision();
        }

        internal void UpgradeStation(ItemTier tier)
        {
            if (!tier.IsValid() || tier.ToNumber() < StationTier.ToNumber())
                throw new InvalidOperationException("Tier de estação inválido.");
            StationTier = tier;
            AdvanceRevision();
        }

        private void Validate(ProfessionProgressionTuning tuning)
        {
            if (!ProfessionType.IsCraftingProfession())
                throw new InvalidOperationException("Profissão inválida.");
            if (Level < 1 || Level > tuning.maximumLevel || Experience < 0 ||
                MasteryExperience < 0 || MasteryPoints < 0 || MythicPityCounter < 0 ||
                ServerRevision < 0)
                throw new InvalidOperationException("Progresso profissional inválido.");
            if (Rank != ProfessionProgression.GetRankForLevel(Level, tuning) ||
                MaximumUnlockedTier != ProfessionProgression.GetMaximumUnlockedTier(Level, tuning))
                throw new InvalidOperationException("Caches derivados de grau/Tier são inconsistentes.");
            if (!StationTier.IsValid() || FocusMaximum <= 0 || FocusCurrent < 0 ||
                FocusCurrent > FocusMaximum || !Enum.IsDefined(typeof(ProfessionSpecialization), Specialization))
                throw new InvalidOperationException("Estação, Foco ou especialização inválidos.");
        }

        private void AdvanceRevision()
        {
            ServerRevision = checked(ServerRevision + 1);
        }

        private static List<string> CopyUnique(IEnumerable<string> values, string label)
        {
            var result = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            if (values == null) return result;
            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value) || !unique.Add(value))
                    throw new InvalidOperationException($"{label} vazio ou duplicado.");
                result.Add(value);
            }
            return result;
        }
    }

    public sealed class CraftingJob
    {
        private readonly List<ReservedItemReference> reservedItemReferences;
        private readonly List<string> outputInstanceIds = new List<string>();

        public CraftingJob(
            string jobId,
            string ownerPlayerId,
            CraftingProfession profession,
            string recipeId,
            int quantity,
            CraftingJobStatus state,
            long startedAtServerTime,
            long completesAtServerTime,
            IEnumerable<ReservedItemReference> reservedItemReferences,
            string selectedToolInstanceId,
            string selectedCatalystInstanceId,
            long deterministicSeed,
            int expectedOutputCount,
            int rulesVersion,
            long serverRevision,
            string failureReason,
            CraftingProvenance provenance)
        {
            JobId = jobId ?? string.Empty;
            OwnerPlayerId = ownerPlayerId ?? string.Empty;
            Profession = profession;
            RecipeId = recipeId ?? string.Empty;
            Quantity = quantity;
            State = state;
            StartedAtServerTime = startedAtServerTime;
            CompletesAtServerTime = completesAtServerTime;
            this.reservedItemReferences = new List<ReservedItemReference>(
                reservedItemReferences ?? Array.Empty<ReservedItemReference>());
            SelectedToolInstanceId = selectedToolInstanceId ?? string.Empty;
            SelectedCatalystInstanceId = selectedCatalystInstanceId ?? string.Empty;
            DeterministicSeed = deterministicSeed;
            ExpectedOutputCount = expectedOutputCount;
            RulesVersion = rulesVersion;
            ServerRevision = serverRevision;
            FailureReason = failureReason ?? string.Empty;
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            Validate();
        }

        public string JobId { get; }
        public string OwnerPlayerId { get; }
        public CraftingProfession Profession { get; }
        public string RecipeId { get; }
        public int Quantity { get; }
        public CraftingJobStatus State { get; private set; }
        public long StartedAtServerTime { get; }
        public long CompletesAtServerTime { get; }
        public IReadOnlyList<ReservedItemReference> ReservedItemReferences =>
            new ReadOnlyCollection<ReservedItemReference>(reservedItemReferences);
        public string SelectedToolInstanceId { get; }
        public string SelectedCatalystInstanceId { get; }
        public long DeterministicSeed { get; }
        public int ExpectedOutputCount { get; }
        public IReadOnlyList<string> OutputInstanceIds =>
            new ReadOnlyCollection<string>(outputInstanceIds);
        public int RulesVersion { get; }
        public long ServerRevision { get; private set; }
        public string FailureReason { get; private set; }
        public CraftingProvenance Provenance { get; }
        public CraftingResult Result { get; private set; }

        public bool IsActive => State == CraftingJobStatus.Pending ||
                                State == CraftingJobStatus.Running ||
                                State == CraftingJobStatus.ReadyToClaim;

        internal void MarkReady(long now)
        {
            if (State == CraftingJobStatus.Running && now >= CompletesAtServerTime)
            {
                State = CraftingJobStatus.ReadyToClaim;
                ServerRevision = checked(ServerRevision + 1);
            }
        }

        internal void Complete(CraftingResult result)
        {
            if (State != CraftingJobStatus.ReadyToClaim)
                throw new InvalidOperationException("Job ainda não está pronto.");
            Result = result ?? throw new ArgumentNullException(nameof(result));
            outputInstanceIds.Clear();
            for (int i = 0; i < result.Outputs.Count; i++)
                outputInstanceIds.Add(result.Outputs[i].InstanceId);
            State = CraftingJobStatus.Completed;
            ServerRevision = checked(ServerRevision + 1);
        }

        internal void Cancel()
        {
            if (State == CraftingJobStatus.Completed)
                throw new InvalidOperationException("Job concluído não pode ser cancelado.");
            if (State == CraftingJobStatus.ReadyToClaim)
                throw new InvalidOperationException("Job pronto para coleta não pode ser cancelado.");
            if (State != CraftingJobStatus.Pending && State != CraftingJobStatus.Running)
                throw new InvalidOperationException("Estado do job não permite cancelamento.");
            State = CraftingJobStatus.Cancelled;
            ServerRevision = checked(ServerRevision + 1);
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(JobId) || string.IsNullOrWhiteSpace(OwnerPlayerId) ||
                string.IsNullOrWhiteSpace(RecipeId) || !Profession.IsCraftingProfession())
                throw new InvalidOperationException("Identidade do job inválida.");
            if (Quantity <= 0 || ExpectedOutputCount <= 0 ||
                CompletesAtServerTime < StartedAtServerTime || RulesVersion <= 0 ||
                ServerRevision < 0 || !Enum.IsDefined(typeof(CraftingJobStatus), State))
                throw new InvalidOperationException("Dados do job inválidos.");
        }
    }

    public sealed class CraftingQueue
    {
        private readonly List<CraftingJob> jobs = new List<CraftingJob>();

        public IReadOnlyList<CraftingJob> Jobs => new ReadOnlyCollection<CraftingJob>(jobs);

        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < jobs.Count; i++)
                    if (jobs[i].IsActive) count++;
                return count;
            }
        }

        public void Add(CraftingJob job, int slotCount)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (slotCount <= 0 || ActiveCount >= slotCount)
                throw new InvalidOperationException("Fila de crafting cheia.");
            for (int i = 0; i < jobs.Count; i++)
                if (string.Equals(jobs[i].JobId, job.JobId, StringComparison.Ordinal))
                    throw new InvalidOperationException("jobId duplicado.");
            jobs.Add(job);
        }

        public CraftingJob Get(string jobId)
        {
            for (int i = 0; i < jobs.Count; i++)
                if (string.Equals(jobs[i].JobId, jobId, StringComparison.Ordinal))
                    return jobs[i];
            throw new KeyNotFoundException($"Job não encontrado: {jobId}.");
        }

        public void Refresh(long now)
        {
            for (int i = 0; i < jobs.Count; i++) jobs[i].MarkReady(now);
        }
    }

    public sealed class CraftingEvent
    {
        public CraftingEvent(string eventType, string jobId, long serverTime)
        {
            EventType = eventType ?? string.Empty;
            JobId = jobId ?? string.Empty;
            ServerTime = serverTime;
        }

        public string EventType { get; }
        public string JobId { get; }
        public long ServerTime { get; }
    }
}
