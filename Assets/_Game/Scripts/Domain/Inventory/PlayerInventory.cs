using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Content;

namespace IdleMedievalLegends.Domain.Inventory
{
    /// <summary>
    /// Agregado local de inventário. Em produção, seus comandos serão alimentados
    /// por resultados autoritativos do backend; o cache nunca prova propriedade.
    /// </summary>
    [Serializable]
    public sealed class PlayerInventory
    {
        private InventorySnapshotData snapshot = InventorySnapshotData.CreateEmpty();

        [NonSerialized]
        private Dictionary<string, ItemInstance> itemIndex =
            new Dictionary<string, ItemInstance>(StringComparer.Ordinal);

        public event Action Changed;

        public string PlayerId => snapshot.PlayerId;
        public long Revision => snapshot.Revision;
        public long ServerRevision => snapshot.ServerRevision;
        public IReadOnlyList<ItemInstance> Items => snapshot.Items;

        public void ApplySnapshot(
            InventorySnapshotData newSnapshot,
            ContentCatalogLookup catalog = null)
        {
            ApplyServerSnapshot(newSnapshot, catalog);
        }

        public void ApplyServerSnapshot(
            InventorySnapshotData newSnapshot,
            ContentCatalogLookup catalog = null)
        {
            if (newSnapshot == null) throw new ArgumentNullException(nameof(newSnapshot));
            if (newSnapshot.SchemaVersion != InventorySnapshotData.CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Schema de inventário não suportado: {newSnapshot.SchemaVersion}.");
            }
            if (newSnapshot.Revision < 0 || newSnapshot.ServerRevision < 0)
                throw new InvalidOperationException(
                    "Revisões local e de servidor não podem ser negativas.");

            var rebuiltIndex = new Dictionary<string, ItemInstance>(
                newSnapshot.Items.Count,
                StringComparer.Ordinal);
            var validatedItems = new List<ItemInstance>(newSnapshot.Items.Count);
            for (int i = 0; i < newSnapshot.Items.Count; i++)
            {
                ItemInstance source = newSnapshot.Items[i];
                if (source == null)
                    throw new InvalidOperationException($"Item nulo no índice {i} do snapshot.");
                source.Validate();
                if (!string.IsNullOrWhiteSpace(newSnapshot.PlayerId) &&
                    !string.Equals(newSnapshot.PlayerId, source.OwnerPlayerId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Item {source.InstanceId} pertence a outro jogador.");
                }
                if (rebuiltIndex.ContainsKey(source.InstanceId))
                    throw new InvalidOperationException(
                        $"instanceId duplicado no snapshot: {source.InstanceId}");
                if (catalog != null)
                {
                    if (!catalog.TryGetItem(source.DefinitionId, out ItemDefinition definition))
                        throw new InvalidOperationException(
                            $"Definição inexistente: {source.DefinitionId}.");
                    ValidateDefinitionCompatibility(source, definition);
                }
                ItemInstance item = source.Clone();
                validatedItems.Add(item);
                rebuiltIndex.Add(item.InstanceId, item);
            }

            snapshot = new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                newSnapshot.PlayerId,
                newSnapshot.ServerRevision,
                newSnapshot.Revision,
                newSnapshot.GeneratedAtUnixMilliseconds,
                validatedItems);
            itemIndex = rebuiltIndex;
            if (catalog != null) definitionResolver = catalog;
            Changed?.Invoke();
        }

        public void ConfigureDefinitionResolver(ContentCatalogLookup catalog)
        {
            definitionResolver = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public bool TryGetItem(string instanceId, out ItemInstance item)
        {
            EnsureRuntimeState();
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                item = null;
                return false;
            }
            return itemIndex.TryGetValue(instanceId, out item);
        }

        public ItemInstance GetItem(string instanceId)
        {
            if (!TryGetItem(instanceId, out ItemInstance item))
                throw new KeyNotFoundException($"Item inexistente: {instanceId}.");
            return item;
        }

        public void AddAuthorizedItem(
            ItemInstance item,
            ItemDefinition definition,
            long timestamp)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            EnsureRuntimeState();
            item.Validate();
            ValidateTimestamp(timestamp);
            item.ValidateCanTouch(timestamp);
            ValidateDefinitionCompatibility(item, definition);
            if (item.State != InventoryItemState.Owned)
                throw new InvalidOperationException("Novo item autorizado deve entrar como Owned.");
            if (!string.IsNullOrWhiteSpace(PlayerId) &&
                !string.Equals(PlayerId, item.OwnerPlayerId, StringComparison.Ordinal))
                throw new InvalidOperationException("Item autorizado pertence a outro jogador.");
            if (itemIndex.ContainsKey(item.InstanceId))
                throw new InvalidOperationException($"instanceId duplicado: {item.InstanceId}.");

            ItemInstance copy = item.Clone();
            snapshot.MutableItems.Add(copy);
            itemIndex.Add(copy.InstanceId, copy);
            AdvanceRevision(timestamp);
        }

        public void AddStack(
            ItemInstance incoming,
            ItemDefinition definition,
            long timestamp)
        {
            if (definition == null || !definition.Stackable)
                throw new InvalidOperationException("AddStack requer definição empilhável.");
            AddAuthorizedItem(incoming, definition, timestamp);
        }

        public void RemoveQuantity(string instanceId, long quantity, long timestamp)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            ItemInstance item = RequireMutableOwned(instanceId, "remover quantidade");
            if (quantity > item.Quantity)
                throw new InvalidOperationException("Quantidade solicitada excede a pilha.");
            PrepareMutation(timestamp, item);
            long remaining = item.Quantity - quantity;
            if (remaining == 0)
                item.MarkTerminal(InventoryItemState.Consumed, timestamp);
            else
                item.SetQuantity(remaining, timestamp);
            AdvanceRevision(timestamp);
        }

        public ItemInstance SplitStack(
            string sourceInstanceId,
            string newInstanceId,
            long quantity,
            ItemDefinition definition,
            long timestamp)
        {
            if (definition == null || !definition.Stackable)
                throw new InvalidOperationException("Somente item empilhável pode ser dividido.");
            if (string.IsNullOrWhiteSpace(newInstanceId))
                throw new ArgumentException("Novo instanceId é obrigatório.", nameof(newInstanceId));
            if (itemIndex.ContainsKey(newInstanceId))
                throw new InvalidOperationException($"instanceId duplicado: {newInstanceId}.");
            ItemInstance source = RequireMutableOwned(sourceInstanceId, "dividir");
            ValidateDefinitionCompatibility(source, definition);
            if (!source.Stackable || quantity <= 0 || quantity >= source.Quantity)
                throw new InvalidOperationException("Quantidade de split deve ser positiva e menor que a pilha.");
            PrepareMutation(timestamp, source);

            var split = new ItemInstance(
                newInstanceId, source.DefinitionId, source.OwnerPlayerId, source.Kind,
                source.Rarity, source.Tier, quantity, true, InventoryItemState.Owned,
                source.Binding, string.Empty, string.Empty, string.Empty, source.RollSeed,
                source.RolledStats, source.EnhancementLevel, source.Durability,
                source.MaxDurability, source.LockedByPlayer, 0, timestamp, timestamp,
                new ItemProvenanceData(
                    "stack_split", source.InstanceId,
                    $"split:{source.InstanceId}:{newInstanceId}", source.InstanceId),
                source.BoundHeroInstanceId,
                source.RollSeedHash);
            source.SetQuantity(source.Quantity - quantity, timestamp);
            snapshot.MutableItems.Add(split);
            itemIndex.Add(split.InstanceId, split);
            AdvanceRevision(timestamp);
            return split;
        }

        public void CombineStacks(
            string targetInstanceId,
            string sourceInstanceId,
            ItemDefinition definition,
            long timestamp)
        {
            if (string.Equals(targetInstanceId, sourceInstanceId, StringComparison.Ordinal))
                throw new InvalidOperationException("Uma pilha não pode ser combinada consigo mesma.");
            if (definition == null || !definition.Stackable)
                throw new InvalidOperationException("Somente definição empilhável pode combinar pilhas.");
            ItemInstance target = RequireMutableOwned(targetInstanceId, "combinar");
            ItemInstance source = RequireMutableOwned(sourceInstanceId, "combinar");
            if (!string.Equals(target.DefinitionId, source.DefinitionId, StringComparison.Ordinal))
                throw new InvalidOperationException("Pilhas de definições diferentes não combinam.");
            ValidateDefinitionCompatibility(target, definition);
            ValidateDefinitionCompatibility(source, definition);
            if (target.Binding != source.Binding ||
                !string.Equals(target.BoundHeroInstanceId, source.BoundHeroInstanceId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Pilhas com bindings diferentes não combinam.");
            long combined = checked(target.Quantity + source.Quantity);
            if (combined > definition.MaxStackSize)
                throw new InvalidOperationException("Combinação excede o tamanho máximo da pilha.");
            PrepareMutation(timestamp, target, source);
            target.SetQuantity(combined, timestamp);
            source.MarkTerminal(InventoryItemState.Consumed, timestamp);
            AdvanceRevision(timestamp);
        }

        public void Equip(
            string instanceId,
            HeroEquipmentContext hero,
            EquipmentDefinition definition,
            long timestamp)
        {
            ItemInstance item = GetItem(instanceId);
            EquipmentRules.ValidateEquip(item, definition, hero);
            for (int i = 0; i < snapshot.Items.Count; i++)
            {
                ItemInstance equipped = snapshot.Items[i];
                if (equipped.State != InventoryItemState.Equipped ||
                    !string.Equals(equipped.EquippedHeroInstanceId, hero.HeroInstanceId,
                        StringComparison.Ordinal)) continue;
                if (equipped.InstanceId == item.InstanceId)
                    throw new InvalidOperationException("Item já está equipado.");
                if (definition.EquipmentSlot == ResolveEquipmentSlot(equipped))
                    throw new InvalidOperationException(
                        $"Herói já possui item no slot {definition.EquipmentSlot}.");
            }

            PrepareMutation(timestamp, item);
            switch (definition.BindingRule)
            {
                case BindingRule.UnboundUntilEquipped:
                    if (item.Binding == ItemBinding.Unbound)
                        item.SetBinding(ItemBinding.AccountBound, string.Empty, timestamp);
                    break;
                case BindingRule.AccountBoundOnAcquire:
                    if (item.Binding != ItemBinding.AccountBound)
                        throw new InvalidOperationException("Item deveria estar AccountBound ao adquirir.");
                    break;
                case BindingRule.AlwaysUnbound:
                    if (item.Binding != ItemBinding.Unbound)
                        throw new InvalidOperationException("Item AlwaysUnbound possui binding inválido.");
                    break;
                default:
                    throw new InvalidOperationException("BindingRule inválida.");
            }
            item.SetState(InventoryItemState.Equipped, timestamp, hero.HeroInstanceId);
            AdvanceRevision(timestamp);
        }

        public void Unequip(string instanceId, long timestamp)
        {
            ItemInstance item = GetItem(instanceId);
            if (item.State != InventoryItemState.Equipped)
                throw new InvalidOperationException("Item não está equipado.");
            PrepareMutation(timestamp, item);
            item.SetState(InventoryItemState.Owned, timestamp);
            AdvanceRevision(timestamp);
        }

        public void Lock(string instanceId, long timestamp)
        {
            ItemInstance item = RequireNonTerminal(instanceId, "bloquear");
            if (item.LockedByPlayer) throw new InvalidOperationException("Item já está bloqueado.");
            PrepareMutation(timestamp, item);
            item.SetLocked(true, timestamp);
            AdvanceRevision(timestamp);
        }

        public void Unlock(string instanceId, long timestamp)
        {
            ItemInstance item = RequireNonTerminal(instanceId, "desbloquear");
            if (!item.LockedByPlayer) throw new InvalidOperationException("Item não está bloqueado.");
            PrepareMutation(timestamp, item);
            item.SetLocked(false, timestamp);
            AdvanceRevision(timestamp);
        }

        public void Reserve(string instanceId, string reservationId, long timestamp)
        {
            if (string.IsNullOrWhiteSpace(reservationId))
                throw new ArgumentException("reservationId é obrigatório.", nameof(reservationId));
            ItemInstance item = RequireMutableOwned(instanceId, "reservar");
            if (item.LockedByPlayer)
                throw new InvalidOperationException("Item bloqueado não pode ser reservado.");
            PrepareMutation(timestamp, item);
            item.SetState(InventoryItemState.ReservedByServer, timestamp,
                reservation: reservationId);
            AdvanceRevision(timestamp);
        }

        public void ReleaseReservation(string instanceId, string reservationId, long timestamp)
        {
            ItemInstance item = GetItem(instanceId);
            if (item.State != InventoryItemState.ReservedByServer ||
                !string.Equals(item.ReservationId, reservationId, StringComparison.Ordinal))
                throw new InvalidOperationException("Reserva não corresponde ao item.");
            PrepareMutation(timestamp, item);
            item.SetState(InventoryItemState.Owned, timestamp);
            AdvanceRevision(timestamp);
        }

        public void ConsumeReservation(
            string instanceId,
            string reservationId,
            long quantity,
            long timestamp)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            ItemInstance item = GetItem(instanceId);
            if (item.State != InventoryItemState.ReservedByServer ||
                !string.Equals(item.ReservationId, reservationId, StringComparison.Ordinal))
                throw new InvalidOperationException("Reserva não corresponde ao item.");
            if (quantity > item.Quantity)
                throw new InvalidOperationException("Quantidade reservada insuficiente.");
            PrepareMutation(timestamp, item);
            if (quantity == item.Quantity)
                item.MarkTerminal(InventoryItemState.Consumed, timestamp);
            else
            {
                item.SetQuantity(item.Quantity - quantity, timestamp);
                item.SetState(InventoryItemState.Owned, timestamp);
            }
            AdvanceRevision(timestamp);
        }

        public void Consume(string instanceId, long quantity, long timestamp)
        {
            RemoveQuantity(instanceId, quantity, timestamp);
        }

        public void Destroy(
            string instanceId,
            ItemDefinition definition,
            long timestamp)
        {
            ItemInstance item = GetItem(instanceId);
            InventoryDismantleRules.Evaluate(item, definition);
            PrepareMutation(timestamp, item);
            item.MarkTerminal(InventoryItemState.Destroyed, timestamp);
            AdvanceRevision(timestamp);
        }

        public IReadOnlyList<DismantleYield> Dismantle(
            string instanceId,
            ItemDefinition definition,
            long timestamp)
        {
            ItemInstance item = GetItem(instanceId);
            IReadOnlyList<DismantleYield> outputs =
                InventoryDismantleRules.Evaluate(item, definition);
            PrepareMutation(timestamp, item);
            item.MarkTerminal(InventoryItemState.Destroyed, timestamp);
            AdvanceRevision(timestamp);
            return outputs;
        }

        public void MarkEscrow(
            string instanceId,
            string listingId,
            ItemDefinition definition,
            long timestamp)
        {
            if (string.IsNullOrWhiteSpace(listingId))
                throw new ArgumentException("listingId é obrigatório.", nameof(listingId));
            ItemInstance item = RequireMutableOwned(instanceId, "anunciar");
            ValidateDefinitionCompatibility(item, definition);
            if (!definition.Tradable || item.Binding != ItemBinding.Unbound)
                throw new InvalidOperationException("Item não é negociável.");
            PrepareMutation(timestamp, item);
            item.SetState(InventoryItemState.Escrow, timestamp, listingId: listingId);
            AdvanceRevision(timestamp);
        }

        public void CancelEscrow(string instanceId, string listingId, long timestamp)
        {
            ItemInstance item = GetItem(instanceId);
            if (item.State != InventoryItemState.Escrow ||
                !string.Equals(item.MarketListingId, listingId, StringComparison.Ordinal))
                throw new InvalidOperationException("Anúncio não corresponde ao item.");
            PrepareMutation(timestamp, item);
            item.SetState(InventoryItemState.Owned, timestamp);
            AdvanceRevision(timestamp);
        }

        public ItemInstance TransferOwnershipAuthorized(
            string instanceId,
            string newOwnerPlayerId,
            ItemDefinition definition,
            string transactionId,
            long timestamp)
        {
            if (string.IsNullOrWhiteSpace(newOwnerPlayerId) ||
                string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Novo dono e transação são obrigatórios.");
            ItemInstance item = RequireMutableOwned(instanceId, "transferir");
            ValidateDefinitionCompatibility(item, definition);
            if (!definition.Tradable || item.Binding != ItemBinding.Unbound)
                throw new InvalidOperationException("Binding impede transferência.");
            if (item.LockedByPlayer)
                throw new InvalidOperationException("Item bloqueado não pode ser transferido.");
            PrepareMutation(timestamp, item);
            item.SetOwner(newOwnerPlayerId, timestamp);
            item.SetProvenance(new ItemProvenanceData(
                "authorized_transfer", item.InstanceId, transactionId), timestamp);
            snapshot.MutableItems.Remove(item);
            itemIndex.Remove(item.InstanceId);
            AdvanceRevision(timestamp);
            return item.Clone();
        }

        public InventorySnapshotData CaptureSnapshotForCache()
        {
            var copies = new List<ItemInstance>(snapshot.Items.Count);
            for (int i = 0; i < snapshot.Items.Count; i++)
                copies.Add(snapshot.Items[i].Clone());
            return new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                snapshot.PlayerId,
                snapshot.ServerRevision,
                snapshot.Revision,
                snapshot.GeneratedAtUnixMilliseconds,
                copies);
        }

        public void Clear(string playerId = "")
        {
            ApplyServerSnapshot(InventorySnapshotData.CreateEmpty(playerId));
        }

        private ItemInstance RequireMutableOwned(string instanceId, string action)
        {
            ItemInstance item = GetItem(instanceId);
            if (item.State == InventoryItemState.Destroyed)
                throw new InvalidOperationException("Item Destroyed nunca pode retornar.");
            if (item.State == InventoryItemState.Consumed)
                throw new InvalidOperationException("Item Consumed não pode ser usado novamente.");
            if (item.State == InventoryItemState.ReservedByServer)
                throw new InvalidOperationException(
                    $"Item ReservedByServer não pode ser usado para {action}.");
            if (item.State != InventoryItemState.Owned)
                throw new InvalidOperationException($"Item deve estar Owned para {action}.");
            return item;
        }

        private ItemInstance RequireNonTerminal(string instanceId, string action)
        {
            ItemInstance item = GetItem(instanceId);
            if (item.IsTerminal)
                throw new InvalidOperationException($"Item terminal não pode ser usado para {action}.");
            return item;
        }

        private EquipmentSlot ResolveEquipmentSlot(ItemInstance item)
        {
            if (definitionResolver == null ||
                !definitionResolver.TryGetItem(item.DefinitionId, out ItemDefinition definition) ||
                !(definition is EquipmentDefinition equipment))
            {
                throw new InvalidOperationException(
                    $"Definição de equipamento não disponível para {item.DefinitionId}.");
            }
            return equipment.EquipmentSlot;
        }

        [NonSerialized] private ContentCatalogLookup definitionResolver;

        private static void ValidateDefinitionCompatibility(
            ItemInstance item,
            ItemDefinition definition)
        {
            if (!string.Equals(item.DefinitionId, definition.DefinitionId, StringComparison.Ordinal))
                throw new InvalidOperationException("Instância referencia outra definição.");
            if (item.Stackable != definition.Stackable)
                throw new InvalidOperationException("Empilhamento diverge da definição.");
            if (item.Quantity > definition.MaxStackSize)
                throw new InvalidOperationException("Quantidade excede maxStackSize.");
            if (item.Tier.ToContentTier() != definition.Tier)
                throw new InvalidOperationException("Tier diverge da definição.");
            if (!item.IsCrafted && item.Rarity.ToCatalogRarity() != definition.Rarity)
                throw new InvalidOperationException("Raridade diverge da definição.");
            if (!IsKindCompatible(item.Kind, definition.ItemType))
                throw new InvalidOperationException("Kind diverge do ItemType da definição.");
            if (definition is EquipmentDefinition equipment)
            {
                if (item.EnhancementLevel > equipment.EnhancementLimit)
                    throw new InvalidOperationException(
                        "Aprimoramento excede o limite da definição.");
                ValidateEquipmentBinding(item, equipment);
            }
        }

        private static void ValidateEquipmentBinding(
            ItemInstance item,
            EquipmentDefinition definition)
        {
            switch (definition.BindingRule)
            {
                case BindingRule.AccountBoundOnAcquire:
                    if (item.Binding != ItemBinding.AccountBound)
                    {
                        throw new InvalidOperationException(
                            "Equipamento AccountBoundOnAcquire deve entrar AccountBound.");
                    }
                    break;
                case BindingRule.AlwaysUnbound:
                    if (item.Binding != ItemBinding.Unbound)
                    {
                        throw new InvalidOperationException(
                            "Equipamento AlwaysUnbound deve permanecer Unbound.");
                    }
                    break;
                case BindingRule.UnboundUntilEquipped:
                    if (item.Binding == ItemBinding.HeroBound ||
                        (item.State == InventoryItemState.Equipped &&
                         item.Binding == ItemBinding.Unbound))
                    {
                        throw new InvalidOperationException(
                            "Binding do equipamento diverge de UnboundUntilEquipped.");
                    }
                    break;
                default:
                    throw new InvalidOperationException("BindingRule inválida.");
            }
        }

        private static InventoryItemKind ToKind(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Equipment: return InventoryItemKind.Equipment;
                case ItemType.Material: return InventoryItemKind.Material;
                case ItemType.Consumable: return InventoryItemKind.Consumable;
                case ItemType.Enchantment: return InventoryItemKind.Enchantment;
                case ItemType.RecipeDiagram: return InventoryItemKind.Diagram;
                case ItemType.Tool: return InventoryItemKind.CraftingTool;
                default: throw new InvalidOperationException($"ItemType não suportado: {itemType}.");
            }
        }

        private static bool IsKindCompatible(
            InventoryItemKind itemKind,
            ItemType definitionType)
        {
            if (definitionType == ItemType.Material)
            {
                return itemKind == InventoryItemKind.Material ||
                       itemKind == InventoryItemKind.RefinedMaterial;
            }
            return itemKind == ToKind(definitionType);
        }

        private void PrepareMutation(long timestamp, params ItemInstance[] items)
        {
            ValidateTimestamp(timestamp);
            for (int i = 0; i < items.Length; i++)
                items[i].ValidateCanTouch(timestamp);
        }

        private void AdvanceRevision(long timestamp)
        {
            ValidateTimestamp(timestamp);
            snapshot = new InventorySnapshotData(
                InventorySnapshotData.CurrentSchemaVersion,
                snapshot.PlayerId,
                snapshot.ServerRevision,
                checked(snapshot.Revision + 1),
                timestamp,
                snapshot.MutableItems);
            Changed?.Invoke();
        }

        private void ValidateTimestamp(long timestamp)
        {
            if (timestamp < 0 || timestamp < snapshot.GeneratedAtUnixMilliseconds)
                throw new InvalidOperationException("Timestamp do inventário não pode regredir.");
        }

        private void EnsureRuntimeState()
        {
            snapshot ??= InventorySnapshotData.CreateEmpty();
            if (itemIndex != null) return;
            itemIndex = new Dictionary<string, ItemInstance>(StringComparer.Ordinal);
            for (int i = 0; i < snapshot.Items.Count; i++)
            {
                ItemInstance item = snapshot.Items[i];
                if (item != null) itemIndex[item.InstanceId] = item;
            }
        }

        private void SetDefinitionResolver(ContentCatalogLookup catalog)
        {
            definitionResolver = catalog;
        }
    }
}
