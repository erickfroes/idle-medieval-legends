using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;

namespace IdleMedievalLegends.Domain.Heroes
{
    public sealed class HeroLevelUpResult
    {
        public HeroLevelUpResult(HeroInstance hero, long goldCost)
        {
            Hero = hero ?? throw new ArgumentNullException(nameof(hero));
            GoldCost = goldCost;
        }

        public HeroInstance Hero { get; }
        public long GoldCost { get; }
    }

    public static class HeroProgressionRules
    {
        public static long GetExperienceRequiredForNextLevel(
            int currentLevel,
            CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);
            ValidateCurrentLevel(currentLevel, tuning);
            if (currentLevel == tuning.maxHeroLevel)
                return 0;

            HeroLevelProgressionOverride progressionOverride =
                FindOverride(currentLevel, tuning);
            if (progressionOverride != null)
                return progressionOverride.experienceRequired;

            return CalculateCurveValue(
                tuning.baseExperiencePerLevel,
                currentLevel,
                tuning.experienceExponent,
                "XP");
        }

        public static long GetGoldCostForNextLevel(
            int currentLevel,
            CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);
            ValidateCurrentLevel(currentLevel, tuning);
            if (currentLevel == tuning.maxHeroLevel)
                return 0;

            HeroLevelProgressionOverride progressionOverride =
                FindOverride(currentLevel, tuning);
            if (progressionOverride != null)
                return progressionOverride.goldCost;

            return CalculateCurveValue(
                tuning.baseGoldCostPerLevel,
                currentLevel,
                tuning.goldCostExponent,
                "ouro");
        }

        public static long GetUnlockFragmentCost(
            Rarity rarity,
            CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);
            ValidateRarity(rarity, tuning);
            return tuning.unlockFragmentsByRarity[(int)rarity];
        }

        public static long GetAscensionFragmentCost(
            int currentAscensionLevel,
            CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);
            if (currentAscensionLevel < 0 ||
                currentAscensionLevel >= tuning.maxAscensionLevel)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentAscensionLevel),
                    "Ascensão atual não possui próximo nível.");
            }

            return tuning.ascensionFragmentCosts[currentAscensionLevel];
        }

        public static long GetRarityPromotionFragmentCost(
            Rarity currentRarity,
            CombatBalanceTuning tuning)
        {
            HeroBalanceTuningValidator.Validate(tuning);
            ValidateRarity(currentRarity, tuning);
            if ((int)currentRarity >= (int)tuning.maximumHeroRarity)
                throw new InvalidOperationException("Herói já está na raridade máxima.");

            return tuning.rarityPromotionFragmentCosts[(int)currentRarity];
        }

        public static HeroInstance AddExperience(
            HeroInstance hero,
            long amount,
            CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            if (!hero.Unlocked)
                throw new InvalidOperationException("Herói bloqueado não recebe XP.");
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "XP não pode ser negativo.");
            if (hero.Level >= tuning.maxHeroLevel && amount > 0)
                throw new InvalidOperationException("Herói já está no nível máximo.");

            return hero.Copy(
                tuning,
                nextExperience: checked(hero.Experience + amount));
        }

        public static bool CanLevelUp(HeroInstance hero, CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            return hero.Unlocked &&
                   hero.Level < tuning.maxHeroLevel &&
                   hero.Experience >= GetExperienceRequiredForNextLevel(hero.Level, tuning);
        }

        public static HeroLevelUpResult LevelUp(
            HeroInstance hero,
            long availableGold,
            CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            if (availableGold < 0)
                throw new ArgumentOutOfRangeException(nameof(availableGold));
            if (!hero.Unlocked)
                throw new InvalidOperationException("Herói bloqueado não pode subir de nível.");
            if (hero.Level >= tuning.maxHeroLevel)
                throw new InvalidOperationException("Herói já está no nível máximo.");

            long experienceCost = GetExperienceRequiredForNextLevel(hero.Level, tuning);
            long goldCost = GetGoldCostForNextLevel(hero.Level, tuning);
            if (hero.Experience < experienceCost)
                throw new InvalidOperationException("XP insuficiente para subir de nível.");
            if (availableGold < goldCost)
                throw new InvalidOperationException("Ouro insuficiente para subir de nível.");

            HeroInstance updated = hero.Copy(
                tuning,
                nextLevel: checked(hero.Level + 1),
                nextExperience: checked(hero.Experience - experienceCost),
                nextCalculatedPower: 0);
            return new HeroLevelUpResult(updated, goldCost);
        }

        public static HeroInstance AddFragments(
            HeroInstance hero,
            long amount,
            CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            if (amount < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(amount), "Fragmentos não podem ser negativos.");

            return hero.Copy(
                tuning,
                nextOwnedFragments: checked(hero.OwnedFragments + amount));
        }

        public static HeroInstance Unlock(
            HeroInstance hero,
            CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            if (hero.Unlocked)
                throw new InvalidOperationException("Herói já está desbloqueado.");

            long cost = GetUnlockFragmentCost(hero.Rarity, tuning);
            if (hero.OwnedFragments < cost)
                throw new InvalidOperationException("Fragmentos insuficientes para desbloqueio.");

            return hero.Copy(
                tuning,
                nextOwnedFragments: checked(hero.OwnedFragments - cost),
                nextUnlocked: true,
                nextCalculatedPower: 0);
        }

        public static bool CanAscend(HeroInstance hero, CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            if (!hero.Unlocked || hero.AscensionLevel >= tuning.maxAscensionLevel)
                return false;

            return hero.OwnedFragments >=
                   GetAscensionFragmentCost(hero.AscensionLevel, tuning);
        }

        public static HeroInstance Ascend(
            HeroInstance hero,
            CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            if (!hero.Unlocked)
                throw new InvalidOperationException("Herói bloqueado não pode ascender.");
            if (hero.AscensionLevel >= tuning.maxAscensionLevel)
                throw new InvalidOperationException("Herói já está na ascensão máxima.");

            long cost = GetAscensionFragmentCost(hero.AscensionLevel, tuning);
            if (hero.OwnedFragments < cost)
                throw new InvalidOperationException("Fragmentos insuficientes para ascensão.");

            return hero.Copy(
                tuning,
                nextAscensionLevel: checked(hero.AscensionLevel + 1),
                nextOwnedFragments: checked(hero.OwnedFragments - cost),
                nextCalculatedPower: 0);
        }

        public static HeroInstance PromoteRarity(
            HeroInstance hero,
            CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            if (!hero.Unlocked)
                throw new InvalidOperationException("Herói bloqueado não pode ser promovido.");
            if ((int)hero.Rarity >= (int)tuning.maximumHeroRarity)
                throw new InvalidOperationException("Herói já está na raridade máxima.");

            long cost = GetRarityPromotionFragmentCost(hero.Rarity, tuning);
            if (cost <= 0 || hero.OwnedFragments < cost)
                throw new InvalidOperationException("Requisitos de promoção não atendidos.");

            Rarity promoted = (Rarity)checked((int)hero.Rarity + 1);
            ValidateRarity(promoted, tuning);
            return hero.Copy(
                tuning,
                nextRarity: promoted,
                nextOwnedFragments: checked(hero.OwnedFragments - cost),
                nextCalculatedPower: 0);
        }

        public static HeroInstance EquipItemReference(
            HeroInstance hero,
            string itemInstanceId,
            CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            if (!hero.Unlocked)
                throw new InvalidOperationException("Herói bloqueado não pode equipar itens.");
            if (string.IsNullOrWhiteSpace(itemInstanceId))
                throw new ArgumentException("itemInstanceId é obrigatório.", nameof(itemInstanceId));

            var ids = new List<string>(hero.EquippedItemInstanceIds);
            if (ids.Contains(itemInstanceId))
                throw new InvalidOperationException("Referência de equipamento duplicada.");
            ids.Add(itemInstanceId);
            return hero.Copy(
                tuning,
                nextEquippedItemInstanceIds: ids,
                nextCalculatedPower: 0);
        }

        public static HeroInstance UnequipItemReference(
            HeroInstance hero,
            string itemInstanceId,
            CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            if (string.IsNullOrWhiteSpace(itemInstanceId))
                throw new ArgumentException("itemInstanceId é obrigatório.", nameof(itemInstanceId));

            var ids = new List<string>(hero.EquippedItemInstanceIds);
            if (!ids.Remove(itemInstanceId))
                throw new InvalidOperationException("Referência de equipamento não encontrada.");
            return hero.Copy(
                tuning,
                nextEquippedItemInstanceIds: ids,
                nextCalculatedPower: 0);
        }

        public static HeroInstance ReplacePermanentModifiers(
            HeroInstance hero,
            IEnumerable<HeroPermanentModifier> modifiers,
            CombatBalanceTuning tuning)
        {
            ValidateHero(hero, tuning);
            if (modifiers == null) throw new ArgumentNullException(nameof(modifiers));
            return hero.Copy(
                tuning,
                nextPermanentModifiers: new List<HeroPermanentModifier>(modifiers),
                nextCalculatedPower: 0);
        }

        public static void ValidateUniqueInstanceIds(IEnumerable<HeroInstance> heroes)
        {
            if (heroes == null) throw new ArgumentNullException(nameof(heroes));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (HeroInstance hero in heroes)
            {
                if (hero == null)
                    throw new InvalidOperationException("Coleção possui HeroInstance nulo.");
                if (!ids.Add(hero.InstanceId))
                    throw new InvalidOperationException(
                        $"instanceId de herói duplicado: {hero.InstanceId}.");
            }
        }

        private static HeroLevelProgressionOverride FindOverride(
            int level,
            CombatBalanceTuning tuning)
        {
            for (int i = 0; i < tuning.levelProgressionOverrides.Count; i++)
            {
                if (tuning.levelProgressionOverrides[i].level == level)
                    return tuning.levelProgressionOverrides[i];
            }
            return null;
        }

        private static long CalculateCurveValue(
            long baseValue,
            int level,
            double exponent,
            string valueName)
        {
            double value = baseValue * Math.Pow(level, exponent);
            if (double.IsNaN(value) || double.IsInfinity(value) || value > long.MaxValue)
                throw new OverflowException($"Curva de {valueName} excedeu Int64.");
            return checked((long)Math.Round(value, MidpointRounding.AwayFromZero));
        }

        private static void ValidateCurrentLevel(int level, CombatBalanceTuning tuning)
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

        private static void ValidateHero(HeroInstance hero, CombatBalanceTuning tuning)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            hero.Validate(tuning);
        }
    }

    public static class HeroBalanceTuningValidator
    {
        public static void Validate(CombatBalanceTuning tuning)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (tuning.version != CombatBalanceTuningMigration.CurrentVersion)
            {
                throw new InvalidOperationException(
                    $"Versão de CombatBalanceTuning não suportada: {tuning.version}.");
            }
            if (tuning.maxHeroLevel < 1)
                throw new InvalidOperationException("maxHeroLevel deve ser maior que zero.");
            if (tuning.maxAscensionLevel < 0)
                throw new InvalidOperationException("maxAscensionLevel não pode ser negativo.");
            if (!Enum.IsDefined(typeof(Rarity), tuning.minimumHeroRarity) ||
                !Enum.IsDefined(typeof(Rarity), tuning.maximumHeroRarity) ||
                (int)tuning.minimumHeroRarity > (int)tuning.maximumHeroRarity)
            {
                throw new InvalidOperationException("Limites de raridade são inválidos.");
            }

            RequireFiniteNonNegative(tuning.levelLinearCoefficient, "levelLinearCoefficient");
            RequireFiniteNonNegative(
                tuning.levelQuadraticCoefficient,
                "levelQuadraticCoefficient");
            if (tuning.baseExperiencePerLevel <= 0 || tuning.experienceExponent <= 0d ||
                tuning.baseGoldCostPerLevel < 0 || tuning.goldCostExponent <= 0d)
            {
                throw new InvalidOperationException("Curvas de XP/ouro são inválidas.");
            }
            RequireFinite(tuning.experienceExponent, "experienceExponent");
            RequireFinite(tuning.goldCostExponent, "goldCostExponent");

            RequireArray(tuning.rarityMultipliers, 6, "rarityMultipliers", true);
            RequireArray(tuning.unlockFragmentsByRarity, 6, "unlockFragmentsByRarity", true);
            RequireArray(
                tuning.rarityPromotionFragmentCosts,
                6,
                "rarityPromotionFragmentCosts",
                false);
            for (int i = (int)tuning.minimumHeroRarity;
                 i < (int)tuning.maximumHeroRarity;
                 i++)
            {
                if (tuning.rarityPromotionFragmentCosts[i] <= 0)
                {
                    throw new InvalidOperationException(
                        "Toda promoção permitida deve possuir custo de fragmentos.");
                }
            }
            RequireArray(
                tuning.ascensionMultipliers,
                tuning.maxAscensionLevel + 1,
                "ascensionMultipliers",
                true);
            RequireArray(
                tuning.ascensionFragmentCosts,
                tuning.maxAscensionLevel,
                "ascensionFragmentCosts",
                true);

            if (tuning.levelProgressionOverrides == null)
                throw new InvalidOperationException("levelProgressionOverrides não pode ser nulo.");
            var overrideLevels = new HashSet<int>();
            for (int i = 0; i < tuning.levelProgressionOverrides.Count; i++)
            {
                HeroLevelProgressionOverride value = tuning.levelProgressionOverrides[i];
                if (value == null || value.level < 1 || value.level >= tuning.maxHeroLevel ||
                    value.experienceRequired <= 0 || value.goldCost < 0 ||
                    !overrideLevels.Add(value.level))
                {
                    throw new InvalidOperationException(
                        "Override de progressão nulo, duplicado ou inválido.");
                }
            }

            RequireFinite(tuning.defenseScaleAtLevelOne, "defenseScaleAtLevelOne");
            RequireFinite(tuning.maximumDamageReduction, "maximumDamageReduction");
            if (tuning.defenseScaleAtLevelOne <= 0d ||
                tuning.maximumDamageReduction < 0d ||
                tuning.maximumDamageReduction >= 1d)
            {
                throw new InvalidOperationException("Configuração de Defesa inválida.");
            }
            RequireFinite(tuning.speedBaseline, "speedBaseline");
            RequireFinite(tuning.speedPowerExponent, "speedPowerExponent");
            RequireFinite(tuning.minimumSpeedFactor, "minimumSpeedFactor");
            RequireFinite(tuning.maximumSpeedFactor, "maximumSpeedFactor");
            RequireFinite(tuning.minimumFinalSpeed, "minimumFinalSpeed");
            RequireFinite(tuning.maximumFinalSpeed, "maximumFinalSpeed");
            if (tuning.speedBaseline <= 0d || tuning.speedPowerExponent <= 0d ||
                tuning.minimumSpeedFactor <= 0d ||
                tuning.maximumSpeedFactor < tuning.minimumSpeedFactor ||
                tuning.minimumFinalSpeed <= 0d ||
                tuning.maximumFinalSpeed < tuning.minimumFinalSpeed)
            {
                throw new InvalidOperationException("Configuração de Velocidade inválida.");
            }
            RequireFinite(tuning.powerDisplayScale, "powerDisplayScale");
            if (tuning.powerDisplayScale <= 0d || tuning.activeTeamSize < 1 ||
                tuning.competitiveReserveHeroCount < 0 ||
                tuning.competitiveReserveWeightBasisPoints < 0 ||
                tuning.competitiveReserveWeightBasisPoints > 10000)
            {
                throw new InvalidOperationException("Configuração de Poder inválida.");
            }
        }

        private static void RequireArray(
            double[] values,
            int expectedCount,
            string name,
            bool requirePositive)
        {
            if (values == null || values.Length != expectedCount)
                throw new InvalidOperationException($"{name} deve conter {expectedCount} valores.");
            for (int i = 0; i < values.Length; i++)
            {
                RequireFinite(values[i], name);
                if ((requirePositive && values[i] <= 0d) || (!requirePositive && values[i] < 0d))
                    throw new InvalidOperationException($"{name} possui valor inválido.");
            }
        }

        private static void RequireArray(
            long[] values,
            int expectedCount,
            string name,
            bool requirePositive)
        {
            if (values == null || values.Length != expectedCount)
                throw new InvalidOperationException($"{name} deve conter {expectedCount} valores.");
            for (int i = 0; i < values.Length; i++)
            {
                if ((requirePositive && values[i] <= 0) || (!requirePositive && values[i] < 0))
                    throw new InvalidOperationException($"{name} possui valor inválido.");
            }
        }

        private static void RequireFiniteNonNegative(double value, string name)
        {
            RequireFinite(value, name);
            if (value < 0d) throw new InvalidOperationException($"{name} não pode ser negativo.");
        }

        private static void RequireFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException($"{name} deve ser finito.");
        }
    }
}
