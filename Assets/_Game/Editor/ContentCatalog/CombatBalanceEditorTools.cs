using System;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Heroes;
using UnityEditor;
using UnityEngine;

namespace IdleMedievalLegends.Editor.ContentCatalog
{
    public static class CombatBalanceEditorTools
    {
        [MenuItem(
            "Tools/Idle Medieval Legends/Balance/Upgrade Combat Balance Assets",
            priority = 130)]
        public static void UpgradeFromMenu()
        {
            int upgraded = UpgradeAllAssets();
            Debug.Log($"[CombatBalance] {upgraded} asset(s) validado(s) e salvo(s) na versão " +
                      $"{CombatBalanceTuningMigration.CurrentVersion}.");
        }

        public static void UpgradeFromCommandLine()
        {
            int upgraded = UpgradeAllAssets();
            if (upgraded == 0)
                throw new InvalidOperationException("Nenhum CombatBalanceConfigAsset encontrado.");
        }

        private static int UpgradeAllAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:CombatBalanceConfigAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CombatBalanceConfigAsset asset =
                    AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(path);
                if (asset == null)
                    throw new InvalidOperationException($"Asset inválido em {path}.");

                asset.EnsureInitialized();
                HeroBalanceTuningValidator.Validate(asset.Tuning);
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            return guids.Length;
        }
    }
}
