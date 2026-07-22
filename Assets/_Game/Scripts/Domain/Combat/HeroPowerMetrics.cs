using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;

namespace IdleMedievalLegends.Domain.Combat
{
    public sealed class HeroPowerMetricEntry
    {
        public HeroPowerMetricEntry(
            string instanceId,
            HeroPower power,
            bool unlocked,
            bool activeTeamMember)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                throw new ArgumentException("instanceId é obrigatório.", nameof(instanceId));
            InstanceId = instanceId;
            Power = power;
            Unlocked = unlocked;
            ActiveTeamMember = activeTeamMember;
        }

        public string InstanceId { get; }
        public HeroPower Power { get; }
        public bool Unlocked { get; }
        public bool ActiveTeamMember { get; }
    }

    public sealed class HeroPowerService
    {
        private readonly ContentCatalogLookup catalog;
        private readonly IHeroEquipmentModifierProvider equipmentModifierProvider;
        private readonly CombatBalanceTuning tuning;

        public HeroPowerService(
            ContentCatalogLookup catalog,
            IHeroEquipmentModifierProvider equipmentModifierProvider,
            CombatBalanceTuning tuning)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.equipmentModifierProvider = equipmentModifierProvider ??
                throw new ArgumentNullException(nameof(equipmentModifierProvider));
            HeroBalanceTuningValidator.Validate(tuning);
            this.tuning = tuning;
        }

        public HeroPowerBreakdown Evaluate(HeroInstance hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            HeroDefinition definition = catalog.GetHero(hero.DefinitionId);
            return HeroPowerCalculator.CalculateBreakdown(
                hero,
                definition,
                equipmentModifierProvider,
                tuning);
        }

        public IReadOnlyList<HeroPowerMetricEntry> BuildMetricEntries(
            IEnumerable<HeroInstance> heroes,
            IEnumerable<string> activeTeamInstanceIds)
        {
            if (heroes == null) throw new ArgumentNullException(nameof(heroes));
            if (activeTeamInstanceIds == null)
                throw new ArgumentNullException(nameof(activeTeamInstanceIds));

            var teamIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in activeTeamInstanceIds)
            {
                if (string.IsNullOrWhiteSpace(id) || !teamIds.Add(id))
                    throw new InvalidOperationException("Equipe possui ID vazio ou duplicado.");
            }

            var heroList = new List<HeroInstance>(heroes);
            HeroProgressionRules.ValidateUniqueInstanceIds(heroList);
            var entries = new List<HeroPowerMetricEntry>(heroList.Count);
            var foundTeamIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < heroList.Count; i++)
            {
                HeroInstance hero = heroList[i];
                bool active = teamIds.Contains(hero.InstanceId);
                if (active) foundTeamIds.Add(hero.InstanceId);
                HeroPower power = Evaluate(hero).HeroPower;
                entries.Add(new HeroPowerMetricEntry(
                    hero.InstanceId,
                    power,
                    hero.Unlocked,
                    active));
            }

            if (foundTeamIds.Count != teamIds.Count)
                throw new InvalidOperationException("Equipe referencia herói inexistente.");
            return entries.AsReadOnly();
        }
    }

    public static class HeroPowerMetrics
    {
        public static TeamPower CalculateTeamPower(
            IEnumerable<HeroPowerMetricEntry> entries,
            CombatBalanceTuning tuning)
        {
            List<HeroPowerMetricEntry> validated = ValidateEntries(entries, tuning);
            long total = 0;
            int teamCount = 0;
            for (int i = 0; i < validated.Count; i++)
            {
                HeroPowerMetricEntry entry = validated[i];
                if (!entry.ActiveTeamMember)
                    continue;
                if (!entry.Unlocked)
                    throw new InvalidOperationException("Equipe ativa contém herói bloqueado.");
                teamCount++;
                if (teamCount > tuning.activeTeamSize)
                    throw new InvalidOperationException("Equipe excede o tamanho configurado.");
                total = checked(total + entry.Power.Value);
            }
            return new TeamPower(total);
        }

        public static AccountPower CalculateAccountPower(
            IEnumerable<HeroPowerMetricEntry> entries,
            CombatBalanceTuning tuning)
        {
            List<HeroPowerMetricEntry> validated = ValidateEntries(entries, tuning);
            long total = 0;
            for (int i = 0; i < validated.Count; i++)
            {
                if (validated[i].Unlocked)
                    total = checked(total + validated[i].Power.Value);
            }
            return new AccountPower(total);
        }

        public static CompetitivePower CalculateCompetitivePower(
            IEnumerable<HeroPowerMetricEntry> entries,
            CombatBalanceTuning tuning)
        {
            List<HeroPowerMetricEntry> validated = ValidateEntries(entries, tuning);
            TeamPower teamPower = CalculateTeamPower(validated, tuning);
            var reserves = new List<long>();
            for (int i = 0; i < validated.Count; i++)
            {
                HeroPowerMetricEntry entry = validated[i];
                if (entry.Unlocked && !entry.ActiveTeamMember)
                    reserves.Add(entry.Power.Value);
            }
            reserves.Sort((left, right) => right.CompareTo(left));

            long reserveTotal = 0;
            int count = Math.Min(tuning.competitiveReserveHeroCount, reserves.Count);
            for (int i = 0; i < count; i++)
                reserveTotal = checked(reserveTotal + reserves[i]);

            long weightedReserve = ApplyBasisPoints(
                reserveTotal,
                tuning.competitiveReserveWeightBasisPoints);
            return new CompetitivePower(checked(teamPower.Value + weightedReserve));
        }

        private static List<HeroPowerMetricEntry> ValidateEntries(
            IEnumerable<HeroPowerMetricEntry> entries,
            CombatBalanceTuning tuning)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            HeroBalanceTuningValidator.Validate(tuning);
            var result = new List<HeroPowerMetricEntry>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (HeroPowerMetricEntry entry in entries)
            {
                if (entry == null)
                    throw new InvalidOperationException("Métrica possui entrada nula.");
                if (!ids.Add(entry.InstanceId))
                    throw new InvalidOperationException("Métrica possui instanceId duplicado.");
                result.Add(entry);
            }
            return result;
        }

        private static long ApplyBasisPoints(long value, int basisPoints)
        {
            long whole = checked((value / 10000) * basisPoints);
            long remainder = checked(((value % 10000) * basisPoints) / 10000);
            return checked(whole + remainder);
        }
    }

    public sealed class SeasonPeakPower
    {
        public SeasonPeakPower(string seasonId, long value, long serverVersion = 0)
        {
            if (string.IsNullOrWhiteSpace(seasonId))
                throw new ArgumentException("seasonId é obrigatório.", nameof(seasonId));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (serverVersion < 0) throw new ArgumentOutOfRangeException(nameof(serverVersion));
            SeasonId = seasonId;
            Value = value;
            ServerVersion = serverVersion;
        }

        public string SeasonId { get; }
        public long Value { get; }
        public long ServerVersion { get; }

        public SeasonPeakPower Observe(
            CompetitivePower currentPower,
            long nextServerVersion)
        {
            if (nextServerVersion < ServerVersion)
                throw new InvalidOperationException("serverVersion sazonal não pode regredir.");
            long nextPeak = Math.Max(Value, currentPower.Value);
            return new SeasonPeakPower(SeasonId, nextPeak, nextServerVersion);
        }
    }
}
