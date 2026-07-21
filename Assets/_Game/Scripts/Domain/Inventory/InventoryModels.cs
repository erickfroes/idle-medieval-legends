using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Common;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Inventory
{
    public enum InventoryItemKind
    {
        Material = 0,
        Equipment = 1,
        Skin = 2,
        Diagram = 3,
        Consumable = 4,
        CurrencyMirror = 5,
        RefinedMaterial = 6,
        CraftingTool = 7,
        Enchantment = 8
    }

    public enum InventoryItemState
    {
        Available = 0,
        Equipped = 1,
        EscrowedForMarket = 2,
        ReservedByServer = 3,
        Consumed = 4,
        Destroyed = 5
    }

    public enum ItemBinding
    {
        Unbound = 0,
        AccountBound = 1,
        HeroBound = 2
    }

    [Serializable]
    public sealed class RolledStatData
    {
        [SerializeField] private string statId = string.Empty;
        [SerializeField] private long flatValue;
        [SerializeField] private float percentValue;

        public string StatId => statId;
        public long FlatValue => flatValue;
        public float PercentValue => percentValue;

        public RolledStatData()
        {
        }

        public RolledStatData(string statId, long flatValue, float percentValue)
        {
            this.statId = statId ?? throw new ArgumentNullException(nameof(statId));
            this.flatValue = flatValue;
            this.percentValue = percentValue;
        }
    }

    /// <summary>
    /// Snapshot serializável de uma instância ou pilha de item. Tier, raridade,
    /// origem, estado e versão são dados autoritativos do servidor.
    /// </summary>
    [Serializable]
    public sealed class InventoryItemData
    {
        [SerializeField] private string instanceId = string.Empty;
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private string ownerPlayerId = string.Empty;
        [SerializeField] private InventoryItemKind kind;
        [SerializeField] private GameRarity rarity = GameRarity.Common;
        [SerializeField] private ItemTier tier = ItemTier.Tier1;
        [SerializeField] private long quantity = 1;
        [SerializeField] private bool stackable;
        [SerializeField] private InventoryItemState state = InventoryItemState.Available;
        [SerializeField] private ItemBinding binding = ItemBinding.Unbound;
        [SerializeField] private string equippedHeroInstanceId = string.Empty;
        [SerializeField] private string marketListingId = string.Empty;
        [SerializeField] private string reservationId = string.Empty;
        [SerializeField] private CraftingProfession sourceProfession = CraftingProfession.None;
        [SerializeField] private string recipeId = string.Empty;
        [SerializeField] private string craftedByPlayerId = string.Empty;
        [SerializeField] private string originTransactionId = string.Empty;
        [SerializeField] private string parentInstanceId = string.Empty;
        [SerializeField] private int qualityScoreBasisPoints;
        [SerializeField] private int enhancementLevel;
        [SerializeField] private long craftedAtUnixMilliseconds;
        [SerializeField] private long rollSeed; // Legado v1; não usar para novos itens.
        [SerializeField] private string rollSeedHash = string.Empty;
        [SerializeField] private long serverVersion;
        [SerializeField] private List<RolledStatData> rolledStats = new List<RolledStatData>();

        public string InstanceId => instanceId;
        public string DefinitionId => definitionId;
        public string OwnerPlayerId => ownerPlayerId;
        public InventoryItemKind Kind => kind;
        public GameRarity Rarity => rarity;
        public ItemTier Tier => tier;
        public long Quantity => quantity;
        public bool Stackable => stackable;
        public InventoryItemState State => state;
        public ItemBinding Binding => binding;
        public string EquippedHeroInstanceId => equippedHeroInstanceId;
        public string MarketListingId => marketListingId;
        public string ReservationId => reservationId;
        public CraftingProfession SourceProfession => sourceProfession;
        public string RecipeId => recipeId;
        public string CraftedByPlayerId => craftedByPlayerId;
        public string OriginTransactionId => originTransactionId;
        public string ParentInstanceId => parentInstanceId;
        public int QualityScoreBasisPoints => qualityScoreBasisPoints;
        public int EnhancementLevel => enhancementLevel;
        public long CraftedAtUnixMilliseconds => craftedAtUnixMilliseconds;
        [Obsolete("Use RollSeedHash; rollSeed existe apenas para migração v1.")]
        public long RollSeed => rollSeed;
        public string RollSeedHash => rollSeedHash;
        public long ServerVersion => serverVersion;
        public IReadOnlyList<RolledStatData> RolledStats
        {
            get
            {
                if (rolledStats == null)
                    rolledStats = new List<RolledStatData>();
                return rolledStats;
            }
        }

        public bool IsCrafted => !string.IsNullOrWhiteSpace(recipeId);

        public bool IsTradable =>
            binding == ItemBinding.Unbound &&
            state == InventoryItemState.Available &&
            kind != InventoryItemKind.CurrencyMirror &&
            quantity > 0;

        public InventoryItemData()
        {
        }

        /// <summary>
        /// Sobrecarga compatível com os protótipos v1. Itens antigos são
        /// migrados como Comum/Tier 1 até o servidor enviar um novo snapshot.
        /// </summary>
        public InventoryItemData(
            string instanceId,
            string definitionId,
            string ownerPlayerId,
            InventoryItemKind kind,
            long quantity,
            bool stackable,
            long rollSeed,
            long serverVersion,
            List<RolledStatData> rolledStats = null)
            : this(
                instanceId,
                definitionId,
                ownerPlayerId,
                kind,
                GameRarity.Common,
                ItemTier.Tier1,
                quantity,
                stackable,
                rollSeed,
                serverVersion,
                CraftingProfession.None,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                rolledStats)
        {
        }

        public InventoryItemData(
            string instanceId,
            string definitionId,
            string ownerPlayerId,
            InventoryItemKind kind,
            GameRarity rarity,
            ItemTier tier,
            long quantity,
            bool stackable,
            long rollSeed,
            long serverVersion,
            CraftingProfession sourceProfession,
            string recipeId,
            string craftedByPlayerId,
            string originTransactionId,
            int qualityScoreBasisPoints,
            int enhancementLevel,
            long craftedAtUnixMilliseconds,
            List<RolledStatData> rolledStats = null,
            string rollSeedHash = null)
        {
            this.instanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
            this.definitionId = definitionId ?? throw new ArgumentNullException(nameof(definitionId));
            this.ownerPlayerId = ownerPlayerId ?? throw new ArgumentNullException(nameof(ownerPlayerId));
            this.kind = kind;
            this.rarity = rarity;
            this.tier = tier;
            this.quantity = quantity;
            this.stackable = stackable;
            this.rollSeed = rollSeed;
            this.serverVersion = serverVersion;
            this.sourceProfession = sourceProfession;
            this.recipeId = recipeId ?? string.Empty;
            this.craftedByPlayerId = craftedByPlayerId ?? string.Empty;
            this.originTransactionId = originTransactionId ?? string.Empty;
            this.qualityScoreBasisPoints = qualityScoreBasisPoints;
            this.enhancementLevel = enhancementLevel;
            this.craftedAtUnixMilliseconds = craftedAtUnixMilliseconds;
            this.rollSeedHash = rollSeedHash ??
                (rollSeed == 0 ? string.Empty : $"legacy:{rollSeed}");
            this.rolledStats = rolledStats ?? new List<RolledStatData>();

            Validate();
        }

        internal void ApplyLegacyDefaults(int sourceSchemaVersion)
        {
            if (sourceSchemaVersion >= InventorySnapshotData.CurrentSchemaVersion)
                return;

            // O schema v1 não persistia Tier, raridade nem proveniência.
            if (!tier.IsValid()) tier = ItemTier.Tier1;
            if (!rarity.IsValid()) rarity = GameRarity.Common;
            if (qualityScoreBasisPoints < 0 || qualityScoreBasisPoints > 10000)
                qualityScoreBasisPoints = 0;
            if (enhancementLevel < 0) enhancementLevel = 0;
            if (rolledStats == null) rolledStats = new List<RolledStatData>();
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                throw new InvalidOperationException("Item sem instanceId.");
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new InvalidOperationException($"Item {instanceId} sem definitionId.");
            if (string.IsNullOrWhiteSpace(ownerPlayerId))
                throw new InvalidOperationException($"Item {instanceId} sem ownerPlayerId.");
            if (!Enum.IsDefined(typeof(InventoryItemKind), kind))
                throw new InvalidOperationException($"Item {instanceId} com kind inválido.");
            if (!Enum.IsDefined(typeof(InventoryItemState), state))
                throw new InvalidOperationException($"Item {instanceId} com state inválido.");
            if (!Enum.IsDefined(typeof(ItemBinding), binding))
                throw new InvalidOperationException($"Item {instanceId} com binding inválido.");
            if (!rarity.IsValid())
                throw new InvalidOperationException($"Item {instanceId} com raridade inválida.");
            if (!tier.IsValid())
                throw new InvalidOperationException($"Item {instanceId} com Tier inválido.");
            if (quantity < 0)
                throw new InvalidOperationException($"Item {instanceId} com quantidade negativa.");
            if (quantity == 0 &&
                state != InventoryItemState.Consumed &&
                state != InventoryItemState.Destroyed)
            {
                throw new InvalidOperationException(
                    $"Item ativo {instanceId} não pode ter quantidade zero.");
            }
            if (!stackable && quantity > 1)
                throw new InvalidOperationException($"Item único {instanceId} não pode ter quantidade maior que 1.");
            if (qualityScoreBasisPoints < 0 || qualityScoreBasisPoints > 10000)
                throw new InvalidOperationException($"Item {instanceId} com qualityScore fora de 0..10000.");
            if (enhancementLevel < 0)
                throw new InvalidOperationException($"Item {instanceId} com enhancementLevel negativo.");
            if (state == InventoryItemState.Equipped && string.IsNullOrWhiteSpace(equippedHeroInstanceId))
                throw new InvalidOperationException($"Item equipado {instanceId} sem herói associado.");
            if (state == InventoryItemState.EscrowedForMarket && string.IsNullOrWhiteSpace(marketListingId))
                throw new InvalidOperationException($"Item em escrow {instanceId} sem listingId.");
            if (state == InventoryItemState.ReservedByServer && string.IsNullOrWhiteSpace(reservationId))
                throw new InvalidOperationException($"Item reservado {instanceId} sem reservationId.");
            if (sourceProfession != CraftingProfession.None && !sourceProfession.IsCraftingProfession())
                throw new InvalidOperationException($"Item {instanceId} com profissão de origem inválida.");
            if (!string.IsNullOrWhiteSpace(craftedByPlayerId) && string.IsNullOrWhiteSpace(recipeId))
                throw new InvalidOperationException($"Item craftado {instanceId} sem recipeId.");
            if (!string.IsNullOrWhiteSpace(recipeId))
            {
                if (!sourceProfession.IsCraftingProfession())
                    throw new InvalidOperationException($"Item craftado {instanceId} sem profissão válida.");
                if (string.IsNullOrWhiteSpace(craftedByPlayerId))
                    throw new InvalidOperationException($"Item craftado {instanceId} sem artesão.");
                if (string.IsNullOrWhiteSpace(originTransactionId))
                    throw new InvalidOperationException($"Item craftado {instanceId} sem proveniência.");
                if (string.IsNullOrWhiteSpace(rollSeedHash))
                    throw new InvalidOperationException($"Item craftado {instanceId} sem hash de roll.");
            }

            if (rolledStats == null)
            {
                rolledStats = new List<RolledStatData>();
            }
        }
    }

    /// <summary>
    /// Objeto raiz para JsonUtility. Listas ficam dentro de uma classe porque
    /// JsonUtility exige um objeto no nível superior.
    /// </summary>
    [Serializable]
    public sealed class InventorySnapshotData
    {
        public const int CurrentSchemaVersion = 2;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string playerId = string.Empty;
        [SerializeField] private long serverRevision;
        [SerializeField] private long generatedAtUnixMilliseconds;
        [SerializeField] private List<InventoryItemData> items = new List<InventoryItemData>();

        public int SchemaVersion => schemaVersion;
        public string PlayerId => playerId;
        public long ServerRevision => serverRevision;
        public long GeneratedAtUnixMilliseconds => generatedAtUnixMilliseconds;
        public IReadOnlyList<InventoryItemData> Items
        {
            get
            {
                if (items == null) items = new List<InventoryItemData>();
                return items;
            }
        }

        internal List<InventoryItemData> MutableItems
        {
            get
            {
                if (items == null) items = new List<InventoryItemData>();
                return items;
            }
        }

        public InventorySnapshotData()
        {
        }

        public InventorySnapshotData(
            int schemaVersion,
            string playerId,
            long serverRevision,
            long generatedAtUnixMilliseconds,
            List<InventoryItemData> items)
        {
            this.schemaVersion = schemaVersion;
            this.playerId = playerId ?? string.Empty;
            this.serverRevision = serverRevision;
            this.generatedAtUnixMilliseconds = generatedAtUnixMilliseconds;
            this.items = items ?? new List<InventoryItemData>();
        }

        internal void NormalizeAfterLoad(string fallbackPlayerId)
        {
            int sourceSchemaVersion = schemaVersion <= 0 ? 1 : schemaVersion;
            if (string.IsNullOrWhiteSpace(playerId))
                playerId = fallbackPlayerId ?? string.Empty;
            if (items == null)
                items = new List<InventoryItemData>();

            for (int i = 0; i < items.Count; i++)
            {
                items[i]?.ApplyLegacyDefaults(sourceSchemaVersion);
            }

            schemaVersion = CurrentSchemaVersion;
        }

        public static InventorySnapshotData CreateEmpty(string playerId = "")
        {
            return new InventorySnapshotData(
                CurrentSchemaVersion,
                playerId ?? string.Empty,
                0,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                new List<InventoryItemData>());
        }
    }
}
