using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IdleMedievalLegends.Domain.Campaign;
using IdleMedievalLegends.Domain.Combat;

namespace IdleMedievalLegends.Domain.Dungeons
{
    [Serializable]
    public sealed class EnergyWallet
    {
        public EnergyWallet(
            int currentEnergy,
            int maximumEnergy,
            long lastRegenerationTime,
            long revision = 0)
        {
            if (maximumEnergy < 1)
                throw new ArgumentOutOfRangeException(nameof(maximumEnergy));
            if (currentEnergy < 0 || currentEnergy > maximumEnergy)
                throw new ArgumentOutOfRangeException(nameof(currentEnergy));
            if (lastRegenerationTime < 0)
                throw new ArgumentOutOfRangeException(nameof(lastRegenerationTime));
            if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));
            CurrentEnergy = currentEnergy;
            MaximumEnergy = maximumEnergy;
            LastRegenerationTime = lastRegenerationTime;
            Revision = revision;
        }

        public int CurrentEnergy { get; private set; }
        public int MaximumEnergy { get; private set; }
        public long LastRegenerationTime { get; private set; }
        public long Revision { get; private set; }

        internal void ApplyRegeneration(int energy, long regenerationTime)
        {
            if (energy < CurrentEnergy || energy > MaximumEnergy)
                throw new ArgumentOutOfRangeException(nameof(energy));
            if (regenerationTime < LastRegenerationTime)
                throw new ArgumentOutOfRangeException(nameof(regenerationTime));
            if (energy == CurrentEnergy && regenerationTime == LastRegenerationTime)
                return;
            CurrentEnergy = energy;
            LastRegenerationTime = regenerationTime;
            Revision = checked(Revision + 1);
        }

        public void Consume(int amount)
        {
            if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
            if (CurrentEnergy < amount)
                throw new InvalidOperationException("Energia insuficiente.");
            CurrentEnergy -= amount;
            Revision = checked(Revision + 1);
        }

        public void Refund(int amount)
        {
            if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
            CurrentEnergy = Math.Min(MaximumEnergy, checked(CurrentEnergy + amount));
            Revision = checked(Revision + 1);
        }
    }

    [Serializable]
    public sealed class EnergyRegenerationRules
    {
        public EnergyRegenerationRules(int minutesPerPoint, int maximumEnergy)
        {
            if (minutesPerPoint < 1)
                throw new ArgumentOutOfRangeException(nameof(minutesPerPoint));
            if (maximumEnergy < 1)
                throw new ArgumentOutOfRangeException(nameof(maximumEnergy));
            MinutesPerPoint = minutesPerPoint;
            MaximumEnergy = maximumEnergy;
        }

        public int MinutesPerPoint { get; }
        public int MaximumEnergy { get; }

        public int Regenerate(EnergyWallet wallet, IGameClock clock)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            if (wallet.MaximumEnergy != MaximumEnergy)
                throw new InvalidOperationException(
                    "Carteira e regras possuem máximos diferentes.");

            long now = clock.UtcNowUnixMilliseconds;
            if (now < wallet.LastRegenerationTime)
                throw new InvalidOperationException("Relógio UTC regrediu.");
            if (wallet.CurrentEnergy >= MaximumEnergy)
            {
                wallet.ApplyRegeneration(MaximumEnergy, now);
                return 0;
            }

            long interval = checked(MinutesPerPoint * 60L * 1000L);
            long elapsed = now - wallet.LastRegenerationTime;
            long aggregatedPoints = elapsed / interval;
            if (aggregatedPoints <= 0)
                return 0;
            int missing = MaximumEnergy - wallet.CurrentEnergy;
            int granted = (int)Math.Min(aggregatedPoints, missing);
            long nextTime = granted == missing
                ? now
                : checked(wallet.LastRegenerationTime + granted * interval);
            wallet.ApplyRegeneration(wallet.CurrentEnergy + granted, nextTime);
            return granted;
        }
    }

    public sealed class DungeonProgress
    {
        private readonly HashSet<string> firstClears =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> dailyAttempts =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public long Revision { get; private set; }
        public IReadOnlyCollection<string> FirstClears => firstClears;

        public bool HasFirstClear(string dungeonId, string difficultyId)
        {
            return firstClears.Contains(BuildFirstClearKey(dungeonId, difficultyId));
        }

        public int GetDailyAttempts(string dungeonId, long utcUnixMilliseconds)
        {
            string key = BuildDailyKey(dungeonId, utcUnixMilliseconds);
            return dailyAttempts.TryGetValue(key, out int count) ? count : 0;
        }

        internal void ReserveAttempt(string dungeonId, long utcUnixMilliseconds)
        {
            string key = BuildDailyKey(dungeonId, utcUnixMilliseconds);
            dailyAttempts[key] = checked(GetDailyAttempts(
                dungeonId,
                utcUnixMilliseconds) + 1);
            Revision = checked(Revision + 1);
        }

        internal void ReleaseAttempt(string dungeonId, long utcUnixMilliseconds)
        {
            string key = BuildDailyKey(dungeonId, utcUnixMilliseconds);
            int count = GetDailyAttempts(dungeonId, utcUnixMilliseconds);
            if (count < 1)
                throw new InvalidOperationException("Tentativa diária não estava reservada.");
            if (count == 1)
                dailyAttempts.Remove(key);
            else
                dailyAttempts[key] = count - 1;
            Revision = checked(Revision + 1);
        }

        internal void MarkFirstClear(string dungeonId, string difficultyId)
        {
            if (firstClears.Add(BuildFirstClearKey(dungeonId, difficultyId)))
                Revision = checked(Revision + 1);
        }

        private static string BuildFirstClearKey(string dungeonId, string difficultyId)
        {
            return $"{dungeonId}:{difficultyId}";
        }

        private static string BuildDailyKey(string dungeonId, long utcUnixMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(dungeonId))
                throw new ArgumentException("dungeonId é obrigatório.", nameof(dungeonId));
            if (utcUnixMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(utcUnixMilliseconds));
            long day = utcUnixMilliseconds / 86_400_000L;
            return $"{day}:{dungeonId}";
        }
    }

    public sealed class DungeonResolvedRewards
    {
        public DungeonResolvedRewards(
            IReadOnlyList<DungeonRewardGrant> itemRewards,
            long gold)
        {
            ItemRewards = itemRewards ??
                throw new ArgumentNullException(nameof(itemRewards));
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            Gold = gold;
        }

        public IReadOnlyList<DungeonRewardGrant> ItemRewards { get; }
        public long Gold { get; }
    }

    public sealed class DungeonRewardResolver
    {
        public DungeonResolvedRewards Resolve(
            DungeonRewardTable table,
            long serverSeed,
            bool firstClear,
            int entryIndexOffset = 0)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (serverSeed <= 0) throw new ArgumentOutOfRangeException(nameof(serverSeed));
            if (entryIndexOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(entryIndexOffset));

            var random = new DeterministicRandom(serverSeed);
            var rewards = new List<DungeonRewardGrant>();
            for (int i = 0; i < table.Entries.Count; i++)
            {
                DungeonRewardTableEntry entry = table.Entries[i];
                if (entry.FirstClearOnly && !firstClear)
                    continue;
                bool granted = entry.Guaranteed ||
                    random.NextInt(10000) < entry.ChanceBasisPoints;
                if (!granted)
                    continue;
                long range = checked(entry.MaximumQuantity - entry.MinimumQuantity);
                long quantity = entry.MinimumQuantity;
                if (range > 0)
                {
                    if (range >= int.MaxValue)
                        throw new InvalidOperationException(
                            "Faixa de quantidade excede o protótipo local.");
                    quantity = checked(quantity + random.NextInt((int)range + 1));
                }
                quantity = checked(quantity * entry.DifficultyMultiplier / 10000L);
                if (quantity > 0)
                {
                    rewards.Add(new DungeonRewardGrant(
                        entry.ItemDefinitionId,
                        quantity,
                        entryIndexOffset + i));
                }
            }
            return new DungeonResolvedRewards(
                new ReadOnlyCollection<DungeonRewardGrant>(rewards),
                table.Gold);
        }
    }
}
