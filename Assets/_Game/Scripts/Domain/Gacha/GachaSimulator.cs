#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Content;

namespace IdleMedievalLegends.Domain.Gacha
{
    /// <summary>
    /// Moeda sem valor real, compilada apenas em Editor, Development Build e testes.
    /// Não representa saldo de Gemas nem autoridade de produção.
    /// </summary>
    public sealed class DevelopmentGachaCurrency
    {
        public DevelopmentGachaCurrency(long balance)
        {
            if (balance < 0)
                throw new ArgumentOutOfRangeException(nameof(balance));
            Balance = balance;
        }

        public long Balance { get; private set; }

        public void Credit(long amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            Balance = checked(Balance + amount);
        }

        internal void EnsureCanSpend(long amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (Balance < amount)
                throw new InvalidOperationException(
                    "DevelopmentGachaCurrency insuficiente.");
        }

        internal void Spend(long amount)
        {
            EnsureCanSpend(amount);
            Balance -= amount;
        }
    }

    public sealed class GachaSimulator
    {
        private readonly DevelopmentGachaCurrency currency;
        private readonly FragmentWallet fragmentWallet;
        private readonly ISet<string> heroDefinitionIds;
        private readonly ISet<string> unlockedHeroDefinitionIds;
        private readonly Dictionary<string, GachaPityState> pityByKey =
            new Dictionary<string, GachaPityState>(StringComparer.Ordinal);
        private readonly Dictionary<string, ProcessedRequest> requests =
            new Dictionary<string, ProcessedRequest>(StringComparer.Ordinal);
        private readonly List<GachaHistoryEntry> history =
            new List<GachaHistoryEntry>();

        public GachaSimulator(
            DevelopmentGachaCurrency currency,
            FragmentWallet fragmentWallet,
            ISet<string> heroDefinitionIds,
            ISet<string> unlockedHeroDefinitionIds = null)
        {
            this.currency = currency ?? throw new ArgumentNullException(nameof(currency));
            this.fragmentWallet =
                fragmentWallet ?? throw new ArgumentNullException(nameof(fragmentWallet));
            this.heroDefinitionIds = heroDefinitionIds;
            this.unlockedHeroDefinitionIds = unlockedHeroDefinitionIds ??
                new HashSet<string>(StringComparer.Ordinal);
        }

        public DevelopmentGachaCurrency Currency => currency;
        public FragmentWallet FragmentWallet => fragmentWallet;
        public IReadOnlyList<GachaHistoryEntry> History => history.AsReadOnly();

        public GachaPityState GetPityState(GachaBannerDefinition banner)
        {
            if (banner == null) throw new ArgumentNullException(nameof(banner));
            return pityByKey.TryGetValue(banner.PityKey, out GachaPityState state)
                ? state.Copy()
                : GachaPityState.Create(banner.PityKey);
        }

        public void RestorePityState(GachaPityState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.Validate();
            pityByKey[state.BannerOrGroupId] = state.Copy();
        }

        public GachaPullResult Execute(
            GachaBannerDefinition banner,
            GachaPullRequest request)
        {
            GachaProbabilityValidator.Validate(banner, heroDefinitionIds).ThrowIfInvalid();
            GachaProbabilityValidator.ValidateRequest(banner, request);
            if (requests.TryGetValue(request.RequestId, out ProcessedRequest processed))
            {
                processed.ValidateReplay(request);
                return processed.Result.AsReplay();
            }

            long cost = request.PullCount == 1
                ? banner.CostSingle
                : banner.CostMulti;
            currency.EnsureCanSpend(cost);

            GachaPityState state = GetPityState(banner);
            var random = new DeterministicGachaRandom(request.Seed);
            var rewards = new List<GachaReward>(request.PullCount);
            var entries = new List<GachaHistoryEntry>(request.PullCount);
            var unlockedDuringRequest = new HashSet<string>(
                unlockedHeroDefinitionIds,
                StringComparer.Ordinal);
            for (int sequence = 0; sequence < request.PullCount; sequence++)
            {
                GachaPityState before = state;
                GachaReward reward = RollOne(
                    banner,
                    request.PlayerProgress,
                    ref random,
                    before,
                    out state);
                reward = ResolveDuplicate(
                    banner,
                    reward,
                    unlockedDuringRequest);
                rewards.Add(reward);
                entries.Add(new GachaHistoryEntry(
                    request.RequestId,
                    banner.BannerId,
                    sequence,
                    reward,
                    before,
                    state,
                    request.Seed,
                    banner.RulesVersion,
                    checked(request.LogicalTimestamp + sequence)));
            }

            fragmentWallet.CreditRewards(rewards);
            currency.Spend(cost);
            for (int i = 0; i < rewards.Count; i++)
            {
                GachaReward reward = rewards[i];
                if (reward.RewardType == GachaRewardType.DirectHeroUnlock)
                    unlockedHeroDefinitionIds.Add(reward.HeroDefinitionId);
            }
            pityByKey[banner.PityKey] = state.Copy();
            history.AddRange(entries);
            var result = new GachaPullResult(
                request.RequestId,
                banner.BannerId,
                cost,
                currency.Balance,
                false,
                rewards,
                entries,
                state);
            requests.Add(request.RequestId, new ProcessedRequest(request, result));
            return result;
        }

        public void MarkHeroUnlockedForSimulation(string heroDefinitionId)
        {
            if (string.IsNullOrWhiteSpace(heroDefinitionId) ||
                (heroDefinitionIds != null &&
                 !heroDefinitionIds.Contains(heroDefinitionId)))
            {
                throw new ArgumentException(
                    "heroDefinitionId inválido.",
                    nameof(heroDefinitionId));
            }
            unlockedHeroDefinitionIds.Add(heroDefinitionId);
        }

        public static GachaSimulationReport SimulateAggregate(
            GachaBannerDefinition banner,
            long pullCount,
            long seed,
            int playerProgress,
            ISet<string> heroDefinitionIds,
            IReadOnlyDictionary<string, long> unlockCostsByHero)
        {
            GachaProbabilityValidator.Validate(banner, heroDefinitionIds).ThrowIfInvalid();
            if (pullCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(pullCount));
            if (playerProgress < 0)
                throw new ArgumentOutOfRangeException(nameof(playerProgress));

            var rewardCounts = new Dictionary<string, long>(StringComparer.Ordinal);
            var rarityCounts = new long[6];
            var fragments = new Dictionary<string, long>(StringComparer.Ordinal);
            var highIntervals = new List<int>();
            var random = new DeterministicGachaRandom(seed);
            IReadOnlyList<GachaPoolEntry> eligible =
                BuildEligibleEntries(banner, playerProgress);
            int pullsSinceHighRarity = 0;
            bool featuredGuarantee = false;
            long hardPityActivations = 0;
            long highRarityCount = 0;
            long featuredHighRarityCount = 0;
            double intervalMean = 0d;
            double intervalM2 = 0d;
            int currentInterval = 0;
            int maximumInterval = 0;

            for (long i = 0; i < pullCount; i++)
            {
                currentInterval++;
                RollSelection selection = SelectOneCore(
                    banner,
                    eligible,
                    ref random,
                    pullsSinceHighRarity,
                    featuredGuarantee,
                    out pullsSinceHighRarity,
                    out featuredGuarantee);
                GachaPoolEntry selected = selection.Entry;
                Increment(rewardCounts, selected.EntryId, 1);
                rarityCounts[(int)selected.Rarity] =
                    checked(rarityCounts[(int)selected.Rarity] + 1);
                if (selected.RewardType == GachaRewardType.HeroFragments)
                {
                    Increment(
                        fragments,
                        selected.HeroDefinitionId,
                        selected.FragmentQuantity);
                }
                if (selection.HardPityApplied)
                    hardPityActivations = checked(hardPityActivations + 1);
                if (selected.Rarity >= banner.HighRarityThreshold)
                {
                    highRarityCount++;
                    if (selection.Featured)
                        featuredHighRarityCount++;
                    maximumInterval = Math.Max(maximumInterval, currentInterval);
                    highIntervals.Add(currentInterval);
                    double delta = currentInterval - intervalMean;
                    intervalMean += delta / highRarityCount;
                    intervalM2 += delta * (currentInterval - intervalMean);
                    currentInterval = 0;
                }
            }

            var rewardFrequencies = new List<GachaFrequency>();
            foreach (GachaPoolEntry entry in banner.PoolEntries)
            {
                rewardFrequencies.Add(new GachaFrequency(
                    entry.EntryId,
                    rewardCounts.TryGetValue(entry.EntryId, out long count) ? count : 0));
            }
            var rarityFrequencies = new List<GachaFrequency>(6);
            for (int i = 0; i < rarityCounts.Length; i++)
                rarityFrequencies.Add(new GachaFrequency(((Rarity)i).ToString(), rarityCounts[i]));

            var fragmentAverages = new List<GachaAverageFragments>();
            foreach (KeyValuePair<string, long> value in fragments)
            {
                fragmentAverages.Add(new GachaAverageFragments(
                    value.Key,
                    value.Value,
                    value.Value / (double)pullCount));
            }
            fragmentAverages.Sort((left, right) => string.CompareOrdinal(
                left.HeroDefinitionId,
                right.HeroDefinitionId));

            double theoreticalUnlocks = 0d;
            if (unlockCostsByHero != null)
            {
                foreach (KeyValuePair<string, long> value in fragments)
                {
                    if (unlockCostsByHero.TryGetValue(value.Key, out long unlockCost) &&
                        unlockCost > 0)
                    {
                        theoreticalUnlocks += value.Value / (double)unlockCost;
                    }
                }
            }
            double spent = CalculateAggregateCost(banner, pullCount);
            double averageCostPerUnlock = theoreticalUnlocks > 0d
                ? spent / theoreticalUnlocks
                : 0d;

            highIntervals.Sort();
            var percentiles = new List<GachaPercentile>();
            if (highIntervals.Count > 0)
            {
                AddPercentile(percentiles, highIntervals, 50);
                AddPercentile(percentiles, highIntervals, 90);
                AddPercentile(percentiles, highIntervals, 95);
                AddPercentile(percentiles, highIntervals, 99);
            }

            return new GachaSimulationReport(
                banner.BannerId,
                banner.RulesVersion,
                seed,
                pullCount,
                rewardFrequencies,
                rarityFrequencies,
                highRarityCount > 0 ? intervalMean : 0d,
                maximumInterval,
                hardPityActivations,
                highRarityCount > 0
                    ? featuredHighRarityCount / (double)highRarityCount
                    : 0d,
                fragmentAverages,
                averageCostPerUnlock,
                highRarityCount > 1 ? intervalM2 / (highRarityCount - 1) : 0d,
                percentiles);
        }

        internal static GachaReward RollOne(
            GachaBannerDefinition banner,
            int playerProgress,
            ref DeterministicGachaRandom random,
            GachaPityState before,
            out GachaPityState after)
        {
            RollSelection selection = SelectOne(
                banner,
                BuildEligibleEntries(banner, playerProgress),
                ref random,
                before,
                out after);
            GachaPoolEntry selected = selection.Entry;
            long quantity = selected.RewardType == GachaRewardType.DirectHeroUnlock
                ? 1
                : selected.FragmentQuantity;
            return new GachaReward(
                selected.EntryId,
                selected.RewardType,
                selected.HeroDefinitionId,
                quantity,
                selected.Rarity,
                selection.Featured,
                selection.HardPityApplied);
        }

        private static RollSelection SelectOne(
            GachaBannerDefinition banner,
            IReadOnlyList<GachaPoolEntry> eligible,
            ref DeterministicGachaRandom random,
            GachaPityState before,
            out GachaPityState after)
        {
            RollSelection selection = SelectOneCore(
                banner,
                eligible,
                ref random,
                before.PullsSinceHighRarity,
                before.FeaturedGuarantee,
                out int nextPullsSinceHighRarity,
                out bool nextFeaturedGuarantee);
            bool highRarity = nextPullsSinceHighRarity == 0;
            after = before.Advance(highRarity, nextFeaturedGuarantee);
            return selection;
        }

        private static RollSelection SelectOneCore(
            GachaBannerDefinition banner,
            IReadOnlyList<GachaPoolEntry> eligible,
            ref DeterministicGachaRandom random,
            int pullsSinceHighRarity,
            bool featuredGuarantee,
            out int nextPullsSinceHighRarity,
            out bool nextFeaturedGuarantee)
        {
            bool hardPity = pullsSinceHighRarity + 1 >= banner.HardPityThreshold;
            if (hardPity &&
                CountCandidates(eligible, banner.HighRarityThreshold, false, banner) == 0)
            {
                throw new InvalidOperationException(
                    "Nenhuma entrada elegível para aplicar hard pity.");
            }

            GachaPoolEntry selected = SelectWeighted(
                eligible,
                banner,
                pullsSinceHighRarity,
                ref random,
                hardPity);
            bool highRarity = selected.Rarity >= banner.HighRarityThreshold;
            bool selectedFeatured = IsFeatured(selected, banner);
            if (highRarity && featuredGuarantee &&
                banner.FeaturedGuaranteeEnabled && !selectedFeatured)
            {
                if (CountCandidates(
                        eligible,
                        banner.HighRarityThreshold,
                        true,
                        banner) == 0)
                {
                    throw new InvalidOperationException(
                        "Featured guarantee sem entrada elegível.");
                }
                selected = SelectBaseWeighted(
                    eligible,
                    banner.HighRarityThreshold,
                    true,
                    banner,
                    ref random);
                selectedFeatured = true;
            }

            nextFeaturedGuarantee = featuredGuarantee;
            if (highRarity && banner.FeaturedGuaranteeEnabled)
                nextFeaturedGuarantee = !selectedFeatured;
            nextPullsSinceHighRarity = highRarity
                ? 0
                : checked(pullsSinceHighRarity + 1);
            return new RollSelection(selected, selectedFeatured, hardPity);
        }

        private static GachaPoolEntry SelectWeighted(
            IReadOnlyList<GachaPoolEntry> candidates,
            GachaBannerDefinition banner,
            int pullsSinceHighRarity,
            ref DeterministicGachaRandom random,
            bool hardPity)
        {
            if (hardPity)
            {
                return SelectBaseWeighted(
                    candidates,
                    banner.HighRarityThreshold,
                    false,
                    banner,
                    ref random);
            }
            if (pullsSinceHighRarity + 1 < banner.SoftPityStart ||
                banner.SoftPityIncreaseBasisPoints == 0)
            {
                return SelectBaseWeighted(
                    candidates,
                    null,
                    false,
                    banner,
                    ref random);
            }

            long baseTotal = 0;
            long highTotal = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                baseTotal = checked(baseTotal + candidates[i].Weight);
                if (candidates[i].Rarity >= banner.HighRarityThreshold)
                    highTotal = checked(highTotal + candidates[i].Weight);
            }
            if (highTotal == 0)
            {
                return SelectBaseWeighted(
                    candidates,
                    null,
                    false,
                    banner,
                    ref random);
            }

            long softStep = pullsSinceHighRarity + 2L - banner.SoftPityStart;
            long bonusBasisPoints = checked(
                softStep * banner.SoftPityIncreaseBasisPoints);
            long extraScaledTotal = checked(baseTotal * bonusBasisPoints);
            long totalScaledWeight = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                totalScaledWeight = checked(
                    totalScaledWeight +
                    GetSoftPityWeight(
                        candidates[i],
                        banner.HighRarityThreshold,
                        highTotal,
                        extraScaledTotal));
            }
            long roll = random.NextInt64(totalScaledWeight);
            long cumulative = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative = checked(
                    cumulative +
                    GetSoftPityWeight(
                        candidates[i],
                        banner.HighRarityThreshold,
                        highTotal,
                        extraScaledTotal));
                if (roll < cumulative)
                    return candidates[i];
            }
            throw new InvalidOperationException(
                "Seleção com soft pity não encontrou entrada.");
        }

        private static long GetSoftPityWeight(
            GachaPoolEntry entry,
            Rarity highRarityThreshold,
            long highTotal,
            long extraScaledTotal)
        {
            long effective = checked(entry.Weight * 10000L);
            if (entry.Rarity >= highRarityThreshold)
            {
                effective = checked(
                    effective + extraScaledTotal * entry.Weight / highTotal);
            }
            return effective;
        }

        private static GachaPoolEntry SelectBaseWeighted(
            IReadOnlyList<GachaPoolEntry> candidates,
            Rarity? minimumRarity,
            bool featuredOnly,
            GachaBannerDefinition banner,
            ref DeterministicGachaRandom random)
        {
            long total = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (IsCandidate(candidates[i], minimumRarity, featuredOnly, banner))
                    total = checked(total + candidates[i].Weight);
            }
            if (total <= 0)
                throw new InvalidOperationException("Pesos elegíveis inválidos.");
            long roll = random.NextInt64(total);
            long cumulative = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!IsCandidate(candidates[i], minimumRarity, featuredOnly, banner))
                    continue;
                cumulative = checked(cumulative + candidates[i].Weight);
                if (roll < cumulative)
                    return candidates[i];
            }
            throw new InvalidOperationException("Seleção ponderada não encontrou entrada.");
        }

        private static IReadOnlyList<GachaPoolEntry> BuildEligibleEntries(
            GachaBannerDefinition banner,
            int playerProgress)
        {
            var result = new List<GachaPoolEntry>(banner.PoolEntries.Count);
            for (int i = 0; i < banner.PoolEntries.Count; i++)
            {
                GachaPoolEntry entry = banner.PoolEntries[i];
                if (!entry.MinimumPlayerProgress.HasValue ||
                    playerProgress >= entry.MinimumPlayerProgress.Value)
                {
                    result.Add(entry);
                }
            }
            if (result.Count == 0)
                throw new InvalidOperationException("Nenhuma entrada elegível no pool.");
            return result;
        }

        private static int CountCandidates(
            IReadOnlyList<GachaPoolEntry> entries,
            Rarity? minimumRarity,
            bool featuredOnly,
            GachaBannerDefinition banner)
        {
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (IsCandidate(entries[i], minimumRarity, featuredOnly, banner))
                    count++;
            }
            return count;
        }

        private static bool IsCandidate(
            GachaPoolEntry entry,
            Rarity? minimumRarity,
            bool featuredOnly,
            GachaBannerDefinition banner)
        {
            return (!minimumRarity.HasValue || entry.Rarity >= minimumRarity.Value) &&
                   (!featuredOnly || IsFeatured(entry, banner));
        }

        private static bool IsFeatured(
            GachaPoolEntry entry,
            GachaBannerDefinition banner)
        {
            if (entry.Featured)
                return true;
            for (int i = 0; i < banner.FeaturedEntryIds.Count; i++)
            {
                if (string.Equals(
                        banner.FeaturedEntryIds[i],
                        entry.EntryId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private GachaReward ResolveDuplicate(
            GachaBannerDefinition banner,
            GachaReward reward,
            ISet<string> unlockedDuringRequest)
        {
            if (reward.RewardType != GachaRewardType.DirectHeroUnlock)
            {
                return reward;
            }
            if (!unlockedDuringRequest.Contains(reward.HeroDefinitionId))
            {
                unlockedDuringRequest.Add(reward.HeroDefinitionId);
                return reward;
            }

            GachaPoolEntry source = null;
            for (int i = 0; i < banner.PoolEntries.Count; i++)
            {
                if (string.Equals(
                        banner.PoolEntries[i].EntryId,
                        reward.EntryId,
                        StringComparison.Ordinal))
                {
                    source = banner.PoolEntries[i];
                    break;
                }
            }
            if (source == null ||
                source.DuplicateRule !=
                GachaDuplicateRule.ConvertDirectUnlockToFragments)
            {
                throw new InvalidOperationException(
                    "Desbloqueio direto duplicado sem regra de conversão.");
            }

            for (int i = 0; i < banner.DuplicateConversionRules.Count; i++)
            {
                GachaDuplicateConversionRule rule =
                    banner.DuplicateConversionRules[i];
                if (rule.Rarity == reward.Rarity)
                {
                    return new GachaReward(
                        reward.EntryId,
                        GachaRewardType.HeroFragments,
                        reward.HeroDefinitionId,
                        rule.FragmentQuantity,
                        reward.Rarity,
                        reward.Featured,
                        reward.HardPityApplied);
                }
            }
            throw new InvalidOperationException(
                "Regra de conversão não encontrada para a raridade da duplicata.");
        }

        private static void Increment(
            IDictionary<string, long> values,
            string key,
            long amount)
        {
            long current = values.TryGetValue(key, out long value) ? value : 0;
            values[key] = checked(current + amount);
        }

        private static double CalculateAggregateCost(
            GachaBannerDefinition banner,
            long pullCount)
        {
            long multis = pullCount / banner.MultiPullCount;
            long singles = pullCount % banner.MultiPullCount;
            long cost = checked(
                checked(multis * banner.CostMulti) +
                checked(singles * banner.CostSingle));
            return cost;
        }

        private static void AddPercentile(
            ICollection<GachaPercentile> destination,
            IReadOnlyList<int> sorted,
            int percentile)
        {
            double rank = percentile / 100d * (sorted.Count - 1);
            int index = (int)Math.Ceiling(rank);
            destination.Add(new GachaPercentile(percentile, sorted[index]));
        }

        private sealed class ProcessedRequest
        {
            private readonly string bannerId;
            private readonly int pullCount;
            private readonly long seed;
            private readonly int playerProgress;
            private readonly long logicalTimestamp;

            public ProcessedRequest(GachaPullRequest request, GachaPullResult result)
            {
                bannerId = request.BannerId;
                pullCount = request.PullCount;
                seed = request.Seed;
                playerProgress = request.PlayerProgress;
                logicalTimestamp = request.LogicalTimestamp;
                Result = result;
            }

            public GachaPullResult Result { get; }

            public void ValidateReplay(GachaPullRequest request)
            {
                if (!string.Equals(bannerId, request.BannerId, StringComparison.Ordinal) ||
                    pullCount != request.PullCount ||
                    seed != request.Seed ||
                    playerProgress != request.PlayerProgress ||
                    logicalTimestamp != request.LogicalTimestamp)
                {
                    throw new InvalidOperationException(
                        "requestId reutilizado com payload diferente.");
                }
            }
        }

        private readonly struct RollSelection
        {
            public RollSelection(
                GachaPoolEntry entry,
                bool featured,
                bool hardPityApplied)
            {
                Entry = entry;
                Featured = featured;
                HardPityApplied = hardPityApplied;
            }

            public GachaPoolEntry Entry { get; }
            public bool Featured { get; }
            public bool HardPityApplied { get; }
        }
    }

    internal struct DeterministicGachaRandom
    {
        private ulong state;

        public DeterministicGachaRandom(long seed)
        {
            state = unchecked((ulong)seed);
            state += 0x9E3779B97F4A7C15UL;
            _ = NextUInt64();
        }

        public long NextInt64(long exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            ulong bound = (ulong)exclusiveMaximum;
            ulong threshold = unchecked(0UL - bound) % bound;
            while (true)
            {
                ulong value = NextUInt64();
                if (value >= threshold)
                    return (long)(value % bound);
            }
        }

        private ulong NextUInt64()
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong value = state;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
#endif
