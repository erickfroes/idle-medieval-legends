using System;
using System.Collections.Generic;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Presentation.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleMedievalLegends.Editor.Inventory
{
    public static class InventorySceneTools
    {
        public const string InventoryScenePath = "Assets/_Game/Scenes/Inventory.unity";
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";

        [MenuItem("Tools/Idle Medieval Legends/Scenes/Create or Repair Inventory Scene")]
        public static void GenerateFromMenu()
        {
            Generate();
            Debug.Log("Inventory scene gerada e validada.");
        }

        public static void GenerateFromCommandLine()
        {
            Generate();
        }

        public static void ValidateFromCommandLine()
        {
            List<string> errors = Validate();
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));
            Debug.Log("Inventory scene válida.");
        }

        private static void Generate()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            var root = new GameObject("InventoryPresentation");
            root.AddComponent<InventoryPanelController>();
            EditorSceneManager.SaveScene(scene, InventoryScenePath);

            Scene bootstrap = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            GameManager manager = FindInScene<GameManager>(bootstrap);
            if (manager == null)
                throw new InvalidOperationException("Bootstrap não contém GameManager.");
            manager.ConfigureDevelopmentInventorySeed(true);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.SaveScene(bootstrap);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (!string.Equals(scenes[i].path, InventoryScenePath,
                    StringComparison.Ordinal)) continue;
                scenes[i] = new EditorBuildSettingsScene(InventoryScenePath, true);
                found = true;
            }
            if (!found) scenes.Add(new EditorBuildSettingsScene(InventoryScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();

            List<string> errors = Validate();
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));
        }

        public static List<string> Validate()
        {
            var errors = new List<string>();
            if (!System.IO.File.Exists(InventoryScenePath))
            {
                errors.Add("Inventory.unity não existe.");
                return errors;
            }
            Scene scene = EditorSceneManager.OpenScene(InventoryScenePath, OpenSceneMode.Single);
            InventoryPanelController controller = FindInScene<InventoryPanelController>(scene);
            if (controller == null) errors.Add("InventoryPanelController ausente.");
            bool inBuild = false;
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled && buildScene.path == InventoryScenePath)
                    inBuild = true;
            }
            if (!inBuild) errors.Add("Inventory não está habilitada nos Build Settings.");
            return errors;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T found = roots[i].GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
