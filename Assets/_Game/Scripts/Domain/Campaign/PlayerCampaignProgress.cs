using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Campaign
{
    [Serializable]
    public sealed class PlayerCampaignProgress
    {
        [SerializeField] private string currentStageId = string.Empty;
        [SerializeField] private string highestClearedStageId = string.Empty;
        [SerializeField] private long lastClaimedServerTime;
        [SerializeField] private long lastSessionStartTime;
        [SerializeField] private List<string> pendingFirstClearRewards = new List<string>();
        [SerializeField] private long revision;
        [SerializeField] private string rulesVersion = string.Empty;
        [SerializeField] private List<string> clearedStageIds = new List<string>();
        [SerializeField] private List<string> claimedFirstClearStageIds = new List<string>();
        [SerializeField] private List<string> processedBattleRequestIds = new List<string>();
        [SerializeField] private List<string> collectedOfflineRequestIds = new List<string>();
        [SerializeField] private OfflineRewardReport pendingOfflineReport;
        [SerializeField] private long accountExperience;

        public PlayerCampaignProgress()
        {
        }

        public PlayerCampaignProgress(
            string currentStageId,
            string highestClearedStageId,
            long lastClaimedServerTime,
            long lastSessionStartTime,
            IEnumerable<string> pendingFirstClearRewards,
            long revision,
            string rulesVersion,
            IEnumerable<string> clearedStageIds = null,
            IEnumerable<string> claimedFirstClearStageIds = null,
            IEnumerable<string> processedBattleRequestIds = null,
            IEnumerable<string> collectedOfflineRequestIds = null,
            OfflineRewardReport pendingOfflineReport = null,
            long accountExperience = 0)
        {
            this.currentStageId = currentStageId ?? string.Empty;
            this.highestClearedStageId = highestClearedStageId ?? string.Empty;
            this.lastClaimedServerTime = lastClaimedServerTime;
            this.lastSessionStartTime = lastSessionStartTime;
            this.pendingFirstClearRewards = Copy(pendingFirstClearRewards);
            this.revision = revision;
            this.rulesVersion = rulesVersion ?? string.Empty;
            this.clearedStageIds = Copy(clearedStageIds);
            this.claimedFirstClearStageIds = Copy(claimedFirstClearStageIds);
            this.processedBattleRequestIds = Copy(processedBattleRequestIds);
            this.collectedOfflineRequestIds = Copy(collectedOfflineRequestIds);
            this.pendingOfflineReport = pendingOfflineReport;
            this.accountExperience = accountExperience;
            ValidateBasic();
        }

        public string CurrentStageId => currentStageId;
        public string HighestClearedStageId => highestClearedStageId;
        public long LastClaimedServerTime => lastClaimedServerTime;
        public long LastSessionStartTime => lastSessionStartTime;
        public IReadOnlyList<string> PendingFirstClearRewards =>
            new ReadOnlyCollection<string>(pendingFirstClearRewards);
        public long Revision => revision;
        public string RulesVersion => rulesVersion;
        public IReadOnlyList<string> ClearedStageIds =>
            new ReadOnlyCollection<string>(clearedStageIds);
        public IReadOnlyList<string> ClaimedFirstClearStageIds =>
            new ReadOnlyCollection<string>(claimedFirstClearStageIds);
        public IReadOnlyList<string> ProcessedBattleRequestIds =>
            new ReadOnlyCollection<string>(processedBattleRequestIds);
        public IReadOnlyList<string> CollectedOfflineRequestIds =>
            new ReadOnlyCollection<string>(collectedOfflineRequestIds);
        public OfflineRewardReport PendingOfflineReport => pendingOfflineReport;
        public long AccountExperience => accountExperience;

        public static PlayerCampaignProgress CreateNew(
            CampaignDefinition campaign,
            long nowUnixMilliseconds)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            if (nowUnixMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(nowUnixMilliseconds));
            return new PlayerCampaignProgress(
                campaign.Stages[0].StageId,
                string.Empty,
                nowUnixMilliseconds,
                nowUnixMilliseconds,
                Array.Empty<string>(),
                0,
                campaign.RulesVersion);
        }

        public PlayerCampaignProgress Clone()
        {
            return new PlayerCampaignProgress(
                currentStageId,
                highestClearedStageId,
                lastClaimedServerTime,
                lastSessionStartTime,
                pendingFirstClearRewards,
                revision,
                rulesVersion,
                clearedStageIds,
                claimedFirstClearStageIds,
                processedBattleRequestIds,
                collectedOfflineRequestIds,
                pendingOfflineReport,
                accountExperience);
        }

        internal bool HasCleared(string stageId)
        {
            return clearedStageIds.Contains(stageId);
        }

        internal bool HasClaimedFirstClear(string stageId)
        {
            return claimedFirstClearStageIds.Contains(stageId);
        }

        internal bool HasProcessedBattle(string requestId)
        {
            return processedBattleRequestIds.Contains(requestId);
        }

        internal bool HasCollectedOffline(string requestId)
        {
            return collectedOfflineRequestIds.Contains(requestId);
        }

        internal void RecordBattle(
            string stageId,
            string nextCurrentStageId,
            string newHighestClearedStageId,
            string requestId,
            bool victory,
            bool firstClear)
        {
            if (processedBattleRequestIds.Contains(requestId))
                return;
            processedBattleRequestIds.Add(requestId);
            if (victory)
            {
                if (!clearedStageIds.Contains(stageId))
                    clearedStageIds.Add(stageId);
                currentStageId = nextCurrentStageId;
                highestClearedStageId = newHighestClearedStageId;
                if (firstClear && !pendingFirstClearRewards.Contains(stageId))
                    pendingFirstClearRewards.Add(stageId);
            }
            AdvanceRevision();
        }

        internal void MarkFirstClearDelivered(string stageId)
        {
            pendingFirstClearRewards.Remove(stageId);
            if (!claimedFirstClearStageIds.Contains(stageId))
                claimedFirstClearStageIds.Add(stageId);
            AdvanceRevision();
        }

        internal void InitializeTimestamps(long nowUnixMilliseconds)
        {
            if (lastClaimedServerTime <= 0)
                lastClaimedServerTime = nowUnixMilliseconds;
            lastSessionStartTime = nowUnixMilliseconds;
            AdvanceRevision();
        }

        internal void StorePendingReport(OfflineRewardReport report)
        {
            pendingOfflineReport = report ??
                throw new ArgumentNullException(nameof(report));
            lastSessionStartTime = report.EndUnixMilliseconds;
            AdvanceRevision();
        }

        internal void MarkOfflineCollected(string requestId, long claimTime)
        {
            if (!collectedOfflineRequestIds.Contains(requestId))
                collectedOfflineRequestIds.Add(requestId);
            pendingOfflineReport?.MarkCollected();
            lastClaimedServerTime = Math.Max(lastClaimedServerTime, claimTime);
            pendingOfflineReport = null;
            AdvanceRevision();
        }

        internal void AddAccountExperience(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            accountExperience = checked(accountExperience + amount);
            if (amount > 0)
                AdvanceRevision();
        }

        internal void ValidateAgainst(CampaignDefinition campaign)
        {
            ValidateBasic();
            if (!string.Equals(rulesVersion, campaign.RulesVersion, StringComparison.Ordinal))
                throw new InvalidOperationException("rulesVersion de campanha incompatível.");
            CampaignStageDefinition current = campaign.GetStage(currentStageId);
            CampaignStageDefinition highest = null;
            if (!string.IsNullOrWhiteSpace(highestClearedStageId))
                highest = campaign.GetStage(highestClearedStageId);
            int maximumClearedSequence = 0;
            for (int i = 0; i < clearedStageIds.Count; i++)
            {
                CampaignStageDefinition cleared = campaign.GetStage(clearedStageIds[i]);
                maximumClearedSequence = Math.Max(maximumClearedSequence, cleared.Sequence);
            }
            if ((highest == null && maximumClearedSequence != 0) ||
                (highest != null && highest.Sequence != maximumClearedSequence))
            {
                throw new InvalidOperationException(
                    "highestClearedStageId não corresponde ao maior estágio concluído.");
            }
            if (highest != null && current.Sequence < highest.Sequence)
                throw new InvalidOperationException("Estágio atual não pode preceder o maior concluído.");
            for (int i = 0; i < pendingFirstClearRewards.Count; i++)
            {
                string stageId = pendingFirstClearRewards[i];
                campaign.GetStage(stageId);
                if (!clearedStageIds.Contains(stageId) ||
                    claimedFirstClearStageIds.Contains(stageId))
                {
                    throw new InvalidOperationException(
                        "First clear pendente possui estado inconsistente.");
                }
            }
            for (int i = 0; i < claimedFirstClearStageIds.Count; i++)
            {
                string stageId = claimedFirstClearStageIds[i];
                campaign.GetStage(stageId);
                if (!clearedStageIds.Contains(stageId))
                    throw new InvalidOperationException("First clear foi entregue sem conclusão.");
            }
        }

        private void ValidateBasic()
        {
            if (string.IsNullOrWhiteSpace(currentStageId))
                throw new InvalidOperationException("currentStageId é obrigatório.");
            if (lastClaimedServerTime < 0 || lastSessionStartTime < 0 ||
                revision < 0 || accountExperience < 0)
                throw new InvalidOperationException("Tempos e revisão não podem ser negativos.");
            if (string.IsNullOrWhiteSpace(rulesVersion))
                throw new InvalidOperationException("rulesVersion é obrigatório.");
            ValidateUnique(clearedStageIds, nameof(clearedStageIds));
            ValidateUnique(pendingFirstClearRewards, nameof(pendingFirstClearRewards));
            ValidateUnique(claimedFirstClearStageIds, nameof(claimedFirstClearStageIds));
            ValidateUnique(processedBattleRequestIds, nameof(processedBattleRequestIds));
            ValidateUnique(collectedOfflineRequestIds, nameof(collectedOfflineRequestIds));
        }

        private void AdvanceRevision()
        {
            revision = checked(revision + 1);
        }

        private static List<string> Copy(IEnumerable<string> source)
        {
            return source == null ? new List<string>() : new List<string>(source);
        }

        private static void ValidateUnique(List<string> values, string name)
        {
            values ??= new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < values.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(values[i]) || !unique.Add(values[i]))
                    throw new InvalidOperationException($"{name} contém ID vazio ou duplicado.");
            }
        }
    }
}
