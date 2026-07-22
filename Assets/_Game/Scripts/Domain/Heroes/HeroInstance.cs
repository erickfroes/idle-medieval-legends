using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Heroes
{
    [Serializable]
    public sealed class HeroPermanentModifier
    {
        [SerializeField] private string sourceId = string.Empty;
        [SerializeField] private long flatHealth;
        [SerializeField] private double percentHealth;
        [SerializeField] private long flatAttack;
        [SerializeField] private double percentAttack;
        [SerializeField] private long flatDefense;
        [SerializeField] private double percentDefense;
        [SerializeField] private double flatSpeed;
        [SerializeField] private double percentSpeed;

        public HeroPermanentModifier()
        {
        }

        public HeroPermanentModifier(string sourceId, HeroStatModifiers modifiers)
        {
            this.sourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
            flatHealth = modifiers.FlatHealth;
            percentHealth = modifiers.PercentHealth;
            flatAttack = modifiers.FlatAttack;
            percentAttack = modifiers.PercentAttack;
            flatDefense = modifiers.FlatDefense;
            percentDefense = modifiers.PercentDefense;
            flatSpeed = modifiers.FlatSpeed;
            percentSpeed = modifiers.PercentSpeed;
            Validate();
        }

        public string SourceId => sourceId;

        public HeroStatModifiers Modifiers => new HeroStatModifiers(
            flatHealth,
            percentHealth,
            flatAttack,
            percentAttack,
            flatDefense,
            percentDefense,
            flatSpeed,
            percentSpeed);

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new InvalidOperationException("Modificador permanente sem sourceId.");
            Modifiers.Validate();
        }
    }

    /// <summary>
    /// Snapshot imutável de uma instância de herói. Transições retornam uma nova
    /// instância; calculatedPower é somente cache e nunca participa das fórmulas.
    /// </summary>
    [Serializable]
    public sealed class HeroInstance
    {
        [SerializeField] private string instanceId = string.Empty;
        [SerializeField] private string definitionId = string.Empty;
        [SerializeField] private string ownerPlayerId = string.Empty;
        [SerializeField] private int level = 1;
        [SerializeField] private long experience;
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField] private int ascensionLevel;
        [SerializeField] private long ownedFragments;
        [SerializeField] private List<string> equippedItemInstanceIds = new List<string>();
        [SerializeField] private bool unlocked;
        [SerializeField] private long createdAtUnixMilliseconds;
        [SerializeField] private long serverVersion;
        [SerializeField] private List<HeroPermanentModifier> permanentModifiers =
            new List<HeroPermanentModifier>();
        [SerializeField] private long calculatedPower;

        private HeroInstance()
        {
        }

        private HeroInstance(
            string instanceId,
            string definitionId,
            string ownerPlayerId,
            int level,
            long experience,
            Rarity rarity,
            int ascensionLevel,
            long ownedFragments,
            IEnumerable<string> equippedItemInstanceIds,
            bool unlocked,
            long createdAtUnixMilliseconds,
            long serverVersion,
            IEnumerable<HeroPermanentModifier> permanentModifiers,
            long calculatedPower,
            CombatBalanceTuning tuning)
        {
            this.instanceId = instanceId;
            this.definitionId = definitionId;
            this.ownerPlayerId = ownerPlayerId ?? string.Empty;
            this.level = level;
            this.experience = experience;
            this.rarity = rarity;
            this.ascensionLevel = ascensionLevel;
            this.ownedFragments = ownedFragments;
            this.equippedItemInstanceIds = equippedItemInstanceIds == null
                ? new List<string>()
                : new List<string>(equippedItemInstanceIds);
            this.unlocked = unlocked;
            this.createdAtUnixMilliseconds = createdAtUnixMilliseconds;
            this.serverVersion = serverVersion;
            this.permanentModifiers = permanentModifiers == null
                ? new List<HeroPermanentModifier>()
                : new List<HeroPermanentModifier>(permanentModifiers);
            this.calculatedPower = calculatedPower;
            Validate(tuning);
        }

        public string InstanceId => instanceId;
        public string DefinitionId => definitionId;
        public string OwnerPlayerId => ownerPlayerId;
        public int Level => level;
        public long Experience => experience;
        public Rarity Rarity => rarity;
        public int AscensionLevel => ascensionLevel;
        public long OwnedFragments => ownedFragments;
        public IReadOnlyList<string> EquippedItemInstanceIds =>
            equippedItemInstanceIds.AsReadOnly();
        public bool Unlocked => unlocked;
        public long CreatedAtUnixMilliseconds => createdAtUnixMilliseconds;
        public long ServerVersion => serverVersion;
        public IReadOnlyList<HeroPermanentModifier> PermanentModifiers =>
            permanentModifiers.AsReadOnly();

        /// <summary>
        /// Cache informativo recebido/calculado. Métricas oficiais sempre recalculam
        /// a partir da definição, progressão, tuning e modificadores atuais.
        /// </summary>
        public long CalculatedPower => calculatedPower;

        public static HeroInstance CreateLocked(
            string instanceId,
            string definitionId,
            string ownerPlayerId,
            Rarity initialRarity,
            long createdAtUnixMilliseconds,
            CombatBalanceTuning tuning)
        {
            return new HeroInstance(
                instanceId, definitionId, ownerPlayerId, 1, 0, initialRarity, 0, 0,
                null, false, createdAtUnixMilliseconds, 0, null, 0, tuning);
        }

        public static HeroInstance Restore(
            string instanceId,
            string definitionId,
            string ownerPlayerId,
            int level,
            long experience,
            Rarity rarity,
            int ascensionLevel,
            long ownedFragments,
            IEnumerable<string> equippedItemInstanceIds,
            bool unlocked,
            long createdAtUnixMilliseconds,
            long serverVersion,
            IEnumerable<HeroPermanentModifier> permanentModifiers,
            long calculatedPower,
            CombatBalanceTuning tuning)
        {
            return new HeroInstance(
                instanceId, definitionId, ownerPlayerId, level, experience, rarity,
                ascensionLevel, ownedFragments, equippedItemInstanceIds, unlocked,
                createdAtUnixMilliseconds, serverVersion, permanentModifiers,
                calculatedPower, tuning);
        }

        public HeroStatModifiers GetPermanentModifiers()
        {
            HeroStatModifiers combined = HeroStatModifiers.None;
            for (int i = 0; i < permanentModifiers.Count; i++)
                combined = HeroStatModifiers.Combine(combined, permanentModifiers[i].Modifiers);
            return combined;
        }

        public void Validate(CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);

            if (string.IsNullOrWhiteSpace(instanceId))
                throw new InvalidOperationException("HeroInstance sem instanceId.");
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new InvalidOperationException($"HeroInstance {instanceId} sem definitionId.");
            if (level < 1 || level > tuning.maxHeroLevel)
                throw new InvalidOperationException($"HeroInstance {instanceId} com nível inválido.");
            if (experience < 0)
                throw new InvalidOperationException($"HeroInstance {instanceId} com XP negativo.");
            if (!Enum.IsDefined(typeof(Rarity), rarity) ||
                (int)rarity < (int)tuning.minimumHeroRarity ||
                (int)rarity > (int)tuning.maximumHeroRarity)
            {
                throw new InvalidOperationException($"HeroInstance {instanceId} com raridade inválida.");
            }
            if (ascensionLevel < 0 || ascensionLevel > tuning.maxAscensionLevel)
                throw new InvalidOperationException($"HeroInstance {instanceId} com ascensão inválida.");
            if (ownedFragments < 0)
                throw new InvalidOperationException($"HeroInstance {instanceId} com fragmentos negativos.");
            if (createdAtUnixMilliseconds < 0 || serverVersion < 0 || calculatedPower < 0)
                throw new InvalidOperationException(
                    $"HeroInstance {instanceId} possui timestamps, versão ou cache inválido.");
            if (!unlocked && (level != 1 || experience != 0 || ascensionLevel != 0))
                throw new InvalidOperationException(
                    $"HeroInstance bloqueado {instanceId} possui progressão aplicada.");

            var equippedIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < equippedItemInstanceIds.Count; i++)
            {
                string itemId = equippedItemInstanceIds[i];
                if (string.IsNullOrWhiteSpace(itemId) || !equippedIds.Add(itemId))
                    throw new InvalidOperationException(
                        $"HeroInstance {instanceId} possui equipamento vazio ou duplicado.");
            }

            var modifierIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < permanentModifiers.Count; i++)
            {
                HeroPermanentModifier modifier = permanentModifiers[i];
                if (modifier == null)
                    throw new InvalidOperationException(
                        $"HeroInstance {instanceId} possui modificador permanente nulo.");
                modifier.Validate();
                if (!modifierIds.Add(modifier.SourceId))
                    throw new InvalidOperationException(
                        $"HeroInstance {instanceId} possui sourceId de modificador duplicado.");
            }
        }

        internal HeroInstance Copy(
            CombatBalanceTuning tuning,
            int? nextLevel = null,
            long? nextExperience = null,
            Rarity? nextRarity = null,
            int? nextAscensionLevel = null,
            long? nextOwnedFragments = null,
            IEnumerable<string> nextEquippedItemInstanceIds = null,
            bool? nextUnlocked = null,
            IEnumerable<HeroPermanentModifier> nextPermanentModifiers = null,
            long? nextCalculatedPower = null)
        {
            return new HeroInstance(
                instanceId,
                definitionId,
                ownerPlayerId,
                nextLevel ?? level,
                nextExperience ?? experience,
                nextRarity ?? rarity,
                nextAscensionLevel ?? ascensionLevel,
                nextOwnedFragments ?? ownedFragments,
                nextEquippedItemInstanceIds ?? equippedItemInstanceIds,
                nextUnlocked ?? unlocked,
                createdAtUnixMilliseconds,
                serverVersion,
                nextPermanentModifiers ?? permanentModifiers,
                nextCalculatedPower ?? calculatedPower,
                tuning);
        }
    }
}
