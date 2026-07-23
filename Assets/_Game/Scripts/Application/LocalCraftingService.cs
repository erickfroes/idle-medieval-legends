using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Inventory;

namespace IdleMedievalLegends.Application
{
    /// <summary>
    /// Carteira de ouro simulada. Produção deve substituir esta porta por uma
    /// transação autoritativa e idempotente no backend.
    /// </summary>
    public interface IGoldEconomyService
    {
        long GoldBalance { get; }
        bool TrySpend(long amount, string reasonId);
        void Credit(long amount, string reasonId);
    }

    public sealed class LocalGoldEconomyService : IGoldEconomyService
    {
        public LocalGoldEconomyService(long initialGold)
        {
            if (initialGold < 0) throw new ArgumentOutOfRangeException(nameof(initialGold));
            GoldBalance = initialGold;
        }

        public long GoldBalance { get; private set; }

        public bool TrySpend(long amount, string reasonId)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (string.IsNullOrWhiteSpace(reasonId))
                throw new ArgumentException("reasonId é obrigatório.", nameof(reasonId));
            if (GoldBalance < amount) return false;
            GoldBalance -= amount;
            return true;
        }

        public void Credit(long amount, string reasonId)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (string.IsNullOrWhiteSpace(reasonId))
                throw new ArgumentException("reasonId é obrigatório.", nameof(reasonId));
            GoldBalance = checked(GoldBalance + amount);
        }
    }

    public interface ICraftingSeedGenerator
    {
        long CreateSeed(string jobId, long serverTime);
    }

    public sealed class LocalCraftingSeedGenerator : ICraftingSeedGenerator
    {
        private long sequence;

        public long CreateSeed(string jobId, long serverTime)
        {
            ulong hash = 14695981039346656037UL;
            string text = $"{jobId}:{serverTime}:{sequence++}";
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 1099511628211UL;
            }
            return unchecked((long)hash);
        }
    }

    public sealed class CraftingCancellationResult
    {
        public CraftingCancellationResult(
            CraftingJob job,
            long goldRefunded,
            int focusRefunded,
            IEnumerable<MaterialRefund> materialRefunds)
        {
            Job = job;
            GoldRefunded = goldRefunded;
            FocusRefunded = focusRefunded;
            MaterialRefunds = new ReadOnlyCollection<MaterialRefund>(
                new List<MaterialRefund>(materialRefunds));
        }

        public CraftingJob Job { get; }
        public long GoldRefunded { get; }
        public int FocusRefunded { get; }
        public IReadOnlyList<MaterialRefund> MaterialRefunds { get; }
    }

    /// <summary>
    /// Vertical slice local. Ele simula o limite transacional do servidor para
    /// testes e UX; nunca deve ser tratado como prova de propriedade em produção.
    /// </summary>
    public sealed class LocalCraftingService
    {
        private readonly PlayerInventory inventory;
        private readonly ContentCatalogLookup catalog;
        private readonly IServerClock clock;
        private readonly IGoldEconomyService economy;
        private readonly ICraftingSeedGenerator seedGenerator;
        private readonly ProfessionProgressionTuning progressionTuning;
        private readonly CraftingRuntimeTuning tuning;
        private readonly Dictionary<CraftingProfession, ProfessionProgress> progressByProfession;
        private readonly Dictionary<string, JobCosts> costsByJob =
            new Dictionary<string, JobCosts>(StringComparer.Ordinal);
        private readonly List<CraftingEvent> events = new List<CraftingEvent>();
        private long lastSpecializationChangedAt = -1;

        public LocalCraftingService(
            string playerId,
            PlayerInventory inventory,
            ContentCatalogLookup catalog,
            IServerClock clock,
            IGoldEconomyService economy,
            ICraftingSeedGenerator seedGenerator,
            ProfessionProgressionTuning progressionTuning,
            CraftingRuntimeTuning tuning,
            IEnumerable<ProfessionProgress> progress,
            bool allowDisabledTestRecipes = false)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS)
            throw new PlatformNotSupportedException(
                "LocalCraftingService é um protótipo exclusivo de desenvolvimento.");
#else
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("playerId é obrigatório.", nameof(playerId));
            PlayerId = playerId;
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
            this.seedGenerator = seedGenerator ?? throw new ArgumentNullException(nameof(seedGenerator));
            this.progressionTuning = progressionTuning ??
                throw new ArgumentNullException(nameof(progressionTuning));
            this.tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            CraftingRules.ValidateRuntimeTuning(tuning);
            ProfessionProgression.ValidateTuning(progressionTuning);
            AllowDisabledTestRecipes = allowDisabledTestRecipes;
            progressByProfession = new Dictionary<CraftingProfession, ProfessionProgress>();
            foreach (ProfessionProgress entry in progress ?? Array.Empty<ProfessionProgress>())
            {
                if (entry == null || progressByProfession.ContainsKey(entry.ProfessionType))
                    throw new InvalidOperationException("Progresso profissional nulo ou duplicado.");
                progressByProfession.Add(entry.ProfessionType, entry);
            }
            if (progressByProfession.Count != 5)
                throw new InvalidOperationException("As cinco profissões são obrigatórias.");
            foreach (ProfessionProgress entry in progressByProfession.Values)
            {
                if (entry.Specialization == ProfessionSpecialization.Primary)
                {
                    if (lastSpecializationChangedAt >= 0)
                        throw new InvalidOperationException("Somente uma profissão pode ser principal.");
                    lastSpecializationChangedAt = clock.UtcNowUnixMilliseconds;
                }
            }
#endif
        }

        public string PlayerId { get; }
        public CraftingQueue Queue { get; } = new CraftingQueue();
        public bool AllowDisabledTestRecipes { get; }
        public long GoldBalance => economy.GoldBalance;
        public long ServerTime => clock.UtcNowUnixMilliseconds;
        public IReadOnlyCollection<ProfessionProgress> Progress => progressByProfession.Values;
        public IReadOnlyList<CraftingEvent> Events => new ReadOnlyCollection<CraftingEvent>(events);

        public ProfessionProgress GetProgress(CraftingProfession profession)
        {
            if (!progressByProfession.TryGetValue(profession, out ProfessionProgress value))
                throw new KeyNotFoundException($"Profissão inexistente: {profession}.");
            return value;
        }

        public int GetQueueSlotCount(CraftingProfession profession)
        {
            ProfessionProgress progress = GetProgress(profession);
            int nodeBonus = progress.HasMasteryNode("commerce_queue_1") ? 1 : 0;
            return CraftingRuntimeRules.GetQueueSlotCount(progress, nodeBonus, tuning);
        }

        public CraftingJob StartCraft(
            CraftingProfession selectedProfession,
            string recipeId,
            int quantity,
            string selectedToolInstanceId = "",
            string selectedCatalystInstanceId = "")
        {
            if (!catalog.TryGetRecipe(recipeId, out RecipeDefinition recipe))
                throw Reject(CraftingEligibilityCode.InvalidRecipe, "Receita não existe.");
            if (!recipe.EnabledForNormalGameplay && !AllowDisabledTestRecipes)
                throw Reject(CraftingEligibilityCode.InvalidRecipe,
                    "Receita de teste está desabilitada no gameplay normal.");
            if (quantity <= 0 || quantity > tuning.maximumRecipeQuantity)
                throw Reject(CraftingEligibilityCode.InvalidQuantity, "Quantidade da receita inválida.");
            if (!progressByProfession.TryGetValue(selectedProfession, out ProfessionProgress progress) ||
                recipe.Profession.ToLegacyProfession() != selectedProfession)
                throw Reject(CraftingEligibilityCode.WrongProfession,
                    "A receita pertence a outra profissão.");
            if (!progress.KnowsRecipe(recipeId))
                throw Reject(CraftingEligibilityCode.RecipeLocked, "Receita não foi aprendida.");
            if (progress.Level < recipe.RequiredProfessionLevel)
                throw Reject(CraftingEligibilityCode.ProfessionLevelTooLow,
                    "Nível profissional insuficiente.");
            if (progress.Rank < recipe.RequiredProfessionRank)
                throw Reject(CraftingEligibilityCode.RankTooLow, "Grau profissional insuficiente.");
            if (progress.MaximumUnlockedTier.ToNumber() < (int)recipe.RequiredTier)
                throw Reject(CraftingEligibilityCode.TierLocked, "Tier ainda não foi liberado.");
            if (progress.StationTier.ToNumber() < (int)recipe.RequiredStationTier)
                throw Reject(CraftingEligibilityCode.StationTierTooLow,
                    "Tier da estação é insuficiente.");

            int slots = GetQueueSlotCount(selectedProfession);
            if (Queue.ActiveCount >= slots)
                throw Reject(CraftingEligibilityCode.QueueFull, "Fila de crafting cheia.");

            int focusReduction = progress.HasMasteryNode("efficiency_focus_1")
                ? 1000
                : 0;
            int focusCost = CraftingRuntimeRules.CalculateFocusCost(
                recipe, quantity, focusReduction, tuning);
            if (progress.FocusCurrent < focusCost)
                throw Reject(CraftingEligibilityCode.InsufficientFocus,
                    "Foco artesanal insuficiente.");
            long goldCost = checked(recipe.GoldCost * quantity);
            if (economy.GoldBalance < goldCost)
                throw Reject(CraftingEligibilityCode.InsufficientGold, "Ouro insuficiente.");

            List<ReservedItemReference> reservations = BuildReservations(
                recipe,
                quantity,
                selectedToolInstanceId,
                selectedCatalystInstanceId);
            string jobId = $"craft_{Guid.NewGuid():N}";
            long now = clock.UtcNowUnixMilliseconds;
            long seed = seedGenerator.CreateSeed(jobId, now);
            int duration = CraftingRuntimeRules.CalculateDurationSeconds(
                recipe,
                progress,
                progress.HasMasteryNode("efficiency_duration_1") ? 500 : 0,
                tuning);
            long completesAt = checked(now + duration * 1000L);
            ItemDefinition outputDefinition = catalog.GetItem(recipe.OutputDefinitionId);
            long outputUnits = checked(recipe.OutputQuantity * quantity);
            int expectedOutputCount = outputDefinition.Stackable
                ? 1
                : checked((int)outputUnits);
            var provenance = new CraftingProvenance(
                jobId, recipeId, PlayerId, HashSeed(seed), tuning.rulesVersion);
            var job = new CraftingJob(
                jobId, PlayerId, selectedProfession, recipeId, quantity,
                CraftingJobStatus.Running, now, completesAt, reservations,
                selectedToolInstanceId, selectedCatalystInstanceId, seed,
                expectedOutputCount, tuning.rulesVersion, 0, string.Empty, provenance);

            var successfullyReserved = new List<ReservedItemReference>();
            bool goldSpent = false;
            bool focusConsumed = false;
            try
            {
                for (int i = 0; i < reservations.Count; i++)
                {
                    inventory.Reserve(reservations[i].ItemInstanceId, jobId, now);
                    successfullyReserved.Add(reservations[i]);
                }
                progress.ConsumeFocus(focusCost);
                focusConsumed = true;
                if (!economy.TrySpend(goldCost, jobId))
                    throw Reject(CraftingEligibilityCode.InsufficientGold, "Ouro insuficiente.");
                goldSpent = true;
                Queue.Add(job, slots);
            }
            catch
            {
                for (int i = 0; i < successfullyReserved.Count; i++)
                {
                    ItemInstance item = inventory.GetItem(successfullyReserved[i].ItemInstanceId);
                    if (item.State == InventoryItemState.ReservedByServer &&
                        item.ReservationId == jobId)
                        inventory.ReleaseReservation(item.InstanceId, jobId, now);
                }
                if (focusConsumed)
                    progress.RestoreFocus(focusCost);
                if (goldSpent) economy.Credit(goldCost, $"rollback:{jobId}");
                throw;
            }

            costsByJob.Add(jobId, new JobCosts(focusCost, goldCost));
            events.Add(new CraftingEvent("craft_started", jobId, now));
            return job;
        }

        public void Refresh()
        {
            Queue.Refresh(clock.UtcNowUnixMilliseconds);
        }

        public CraftingCancellationResult CancelCraft(string jobId)
        {
            CraftingJob job = Queue.Get(jobId);
            if (!tuning.cancellation.enabled)
                throw new InvalidOperationException("Cancelamento está desabilitado.");
            if (job.State == CraftingJobStatus.Completed)
                throw new InvalidOperationException("Job concluído não pode ser cancelado.");
            Refresh();
            if (job.State == CraftingJobStatus.ReadyToClaim)
                throw new InvalidOperationException("O limite temporal de cancelamento expirou.");
            long now = clock.UtcNowUnixMilliseconds;
            long duration = Math.Max(1, job.CompletesAtServerTime - job.StartedAtServerTime);
            long elapsed = Math.Max(0, now - job.StartedAtServerTime);
            long progressBasisPoints = Math.Min(10000, elapsed * 10000 / duration);
            if (progressBasisPoints > tuning.cancellation.latestCancellationProgressBasisPoints)
                throw new InvalidOperationException("O limite temporal de cancelamento expirou.");

            JobCosts costs = costsByJob[jobId];
            var refunds = new List<MaterialRefund>();
            if (tuning.cancellation.returnMaterials)
            {
                for (int i = 0; i < job.ReservedItemReferences.Count; i++)
                {
                    ReservedItemReference reference = job.ReservedItemReferences[i];
                    inventory.ReleaseReservation(reference.ItemInstanceId, job.JobId, now);
                    refunds.Add(new MaterialRefund(reference.ItemInstanceId, reference.Quantity));
                }
            }
            else
            {
                for (int i = 0; i < job.ReservedItemReferences.Count; i++)
                {
                    ReservedItemReference reference = job.ReservedItemReferences[i];
                    if (reference.Role == CraftingReservationRole.Tool)
                        inventory.ReleaseReservation(reference.ItemInstanceId, job.JobId, now);
                    else
                        inventory.ConsumeReservation(
                            reference.ItemInstanceId, job.JobId, reference.Quantity, now);
                }
            }
            int focusRefund = tuning.cancellation.refundFocus ? costs.Focus : 0;
            if (focusRefund > 0) GetProgress(job.Profession).RestoreFocus(focusRefund);
            long goldRefund = CraftingRuntimeRules.MultiplyBasisPoints(
                costs.Gold, tuning.cancellation.goldRefundBasisPoints);
            if (goldRefund > 0) economy.Credit(goldRefund, $"cancel:{job.JobId}");
            job.Cancel();
            events.Add(new CraftingEvent("craft_cancelled", job.JobId, now));
            return new CraftingCancellationResult(job, goldRefund, focusRefund, refunds);
        }

        public CraftingResult CompleteCraft(string jobId)
        {
            CraftingJob job = Queue.Get(jobId);
            if (job.State == CraftingJobStatus.Completed) return job.Result;
            Refresh();
            if (job.State != CraftingJobStatus.ReadyToClaim)
                throw new InvalidOperationException("Job ainda não está pronto para coleta.");

            RecipeDefinition recipe = catalog.GetRecipe(job.RecipeId);
            ProfessionProgress progress = GetProgress(job.Profession);
            bool hasDivineCatalyst = HasDivineCatalyst(job);
            bool mythicEligible = CraftingRuntimeRules.IsMythicEligible(
                recipe, progress, hasDivineCatalyst);
            int qualityScore = CraftingRuntimeRules.CalculateQualityScore(
                recipe,
                progress,
                string.IsNullOrWhiteSpace(job.SelectedToolInstanceId) ? 0 : 2,
                string.IsNullOrWhiteSpace(job.SelectedCatalystInstanceId) ? 0 : 2,
                progress.HasMasteryNode("excellence_quality_1") ? 3 : 0,
                tuning);
            CraftingQualityBand band = CraftingRuntimeRules.GetQualityBand(
                qualityScore, progress.Rank, tuning);
            int pityBefore = progress.MythicPityCounter;
            RarityWeightSet weights = CraftingRules.BuildConfiguredRarityWeights(
                recipe.RequiredTier.ToLegacyTier(), band, mythicEligible, pityBefore, tuning);
            var random = new DeterministicCraftingRandom(job.DeterministicSeed);
            GameRarity rarity = CraftingRules.RollRarity(weights, random.NextBasisPoints());
            bool mythicTriggered = rarity == GameRarity.Mythic;
            int pityAfter = pityBefore;
            if (mythicEligible)
                pityAfter = mythicTriggered ? 0 : checked(pityBefore + 1);

            long now = clock.UtcNowUnixMilliseconds;
            ItemDefinition definition = catalog.GetItem(recipe.OutputDefinitionId);
            List<CraftingOutput> outputs = BuildOutputs(
                job, recipe, definition, rarity, qualityScore, now);
            ValidateExistingOutputs(job, recipe, outputs);
            for (int i = 0; i < job.ReservedItemReferences.Count; i++)
            {
                ReservedItemReference reference = job.ReservedItemReferences[i];
                if (reference.ConsumedAtCompletion)
                    inventory.ConsumeReservation(
                        reference.ItemInstanceId, job.JobId, reference.Quantity, now);
                else
                    inventory.ReleaseReservation(reference.ItemInstanceId, job.JobId, now);
            }
            MaterializeOutputs(job, recipe, definition, outputs, now);

            long experience = CraftingRuntimeRules.CalculateExperience(
                recipe, progress, job.Quantity, tuning);
            long masteryExperience = CraftingRuntimeRules.CalculateMasteryExperience(
                recipe, progress, job.Quantity, tuning);
            progress.SetPityCounter(pityAfter);
            progress.AddExperience(
                experience, masteryExperience, progressionTuning, tuning.experience);
            IReadOnlyList<string> affixes = BuildAffixes(rarity);
            var result = new CraftingResult(
                outputs, rarity, qualityScore, affixes, experience, masteryExperience,
                pityBefore, pityAfter, mythicTriggered, Array.Empty<MaterialRefund>(),
                job.Provenance);
            job.Complete(result);
            events.Add(new CraftingEvent("craft_completed", job.JobId, now));
            return result;
        }

        public void SelectPrimaryProfession(CraftingProfession profession)
        {
            ProfessionProgress target = GetProgress(profession);
            if (target.Specialization == ProfessionSpecialization.Primary)
                throw new InvalidOperationException("A profissão já é a especialização principal.");
            long now = clock.UtcNowUnixMilliseconds;
            bool isInitialSelection = lastSpecializationChangedAt < 0;
            if (!isInitialSelection)
            {
                long availableAt = checked(lastSpecializationChangedAt +
                                           tuning.specializationCooldownSeconds * 1000L);
                if (now < availableAt)
                    throw new InvalidOperationException("Cooldown de especialização ainda ativo.");
                if (!economy.TrySpend(
                    tuning.specializationChangeGoldCost,
                    $"specialization:{profession}:{now}"))
                    throw new InvalidOperationException("Ouro insuficiente para trocar especialização.");
            }
            foreach (ProfessionProgress progress in progressByProfession.Values)
                progress.SetSpecialization(
                    progress == target
                        ? ProfessionSpecialization.Primary
                        : ProfessionSpecialization.None);
            lastSpecializationChangedAt = now;
        }

        private List<ReservedItemReference> BuildReservations(
            RecipeDefinition recipe,
            int quantity,
            string toolId,
            string catalystId)
        {
            var result = new List<ReservedItemReference>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                RecipeIngredientDefinition ingredient = recipe.Ingredients[i];
                if (ingredient.Optional) continue;
                long required = checked(ingredient.Quantity * quantity);
                ReserveDefinition(
                    ingredient.ItemDefinitionId,
                    required,
                    ingredient.Consumed,
                    CraftingReservationRole.Material,
                    used,
                    result);
            }
            if (!string.IsNullOrWhiteSpace(toolId))
            {
                ItemInstance tool = ValidateSelectedItem(toolId, CraftingReservationRole.Tool);
                if (tool.Kind != InventoryItemKind.CraftingTool)
                    throw Reject(CraftingEligibilityCode.ItemUnavailable,
                        "Ferramenta selecionada não é uma ferramenta de crafting.");
                used.Add(toolId);
                result.Add(new ReservedItemReference(
                    toolId, 1, CraftingReservationRole.Tool, false));
            }
            if (!string.IsNullOrWhiteSpace(catalystId))
            {
                ItemInstance catalyst = ValidateSelectedItem(
                    catalystId, CraftingReservationRole.Catalyst);
                RecipeIngredientDefinition catalystDefinition = null;
                for (int i = 0; i < recipe.OptionalCatalysts.Count; i++)
                {
                    if (recipe.OptionalCatalysts[i].ItemDefinitionId == catalyst.DefinitionId)
                    {
                        catalystDefinition = recipe.OptionalCatalysts[i];
                        break;
                    }
                }
                if (catalystDefinition == null)
                    throw Reject(CraftingEligibilityCode.ItemUnavailable,
                        "Catalisador não é aceito pela receita.");
                if (!used.Add(catalystId))
                    throw Reject(CraftingEligibilityCode.ItemUnavailable,
                        "Uma instância não pode ocupar dois papéis no mesmo job.");
                long catalystQuantity = checked(catalystDefinition.Quantity * quantity);
                if (catalyst.Quantity < catalystQuantity)
                    throw Reject(CraftingEligibilityCode.InsufficientMaterials,
                        "Quantidade do catalisador selecionado é insuficiente.");
                result.Add(new ReservedItemReference(
                    catalystId,
                    catalystQuantity,
                    CraftingReservationRole.Catalyst,
                    catalystDefinition.Consumed));
            }
            return result;
        }

        private void ReserveDefinition(
            string definitionId,
            long required,
            bool consumed,
            CraftingReservationRole role,
            HashSet<string> used,
            List<ReservedItemReference> result)
        {
            long remaining = required;
            for (int i = 0; i < inventory.Items.Count && remaining > 0; i++)
            {
                ItemInstance item = inventory.Items[i];
                if (item.DefinitionId != definitionId || item.IsTerminal || used.Contains(item.InstanceId))
                    continue;
                if (item.State != InventoryItemState.Owned || item.LockedByPlayer ||
                    item.OwnerPlayerId != PlayerId)
                    continue;
                long take = Math.Min(remaining, item.Quantity);
                result.Add(new ReservedItemReference(item.InstanceId, take, role, consumed));
                used.Add(item.InstanceId);
                remaining -= take;
            }
            if (remaining > 0)
                throw Reject(CraftingEligibilityCode.InsufficientMaterials,
                    $"Material insuficiente: {definitionId}.");
        }

        private ItemInstance ValidateSelectedItem(string instanceId, CraftingReservationRole role)
        {
            if (!inventory.TryGetItem(instanceId, out ItemInstance item))
                throw Reject(CraftingEligibilityCode.ItemUnavailable,
                    $"{role} selecionado não existe.");
            if (item.OwnerPlayerId != PlayerId || item.State != InventoryItemState.Owned ||
                item.LockedByPlayer)
                throw Reject(CraftingEligibilityCode.ItemUnavailable,
                    $"{role} está bloqueado, equipado, reservado ou em escrow.");
            return item;
        }

        private List<CraftingOutput> BuildOutputs(
            CraftingJob job,
            RecipeDefinition recipe,
            ItemDefinition definition,
            GameRarity rarity,
            int qualityScore,
            long now)
        {
            long units = checked(recipe.OutputQuantity * job.Quantity);
            int count = definition.Stackable ? 1 : checked((int)units);
            var outputs = new List<CraftingOutput>(count);
            for (int i = 0; i < count; i++)
            {
                string instanceId = $"{job.JobId}_output_{i:D4}";
                outputs.Add(new CraftingOutput(
                    instanceId, definition.DefinitionId, i,
                    definition.Stackable ? units : 1,
                    rarity, qualityScore));
            }
            return outputs;
        }

        private void MaterializeOutputs(
            CraftingJob job,
            RecipeDefinition recipe,
            ItemDefinition definition,
            IReadOnlyList<CraftingOutput> outputs,
            long now)
        {
            for (int i = 0; i < outputs.Count; i++)
            {
                CraftingOutput output = outputs[i];
                if (inventory.TryGetItem(output.InstanceId, out _)) continue;
                ItemInstance item = ItemInstance.CreateCrafted(
                    output.InstanceId,
                    output.DefinitionId,
                    PlayerId,
                    ToKind(definition.ItemType),
                    output.Rarity,
                    definition.Tier.ToLegacyTier(),
                    output.Quantity,
                    definition.Stackable,
                    job.DeterministicSeed,
                    job.Provenance.SeedHash,
                    now,
                    job.Profession,
                    recipe.RecipeId,
                    job.JobId,
                    output.QualityScore * 100);
                inventory.AddAuthorizedItem(item, definition, now);
            }
        }

        private void ValidateExistingOutputs(
            CraftingJob job,
            RecipeDefinition recipe,
            IReadOnlyList<CraftingOutput> outputs)
        {
            string expectedTransactionId = $"craft:{job.JobId}";
            for (int i = 0; i < outputs.Count; i++)
            {
                CraftingOutput output = outputs[i];
                if (!inventory.TryGetItem(output.InstanceId, out ItemInstance existing))
                    continue;
                bool belongsToThisJob =
                    string.Equals(existing.OwnerPlayerId, PlayerId, StringComparison.Ordinal) &&
                    string.Equals(existing.DefinitionId, output.DefinitionId, StringComparison.Ordinal) &&
                    string.Equals(existing.RecipeId, recipe.RecipeId, StringComparison.Ordinal) &&
                    string.Equals(existing.OriginTransactionId, expectedTransactionId,
                        StringComparison.Ordinal) &&
                    existing.Provenance != null &&
                    string.Equals(existing.Provenance.SourceId, job.JobId,
                        StringComparison.Ordinal);
                if (!belongsToThisJob)
                {
                    throw new InvalidOperationException(
                        $"Colisão de instanceId na saída lógica {job.JobId}+{i}.");
                }
            }
        }

        private IReadOnlyList<string> BuildAffixes(GameRarity rarity)
        {
            int count = catalog.GetRarity(rarity.ToCatalogRarity()).MaximumAffixCount;
            var affixes = new List<string>(count);
            for (int i = 0; i < count; i++) affixes.Add($"affix_generated_{i + 1}");
            return affixes;
        }

        private bool HasDivineCatalyst(CraftingJob job)
        {
            if (string.IsNullOrWhiteSpace(job.SelectedCatalystInstanceId)) return false;
            return inventory.GetItem(job.SelectedCatalystInstanceId).DefinitionId ==
                   tuning.divineCatalystDefinitionId;
        }

        private static InventoryItemKind ToKind(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Equipment: return InventoryItemKind.Equipment;
                case ItemType.Material: return InventoryItemKind.Material;
                case ItemType.Consumable: return InventoryItemKind.Consumable;
                case ItemType.Enchantment: return InventoryItemKind.Enchantment;
                case ItemType.Tool: return InventoryItemKind.CraftingTool;
                default: throw new InvalidOperationException(
                    $"Tipo de saída não suportado: {itemType}.");
            }
        }

        private static CraftingCommandException Reject(
            CraftingEligibilityCode code,
            string message)
        {
            return new CraftingCommandException(code, message);
        }

        private static string HashSeed(long seed)
        {
            ulong value = unchecked((ulong)seed);
            value ^= value >> 33;
            value *= 0xff51afd7ed558ccdUL;
            value ^= value >> 33;
            return value.ToString("X16");
        }

        private readonly struct JobCosts
        {
            public JobCosts(int focus, long gold)
            {
                Focus = focus;
                Gold = gold;
            }

            public int Focus { get; }
            public long Gold { get; }
        }
    }
}
