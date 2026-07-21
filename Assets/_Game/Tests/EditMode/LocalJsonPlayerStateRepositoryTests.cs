using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IdleMedievalLegends.Infrastructure.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class LocalJsonPlayerStateRepositoryTests
    {
        [Test]
        public async Task LoadAsync_MalformedDisposableCache_ReturnsEmptyState()
        {
            var gameObject = new GameObject("LocalJsonPlayerStateRepositoryTests");
            LocalJsonPlayerStateRepository repository =
                gameObject.AddComponent<LocalJsonPlayerStateRepository>();
            string fileName = $"player_cache_test_{Guid.NewGuid():N}.json";
            string path = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);

            try
            {
                FieldInfo fileNameField = typeof(LocalJsonPlayerStateRepository).GetField(
                    "fileName",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(fileNameField, Is.Not.Null);
                fileNameField.SetValue(repository, fileName);

                File.WriteAllText(path, "{\"schemaVersion\":", Encoding.UTF8);
                LogAssert.Expect(
                    LogType.Warning,
                    new Regex("^Cache local inválido .* e será descartado:"));

                GameSaveData result = await repository.LoadAsync(CancellationToken.None);

                Assert.That(result, Is.Not.Null);
                Assert.That(result.SchemaVersion, Is.EqualTo(GameSaveData.CurrentSchemaVersion));
                Assert.That(result.PlayerId, Is.EqualTo("local-player"));
                Assert.That(result.Inventory.Items, Is.Empty);
                Assert.That(result.Professions.Professions.Count, Is.EqualTo(5));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
