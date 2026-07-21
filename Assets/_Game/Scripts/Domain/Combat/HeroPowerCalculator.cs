using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Common;

namespace IdleMedievalLegends.Domain.Combat
{
    [Serializable]
    public struct HeroStatBlock
    {
        public long maxHealth;
        public long attack;
        public long defense;
        public float speed;

        public HeroStatBlock(long maxHealth, long attack, long defense, float speed)
        {
            this.maxHealth = maxHealth;
            this.attack = attack;
            this.defense = defense;
            this.speed = speed;
        }
    }

    [Serializable]
    public struct HeroStatBonuses
    {
        public long flatHealth;
        public long flatAttack;
        public long flatDefense;
        public float flatSpeed;

        // Percentuais em formato decimal: 0.15 = +15%.
        public float healthPercent;
        public float attackPercent;
        public float defensePercent;
        public float speedPercent;

        public static HeroStatBonuses Combine(HeroStatBonuses left, HeroStatBonuses right)
        {
            return new HeroStatBonuses
            {
                flatHealth = checked(left.flatHealth + right.flatHealth),
                flatAttack = checked(left.flatAttack + right.flatAttack),
                flatDefense = checked(left.flatDefense + right.flatDefense),
                flatSpeed = left.flatSpeed + right.flatSpeed,
                healthPercent = left.healthPercent + right.healthPercent,
                attackPercent = left.attackPercent + right.attackPercent,
                defensePercent = left.defensePercent + right.defensePercent,
                speedPercent = left.speedPercent + right.speedPercent
            };
        }
    }

    [Serializable]
    public sealed class HeroBuildData
    {
        public string heroInstanceId = string.Empty;
        public int level = 1;
        public GameRarity rarity = GameRarity.Common;
        public int ascension;
        public HeroStatBlock baseStats;
        public HeroStatBonuses equipmentBonuses;
        public HeroStatBonuses permanentBonuses;
    }

    /// <summary>
    /// Configuração versionada. Em produção, a mesma versão deve existir no
    /// servidor e no cliente; o servidor continua sendo a autoridade.
    /// </summary>
    [Serializable]
    public sealed class CombatBalanceTuning
    {
        public int version = 2;
        public int maxHeroLevel = 100;

        public double levelLinearCoefficient = 0.065d;
        public double levelQuadraticCoefficient = 0.00035d;

        public double defenseScaleAtLevelOne = 400d;
        public double maximumDamageReduction = 0.75d;

        public double speedBaseline = 100d;
        public double speedPowerExponent = 0.65d;
        public double minimumSpeedFactor = 0.75d;
        public double maximumSpeedFactor = 1.50d;
        public float minimumFinalSpeed = 60f;
        public float maximumFinalSpeed = 180f;

        public double powerDisplayScale = 3d;

        // Comum, Incomum, Raro, Épico, Lendário e Mítico.
        public double[] rarityMultipliers =
        {
            1.00d, 1.08d, 1.18d, 1.31d, 1.47d, 1.66d
        };
        public double[] ascensionMultipliers = { 1.00d, 1.08d, 1.18d, 1.30d, 1.44d, 1.60d };
    }

    /// <summary>
    /// Calculador puro: não depende de MonoBehaviour, cena ou estado global.
    /// Pode ser reutilizado em testes e em um backend C#.
    /// </summary>
    public static class HeroPowerCalculator
    {
        public static HeroStatBlock CalculateFinalStats(
            HeroBuildData hero,
            CombatBalanceTuning tuning)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));

            ValidateTuning(tuning);

            int level = Clamp(hero.level, 1, tuning.maxHeroLevel);
            if (!hero.rarity.IsValid())
                throw new InvalidOperationException("Raridade do herói é inválida.");

            int rarityIndex = (int)hero.rarity;
            int ascensionIndex = Clamp(hero.ascension, 0, tuning.ascensionMultipliers.Length - 1);

            double progressionMultiplier =
                CalculateLevelMultiplier(level, tuning) *
                tuning.rarityMultipliers[rarityIndex] *
                tuning.ascensionMultipliers[ascensionIndex];

            HeroStatBonuses bonuses = HeroStatBonuses.Combine(
                hero.equipmentBonuses,
                hero.permanentBonuses);

            long health = ResolvePrimaryStat(
                hero.baseStats.maxHealth,
                progressionMultiplier,
                bonuses.flatHealth,
                bonuses.healthPercent);

            long attack = ResolvePrimaryStat(
                hero.baseStats.attack,
                progressionMultiplier,
                bonuses.flatAttack,
                bonuses.attackPercent);

            long defense = ResolvePrimaryStat(
                hero.baseStats.defense,
                progressionMultiplier,
                bonuses.flatDefense,
                bonuses.defensePercent);

            // Velocidade não recebe o multiplicador de nível/raridade. Isso evita
            // crescimento explosivo da frequência de ações no late game.
            double rawSpeed =
                (hero.baseStats.speed + bonuses.flatSpeed) *
                (1d + bonuses.speedPercent);

            float speed = (float)Clamp(
                rawSpeed,
                tuning.minimumFinalSpeed,
                tuning.maximumFinalSpeed);

            return new HeroStatBlock(health, attack, defense, speed);
        }

        public static long CalculateHeroPower(
            HeroBuildData hero,
            CombatBalanceTuning tuning)
        {
            HeroStatBlock finalStats = CalculateFinalStats(hero, tuning);
            return CalculateHeroPower(finalStats, hero.level, tuning);
        }

        public static long CalculateHeroPower(
            HeroStatBlock finalStats,
            int level,
            CombatBalanceTuning tuning)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            ValidateTuning(tuning);

            if (finalStats.maxHealth <= 0 || finalStats.attack <= 0)
            {
                return 0;
            }

            int clampedLevel = Clamp(level, 1, tuning.maxHeroLevel);
            double damageReduction = CalculateDamageReduction(
                finalStats.defense,
                clampedLevel,
                tuning);

            double effectiveHealth = finalStats.maxHealth / (1d - damageReduction);

            double normalizedSpeed = finalStats.speed / tuning.speedBaseline;
            double speedFactor = Math.Pow(Math.Max(0.01d, normalizedSpeed), tuning.speedPowerExponent);
            speedFactor = Clamp(
                speedFactor,
                tuning.minimumSpeedFactor,
                tuning.maximumSpeedFactor);

            double offenseIndex = finalStats.attack * speedFactor;
            double rawPower = tuning.powerDisplayScale * Math.Sqrt(effectiveHealth * offenseIndex);

            if (double.IsNaN(rawPower) || rawPower <= 0d)
            {
                return 0;
            }

            if (rawPower >= long.MaxValue)
            {
                return long.MaxValue;
            }

            return (long)Math.Round(rawPower, MidpointRounding.AwayFromZero);
        }

        public static long CalculateAccountPower(
            IEnumerable<HeroBuildData> heroes,
            CombatBalanceTuning tuning)
        {
            if (heroes == null) throw new ArgumentNullException(nameof(heroes));

            long total = 0;
            foreach (HeroBuildData hero in heroes)
            {
                if (hero == null) continue;
                total = checked(total + CalculateHeroPower(hero, tuning));
            }

            return total;
        }

        public static double CalculateLevelMultiplier(int level, CombatBalanceTuning tuning)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));

            int clampedLevel = Clamp(level, 1, tuning.maxHeroLevel);
            double x = clampedLevel - 1d;

            return 1d +
                   tuning.levelLinearCoefficient * x +
                   tuning.levelQuadraticCoefficient * x * x;
        }

        public static double CalculateDamageReduction(
            long defense,
            int level,
            CombatBalanceTuning tuning)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (defense <= 0) return 0d;

            double defenseScale =
                tuning.defenseScaleAtLevelOne *
                CalculateLevelMultiplier(level, tuning);

            double reduction = defense / (defense + defenseScale);
            return Clamp(reduction, 0d, tuning.maximumDamageReduction);
        }

        private static long ResolvePrimaryStat(
            long baseValue,
            double progressionMultiplier,
            long flatBonus,
            double percentBonus)
        {
            if (baseValue < 0) throw new ArgumentOutOfRangeException(nameof(baseValue));
            if (percentBonus <= -1d) throw new ArgumentOutOfRangeException(nameof(percentBonus));

            double value =
                (baseValue * progressionMultiplier + flatBonus) *
                (1d + percentBonus);

            if (value <= 1d) return 1;
            if (value >= long.MaxValue) return long.MaxValue;

            return (long)Math.Floor(value);
        }

        private static void ValidateTuning(CombatBalanceTuning tuning)
        {
            if (tuning.maxHeroLevel < 1)
                throw new InvalidOperationException("maxHeroLevel deve ser maior que zero.");
            if (tuning.rarityMultipliers == null ||
                tuning.rarityMultipliers.Length != 6)
            {
                throw new InvalidOperationException(
                    "rarityMultipliers deve conter exatamente as seis raridades.");
            }
            if (tuning.ascensionMultipliers == null || tuning.ascensionMultipliers.Length == 0)
                throw new InvalidOperationException("ascensionMultipliers não pode estar vazio.");
            if (tuning.speedBaseline <= 0d)
                throw new InvalidOperationException("speedBaseline deve ser maior que zero.");
            if (tuning.maximumDamageReduction < 0d || tuning.maximumDamageReduction >= 1d)
                throw new InvalidOperationException("maximumDamageReduction deve estar entre 0 e 1.");
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
    }
}
