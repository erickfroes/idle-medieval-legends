using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace IdleMedievalLegends.Infrastructure.Save
{
    /// <summary>
    /// Implementação para protótipo/cache. Nunca use este arquivo como
    /// autoridade de inventário, gemas, gacha ou transações de mercado.
    /// </summary>
    public sealed class LocalJsonPlayerStateRepository : PlayerStateRepositoryBehaviour
    {
        [SerializeField] private string fileName = "player_cache.json";
        [SerializeField] private bool prettyPrintInDevelopment = true;

        public override async Task<GameSaveData> LoadAsync(
            CancellationToken cancellationToken)
        {
            string path = GetPath();
            if (!File.Exists(path))
            {
                return GameSaveData.CreateEmpty();
            }

            string json = await Task.Run(
                () => File.ReadAllText(path, Encoding.UTF8),
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(json))
            {
                return GameSaveData.CreateEmpty();
            }

            try
            {
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                return GameSaveMigration.UpgradeToCurrent(saveData);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning(
                    $"Cache local inválido em '{path}' e será descartado: " +
                    exception.Message,
                    this);
                return GameSaveData.CreateEmpty();
            }
        }

        public override async Task SaveAsync(
            GameSaveData saveData,
            CancellationToken cancellationToken)
        {
            if (saveData == null)
                throw new ArgumentNullException(nameof(saveData));

            // JsonUtility pode rodar fora da main thread, mas o objeto não deve
            // ser alterado durante a serialização. Serializamos antes do I/O.
            bool pretty = Debug.isDebugBuild && prettyPrintInDevelopment;
            string json = JsonUtility.ToJson(saveData, pretty);

            string path = GetPath();
            string directory = Path.GetDirectoryName(path);
            string temporaryPath = path + ".tmp";
            string backupPath = path + ".bak";

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(temporaryPath, json, Encoding.UTF8);

                if (!File.Exists(path))
                {
                    File.Move(temporaryPath, path);
                    return;
                }

                try
                {
                    File.Replace(temporaryPath, path, backupPath);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(temporaryPath, path, true);
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                    File.Copy(temporaryPath, path, true);
                    File.Delete(temporaryPath);
                }
            }, cancellationToken);
        }

        private string GetPath()
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new InvalidOperationException("fileName não foi configurado.");
            }

            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}
