using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Gacha;
using IdleMedievalLegends.Domain.Heroes;
using UnityEditor;
using UnityEngine;

namespace IdleMedievalLegends.Editor.Gacha
{
    public sealed class GachaSimulationWindow : EditorWindow
    {
        private const string CatalogPath =
            "Assets/_Game/Data/Content/ContentCatalog.asset";
        private const string CombatBalancePath =
            "Assets/_Game/Data/Balance/CombatBalanceConfig.asset";

        private int simulationPullCount = 10000;
        private long seed = 11011;
        private int requestSequence;
        private Vector2 scroll;
        private GachaBannerDefinition banner;
        private GachaSimulator simulator;
        private CombatBalanceTuning combatTuning;
        private Dictionary<string, HeroInstance> heroes;
        private GachaPullResult lastPull;
        private GachaSimulationReport lastReport;
        private string status = string.Empty;

        [MenuItem(
            "Tools/Idle Medieval Legends/Economy/Run Gacha Simulation",
            priority = 160)]
        public static void Open()
        {
            GetWindow<GachaSimulationWindow>("Gacha DEV");
        }

        public static void RunValidationSimulations()
        {
            GachaBannerDefinition validationBanner =
                GachaDevelopmentContent.CreateMainBanner();
            HashSet<string> heroIds = GachaDevelopmentContent.GetDemoHeroIds();
            var costs = new Dictionary<string, long>(StringComparer.Ordinal)
            {
                { "hero_paladin_001", 80 },
                { "hero_archer_001", 50 },
                { "hero_mage_001", 80 }
            };
            string directory = Path.Combine(
                Path.GetTempPath(),
                "IdleMedievalLegends-validation",
                "gacha");
            Directory.CreateDirectory(directory);
            int[] pullCounts = { 10000, 100000, 1000000 };
            for (int i = 0; i < pullCounts.Length; i++)
            {
                GachaSimulationReport report = GachaSimulator.SimulateAggregate(
                    validationBanner,
                    pullCounts[i],
                    11011,
                    0,
                    heroIds,
                    costs);
                string path = Path.Combine(
                    directory,
                    $"gacha-{pullCounts[i]}-seed-11011.json");
                File.WriteAllText(path, JsonUtility.ToJson(report, true), Encoding.UTF8);
                Debug.Log(FormatSummary(report) + $" | Arquivo={path}");
            }
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                ContentCatalogAsset catalogAsset =
                    AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(CatalogPath);
                CombatBalanceConfigAsset balanceAsset =
                    AssetDatabase.LoadAssetAtPath<CombatBalanceConfigAsset>(
                        CombatBalancePath);
                if (catalogAsset == null || balanceAsset == null)
                    throw new InvalidOperationException(
                        "Assets de catálogo/balanceamento não encontrados.");

                balanceAsset.EnsureInitialized();
                ContentCatalogLookup catalog = catalogAsset.BuildValidatedLookup();
                combatTuning = balanceAsset.Tuning;
                banner = GachaDevelopmentContent.CreateMainBanner();
                var heroIds = new HashSet<string>(StringComparer.Ordinal);
                heroes = new Dictionary<string, HeroInstance>(StringComparer.Ordinal);
                for (int i = 0; i < catalog.Catalog.Heroes.Count; i++)
                {
                    HeroDefinition definition = catalog.Catalog.Heroes[i];
                    heroIds.Add(definition.DefinitionId);
                    heroes.Add(
                        definition.DefinitionId,
                        HeroInstance.CreateLocked(
                            "gacha_dev_" + definition.DefinitionId,
                            definition.DefinitionId,
                            "gacha_editor_player",
                            definition.InitialRarity,
                            0,
                            combatTuning));
                }
                simulator = new GachaSimulator(
                    new DevelopmentGachaCurrency(10000000),
                    new FragmentWallet(),
                    heroIds);
                GachaProbabilityValidator.Validate(banner, heroIds).ThrowIfInvalid();
                status = "Sessão local inicializada.";
            }
            catch (Exception exception)
            {
                status = exception.Message;
                simulator = null;
            }
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox(
                "AMBIENTE DE DESENVOLVIMENTO — moeda simulada, sem Gemas, " +
                "IAP, anúncios, loja ou autoridade de produção.",
                MessageType.Warning);
            if (simulator == null || banner == null)
            {
                EditorGUILayout.HelpBox(status, MessageType.Error);
                if (GUILayout.Button("Tentar inicializar"))
                    Initialize();
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField("Banner", banner.DisplayName);
            EditorGUILayout.LabelField("ID", banner.BannerId);
            EditorGUILayout.LabelField("Versão", banner.RulesVersion);
            EditorGUILayout.LabelField(
                "Moeda",
                GachaCurrencyType.DevelopmentGachaCurrency.ToString());
            EditorGUILayout.LabelField(
                "Saldo simulado",
                simulator.Currency.Balance.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField(
                "Custos",
                $"1x {banner.CostSingle} | {banner.MultiPullCount}x {banner.CostMulti}");
            EditorGUILayout.LabelField(
                "Pity",
                $"{simulator.GetPityState(banner).PullsSinceHighRarity}/" +
                $"{banner.HardPityThreshold} | soft {banner.SoftPityStart}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Probabilidades-base", EditorStyles.boldLabel);
            long totalWeight = 0;
            for (int i = 0; i < banner.PoolEntries.Count; i++)
                totalWeight += banner.PoolEntries[i].Weight;
            for (int i = 0; i < banner.PoolEntries.Count; i++)
            {
                GachaPoolEntry entry = banner.PoolEntries[i];
                double percent = 100d * entry.Weight / totalWeight;
                EditorGUILayout.LabelField(
                    entry.EntryId,
                    $"{percent:F2}% | {entry.Rarity} | x{entry.FragmentQuantity}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pull simples"))
                Pull(1);
            if (GUILayout.Button($"Multi-pull ({banner.MultiPullCount})"))
                Pull(banner.MultiPullCount);
            EditorGUILayout.EndHorizontal();
            if (lastPull != null)
            {
                EditorGUILayout.LabelField(
                    "Último resultado",
                    $"{lastPull.Rewards.Count} recompensas | custo " +
                    lastPull.ChargedCost);
                for (int i = 0; i < lastPull.Rewards.Count; i++)
                {
                    GachaReward reward = lastPull.Rewards[i];
                    EditorGUILayout.LabelField(
                        $"#{i + 1}",
                        $"{reward.EntryId} | {reward.Rarity} | x{reward.Quantity}" +
                        (reward.HardPityApplied ? " | HARD PITY" : string.Empty));
                }
            }

            DrawFragmentsAndUnlocks();
            DrawSimulation();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void DrawFragmentsAndUnlocks()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fragmentos e heróis", EditorStyles.boldLabel);
            foreach (KeyValuePair<string, HeroInstance> pair in heroes)
            {
                long fragments = simulator.FragmentWallet.GetBalance(pair.Key);
                HeroInstance hero = pair.Value;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    pair.Key,
                    $"{fragments} fragmentos | " +
                    (hero.Unlocked ? "Desbloqueado" : "Bloqueado"));
                using (new EditorGUI.DisabledScope(
                           !BannerEligibilityRules.CanUnlock(
                               simulator.FragmentWallet,
                               hero,
                               combatTuning)))
                {
                    if (GUILayout.Button("Desbloquear", GUILayout.Width(100)))
                    {
                        heroes[pair.Key] = BannerEligibilityRules.Unlock(
                            simulator.FragmentWallet,
                            hero,
                            combatTuning);
                        status = pair.Key + " desbloqueado; excedentes preservados.";
                        GUIUtility.ExitGUI();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSimulation()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Simulação agregada", EditorStyles.boldLabel);
            simulationPullCount = EditorGUILayout.IntField(
                "Número de pulls",
                simulationPullCount);
            seed = EditorGUILayout.LongField("Seed explícita", seed);
            if (GUILayout.Button("Executar simulação agregada"))
            {
                try
                {
                    lastReport = GachaSimulator.SimulateAggregate(
                        banner,
                        simulationPullCount,
                        seed,
                        0,
                        GachaDevelopmentContent.GetDemoHeroIds(),
                        BuildUnlockCosts());
                    status = FormatSummary(lastReport);
                }
                catch (Exception exception)
                {
                    status = exception.Message;
                }
            }
            if (lastReport == null)
                return;
            EditorGUILayout.LabelField(
                "Média até alta raridade",
                lastReport.AveragePullsToHighRarity.ToString("F3"));
            EditorGUILayout.LabelField(
                "Máximo observado",
                lastReport.MaximumPullsToHighRarity.ToString());
            EditorGUILayout.LabelField(
                "Hard pity",
                lastReport.HardPityActivations.ToString());
            EditorGUILayout.LabelField(
                "Featured rate",
                lastReport.FeaturedRate.ToString("P2"));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Exportar JSON"))
                Export(lastReport, "json");
            if (GUILayout.Button("Exportar CSV"))
                Export(lastReport, "csv");
            EditorGUILayout.EndHorizontal();
        }

        private void Pull(int count)
        {
            try
            {
                requestSequence++;
                lastPull = simulator.Execute(
                    banner,
                    new GachaPullRequest(
                        $"editor_request_{requestSequence}",
                        banner.BannerId,
                        count,
                        checked(seed + requestSequence),
                        0,
                        requestSequence * 100L));
                status = "Pull concluído com sequência e pity persistidos localmente.";
            }
            catch (Exception exception)
            {
                status = exception.Message;
            }
        }

        private Dictionary<string, long> BuildUnlockCosts()
        {
            var result = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, HeroInstance> pair in heroes)
            {
                result[pair.Key] = HeroProgressionRules.GetUnlockFragmentCost(
                    pair.Value.Rarity,
                    combatTuning);
            }
            return result;
        }

        private void Export(GachaSimulationReport report, string extension)
        {
            string path = EditorUtility.SaveFilePanel(
                "Exportar relatório de gacha",
                string.Empty,
                $"gacha-{report.PullCount}-seed-{report.Seed}.{extension}",
                extension);
            if (string.IsNullOrWhiteSpace(path))
                return;
            string contents = extension == "json"
                ? JsonUtility.ToJson(report, true)
                : BuildCsv(report);
            File.WriteAllText(path, contents, Encoding.UTF8);
            status = "Relatório exportado para " + path;
        }

        private static string BuildCsv(GachaSimulationReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("metric,key,value");
            builder.AppendLine($"pulls,,{report.PullCount}");
            builder.AppendLine(
                $"average_pulls_to_high,,{report.AveragePullsToHighRarity.ToString(CultureInfo.InvariantCulture)}");
            builder.AppendLine($"maximum_pulls_to_high,,{report.MaximumPullsToHighRarity}");
            builder.AppendLine($"hard_pity_activations,,{report.HardPityActivations}");
            builder.AppendLine(
                $"featured_rate,,{report.FeaturedRate.ToString(CultureInfo.InvariantCulture)}");
            builder.AppendLine(
                $"average_cost_per_unlock,,{report.AverageCostPerUnlock.ToString(CultureInfo.InvariantCulture)}");
            for (int i = 0; i < report.RewardFrequencies.Count; i++)
            {
                GachaFrequency value = report.RewardFrequencies[i];
                builder.AppendLine($"reward,{value.Key},{value.Count}");
            }
            for (int i = 0; i < report.RarityFrequencies.Count; i++)
            {
                GachaFrequency value = report.RarityFrequencies[i];
                builder.AppendLine($"rarity,{value.Key},{value.Count}");
            }
            for (int i = 0; i < report.AverageFragmentsByHero.Count; i++)
            {
                GachaAverageFragments value = report.AverageFragmentsByHero[i];
                builder.AppendLine(
                    $"fragments,{value.HeroDefinitionId}," +
                    value.AveragePerPull.ToString(CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static string FormatSummary(GachaSimulationReport report)
        {
            return $"[GachaSimulation] Pulls={report.PullCount} | Seed={report.Seed} | " +
                   $"MédiaAlta={report.AveragePullsToHighRarity:F3} | " +
                   $"Máximo={report.MaximumPullsToHighRarity} | " +
                   $"HardPity={report.HardPityActivations} | " +
                   $"Featured={report.FeaturedRate:P2} | " +
                   $"Custo/Unlock={report.AverageCostPerUnlock:F2}";
        }
    }
}
