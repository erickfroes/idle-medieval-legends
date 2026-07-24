using System;
using System.Collections.Generic;
using System.IO;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Presentation.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace IdleMedievalLegends.Editor.Dungeon
{
    public static class DungeonSceneTools
    {
        public const string DungeonScenePath = "Assets/_Game/Scenes/Dungeon.unity";
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";
        public const string DungeonConfigPath =
            "Assets/_Game/Data/Balance/DungeonConfig.asset";

        [MenuItem(
            "Tools/Idle Medieval Legends/Scenes/Create or Repair Dungeon Scene",
            priority = 134)]
        public static void CreateOrRepairFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            CreateOrRepair();
            EditorSceneManager.OpenScene(DungeonScenePath, OpenSceneMode.Single);
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
                    $"Masmorras inválidas: {errors.Count} erro(s).");
        }

        public static IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();
            DungeonConfigAsset config =
                AssetDatabase.LoadAssetAtPath<DungeonConfigAsset>(DungeonConfigPath);
            if (config == null)
            {
                errors.Add($"Config ausente: {DungeonConfigPath}.");
            }
            else
            {
                try
                {
                    if (config.BuildCatalog().Dungeons.Count != 5)
                        errors.Add("DungeonConfig deve gerar cinco masmorras.");
                }
                catch (Exception exception)
                {
                    errors.Add($"DungeonConfig inválida: {exception.Message}");
                }
            }
            ValidateScene(errors);
            ValidateBootstrap(errors, config);
            ValidateBuildSettings(errors);
            return errors.AsReadOnly();
        }

        private static void CreateOrRepair()
        {
            EnsureFolder("Assets/_Game/Scenes");
            EnsureFolder("Assets/_Game/Data/Balance");
            DungeonConfigAsset config =
                AssetDatabase.LoadAssetAtPath<DungeonConfigAsset>(DungeonConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<DungeonConfigAsset>();
                AssetDatabase.CreateAsset(config, DungeonConfigPath);
            }
            config.EnsureValid();
            if (config.BuildCatalog().Dungeons.Count != 5)
                throw new InvalidOperationException("Config não gerou cinco masmorras.");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            if (!File.Exists(DungeonScenePath) || HasSceneErrors())
                CreateCanonicalScene();
            ConfigureBootstrap(config);
            PutSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            IReadOnlyList<string> errors = GetValidationErrors();
            Log(errors);
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    $"Cena Dungeon gerada com {errors.Count} erro(s).");
        }

        private static void CreateCanonicalScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            new GameObject("DungeonPresentation")
                .AddComponent<DungeonPanelController>();
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, DungeonScenePath))
                throw new InvalidOperationException("Não foi possível salvar Dungeon.unity.");
        }

        private static void ConfigureBootstrap(DungeonConfigAsset config)
        {
            Scene scene = EditorSceneManager.OpenScene(
                BootstrapScenePath,
                OpenSceneMode.Single);
            GameManager manager = FindInScene<GameManager>(scene);
            if (manager == null)
                throw new InvalidOperationException("Bootstrap não contém GameManager.");
            manager.ConfigureDungeonConfig(config);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, BootstrapScenePath))
                throw new InvalidOperationException("Não foi possível salvar Bootstrap.");
        }

        private static void PutSceneInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int found = -1;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (!string.Equals(
                    scenes[i].path,
                    DungeonScenePath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (found >= 0)
                {
                    scenes.RemoveAt(i--);
                    continue;
                }
                found = i;
                scenes[i] = new EditorBuildSettingsScene(DungeonScenePath, true);
            }
            if (found < 0)
                scenes.Add(new EditorBuildSettingsScene(DungeonScenePath, true));
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
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DungeonScenePath) == null)
            {
                errors.Add($"Cena ausente: {DungeonScenePath}.");
                return;
            }
            Scene loaded = SceneManager.GetSceneByPath(DungeonScenePath);
            bool close = !loaded.IsValid() || !loaded.isLoaded;
            Scene scene = close
                ? EditorSceneManager.OpenScene(DungeonScenePath, OpenSceneMode.Additive)
                : loaded;
            try
            {
                if (CountInScene<DungeonPanelController>(scene) != 1)
                    errors.Add("Dungeon requer um DungeonPanelController.");
                if (CountInScene<EventSystem>(scene) != 1)
                    errors.Add("Dungeon requer um EventSystem.");
                if (CountInScene<InputSystemUIInputModule>(scene) != 1)
                    errors.Add("Dungeon requer um InputSystemUIInputModule.");
            }
            finally
            {
                if (close)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ValidateBootstrap(
            List<string> errors,
            DungeonConfigAsset expected)
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
                if (manager == null || manager.DungeonConfig != expected)
                    errors.Add("GameManager não referencia DungeonConfig canônico.");
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
                if (!string.Equals(
                    scenes[i].path,
                    DungeonScenePath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                count++;
                if (!scenes[i].enabled)
                    errors.Add("Dungeon está desabilitada nos Build Settings.");
            }
            if (count != 1)
                errors.Add("Dungeon deve aparecer uma vez nos Build Settings.");
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
                Debug.LogError($"[DungeonSceneValidation] {errors[i]}");
            if (errors.Count == 0)
                Debug.Log("[DungeonSceneValidation] Cinco masmorras válidas.");
        }
    }
}
