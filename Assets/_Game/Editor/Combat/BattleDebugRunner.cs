using System;
using System.Linq;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;
using UnityEditor;
using UnityEngine;

namespace IdleMedievalLegends.Editor.Combat
{
    public static class BattleDebugRunner
    {
        private const string CatalogPath =
            "Assets/_Game/Data/Content/ContentCatalog.asset";
        private const string CombatBalancePath =
            "Assets/_Game/Data/Balance/CombatBalanceConfig.asset";
        private const long DemoSeed = 5005;
        private const string RulesVersion = "combat_rules_v1";

        [MenuItem(
            "Tools/Idle Medieval Legends/Combat/Run Deterministic Demo Battle",
            priority = 140)]
        public static void RunFromMenu()
        {
            BattleResult result = RunDemoBattle();
            LogSummary(result);
        }

        public static void RunFromCommandLine()
        {
            BattleResult first = RunDemoBattle();
            BattleResult second = RunDemoBattle();
            BattleResult third = RunDemoBattle();
            if (!string.Equals(
                    first.DeterministicHash,
                    second.DeterministicHash,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    first.DeterministicHash,
                    third.DeterministicHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A batalha demonstrativa produziu hashes divergentes.");
            }
            LogSummary(first);
            Debug.Log($"[BattleDebug] Hash repetido 3x: {first.DeterministicHash}");
        }

        private static BattleResult RunDemoBattle()
        {
            ContentCatalogAsset catalogAsset =
                AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(CatalogPath);
            CombatBalanceConfigAsset balanceAsset =
                AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(CombatBalancePath);
            if (catalogAsset == null || balanceAsset == null)
                throw new InvalidOperationException("Assets de catálogo/balanceamento ausentes.");

            balanceAsset.EnsureInitialized();
            ContentCatalogLookup catalog = catalogAsset.BuildValidatedLookup();
            var battleConfiguration = new BattleConfiguration(maximumActions: 200);
            HeroDefinition paladin = catalog.GetHero("hero_paladin_001");
            HeroDefinition archer = catalog.GetHero("hero_archer_001");
            HeroDefinition mage = catalog.GetHero("hero_mage_001");

            BattleUnit paladinUnit = CreateUnit(
                "debug_paladin", paladin, BattleSide.Attacker, 0,
                balanceAsset.Tuning, battleConfiguration);
            BattleUnit archerUnit = CreateUnit(
                "debug_archer", archer, BattleSide.Attacker, 1,
                balanceAsset.Tuning, battleConfiguration);
            BattleUnit mageUnit = CreateUnit(
                "debug_mage", mage, BattleSide.Defender, 0,
                balanceAsset.Tuning, battleConfiguration);
            var request = new BattleRequest(
                new BattleTeam(BattleSide.Attacker, new[] { paladinUnit, archerUnit }),
                new BattleTeam(BattleSide.Defender, new[] { mageUnit }),
                DemoSeed,
                battleConfiguration,
                RulesVersion,
                "editor_demo_battle");
            return new BattleSimulator().Simulate(request);
        }

        private static BattleUnit CreateUnit(
            string instanceId,
            HeroDefinition definition,
            BattleSide side,
            int slot,
            CombatBalanceTuning heroTuning,
            BattleConfiguration battleConfiguration)
        {
            HeroInstance hero = HeroInstance.Restore(
                instanceId,
                definition.DefinitionId,
                "editor_debug_player",
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

        private static void LogSummary(BattleResult result)
        {
            long totalDamage = result.Events
                .Where(value => value.EventType == CombatEventType.DamageDealt)
                .Sum(value => value.Value);
            int defeated = result.Events.Count(
                value => value.EventType == CombatEventType.UnitDefeated);
            Debug.Log(
                $"[BattleDebug] Seed={result.Seed} | Vencedor=" +
                $"{result.WinningTeam?.ToString() ?? "Empate"} | " +
                $"Turnos={result.TurnCount} | Ações={result.ActionCount} | " +
                $"Dano total={totalDamage} | Derrotados={defeated} | " +
                $"Hash={result.DeterministicHash}");
        }
    }
}
