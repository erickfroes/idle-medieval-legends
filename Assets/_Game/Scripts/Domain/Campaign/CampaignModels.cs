using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Campaign
{
    [Serializable]
    public sealed class CampaignMaterialReward
    {
        [SerializeField] private string materialDefinitionId = string.Empty;
        [SerializeField] private long quantity;

        public CampaignMaterialReward(string materialDefinitionId, long quantity)
        {
            if (string.IsNullOrWhiteSpace(materialDefinitionId))
                throw new ArgumentException("materialDefinitionId é obrigatório.",
                    nameof(materialDefinitionId));
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            this.materialDefinitionId = materialDefinitionId;
            this.quantity = quantity;
        }

        public string MaterialDefinitionId => materialDefinitionId;
        public long Quantity => quantity;
    }

    [Serializable]
    public sealed class StageRewardDefinition
    {
        [SerializeField] private long gold;
        [SerializeField] private long accountExperience;
        [SerializeField] private List<CampaignMaterialReward> materials =
            new List<CampaignMaterialReward>();

        public StageRewardDefinition(
            long gold,
            IEnumerable<CampaignMaterialReward> materials = null,
            long accountExperience = 0)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            if (accountExperience < 0)
                throw new ArgumentOutOfRangeException(nameof(accountExperience));
            this.gold = gold;
            this.accountExperience = accountExperience;
            this.materials = CopyMaterials(materials);
        }

        public long Gold => gold;
        public long AccountExperience => accountExperience;
        public IReadOnlyList<CampaignMaterialReward> Materials =>
            new ReadOnlyCollection<CampaignMaterialReward>(materials);

        private static List<CampaignMaterialReward> CopyMaterials(
            IEnumerable<CampaignMaterialReward> source)
        {
            var result = source == null
                ? new List<CampaignMaterialReward>()
                : new List<CampaignMaterialReward>(source);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < result.Count; i++)
            {
                CampaignMaterialReward entry = result[i] ??
                    throw new InvalidOperationException("Recompensa material nula.");
                if (!ids.Add(entry.MaterialDefinitionId))
                {
                    throw new InvalidOperationException(
                        $"Material duplicado na recompensa: {entry.MaterialDefinitionId}.");
                }
            }
            return result;
        }
    }

    [Serializable]
    public sealed class StageEnemy
    {
        [SerializeField] private string enemyId = string.Empty;
        [SerializeField] private string heroDefinitionId = string.Empty;
        [SerializeField] private int slot;
        [SerializeField] private int level = 1;
        [SerializeField] private int statMultiplierBasisPoints = 10000;
        [SerializeField] private List<string> tags = new List<string>();

        public StageEnemy(
            string enemyId,
            string heroDefinitionId,
            int slot,
            int level,
            int statMultiplierBasisPoints,
            IEnumerable<string> tags = null)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
                throw new ArgumentException("enemyId é obrigatório.", nameof(enemyId));
            if (string.IsNullOrWhiteSpace(heroDefinitionId))
                throw new ArgumentException("heroDefinitionId é obrigatório.",
                    nameof(heroDefinitionId));
            if (slot < 0 || slot > 4) throw new ArgumentOutOfRangeException(nameof(slot));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            if (statMultiplierBasisPoints <= 0)
                throw new ArgumentOutOfRangeException(nameof(statMultiplierBasisPoints));
            this.enemyId = enemyId;
            this.heroDefinitionId = heroDefinitionId;
            this.slot = slot;
            this.level = level;
            this.statMultiplierBasisPoints = statMultiplierBasisPoints;
            this.tags = tags == null ? new List<string>() : new List<string>(tags);
        }

        public string EnemyId => enemyId;
        public string HeroDefinitionId => heroDefinitionId;
        public int Slot => slot;
        public int Level => level;
        public int StatMultiplierBasisPoints => statMultiplierBasisPoints;
        public IReadOnlyList<string> Tags => new ReadOnlyCollection<string>(tags);
    }

    [Serializable]
    public sealed class StageEnemyFormation
    {
        [SerializeField] private List<StageEnemy> enemies = new List<StageEnemy>();

        public StageEnemyFormation(IEnumerable<StageEnemy> enemies)
        {
            this.enemies = enemies == null
                ? new List<StageEnemy>()
                : new List<StageEnemy>(enemies);
            if (this.enemies.Count < 1 || this.enemies.Count > 5)
                throw new InvalidOperationException("Formação deve possuir de um a cinco inimigos.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var slots = new HashSet<int>();
            for (int i = 0; i < this.enemies.Count; i++)
            {
                StageEnemy enemy = this.enemies[i] ??
                    throw new InvalidOperationException("Inimigo nulo na formação.");
                if (!ids.Add(enemy.EnemyId))
                    throw new InvalidOperationException($"enemyId duplicado: {enemy.EnemyId}.");
                if (!slots.Add(enemy.Slot))
                    throw new InvalidOperationException($"Slot inimigo duplicado: {enemy.Slot}.");
            }
        }

        public IReadOnlyList<StageEnemy> Enemies =>
            new ReadOnlyCollection<StageEnemy>(enemies);
    }

    [Serializable]
    public sealed class CampaignStageDefinition
    {
        [SerializeField] private string stageId = string.Empty;
        [SerializeField] private string chapterId = string.Empty;
        [SerializeField] private int sequence;
        [SerializeField] private StageEnemyFormation enemyFormation;
        [SerializeField] private long recommendedPower;
        [SerializeField] private long baseGoldPerMinute;
        [SerializeField] private List<CampaignMaterialReward> rewardMaterialTable =
            new List<CampaignMaterialReward>();
        [SerializeField] private StageRewardDefinition firstClearRewards;
        [SerializeField] private StageRewardDefinition repeatRewards;
        [SerializeField] private bool idleUnlocked;
        [SerializeField] private bool boss;
        [SerializeField] private int maximumOfflineHours = 8;
        [SerializeField] private List<string> tags = new List<string>();

        public CampaignStageDefinition(
            string stageId,
            string chapterId,
            int sequence,
            StageEnemyFormation enemyFormation,
            long recommendedPower,
            long baseGoldPerMinute,
            IEnumerable<CampaignMaterialReward> rewardMaterialTable,
            StageRewardDefinition firstClearRewards,
            StageRewardDefinition repeatRewards,
            bool idleUnlocked,
            bool boss,
            int maximumOfflineHours,
            IEnumerable<string> tags = null)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("stageId é obrigatório.", nameof(stageId));
            if (string.IsNullOrWhiteSpace(chapterId))
                throw new ArgumentException("chapterId é obrigatório.", nameof(chapterId));
            if (sequence < 1) throw new ArgumentOutOfRangeException(nameof(sequence));
            if (recommendedPower < 0)
                throw new ArgumentOutOfRangeException(nameof(recommendedPower));
            if (baseGoldPerMinute < 0)
                throw new ArgumentOutOfRangeException(nameof(baseGoldPerMinute));
            if (maximumOfflineHours < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumOfflineHours));
            this.stageId = stageId;
            this.chapterId = chapterId;
            this.sequence = sequence;
            this.enemyFormation = enemyFormation ??
                throw new ArgumentNullException(nameof(enemyFormation));
            this.recommendedPower = recommendedPower;
            this.baseGoldPerMinute = baseGoldPerMinute;
            this.rewardMaterialTable = rewardMaterialTable == null
                ? new List<CampaignMaterialReward>()
                : new List<CampaignMaterialReward>(rewardMaterialTable);
            this.firstClearRewards = firstClearRewards ??
                throw new ArgumentNullException(nameof(firstClearRewards));
            this.repeatRewards = repeatRewards ??
                throw new ArgumentNullException(nameof(repeatRewards));
            this.idleUnlocked = idleUnlocked;
            this.boss = boss;
            this.maximumOfflineHours = maximumOfflineHours;
            this.tags = tags == null ? new List<string>() : new List<string>(tags);
            ValidateMaterialTable();
        }

        public string StageId => stageId;
        public string ChapterId => chapterId;
        public int Sequence => sequence;
        public StageEnemyFormation EnemyFormation => enemyFormation;
        public long RecommendedPower => recommendedPower;
        public long BaseGoldPerMinute => baseGoldPerMinute;
        public IReadOnlyList<CampaignMaterialReward> RewardMaterialTable =>
            new ReadOnlyCollection<CampaignMaterialReward>(rewardMaterialTable);
        public StageRewardDefinition FirstClearRewards => firstClearRewards;
        public StageRewardDefinition RepeatRewards => repeatRewards;
        public bool IdleUnlocked => idleUnlocked;
        public bool Boss => boss;
        public int MaximumOfflineHours => maximumOfflineHours;
        public IReadOnlyList<string> Tags => new ReadOnlyCollection<string>(tags);

        private void ValidateMaterialTable()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rewardMaterialTable.Count; i++)
            {
                CampaignMaterialReward entry = rewardMaterialTable[i] ??
                    throw new InvalidOperationException("Taxa material nula.");
                if (!ids.Add(entry.MaterialDefinitionId))
                {
                    throw new InvalidOperationException(
                        $"Material duplicado na tabela idle: {entry.MaterialDefinitionId}.");
                }
            }
        }
    }

    [Serializable]
    public sealed class CampaignChapterDefinition
    {
        [SerializeField] private string chapterId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private int sequence;
        [SerializeField] private List<CampaignStageDefinition> stages =
            new List<CampaignStageDefinition>();

        public CampaignChapterDefinition(
            string chapterId,
            string displayName,
            int sequence,
            IEnumerable<CampaignStageDefinition> stages)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
                throw new ArgumentException("chapterId é obrigatório.", nameof(chapterId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("displayName é obrigatório.", nameof(displayName));
            if (sequence < 1) throw new ArgumentOutOfRangeException(nameof(sequence));
            this.chapterId = chapterId;
            this.displayName = displayName;
            this.sequence = sequence;
            this.stages = stages == null
                ? new List<CampaignStageDefinition>()
                : new List<CampaignStageDefinition>(stages);
            if (this.stages.Count == 0)
                throw new InvalidOperationException("Capítulo deve possuir estágios.");
            for (int i = 0; i < this.stages.Count; i++)
            {
                if (!string.Equals(this.stages[i].ChapterId, chapterId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Estágio pertence a outro capítulo.");
            }
        }

        public string ChapterId => chapterId;
        public string DisplayName => displayName;
        public int Sequence => sequence;
        public IReadOnlyList<CampaignStageDefinition> Stages =>
            new ReadOnlyCollection<CampaignStageDefinition>(stages);
    }

    [Serializable]
    public sealed class CampaignDefinition
    {
        [SerializeField] private string campaignId = string.Empty;
        [SerializeField] private string rulesVersion = string.Empty;
        [SerializeField] private List<CampaignChapterDefinition> chapters =
            new List<CampaignChapterDefinition>();

        [NonSerialized] private Dictionary<string, CampaignStageDefinition> stageIndex;
        [NonSerialized] private List<CampaignStageDefinition> orderedStages;

        public CampaignDefinition(
            string campaignId,
            string rulesVersion,
            IEnumerable<CampaignChapterDefinition> chapters)
        {
            if (string.IsNullOrWhiteSpace(campaignId))
                throw new ArgumentException("campaignId é obrigatório.", nameof(campaignId));
            if (string.IsNullOrWhiteSpace(rulesVersion))
                throw new ArgumentException("rulesVersion é obrigatório.", nameof(rulesVersion));
            this.campaignId = campaignId;
            this.rulesVersion = rulesVersion;
            this.chapters = chapters == null
                ? new List<CampaignChapterDefinition>()
                : new List<CampaignChapterDefinition>(chapters);
            BuildIndex();
        }

        public string CampaignId => campaignId;
        public string RulesVersion => rulesVersion;
        public IReadOnlyList<CampaignChapterDefinition> Chapters =>
            new ReadOnlyCollection<CampaignChapterDefinition>(chapters);
        public IReadOnlyList<CampaignStageDefinition> Stages
        {
            get
            {
                EnsureIndex();
                return new ReadOnlyCollection<CampaignStageDefinition>(orderedStages);
            }
        }

        public CampaignStageDefinition GetStage(string stageId)
        {
            EnsureIndex();
            if (string.IsNullOrWhiteSpace(stageId) ||
                !stageIndex.TryGetValue(stageId, out CampaignStageDefinition result))
            {
                throw new KeyNotFoundException($"Estágio inexistente: {stageId}.");
            }
            return result;
        }

        public CampaignStageDefinition GetNextStage(string stageId)
        {
            CampaignStageDefinition current = GetStage(stageId);
            EnsureIndex();
            int index = orderedStages.IndexOf(current);
            return index >= 0 && index + 1 < orderedStages.Count
                ? orderedStages[index + 1]
                : null;
        }

        private void EnsureIndex()
        {
            if (stageIndex == null || orderedStages == null)
                BuildIndex();
        }

        private void BuildIndex()
        {
            if (chapters == null || chapters.Count == 0)
                throw new InvalidOperationException("Campanha deve possuir capítulos.");
            stageIndex = new Dictionary<string, CampaignStageDefinition>(StringComparer.Ordinal);
            orderedStages = new List<CampaignStageDefinition>();
            var chapterIds = new HashSet<string>(StringComparer.Ordinal);
            var chapterSequences = new HashSet<int>();
            for (int i = 0; i < chapters.Count; i++)
            {
                CampaignChapterDefinition chapter = chapters[i] ??
                    throw new InvalidOperationException("Capítulo nulo.");
                if (!chapterIds.Add(chapter.ChapterId) ||
                    !chapterSequences.Add(chapter.Sequence))
                {
                    throw new InvalidOperationException("Capítulo duplicado.");
                }
                for (int j = 0; j < chapter.Stages.Count; j++)
                {
                    CampaignStageDefinition stage = chapter.Stages[j];
                    if (!stageIndex.TryAdd(stage.StageId, stage))
                        throw new InvalidOperationException($"stageId duplicado: {stage.StageId}.");
                    orderedStages.Add(stage);
                }
            }
            orderedStages.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            for (int i = 0; i < orderedStages.Count; i++)
            {
                if (orderedStages[i].Sequence != i + 1)
                    throw new InvalidOperationException("Sequência global de estágios deve ser contínua.");
            }
        }
    }
}
