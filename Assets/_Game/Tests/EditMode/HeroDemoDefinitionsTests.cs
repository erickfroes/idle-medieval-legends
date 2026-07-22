using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Editor.ContentCatalog;
using NUnit.Framework;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class HeroDemoDefinitionsTests
    {
        [Test]
        public void DemoHeroes_ArchetypeProfiles_MatchTaskRequirements()
        {
            var lookup = new ContentCatalogLookup(ContentCatalogDemoFactory.Create());
            HeroDefinition paladin = lookup.GetHero("hero_paladin_001");
            HeroDefinition archer = lookup.GetHero("hero_archer_001");
            HeroDefinition mage = lookup.GetHero("hero_mage_001");

            Assert.That(paladin.Archetype, Is.EqualTo(HeroArchetype.Tank));
            Assert.That(paladin.BaseHealth, Is.GreaterThan(archer.BaseHealth));
            Assert.That(paladin.BaseDefense, Is.GreaterThan(archer.BaseDefense));
            Assert.That(paladin.BaseSpeed, Is.LessThan(archer.BaseSpeed));

            Assert.That(archer.BaseAttack, Is.GreaterThan(paladin.BaseAttack));
            Assert.That(archer.BaseSpeed, Is.GreaterThan(mage.BaseSpeed));
            Assert.That(archer.BaseHealth, Is.GreaterThan(mage.BaseHealth));

            Assert.That(mage.Archetype, Is.EqualTo(HeroArchetype.Mage));
            Assert.That(mage.BaseAttack, Is.GreaterThan(paladin.BaseAttack));
            Assert.That(mage.BaseDefense, Is.LessThan(archer.BaseDefense));
        }
    }
}
