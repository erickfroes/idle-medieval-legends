using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdleMedievalLegends.Editor.ProjectValidation
{
    public static class ProjectValidator
    {
        private const string ExpectedEditorVersion = "6000.5.4f1";

        private static readonly RequiredAssembly[] RequiredAssemblies =
        {
            new RequiredAssembly(
                "Assets/_Game/Scripts/Domain/IdleMedievalLegends.Domain.asmdef",
                "IdleMedievalLegends.Domain"),
            new RequiredAssembly(
                "Assets/_Game/Scripts/Application/IdleMedievalLegends.Application.asmdef",
                "IdleMedievalLegends.Application"),
            new RequiredAssembly(
                "Assets/_Game/Scripts/Infrastructure/IdleMedievalLegends.Infrastructure.asmdef",
                "IdleMedievalLegends.Infrastructure"),
            new RequiredAssembly(
                "Assets/_Game/Scripts/Config/IdleMedievalLegends.Config.asmdef",
                "IdleMedievalLegends.Config"),
            new RequiredAssembly(
                "Assets/_Game/Tests/EditMode/IdleMedievalLegends.Tests.EditMode.asmdef",
                "IdleMedievalLegends.Tests.EditMode"),
            new RequiredAssembly(
                "Assets/_Game/Tests/PlayMode/IdleMedievalLegends.Tests.PlayMode.asmdef",
                "IdleMedievalLegends.Tests.PlayMode"),
            new RequiredAssembly(
                "Assets/_Game/Editor/ProjectValidation/" +
                "IdleMedievalLegends.Editor.ProjectValidation.asmdef",
                "IdleMedievalLegends.Editor.ProjectValidation"),
            new RequiredAssembly(
                "Assets/_Game/Editor/Bootstrap/" +
                "IdleMedievalLegends.Editor.Bootstrap.asmdef",
                "IdleMedievalLegends.Editor.Bootstrap"),
            new RequiredAssembly(
                "Assets/_Game/Editor/ContentCatalog/" +
                "IdleMedievalLegends.Editor.ContentCatalog.asmdef",
                "IdleMedievalLegends.Editor.ContentCatalog")
        };

        private static readonly string[] RequiredDocuments =
        {
            "AGENTS.md",
            "Docs/Architecture_GDD.md",
            "Docs/README.md",
            "Docs/PROJECT_STRUCTURE.md"
        };

        private static readonly string[] RequiredConfigurationFiles =
        {
            "Packages/manifest.json",
            "ProjectSettings/ProjectVersion.txt",
            "ProjectSettings/ProjectSettings.asset",
            "ProjectSettings/GraphicsSettings.asset",
            "ProjectSettings/QualitySettings.asset",
            "ProjectSettings/EditorBuildSettings.asset",
            "ProjectSettings/URPProjectSettings.asset",
            "Assets/Settings/Mobile_RPAsset.asset",
            "Assets/Settings/PC_RPAsset.asset",
            "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset"
        };

        [MenuItem("Tools/Idle Medieval Legends/Validate Project", priority = 100)]
        public static void ValidateFromMenu()
        {
            ValidationReport report = Validate();
            Log(report);
        }

        // Entry point for: -executeMethod
        // IdleMedievalLegends.Editor.ProjectValidation.ProjectValidator.ValidateFromCommandLine
        public static void ValidateFromCommandLine()
        {
            ValidationReport report = Validate();
            Log(report);

            if (report.Errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Project validation failed with {report.Errors.Count} error(s).");
            }
        }

        private static ValidationReport Validate()
        {
            var report = new ValidationReport();

            ValidateFiles(RequiredDocuments, "documento obrigatório", report);
            ValidateFiles(RequiredConfigurationFiles, "configuração essencial", report);
            ValidateAssemblies(report);
            ValidateEditorVersion(report);
            ValidatePackages(report);
            ValidateRenderPipeline(report);
            ValidateBuildScenes(report);
            ValidateMobileIdentity(report);

            return report;
        }

        private static void ValidateAssemblies(ValidationReport report)
        {
            for (int i = 0; i < RequiredAssemblies.Length; i++)
            {
                RequiredAssembly assembly = RequiredAssemblies[i];
                string absolutePath = ToAbsolutePath(assembly.Path);

                if (!File.Exists(absolutePath))
                {
                    report.Errors.Add($"Assembly ausente: {assembly.Path}");
                    continue;
                }

                string contents = File.ReadAllText(absolutePath);
                string expectedName = $"\"name\": \"{assembly.Name}\"";
                if (contents.IndexOf(expectedName, StringComparison.Ordinal) < 0)
                {
                    report.Errors.Add(
                        $"Assembly {assembly.Path} não declara o nome esperado " +
                        $"'{assembly.Name}'.");
                }
            }
        }

        private static void ValidateEditorVersion(ValidationReport report)
        {
            string versionPath = ToAbsolutePath("ProjectSettings/ProjectVersion.txt");
            if (!File.Exists(versionPath))
                return;

            string contents = File.ReadAllText(versionPath);
            string expectedEntry = $"m_EditorVersion: {ExpectedEditorVersion}";
            if (contents.IndexOf(expectedEntry, StringComparison.Ordinal) < 0)
            {
                report.Errors.Add(
                    $"Versão do Unity divergente. Esperada: {ExpectedEditorVersion}.");
            }
        }

        private static void ValidatePackages(ValidationReport report)
        {
            string manifestPath = ToAbsolutePath("Packages/manifest.json");
            if (!File.Exists(manifestPath))
                return;

            string manifest = File.ReadAllText(manifestPath);
            RequireText(
                manifest,
                "com.unity.render-pipelines.universal",
                "Pacote Universal Render Pipeline ausente do manifesto.",
                report);
            RequireText(
                manifest,
                "com.unity.test-framework",
                "Pacote Unity Test Framework ausente do manifesto.",
                report);
        }

        private static void ValidateRenderPipeline(ValidationReport report)
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                report.Errors.Add(
                    "Graphics Settings não possui um Render Pipeline Asset padrão.");
            }
        }

        private static void ValidateBuildScenes(ValidationReport report)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].enabled && !string.IsNullOrWhiteSpace(scenes[i].path))
                    return;
            }

            report.Errors.Add("Build Settings não possui uma cena habilitada.");
        }

        private static void ValidateMobileIdentity(ValidationReport report)
        {
            if (string.Equals(PlayerSettings.companyName, "DefaultCompany", StringComparison.Ordinal))
            {
                report.Warnings.Add(
                    "Player Settings ainda usa 'DefaultCompany'; defina a identidade " +
                    "antes de gerar builds móveis de distribuição.");
            }
        }

        private static void ValidateFiles(
            IReadOnlyList<string> paths,
            string description,
            ValidationReport report)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                if (!File.Exists(ToAbsolutePath(paths[i])))
                {
                    report.Errors.Add($"Ausência de {description}: {paths[i]}");
                }
            }
        }

        private static void RequireText(
            string source,
            string requiredText,
            string error,
            ValidationReport report)
        {
            if (source.IndexOf(requiredText, StringComparison.Ordinal) < 0)
                report.Errors.Add(error);
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Não foi possível localizar a raiz do projeto.");

            string normalized = projectRelativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(projectRoot, normalized);
        }

        private static void Log(ValidationReport report)
        {
            for (int i = 0; i < report.Errors.Count; i++)
                Debug.LogError($"[ProjectValidation] {report.Errors[i]}");

            for (int i = 0; i < report.Warnings.Count; i++)
                Debug.LogWarning($"[ProjectValidation] {report.Warnings[i]}");

            if (report.Errors.Count == 0)
            {
                Debug.Log(
                    $"[ProjectValidation] Projeto válido. " +
                    $"{report.Warnings.Count} aviso(s).");
            }
            else
            {
                Debug.LogError(
                    $"[ProjectValidation] Falha: {report.Errors.Count} erro(s), " +
                    $"{report.Warnings.Count} aviso(s).");
            }
        }

        private readonly struct RequiredAssembly
        {
            public RequiredAssembly(string path, string name)
            {
                Path = path;
                Name = name;
            }

            public string Path { get; }
            public string Name { get; }
        }

        private sealed class ValidationReport
        {
            public List<string> Errors { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
        }
    }
}
