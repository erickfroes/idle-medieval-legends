using System;
using System.Collections.Generic;

namespace IdleMedievalLegends.Domain.Combat
{
    public enum BattleSide
    {
        Attacker = 1,
        Defender = 2
    }

    public enum BattleOutcome
    {
        Draw = 0,
        AttackerVictory = 1,
        DefenderVictory = 2
    }

    public enum CombatEventType
    {
        BattleStarted = 1,
        TurnStarted = 2,
        UnitSelected = 3,
        BasicAttackStarted = 4,
        AttackMissed = 5,
        DamageDealt = 6,
        CriticalHit = 7,
        UnitDefeated = 8,
        TurnEnded = 9,
        BattleEnded = 10
    }

    public enum TargetSelectionMode
    {
        LowestSlot = 0,
        Random = 1
    }

    [Serializable]
    public sealed class BattleConfiguration
    {
        public BattleConfiguration(
            int maximumTeamSize = 5,
            int maximumActions = 500,
            double actionGaugeThreshold = 1000d,
            double basicAttackMultiplier = 1d,
            double minimumDamageVariation = 0.95d,
            double maximumDamageVariation = 1.05d,
            long minimumDamage = 1,
            double defaultCriticalChance = 0.05d,
            double defaultCriticalMultiplier = 1.5d,
            double defaultAccuracy = 0.95d,
            double defaultEvasion = 0.05d,
            double minimumHitChance = 0.05d,
            double maximumHitChance = 0.99d,
            double defenseScaleAtLevelOne = 400d,
            double maximumDamageReduction = 0.75d,
            double levelLinearCoefficient = 0.065d,
            double levelQuadraticCoefficient = 0.00035d,
            TargetSelectionMode targetSelectionMode = TargetSelectionMode.LowestSlot)
        {
            MaximumTeamSize = maximumTeamSize;
            MaximumActions = maximumActions;
            ActionGaugeThreshold = actionGaugeThreshold;
            BasicAttackMultiplier = basicAttackMultiplier;
            MinimumDamageVariation = minimumDamageVariation;
            MaximumDamageVariation = maximumDamageVariation;
            MinimumDamage = minimumDamage;
            DefaultCriticalChance = defaultCriticalChance;
            DefaultCriticalMultiplier = defaultCriticalMultiplier;
            DefaultAccuracy = defaultAccuracy;
            DefaultEvasion = defaultEvasion;
            MinimumHitChance = minimumHitChance;
            MaximumHitChance = maximumHitChance;
            DefenseScaleAtLevelOne = defenseScaleAtLevelOne;
            MaximumDamageReduction = maximumDamageReduction;
            LevelLinearCoefficient = levelLinearCoefficient;
            LevelQuadraticCoefficient = levelQuadraticCoefficient;
            TargetSelectionMode = targetSelectionMode;
            Validate();
        }

        public int MaximumTeamSize { get; }
        public int MaximumActions { get; }
        public double ActionGaugeThreshold { get; }
        public double BasicAttackMultiplier { get; }
        public double MinimumDamageVariation { get; }
        public double MaximumDamageVariation { get; }
        public long MinimumDamage { get; }
        public double DefaultCriticalChance { get; }
        public double DefaultCriticalMultiplier { get; }
        public double DefaultAccuracy { get; }
        public double DefaultEvasion { get; }
        public double MinimumHitChance { get; }
        public double MaximumHitChance { get; }
        public double DefenseScaleAtLevelOne { get; }
        public double MaximumDamageReduction { get; }
        public double LevelLinearCoefficient { get; }
        public double LevelQuadraticCoefficient { get; }
        public TargetSelectionMode TargetSelectionMode { get; }

        public void Validate()
        {
            if (MaximumTeamSize < 1 || MaximumTeamSize > 5)
                throw new ArgumentOutOfRangeException(nameof(MaximumTeamSize));
            if (MaximumActions < 1)
                throw new ArgumentOutOfRangeException(nameof(MaximumActions));
            RequirePositiveFinite(ActionGaugeThreshold, nameof(ActionGaugeThreshold));
            RequirePositiveFinite(BasicAttackMultiplier, nameof(BasicAttackMultiplier));
            RequirePositiveFinite(MinimumDamageVariation, nameof(MinimumDamageVariation));
            RequirePositiveFinite(MaximumDamageVariation, nameof(MaximumDamageVariation));
            if (MaximumDamageVariation < MinimumDamageVariation)
                throw new ArgumentException("Faixa de variação de dano inválida.");
            if (MinimumDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(MinimumDamage));
            RequireProbability(DefaultCriticalChance, nameof(DefaultCriticalChance));
            if (!IsFinite(DefaultCriticalMultiplier) || DefaultCriticalMultiplier < 1d)
                throw new ArgumentOutOfRangeException(nameof(DefaultCriticalMultiplier));
            RequireProbability(DefaultAccuracy, nameof(DefaultAccuracy));
            RequireProbability(DefaultEvasion, nameof(DefaultEvasion));
            RequireProbability(MinimumHitChance, nameof(MinimumHitChance));
            RequireProbability(MaximumHitChance, nameof(MaximumHitChance));
            if (MaximumHitChance < MinimumHitChance)
                throw new ArgumentException("Limites de acerto inválidos.");
            RequirePositiveFinite(DefenseScaleAtLevelOne, nameof(DefenseScaleAtLevelOne));
            RequireProbability(MaximumDamageReduction, nameof(MaximumDamageReduction));
            if (MaximumDamageReduction >= 1d)
                throw new ArgumentOutOfRangeException(nameof(MaximumDamageReduction));
            RequireNonNegativeFinite(LevelLinearCoefficient, nameof(LevelLinearCoefficient));
            RequireNonNegativeFinite(LevelQuadraticCoefficient, nameof(LevelQuadraticCoefficient));
            if (!Enum.IsDefined(typeof(TargetSelectionMode), TargetSelectionMode))
                throw new ArgumentOutOfRangeException(nameof(TargetSelectionMode));
        }

        internal static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void RequireProbability(double value, string name)
        {
            if (!IsFinite(value) || value < 0d || value > 1d)
                throw new ArgumentOutOfRangeException(name);
        }

        private static void RequirePositiveFinite(double value, string name)
        {
            if (!IsFinite(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(name);
        }

        private static void RequireNonNegativeFinite(double value, string name)
        {
            if (!IsFinite(value) || value < 0d)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public sealed class BattleUnit
    {
        public BattleUnit(
            string unitId,
            string definitionId,
            BattleSide team,
            int slot,
            int level,
            long maximumHealth,
            long currentHealth,
            long attack,
            long defense,
            double speed,
            double criticalChance,
            double criticalMultiplier,
            double accuracy,
            double evasion,
            double actionGauge,
            IEnumerable<string> tags)
        {
            UnitId = unitId;
            DefinitionId = definitionId;
            Team = team;
            Slot = slot;
            Level = level;
            MaximumHealth = maximumHealth;
            CurrentHealth = currentHealth;
            Attack = attack;
            Defense = defense;
            Speed = speed;
            CriticalChance = criticalChance;
            CriticalMultiplier = criticalMultiplier;
            Accuracy = accuracy;
            Evasion = evasion;
            Alive = currentHealth > 0;
            ActionGauge = actionGauge;
            Tags = CopyTags(tags);
            BattleUnitValidation.Validate(this);
        }

        public string UnitId { get; }
        public string DefinitionId { get; }
        public BattleSide Team { get; }
        public int Slot { get; }
        public int Level { get; }
        public long MaximumHealth { get; }
        public long CurrentHealth { get; }
        public long Attack { get; }
        public long Defense { get; }
        public double Speed { get; }
        public double CriticalChance { get; }
        public double CriticalMultiplier { get; }
        public double Accuracy { get; }
        public double Evasion { get; }
        public bool Alive { get; }
        public double ActionGauge { get; }
        public IReadOnlyList<string> Tags { get; }

        private static IReadOnlyList<string> CopyTags(IEnumerable<string> tags)
        {
            return (tags == null ? new List<string>() : new List<string>(tags)).AsReadOnly();
        }
    }

    public sealed class BattleUnitState
    {
        public BattleUnitState(BattleUnit unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            UnitId = unit.UnitId;
            DefinitionId = unit.DefinitionId;
            Team = unit.Team;
            Slot = unit.Slot;
            Level = unit.Level;
            MaximumHealth = unit.MaximumHealth;
            CurrentHealth = unit.CurrentHealth;
            Attack = unit.Attack;
            Defense = unit.Defense;
            Speed = unit.Speed;
            CriticalChance = unit.CriticalChance;
            CriticalMultiplier = unit.CriticalMultiplier;
            Accuracy = unit.Accuracy;
            Evasion = unit.Evasion;
            Alive = unit.Alive;
            ActionGauge = unit.ActionGauge;
            Tags = unit.Tags;
        }

        public string UnitId { get; }
        public string DefinitionId { get; }
        public BattleSide Team { get; }
        public int Slot { get; }
        public int Level { get; }
        public long MaximumHealth { get; }
        public long CurrentHealth { get; private set; }
        public long Attack { get; }
        public long Defense { get; }
        public double Speed { get; }
        public double CriticalChance { get; }
        public double CriticalMultiplier { get; }
        public double Accuracy { get; }
        public double Evasion { get; }
        public bool Alive { get; private set; }
        public double ActionGauge { get; private set; }
        public IReadOnlyList<string> Tags { get; }

        internal void AdvanceGauge(double elapsed)
        {
            double next = ActionGauge + Speed * elapsed;
            if (!BattleConfiguration.IsFinite(next) || next < 0d)
                throw new OverflowException("Action gauge inválido.");
            ActionGauge = next;
        }

        internal void ConsumeGauge(double threshold)
        {
            ActionGauge = Math.Max(0d, ActionGauge - threshold);
        }

        internal void ApplyDamage(long damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            CurrentHealth = damage >= CurrentHealth ? 0 : checked(CurrentHealth - damage);
            Alive = CurrentHealth > 0;
        }

        public CombatSnapshot ToSnapshot()
        {
            return new CombatSnapshot(
                UnitId, DefinitionId, Team, Slot, Level, MaximumHealth,
                CurrentHealth, Attack, Defense, Speed, CriticalChance,
                CriticalMultiplier, Accuracy, Evasion, Alive, ActionGauge, Tags);
        }
    }

    public sealed class CombatSnapshot
    {
        public CombatSnapshot(
            string unitId,
            string definitionId,
            BattleSide team,
            int slot,
            int level,
            long maximumHealth,
            long currentHealth,
            long attack,
            long defense,
            double speed,
            double criticalChance,
            double criticalMultiplier,
            double accuracy,
            double evasion,
            bool alive,
            double actionGauge,
            IEnumerable<string> tags)
        {
            UnitId = unitId;
            DefinitionId = definitionId;
            Team = team;
            Slot = slot;
            Level = level;
            MaximumHealth = maximumHealth;
            CurrentHealth = currentHealth;
            Attack = attack;
            Defense = defense;
            Speed = speed;
            CriticalChance = criticalChance;
            CriticalMultiplier = criticalMultiplier;
            Accuracy = accuracy;
            Evasion = evasion;
            Alive = alive;
            ActionGauge = actionGauge;
            Tags = (tags == null ? new List<string>() : new List<string>(tags)).AsReadOnly();
            BattleUnitValidation.Validate(this);
        }

        public string UnitId { get; }
        public string DefinitionId { get; }
        public BattleSide Team { get; }
        public int Slot { get; }
        public int Level { get; }
        public long MaximumHealth { get; }
        public long CurrentHealth { get; }
        public long Attack { get; }
        public long Defense { get; }
        public double Speed { get; }
        public double CriticalChance { get; }
        public double CriticalMultiplier { get; }
        public double Accuracy { get; }
        public double Evasion { get; }
        public bool Alive { get; }
        public double ActionGauge { get; }
        public IReadOnlyList<string> Tags { get; }
    }

    public sealed class BattleTeam
    {
        public BattleTeam(BattleSide side, IEnumerable<BattleUnit> units)
        {
            if (!Enum.IsDefined(typeof(BattleSide), side))
                throw new ArgumentOutOfRangeException(nameof(side));
            if (units == null) throw new ArgumentNullException(nameof(units));
            var copy = new List<BattleUnit>(units);
            if (copy.Count == 0)
                throw new ArgumentException("Equipe não pode estar vazia.", nameof(units));
            Side = side;
            Units = copy.AsReadOnly();
        }

        public BattleSide Side { get; }
        public IReadOnlyList<BattleUnit> Units { get; }
    }

    public sealed class BattleRequest
    {
        public BattleRequest(
            BattleTeam attacker,
            BattleTeam defender,
            long seed,
            BattleConfiguration configuration,
            string rulesVersion,
            string battleId = null)
        {
            Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
            Defender = defender ?? throw new ArgumentNullException(nameof(defender));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (seed <= 0) throw new ArgumentOutOfRangeException(nameof(seed));
            if (string.IsNullOrWhiteSpace(rulesVersion))
                throw new ArgumentException("rulesVersion é obrigatória.", nameof(rulesVersion));
            if (battleId != null && string.IsNullOrWhiteSpace(battleId))
                throw new ArgumentException("battleId opcional não pode ser vazio.", nameof(battleId));

            Seed = seed;
            RulesVersion = rulesVersion;
            BattleId = battleId ?? string.Empty;
            ValidateTeams();
        }

        public BattleTeam Attacker { get; }
        public BattleTeam Defender { get; }
        public long Seed { get; }
        public BattleConfiguration Configuration { get; }
        public string BattleId { get; }
        public string RulesVersion { get; }

        private void ValidateTeams()
        {
            Configuration.Validate();
            if (Attacker.Side != BattleSide.Attacker ||
                Defender.Side != BattleSide.Defender)
            {
                throw new InvalidOperationException("Lados atacante/defensor inválidos.");
            }
            if (Attacker.Units.Count > Configuration.MaximumTeamSize ||
                Defender.Units.Count > Configuration.MaximumTeamSize)
            {
                throw new InvalidOperationException("Equipe excede o limite configurado.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            ValidateTeam(Attacker, ids, Configuration.MaximumTeamSize);
            ValidateTeam(Defender, ids, Configuration.MaximumTeamSize);
        }

        private static void ValidateTeam(
            BattleTeam team,
            HashSet<string> ids,
            int maximumTeamSize)
        {
            var slots = new HashSet<int>();
            for (int i = 0; i < team.Units.Count; i++)
            {
                BattleUnit unit = team.Units[i];
                if (unit == null)
                    throw new InvalidOperationException("Equipe contém unidade nula.");
                BattleUnitValidation.Validate(unit);
                if (unit.Team != team.Side)
                    throw new InvalidOperationException("Unidade pertence ao lado incorreto.");
                if (unit.Slot >= maximumTeamSize)
                    throw new InvalidOperationException(
                        $"Slot {unit.Slot} excede o limite da equipe.");
                if (!ids.Add(unit.UnitId))
                    throw new InvalidOperationException($"unitId duplicado: {unit.UnitId}.");
                if (!slots.Add(unit.Slot))
                    throw new InvalidOperationException(
                        $"Slot duplicado em {team.Side}: {unit.Slot}.");
            }
        }
    }

    [Serializable]
    public sealed class CombatEventMetadata
    {
        public CombatEventMetadata(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Chave de metadata obrigatória.", nameof(key));
            Key = key;
            Value = value ?? string.Empty;
        }

        public string Key { get; }
        public string Value { get; }
    }

    public sealed class CombatEvent
    {
        public CombatEvent(
            int sequence,
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
            if (sequence < 0 || turn < 0 || logicalTick < 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            if (!Enum.IsDefined(typeof(CombatEventType), eventType))
                throw new ArgumentOutOfRangeException(nameof(eventType));
            if (value < 0 || targetHealthBefore < 0 || targetHealthAfter < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Sequence = sequence;
            Turn = turn;
            LogicalTick = logicalTick;
            EventType = eventType;
            SourceUnitId = sourceUnitId ?? string.Empty;
            TargetUnitId = targetUnitId ?? string.Empty;
            Value = value;
            Critical = critical;
            TargetHealthBefore = targetHealthBefore;
            TargetHealthAfter = targetHealthAfter;
            Metadata = (metadata == null
                ? new List<CombatEventMetadata>()
                : new List<CombatEventMetadata>(metadata)).AsReadOnly();
        }

        public int Sequence { get; }
        public int Turn { get; }
        public long LogicalTick { get; }
        public CombatEventType EventType { get; }
        public string SourceUnitId { get; }
        public string TargetUnitId { get; }
        public long Value { get; }
        public bool Critical { get; }
        public long TargetHealthBefore { get; }
        public long TargetHealthAfter { get; }
        public IReadOnlyList<CombatEventMetadata> Metadata { get; }
    }

    public sealed class BattleResult
    {
        public BattleResult(
            string battleId,
            long seed,
            BattleOutcome outcome,
            BattleSide? winningTeam,
            int turnCount,
            int actionCount,
            string endReason,
            string rulesVersion,
            IEnumerable<CombatEvent> events,
            IEnumerable<CombatSnapshot> finalSnapshots)
        {
            BattleId = battleId ?? string.Empty;
            Seed = seed;
            Outcome = outcome;
            WinningTeam = winningTeam;
            TurnCount = turnCount;
            ActionCount = actionCount;
            EndReason = endReason ?? string.Empty;
            RulesVersion = rulesVersion ?? string.Empty;
            Events = new List<CombatEvent>(
                events ?? throw new ArgumentNullException(nameof(events))).AsReadOnly();
            FinalSnapshots = new List<CombatSnapshot>(
                finalSnapshots ?? throw new ArgumentNullException(nameof(finalSnapshots)))
                .AsReadOnly();
            DeterministicHash = DeterministicBattleHasher.Compute(this);
        }

        public string BattleId { get; }
        public long Seed { get; }
        public BattleOutcome Outcome { get; }
        public BattleSide? WinningTeam { get; }
        public int TurnCount { get; }
        public int ActionCount { get; }
        public string EndReason { get; }
        public string RulesVersion { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        public IReadOnlyList<CombatSnapshot> FinalSnapshots { get; }
        public string DeterministicHash { get; }
    }

    internal static class BattleUnitValidation
    {
        public static void Validate(BattleUnit unit)
        {
            ValidateValues(
                unit.UnitId, unit.DefinitionId, unit.Team, unit.Slot, unit.Level,
                unit.MaximumHealth, unit.CurrentHealth, unit.Attack, unit.Defense,
                unit.Speed, unit.CriticalChance, unit.CriticalMultiplier,
                unit.Accuracy, unit.Evasion, unit.Alive, unit.ActionGauge, unit.Tags,
                true);
        }

        public static void Validate(CombatSnapshot snapshot)
        {
            ValidateValues(
                snapshot.UnitId, snapshot.DefinitionId, snapshot.Team, snapshot.Slot,
                snapshot.Level, snapshot.MaximumHealth, snapshot.CurrentHealth,
                snapshot.Attack, snapshot.Defense, snapshot.Speed,
                snapshot.CriticalChance, snapshot.CriticalMultiplier,
                snapshot.Accuracy, snapshot.Evasion, snapshot.Alive,
                snapshot.ActionGauge, snapshot.Tags, false);
        }

        private static void ValidateValues(
            string unitId,
            string definitionId,
            BattleSide team,
            int slot,
            int level,
            long maximumHealth,
            long currentHealth,
            long attack,
            long defense,
            double speed,
            double criticalChance,
            double criticalMultiplier,
            double accuracy,
            double evasion,
            bool alive,
            double actionGauge,
            IReadOnlyList<string> tags,
            bool requireAlive)
        {
            if (string.IsNullOrWhiteSpace(unitId) || string.IsNullOrWhiteSpace(definitionId))
                throw new InvalidOperationException("IDs da unidade são obrigatórios.");
            if (!Enum.IsDefined(typeof(BattleSide), team))
                throw new InvalidOperationException("Lado da unidade inválido.");
            if (slot < 0 || level < 1)
                throw new InvalidOperationException("Slot ou nível inválido.");
            if (maximumHealth <= 0 || currentHealth < 0 || currentHealth > maximumHealth ||
                (requireAlive && currentHealth <= 0))
            {
                throw new InvalidOperationException("Vida da unidade inválida.");
            }
            if (attack <= 0 || defense < 0 || !BattleConfiguration.IsFinite(speed) ||
                speed <= 0d)
            {
                throw new InvalidOperationException("Atributos da unidade inválidos.");
            }
            RequireProbability(criticalChance, "criticalChance");
            if (!BattleConfiguration.IsFinite(criticalMultiplier) || criticalMultiplier < 1d)
                throw new InvalidOperationException("criticalMultiplier inválido.");
            RequireProbability(accuracy, "accuracy");
            RequireProbability(evasion, "evasion");
            if (alive != (currentHealth > 0))
                throw new InvalidOperationException("Estado alive inconsistente com a vida.");
            if (!BattleConfiguration.IsFinite(actionGauge) || actionGauge < 0d)
                throw new InvalidOperationException("Action gauge inválido.");
            if (tags == null)
                throw new InvalidOperationException("Tags não podem ser nulas.");
            var uniqueTags = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < tags.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(tags[i]) || !uniqueTags.Add(tags[i]))
                    throw new InvalidOperationException("Tag vazia ou duplicada.");
            }
        }

        private static void RequireProbability(double value, string name)
        {
            if (!BattleConfiguration.IsFinite(value) || value < 0d || value > 1d)
                throw new InvalidOperationException($"{name} inválido.");
        }
    }
}
