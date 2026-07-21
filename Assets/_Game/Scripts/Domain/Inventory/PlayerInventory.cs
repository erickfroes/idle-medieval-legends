using System;
using System.Collections.Generic;

namespace IdleMedievalLegends.Domain.Inventory
{
    /// <summary>
    /// Agregado de inventário do cliente. Ele não cria itens nem altera saldo:
    /// apenas aplica snapshots/mutações já validados pelo backend autoritativo.
    /// </summary>
    [Serializable]
    public sealed class PlayerInventory
    {
        private InventorySnapshotData snapshot = InventorySnapshotData.CreateEmpty();

        [NonSerialized]
        private Dictionary<string, InventoryItemData> itemIndex =
            new Dictionary<string, InventoryItemData>(StringComparer.Ordinal);

        public event Action Changed;

        public string PlayerId => snapshot.PlayerId;
        public long ServerRevision => snapshot.ServerRevision;
        public IReadOnlyList<InventoryItemData> Items => snapshot.Items;

        public void ApplyServerSnapshot(InventorySnapshotData newSnapshot)
        {
            if (newSnapshot == null)
                throw new ArgumentNullException(nameof(newSnapshot));

            EnsureRuntimeState();

            if (newSnapshot.SchemaVersion != InventorySnapshotData.CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Schema de inventário não suportado: {newSnapshot.SchemaVersion}.");
            }

            var rebuiltIndex = new Dictionary<string, InventoryItemData>(
                newSnapshot.Items.Count,
                StringComparer.Ordinal);

            for (int i = 0; i < newSnapshot.Items.Count; i++)
            {
                InventoryItemData item = newSnapshot.Items[i];
                if (item == null)
                    throw new InvalidOperationException($"Item nulo no índice {i} do snapshot.");

                item.Validate();

                if (!string.IsNullOrWhiteSpace(newSnapshot.PlayerId) &&
                    !string.Equals(
                        newSnapshot.PlayerId,
                        item.OwnerPlayerId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Item {item.InstanceId} pertence a outro jogador.");
                }

                if (rebuiltIndex.ContainsKey(item.InstanceId))
                {
                    throw new InvalidOperationException(
                        $"instanceId duplicado no snapshot: {item.InstanceId}");
                }

                rebuiltIndex.Add(item.InstanceId, item);
            }

            snapshot = newSnapshot;
            itemIndex = rebuiltIndex;
            Changed?.Invoke();
        }

        public bool TryGetItem(string instanceId, out InventoryItemData item)
        {
            EnsureRuntimeState();

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                item = null;
                return false;
            }

            return itemIndex.TryGetValue(instanceId, out item);
        }

        public InventorySnapshotData CaptureSnapshotForCache()
        {
            // O agregado troca o snapshot por referência, mas não expõe setters.
            // O repositório deve serializar imediatamente e não modificar o objeto.
            return snapshot;
        }

        public void Clear(string playerId = "")
        {
            ApplyServerSnapshot(InventorySnapshotData.CreateEmpty(playerId));
        }

        private void EnsureRuntimeState()
        {
            if (snapshot == null)
            {
                snapshot = InventorySnapshotData.CreateEmpty();
            }

            if (itemIndex != null)
            {
                return;
            }

            itemIndex = new Dictionary<string, InventoryItemData>(StringComparer.Ordinal);
            for (int i = 0; i < snapshot.Items.Count; i++)
            {
                InventoryItemData item = snapshot.Items[i];
                if (item == null) continue;
                itemIndex[item.InstanceId] = item;
            }
        }
    }
}
