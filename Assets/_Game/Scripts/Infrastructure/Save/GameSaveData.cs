using System;
using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Inventory;
using UnityEngine;

namespace IdleMedievalLegends.Infrastructure.Save
{
    /// <summary>
    /// Cache local. Não é a fonte de verdade para gemas, drops, profissões,
    /// crafting, mercado ou poder.
    /// </summary>
    [Serializable]
    public sealed class GameSaveData
    {
        public const int CurrentSchemaVersion = 2;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string playerId = string.Empty;
        [SerializeField] private long cachedAtUnixMilliseconds;
        [SerializeField] private InventorySnapshotData inventory =
            InventorySnapshotData.CreateEmpty();
        [SerializeField] private ProfessionSnapshotData professions =
            ProfessionSnapshotData.CreateEmpty();

        public int SchemaVersion => schemaVersion;
        public string PlayerId => playerId;
        public long CachedAtUnixMilliseconds => cachedAtUnixMilliseconds;
        public InventorySnapshotData Inventory => inventory;
        public ProfessionSnapshotData Professions => professions;

        public GameSaveData()
        {
        }

        public GameSaveData(
            string playerId,
            InventorySnapshotData inventory,
            ProfessionSnapshotData professions)
        {
            schemaVersion = CurrentSchemaVersion;
            this.playerId = playerId ?? string.Empty;
            this.inventory = inventory ?? InventorySnapshotData.CreateEmpty(this.playerId);
            this.professions = professions ?? ProfessionSnapshotData.CreateEmpty(this.playerId);
            cachedAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>Compatibilidade de código com a versão v1.</summary>
        public GameSaveData(string playerId, InventorySnapshotData inventory)
            : this(
                playerId,
                inventory,
                ProfessionSnapshotData.CreateEmpty(playerId))
        {
        }

        public static GameSaveData CreateEmpty(string playerId = "")
        {
            return new GameSaveData(
                playerId ?? string.Empty,
                InventorySnapshotData.CreateEmpty(playerId),
                ProfessionSnapshotData.CreateEmpty(playerId));
        }
    }

    public static class GameSaveMigration
    {
        public static GameSaveData UpgradeToCurrent(GameSaveData source)
        {
            if (source == null)
                return GameSaveData.CreateEmpty();

            string playerId = source.PlayerId ?? string.Empty;
            InventorySnapshotData inventory =
                source.Inventory ?? InventorySnapshotData.CreateEmpty(playerId);
            ProfessionSnapshotData professions =
                source.Professions ?? ProfessionSnapshotData.CreateEmpty(playerId);

            inventory.NormalizeAfterLoad(playerId);
            professions.NormalizeAfterLoad(playerId);

            // O schema v1 não possuía profissões nem raridade/Tier de item.
            // Campos novos recebem seus defaults locais, mas o próximo bootstrap
            // online deve substituir tudo por um snapshot autoritativo v2.
            if (source.SchemaVersion < GameSaveData.CurrentSchemaVersion)
            {
                return new GameSaveData(playerId, inventory, professions);
            }

            return source;
        }
    }
}
