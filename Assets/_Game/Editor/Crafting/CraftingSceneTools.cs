using System;
using System.Collections.Generic;
using IdleMedievalLegends.Presentation.Crafting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleMedievalLegends.Editor.Crafting
{
    public static class CraftingSceneTools
    {
        public const string ScenePath = "Assets/_Game/Scenes/Crafting.unity";

        [MenuItem("Tools/Idle Medieval Legends/Scenes/Create or Repair Crafting Scene")]
        public static void GenerateFromMenu()
        {
            Generate();
            Debug.Log("Crafting scene gerada e validada.");
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
            Debug.Log("Crafting scene válida.");
        }

        public static List<string> Validate()
        {
            var errors = new List<string>();
            if (!System.IO.File.Exists(ScenePath))
            {
                errors.Add("Crafting.unity não existe.");
                return errors;
            }
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (FindInScene<CraftingPanelController>(scene) == null)
                errors.Add("CraftingPanelController ausente.");
            bool enabled = false;
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
                if (buildScene.enabled && buildScene.path == ScenePath) enabled = true;
            if (!enabled) errors.Add("Crafting não está habilitada nos Build Settings.");
            return errors;
        }

        private static void Generate()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("CraftingPresentation");
            root.AddComponent<CraftingPanelController>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path != ScenePath) continue;
                scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
                found = true;
            }
            if (!found) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            List<string> errors = Validate();
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
