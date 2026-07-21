using System;
using System.Collections.Generic;
using System.Text;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Content;
using UnityEditor;
using UnityEngine;

namespace IdleMedievalLegends.Editor.ContentCatalog
{
    public static class ContentCatalogEditorTools
    {
        public const string CatalogAssetPath =
            "Assets/_Game/Data/Content/ContentCatalog.asset";

        [MenuItem(
            "Tools/Idle Medieval Legends/Content/Generate or Reset Demo Catalog",
            priority = 120)]
        public static void GenerateDemoFromMenu()
        {
            ContentCatalogAsset asset = GenerateOrResetDemoCatalog();
            Selection.activeObject = asset;
            LogValidation(asset);
        }

        [MenuItem(
            "Tools/Idle Medieval Legends/Validate Content Catalog",
            priority = 121)]
        public static void ValidateFromMenu()
        {
            ValidateAllCatalogAssets(false);
        }

        public static void ValidateFromCommandLine()
        {
            ValidateAllCatalogAssets(true);
        }

        public static ContentCatalogAsset LoadOrCreateDemoCatalog()
        {
            ContentCatalogAsset asset =
                AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(CatalogAssetPath);
            if (asset != null)
                return asset;

            return GenerateOrResetDemoCatalog();
        }

        public static ContentCatalogAsset GenerateOrResetDemoCatalog()
        {
            EnsureFolder("Assets/_Game/Data/Content");
            ContentCatalogAsset asset =
                AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(CatalogAssetPath);
            if (asset == null)
            {
                UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(CatalogAssetPath);
                if (existing != null)
                {
                    throw new InvalidOperationException(
                        $"Asset incompatível já existe em {CatalogAssetPath}.");
                }

                asset = ScriptableObject.CreateInstance<ContentCatalogAsset>();
                AssetDatabase.CreateAsset(asset, CatalogAssetPath);
            }

            asset.ReplaceDefinitionsForEditor(ContentCatalogDemoFactory.Create());
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(CatalogAssetPath);
        }

        public static ContentCatalogValidationReport ValidateAsset(ContentCatalogAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            return asset.ValidateCatalog();
        }

        private static void ValidateAllCatalogAssets(bool throwOnErrors)
        {
            string[] guids = AssetDatabase.FindAssets("t:ContentCatalogAsset");
            if (guids.Length == 0)
            {
                const string message = "Nenhum ContentCatalogAsset foi encontrado.";
                Debug.LogError($"[ContentCatalogValidation] {message}");
                if (throwOnErrors)
                    throw new InvalidOperationException(message);
                return;
            }

            int totalErrors = 0;
            int totalWarnings = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ContentCatalogAsset asset =
                    AssetDatabase.LoadAssetAtPath<ContentCatalogAsset>(path);
                ContentCatalogValidationReport report = LogValidation(asset);
                totalErrors += report.ErrorCount;
                totalWarnings += report.WarningCount;
            }

            Debug.Log(
                $"[ContentCatalogValidation] Assets={guids.Length}, " +
                $"erros={totalErrors}, avisos={totalWarnings}.");

            if (throwOnErrors && totalErrors > 0)
            {
                throw new InvalidOperationException(
                    $"Catálogo de conteúdo inválido: {totalErrors} erro(s), " +
                    $"{totalWarnings} aviso(s). Consulte o log.");
            }
        }

        private static ContentCatalogValidationReport LogValidation(ContentCatalogAsset asset)
        {
            ContentCatalogValidationReport report = asset.ValidateCatalog();
            for (int i = 0; i < report.Messages.Count; i++)
            {
                ContentValidationMessage message = report.Messages[i];
                string formatted = "[ContentCatalogValidation] " + message;
                if (message.Severity == ContentValidationSeverity.Error)
                    Debug.LogError(formatted, asset);
                else
                    Debug.LogWarning(formatted, asset);
            }

            Debug.Log(BuildSummary(asset, report), asset);
            return report;
        }

        private static string BuildSummary(
            ContentCatalogAsset asset,
            ContentCatalogValidationReport report)
        {
            ContentCatalogSummary summary = report.Summary;
            var builder = new StringBuilder();
            builder.Append("[ContentCatalogValidation] ");
            builder.Append(AssetDatabase.GetAssetPath(asset));
            builder.Append($": heróis={summary.HeroCount}, itens={summary.ItemCount}, ");
            builder.Append($"equipamentos={summary.EquipmentCount}, ");
            builder.Append($"materiais={summary.MaterialCount}, receitas={summary.RecipeCount}, ");
            builder.Append($"erros={report.ErrorCount}, avisos={report.WarningCount}. ");
            AppendCounts(builder, "Tiers", summary.TotalsByTier);
            AppendCounts(builder, "Raridades", summary.TotalsByRarity);
            AppendCounts(builder, "Profissões", summary.TotalsByProfession);
            return builder.ToString();
        }

        private static void AppendCounts<T>(
            StringBuilder builder,
            string label,
            IReadOnlyDictionary<T, int> counts)
        {
            builder.Append(label);
            builder.Append("=[");
            bool first = true;
            foreach (KeyValuePair<T, int> pair in counts)
            {
                if (!first)
                    builder.Append(", ");
                builder.Append(pair.Key);
                builder.Append('=');
                builder.Append(pair.Value);
                first = false;
            }
            builder.Append("] ");
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
