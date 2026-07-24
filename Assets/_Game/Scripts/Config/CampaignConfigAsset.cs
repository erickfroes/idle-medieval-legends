using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Campaign;
using UnityEngine;

namespace IdleMedievalLegends.Config
{
    [CreateAssetMenu(
        fileName = "CampaignConfig",
        menuName = "Idle Medieval Legends/Balance/Campaign Config")]
    public sealed class CampaignConfigAsset : ScriptableObject
    {
        public const string DefaultRulesVersion = "campaign_rules_v1";

        [Header("Campaign Growth")]
        [SerializeField, Min(1)] private long initialRecommendedPower = 1600;
        [SerializeField, Min(0)] private int recommendedPowerGrowthBasisPoints = 1800;
        [SerializeField, Min(0)] private long initialGoldPerMinute = 10;
        [SerializeField, Min(0)] private long goldPerStageIncrement = 3;
        [SerializeField, Min(1)] private int enemyBaseStatMultiplierBasisPoints = 6500;
        [SerializeField, Min(0)] private int enemyStatGrowthBasisPoints = 900;

        [Header("Offline")]
        [SerializeField, Min(0)] private int basePlayerOfflineHours = 8;
        [SerializeField, Min(0)] private int stageOfflineHours = 8;
        [SerializeField, Min(1)] private int maximumAbsoluteClockJumpHours = 720;
        [SerializeField, Min(0)] private int safeClockJumpHours = 8;

        public long InitialRecommendedPower => initialRecommendedPower;
        public int RecommendedPowerGrowthBasisPoints => recommendedPowerGrowthBasisPoints;
        public long InitialGoldPerMinute => initialGoldPerMinute;
        public long GoldPerStageIncrement => goldPerStageIncrement;
        public int EnemyBaseStatMultiplierBasisPoints => enemyBaseStatMultiplierBasisPoints;
        public int EnemyStatGrowthBasisPoints => enemyStatGrowthBasisPoints;
        public int BasePlayerOfflineHours => basePlayerOfflineHours;
        public int StageOfflineHours => stageOfflineHours;
        public int MaximumAbsoluteClockJumpHours => maximumAbsoluteClockJumpHours;
        public int SafeClockJumpHours => safeClockJumpHours;

        public CampaignDefinition BuildDefinition()
        {
            EnsureValid();
            var stages = new List<CampaignStageDefinition>(10);
            long recommendedPower = initialRecommendedPower;
            for (int sequence = 1; sequence <= 10; sequence++)
            {
                string chapterId = sequence <= 5 ? "chapter_frontier" : "chapter_highlands";
                bool boss = sequence == 5 || sequence == 10;
                int enemyCount = sequence <= 1 ? 1 : sequence <= 3 ? 2 : 3;
                int level = 1 + (sequence - 1) / 3;
                int statMultiplier = checked(
                    enemyBaseStatMultiplierBasisPoints +
                    enemyStatGrowthBasisPoints * (sequence - 1));
                var enemies = new List<StageEnemy>(enemyCount);
                string[] heroIds =
                {
                    "hero_paladin_001",
                    "hero_archer_001",
                    "hero_mage_001"
                };
                for (int slot = 0; slot < enemyCount; slot++)
                {
                    enemies.Add(new StageEnemy(
                        $"enemy_placeholder_{sequence:D2}_{slot + 1}",
                        heroIds[(sequence + slot - 1) % heroIds.Length],
                        slot,
                        level,
                        boss ? checked(statMultiplier + 1000) : statMultiplier,
                        new[] { "placeholder", boss ? "boss_guard" : "campaign_enemy" }));
                }

                var idleMaterials = new List<CampaignMaterialReward>
                {
                    new CampaignMaterialReward(
                        "material_iron_ore_t1",
                        1 + (sequence - 1) / 3)
                };
                if (sequence >= 3)
                {
                    idleMaterials.Add(new CampaignMaterialReward(
                        "material_raw_hide_t1",
                        1 + (sequence - 3) / 4));
                }
                if (sequence >= 7)
                {
                    idleMaterials.Add(new CampaignMaterialReward(
                        "material_arcane_essence_t1",
                        1));
                }

                var firstClearMaterials = new List<CampaignMaterialReward>
                {
                    new CampaignMaterialReward(
                        "material_iron_ore_t1",
                        checked(sequence * 5L))
                };
                if (sequence >= 5)
                {
                    firstClearMaterials.Add(new CampaignMaterialReward(
                        "material_arcane_essence_t1",
                        sequence));
                }
                var repeatMaterials = new[]
                {
                    new CampaignMaterialReward(
                        "material_iron_ore_t1",
                        Math.Max(1, sequence / 2))
                };

                stages.Add(new CampaignStageDefinition(
                    $"stage_{sequence:D2}",
                    chapterId,
                    sequence,
                    new StageEnemyFormation(enemies),
                    recommendedPower,
                    checked(initialGoldPerMinute + goldPerStageIncrement * (sequence - 1)),
                    idleMaterials,
                    new StageRewardDefinition(
                        checked(sequence * 100L),
                        firstClearMaterials,
                        checked(sequence * 25L)),
                    new StageRewardDefinition(
                        checked(sequence * 10L),
                        repeatMaterials,
                        checked(sequence * 2L)),
                    idleUnlocked: true,
                    boss: boss,
                    maximumOfflineHours: stageOfflineHours,
                    tags: new[]
                    {
                        sequence <= 5 ? "frontier" : "highlands",
                        boss ? "boss" : "normal"
                    }));

                recommendedPower = checked(
                    recommendedPower +
                    recommendedPower * recommendedPowerGrowthBasisPoints / 10000L);
            }

            return new CampaignDefinition(
                "campaign_demo",
                DefaultRulesVersion,
                new[]
                {
                    new CampaignChapterDefinition(
                        "chapter_frontier",
                        "Fronteira",
                        1,
                        stages.GetRange(0, 5)),
                    new CampaignChapterDefinition(
                        "chapter_highlands",
                        "Terras Altas",
                        2,
                        stages.GetRange(5, 5))
                });
        }

        public void EnsureValid()
        {
            initialRecommendedPower = Math.Max(1, initialRecommendedPower);
            recommendedPowerGrowthBasisPoints =
                Math.Max(0, recommendedPowerGrowthBasisPoints);
            initialGoldPerMinute = Math.Max(0, initialGoldPerMinute);
            goldPerStageIncrement = Math.Max(0, goldPerStageIncrement);
            enemyBaseStatMultiplierBasisPoints =
                Math.Max(1, enemyBaseStatMultiplierBasisPoints);
            enemyStatGrowthBasisPoints = Math.Max(0, enemyStatGrowthBasisPoints);
            basePlayerOfflineHours = Math.Max(0, basePlayerOfflineHours);
            stageOfflineHours = Math.Max(0, stageOfflineHours);
            maximumAbsoluteClockJumpHours = Math.Max(1, maximumAbsoluteClockJumpHours);
            safeClockJumpHours = Math.Max(0, safeClockJumpHours);
        }

        private void OnValidate()
        {
            EnsureValid();
        }
    }
}
