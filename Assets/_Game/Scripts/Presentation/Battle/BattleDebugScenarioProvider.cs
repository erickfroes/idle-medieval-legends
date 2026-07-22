using System;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;
using UnityEngine;

namespace IdleMedievalLegends.Presentation.Battle
{
    public static class BattleDebugScenarioFactory
    {
        public const string RulesVersion = "combat_rules_v1";

        public static BattleDebugScenario Create(
            ContentCatalogLookup catalog,
            CombatBalanceTuning heroTuning,
            long seed,
            int maximumActions = 200)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (heroTuning == null) throw new ArgumentNullException(nameof(heroTuning));
            if (seed <= 0) throw new ArgumentOutOfRangeException(nameof(seed));
            if (maximumActions < 1) throw new ArgumentOutOfRangeException(nameof(maximumActions));

            var battleConfiguration = new BattleConfiguration(
                maximumActions: maximumActions,
                basicAttackMultiplier: 2.25d,
                defaultCriticalChance: 0.12d,
                defaultAccuracy: 0.97d,
                defaultEvasion: 0.03d,
                targetSelectionMode: TargetSelectionMode.Random);

            HeroDefinition paladin = catalog.GetHero("hero_paladin_001");
            HeroDefinition archer = catalog.GetHero("hero_archer_001");
            HeroDefinition mage = catalog.GetHero("hero_mage_001");

            BattleUnit[] attackers =
            {
                CreateUnit(
                    "player_paladin", paladin, BattleSide.Attacker, 0,
                    "debug_player", heroTuning, battleConfiguration),
                CreateUnit(
                    "player_archer", archer, BattleSide.Attacker, 1,
                    "debug_player", heroTuning, battleConfiguration),
                CreateUnit(
                    "player_mage", mage, BattleSide.Attacker, 2,
                    "debug_player", heroTuning, battleConfiguration)
            };
            BattleUnit[] defenders =
            {
                CreateUnit(
                    "enemy_debug_paladin", paladin, BattleSide.Defender, 0,
                    "debug_enemy", heroTuning, battleConfiguration),
                CreateUnit(
                    "enemy_debug_archer", archer, BattleSide.Defender, 1,
                    "debug_enemy", heroTuning, battleConfiguration),
                CreateUnit(
                    "enemy_debug_mage", mage, BattleSide.Defender, 2,
                    "debug_enemy", heroTuning, battleConfiguration)
            };

            var request = new BattleRequest(
                new BattleTeam(BattleSide.Attacker, attackers),
                new BattleTeam(BattleSide.Defender, defenders),
                seed,
                battleConfiguration,
                RulesVersion,
                $"visual_debug_{seed}");
            BattleResult result = new BattleSimulator().Simulate(request);
            return new BattleDebugScenario(catalog, request, result);
        }

        private static BattleUnit CreateUnit(
            string instanceId,
            HeroDefinition definition,
            BattleSide side,
            int slot,
            string ownerId,
            CombatBalanceTuning heroTuning,
            BattleConfiguration battleConfiguration)
        {
            HeroInstance hero = HeroInstance.Restore(
                instanceId,
                definition.DefinitionId,
                ownerId,
                1,
                0,
                definition.InitialRarity,
                0,
                0,
                null,
                true,
                0,
                0,
                null,
                0,
                heroTuning);
            return BattleUnitFactory.FromHero(
                hero,
                definition,
                EmptyHeroEquipmentModifierProvider.Instance,
                heroTuning,
                battleConfiguration,
                side,
                slot);
        }
    }

    public sealed class BattleDebugScenarioProvider : MonoBehaviour
    {
        [SerializeField] private ContentCatalogAsset contentCatalog;
        [SerializeField] private CombatBalanceConfigAsset combatBalance;
        [SerializeField] private long seed = 6006;
        [SerializeField, Min(1)] private int maximumActions = 200;

        public long Seed => seed;
        public ContentCatalogAsset ContentCatalog => contentCatalog;
        public CombatBalanceConfigAsset CombatBalance => combatBalance;
        public bool IsConfigured => contentCatalog != null && combatBalance != null &&
            seed > 0 && maximumActions > 0;

        public void Configure(
            ContentCatalogAsset catalog,
            CombatBalanceConfigAsset balance,
            long scenarioSeed,
            int actionLimit = 200)
        {
            contentCatalog = catalog != null
                ? catalog
                : throw new ArgumentNullException(nameof(catalog));
            combatBalance = balance != null
                ? balance
                : throw new ArgumentNullException(nameof(balance));
            if (scenarioSeed <= 0)
                throw new ArgumentOutOfRangeException(nameof(scenarioSeed));
            if (actionLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(actionLimit));

            seed = scenarioSeed;
            maximumActions = actionLimit;
        }

        public BattleDebugScenario CreateScenario()
        {
            if (contentCatalog == null || combatBalance == null)
            {
                throw new InvalidOperationException(
                    "O cenário de debug requer catálogo e balanceamento explícitos.");
            }

            combatBalance.EnsureInitialized();
            return BattleDebugScenarioFactory.Create(
                contentCatalog.BuildValidatedLookup(),
                combatBalance.Tuning,
                seed,
                maximumActions);
        }

        private void OnValidate()
        {
            if (seed <= 0)
                seed = 6006;
            maximumActions = Math.Max(1, maximumActions);
        }
    }
}
