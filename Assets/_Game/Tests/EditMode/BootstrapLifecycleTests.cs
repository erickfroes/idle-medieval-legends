using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Editor.ContentCatalog;
using IdleMedievalLegends.Infrastructure.Save;
using NUnit.Framework;
using UnityEngine;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class BootstrapLifecycleTests
    {
        [Test]
        public void Dependencies_AllConfigured_AreAccepted()
        {
            var gameObject = new GameObject("BootstrapDependenciesTests");
            var repository = gameObject.AddComponent<LocalJsonPlayerStateRepository>();
            var combatConfig = ScriptableObject.CreateInstance<CombatBalanceConfigAsset>();
            var craftingConfig = ScriptableObject.CreateInstance<CraftingBalanceConfigAsset>();
            var contentCatalog = ScriptableObject.CreateInstance<ContentCatalogAsset>();
            contentCatalog.ReplaceDefinitionsForEditor(ContentCatalogDemoFactory.Create());

            try
            {
                Assert.DoesNotThrow(
                    () => GameBootstrapDependencies.Validate(
                        repository,
                        combatConfig,
                        craftingConfig,
                        contentCatalog));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(combatConfig);
                UnityEngine.Object.DestroyImmediate(craftingConfig);
                UnityEngine.Object.DestroyImmediate(contentCatalog);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Dependencies_MissingBalanceAsset_AreRejected()
        {
            var gameObject = new GameObject("BootstrapDependenciesTests");
            var repository = gameObject.AddComponent<LocalJsonPlayerStateRepository>();
            var craftingConfig = ScriptableObject.CreateInstance<CraftingBalanceConfigAsset>();
            var contentCatalog = ScriptableObject.CreateInstance<ContentCatalogAsset>();
            contentCatalog.ReplaceDefinitionsForEditor(ContentCatalogDemoFactory.Create());

            try
            {
                Assert.Throws<InvalidOperationException>(
                    () => GameBootstrapDependencies.Validate(
                        repository,
                        null,
                        craftingConfig,
                        contentCatalog));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(craftingConfig);
                UnityEngine.Object.DestroyImmediate(contentCatalog);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public async Task InitializeAsync_ValidDependencies_ReachesReadyWithLocalIdentity()
        {
            Assert.That(GameManager.Instance, Is.Null);

            var gameObject = new GameObject("BootstrapLifecycleTests");
            var repository = gameObject.AddComponent<LocalJsonPlayerStateRepository>();
            var gameManager = gameObject.AddComponent<GameManager>();
            var combatConfig = ScriptableObject.CreateInstance<CombatBalanceConfigAsset>();
            var craftingConfig = ScriptableObject.CreateInstance<CraftingBalanceConfigAsset>();
            var contentCatalog = ScriptableObject.CreateInstance<ContentCatalogAsset>();
            contentCatalog.ReplaceDefinitionsForEditor(ContentCatalogDemoFactory.Create());
            string fileName = $"bootstrap_cache_test_{Guid.NewGuid():N}.json";
            string path = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);

            try
            {
                SetField(repository, "fileName", fileName);
                SetField(repository, "localPlayerId", "local-bootstrap-test");
                gameManager.ConfigureBootstrapDependencies(
                    repository,
                    combatConfig,
                    craftingConfig,
                    contentCatalog);

                await gameManager.InitializeAsync(CancellationToken.None);

                Assert.That(gameManager.State, Is.EqualTo(GameLifecycleState.Ready));
                Assert.That(gameManager.CurrentPlayerId, Is.EqualTo("local-bootstrap-test"));
                Assert.That(gameManager.Inventory.ServerRevision, Is.Zero);
                Assert.That(gameManager.Professions.ServerRevision, Is.Zero);
                Assert.That(gameManager.ContentCatalog, Is.Not.Null);
                Assert.That(gameManager.ContentCatalog.GetHero("hero_paladin_001"), Is.Not.Null);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
                UnityEngine.Object.DestroyImmediate(combatConfig);
                UnityEngine.Object.DestroyImmediate(craftingConfig);
                UnityEngine.Object.DestroyImmediate(contentCatalog);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Diagnostics_ReadyState_FormatsRequiredValues()
        {
            string message = BootstrapDiagnosticsFormatter.Format(
                GameLifecycleState.Ready,
                "local-player",
                12,
                34);

            Assert.That(message, Does.Contain("state=Ready"));
            Assert.That(message, Does.Contain("playerId=local-player"));
            Assert.That(message, Does.Contain("inventoryRevision=12"));
            Assert.That(message, Does.Contain("professionRevision=34"));
        }

        private static void SetField<TTarget, TValue>(
            TTarget target,
            string fieldName,
            TValue value)
        {
            FieldInfo field = typeof(TTarget).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Campo não encontrado: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
