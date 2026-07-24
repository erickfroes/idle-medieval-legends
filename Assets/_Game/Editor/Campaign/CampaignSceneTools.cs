using System;
using System.Collections.Generic;
using System.IO;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Presentation.Campaign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace IdleMedievalLegends.Editor.Campaign
{
    public static class CampaignSceneTools
    {
        public const string CampaignScenePath = "Assets/_Game/Scenes/Campaign.unity";
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";
        public const string CampaignConfigPath =
            "Assets/_Game/Data/Balance/CampaignConfig.asset";

        [MenuItem(
            "Tools/Idle Medieval Legends/Scenes/Create or Repair Campaign Scene",
            priority = 133)]
        public static void CreateOrRepairFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            CreateOrRepair();
            EditorSceneManager.OpenScene(CampaignScenePath, OpenSceneMode.Single);
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(CampaignScenePath);
        }

        public static void GenerateFromCommandLine()
        {
            CreateOrRepair();
        }

        public static void ValidateFromCommandLine()
        {
            IReadOnlyList<string> errors = GetValidationErrors();
            Log(errors);
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    $"Campanha inválida: {errors.Count} erro(s).");
        }

        public static IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            CampaignConfigAsset config =
                AssetDatabase.LoadAssetAtPath<CampaignConfigAsset>(CampaignConfigPath);
            if (config == null)
                errors.Add($"Config ausente: {CampaignConfigPath}.");
            else if (config.BuildDefinition().Stages.Count != 10)
                errors.Add("CampaignConfig deve gerar exatamente dez estágios demonstrativos.");
            ValidateScene(errors);
            ValidateBootstrap(errors, config);
            ValidateBuildSettings(errors);
            return errors.AsReadOnly();
        }

        private static void CreateOrRepair()
        {
            EnsureFolder("Assets/_Game/Scenes");
            EnsureFolder("Assets/_Game/Data/Balance");
            CampaignConfigAsset config =
                AssetDatabase.LoadAssetAtPath<CampaignConfigAsset>(CampaignConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CampaignConfigAsset>();
                AssetDatabase.CreateAsset(config, CampaignConfigPath);
            }
            config.EnsureValid();
            if (config.BuildDefinition().Stages.Count != 10)
                throw new InvalidOperationException("Configuração não gerou dez estágios.");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            if (!File.Exists(CampaignScenePath) || HasSceneErrors())
                CreateCanonicalScene();
            config = AssetDatabase.LoadAssetAtPath<CampaignConfigAsset>(
                CampaignConfigPath);
            if (config == null)
                throw new InvalidOperationException("CampaignConfig ficou indisponível.");
            ConfigureBootstrap(config);
            PutSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            IReadOnlyList<string> errors = GetValidationErrors();
            Log(errors);
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    $"Cena Campaign gerada com {errors.Count} erro(s).");
        }

        private static void CreateCanonicalScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var root = new GameObject("CampaignPresentation");
            root.AddComponent<CampaignPanelController>();
            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CampaignScenePath))
                throw new InvalidOperationException("Não foi possível salvar Campaign.unity.");
        }

        private static void ConfigureBootstrap(CampaignConfigAsset config)
        {
            Scene scene = EditorSceneManager.OpenScene(
                BootstrapScenePath,
                OpenSceneMode.Single);
            GameManager manager = FindInScene<GameManager>(scene);
            if (manager == null)
                throw new InvalidOperationException("Bootstrap não contém GameManager.");
            manager.ConfigureCampaignConfig(config);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, BootstrapScenePath))
                throw new InvalidOperationException("Não foi possível salvar Bootstrap.");
        }

        private static void PutSceneInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            int found = -1;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (string.Equals(
                    scenes[i].path,
                    CampaignScenePath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (found >= 0)
                    {
                        scenes.RemoveAt(i--);
                        continue;
                    }
                    found = i;
                    scenes[i] = new EditorBuildSettingsScene(CampaignScenePath, true);
                }
            }
            if (found < 0)
                scenes.Add(new EditorBuildSettingsScene(CampaignScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static bool HasSceneErrors()
        {
            var errors = new List<string>();
            ValidateScene(errors);
            return errors.Count > 0;
        }

        private static void ValidateScene(List<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CampaignScenePath) == null)
            {
                errors.Add($"Cena ausente: {CampaignScenePath}.");
                return;
            }
            Scene loaded = SceneManager.GetSceneByPath(CampaignScenePath);
            bool close = !loaded.IsValid() || !loaded.isLoaded;
            Scene scene = close
                ? EditorSceneManager.OpenScene(CampaignScenePath, OpenSceneMode.Additive)
                : loaded;
            try
            {
                if (CountInScene<CampaignPanelController>(scene) != 1)
                    errors.Add("Campaign requer um CampaignPanelController.");
                if (CountInScene<EventSystem>(scene) != 1)
                    errors.Add("Campaign requer um EventSystem.");
                if (CountInScene<InputSystemUIInputModule>(scene) != 1)
                    errors.Add("Campaign requer um InputSystemUIInputModule.");
            }
            finally
            {
                if (close)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ValidateBootstrap(
            List<string> errors,
            CampaignConfigAsset expected)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath) == null)
            {
                errors.Add("Bootstrap ausente.");
                return;
            }
            Scene loaded = SceneManager.GetSceneByPath(BootstrapScenePath);
            bool close = !loaded.IsValid() || !loaded.isLoaded;
            Scene scene = close
                ? EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive)
                : loaded;
            try
            {
                GameManager manager = FindInScene<GameManager>(scene);
                if (manager == null || manager.CampaignConfig != expected)
                    errors.Add("GameManager não referencia CampaignConfig canônico.");
            }
            finally
            {
                if (close)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ValidateBuildSettings(List<string> errors)
        {
            int count = 0;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (string.Equals(
                    scenes[i].path,
                    CampaignScenePath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    if (!scenes[i].enabled)
                        errors.Add("Campaign está desabilitada nos Build Settings.");
                }
            }
            if (count != 1)
                errors.Add("Campaign deve aparecer uma vez nos Build Settings.");
        }

        private static int CountInScene<T>(Scene scene) where T : Component
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                count += roots[i].GetComponentsInChildren<T>(true).Length;
            return count;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T result = roots[i].GetComponentInChildren<T>(true);
                if (result != null)
                    return result;
            }
            return null;
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

        private static void Log(IReadOnlyList<string> errors)
        {
            for (int i = 0; i < errors.Count; i++)
                Debug.LogError($"[CampaignSceneValidation] {errors[i]}");
            if (errors.Count == 0)
                Debug.Log("[CampaignSceneValidation] Campanha válida com dez estágios.");
        }
    }
}
