using System.Collections.Generic;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Editor.Bootstrap;
using IdleMedievalLegends.Editor.ContentCatalog;
using IdleMedievalLegends.Infrastructure.Save;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class BootstrapSceneValidationTests
    {
        private Scene scene;
        private GameObject app;
        private GameManager gameManager;
        private CombatBalanceConfigAsset combatConfig;
        private CraftingBalanceConfigAsset craftingConfig;
        private ContentCatalogAsset contentCatalog;

        [SetUp]
        public void SetUp()
        {
            scene = EditorSceneManager.NewPreviewScene();
            app = new GameObject("App");
            SceneManager.MoveGameObjectToScene(app, scene);

            var repository = app.AddComponent<LocalJsonPlayerStateRepository>();
            gameManager = app.AddComponent<GameManager>();
            BootstrapDiagnostics diagnostics = app.AddComponent<BootstrapDiagnostics>();
            combatConfig = ScriptableObject.CreateInstance<CombatBalanceConfigAsset>();
            craftingConfig = ScriptableObject.CreateInstance<CraftingBalanceConfigAsset>();
            contentCatalog = ScriptableObject.CreateInstance<ContentCatalogAsset>();
            contentCatalog.ReplaceDefinitionsForEditor(ContentCatalogDemoFactory.Create());

            gameManager.ConfigureBootstrapDependencies(
                repository,
                combatConfig,
                craftingConfig,
                contentCatalog);
            diagnostics.Configure(gameManager);
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.ClosePreviewScene(scene);
            if (combatConfig != null)
                UnityEngine.Object.DestroyImmediate(combatConfig);
            if (craftingConfig != null)
                UnityEngine.Object.DestroyImmediate(craftingConfig);
            if (contentCatalog != null)
                UnityEngine.Object.DestroyImmediate(contentCatalog);
        }

        [Test]
        public void SceneValidation_GameManagerOutsideApp_IsRejected()
        {
            var otherRoot = new GameObject("OtherRoot");
            SceneManager.MoveGameObjectToScene(otherRoot, scene);
            otherRoot.AddComponent<GameManager>();

            IReadOnlyList<string> errors = BootstrapSceneTools.GetSceneValidationErrors(
                scene,
                combatConfig,
                craftingConfig,
                contentCatalog);

            Assert.That(
                errors,
                Has.Some.Contains(
                    "A cena Bootstrap deve possuir exatamente um GameManager; encontrados: 2."));
            Assert.That(
                errors,
                Has.Some.Contains("GameManager deve existir somente no objeto raiz App."));
        }

        [Test]
        public void SceneValidation_InactiveAppAndDisabledManager_AreRejected()
        {
            app.SetActive(false);
            gameManager.enabled = false;

            IReadOnlyList<string> errors = BootstrapSceneTools.GetSceneValidationErrors(
                scene,
                combatConfig,
                craftingConfig,
                contentCatalog);

            Assert.That(errors, Has.Some.Contains("O objeto raiz App deve estar ativo."));
            Assert.That(errors, Has.Some.Contains("GameManager deve estar habilitado."));
        }
    }
}
