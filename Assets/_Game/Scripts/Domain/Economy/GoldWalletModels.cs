using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Economy
{
    [Serializable]
    public sealed class GoldLedgerEntry
    {
        [SerializeField] private string entryId = string.Empty;
        [SerializeField] private string reason = string.Empty;
        [SerializeField] private long delta;
        [SerializeField] private long balanceAfter;
        [SerializeField] private string requestId = string.Empty;
        [SerializeField] private long timestamp;
        [SerializeField] private string source = string.Empty;

        public GoldLedgerEntry(
            string entryId,
            string reason,
            long delta,
            long balanceAfter,
            string requestId,
            long timestamp,
            string source)
        {
            if (string.IsNullOrWhiteSpace(entryId))
                throw new ArgumentException("entryId é obrigatório.", nameof(entryId));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("reason é obrigatório.", nameof(reason));
            if (balanceAfter < 0) throw new ArgumentOutOfRangeException(nameof(balanceAfter));
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("requestId é obrigatório.", nameof(requestId));
            if (timestamp < 0) throw new ArgumentOutOfRangeException(nameof(timestamp));
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("source é obrigatório.", nameof(source));
            this.entryId = entryId;
            this.reason = reason;
            this.delta = delta;
            this.balanceAfter = balanceAfter;
            this.requestId = requestId;
            this.timestamp = timestamp;
            this.source = source;
        }

        public string EntryId => entryId;
        public string Reason => reason;
        public long Delta => delta;
        public long BalanceAfter => balanceAfter;
        public string RequestId => requestId;
        public long Timestamp => timestamp;
        public string Source => source;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(entryId))
                throw new InvalidOperationException("Entrada do ledger sem entryId.");
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException($"Entrada {entryId} sem reason.");
            if (balanceAfter < 0)
                throw new InvalidOperationException(
                    $"Entrada {entryId} possui balanceAfter negativo.");
            if (string.IsNullOrWhiteSpace(requestId))
                throw new InvalidOperationException($"Entrada {entryId} sem requestId.");
            if (timestamp < 0)
                throw new InvalidOperationException(
                    $"Entrada {entryId} possui timestamp negativo.");
            if (string.IsNullOrWhiteSpace(source))
                throw new InvalidOperationException($"Entrada {entryId} sem source.");
        }
    }

    [Serializable]
    public sealed class GoldWalletSnapshot
    {
        [SerializeField] private long balance;
        [SerializeField] private long revision;
        [SerializeField] private List<GoldLedgerEntry> ledger = new List<GoldLedgerEntry>();

        public GoldWalletSnapshot()
        {
        }

        public GoldWalletSnapshot(
            long balance,
            long revision,
            IEnumerable<GoldLedgerEntry> ledger)
        {
            if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
            if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));
            this.balance = balance;
            this.revision = revision;
            this.ledger = ledger == null
                ? new List<GoldLedgerEntry>()
                : new List<GoldLedgerEntry>(ledger);
            Validate();
        }

        public long Balance => balance;
        public long Revision => revision;
        public IReadOnlyList<GoldLedgerEntry> Ledger =>
            new ReadOnlyCollection<GoldLedgerEntry>(ledger);

        public static GoldWalletSnapshot CreateEmpty()
        {
            return new GoldWalletSnapshot(0, 0, Array.Empty<GoldLedgerEntry>());
        }

        public void Validate()
        {
            if (balance < 0)
                throw new InvalidOperationException("Saldo da carteira não pode ser negativo.");
            if (revision < 0)
                throw new InvalidOperationException("Revisão da carteira não pode ser negativa.");
            if (ledger == null)
                throw new InvalidOperationException("Ledger da carteira não pode ser nulo.");
            if (revision != ledger.Count)
            {
                throw new InvalidOperationException(
                    "Revisão da carteira deve corresponder ao número de entradas.");
            }

            var requests = new HashSet<string>(StringComparer.Ordinal);
            var entryIds = new HashSet<string>(StringComparer.Ordinal);
            long previousBalance = 0;
            for (int i = 0; i < ledger.Count; i++)
            {
                GoldLedgerEntry entry = ledger[i] ??
                    throw new InvalidOperationException("Entrada nula no ledger.");
                entry.Validate();
                if (!entryIds.Add(entry.EntryId))
                    throw new InvalidOperationException(
                        $"entryId duplicado no ledger: {entry.EntryId}.");
                if (!requests.Add(entry.RequestId))
                    throw new InvalidOperationException(
                        $"requestId duplicado no ledger: {entry.RequestId}.");
                long expected = checked(previousBalance + entry.Delta);
                if (expected != entry.BalanceAfter)
                    throw new InvalidOperationException("Ledger de ouro inconsistente.");
                previousBalance = entry.BalanceAfter;
            }
            if (ledger.Count > 0 && previousBalance != balance)
                throw new InvalidOperationException("Saldo difere do ledger de ouro.");
            if (ledger.Count == 0 && balance != 0)
                throw new InvalidOperationException("Saldo sem entrada correspondente no ledger.");
        }
    }
}
