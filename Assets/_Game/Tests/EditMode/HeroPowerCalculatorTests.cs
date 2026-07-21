using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Common;
using NUnit.Framework;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class HeroPowerCalculatorTests
    {
        [Test]
        public void Fighter_LevelOne_HasExpectedPower()
        {
            var hero = new HeroBuildData
            {
                level = 1,
                rarity = GameRarity.Common,
                ascension = 0,
                baseStats = new HeroStatBlock(1100, 105, 100, 100f)
            };

            long power = HeroPowerCalculator.CalculateHeroPower(
                hero,
                new CombatBalanceTuning());

            Assert.That(power, Is.EqualTo(1140));
        }

        [Test]
        public void MythicHero_IsStrongerThanLegendaryAtSameProgression()
        {
            var tuning = new CombatBalanceTuning();
            var legendary = new HeroBuildData
            {
                level = 100,
                rarity = GameRarity.Legendary,
                ascension = 5,
                baseStats = new HeroStatBlock(1100, 105, 100, 100f)
            };
            var mythic = new HeroBuildData
            {
                level = 100,
                rarity = GameRarity.Mythic,
                ascension = 5,
                baseStats = new HeroStatBlock(1100, 105, 100, 100f)
            };

            long legendaryPower = HeroPowerCalculator.CalculateHeroPower(legendary, tuning);
            long mythicPower = HeroPowerCalculator.CalculateHeroPower(mythic, tuning);

            Assert.That(legendaryPower, Is.EqualTo(32830));
            Assert.That(mythicPower, Is.EqualTo(37951));
            Assert.That(mythicPower, Is.GreaterThan(legendaryPower));
        }

        [Test]
        public void DefenseReduction_NeverExceedsConfiguredCap()
        {
            var tuning = new CombatBalanceTuning();

            double reduction = HeroPowerCalculator.CalculateDamageReduction(
                long.MaxValue,
                100,
                tuning);

            Assert.That(reduction, Is.EqualTo(tuning.maximumDamageReduction));
        }
    }
}
