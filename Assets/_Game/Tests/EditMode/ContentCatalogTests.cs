using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Editor.ContentCatalog;
using NUnit.Framework;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class ContentCatalogTests
    {
        [Test]
        public void Catalog_DemoDefinitions_IsValid()
        {
            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(ContentCatalogDemoFactory.Create());

            Assert.That(report.IsValid, Is.True, JoinMessages(report));
            Assert.That(report.Summary.HeroCount, Is.EqualTo(3));
            Assert.That(report.Summary.ItemCount, Is.EqualTo(11));
            Assert.That(report.Summary.EquipmentCount, Is.EqualTo(4));
            Assert.That(report.Summary.MaterialCount, Is.EqualTo(6));
            Assert.That(report.Summary.RecipeCount, Is.EqualTo(8));
        }

        [Test]
        public void Catalog_DuplicateDefinitionId_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var heroes = new List<HeroDefinition>(source.Heroes)
            {
                new HeroDefinition(
                    "hero_paladin_001", "Duplicado", "Teste", HeroArchetype.Warrior,
                    100, 10, 10, 100, Rarity.Common)
            };

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, heroes: heroes));

            AssertErrorContains(report, "ID duplicado");
        }

        [Test]
        public void Catalog_EmptyDefinitionId_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var heroes = new List<HeroDefinition>(source.Heroes)
            {
                new HeroDefinition(
                    " ", "Sem ID", "Teste", HeroArchetype.Warrior,
                    100, 10, 10, 100, Rarity.Common)
            };

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, heroes: heroes));

            AssertErrorContains(report, "ID nulo, vazio ou whitespace");
        }

        [Test]
        public void Catalog_MissingIngredientReference_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var recipes = new List<RecipeDefinition>(source.Recipes)
            {
                CreateRecipe(
                    "recipe_missing_reference",
                    "item_iron_sword_t1",
                    new RecipeIngredientDefinition(
                        "material_missing_t1", 1, true, false))
            };

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, recipes: recipes));

            AssertErrorContains(report, "Ingrediente inexistente");
        }

        [Test]
        public void Catalog_RecipeWithoutOutputOrIngredients_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var recipes = new List<RecipeDefinition>(source.Recipes)
            {
                new RecipeDefinition(
                    "recipe_invalid_t1", string.Empty, 1, ProfessionType.Blacksmith, 1,
                    ProfessionRank.Apprentice, ContentTier.Tier1, ContentTier.Tier1,
                    30, 1, 0, new RecipeIngredientDefinition[0],
                    new RecipeIngredientDefinition[0], false, false)
            };

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, recipes: recipes));

            AssertErrorContains(report, "Receita sem saída");
            AssertErrorContains(report, "explicitamente gratuita");
        }

        [Test]
        public void Catalog_InvalidTier_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var equipment = new List<EquipmentDefinition>(source.Equipment)
            {
                CreateEquipment(
                    "item_invalid_tier", EquipmentSlot.Weapon, (ContentTier)99,
                    Rarity.Common)
            };

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, equipment: equipment));

            AssertErrorContains(report, "Tier inválido");
        }

        [Test]
        public void Catalog_InvalidRarity_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var heroes = new List<HeroDefinition>(source.Heroes)
            {
                new HeroDefinition(
                    "hero_invalid_rarity", "Inválido", "Teste", HeroArchetype.Warrior,
                    100, 10, 10, 100, (Rarity)99)
            };

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, heroes: heroes));

            AssertErrorContains(report, "Raridade desconhecida");
        }

        [Test]
        public void Catalog_IngredientWithNonPositiveQuantity_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var recipes = new List<RecipeDefinition>(source.Recipes)
            {
                CreateRecipe(
                    "recipe_invalid_quantity",
                    "item_iron_sword_t1",
                    new RecipeIngredientDefinition(
                        "material_iron_ingot_t1", 0, true, false))
            };

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, recipes: recipes));

            AssertErrorContains(report, "quantidade inválida");
        }

        [Test]
        public void Lookup_ExistingIds_ReturnsTypedDefinitions()
        {
            var lookup = new ContentCatalogLookup(ContentCatalogDemoFactory.Create());

            Assert.That(lookup.GetHero("hero_paladin_001").DisplayName, Is.EqualTo("Paladino"));
            Assert.That(
                lookup.GetItem("item_iron_sword_t1"),
                Is.SameAs(lookup.GetEquipment("item_iron_sword_t1")));
            Assert.That(
                lookup.GetRecipe("recipe_iron_ingot_t1").OutputDefinitionId,
                Is.EqualTo("material_iron_ingot_t1"));
            Assert.That(
                lookup.GetProfession(ProfessionType.Blacksmith).DefinitionId,
                Is.EqualTo("profession_blacksmith"));
        }

        [Test]
        public void Lookup_UnknownId_ThrowsKeyNotFoundException()
        {
            var lookup = new ContentCatalogLookup(ContentCatalogDemoFactory.Create());

            Assert.Throws<KeyNotFoundException>(() => lookup.GetItem("item_unknown"));
            Assert.That(lookup.TryGetItem("item_unknown", out _), Is.False);
        }

        [Test]
        public void Catalog_EquipmentWithoutSlot_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var equipment = new List<EquipmentDefinition>(source.Equipment)
            {
                CreateEquipment(
                    "item_without_slot", EquipmentSlot.None, ContentTier.Tier1,
                    Rarity.Common)
            };

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, equipment: equipment));

            AssertErrorContains(report, "Equipamento sem slot válido");
        }

        [Test]
        public void Catalog_MaterialWithInvalidStackSize_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var materials = new List<MaterialDefinition>(source.Materials)
            {
                new MaterialDefinition(
                    "material_invalid_stack", "Material", "Teste",
                    MaterialCategory.Ore, ContentTier.Tier1, Rarity.Common,
                    false, null, new[] { "test" }, true, 0)
            };

            var catalog = new ContentCatalog(
                source.Heroes, source.Items, source.Equipment, materials, source.Recipes,
                source.Professions, source.Rarities, source.Tiers);
            ContentCatalogValidationReport report = ContentCatalogValidator.Validate(catalog);

            AssertErrorContains(report, "maxStackSize deve ser maior que zero");
        }

        [Test]
        public void Catalog_InconsistentProfessionThresholds_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var professions = new List<ProfessionDefinition>(source.Professions);
            ProfessionDefinition original = professions[0];
            professions[0] = new ProfessionDefinition(
                original.DefinitionId,
                original.ProfessionType,
                original.DisplayName,
                original.Description,
                original.StationName,
                original.MaxLevel,
                original.UnlockThresholds,
                new[]
                {
                    new ProfessionRankThreshold(ProfessionRank.Apprentice, 1),
                    new ProfessionRankThreshold(ProfessionRank.Proficient, 20),
                    new ProfessionRankThreshold(ProfessionRank.Master, 10),
                    new ProfessionRankThreshold(ProfessionRank.Grandmaster, 64),
                    new ProfessionRankThreshold(ProfessionRank.God, 90)
                },
                original.TierUnlockThresholds,
                original.MasteryBranches,
                original.SpecializationBonuses);

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, professions: professions));

            AssertErrorContains(report, "rankThresholds devem crescer");
        }

        [Test]
        public void Catalog_MissingPersistedProfession_IsRejected()
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var professions = new List<ProfessionDefinition>();
            for (int i = 0; i < source.Professions.Count; i++)
            {
                if (source.Professions[i].ProfessionType != ProfessionType.Gatherer)
                    professions.Add(source.Professions[i]);
            }

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, professions: professions));

            AssertErrorContains(report, "Definição de profissão persistida ausente");
        }

        [TestCase(ItemType.Equipment)]
        [TestCase(ItemType.Material)]
        public void Catalog_SpecializedTypeInGenericItems_IsRejected(ItemType itemType)
        {
            ContentCatalog source = ContentCatalogDemoFactory.Create();
            var items = new List<ItemDefinition>(source.Items)
            {
                new ItemDefinition(
                    $"item_misplaced_{itemType.ToString().ToLowerInvariant()}",
                    "Item mal posicionado",
                    "Teste",
                    itemType,
                    ContentTier.Tier1,
                    Rarity.Common,
                    itemType == ItemType.Material,
                    itemType == ItemType.Material ? 99 : 1,
                    true,
                    true,
                    true)
            };

            ContentCatalogValidationReport report =
                ContentCatalogValidator.Validate(Copy(source, items: items));

            AssertErrorContains(
                report,
                "Equipment e Material devem usar suas coleções especializadas");
        }

        [Test]
        public void Catalog_RuntimeCollections_RejectMutation()
        {
            ContentCatalog catalog = ContentCatalogDemoFactory.Create();
            var mutableView = (IList<HeroDefinition>)catalog.Heroes;

            Assert.Throws<NotSupportedException>(() => mutableView.Add(catalog.Heroes[0]));
        }

        [Test]
        public void PersistedEnums_ValuesAndLegacyAdapters_AreStable()
        {
            Assert.That((int)Rarity.Common, Is.Zero);
            Assert.That((int)Rarity.Mythic, Is.EqualTo(5));
            Assert.That((int)ContentTier.Tier1, Is.EqualTo(1));
            Assert.That((int)ContentTier.Tier9, Is.EqualTo(9));
            Assert.That((int)ProfessionType.Blacksmith, Is.EqualTo(1));
            Assert.That((int)ProfessionType.Gatherer, Is.EqualTo(5));
            Assert.That(GameRarity.Mythic.ToCatalogRarity(), Is.EqualTo(Rarity.Mythic));
            Assert.That(ContentTier.Tier9.ToLegacyTier(), Is.EqualTo(ItemTier.Tier9));
            Assert.That(
                CraftingProfession.Enchanter.ToProfessionType(),
                Is.EqualTo(ProfessionType.Enchanter));
        }

        private static RecipeDefinition CreateRecipe(
            string recipeId,
            string outputId,
            RecipeIngredientDefinition ingredient)
        {
            return new RecipeDefinition(
                recipeId, outputId, 1, ProfessionType.Blacksmith, 1,
                ProfessionRank.Apprentice, ContentTier.Tier1, ContentTier.Tier1,
                30, 1, 0, new[] { ingredient }, new RecipeIngredientDefinition[0],
                false, false);
        }

        private static EquipmentDefinition CreateEquipment(
            string id,
            EquipmentSlot slot,
            ContentTier tier,
            Rarity rarity)
        {
            return new EquipmentDefinition(
                id, "Equipamento de Teste", "Teste", tier, rarity, true, true, true,
                slot, 100, new string[0], 1, new string[0], ProfessionType.Blacksmith,
                3, BindingRule.UnboundUntilEquipped);
        }

        private static ContentCatalog Copy(
            ContentCatalog source,
            IReadOnlyList<HeroDefinition> heroes = null,
            IReadOnlyList<ItemDefinition> items = null,
            IReadOnlyList<EquipmentDefinition> equipment = null,
            IReadOnlyList<RecipeDefinition> recipes = null,
            IReadOnlyList<ProfessionDefinition> professions = null)
        {
            return new ContentCatalog(
                heroes ?? source.Heroes,
                items ?? source.Items,
                equipment ?? source.Equipment,
                source.Materials,
                recipes ?? source.Recipes,
                professions ?? source.Professions,
                source.Rarities,
                source.Tiers);
        }

        private static void AssertErrorContains(
            ContentCatalogValidationReport report,
            string expectedText)
        {
            Assert.That(
                report.Messages,
                Has.Some.Matches<ContentValidationMessage>(message =>
                    message.Severity == ContentValidationSeverity.Error &&
                    message.Reason.Contains(expectedText)),
                JoinMessages(report));
        }

        private static string JoinMessages(ContentCatalogValidationReport report)
        {
            var lines = new List<string>();
            for (int i = 0; i < report.Messages.Count; i++)
                lines.Add(report.Messages[i].ToString());
            return string.Join(Environment.NewLine, lines);
        }
    }
}
