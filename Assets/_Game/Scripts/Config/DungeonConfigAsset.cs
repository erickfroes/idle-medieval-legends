using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IdleMedievalLegends.Domain.Campaign;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Dungeons;
using UnityEngine;

namespace IdleMedievalLegends.Config
{
    [Serializable]
    public sealed class DungeonRewardEntryConfig
    {
        [SerializeField] private string itemDefinitionId = string.Empty;
        [SerializeField] private long minimumQuantity;
        [SerializeField] private long maximumQuantity;
        [SerializeField, Range(0, 10000)] private int chanceBasisPoints;
        [SerializeField] private bool guaranteed;
        [SerializeField] private bool firstClearOnly;
        [SerializeField, Min(0)] private int difficultyMultiplier = 10000;

        public DungeonRewardEntryConfig(
            string itemDefinitionId,
            long minimumQuantity,
            long maximumQuantity,
            int chanceBasisPoints,
            bool guaranteed,
            bool firstClearOnly = false,
            int difficultyMultiplier = 10000)
        {
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

        public DungeonRewardTableEntry Build()
        {
            return new DungeonRewardTableEntry(
                itemDefinitionId,
                minimumQuantity,
                maximumQuantity,
                chanceBasisPoints,
                guaranteed,
                firstClearOnly,
                difficultyMultiplier);
        }
    }

    [Serializable]
    public sealed class DungeonRewardTableConfig
    {
        [SerializeField, Min(0)] private long gold;
        [SerializeField] private List<DungeonRewardEntryConfig> entries =
            new List<DungeonRewardEntryConfig>();

        public DungeonRewardTableConfig(
            long gold,
            IEnumerable<DungeonRewardEntryConfig> entries)
        {
            this.gold = gold;
            this.entries = entries == null
                ? new List<DungeonRewardEntryConfig>()
                : new List<DungeonRewardEntryConfig>(entries);
        }

        public long Gold => gold;
        public IReadOnlyList<DungeonRewardEntryConfig> Entries =>
            new ReadOnlyCollection<DungeonRewardEntryConfig>(entries);

        public DungeonRewardTable Build()
        {
            if (entries == null)
                throw new InvalidOperationException("Tabela de recompensa sem entries.");
            var result = new List<DungeonRewardTableEntry>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                result.Add((entries[i] ??
                    throw new InvalidOperationException("Reward config nula.")).Build());
            }
            return new DungeonRewardTable(result, gold);
        }
    }

    [Serializable]
    public sealed class DungeonEnemyConfig
    {
        [SerializeField] private string enemyId = string.Empty;
        [SerializeField] private string heroDefinitionId = string.Empty;
        [SerializeField, Range(0, 4)] private int slot;
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(1)] private int statMultiplierBasisPoints = 10000;
        [SerializeField] private List<string> tags = new List<string>();

        public DungeonEnemyConfig(
            string enemyId,
            string heroDefinitionId,
            int slot,
            int level,
            int statMultiplierBasisPoints,
            IEnumerable<string> tags)
        {
            this.enemyId = enemyId;
            this.heroDefinitionId = heroDefinitionId;
            this.slot = slot;
            this.level = level;
            this.statMultiplierBasisPoints = statMultiplierBasisPoints;
            this.tags = tags == null ? new List<string>() : new List<string>(tags);
        }

        public StageEnemy Build()
        {
            return new StageEnemy(
                enemyId,
                heroDefinitionId,
                slot,
                level,
                statMultiplierBasisPoints,
                tags);
        }
    }

    [Serializable]
    public sealed class DungeonDifficultyConfig
    {
        [SerializeField] private string difficultyId = string.Empty;
        [SerializeField, Min(0)] private long recommendedPower;
        [SerializeField, Min(1)] private int energyCost = 1;
        [SerializeField] private string encounterId = string.Empty;
        [SerializeField] private string scenarioId = string.Empty;
        [SerializeField] private string battleSceneName = "Battle";
        [SerializeField] private List<DungeonEnemyConfig> enemyFormation =
            new List<DungeonEnemyConfig>();
        [SerializeField] private DungeonRewardTableConfig rewardTable;
        [SerializeField] private DungeonRewardTableConfig firstClearReward;
        [SerializeField, Min(1)] private int minimumPlayerLevel = 1;
        [SerializeField] private string requiredCampaignStage = string.Empty;
        [SerializeField, Min(0)] private int durationEstimateSeconds;
        [SerializeField, Range(1, 9)] private int materialTier = 1;
        [SerializeField] private bool blockBelowRecommendedPower;

        public DungeonDifficultyConfig(
            string difficultyId,
            long recommendedPower,
            int energyCost,
            string encounterId,
            string scenarioId,
            string battleSceneName,
            IEnumerable<DungeonEnemyConfig> enemyFormation,
            DungeonRewardTableConfig rewardTable,
            DungeonRewardTableConfig firstClearReward,
            int minimumPlayerLevel,
            string requiredCampaignStage,
            int durationEstimateSeconds,
            int materialTier,
            bool blockBelowRecommendedPower = false)
        {
            this.difficultyId = difficultyId;
            this.recommendedPower = recommendedPower;
            this.energyCost = energyCost;
            this.encounterId = encounterId;
            this.scenarioId = scenarioId;
            this.battleSceneName = battleSceneName;
            this.enemyFormation = enemyFormation == null
                ? new List<DungeonEnemyConfig>()
                : new List<DungeonEnemyConfig>(enemyFormation);
            this.rewardTable = rewardTable;
            this.firstClearReward = firstClearReward;
            this.minimumPlayerLevel = minimumPlayerLevel;
            this.requiredCampaignStage = requiredCampaignStage;
            this.durationEstimateSeconds = durationEstimateSeconds;
            this.materialTier = materialTier;
            this.blockBelowRecommendedPower = blockBelowRecommendedPower;
        }

        public string DifficultyId => difficultyId;
        public long RecommendedPower => recommendedPower;
        public int EnergyCost => energyCost;
        public IReadOnlyList<DungeonEnemyConfig> EnemyFormation =>
            new ReadOnlyCollection<DungeonEnemyConfig>(enemyFormation);
        public DungeonRewardTableConfig RewardTable => rewardTable;
        public DungeonRewardTableConfig FirstClearReward => firstClearReward;

        public DungeonDifficultyDefinition Build()
        {
            if (enemyFormation == null || enemyFormation.Count == 0)
                throw new InvalidOperationException(
                    $"Dificuldade {difficultyId} sem formação.");
            var enemies = new List<StageEnemy>(enemyFormation.Count);
            for (int i = 0; i < enemyFormation.Count; i++)
            {
                enemies.Add((enemyFormation[i] ??
                    throw new InvalidOperationException("Enemy config nula.")).Build());
            }
            var encounter = new DungeonEncounterDefinition(
                encounterId,
                new StageEnemyFormation(enemies),
                scenarioId,
                battleSceneName);
            return new DungeonDifficultyDefinition(
                difficultyId,
                recommendedPower,
                energyCost,
                encounter,
                (rewardTable ??
                    throw new InvalidOperationException("Reward table ausente.")).Build(),
                (firstClearReward ??
                    throw new InvalidOperationException(
                        "First-clear reward ausente.")).Build(),
                minimumPlayerLevel,
                requiredCampaignStage,
                durationEstimateSeconds,
                ProgressionTypes.FromTierNumber(materialTier),
                blockBelowRecommendedPower);
        }
    }

    [Serializable]
    public sealed class DungeonDefinitionConfig
    {
        [SerializeField] private string dungeonId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private CraftingProfession associatedProfession;
        [SerializeField] private string unlockStageId = string.Empty;
        [SerializeField] private List<DungeonDifficultyConfig> availableDifficulties =
            new List<DungeonDifficultyConfig>();
        [SerializeField] private bool hasDailyAttemptLimit;
        [SerializeField, Min(1)] private int dailyAttemptLimit = 1;
        [SerializeField] private string scheduleId = string.Empty;
        [SerializeField, TextArea] private string futureScheduleNotes = string.Empty;
        [SerializeField] private bool requiresServerSchedule;
        [SerializeField] private string iconPlaceholder = string.Empty;
        [SerializeField] private List<string> tags = new List<string>();

        public DungeonDefinitionConfig(
            string dungeonId,
            string displayName,
            string description,
            CraftingProfession associatedProfession,
            string unlockStageId,
            IEnumerable<DungeonDifficultyConfig> availableDifficulties,
            int? dailyAttemptLimit,
            string scheduleId,
            string futureScheduleNotes,
            bool requiresServerSchedule,
            string iconPlaceholder,
            IEnumerable<string> tags)
        {
            this.dungeonId = dungeonId;
            this.displayName = displayName;
            this.description = description;
            this.associatedProfession = associatedProfession;
            this.unlockStageId = unlockStageId;
            this.availableDifficulties = availableDifficulties == null
                ? new List<DungeonDifficultyConfig>()
                : new List<DungeonDifficultyConfig>(availableDifficulties);
            hasDailyAttemptLimit = dailyAttemptLimit.HasValue;
            this.dailyAttemptLimit = dailyAttemptLimit ?? 1;
            this.scheduleId = scheduleId ?? string.Empty;
            this.futureScheduleNotes = futureScheduleNotes ?? string.Empty;
            this.requiresServerSchedule = requiresServerSchedule;
            this.iconPlaceholder = iconPlaceholder;
            this.tags = tags == null ? new List<string>() : new List<string>(tags);
        }

        public string DungeonId => dungeonId;
        public IReadOnlyList<DungeonDifficultyConfig> AvailableDifficulties =>
            new ReadOnlyCollection<DungeonDifficultyConfig>(availableDifficulties);

        public DungeonDefinition Build()
        {
            if (availableDifficulties == null || availableDifficulties.Count == 0)
                throw new InvalidOperationException(
                    $"Masmorra {dungeonId} sem dificuldades.");
            var difficulties =
                new List<DungeonDifficultyDefinition>(availableDifficulties.Count);
            for (int i = 0; i < availableDifficulties.Count; i++)
            {
                difficulties.Add((availableDifficulties[i] ??
                    throw new InvalidOperationException(
                        "Difficulty config nula.")).Build());
            }
            return new DungeonDefinition(
                dungeonId,
                displayName,
                description,
                associatedProfession,
                unlockStageId,
                difficulties,
                hasDailyAttemptLimit ? dailyAttemptLimit : (int?)null,
                new DungeonScheduleMetadata(
                    scheduleId,
                    futureScheduleNotes,
                    requiresServerSchedule),
                iconPlaceholder,
                tags);
        }
    }

    [CreateAssetMenu(
        fileName = "DungeonConfig",
        menuName = "Idle Medieval Legends/Balance/Dungeon Config")]
    public sealed class DungeonConfigAsset : ScriptableObject
    {
        [Header("Energy")]
        [SerializeField, Min(1)] private int maximumEnergy = 100;
        [SerializeField, Min(1)] private int minutesPerEnergy = 5;
        [SerializeField, Min(0)] private int initialEnergy = 100;

        [Header("Dungeon Balance")]
        [SerializeField] private List<DungeonDefinitionConfig> dungeonDefinitions =
            new List<DungeonDefinitionConfig>();

        public int MaximumEnergy => maximumEnergy;
        public int MinutesPerEnergy => minutesPerEnergy;
        public int InitialEnergy => Math.Min(initialEnergy, maximumEnergy);
        public IReadOnlyList<DungeonDefinitionConfig> DungeonDefinitions =>
            new ReadOnlyCollection<DungeonDefinitionConfig>(dungeonDefinitions);

        public DungeonCatalog BuildCatalog()
        {
            EnsureInitialized();
            ClampEnergy();
            return BuildCatalogInternal();
        }

        public EnergyRegenerationRules BuildEnergyRules()
        {
            ClampEnergy();
            return new EnergyRegenerationRules(minutesPerEnergy, maximumEnergy);
        }

        public void EnsureValid()
        {
            EnsureInitialized();
            ClampEnergy();
            BuildCatalogInternal();
        }

        private DungeonCatalog BuildCatalogInternal()
        {
            var result = new List<DungeonDefinition>(dungeonDefinitions.Count);
            for (int i = 0; i < dungeonDefinitions.Count; i++)
            {
                result.Add((dungeonDefinitions[i] ??
                    throw new InvalidOperationException(
                        "Dungeon definition config nula.")).Build());
            }
            return new DungeonCatalog(result);
        }

        private void EnsureInitialized()
        {
            dungeonDefinitions ??= new List<DungeonDefinitionConfig>();
            if (dungeonDefinitions.Count == 0)
                dungeonDefinitions = CreateDefaultDungeonDefinitions();
        }

        private void ClampEnergy()
        {
            maximumEnergy = Math.Max(1, maximumEnergy);
            minutesPerEnergy = Math.Max(1, minutesPerEnergy);
            initialEnergy = Math.Max(0, Math.Min(initialEnergy, maximumEnergy));
        }

        private void OnValidate()
        {
            EnsureInitialized();
            ClampEnergy();
        }

        private static List<DungeonDefinitionConfig> CreateDefaultDungeonDefinitions()
        {
            DungeonRewardEntryConfig Guaranteed(
                string itemId,
                long minimum,
                long maximum,
                int multiplier = 10000)
            {
                return new DungeonRewardEntryConfig(
                    itemId,
                    minimum,
                    maximum,
                    10000,
                    true,
                    difficultyMultiplier: multiplier);
            }

            DungeonRewardEntryConfig Chance(
                string itemId,
                long minimum,
                long maximum,
                int chanceBasisPoints,
                int multiplier = 10000)
            {
                return new DungeonRewardEntryConfig(
                    itemId,
                    minimum,
                    maximum,
                    chanceBasisPoints,
                    false,
                    difficultyMultiplier: multiplier);
            }

            DungeonRewardTableConfig Table(
                long gold,
                params DungeonRewardEntryConfig[] entries)
            {
                return new DungeonRewardTableConfig(gold, entries);
            }

            DungeonRewardTableConfig FirstClear(
                string itemId,
                long quantity,
                long gold)
            {
                return Table(gold, Guaranteed(itemId, quantity, quantity));
            }

            List<DungeonEnemyConfig> Enemies(
                string encounterId,
                string heroDefinitionId,
                int count,
                int multiplierBasisPoints)
            {
                var result = new List<DungeonEnemyConfig>(count);
                for (int i = 0; i < count; i++)
                {
                    result.Add(new DungeonEnemyConfig(
                        $"{encounterId}_enemy_{i + 1}",
                        heroDefinitionId,
                        i,
                        1,
                        multiplierBasisPoints,
                        new[] { "dungeon_enemy", encounterId }));
                }
                return result;
            }

            DungeonDifficultyConfig Difficulty(
                string difficultyId,
                long recommendedPower,
                int energyCost,
                string heroDefinitionId,
                int enemyCount,
                int enemyMultiplierBasisPoints,
                DungeonRewardTableConfig rewards,
                DungeonRewardTableConfig firstClear,
                string requiredStage,
                int durationSeconds)
            {
                return new DungeonDifficultyConfig(
                    difficultyId,
                    recommendedPower,
                    energyCost,
                    difficultyId,
                    $"dungeon_scenario_{difficultyId}",
                    "Battle",
                    Enemies(
                        difficultyId,
                        heroDefinitionId,
                        enemyCount,
                        enemyMultiplierBasisPoints),
                    rewards,
                    firstClear,
                    1,
                    requiredStage,
                    durationSeconds,
                    1);
            }

            var mineDifficulties = new[]
            {
                Difficulty(
                    "mine_apprentice", 1800, 10, "hero_paladin_001", 1, 6000,
                    Table(
                        20,
                        Guaranteed("material_iron_ore_t1", 4, 6),
                        Chance("material_iron_ingot_t1", 1, 2, 2500)),
                    FirstClear("material_iron_ore_t1", 8, 50),
                    "stage_01",
                    60),
                Difficulty(
                    "mine_journeyman", 2600, 14, "hero_paladin_001", 2, 7500,
                    Table(
                        35,
                        Guaranteed(
                            "material_iron_ore_t1", 5, 8, 12500),
                        Chance("material_iron_ingot_t1", 1, 3, 4000)),
                    FirstClear("material_iron_ingot_t1", 4, 80),
                    "stage_02",
                    90),
                Difficulty(
                    "mine_master", 3600, 18, "hero_paladin_001", 3, 9000,
                    Table(
                        55,
                        Guaranteed(
                            "material_iron_ore_t1", 7, 10, 15000),
                        Chance("material_iron_ingot_t1", 2, 4, 5500)),
                    FirstClear("material_iron_ingot_t1", 6, 120),
                    "stage_03",
                    120)
            };

            return new List<DungeonDefinitionConfig>
            {
                new DungeonDefinitionConfig(
                    "dungeon_ore_mine",
                    "Mina de Minérios",
                    "Combates ativos por minérios e lingotes.",
                    CraftingProfession.Blacksmith,
                    "stage_01",
                    mineDifficulties,
                    null,
                    string.Empty,
                    "Disponibilidade futura validada pelo servidor.",
                    false,
                    "icon_pickaxe_placeholder",
                    new[] { "mine", "ore", "blacksmith" }),
                new DungeonDefinitionConfig(
                    "dungeon_forest",
                    "Floresta de Couros e Fibras",
                    "Caçada por couro cru e fibras de linho.",
                    CraftingProfession.Tailor,
                    "stage_01",
                    new[]
                    {
                        Difficulty(
                            "forest_apprentice", 1900, 10,
                            "hero_archer_001", 2, 6200,
                            Table(
                                0,
                                Guaranteed("material_raw_hide_t1", 3, 5),
                                Chance(
                                    "material_linen_fiber_t1", 2, 4, 6500)),
                            FirstClear("material_linen_fiber_t1", 6, 40),
                            "stage_01",
                            75)
                    },
                    5,
                    string.Empty,
                    string.Empty,
                    false,
                    "icon_forest_placeholder",
                    new[] { "forest", "hide", "fiber", "tailor" }),
                new DungeonDefinitionConfig(
                    "dungeon_arcane_ruins",
                    "Ruínas Arcanas",
                    "Recupere essências em ruínas instáveis.",
                    CraftingProfession.Enchanter,
                    "stage_02",
                    new[]
                    {
                        Difficulty(
                            "ruins_apprentice", 2400, 12,
                            "hero_mage_001", 2, 7200,
                            Table(
                                0,
                                Guaranteed(
                                    "material_arcane_essence_t1", 2, 4),
                                Chance(
                                    "material_arcane_essence_t1", 1, 2, 3000)),
                            FirstClear(
                                "material_arcane_essence_t1", 5, 60),
                            "stage_02",
                            90)
                    },
                    4,
                    string.Empty,
                    string.Empty,
                    false,
                    "icon_ruins_placeholder",
                    new[] { "ruins", "arcane", "enchanter" }),
                new DungeonDefinitionConfig(
                    "dungeon_abandoned_lab",
                    "Laboratório Abandonado",
                    "Colete ervas e reagentes alquímicos preservados.",
                    CraftingProfession.Alchemist,
                    "stage_02",
                    new[]
                    {
                        Difficulty(
                            "lab_apprentice", 2300, 12,
                            "hero_mage_001", 2, 7000,
                            Table(
                                0,
                                Guaranteed("material_wild_herb_t1", 3, 5),
                                Chance(
                                    "material_alchemical_reagent_t1",
                                    1,
                                    2,
                                    5000)),
                            FirstClear(
                                "material_alchemical_reagent_t1", 4, 60),
                            "stage_02",
                            85)
                    },
                    4,
                    string.Empty,
                    string.Empty,
                    false,
                    "icon_lab_placeholder",
                    new[] { "laboratory", "herb", "alchemist" }),
                new DungeonDefinitionConfig(
                    "dungeon_gathering_expedition",
                    "Expedição de Coleta",
                    "Rota ativa com lotes mistos de matéria-prima.",
                    CraftingProfession.Gatherer,
                    "stage_01",
                    new[]
                    {
                        Difficulty(
                            "expedition_apprentice", 2000, 11,
                            "hero_archer_001", 2, 6500,
                            Table(
                                0,
                                Guaranteed("material_iron_ore_t1", 2, 4),
                                Chance(
                                    "material_raw_hide_t1", 2, 3, 6000),
                                Chance(
                                    "material_wild_herb_t1", 1, 3, 6000)),
                            FirstClear("material_iron_ore_t1", 5, 45),
                            "stage_01",
                            80)
                    },
                    6,
                    string.Empty,
                    string.Empty,
                    false,
                    "icon_expedition_placeholder",
                    new[] { "expedition", "gathering", "gatherer" })
            };
        }
    }
}
