#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Content;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Gacha
{
    public enum GachaCurrencyType
    {
        DevelopmentGachaCurrency = 9000
    }

    public enum GachaRewardType
    {
        HeroFragments = 0,
        AuxiliaryDevelopmentReward = 1,
        DirectHeroUnlock = 2
    }

    public enum GachaPityScope
    {
        Banner = 0,
        Group = 1
    }

    public enum GachaDuplicateRule
    {
        KeepAsFragments = 0,
        ConvertDirectUnlockToFragments = 1
    }

    [Serializable]
    public sealed class GachaDuplicateConversionRule
    {
        [SerializeField] private Rarity rarity;
        [SerializeField] private long fragmentQuantity;

        public GachaDuplicateConversionRule()
        {
        }

        public GachaDuplicateConversionRule(Rarity rarity, long fragmentQuantity)
        {
            this.rarity = rarity;
            this.fragmentQuantity = fragmentQuantity;
        }

        public Rarity Rarity => rarity;
        public long FragmentQuantity => fragmentQuantity;
    }

    [Serializable]
    public sealed class GachaPoolEntry
    {
        [SerializeField] private string entryId = string.Empty;
        [SerializeField] private GachaRewardType rewardType;
        [SerializeField] private string heroDefinitionId = string.Empty;
        [SerializeField] private long fragmentQuantity;
        [SerializeField] private Rarity rarity;
        [SerializeField] private int weight;
        [SerializeField] private bool featured;
        [SerializeField] private int minimumPlayerProgress = -1;
        [SerializeField] private GachaDuplicateRule duplicateRule;
        [SerializeField] private List<string> tags = new List<string>();

        public GachaPoolEntry()
        {
        }

        public GachaPoolEntry(
            string entryId,
            GachaRewardType rewardType,
            string heroDefinitionId,
            long fragmentQuantity,
            Rarity rarity,
            int weight,
            bool featured,
            int? minimumPlayerProgress,
            GachaDuplicateRule duplicateRule,
            IEnumerable<string> tags)
        {
            this.entryId = entryId ?? string.Empty;
            this.rewardType = rewardType;
            this.heroDefinitionId = heroDefinitionId ?? string.Empty;
            this.fragmentQuantity = fragmentQuantity;
            this.rarity = rarity;
            this.weight = weight;
            this.featured = featured;
            this.minimumPlayerProgress = minimumPlayerProgress ?? -1;
            this.duplicateRule = duplicateRule;
            this.tags = tags == null ? new List<string>() : new List<string>(tags);
        }

        public string EntryId => entryId;
        public GachaRewardType RewardType => rewardType;
        public string HeroDefinitionId => heroDefinitionId;
        public long FragmentQuantity => fragmentQuantity;
        public Rarity Rarity => rarity;
        public int Weight => weight;
        public bool Featured => featured;
        public int? MinimumPlayerProgress =>
            minimumPlayerProgress == -1 ? (int?)null : minimumPlayerProgress;
        public GachaDuplicateRule DuplicateRule => duplicateRule;
        public IReadOnlyList<string> Tags => tags.AsReadOnly();
    }

    [Serializable]
    public sealed class GachaBannerDefinition
    {
        [SerializeField] private string bannerId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string description = string.Empty;
        [SerializeField] private long startTime = -1;
        [SerializeField] private long endTime = -1;
        [SerializeField] private bool enabled;
        [SerializeField] private GachaCurrencyType currencyType =
            GachaCurrencyType.DevelopmentGachaCurrency;
        [SerializeField] private long costSingle;
        [SerializeField] private long costMulti;
        [SerializeField] private int multiPullCount = 10;
        [SerializeField] private List<GachaPoolEntry> poolEntries =
            new List<GachaPoolEntry>();
        [SerializeField] private List<string> featuredEntryIds = new List<string>();
        [SerializeField] private int softPityStart;
        [SerializeField] private int softPityIncreaseBasisPoints;
        [SerializeField] private int hardPityThreshold;
        [SerializeField] private Rarity highRarityThreshold = Rarity.Epic;
        [SerializeField] private GachaPityScope pityScope;
        [SerializeField] private string pityGroupId = string.Empty;
        [SerializeField] private bool featuredGuaranteeEnabled;
        [SerializeField] private string rulesVersion = string.Empty;
        [SerializeField] private List<GachaDuplicateConversionRule> duplicateConversionRules =
            new List<GachaDuplicateConversionRule>();
        [SerializeField] private List<string> tags = new List<string>();

        public GachaBannerDefinition()
        {
        }

        public GachaBannerDefinition(
            string bannerId,
            string displayName,
            string description,
            long? startTime,
            long? endTime,
            bool enabled,
            GachaCurrencyType currencyType,
            long costSingle,
            long costMulti,
            int multiPullCount,
            IEnumerable<GachaPoolEntry> poolEntries,
            IEnumerable<string> featuredEntryIds,
            int softPityStart,
            int softPityIncreaseBasisPoints,
            int hardPityThreshold,
            Rarity highRarityThreshold,
            GachaPityScope pityScope,
            string pityGroupId,
            bool featuredGuaranteeEnabled,
            string rulesVersion,
            IEnumerable<GachaDuplicateConversionRule> duplicateConversionRules,
            IEnumerable<string> tags)
        {
            this.bannerId = bannerId ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.description = description ?? string.Empty;
            this.startTime = startTime ?? -1;
            this.endTime = endTime ?? -1;
            this.enabled = enabled;
            this.currencyType = currencyType;
            this.costSingle = costSingle;
            this.costMulti = costMulti;
            this.multiPullCount = multiPullCount;
            this.poolEntries = poolEntries == null
                ? new List<GachaPoolEntry>()
                : new List<GachaPoolEntry>(poolEntries);
            this.featuredEntryIds = featuredEntryIds == null
                ? new List<string>()
                : new List<string>(featuredEntryIds);
            this.softPityStart = softPityStart;
            this.softPityIncreaseBasisPoints = softPityIncreaseBasisPoints;
            this.hardPityThreshold = hardPityThreshold;
            this.highRarityThreshold = highRarityThreshold;
            this.pityScope = pityScope;
            this.pityGroupId = pityGroupId ?? string.Empty;
            this.featuredGuaranteeEnabled = featuredGuaranteeEnabled;
            this.rulesVersion = rulesVersion ?? string.Empty;
            this.duplicateConversionRules = duplicateConversionRules == null
                ? new List<GachaDuplicateConversionRule>()
                : new List<GachaDuplicateConversionRule>(duplicateConversionRules);
            this.tags = tags == null ? new List<string>() : new List<string>(tags);
        }

        public string BannerId => bannerId;
        public string DisplayName => displayName;
        public string Description => description;
        public long? StartTime => startTime < 0 ? (long?)null : startTime;
        public long? EndTime => endTime < 0 ? (long?)null : endTime;
        public bool Enabled => enabled;
        public GachaCurrencyType CurrencyType => currencyType;
        public long CostSingle => costSingle;
        public long CostMulti => costMulti;
        public int MultiPullCount => multiPullCount;
        public IReadOnlyList<GachaPoolEntry> PoolEntries => poolEntries.AsReadOnly();
        public IReadOnlyList<string> FeaturedEntryIds => featuredEntryIds.AsReadOnly();
        public int SoftPityStart => softPityStart;
        public int SoftPityIncreaseBasisPoints => softPityIncreaseBasisPoints;
        public int HardPityThreshold => hardPityThreshold;
        public Rarity HighRarityThreshold => highRarityThreshold;
        public GachaPityScope PityScope => pityScope;
        public string PityGroupId => pityGroupId;
        public bool FeaturedGuaranteeEnabled => featuredGuaranteeEnabled;
        public string RulesVersion => rulesVersion;
        public IReadOnlyList<GachaDuplicateConversionRule> DuplicateConversionRules =>
            duplicateConversionRules.AsReadOnly();
        public IReadOnlyList<string> Tags => tags.AsReadOnly();
        public string PityKey => pityScope == GachaPityScope.Group
            ? pityGroupId
            : bannerId;
    }

    [Serializable]
    public sealed class GachaPullRequest
    {
        [SerializeField] private string requestId = string.Empty;
        [SerializeField] private string bannerId = string.Empty;
        [SerializeField] private int pullCount = 1;
        [SerializeField] private long seed;
        [SerializeField] private bool hasExplicitSeed;
        [SerializeField] private int playerProgress;
        [SerializeField] private long logicalTimestamp;

        public GachaPullRequest()
        {
        }

        public GachaPullRequest(
            string requestId,
            string bannerId,
            int pullCount,
            long? seed,
            int playerProgress,
            long logicalTimestamp)
        {
            this.requestId = requestId ?? string.Empty;
            this.bannerId = bannerId ?? string.Empty;
            this.pullCount = pullCount;
            this.seed = seed ?? 0;
            hasExplicitSeed = seed.HasValue;
            this.playerProgress = playerProgress;
            this.logicalTimestamp = logicalTimestamp;
        }

        public string RequestId => requestId;
        public string BannerId => bannerId;
        public int PullCount => pullCount;
        public long Seed => seed;
        public bool HasExplicitSeed => hasExplicitSeed;
        public int PlayerProgress => playerProgress;
        public long LogicalTimestamp => logicalTimestamp;
    }

    [Serializable]
    public sealed class GachaReward
    {
        [SerializeField] private string entryId = string.Empty;
        [SerializeField] private GachaRewardType rewardType;
        [SerializeField] private string heroDefinitionId = string.Empty;
        [SerializeField] private long quantity;
        [SerializeField] private Rarity rarity;
        [SerializeField] private bool featured;
        [SerializeField] private bool hardPityApplied;

        public GachaReward()
        {
        }

        public GachaReward(
            string entryId,
            GachaRewardType rewardType,
            string heroDefinitionId,
            long quantity,
            Rarity rarity,
            bool featured,
            bool hardPityApplied)
        {
            this.entryId = entryId ?? string.Empty;
            this.rewardType = rewardType;
            this.heroDefinitionId = heroDefinitionId ?? string.Empty;
            this.quantity = quantity;
            this.rarity = rarity;
            this.featured = featured;
            this.hardPityApplied = hardPityApplied;
        }

        public string EntryId => entryId;
        public GachaRewardType RewardType => rewardType;
        public string HeroDefinitionId => heroDefinitionId;
        public long Quantity => quantity;
        public Rarity Rarity => rarity;
        public bool Featured => featured;
        public bool HardPityApplied => hardPityApplied;
        public bool IsHighRarity(Rarity threshold) => rarity >= threshold;
    }

    [Serializable]
    public sealed class GachaPityState
    {
        [SerializeField] private string bannerOrGroupId = string.Empty;
        [SerializeField] private int pullsSinceHighRarity;
        [SerializeField] private bool featuredGuarantee;
        [SerializeField] private long totalPulls;
        [SerializeField] private long revision;

        public GachaPityState()
        {
        }

        public GachaPityState(
            string bannerOrGroupId,
            int pullsSinceHighRarity,
            bool featuredGuarantee,
            long totalPulls,
            long revision)
        {
            this.bannerOrGroupId = bannerOrGroupId ?? string.Empty;
            this.pullsSinceHighRarity = pullsSinceHighRarity;
            this.featuredGuarantee = featuredGuarantee;
            this.totalPulls = totalPulls;
            this.revision = revision;
            Validate();
        }

        public string BannerOrGroupId => bannerOrGroupId;
        public int PullsSinceHighRarity => pullsSinceHighRarity;
        public bool FeaturedGuarantee => featuredGuarantee;
        public long TotalPulls => totalPulls;
        public long Revision => revision;

        public static GachaPityState Create(string pityKey)
        {
            return new GachaPityState(pityKey, 0, false, 0, 0);
        }

        internal GachaPityState Advance(
            bool highRarity,
            bool nextFeaturedGuarantee)
        {
            return new GachaPityState(
                bannerOrGroupId,
                highRarity ? 0 : checked(pullsSinceHighRarity + 1),
                nextFeaturedGuarantee,
                checked(totalPulls + 1),
                checked(revision + 1));
        }

        public GachaPityState Copy()
        {
            return new GachaPityState(
                bannerOrGroupId,
                pullsSinceHighRarity,
                featuredGuarantee,
                totalPulls,
                revision);
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(bannerOrGroupId) ||
                pullsSinceHighRarity < 0 || totalPulls < 0 || revision < 0)
            {
                throw new InvalidOperationException("Estado de pity inválido.");
            }
        }
    }

    [Serializable]
    public sealed class GachaHistoryEntry
    {
        [SerializeField] private string requestId = string.Empty;
        [SerializeField] private string bannerId = string.Empty;
        [SerializeField] private int sequence;
        [SerializeField] private GachaReward reward;
        [SerializeField] private GachaPityState pityBefore;
        [SerializeField] private GachaPityState pityAfter;
        [SerializeField] private long seed;
        [SerializeField] private string rulesVersion = string.Empty;
        [SerializeField] private long logicalTimestamp;

        public GachaHistoryEntry()
        {
        }

        public GachaHistoryEntry(
            string requestId,
            string bannerId,
            int sequence,
            GachaReward reward,
            GachaPityState pityBefore,
            GachaPityState pityAfter,
            long seed,
            string rulesVersion,
            long logicalTimestamp)
        {
            this.requestId = requestId ?? string.Empty;
            this.bannerId = bannerId ?? string.Empty;
            this.sequence = sequence;
            this.reward = reward ?? throw new ArgumentNullException(nameof(reward));
            this.pityBefore = pityBefore?.Copy() ??
                throw new ArgumentNullException(nameof(pityBefore));
            this.pityAfter = pityAfter?.Copy() ??
                throw new ArgumentNullException(nameof(pityAfter));
            this.seed = seed;
            this.rulesVersion = rulesVersion ?? string.Empty;
            this.logicalTimestamp = logicalTimestamp;
        }

        public string RequestId => requestId;
        public string BannerId => bannerId;
        public int Sequence => sequence;
        public GachaReward Reward => reward;
        public GachaPityState PityBefore => pityBefore;
        public GachaPityState PityAfter => pityAfter;
        public long Seed => seed;
        public string RulesVersion => rulesVersion;
        public long LogicalTimestamp => logicalTimestamp;
    }

    [Serializable]
    public sealed class GachaPullResult
    {
        [SerializeField] private string requestId = string.Empty;
        [SerializeField] private string bannerId = string.Empty;
        [SerializeField] private long chargedCost;
        [SerializeField] private long remainingDevelopmentCurrency;
        [SerializeField] private bool idempotentReplay;
        [SerializeField] private List<GachaReward> rewards = new List<GachaReward>();
        [SerializeField] private List<GachaHistoryEntry> history =
            new List<GachaHistoryEntry>();
        [SerializeField] private GachaPityState pityAfter;

        public GachaPullResult()
        {
        }

        public GachaPullResult(
            string requestId,
            string bannerId,
            long chargedCost,
            long remainingDevelopmentCurrency,
            bool idempotentReplay,
            IEnumerable<GachaReward> rewards,
            IEnumerable<GachaHistoryEntry> history,
            GachaPityState pityAfter)
        {
            this.requestId = requestId ?? string.Empty;
            this.bannerId = bannerId ?? string.Empty;
            this.chargedCost = chargedCost;
            this.remainingDevelopmentCurrency = remainingDevelopmentCurrency;
            this.idempotentReplay = idempotentReplay;
            this.rewards = rewards == null
                ? new List<GachaReward>()
                : new List<GachaReward>(rewards);
            this.history = history == null
                ? new List<GachaHistoryEntry>()
                : new List<GachaHistoryEntry>(history);
            this.pityAfter = pityAfter?.Copy();
        }

        public string RequestId => requestId;
        public string BannerId => bannerId;
        public long ChargedCost => chargedCost;
        public long RemainingDevelopmentCurrency => remainingDevelopmentCurrency;
        public bool IdempotentReplay => idempotentReplay;
        public IReadOnlyList<GachaReward> Rewards => rewards.AsReadOnly();
        public IReadOnlyList<GachaHistoryEntry> History => history.AsReadOnly();
        public GachaPityState PityAfter => pityAfter;

        internal GachaPullResult AsReplay()
        {
            return new GachaPullResult(
                requestId,
                bannerId,
                0,
                remainingDevelopmentCurrency,
                true,
                rewards,
                history,
                pityAfter);
        }
    }

    [Serializable]
    public sealed class GachaFrequency
    {
        [SerializeField] private string key = string.Empty;
        [SerializeField] private long count;

        public GachaFrequency()
        {
        }

        public GachaFrequency(string key, long count)
        {
            this.key = key ?? string.Empty;
            this.count = count;
        }

        public string Key => key;
        public long Count => count;
    }

    [Serializable]
    public sealed class GachaAverageFragments
    {
        [SerializeField] private string heroDefinitionId = string.Empty;
        [SerializeField] private long totalFragments;
        [SerializeField] private double averagePerPull;

        public GachaAverageFragments()
        {
        }

        public GachaAverageFragments(
            string heroDefinitionId,
            long totalFragments,
            double averagePerPull)
        {
            this.heroDefinitionId = heroDefinitionId ?? string.Empty;
            this.totalFragments = totalFragments;
            this.averagePerPull = averagePerPull;
        }

        public string HeroDefinitionId => heroDefinitionId;
        public long TotalFragments => totalFragments;
        public double AveragePerPull => averagePerPull;
    }

    [Serializable]
    public sealed class GachaPercentile
    {
        [SerializeField] private int percentile;
        [SerializeField] private int pulls;

        public GachaPercentile()
        {
        }

        public GachaPercentile(int percentile, int pulls)
        {
            this.percentile = percentile;
            this.pulls = pulls;
        }

        public int Percentile => percentile;
        public int Pulls => pulls;
    }

    [Serializable]
    public sealed class GachaSimulationReport
    {
        [SerializeField] private string bannerId = string.Empty;
        [SerializeField] private string rulesVersion = string.Empty;
        [SerializeField] private long seed;
        [SerializeField] private long pullCount;
        [SerializeField] private List<GachaFrequency> rewardFrequencies =
            new List<GachaFrequency>();
        [SerializeField] private List<GachaFrequency> rarityFrequencies =
            new List<GachaFrequency>();
        [SerializeField] private double averagePullsToHighRarity;
        [SerializeField] private int maximumPullsToHighRarity;
        [SerializeField] private long hardPityActivations;
        [SerializeField] private double featuredRate;
        [SerializeField] private List<GachaAverageFragments> averageFragmentsByHero =
            new List<GachaAverageFragments>();
        [SerializeField] private double averageCostPerUnlock;
        [SerializeField] private double highRarityIntervalVariance;
        [SerializeField] private List<GachaPercentile> highRarityPercentiles =
            new List<GachaPercentile>();

        public GachaSimulationReport(
            string bannerId,
            string rulesVersion,
            long seed,
            long pullCount,
            IEnumerable<GachaFrequency> rewardFrequencies,
            IEnumerable<GachaFrequency> rarityFrequencies,
            double averagePullsToHighRarity,
            int maximumPullsToHighRarity,
            long hardPityActivations,
            double featuredRate,
            IEnumerable<GachaAverageFragments> averageFragmentsByHero,
            double averageCostPerUnlock,
            double highRarityIntervalVariance,
            IEnumerable<GachaPercentile> highRarityPercentiles)
        {
            this.bannerId = bannerId ?? string.Empty;
            this.rulesVersion = rulesVersion ?? string.Empty;
            this.seed = seed;
            this.pullCount = pullCount;
            this.rewardFrequencies = new List<GachaFrequency>(rewardFrequencies);
            this.rarityFrequencies = new List<GachaFrequency>(rarityFrequencies);
            this.averagePullsToHighRarity = averagePullsToHighRarity;
            this.maximumPullsToHighRarity = maximumPullsToHighRarity;
            this.hardPityActivations = hardPityActivations;
            this.featuredRate = featuredRate;
            this.averageFragmentsByHero =
                new List<GachaAverageFragments>(averageFragmentsByHero);
            this.averageCostPerUnlock = averageCostPerUnlock;
            this.highRarityIntervalVariance = highRarityIntervalVariance;
            this.highRarityPercentiles = new List<GachaPercentile>(
                highRarityPercentiles);
        }

        public string BannerId => bannerId;
        public string RulesVersion => rulesVersion;
        public long Seed => seed;
        public long PullCount => pullCount;
        public IReadOnlyList<GachaFrequency> RewardFrequencies =>
            rewardFrequencies.AsReadOnly();
        public IReadOnlyList<GachaFrequency> RarityFrequencies =>
            rarityFrequencies.AsReadOnly();
        public double AveragePullsToHighRarity => averagePullsToHighRarity;
        public int MaximumPullsToHighRarity => maximumPullsToHighRarity;
        public long HardPityActivations => hardPityActivations;
        public double FeaturedRate => featuredRate;
        public IReadOnlyList<GachaAverageFragments> AverageFragmentsByHero =>
            averageFragmentsByHero.AsReadOnly();
        public double AverageCostPerUnlock => averageCostPerUnlock;
        public double HighRarityIntervalVariance => highRarityIntervalVariance;
        public IReadOnlyList<GachaPercentile> HighRarityPercentiles =>
            highRarityPercentiles.AsReadOnly();
    }
}
#endif
