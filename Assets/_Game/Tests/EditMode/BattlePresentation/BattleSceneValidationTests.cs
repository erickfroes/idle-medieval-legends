using System;
using System.Collections.Generic;
using System.Linq;
using IdleMedievalLegends.Editor.Battle;
using IdleMedievalLegends.Presentation.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class BattleSceneValidationTests
    {
        [Test]
        public void BattleScene_GeneratedComposition_IsValid()
        {
            Assert.That(BattleSceneTools.GetValidationErrors(), Is.Empty);
        }

        [Test]
        public void BuildSettings_BattleScene_IsEnabledExactlyOnceAfterBootstrap()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            Assert.That(scenes, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(scenes[0].path, Is.EqualTo(BattleSceneTools.BootstrapScenePath));
            Assert.That(scenes[1].path, Is.EqualTo(BattleSceneTools.BattleScenePath));
            Assert.That(scenes[0].enabled, Is.True);
            Assert.That(scenes[1].enabled, Is.True);
            Assert.That(scenes.Count(scene =>
                scene.path == BattleSceneTools.BattleScenePath), Is.EqualTo(1));
        }

        [Test]
        public void BattleScene_DisabledController_IsRejected()
        {
            WithLoadedBattleScene(scene =>
            {
                BattleSceneController controller =
                    GetSceneComponent<BattleSceneController>(scene);
                controller.enabled = false;

                IReadOnlyList<string> errors =
                    BattleSceneTools.GetSceneValidationErrors(scene);

                Assert.That(errors.Any(error =>
                    error.Contains("BattleSceneController") &&
                    error.Contains("desabilitado")), Is.True);
            });
        }

        [Test]
        public void BattleScene_InactivePresentationRoot_IsRejected()
        {
            WithLoadedBattleScene(scene =>
            {
                BattleSceneController controller =
                    GetSceneComponent<BattleSceneController>(scene);
                controller.gameObject.SetActive(false);

                IReadOnlyList<string> errors =
                    BattleSceneTools.GetSceneValidationErrors(scene);

                Assert.That(errors.Any(error =>
                    error.Contains("BattleSceneController") &&
                    error.Contains("inativo")), Is.True);
            });
        }

        [Test]
        public void BattleScene_MismatchedTeamReferences_AreRejected()
        {
            WithLoadedBattleScene(scene =>
            {
                BattleSceneController controller =
                    GetSceneComponent<BattleSceneController>(scene);
                BattleEventPlayer eventPlayer = GetSceneComponent<BattleEventPlayer>(scene);
                eventPlayer.Configure(
                    eventPlayer.PresentationConfig,
                    controller.EnemyTeamView,
                    controller.PlayerTeamView,
                    controller.HudView);

                IReadOnlyList<string> errors =
                    BattleSceneTools.GetSceneValidationErrors(scene);

                Assert.That(errors.Any(error =>
                    error.Contains("compartilhar as mesmas equipes")), Is.True);
            });
        }

        private static void WithLoadedBattleScene(Action<Scene> assertion)
        {
            Scene loaded = SceneManager.GetSceneByPath(BattleSceneTools.BattleScenePath);
            bool close = !loaded.IsValid() || !loaded.isLoaded;
            Scene scene = close
                ? EditorSceneManager.OpenScene(
                    BattleSceneTools.BattleScenePath,
                    OpenSceneMode.Additive)
                : loaded;

            BattleSceneController controller =
                GetSceneComponent<BattleSceneController>(scene);
            BattleEventPlayer eventPlayer = GetSceneComponent<BattleEventPlayer>(scene);
            bool controllerEnabled = controller.enabled;
            bool presentationRootActive = controller.gameObject.activeSelf;
            BattlePresentationConfig config = eventPlayer.PresentationConfig;
            BattleTeamView playerTeam = eventPlayer.PlayerTeamView;
            BattleTeamView enemyTeam = eventPlayer.EnemyTeamView;
            BattleHudView hud = eventPlayer.HudView;

            try
            {
                assertion(scene);
            }
            finally
            {
                controller.gameObject.SetActive(presentationRootActive);
                controller.enabled = controllerEnabled;
                eventPlayer.Configure(config, playerTeam, enemyTeam, hud);
                if (close)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T GetSceneComponent<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            throw new InvalidOperationException(
                $"Componente {typeof(T).Name} ausente na cena Battle.");
        }
    }
}
