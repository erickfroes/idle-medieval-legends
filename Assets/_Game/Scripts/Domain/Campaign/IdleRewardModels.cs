using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Campaign
{
    public interface IGameClock
    {
        long UtcNowUnixMilliseconds { get; }
        bool IsAuthoritative { get; }
        string Source { get; }
    }

    public enum TimeValidationCode
    {
        Valid = 0,
        MissingTimestamp = 1,
        ClockRegression = 2,
        ExtremeJumpLimited = 3
    }

    public sealed class TimeValidationResult
    {
        public TimeValidationResult(
            TimeValidationCode code,
            long startUnixMilliseconds,
            long endUnixMilliseconds,
            long elapsedMilliseconds,
            long validatedElapsedMilliseconds,
            string warning)
        {
            Code = code;
            StartUnixMilliseconds = startUnixMilliseconds;
            EndUnixMilliseconds = endUnixMilliseconds;
            ElapsedMilliseconds = elapsedMilliseconds;
            ValidatedElapsedMilliseconds = validatedElapsedMilliseconds;
            Warning = warning ?? string.Empty;
        }

        public TimeValidationCode Code { get; }
        public long StartUnixMilliseconds { get; }
        public long EndUnixMilliseconds { get; }
        public long ElapsedMilliseconds { get; }
        public long ValidatedElapsedMilliseconds { get; }
        public string Warning { get; }
        public bool HasWarning => Code != TimeValidationCode.Valid;
    }

    [Serializable]
    public sealed class IdleRewardMultiplier
    {
        [SerializeField] private string multiplierId = string.Empty;
        [SerializeField] private int basisPoints = 10000;

        public IdleRewardMultiplier(string multiplierId, int basisPoints)
        {
            if (string.IsNullOrWhiteSpace(multiplierId))
                throw new ArgumentException("multiplierId é obrigatório.", nameof(multiplierId));
            if (basisPoints < 0) throw new ArgumentOutOfRangeException(nameof(basisPoints));
            this.multiplierId = multiplierId;
            this.basisPoints = basisPoints;
        }

        public string MultiplierId => multiplierId;
        public int BasisPoints => basisPoints;
    }

    [Serializable]
    public sealed class IdleProductionProfile
    {
        [SerializeField] private string stageId = string.Empty;
        [SerializeField] private long goldPerMinute;
        [SerializeField] private List<CampaignMaterialReward> materialsPerMinute =
            new List<CampaignMaterialReward>();
        [SerializeField] private long accountExperiencePerMinute;
        [SerializeField] private long accumulatedDurationMilliseconds;
        [SerializeField] private long playerOfflineLimitMilliseconds;
        [SerializeField] private long stageOfflineLimitMilliseconds;
        [SerializeField] private List<IdleRewardMultiplier> allowedMultipliers =
            new List<IdleRewardMultiplier>();

        public IdleProductionProfile(
            string stageId,
            long goldPerMinute,
            IEnumerable<CampaignMaterialReward> materialsPerMinute,
            long accountExperiencePerMinute,
            long accumulatedDurationMilliseconds,
            long playerOfflineLimitMilliseconds,
            long stageOfflineLimitMilliseconds,
            IEnumerable<IdleRewardMultiplier> allowedMultipliers)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("stageId é obrigatório.", nameof(stageId));
            if (goldPerMinute < 0) throw new ArgumentOutOfRangeException(nameof(goldPerMinute));
            if (accountExperiencePerMinute < 0)
                throw new ArgumentOutOfRangeException(nameof(accountExperiencePerMinute));
            if (accumulatedDurationMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(accumulatedDurationMilliseconds));
            if (playerOfflineLimitMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(playerOfflineLimitMilliseconds));
            if (stageOfflineLimitMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(stageOfflineLimitMilliseconds));
            this.stageId = stageId;
            this.goldPerMinute = goldPerMinute;
            this.materialsPerMinute = materialsPerMinute == null
                ? new List<CampaignMaterialReward>()
                : new List<CampaignMaterialReward>(materialsPerMinute);
            this.accountExperiencePerMinute = accountExperiencePerMinute;
            this.accumulatedDurationMilliseconds = accumulatedDurationMilliseconds;
            this.playerOfflineLimitMilliseconds = playerOfflineLimitMilliseconds;
            this.stageOfflineLimitMilliseconds = stageOfflineLimitMilliseconds;
            this.allowedMultipliers = allowedMultipliers == null
                ? new List<IdleRewardMultiplier>()
                : new List<IdleRewardMultiplier>(allowedMultipliers);
            if (this.allowedMultipliers.Count == 0)
                this.allowedMultipliers.Add(new IdleRewardMultiplier("base", 10000));
        }

        public string StageId => stageId;
        public long GoldPerMinute => goldPerMinute;
        public IReadOnlyList<CampaignMaterialReward> MaterialsPerMinute =>
            new ReadOnlyCollection<CampaignMaterialReward>(materialsPerMinute);
        public long AccountExperiencePerMinute => accountExperiencePerMinute;
        public long AccumulatedDurationMilliseconds => accumulatedDurationMilliseconds;
        public long PlayerOfflineLimitMilliseconds => playerOfflineLimitMilliseconds;
        public long StageOfflineLimitMilliseconds => stageOfflineLimitMilliseconds;
        public IReadOnlyList<IdleRewardMultiplier> AllowedMultipliers =>
            new ReadOnlyCollection<IdleRewardMultiplier>(allowedMultipliers);
    }

    [Serializable]
    public sealed class OfflineSession
    {
        [SerializeField] private long startUnixMilliseconds;
        [SerializeField] private long endUnixMilliseconds;
        [SerializeField] private string stageId = string.Empty;
        [SerializeField] private long revision;
        [SerializeField] private string rulesVersion = string.Empty;
        [SerializeField] private string requestId = string.Empty;

        public OfflineSession(
            long startUnixMilliseconds,
            long endUnixMilliseconds,
            string stageId,
            long revision,
            string rulesVersion,
            string requestId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("stageId é obrigatório.", nameof(stageId));
            if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));
            if (string.IsNullOrWhiteSpace(rulesVersion))
                throw new ArgumentException("rulesVersion é obrigatório.", nameof(rulesVersion));
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("requestId é obrigatório.", nameof(requestId));
            this.startUnixMilliseconds = startUnixMilliseconds;
            this.endUnixMilliseconds = endUnixMilliseconds;
            this.stageId = stageId;
            this.revision = revision;
            this.rulesVersion = rulesVersion;
            this.requestId = requestId;
        }

        public long StartUnixMilliseconds => startUnixMilliseconds;
        public long EndUnixMilliseconds => endUnixMilliseconds;
        public string StageId => stageId;
        public long Revision => revision;
        public string RulesVersion => rulesVersion;
        public string RequestId => requestId;
    }

    [Serializable]
    public sealed class OfflineRewardReport
    {
        [SerializeField] private long startUnixMilliseconds;
        [SerializeField] private long endUnixMilliseconds;
        [SerializeField] private long realDurationMilliseconds;
        [SerializeField] private long eligibleDurationMilliseconds;
        [SerializeField] private long discardedDurationMilliseconds;
        [SerializeField] private string stageId = string.Empty;
        [SerializeField] private long gold;
        [SerializeField] private List<CampaignMaterialReward> materials =
            new List<CampaignMaterialReward>();
        [SerializeField] private long accountExperience;
        [SerializeField] private List<IdleRewardMultiplier> multipliers =
            new List<IdleRewardMultiplier>();
        [SerializeField] private long revision;
        [SerializeField] private string requestId = string.Empty;
        [SerializeField] private bool collected;
        [SerializeField] private TimeValidationCode timeValidationCode;
        [SerializeField] private string warning = string.Empty;

        public OfflineRewardReport(
            long startUnixMilliseconds,
            long endUnixMilliseconds,
            long realDurationMilliseconds,
            long eligibleDurationMilliseconds,
            long discardedDurationMilliseconds,
            string stageId,
            long gold,
            IEnumerable<CampaignMaterialReward> materials,
            long accountExperience,
            IEnumerable<IdleRewardMultiplier> multipliers,
            long revision,
            string requestId,
            bool collected,
            TimeValidationCode timeValidationCode,
            string warning)
        {
            if (realDurationMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(realDurationMilliseconds));
            if (eligibleDurationMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(eligibleDurationMilliseconds));
            if (discardedDurationMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(discardedDurationMilliseconds));
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            if (accountExperience < 0)
                throw new ArgumentOutOfRangeException(nameof(accountExperience));
            if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("stageId é obrigatório.", nameof(stageId));
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("requestId é obrigatório.", nameof(requestId));
            this.startUnixMilliseconds = startUnixMilliseconds;
            this.endUnixMilliseconds = endUnixMilliseconds;
            this.realDurationMilliseconds = realDurationMilliseconds;
            this.eligibleDurationMilliseconds = eligibleDurationMilliseconds;
            this.discardedDurationMilliseconds = discardedDurationMilliseconds;
            this.stageId = stageId;
            this.gold = gold;
            this.materials = materials == null
                ? new List<CampaignMaterialReward>()
                : new List<CampaignMaterialReward>(materials);
            this.accountExperience = accountExperience;
            this.multipliers = multipliers == null
                ? new List<IdleRewardMultiplier>()
                : new List<IdleRewardMultiplier>(multipliers);
            this.revision = revision;
            this.requestId = requestId;
            this.collected = collected;
            this.timeValidationCode = timeValidationCode;
            this.warning = warning ?? string.Empty;
        }

        public long StartUnixMilliseconds => startUnixMilliseconds;
        public long EndUnixMilliseconds => endUnixMilliseconds;
        public long RealDurationMilliseconds => realDurationMilliseconds;
        public long EligibleDurationMilliseconds => eligibleDurationMilliseconds;
        public long DiscardedDurationMilliseconds => discardedDurationMilliseconds;
        public string StageId => stageId;
        public long Gold => gold;
        public IReadOnlyList<CampaignMaterialReward> Materials =>
            new ReadOnlyCollection<CampaignMaterialReward>(materials);
        public long AccountExperience => accountExperience;
        public IReadOnlyList<IdleRewardMultiplier> Multipliers =>
            new ReadOnlyCollection<IdleRewardMultiplier>(multipliers);
        public long Revision => revision;
        public string RequestId => requestId;
        public bool Collected => collected;
        public TimeValidationCode TimeValidationCode => timeValidationCode;
        public string Warning => warning;

        internal void MarkCollected()
        {
            collected = true;
        }
    }
}
