using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    /// <summary>Valores numéricos persistidos. Nunca reordenar.</summary>
    public enum InventoryItemState
    {
        Owned = 0,
        Equipped = 1,
        Escrow = 2,
        ReservedByServer = 3,
        Consumed = 4,
        Destroyed = 5,

        [Obsolete("Use Owned.")]
        Available = Owned,
        [Obsolete("Use Escrow.")]
        EscrowedForMarket = Escrow
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
        [SerializeField] private double percentValue;

        public string StatId => statId;
        public long FlatValue => flatValue;
        public double PercentValue => percentValue;

        public RolledStatData()
        {
        }

        public RolledStatData(string statId, long flatValue, double percentValue = 0d)
        {
            this.statId = statId ?? throw new ArgumentNullException(nameof(statId));
            this.flatValue = flatValue;
            this.percentValue = percentValue;
            Validate();
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(statId))
                throw new InvalidOperationException("Rolled stat sem statId.");
            if (double.IsNaN(percentValue) || double.IsInfinity(percentValue))
                throw new InvalidOperationException($"Rolled stat {statId} não é finito.");
        }
    }

    [Serializable]
    public sealed class ItemProvenanceData
    {
        [SerializeField] private string sourceType = string.Empty;
        [SerializeField] private string sourceId = string.Empty;
        [SerializeField] private string transactionId = string.Empty;
        [SerializeField] private string parentInstanceId = string.Empty;

        public string SourceType => sourceType;
        public string SourceId => sourceId;
        public string TransactionId => transactionId;
        public string ParentInstanceId => parentInstanceId;

        public ItemProvenanceData()
        {
        }

        public ItemProvenanceData(
            string sourceType,
            string sourceId,
            string transactionId,
            string parentInstanceId = "")
        {
            this.sourceType = sourceType ?? string.Empty;
            this.sourceId = sourceId ?? string.Empty;
            this.transactionId = transactionId ?? string.Empty;
            this.parentInstanceId = parentInstanceId ?? string.Empty;
        }

        internal void Validate(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(sourceType) ||
                string.IsNullOrWhiteSpace(transactionId))
            {
                throw new InvalidOperationException(
                    $"Item {instanceId} sem proveniência mínima.");
            }
        }
    }

    /// <summary>
    /// Instância/pilha serializável. Definições estáticas permanecem no catálogo;
    /// toda transição desta instância é publicada por PlayerInventory.
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        [SerializeField] private string instanceId = string.Empty;
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private string ownerPlayerId = string.Empty;
        [SerializeField] private InventoryItemKind kind;
        [SerializeField] private GameRarity rarity = GameRarity.Common;
        [SerializeField] private ItemTier tier = ItemTier.Tier1;
        [SerializeField] private long quantity = 1;
        [SerializeField] private bool stackable;
        [SerializeField] private InventoryItemState state = InventoryItemState.Owned;
        [SerializeField] private ItemBinding binding = ItemBinding.Unbound;
        [SerializeField] private string boundHeroInstanceId = string.Empty;
        [SerializeField] private string equippedHeroInstanceId = string.Empty;
        [SerializeField] private string marketListingId = string.Empty;
        [SerializeField] private string reservationId = string.Empty;
        [SerializeField] private long rollSeed;
        [SerializeField] private string rollSeedHash = string.Empty;
        [SerializeField] private List<RolledStatData> rolledStats = new List<RolledStatData>();
        [SerializeField] private int enhancementLevel;
        [SerializeField] private int durability = -1;
        [SerializeField] private int maxDurability = -1;
        [SerializeField] private bool lockedByPlayer;
        [SerializeField] private long serverVersion;
        [SerializeField] private long localVersion;
        [SerializeField] private long createdAtUnixMilliseconds;
        [SerializeField] private long updatedAtUnixMilliseconds;
        [SerializeField] private ItemProvenanceData provenance = new ItemProvenanceData();

        // Metadados v2 preservados para cache/auditoria.
        [SerializeField] private CraftingProfession sourceProfession = CraftingProfession.None;
        [SerializeField] private string recipeId = string.Empty;
        [SerializeField] private string craftedByPlayerId = string.Empty;
        [SerializeField] private string originTransactionId = string.Empty;
        [SerializeField] private string parentInstanceId = string.Empty;
        [SerializeField] private int qualityScoreBasisPoints;
        [SerializeField] private long craftedAtUnixMilliseconds;

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
        public string BoundHeroInstanceId => boundHeroInstanceId;
        public string EquippedHeroInstanceId => equippedHeroInstanceId;
        public string MarketListingId => marketListingId;
        public string ReservationId => reservationId;
        public long RollSeed => rollSeed;
        public string RollSeedHash => rollSeedHash;
        public IReadOnlyList<RolledStatData> RolledStats =>
            new ReadOnlyCollection<RolledStatData>(rolledStats ??= new List<RolledStatData>());
        public int EnhancementLevel => enhancementLevel;
        public int Durability => durability;
        public int MaxDurability => maxDurability;
        public bool HasDurability => maxDurability >= 0;
        public bool LockedByPlayer => lockedByPlayer;
        public long ServerVersion => serverVersion;
        public long LocalVersion => localVersion;
        public long CreatedAtUnixMilliseconds => createdAtUnixMilliseconds;
        public long UpdatedAtUnixMilliseconds => updatedAtUnixMilliseconds;
        public ItemProvenanceData Provenance => provenance;
        public CraftingProfession SourceProfession => sourceProfession;
        public string RecipeId => recipeId;
        public string CraftedByPlayerId => craftedByPlayerId;
        public string OriginTransactionId => originTransactionId;
        public string ParentInstanceId => parentInstanceId;
        public int QualityScoreBasisPoints => qualityScoreBasisPoints;
        public long CraftedAtUnixMilliseconds => craftedAtUnixMilliseconds;
        public bool IsCrafted => !string.IsNullOrWhiteSpace(recipeId);
        public bool IsTerminal => state == InventoryItemState.Consumed ||
                                  state == InventoryItemState.Destroyed;
        public bool IsTradable => binding == ItemBinding.Unbound &&
                                  state == InventoryItemState.Owned && quantity > 0;

        public ItemInstance()
        {
        }

        public ItemInstance(
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
                instanceId, definitionId, ownerPlayerId, kind, GameRarity.Common,
                ItemTier.Tier1, quantity, stackable, InventoryItemState.Owned,
                ItemBinding.Unbound, string.Empty, string.Empty, string.Empty,
                rollSeed, rolledStats, 0, -1, -1, false, serverVersion,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                new ItemProvenanceData("legacy", definitionId, $"legacy:{instanceId}"))
        {
        }

        public ItemInstance(
            string instanceId,
            string definitionId,
            string ownerPlayerId,
            InventoryItemKind kind,
            GameRarity rarity,
            ItemTier tier,
            long quantity,
            bool stackable,
            InventoryItemState state,
            ItemBinding binding,
            string equippedHeroInstanceId,
            string marketListingId,
            string reservationId,
            long rollSeed,
            IEnumerable<RolledStatData> rolledStats,
            int enhancementLevel,
            int durability,
            int maxDurability,
            bool lockedByPlayer,
            long serverVersion,
            long createdAtUnixMilliseconds,
            long updatedAtUnixMilliseconds,
            ItemProvenanceData provenance,
            string boundHeroInstanceId = "",
            string rollSeedHash = "",
            long localVersion = 0)
        {
            this.instanceId = instanceId ?? string.Empty;
            this.definitionId = definitionId ?? string.Empty;
            this.ownerPlayerId = ownerPlayerId ?? string.Empty;
            this.kind = kind;
            this.rarity = rarity;
            this.tier = tier;
            this.quantity = quantity;
            this.stackable = stackable;
            this.state = state;
            this.binding = binding;
            this.boundHeroInstanceId = boundHeroInstanceId ?? string.Empty;
            this.equippedHeroInstanceId = equippedHeroInstanceId ?? string.Empty;
            this.marketListingId = marketListingId ?? string.Empty;
            this.reservationId = reservationId ?? string.Empty;
            this.rollSeed = rollSeed;
            this.rollSeedHash = rollSeedHash ?? string.Empty;
            this.rolledStats = rolledStats == null
                ? new List<RolledStatData>()
                : new List<RolledStatData>(rolledStats);
            this.enhancementLevel = enhancementLevel;
            this.durability = durability;
            this.maxDurability = maxDurability;
            this.lockedByPlayer = lockedByPlayer;
            this.serverVersion = serverVersion;
            this.localVersion = localVersion;
            this.createdAtUnixMilliseconds = createdAtUnixMilliseconds;
            this.updatedAtUnixMilliseconds = updatedAtUnixMilliseconds;
            this.provenance = provenance;
            Validate();
        }

        internal ItemInstance Clone()
        {
            return new ItemInstance(
                instanceId, definitionId, ownerPlayerId, kind, rarity, tier, quantity,
                stackable, state, binding, equippedHeroInstanceId, marketListingId,
                reservationId, rollSeed, rolledStats, enhancementLevel, durability,
                maxDurability, lockedByPlayer, serverVersion, createdAtUnixMilliseconds,
                updatedAtUnixMilliseconds, provenance, boundHeroInstanceId, rollSeedHash,
                localVersion)
            {
                sourceProfession = sourceProfession,
                recipeId = recipeId,
                craftedByPlayerId = craftedByPlayerId,
                originTransactionId = originTransactionId,
                parentInstanceId = parentInstanceId,
                qualityScoreBasisPoints = qualityScoreBasisPoints,
                craftedAtUnixMilliseconds = craftedAtUnixMilliseconds
            };
        }

        public static ItemInstance CreateCrafted(
            string instanceId,
            string definitionId,
            string ownerPlayerId,
            InventoryItemKind kind,
            GameRarity rarity,
            ItemTier tier,
            long quantity,
            bool stackable,
            long deterministicSeed,
            string seedHash,
            long timestamp,
            CraftingProfession sourceProfession,
            string recipeId,
            string jobId,
            int qualityScoreBasisPoints,
            IEnumerable<RolledStatData> rolledStats = null)
        {
            var item = new ItemInstance(
                instanceId, definitionId, ownerPlayerId, kind, rarity, tier, quantity,
                stackable, InventoryItemState.Owned, ItemBinding.Unbound, string.Empty,
                string.Empty, string.Empty, deterministicSeed, rolledStats, 0,
                kind == InventoryItemKind.Equipment ? 100 : -1,
                kind == InventoryItemKind.Equipment ? 100 : -1,
                false, 0, timestamp, timestamp,
                new ItemProvenanceData("crafting_job", jobId, $"craft:{jobId}"),
                rollSeedHash: seedHash);
            item.sourceProfession = sourceProfession;
            item.recipeId = recipeId ?? string.Empty;
            item.craftedByPlayerId = ownerPlayerId ?? string.Empty;
            item.originTransactionId = $"craft:{jobId}";
            item.qualityScoreBasisPoints = qualityScoreBasisPoints;
            item.craftedAtUnixMilliseconds = timestamp;
            item.Validate();
            return item;
        }

        internal void SetQuantity(long value, long timestamp)
        {
            long nextVersion = PrepareTouch(timestamp);
            quantity = value;
            CompleteTouch(timestamp, nextVersion);
        }

        internal void SetState(
            InventoryItemState value,
            long timestamp,
            string heroId = "",
            string listingId = "",
            string reservation = "")
        {
            long nextVersion = PrepareTouch(timestamp);
            state = value;
            equippedHeroInstanceId = heroId ?? string.Empty;
            marketListingId = listingId ?? string.Empty;
            reservationId = reservation ?? string.Empty;
            CompleteTouch(timestamp, nextVersion);
        }

        internal void SetBinding(ItemBinding value, string heroId, long timestamp)
        {
            long nextVersion = PrepareTouch(timestamp);
            binding = value;
            boundHeroInstanceId = heroId ?? string.Empty;
            CompleteTouch(timestamp, nextVersion);
        }

        internal void SetLocked(bool value, long timestamp)
        {
            long nextVersion = PrepareTouch(timestamp);
            lockedByPlayer = value;
            CompleteTouch(timestamp, nextVersion);
        }

        internal void SetOwner(string playerId, long timestamp)
        {
            long nextVersion = PrepareTouch(timestamp);
            ownerPlayerId = playerId;
            CompleteTouch(timestamp, nextVersion);
        }

        internal void SetProvenance(ItemProvenanceData value, long timestamp)
        {
            long nextVersion = PrepareTouch(timestamp);
            provenance = value;
            CompleteTouch(timestamp, nextVersion);
        }

        internal void MarkTerminal(InventoryItemState value, long timestamp)
        {
            long nextVersion = PrepareTouch(timestamp);
            quantity = 0;
            state = value;
            equippedHeroInstanceId = string.Empty;
            marketListingId = string.Empty;
            reservationId = string.Empty;
            CompleteTouch(timestamp, nextVersion);
        }

        internal void ApplyLegacyDefaults(int sourceSchemaVersion)
        {
            if (!tier.IsValid()) tier = ItemTier.Tier1;
            if (!rarity.IsValid()) rarity = GameRarity.Common;
            rolledStats ??= new List<RolledStatData>();
            if (provenance == null &&
                sourceSchemaVersion < InventorySnapshotData.CurrentSchemaVersion)
            {
                provenance = new ItemProvenanceData(
                    sourceSchemaVersion < 3 ? "legacy_cache" : "cache",
                    definitionId,
                    $"cache:{instanceId}",
                    parentInstanceId);
            }
            if (createdAtUnixMilliseconds <= 0)
                createdAtUnixMilliseconds = craftedAtUnixMilliseconds > 0
                    ? craftedAtUnixMilliseconds
                    : 1;
            if (updatedAtUnixMilliseconds <= 0)
                updatedAtUnixMilliseconds = createdAtUnixMilliseconds;
            if (maxDurability == 0 && durability == 0)
            {
                maxDurability = -1;
                durability = -1;
            }
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
            if ((int)state < 0 || (int)state > (int)InventoryItemState.Destroyed)
                throw new InvalidOperationException($"Item {instanceId} com state inválido.");
            if (!Enum.IsDefined(typeof(ItemBinding), binding))
                throw new InvalidOperationException($"Item {instanceId} com binding inválido.");
            if (!rarity.IsValid() || !tier.IsValid())
                throw new InvalidOperationException($"Item {instanceId} com Tier/raridade inválido.");
            if (quantity < 0 || (quantity == 0 && !IsTerminal))
                throw new InvalidOperationException($"Item {instanceId} com quantidade inválida.");
            if (!stackable && quantity > 1)
                throw new InvalidOperationException($"Item único {instanceId} deve ter quantity = 1.");
            if (kind == InventoryItemKind.Equipment && quantity > 1)
                throw new InvalidOperationException($"Equipamento {instanceId} deve ter quantity = 1.");
            if (state == InventoryItemState.Equipped &&
                string.IsNullOrWhiteSpace(equippedHeroInstanceId))
                throw new InvalidOperationException($"Item equipado {instanceId} sem herói.");
            if (state != InventoryItemState.Equipped &&
                !string.IsNullOrWhiteSpace(equippedHeroInstanceId))
                throw new InvalidOperationException($"Item {instanceId} referencia herói fora de Equipped.");
            if (state == InventoryItemState.Escrow && string.IsNullOrWhiteSpace(marketListingId))
                throw new InvalidOperationException($"Item em escrow {instanceId} sem anúncio.");
            if (state != InventoryItemState.Escrow && !string.IsNullOrWhiteSpace(marketListingId))
                throw new InvalidOperationException($"Item {instanceId} referencia anúncio fora de Escrow.");
            if (state == InventoryItemState.ReservedByServer &&
                string.IsNullOrWhiteSpace(reservationId))
                throw new InvalidOperationException($"Item reservado {instanceId} sem reserva.");
            if (state != InventoryItemState.ReservedByServer &&
                !string.IsNullOrWhiteSpace(reservationId))
                throw new InvalidOperationException($"Item {instanceId} referencia reserva fora do estado.");
            if (binding == ItemBinding.HeroBound && string.IsNullOrWhiteSpace(boundHeroInstanceId))
                throw new InvalidOperationException($"Item {instanceId} HeroBound sem herói vinculado.");
            if (enhancementLevel < 0 || qualityScoreBasisPoints < 0 ||
                qualityScoreBasisPoints > 10000)
                throw new InvalidOperationException($"Item {instanceId} com progressão inválida.");
            if ((maxDurability < 0 && durability >= 0) ||
                (maxDurability >= 0 && (durability < 0 || durability > maxDurability)))
                throw new InvalidOperationException($"Item {instanceId} com durabilidade inválida.");
            if (serverVersion < 0 || localVersion < 0 || createdAtUnixMilliseconds < 0 ||
                updatedAtUnixMilliseconds < createdAtUnixMilliseconds)
                throw new InvalidOperationException($"Item {instanceId} com versão/timestamps inválidos.");
            if (provenance == null)
                throw new InvalidOperationException(
                    $"Item {instanceId} sem proveniência obrigatória.");
            provenance.Validate(instanceId);
            rolledStats ??= new List<RolledStatData>();
            for (int i = 0; i < rolledStats.Count; i++)
            {
                if (rolledStats[i] == null)
                    throw new InvalidOperationException($"Item {instanceId} contém rolled stat nulo.");
                rolledStats[i].Validate();
            }
        }

        private long PrepareTouch(long timestamp)
        {
            ValidateCanTouch(timestamp);
            return checked(localVersion + 1);
        }

        internal void ValidateCanTouch(long timestamp)
        {
            if (timestamp < 0 || timestamp < updatedAtUnixMilliseconds)
                throw new InvalidOperationException("updatedAt não pode regredir.");
        }

        private void CompleteTouch(long timestamp, long nextVersion)
        {
            updatedAtUnixMilliseconds = timestamp;
            localVersion = nextVersion;
        }
    }

    /// <summary>Alias de compatibilidade para código das Tasks 001–004.</summary>
    [Serializable]
    [Obsolete("Use ItemInstance.")]
    public sealed class InventoryItemData : ItemInstance
    {
        public InventoryItemData()
        {
        }

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
            : base(instanceId, definitionId, ownerPlayerId, kind, quantity, stackable,
                rollSeed, serverVersion, rolledStats)
        {
        }
    }

    [Serializable]
    public sealed class InventorySnapshotData
    {
        public const int CurrentSchemaVersion = 4;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private string playerId = string.Empty;
        [SerializeField] private long revision;
        [SerializeField] private long serverRevision;
        [SerializeField] private long generatedAtUnixMilliseconds;
        [SerializeField] private List<ItemInstance> items = new List<ItemInstance>();

        public int SchemaVersion => schemaVersion;
        public string PlayerId => playerId;
        public long Revision => revision;
        public long ServerRevision => serverRevision;
        public long GeneratedAtUnixMilliseconds => generatedAtUnixMilliseconds;
        public IReadOnlyList<ItemInstance> Items =>
            new ReadOnlyCollection<ItemInstance>(items ??= new List<ItemInstance>());

        internal List<ItemInstance> MutableItems => items ??= new List<ItemInstance>();

        public InventorySnapshotData()
        {
        }

        public InventorySnapshotData(
            int schemaVersion,
            string playerId,
            long serverRevision,
            long generatedAtUnixMilliseconds,
            List<ItemInstance> items)
            : this(
                schemaVersion,
                playerId,
                serverRevision,
                0,
                generatedAtUnixMilliseconds,
                items)
        {
        }

        public InventorySnapshotData(
            int schemaVersion,
            string playerId,
            long serverRevision,
            long revision,
            long generatedAtUnixMilliseconds,
            List<ItemInstance> items)
        {
            this.schemaVersion = schemaVersion;
            this.playerId = playerId ?? string.Empty;
            this.serverRevision = serverRevision;
            this.revision = revision;
            this.generatedAtUnixMilliseconds = generatedAtUnixMilliseconds;
            this.items = items ?? new List<ItemInstance>();
        }

        [Obsolete("Use ItemInstance.")]
        public InventorySnapshotData(
            int schemaVersion,
            string playerId,
            long serverRevision,
            long generatedAtUnixMilliseconds,
            List<InventoryItemData> legacyItems)
            : this(schemaVersion, playerId, serverRevision, generatedAtUnixMilliseconds,
                legacyItems == null
                    ? new List<ItemInstance>()
                    : new List<ItemInstance>(legacyItems))
        {
        }

        internal void NormalizeAfterLoad(string fallbackPlayerId)
        {
            int sourceSchemaVersion = schemaVersion <= 0 ? 1 : schemaVersion;
            if (sourceSchemaVersion > CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Schema de inventário {sourceSchemaVersion} não é compatível.");
            }
            if (string.IsNullOrWhiteSpace(playerId)) playerId = fallbackPlayerId ?? string.Empty;
            items ??= new List<ItemInstance>();
            if (sourceSchemaVersion <= 2)
            {
                // Até v2, serverRevision já era separado e não havia revisão local.
                revision = 0;
            }
            else if (sourceSchemaVersion == 3)
            {
                // A v3 misturava revisão local e de servidor. Como cache não é
                // autoridade, não promovemos esse valor a revisão autoritativa.
                serverRevision = 0;
            }
            for (int i = 0; i < items.Count; i++)
                items[i]?.ApplyLegacyDefaults(sourceSchemaVersion);
            schemaVersion = CurrentSchemaVersion;
        }

        public static InventorySnapshotData CreateEmpty(string playerId = "")
        {
            return new InventorySnapshotData(
                CurrentSchemaVersion, playerId ?? string.Empty, 0, 0,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), new List<ItemInstance>());
        }
    }
}
