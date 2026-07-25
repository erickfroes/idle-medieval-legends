using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Editor.MobileUI;
using IdleMedievalLegends.Presentation.MobileUI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class MobileUiNavigationTests
    {
        [Test]
        public void NavigationHistory_PushRoutes_PreservesOrder()
        {
            var history = new NavigationHistory(NavigationRoute.Home);

            history.Push(NavigationRoute.Battle);
            history.Push(NavigationRoute.Heroes);

            Assert.That(history.Count, Is.EqualTo(3));
            Assert.That(history.Current, Is.EqualTo(NavigationRoute.Heroes));
            Assert.That(history.Entries[0], Is.EqualTo(NavigationRoute.Home));
        }

        [Test]
        public void NavigationHistory_Back_ReturnsPreviousRoute()
        {
            var history = new NavigationHistory(NavigationRoute.Home);
            history.Push(NavigationRoute.Crafting);
            history.Push(NavigationRoute.More);

            bool moved = history.TryBack(out NavigationRoute route);

            Assert.That(moved, Is.True);
            Assert.That(route, Is.EqualTo(NavigationRoute.Crafting));
            Assert.That(history.Current, Is.EqualTo(NavigationRoute.Crafting));
        }

        [Test]
        public void NavigationHistory_BackAtRoot_DoesNotRemoveRoot()
        {
            var history = new NavigationHistory(NavigationRoute.Home);

            bool moved = history.TryBack(out NavigationRoute route);

            Assert.That(moved, Is.False);
            Assert.That(route, Is.EqualTo(NavigationRoute.Home));
            Assert.That(history.Count, Is.EqualTo(1));
        }

        [Test]
        public void NavigationRouteRegistry_InvalidRoute_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => NavigationRouteRegistry.Get((NavigationRoute)999));
        }

        [Test]
        public void ModalService_SecondModalWhileOpen_IsRejected()
        {
            var service = new ModalService();

            bool first = service.TryOpen(new ModalRequest("A", "Primeiro"));
            bool second = service.TryOpen(new ModalRequest("B", "Segundo"));

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(service.Current.Title, Is.EqualTo("A"));
        }

        [Test]
        public void AsyncActionGate_DuplicateBegin_IsRejectedUntilEnd()
        {
            var gate = new AsyncActionGate();

            Assert.That(gate.TryBegin(), Is.True);
            Assert.That(gate.TryBegin(), Is.False);
            gate.End();
            Assert.That(gate.TryBegin(), Is.True);
        }

        [Test]
        public void ToastService_DequeueMultipleMessages_PreservesOrderWithoutReentrantChange()
        {
            var service = new ToastService();
            int changedCount = 0;
            service.Changed += () => changedCount++;

            service.ShowSuccess("primeiro");
            service.ShowWarning("segundo");
            service.ShowError("terceiro");

            Assert.That(changedCount, Is.EqualTo(3));
            Assert.That(service.TryDequeue(out ToastMessage first), Is.True);
            Assert.That(service.TryDequeue(out ToastMessage second), Is.True);
            Assert.That(service.TryDequeue(out ToastMessage third), Is.True);
            Assert.That(first.Message, Is.EqualTo("primeiro"));
            Assert.That(second.Message, Is.EqualTo("segundo"));
            Assert.That(third.Message, Is.EqualTo("terceiro"));
            Assert.That(changedCount, Is.EqualTo(3));
            Assert.That(service.PendingCount, Is.Zero);
        }

        [Test]
        public void MobileUiValidator_SceneScan_RestoresSetupAndRejectsDirtyScene()
        {
            const string temporaryScenePath =
                "Assets/_Game/Tests/__MobileUiValidatorSetupTest.unity";
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene testScene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
                Assert.That(
                    EditorSceneManager.SaveScene(testScene, temporaryScenePath),
                    Is.True);
                EditorSceneManager.SetActiveScene(testScene);

                IReadOnlyList<string> cleanErrors =
                    MobileUiShellTools.ValidateProject(out _);

                Assert.That(cleanErrors, Is.Empty);
                Assert.That(
                    SceneManager.GetActiveScene().path,
                    Is.EqualTo(temporaryScenePath));
                Assert.That(SceneManager.sceneCount, Is.EqualTo(1));

                var unsavedObject = new GameObject("UnsavedValidationWork");
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Assert.That(SceneManager.GetActiveScene().isDirty, Is.True);

                IReadOnlyList<string> dirtyErrors =
                    MobileUiShellTools.ValidateProject(out _);

                Assert.That(
                    string.Join("\n", dirtyErrors),
                    Does.Contain("alterações não salvas"));
                Assert.That(unsavedObject, Is.Not.Null);
                Assert.That(
                    SceneManager.GetActiveScene().path,
                    Is.EqualTo(temporaryScenePath));
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                AssetDatabase.DeleteAsset(temporaryScenePath);
            }
        }

        [TestCase(999L, "999")]
        [TestCase(1_000L, "1K")]
        [TestCase(1_250L, "1.3K")]
        [TestCase(1_500_000L, "1.5M")]
        [TestCase(-2_000L, "-2K")]
        public void CurrencyFormatter_Amount_UsesCompactInvariantFormat(
            long amount,
            string expected)
        {
            Assert.That(CurrencyFormatter.Format(amount), Is.EqualTo(expected));
        }

        [Test]
        public void UiThemeConfig_DefaultTheme_IsValidAndDefinesRarities()
        {
            UiThemeConfig theme = ScriptableObject.CreateInstance<UiThemeConfig>();
            try
            {
                Assert.DoesNotThrow(theme.Validate);
                Assert.That(theme.MinimumTouchSize, Is.GreaterThanOrEqualTo(40f));
                Assert.That(
                    theme.GetRarityColor(GameRarity.Mythic),
                    Is.Not.EqualTo(theme.GetRarityColor(GameRarity.Common)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(theme);
            }
        }

        [Test]
        public void UiPreferenceService_SaveAndLoad_RoundTripsPresentationOnly()
        {
            var store = new MemoryPreferenceStore();
            var service = new UiPreferenceService(store);
            var settings = new UiAccessibilitySettings
            {
                MusicEnabled = false,
                EffectsEnabled = false,
                VibrationEnabled = false,
                ReduceMotion = true,
                HighContrast = true,
                TextScale = 1.4f,
                BattleSpeed = 3,
                Language = "pt-BR"
            };

            service.Save(settings);
            UiAccessibilitySettings loaded = service.Load();

            Assert.That(loaded.MusicEnabled, Is.False);
            Assert.That(loaded.EffectsEnabled, Is.False);
            Assert.That(loaded.VibrationEnabled, Is.False);
            Assert.That(loaded.ReduceMotion, Is.True);
            Assert.That(loaded.HighContrast, Is.True);
            Assert.That(loaded.TextScale, Is.EqualTo(1.4f).Within(0.001f));
            Assert.That(loaded.BattleSpeed, Is.EqualTo(3));
            Assert.That(loaded.Language, Is.EqualTo("pt-BR"));
            Assert.That(store.Keys, Does.Not.Contain("gold"));
            Assert.That(store.Keys, Does.Not.Contain("inventory"));
        }

        private sealed class MemoryPreferenceStore : IUiPreferenceStore
        {
            private readonly Dictionary<string, object> values =
                new Dictionary<string, object>();

            public IEnumerable<string> Keys => values.Keys;

            public int GetInt(string key, int defaultValue) =>
                values.TryGetValue(key, out object value) ? (int)value : defaultValue;

            public float GetFloat(string key, float defaultValue) =>
                values.TryGetValue(key, out object value) ? (float)value : defaultValue;

            public string GetString(string key, string defaultValue) =>
                values.TryGetValue(key, out object value) ? (string)value : defaultValue;

            public void SetInt(string key, int value) => values[key] = value;
            public void SetFloat(string key, float value) => values[key] = value;
            public void SetString(string key, string value) => values[key] = value;
            public void Save()
            {
            }
        }
    }
}
