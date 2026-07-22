using System;
using System.Collections.Generic;
using System.Globalization;

namespace IdleMedievalLegends.Domain.Combat
{
    /// <summary>
    /// Simulador síncrono e puro. Toda mutação fica restrita aos estados internos
    /// criados a partir dos snapshots de entrada.
    /// </summary>
    public sealed class BattleSimulator
    {
        private readonly TurnOrderResolver turnOrderResolver;
        private readonly DamageCalculator damageCalculator;
        private readonly ITargetSelector targetSelectorOverride;

        public BattleSimulator(
            TurnOrderResolver turnOrderResolver = null,
            DamageCalculator damageCalculator = null,
            ITargetSelector targetSelector = null)
        {
            this.turnOrderResolver = turnOrderResolver ?? new TurnOrderResolver();
            this.damageCalculator = damageCalculator ?? new DamageCalculator();
            targetSelectorOverride = targetSelector;
        }

        public BattleResult Simulate(BattleRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Configuration.Validate();

            List<BattleUnitState> states = CreateStates(request);
            var random = new DeterministicRandom(request.Seed);
            ITargetSelector targetSelector = targetSelectorOverride ??
                new TargetSelector(request.Configuration.TargetSelectionMode);
            var events = new List<CombatEvent>();
            int sequence = 0;
            int turnCount = 0;
            int actionCount = 0;

            AddEvent(
                events, ref sequence, 0, 0, CombatEventType.BattleStarted,
                string.Empty, string.Empty, 0, false, 0, 0,
                new[]
                {
                    new CombatEventMetadata("rulesVersion", request.RulesVersion)
                });

            BattleOutcome outcome;
            BattleSide? winningTeam;
            string endReason;

            while (true)
            {
                bool attackersAlive = HasLiving(states, BattleSide.Attacker);
                bool defendersAlive = HasLiving(states, BattleSide.Defender);
                if (!attackersAlive || !defendersAlive)
                {
                    ResolveElimination(
                        attackersAlive,
                        defendersAlive,
                        out outcome,
                        out winningTeam,
                        out endReason);
                    break;
                }
                if (actionCount >= request.Configuration.MaximumActions)
                {
                    outcome = BattleOutcome.Draw;
                    winningTeam = null;
                    endReason = "action_limit";
                    break;
                }

                turnCount = checked(turnCount + 1);
                long logicalTick = turnCount;
                BattleUnitState source = turnOrderResolver.SelectNext(
                    states,
                    request.Configuration.ActionGaugeThreshold);
                AddEvent(
                    events, ref sequence, turnCount, logicalTick,
                    CombatEventType.TurnStarted, source.UnitId, string.Empty,
                    0, false, 0, 0);
                AddEvent(
                    events, ref sequence, turnCount, logicalTick,
                    CombatEventType.UnitSelected, source.UnitId, string.Empty,
                    0, false, 0, 0);

                IReadOnlyList<CombatSnapshot> candidates = GetLivingOpponents(
                    states,
                    source.Team);
                CombatSnapshot selectedTarget = targetSelector.SelectTarget(
                    source.ToSnapshot(),
                    candidates,
                    random);
                if (selectedTarget == null)
                    throw new InvalidOperationException("Política de alvo retornou nulo.");
                BattleUnitState target = FindState(states, selectedTarget.UnitId);
                if (!target.Alive || target.Team == source.Team)
                {
                    throw new InvalidOperationException(
                        "Política de alvo retornou uma unidade inelegível.");
                }
                long healthBefore = target.CurrentHealth;
                AddEvent(
                    events, ref sequence, turnCount, logicalTick,
                    CombatEventType.BasicAttackStarted, source.UnitId, target.UnitId,
                    0, false, healthBefore, healthBefore);

                DamageResult damage = damageCalculator.Calculate(
                    source.ToSnapshot(),
                    target.ToSnapshot(),
                    request.Configuration,
                    random);
                actionCount = checked(actionCount + 1);

                if (!damage.Hit)
                {
                    AddEvent(
                        events, ref sequence, turnCount, logicalTick,
                        CombatEventType.AttackMissed, source.UnitId, target.UnitId,
                        0, false, healthBefore, healthBefore,
                        Metadata("hitChance", damage.HitChance));
                }
                else
                {
                    long appliedDamage = Math.Min(damage.Damage, healthBefore);
                    target.ApplyDamage(appliedDamage);
                    if (damage.Critical)
                    {
                        AddEvent(
                            events, ref sequence, turnCount, logicalTick,
                            CombatEventType.CriticalHit, source.UnitId, target.UnitId,
                            appliedDamage, true, healthBefore, target.CurrentHealth);
                    }
                    AddEvent(
                        events, ref sequence, turnCount, logicalTick,
                        CombatEventType.DamageDealt, source.UnitId, target.UnitId,
                        appliedDamage, damage.Critical, healthBefore,
                        target.CurrentHealth,
                        new[]
                        {
                            new CombatEventMetadata(
                                "variation",
                                damage.Variation.ToString("R", CultureInfo.InvariantCulture)),
                            new CombatEventMetadata(
                                "damageReduction",
                                damage.DamageReduction.ToString(
                                    "R",
                                    CultureInfo.InvariantCulture))
                        });
                    if (!target.Alive)
                    {
                        AddEvent(
                            events, ref sequence, turnCount, logicalTick,
                            CombatEventType.UnitDefeated, source.UnitId, target.UnitId,
                            0, damage.Critical, healthBefore, 0);
                    }
                }

                AddEvent(
                    events, ref sequence, turnCount, logicalTick,
                    CombatEventType.TurnEnded, source.UnitId, target.UnitId,
                    0, damage.Critical, healthBefore, target.CurrentHealth);
            }

            AddEvent(
                events, ref sequence, turnCount, turnCount,
                CombatEventType.BattleEnded, string.Empty, string.Empty,
                0, false, 0, 0,
                new[]
                {
                    new CombatEventMetadata("reason", endReason),
                    new CombatEventMetadata("outcome", outcome.ToString()),
                    new CombatEventMetadata(
                        "winningTeam",
                        winningTeam?.ToString() ?? string.Empty)
                });

            return new BattleResult(
                request.BattleId,
                request.Seed,
                outcome,
                winningTeam,
                turnCount,
                actionCount,
                endReason,
                request.RulesVersion,
                events,
                CreateFinalSnapshots(states));
        }

        private static List<BattleUnitState> CreateStates(BattleRequest request)
        {
            var states = new List<BattleUnitState>(
                request.Attacker.Units.Count + request.Defender.Units.Count);
            AddStates(states, request.Attacker.Units);
            AddStates(states, request.Defender.Units);
            states.Sort(CompareStates);
            return states;
        }

        private static void AddStates(
            List<BattleUnitState> states,
            IReadOnlyList<BattleUnit> units)
        {
            for (int i = 0; i < units.Count; i++)
                states.Add(new BattleUnitState(units[i]));
        }

        private static bool HasLiving(
            IReadOnlyList<BattleUnitState> states,
            BattleSide team)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].Team == team && states[i].Alive)
                    return true;
            }
            return false;
        }

        private static IReadOnlyList<CombatSnapshot> GetLivingOpponents(
            IReadOnlyList<BattleUnitState> states,
            BattleSide sourceTeam)
        {
            var result = new List<CombatSnapshot>();
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].Team != sourceTeam && states[i].Alive)
                    result.Add(states[i].ToSnapshot());
            }
            result.Sort(CompareSnapshots);
            return result.AsReadOnly();
        }

        private static BattleUnitState FindState(
            IReadOnlyList<BattleUnitState> states,
            string unitId)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (string.Equals(states[i].UnitId, unitId, StringComparison.Ordinal))
                    return states[i];
            }
            throw new InvalidOperationException($"Alvo {unitId} não pertence à batalha.");
        }

        private static IReadOnlyList<CombatSnapshot> CreateFinalSnapshots(
            IReadOnlyList<BattleUnitState> states)
        {
            var result = new List<CombatSnapshot>(states.Count);
            for (int i = 0; i < states.Count; i++)
                result.Add(states[i].ToSnapshot());
            result.Sort(CompareSnapshots);
            return result.AsReadOnly();
        }

        private static int CompareStates(BattleUnitState left, BattleUnitState right)
        {
            int team = ((int)left.Team).CompareTo((int)right.Team);
            if (team != 0) return team;
            int slot = left.Slot.CompareTo(right.Slot);
            if (slot != 0) return slot;
            return string.Compare(left.UnitId, right.UnitId, StringComparison.Ordinal);
        }

        private static int CompareSnapshots(CombatSnapshot left, CombatSnapshot right)
        {
            int team = ((int)left.Team).CompareTo((int)right.Team);
            if (team != 0) return team;
            int slot = left.Slot.CompareTo(right.Slot);
            if (slot != 0) return slot;
            return string.Compare(left.UnitId, right.UnitId, StringComparison.Ordinal);
        }

        private static void ResolveElimination(
            bool attackersAlive,
            bool defendersAlive,
            out BattleOutcome outcome,
            out BattleSide? winningTeam,
            out string reason)
        {
            if (attackersAlive && !defendersAlive)
            {
                outcome = BattleOutcome.AttackerVictory;
                winningTeam = BattleSide.Attacker;
                reason = "defender_eliminated";
                return;
            }
            if (!attackersAlive && defendersAlive)
            {
                outcome = BattleOutcome.DefenderVictory;
                winningTeam = BattleSide.Defender;
                reason = "attacker_eliminated";
                return;
            }

            outcome = BattleOutcome.Draw;
            winningTeam = null;
            reason = "both_teams_eliminated";
        }

        private static IEnumerable<CombatEventMetadata> Metadata(
            string key,
            double value)
        {
            return new[]
            {
                new CombatEventMetadata(
                    key,
                    value.ToString("R", CultureInfo.InvariantCulture))
            };
        }

        private static void AddEvent(
            List<CombatEvent> events,
            ref int sequence,
            int turn,
            long logicalTick,
            CombatEventType eventType,
            string sourceUnitId,
            string targetUnitId,
            long value,
            bool critical,
            long targetHealthBefore,
            long targetHealthAfter,
            IEnumerable<CombatEventMetadata> metadata = null)
        {
            events.Add(new CombatEvent(
                sequence,
                turn,
                logicalTick,
                eventType,
                sourceUnitId,
                targetUnitId,
                value,
                critical,
                targetHealthBefore,
                targetHealthAfter,
                metadata));
            sequence = checked(sequence + 1);
        }
    }
}
