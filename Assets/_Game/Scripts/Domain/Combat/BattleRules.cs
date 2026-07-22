using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;

namespace IdleMedievalLegends.Domain.Combat
{
    /// <summary>
    /// Xorshift64* com estado privado. A implementação faz parte da versão das
    /// regras e não depende de System.Random ou UnityEngine.Random.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private ulong state;

        public DeterministicRandom(long seed)
        {
            if (seed <= 0) throw new ArgumentOutOfRangeException(nameof(seed));
            state = unchecked((ulong)seed);
        }

        public ulong NextUInt64()
        {
            ulong value = state;
            value ^= value >> 12;
            value ^= value << 25;
            value ^= value >> 27;
            state = value;
            return value * 2685821657736338717UL;
        }

        public double NextDouble()
        {
            return (NextUInt64() >> 11) * (1d / 9007199254740992d);
        }

        public int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));

            // Rejection sampling evita viés de módulo.
            ulong range = (ulong)exclusiveMaximum;
            ulong limit = ulong.MaxValue - (ulong.MaxValue % range);
            ulong value;
            do
            {
                value = NextUInt64();
            }
            while (value >= limit);
            return (int)(value % range);
        }
    }

    public sealed class TurnOrderResolver
    {
        private const double GaugeEpsilon = 0.000000001d;

        public BattleUnitState SelectNext(
            IReadOnlyList<BattleUnitState> units,
            double actionGaugeThreshold)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));
            if (!BattleConfiguration.IsFinite(actionGaugeThreshold) ||
                actionGaugeThreshold <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(actionGaugeThreshold));
            }

            double elapsed = double.MaxValue;
            bool hasLivingUnit = false;
            for (int i = 0; i < units.Count; i++)
            {
                BattleUnitState unit = units[i];
                if (unit == null || !unit.Alive)
                    continue;
                hasLivingUnit = true;
                double remaining = Math.Max(0d, actionGaugeThreshold - unit.ActionGauge);
                elapsed = Math.Min(elapsed, remaining / unit.Speed);
            }

            if (!hasLivingUnit)
                throw new InvalidOperationException("Não há unidade viva para agir.");
            if (!BattleConfiguration.IsFinite(elapsed) || elapsed < 0d)
                throw new InvalidOperationException("Tempo de action gauge inválido.");

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null && units[i].Alive)
                    units[i].AdvanceGauge(elapsed);
            }

            BattleUnitState selected = null;
            for (int i = 0; i < units.Count; i++)
            {
                BattleUnitState candidate = units[i];
                if (candidate == null || !candidate.Alive ||
                    candidate.ActionGauge + GaugeEpsilon < actionGaugeThreshold)
                {
                    continue;
                }

                if (selected == null || Compare(candidate, selected) < 0)
                    selected = candidate;
            }

            if (selected == null)
                throw new InvalidOperationException("Action gauge não selecionou uma unidade.");
            selected.ConsumeGauge(actionGaugeThreshold);
            return selected;
        }

        private static int Compare(BattleUnitState left, BattleUnitState right)
        {
            int gauge = right.ActionGauge.CompareTo(left.ActionGauge);
            if (gauge != 0) return gauge;
            int speed = right.Speed.CompareTo(left.Speed);
            if (speed != 0) return speed;
            int team = ((int)left.Team).CompareTo((int)right.Team);
            if (team != 0) return team;
            int slot = left.Slot.CompareTo(right.Slot);
            if (slot != 0) return slot;
            return string.Compare(left.UnitId, right.UnitId, StringComparison.Ordinal);
        }
    }

    public interface ITargetSelector
    {
        CombatSnapshot SelectTarget(
            CombatSnapshot source,
            IReadOnlyList<CombatSnapshot> candidates,
            DeterministicRandom random);
    }

    public sealed class TargetSelector : ITargetSelector
    {
        private readonly TargetSelectionMode mode;

        public TargetSelector(TargetSelectionMode mode)
        {
            if (!Enum.IsDefined(typeof(TargetSelectionMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));
            this.mode = mode;
        }

        public CombatSnapshot SelectTarget(
            CombatSnapshot source,
            IReadOnlyList<CombatSnapshot> candidates,
            DeterministicRandom random)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var eligible = new List<CombatSnapshot>();
            for (int i = 0; i < candidates.Count; i++)
            {
                CombatSnapshot candidate = candidates[i];
                if (candidate != null && candidate.Alive && candidate.Team != source.Team)
                    eligible.Add(candidate);
            }
            if (eligible.Count == 0)
                throw new InvalidOperationException("Não há alvo vivo elegível.");

            eligible.Sort(CompareStable);
            int index = mode == TargetSelectionMode.Random
                ? random.NextInt(eligible.Count)
                : 0;
            return eligible[index];
        }

        private static int CompareStable(CombatSnapshot left, CombatSnapshot right)
        {
            int slot = left.Slot.CompareTo(right.Slot);
            if (slot != 0) return slot;
            return string.Compare(left.UnitId, right.UnitId, StringComparison.Ordinal);
        }
    }

    public readonly struct DamageResult
    {
        public DamageResult(
            bool hit,
            bool critical,
            long damage,
            double hitChance,
            double variation,
            double damageReduction)
        {
            Hit = hit;
            Critical = critical;
            Damage = damage;
            HitChance = hitChance;
            Variation = variation;
            DamageReduction = damageReduction;
        }

        public bool Hit { get; }
        public bool Critical { get; }
        public long Damage { get; }
        public double HitChance { get; }
        public double Variation { get; }
        public double DamageReduction { get; }
    }

    public sealed class DamageCalculator
    {
        public DamageResult Calculate(
            CombatSnapshot source,
            CombatSnapshot target,
            BattleConfiguration configuration,
            DeterministicRandom random)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (!source.Alive || !target.Alive || source.Team == target.Team)
                throw new InvalidOperationException("Fonte ou alvo inválido para dano.");

            double hitChance = Clamp(
                source.Accuracy - target.Evasion,
                configuration.MinimumHitChance,
                configuration.MaximumHitChance);
            bool hit = random.NextDouble() < hitChance;
            if (!hit)
                return new DamageResult(false, false, 0, hitChance, 0d, 0d);

            bool critical = random.NextDouble() < source.CriticalChance;
            double variation = configuration.MinimumDamageVariation +
                (configuration.MaximumDamageVariation -
                 configuration.MinimumDamageVariation) * random.NextDouble();
            double rawDamage = source.Attack * configuration.BasicAttackMultiplier;
            if (critical)
                rawDamage *= source.CriticalMultiplier;
            rawDamage *= variation;

            double levelMultiplier = CalculateLevelMultiplier(
                target.Level,
                configuration);
            double defenseScale = configuration.DefenseScaleAtLevelOne * levelMultiplier;
            double reduction = target.Defense <= 0
                ? 0d
                : Clamp(
                    target.Defense / (target.Defense + defenseScale),
                    0d,
                    configuration.MaximumDamageReduction);
            double mitigated = rawDamage * (1d - reduction);
            if (!BattleConfiguration.IsFinite(mitigated) || mitigated < 0d ||
                mitigated > long.MaxValue)
            {
                throw new OverflowException("Dano final inválido ou acima de Int64.");
            }

            long damage = checked((long)Math.Floor(mitigated));
            damage = Math.Max(configuration.MinimumDamage, damage);
            return new DamageResult(true, critical, damage, hitChance, variation, reduction);
        }

        public double CalculateLevelMultiplier(
            int level,
            BattleConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            double x = level - 1d;
            double result = 1d + configuration.LevelLinearCoefficient * x +
                            configuration.LevelQuadraticCoefficient * x * x;
            if (!BattleConfiguration.IsFinite(result) || result <= 0d)
                throw new OverflowException("Multiplicador de nível inválido.");
            return result;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum) return minimum;
            return value > maximum ? maximum : value;
        }
    }

    public static class BattleUnitFactory
    {
        public static BattleUnit FromHero(
            HeroInstance hero,
            HeroDefinition definition,
            IHeroEquipmentModifierProvider equipmentModifierProvider,
            CombatBalanceTuning heroTuning,
            BattleConfiguration battleConfiguration,
            BattleSide side,
            int slot)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (!hero.Unlocked)
                throw new InvalidOperationException("Herói bloqueado não entra em batalha.");
            if (battleConfiguration == null)
                throw new ArgumentNullException(nameof(battleConfiguration));

            HeroPowerBreakdown breakdown = HeroPowerCalculator.CalculateBreakdown(
                hero,
                definition,
                equipmentModifierProvider,
                heroTuning);
            HeroStatBlock stats = breakdown.FinalStats;
            return new BattleUnit(
                hero.InstanceId,
                definition.DefinitionId,
                side,
                slot,
                hero.Level,
                stats.MaxHealth,
                stats.MaxHealth,
                stats.Attack,
                stats.Defense,
                stats.Speed,
                battleConfiguration.DefaultCriticalChance,
                battleConfiguration.DefaultCriticalMultiplier,
                battleConfiguration.DefaultAccuracy,
                battleConfiguration.DefaultEvasion,
                0d,
                definition.Tags);
        }
    }

    internal static class DeterministicBattleHasher
    {
        public static string Compute(BattleResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var canonical = new StringBuilder(4096);
            Append(canonical, result.BattleId);
            Append(canonical, result.Seed);
            Append(canonical, (int)result.Outcome);
            Append(canonical, result.WinningTeam.HasValue
                ? (int)result.WinningTeam.Value
                : 0);
            Append(canonical, result.TurnCount);
            Append(canonical, result.ActionCount);
            Append(canonical, result.EndReason);
            Append(canonical, result.RulesVersion);

            for (int i = 0; i < result.Events.Count; i++)
            {
                CombatEvent value = result.Events[i];
                Append(canonical, value.Sequence);
                Append(canonical, value.Turn);
                Append(canonical, value.LogicalTick);
                Append(canonical, (int)value.EventType);
                Append(canonical, value.SourceUnitId);
                Append(canonical, value.TargetUnitId);
                Append(canonical, value.Value);
                Append(canonical, value.Critical);
                Append(canonical, value.TargetHealthBefore);
                Append(canonical, value.TargetHealthAfter);
                for (int metadataIndex = 0;
                     metadataIndex < value.Metadata.Count;
                     metadataIndex++)
                {
                    Append(canonical, value.Metadata[metadataIndex].Key);
                    Append(canonical, value.Metadata[metadataIndex].Value);
                }
                Append(canonical, "event-end");
            }

            for (int i = 0; i < result.FinalSnapshots.Count; i++)
            {
                CombatSnapshot value = result.FinalSnapshots[i];
                Append(canonical, value.UnitId);
                Append(canonical, value.DefinitionId);
                Append(canonical, (int)value.Team);
                Append(canonical, value.Slot);
                Append(canonical, value.Level);
                Append(canonical, value.MaximumHealth);
                Append(canonical, value.CurrentHealth);
                Append(canonical, value.Attack);
                Append(canonical, value.Defense);
                Append(canonical, value.Speed);
                Append(canonical, value.CriticalChance);
                Append(canonical, value.CriticalMultiplier);
                Append(canonical, value.Accuracy);
                Append(canonical, value.Evasion);
                Append(canonical, value.Alive);
                Append(canonical, value.ActionGauge);
                for (int tagIndex = 0; tagIndex < value.Tags.Count; tagIndex++)
                    Append(canonical, value.Tags[tagIndex]);
                Append(canonical, "snapshot-end");
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
                var text = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    text.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }

        private static void Append(StringBuilder builder, object value)
        {
            string text;
            if (value is double number)
                text = number.ToString("R", CultureInfo.InvariantCulture);
            else if (value is IFormattable formattable)
                text = formattable.ToString(null, CultureInfo.InvariantCulture);
            else
                text = value?.ToString() ?? string.Empty;
            builder.Append(text.Length).Append(':').Append(text).Append('|');
        }
    }
}
