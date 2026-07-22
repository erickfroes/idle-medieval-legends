using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Content;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Combat
{
    [Serializable]
    public struct HeroStatBlock
    {
        // Os nomes preservam o contrato serializado da versão anterior.
        [SerializeField] private long maxHealth;
        [SerializeField] private long attack;
        [SerializeField] private long defense;
        [SerializeField] private double speed;

        public HeroStatBlock(long maxHealth, long attack, long defense, double speed)
        {
            this.maxHealth = maxHealth;
            this.attack = attack;
            this.defense = defense;
            this.speed = speed;
        }

        public long MaxHealth => maxHealth;
        public long Attack => attack;
        public long Defense => defense;
        public double Speed => speed;
    }

    [Serializable]
    public struct HeroStatModifiers
    {
        // Também mantém os nomes de HeroStatBonuses para migração de JSON/YAML.
        [SerializeField] private long flatHealth;
        [SerializeField] private long flatAttack;
        [SerializeField] private long flatDefense;
        [SerializeField] private double flatSpeed;
        [SerializeField] private double healthPercent;
        [SerializeField] private double attackPercent;
        [SerializeField] private double defensePercent;
        [SerializeField] private double speedPercent;

        public HeroStatModifiers(
            long flatHealth,
            double percentHealth,
            long flatAttack,
            double percentAttack,
            long flatDefense,
            double percentDefense,
            double flatSpeed,
            double percentSpeed)
        {
            this.flatHealth = flatHealth;
            healthPercent = percentHealth;
            this.flatAttack = flatAttack;
            attackPercent = percentAttack;
            this.flatDefense = flatDefense;
            defensePercent = percentDefense;
            this.flatSpeed = flatSpeed;
            speedPercent = percentSpeed;
        }

        public long FlatHealth => flatHealth;
        public double PercentHealth => healthPercent;
        public long FlatAttack => flatAttack;
        public double PercentAttack => attackPercent;
        public long FlatDefense => flatDefense;
        public double PercentDefense => defensePercent;
        public double FlatSpeed => flatSpeed;
        public double PercentSpeed => speedPercent;

        public static HeroStatModifiers None => default;

        public static HeroStatModifiers Combine(
            HeroStatModifiers left,
            HeroStatModifiers right)
        {
            return new HeroStatModifiers(
                checked(left.FlatHealth + right.FlatHealth),
                AddFinite(left.PercentHealth, right.PercentHealth, nameof(PercentHealth)),
                checked(left.FlatAttack + right.FlatAttack),
                AddFinite(left.PercentAttack, right.PercentAttack, nameof(PercentAttack)),
                checked(left.FlatDefense + right.FlatDefense),
                AddFinite(left.PercentDefense, right.PercentDefense, nameof(PercentDefense)),
                AddFinite(left.FlatSpeed, right.FlatSpeed, nameof(FlatSpeed)),
                AddFinite(left.PercentSpeed, right.PercentSpeed, nameof(PercentSpeed)));
        }

        public void Validate()
        {
            ValidateFinite(PercentHealth, nameof(PercentHealth));
            ValidateFinite(PercentAttack, nameof(PercentAttack));
            ValidateFinite(PercentDefense, nameof(PercentDefense));
            ValidateFinite(FlatSpeed, nameof(FlatSpeed));
            ValidateFinite(PercentSpeed, nameof(PercentSpeed));

            if (PercentHealth <= -1d || PercentAttack <= -1d ||
                PercentDefense <= -1d || PercentSpeed <= -1d)
            {
                throw new InvalidOperationException(
                    "Modificadores percentuais devem ser maiores que -100%.");
            }
        }

        private static double AddFinite(double left, double right, string fieldName)
        {
            double result = left + right;
            ValidateFinite(result, fieldName);
            return result;
        }

        private static void ValidateFinite(double value, string fieldName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException($"{fieldName} deve ser finito.");
        }
    }

    [Serializable]
    public sealed class HeroLevelProgressionOverride
    {
        public int level = 1;
        public long experienceRequired = 100;
        public long goldCost = 50;
    }

    /// <summary>
    /// Configuração versionada compartilhável com um backend C#. Valores de ouro
    /// no cliente são apenas previsão; o servidor continua sendo a autoridade.
    /// </summary>
    [Serializable]
    public sealed class CombatBalanceTuning
    {
        public int version = 3;
        public int maxHeroLevel = 100;
        public int maxAscensionLevel = 5;
        public Rarity minimumHeroRarity = Rarity.Common;
        public Rarity maximumHeroRarity = Rarity.Mythic;

        public double levelLinearCoefficient = 0.065d;
        public double levelQuadraticCoefficient = 0.00035d;

        public long baseExperiencePerLevel = 100;
        public double experienceExponent = 1.55d;
        public long baseGoldCostPerLevel = 50;
        public double goldCostExponent = 1.25d;
        public List<HeroLevelProgressionOverride> levelProgressionOverrides =
            new List<HeroLevelProgressionOverride>();

        // Common..Mythic. Valores de desbloqueio seguem as sementes do GDD.
        public long[] unlockFragmentsByRarity = { 20, 30, 50, 80, 120, 200 };

        // Índice 0 representa a ascensão de 0 para 1.
        public long[] ascensionFragmentCosts = { 20, 40, 80, 160, 320 };

        // Índice representa a raridade atual; Mythic não possui promoção.
        public long[] rarityPromotionFragmentCosts = { 30, 50, 80, 120, 200, 0 };

        public double defenseScaleAtLevelOne = 400d;
        public double maximumDamageReduction = 0.75d;

        public double speedBaseline = 100d;
        public double speedPowerExponent = 0.65d;
        public double minimumSpeedFactor = 0.75d;
        public double maximumSpeedFactor = 1.50d;
        public double minimumFinalSpeed = 60d;
        public double maximumFinalSpeed = 180d;

        public double powerDisplayScale = 3d;
        public int activeTeamSize = 5;
        public int competitiveReserveHeroCount = 5;
        public int competitiveReserveWeightBasisPoints = 1500;

        public double[] rarityMultipliers =
        {
            1.00d, 1.08d, 1.18d, 1.31d, 1.47d, 1.66d
        };

        public double[] ascensionMultipliers =
        {
            1.00d, 1.08d, 1.18d, 1.30d, 1.44d, 1.60d
        };
    }

    public static class CombatBalanceTuningMigration
    {
        public const int CurrentVersion = 3;

        public static void UpgradeToCurrent(CombatBalanceTuning tuning)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (tuning.version > CurrentVersion)
                throw new InvalidOperationException(
                    $"Versão de CombatBalanceTuning não suportada: {tuning.version}.");
            if (tuning.version >= CurrentVersion)
                return;

            var defaults = new CombatBalanceTuning();
            if (tuning.ascensionMultipliers == null ||
                tuning.ascensionMultipliers.Length == 0)
            {
                tuning.ascensionMultipliers =
                    (double[])defaults.ascensionMultipliers.Clone();
            }
            // A v2 aceitava qualquer tabela não vazia. Ela é a autoridade para
            // o máximo migrado; o campo maxAscensionLevel não existia na v2.
            tuning.maxAscensionLevel = tuning.ascensionMultipliers.Length - 1;
            tuning.minimumHeroRarity = Rarity.Common;
            tuning.maximumHeroRarity = Rarity.Mythic;
            if (tuning.baseExperiencePerLevel <= 0)
                tuning.baseExperiencePerLevel = defaults.baseExperiencePerLevel;
            if (tuning.experienceExponent <= 0d)
                tuning.experienceExponent = defaults.experienceExponent;
            if (tuning.baseGoldCostPerLevel <= 0)
                tuning.baseGoldCostPerLevel = defaults.baseGoldCostPerLevel;
            if (tuning.goldCostExponent <= 0d)
                tuning.goldCostExponent = defaults.goldCostExponent;
            if (tuning.levelProgressionOverrides == null)
            {
                tuning.levelProgressionOverrides =
                    new List<HeroLevelProgressionOverride>();
            }
            if (tuning.unlockFragmentsByRarity == null ||
                tuning.unlockFragmentsByRarity.Length != 6)
            {
                tuning.unlockFragmentsByRarity =
                    (long[])defaults.unlockFragmentsByRarity.Clone();
            }
            if (tuning.ascensionFragmentCosts == null ||
                tuning.ascensionFragmentCosts.Length != tuning.maxAscensionLevel)
            {
                tuning.ascensionFragmentCosts = CreateAscensionFragmentCosts(
                    tuning.maxAscensionLevel,
                    tuning.ascensionFragmentCosts,
                    defaults.ascensionFragmentCosts);
            }
            if (tuning.rarityPromotionFragmentCosts == null ||
                tuning.rarityPromotionFragmentCosts.Length != 6)
            {
                tuning.rarityPromotionFragmentCosts =
                    (long[])defaults.rarityPromotionFragmentCosts.Clone();
            }
            if (tuning.activeTeamSize <= 0)
                tuning.activeTeamSize = defaults.activeTeamSize;
            if (tuning.competitiveReserveHeroCount <= 0)
            {
                tuning.competitiveReserveHeroCount =
                    defaults.competitiveReserveHeroCount;
            }
            if (tuning.competitiveReserveWeightBasisPoints <= 0)
            {
                tuning.competitiveReserveWeightBasisPoints =
                    defaults.competitiveReserveWeightBasisPoints;
            }

            tuning.version = CurrentVersion;
        }

        private static long[] CreateAscensionFragmentCosts(
            int count,
            long[] existing,
            long[] defaults)
        {
            var result = new long[count];
            for (int i = 0; i < result.Length; i++)
            {
                if (existing != null && i < existing.Length && existing[i] > 0)
                {
                    result[i] = existing[i];
                }
                else if (i < defaults.Length && defaults[i] > 0)
                {
                    result[i] = defaults[i];
                }
                else
                {
                    long previous = i == 0 ? 1 : result[i - 1];
                    result[i] = previous > long.MaxValue / 2
                        ? long.MaxValue
                        : previous * 2;
                }
            }
            return result;
        }
    }

    public sealed class HeroPowerBreakdown
    {
        public HeroPowerBreakdown(
            HeroStatBlock baseStats,
            double levelMultiplier,
            double rarityMultiplier,
            double ascensionMultiplier,
            HeroStatModifiers permanentModifiers,
            HeroStatModifiers equipmentModifiers,
            HeroStatModifiers combinedModifiers,
            HeroStatBlock finalStats,
            double damageReduction,
            double effectiveHealth,
            double speedFactor,
            double offenseIndex,
            HeroPower heroPower)
        {
            BaseStats = baseStats;
            LevelMultiplier = levelMultiplier;
            RarityMultiplier = rarityMultiplier;
            AscensionMultiplier = ascensionMultiplier;
            PermanentModifiers = permanentModifiers;
            EquipmentModifiers = equipmentModifiers;
            CombinedModifiers = combinedModifiers;
            FinalStats = finalStats;
            DamageReduction = damageReduction;
            EffectiveHealth = effectiveHealth;
            SpeedFactor = speedFactor;
            OffenseIndex = offenseIndex;
            HeroPower = heroPower;
        }

        public HeroStatBlock BaseStats { get; }
        public double LevelMultiplier { get; }
        public double RarityMultiplier { get; }
        public double AscensionMultiplier { get; }
        public HeroStatModifiers PermanentModifiers { get; }
        public HeroStatModifiers EquipmentModifiers { get; }
        public HeroStatModifiers CombinedModifiers { get; }
        public HeroStatBlock FinalStats { get; }
        public double DamageReduction { get; }
        public double EffectiveHealth { get; }
        public double SpeedFactor { get; }
        public double OffenseIndex { get; }
        public HeroPower HeroPower { get; }
    }

    public readonly struct HeroPower : IEquatable<HeroPower>
    {
        public HeroPower(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public long Value { get; }
        public bool Equals(HeroPower other) => Value == other.Value;
        public override bool Equals(object obj) => obj is HeroPower other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct TeamPower
    {
        public TeamPower(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public long Value { get; }
    }

    public readonly struct AccountPower
    {
        public AccountPower(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public long Value { get; }
    }

    public readonly struct CompetitivePower
    {
        public CompetitivePower(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public long Value { get; }
    }
}
