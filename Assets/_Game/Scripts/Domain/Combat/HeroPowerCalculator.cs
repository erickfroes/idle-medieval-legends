using System;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Combat
{
    /// <summary>
    /// DTO conveniente para simulações sem instância persistida. O fluxo normal
    /// usa HeroInstance + HeroDefinition.
    /// </summary>
    [Serializable]
    public sealed class HeroBuildData
    {
        public int level = 1;
        public Rarity rarity = Rarity.Common;
        public HeroStatBlock baseStats;

        // Backing fields mantêm compatibilidade com JSON/YAML da versão anterior.
        [SerializeField] private int ascension;
        [SerializeField] private HeroStatModifiers equipmentBonuses;
        [SerializeField] private HeroStatModifiers permanentBonuses;

        public int ascensionLevel
        {
            get => ascension;
            set => ascension = value;
        }

        public HeroStatModifiers equipmentModifiers
        {
            get => equipmentBonuses;
            set => equipmentBonuses = value;
        }

        public HeroStatModifiers permanentModifiers
        {
            get => permanentBonuses;
            set => permanentBonuses = value;
        }
    }

    public interface IHeroEquipmentModifierProvider
    {
        HeroStatModifiers GetModifiers(HeroInstance hero);
    }

    public sealed class EmptyHeroEquipmentModifierProvider : IHeroEquipmentModifierProvider
    {
        public static EmptyHeroEquipmentModifierProvider Instance { get; } =
            new EmptyHeroEquipmentModifierProvider();

        private EmptyHeroEquipmentModifierProvider()
        {
        }

        public HeroStatModifiers GetModifiers(HeroInstance hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            return HeroStatModifiers.None;
        }
    }

    /// <summary>
    /// Calculador puro e determinístico. Não usa cena, relógio, estado global nem
    /// o cache CalculatedPower da instância.
    /// </summary>
    public static class HeroPowerCalculator
    {
        public static HeroPowerBreakdown CalculateBreakdown(
            HeroInstance hero,
            HeroDefinition definition,
            IHeroEquipmentModifierProvider equipmentModifierProvider,
            CombatBalanceTuning tuning)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (equipmentModifierProvider == null)
                throw new ArgumentNullException(nameof(equipmentModifierProvider));
            hero.Validate(tuning);
            ValidateDefinitionCompatibility(hero, definition, tuning);

            HeroStatModifiers equipmentModifiers =
                equipmentModifierProvider.GetModifiers(hero);
            return CalculateBreakdown(
                hero,
                definition,
                equipmentModifiers,
                hero.GetPermanentModifiers(),
                tuning);
        }

        public static HeroPowerBreakdown CalculateBreakdown(
            HeroInstance hero,
            HeroDefinition definition,
            HeroStatModifiers equipmentModifiers,
            HeroStatModifiers permanentModifiers,
            CombatBalanceTuning tuning)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            hero.Validate(tuning);
            equipmentModifiers.Validate();
            permanentModifiers.Validate();
            ValidateDefinitionCompatibility(hero, definition, tuning);

            var baseStats = new HeroStatBlock(
                definition.BaseHealth,
                definition.BaseAttack,
                definition.BaseDefense,
                definition.BaseSpeed);
            ValidateBaseStats(baseStats);

            double levelMultiplier = CalculateLevelMultiplier(hero.Level, tuning);
            double rarityMultiplier = GetRarityMultiplier(hero.Rarity, tuning);
            double ascensionMultiplier = GetAscensionMultiplier(
                hero.AscensionLevel,
                tuning);
            double progressionMultiplier =
                levelMultiplier * rarityMultiplier * ascensionMultiplier;

            HeroStatModifiers combinedModifiers = HeroStatModifiers.Combine(
                equipmentModifiers,
                permanentModifiers);
            HeroStatBlock finalStats = CalculateFinalStats(
                baseStats,
                progressionMultiplier,
                combinedModifiers,
                tuning);

            double damageReduction = CalculateDamageReduction(
                finalStats.Defense,
                hero.Level,
                tuning);
            double effectiveHealth = finalStats.MaxHealth / (1d - damageReduction);
            double speedFactor = CalculateSpeedFactor(finalStats.Speed, tuning);
            double offenseIndex = finalStats.Attack * speedFactor;
            HeroPower power = CalculateHeroPower(
                effectiveHealth,
                offenseIndex,
                tuning);

            return new HeroPowerBreakdown(
                baseStats,
                levelMultiplier,
                rarityMultiplier,
                ascensionMultiplier,
                permanentModifiers,
                equipmentModifiers,
                combinedModifiers,
                finalStats,
                damageReduction,
                effectiveHealth,
                speedFactor,
                offenseIndex,
                power);
        }

        public static HeroInstance RefreshCalculatedPowerCache(
            HeroInstance hero,
            HeroDefinition definition,
            IHeroEquipmentModifierProvider equipmentModifierProvider,
            CombatBalanceTuning tuning)
        {
            HeroPowerBreakdown breakdown = CalculateBreakdown(
                hero,
                definition,
                equipmentModifierProvider,
                tuning);
            return hero.Copy(tuning, nextCalculatedPower: breakdown.HeroPower.Value);
        }

        public static HeroStatBlock CalculateFinalStats(
            HeroBuildData hero,
            CombatBalanceTuning tuning)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            HeroBalanceTuningValidator.Validate(tuning);
            ValidateLevel(hero.level, tuning);
            ValidateRarity(hero.rarity, tuning);
            ValidateAscension(hero.ascensionLevel, tuning);
            ValidateBaseStats(hero.baseStats);
            hero.equipmentModifiers.Validate();
            hero.permanentModifiers.Validate();

            double progressionMultiplier =
                CalculateLevelMultiplier(hero.level, tuning) *
                GetRarityMultiplier(hero.rarity, tuning) *
                GetAscensionMultiplier(hero.ascensionLevel, tuning);
            HeroStatModifiers modifiers = HeroStatModifiers.Combine(
                hero.equipmentModifiers,
                hero.permanentModifiers);
            return CalculateFinalStats(
                hero.baseStats,
                progressionMultiplier,
                modifiers,
                tuning);
        }

        public static long CalculateHeroPower(
            HeroBuildData hero,
            CombatBalanceTuning tuning)
        {
            HeroStatBlock finalStats = CalculateFinalStats(hero, tuning);
            double reduction = CalculateDamageReduction(finalStats.Defense, hero.level, tuning);
            double effectiveHealth = finalStats.MaxHealth / (1d - reduction);
            double offenseIndex = finalStats.Attack * CalculateSpeedFactor(
                finalStats.Speed,
                tuning);
            return CalculateHeroPower(effectiveHealth, offenseIndex, tuning).Value;
        }

        public static double CalculateLevelMultiplier(
            int level,
            CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);
            ValidateLevel(level, tuning);
            double x = level - 1d;
            return 1d +
                   tuning.levelLinearCoefficient * x +
                   tuning.levelQuadraticCoefficient * x * x;
        }

        public static double GetRarityMultiplier(
            Rarity rarity,
            CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);
            ValidateRarity(rarity, tuning);
            return tuning.rarityMultipliers[(int)rarity];
        }

        public static double GetAscensionMultiplier(
            int ascensionLevel,
            CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);
            ValidateAscension(ascensionLevel, tuning);
            return tuning.ascensionMultipliers[ascensionLevel];
        }

        public static double CalculateDamageReduction(
            long defense,
            int level,
            CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);
            ValidateLevel(level, tuning);
            if (defense <= 0)
                return 0d;

            double defenseScale =
                tuning.defenseScaleAtLevelOne * CalculateLevelMultiplier(level, tuning);
            double reduction = defense / (defense + defenseScale);
            return Clamp(reduction, 0d, tuning.maximumDamageReduction);
        }

        public static double CalculateSpeedFactor(
            double speed,
            CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);
            if (double.IsNaN(speed) || double.IsInfinity(speed) || speed < 0d)
                throw new ArgumentOutOfRangeException(nameof(speed));

            double normalizedSpeed = speed / tuning.speedBaseline;
            double factor = Math.Pow(
                Math.Max(0.01d, normalizedSpeed),
                tuning.speedPowerExponent);
            return Clamp(factor, tuning.minimumSpeedFactor, tuning.maximumSpeedFactor);
        }

        private static HeroStatBlock CalculateFinalStats(
            HeroStatBlock baseStats,
            double progressionMultiplier,
            HeroStatModifiers modifiers,
            CombatBalanceTuning tuning)
        {
            long health = ResolvePrimaryStat(
                baseStats.MaxHealth,
                progressionMultiplier,
                modifiers.FlatHealth,
                modifiers.PercentHealth,
                "Vida",
                1);
            long attack = ResolvePrimaryStat(
                baseStats.Attack,
                progressionMultiplier,
                modifiers.FlatAttack,
                modifiers.PercentAttack,
                "Ataque",
                1);
            long defense = ResolvePrimaryStat(
                baseStats.Defense,
                progressionMultiplier,
                modifiers.FlatDefense,
                modifiers.PercentDefense,
                "Defesa",
                0);

            double rawSpeed =
                (baseStats.Speed + modifiers.FlatSpeed) *
                (1d + modifiers.PercentSpeed);
            if (double.IsNaN(rawSpeed) || double.IsInfinity(rawSpeed))
                throw new OverflowException("Velocidade final não é finita.");
            double speed = Clamp(
                rawSpeed,
                tuning.minimumFinalSpeed,
                tuning.maximumFinalSpeed);
            return new HeroStatBlock(health, attack, defense, speed);
        }

        private static long ResolvePrimaryStat(
            long baseValue,
            double progressionMultiplier,
            long flatBonus,
            double percentBonus,
            string statName,
            long minimumValue)
        {
            double value =
                (baseValue * progressionMultiplier + flatBonus) *
                (1d + percentBonus);
            if (double.IsNaN(value) || double.IsInfinity(value) || value > long.MaxValue)
                throw new OverflowException($"{statName} final excedeu Int64.");
            if (value < minimumValue)
            {
                throw new InvalidOperationException(
                    $"{statName} final deve ser maior ou igual a {minimumValue}.");
            }
            return checked((long)Math.Floor(value));
        }

        private static HeroPower CalculateHeroPower(
            double effectiveHealth,
            double offenseIndex,
            CombatBalanceTuning tuning)
        {
            double rawPower =
                tuning.powerDisplayScale * Math.Sqrt(effectiveHealth * offenseIndex);
            if (double.IsNaN(rawPower) || double.IsInfinity(rawPower) ||
                rawPower > long.MaxValue)
            {
                throw new OverflowException("Poder do herói excedeu Int64.");
            }
            if (rawPower < 0d)
                throw new InvalidOperationException("Poder do herói não pode ser negativo.");

            return new HeroPower(checked((long)Math.Round(
                rawPower,
                MidpointRounding.AwayFromZero)));
        }

        private static void ValidateBaseStats(HeroStatBlock stats)
        {
            if (stats.MaxHealth <= 0 || stats.Attack <= 0 || stats.Defense < 0 ||
                stats.Speed <= 0d || double.IsNaN(stats.Speed) ||
                double.IsInfinity(stats.Speed))
            {
                throw new InvalidOperationException("Atributos-base do herói são inválidos.");
            }
        }

        private static void ValidateLevel(int level, CombatBalanceTuning tuning)
        {
            if (level < 1 || level > tuning.maxHeroLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
        }

        private static void ValidateRarity(Rarity rarity, CombatBalanceTuning tuning)
        {
            if (!Enum.IsDefined(typeof(Rarity), rarity) ||
                (int)rarity < (int)tuning.minimumHeroRarity ||
                (int)rarity > (int)tuning.maximumHeroRarity)
            {
                throw new ArgumentOutOfRangeException(nameof(rarity));
            }
        }

        private static void ValidateDefinitionCompatibility(
            HeroInstance hero,
            HeroDefinition definition,
            CombatBalanceTuning tuning)
        {
            if (!string.Equals(
                hero.DefinitionId,
                definition.DefinitionId,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "HeroInstance referencia uma HeroDefinition diferente.");
            }

            ValidateRarity(definition.InitialRarity, tuning);
            if ((int)hero.Rarity < (int)definition.InitialRarity)
            {
                throw new InvalidOperationException(
                    $"HeroInstance {hero.InstanceId} possui raridade abaixo da inicial " +
                    $"da definição {definition.DefinitionId}.");
            }
        }

        private static void ValidateAscension(
            int ascensionLevel,
            CombatBalanceTuning tuning)
        {
            if (ascensionLevel < 0 || ascensionLevel > tuning.maxAscensionLevel)
                throw new ArgumentOutOfRangeException(nameof(ascensionLevel));
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum) return minimum;
            return value > maximum ? maximum : value;
        }
    }
}
