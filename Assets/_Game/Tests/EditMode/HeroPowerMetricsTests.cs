using System;
using IdleMedievalLegends.Domain.Combat;
using NUnit.Framework;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class HeroPowerMetricsTests
    {
        private CombatBalanceTuning tuning;

        [SetUp]
        public void SetUp()
        {
            tuning = new CombatBalanceTuning();
        }

        [Test]
        public void TeamPower_ActiveUnlockedHeroes_SumsOnlyTeam()
        {
            TeamPower result = HeroPowerMetrics.CalculateTeamPower(
                new[]
                {
                    Entry("hero_1", 100, true, true),
                    Entry("hero_2", 200, true, true),
                    Entry("hero_3", 1000, true, false)
                },
                tuning);

            Assert.That(result.Value, Is.EqualTo(300));
        }

        [Test]
        public void AccountPower_UnlockedHeroes_SumsTeamAndReserveButNotLocked()
        {
            AccountPower result = HeroPowerMetrics.CalculateAccountPower(
                new[]
                {
                    Entry("hero_1", 100, true, true),
                    Entry("hero_2", 200, true, false),
                    Entry("hero_3", 5000, false, false)
                },
                tuning);

            Assert.That(result.Value, Is.EqualTo(300));
        }

        [Test]
        public void CompetitivePower_TeamPlusFifteenPercentOfTopFiveReserve()
        {
            CompetitivePower result = HeroPowerMetrics.CalculateCompetitivePower(
                new[]
                {
                    Entry("team_1", 100, true, true),
                    Entry("team_2", 200, true, true),
                    Entry("reserve_1", 1000, true, false),
                    Entry("reserve_2", 900, true, false),
                    Entry("reserve_3", 800, true, false),
                    Entry("reserve_4", 700, true, false),
                    Entry("reserve_5", 600, true, false),
                    Entry("reserve_6", 500, true, false),
                    Entry("locked", 9999, false, false)
                },
                tuning);

            Assert.That(result.Value, Is.EqualTo(900));
        }

        [Test]
        public void SeasonPeakPower_LowerObservation_NeverDecreases()
        {
            var peak = new SeasonPeakPower("season_001", 1000, 10);

            SeasonPeakPower lower = peak.Observe(new CompetitivePower(900), 11);
            SeasonPeakPower higher = lower.Observe(new CompetitivePower(1200), 12);

            Assert.That(lower.Value, Is.EqualTo(1000));
            Assert.That(higher.Value, Is.EqualTo(1200));
        }

        [Test]
        public void TeamPower_SumOverflow_ThrowsOverflowException()
        {
            Assert.Throws<OverflowException>(() =>
                HeroPowerMetrics.CalculateTeamPower(
                    new[]
                    {
                        Entry("hero_1", long.MaxValue, true, true),
                        Entry("hero_2", 1, true, true)
                    },
                    tuning));
        }

        private static HeroPowerMetricEntry Entry(
            string id,
            long power,
            bool unlocked,
            bool active)
        {
            return new HeroPowerMetricEntry(id, new HeroPower(power), unlocked, active);
        }
    }
}
