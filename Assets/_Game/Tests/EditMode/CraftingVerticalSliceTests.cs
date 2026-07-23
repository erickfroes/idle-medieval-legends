using System;
using System.Collections.Generic;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Editor.ContentCatalog;
using NUnit.Framework;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class CraftingVerticalSliceTests
    {
        private const string PlayerId = "task008-player";
        private const string IronRecipe = "recipe_iron_ingot_t1";
        private const string SwordRecipe = "recipe_iron_sword_t1";
        private const string MythicRecipe = "recipe_divine_test_blade_t9";
        private static readonly ProfessionProgressionTuning Progression =
            new ProfessionProgressionTuning();

        [TestCase(1, ProfessionRank.Apprentice, ItemTier.Tier1)]
        [TestCase(10, ProfessionRank.Apprentice, ItemTier.Tier2)]
        [TestCase(20, ProfessionRank.Proficient, ItemTier.Tier3)]
        [TestCase(30, ProfessionRank.Proficient, ItemTier.Tier4)]
        [TestCase(40, ProfessionRank.Master, ItemTier.Tier5)]
        [TestCase(52, ProfessionRank.Master, ItemTier.Tier6)]
        [TestCase(64, ProfessionRank.Grandmaster, ItemTier.Tier7)]
        [TestCase(76, ProfessionRank.Grandmaster, ItemTier.Tier8)]
        [TestCase(90, ProfessionRank.God, ItemTier.Tier9)]
        [TestCase(100, ProfessionRank.God, ItemTier.Tier9)]
        public void ProfessionProgression_LevelThresholds_UnlockExpectedRankAndTier(
            int level,
            ProfessionRank rank,
            ItemTier tier)
        {
            Assert.That(ProfessionProgression.GetRankForLevel(level, Progression), Is.EqualTo(rank));
            Assert.That(
                ProfessionProgression.GetMaximumUnlockedTier(level, Progression),
                Is.EqualTo(tier));
        }

        [Test]
        public void Catalog_ContainsFiveProfessionsFiveStationsAndFivePlayableRecipes()
        {
            var catalog = CreateCatalog();
            Assert.That(catalog.Catalog.Professions, Has.Count.EqualTo(5));
            Assert.That(catalog.Catalog.Recipes, Has.Count.GreaterThanOrEqualTo(6));
            foreach (string station in new[]
                     {
                         "Forja", "Ateliê", "Mesa Arcana", "Laboratório",
                         "Acampamento de Expedição"
                     })
            {
                Assert.That(
                    Array.Exists(
                        new List<ProfessionDefinition>(catalog.Catalog.Professions).ToArray(),
                        value => value.StationName == station),
                    Is.True,
                    station);
            }
            Assert.That(catalog.GetRecipe(MythicRecipe).EnabledForNormalGameplay, Is.False);
        }

        [Test]
        public void Specialization_InitialBonusesAndMasterQueue_AreApplied()
        {
            var tuning = new CraftingRuntimeTuning();
            ProfessionProgress primary = CreateProgress(
                CraftingProfession.Blacksmith, 40, ItemTier.Tier5, 100,
                new[] { SwordRecipe }, ProfessionSpecialization.Primary);
            ProfessionProgress secondary = CreateProgress(
                CraftingProfession.Tailor, 40, ItemTier.Tier5, 100,
                Array.Empty<string>(), ProfessionSpecialization.None);
            Assert.That(
                CraftingRuntimeRules.GetQueueSlotCount(primary, 0, tuning),
                Is.EqualTo(CraftingRuntimeRules.GetQueueSlotCount(secondary, 0, tuning) + 1));
            Assert.That(tuning.primaryExperienceBonusBasisPoints, Is.EqualTo(2000));
            Assert.That(tuning.primaryDurationReductionBasisPoints, Is.EqualTo(1500));
            Assert.That(tuning.primaryQualityBonusPoints, Is.EqualTo(5));
        }

        [Test]
        public void Specialization_ChangeHonorsCooldownAndGoldWithoutLosingProgress()
        {
            Harness harness = CreateHarness();
            ProfessionProgress tailor = harness.Service.GetProgress(CraftingProfession.Tailor);
            int levelBefore = tailor.Level;
            Assert.Throws<InvalidOperationException>(() =>
                harness.Service.SelectPrimaryProfession(CraftingProfession.Tailor));
            harness.Clock.AdvanceMilliseconds(
                harness.Tuning.specializationCooldownSeconds * 1000L);
            long goldBefore = harness.Service.GoldBalance;
            harness.Service.SelectPrimaryProfession(CraftingProfession.Tailor);
            Assert.That(tailor.Specialization, Is.EqualTo(ProfessionSpecialization.Primary));
            Assert.That(tailor.Level, Is.EqualTo(levelBefore));
            Assert.That(
                harness.Service.GoldBalance,
                Is.EqualTo(goldBefore - harness.Tuning.specializationChangeGoldCost));
        }

        [Test]
        public void StartCraft_ValidRecipeReservesMaterialsAndCreatesNoOutput()
        {
            Harness harness = CreateHarness();
            int countBefore = harness.Inventory.Items.Count;
            CraftingJob job = harness.Service.StartCraft(
                CraftingProfession.Blacksmith, SwordRecipe, 1);
            Assert.That(job.State, Is.EqualTo(CraftingJobStatus.Running));
            Assert.That(job.OutputInstanceIds, Is.Empty);
            Assert.That(harness.Inventory.Items.Count, Is.EqualTo(countBefore));
            foreach (ReservedItemReference reference in job.ReservedItemReferences)
                Assert.That(
                    harness.Inventory.GetItem(reference.ItemInstanceId).State,
                    Is.EqualTo(InventoryItemState.ReservedByServer));
        }

        [Test]
        public void StartCraft_WrongProfession_IsRejected()
        {
            Harness harness = CreateHarness();
            CraftingCommandException exception = Assert.Throws<CraftingCommandException>(() =>
                harness.Service.StartCraft(CraftingProfession.Tailor, SwordRecipe, 1));
            Assert.That(exception.Code, Is.EqualTo(CraftingEligibilityCode.WrongProfession));
        }

        [Test]
        public void StartCraft_UnknownOrUnlearnedRecipe_IsRejected()
        {
            Harness harness = CreateHarness();
            Assert.That(
                Assert.Throws<CraftingCommandException>(() => harness.Service.StartCraft(
                    CraftingProfession.Blacksmith, "missing", 1)).Code,
                Is.EqualTo(CraftingEligibilityCode.InvalidRecipe));
            Harness unlearned = CreateHarness(knownBlacksmithRecipes: Array.Empty<string>());
            Assert.That(
                Assert.Throws<CraftingCommandException>(() => unlearned.Service.StartCraft(
                    CraftingProfession.Blacksmith, SwordRecipe, 1)).Code,
                Is.EqualTo(CraftingEligibilityCode.RecipeLocked));
        }

        [Test]
        public void StartCraft_LevelTierAndStationRequirements_AreIndependent()
        {
            Harness lowLevel = CreateHarness(
                blacksmithLevel: 1,
                blacksmithStation: ItemTier.Tier1,
                knownBlacksmithRecipes: new[] { MythicRecipe },
                allowTestRecipe: true);
            Assert.That(
                Assert.Throws<CraftingCommandException>(() => lowLevel.Service.StartCraft(
                    CraftingProfession.Blacksmith, MythicRecipe, 1)).Code,
                Is.EqualTo(CraftingEligibilityCode.ProfessionLevelTooLow));

            var legacyRecipe = new CraftingRecipeData(
                "tier2", CraftingProfession.Blacksmith, CraftingOperationType.Refine,
                ItemTier.Tier2, ProfessionRank.Apprentice, 1, ItemTier.Tier1,
                "output", InventoryItemKind.Material, 1, 1, 0, 1, false, true);
            ProfessionProgressData legacyProgress = ProfessionProgressData.CreateNew(
                CraftingProfession.Blacksmith);
            Assert.That(
                CraftingRules.CanStartRecipe(
                    legacyRecipe, legacyProgress, CraftingProfession.None, true, 1, 1).Code,
                Is.EqualTo(CraftingEligibilityCode.TierLocked));

            Harness lowStation = CreateHarness(
                blacksmithLevel: 100,
                blacksmithStation: ItemTier.Tier1,
                knownBlacksmithRecipes: new[] { MythicRecipe },
                allowTestRecipe: true);
            Assert.That(
                Assert.Throws<CraftingCommandException>(() => lowStation.Service.StartCraft(
                    CraftingProfession.Blacksmith, MythicRecipe, 1)).Code,
                Is.EqualTo(CraftingEligibilityCode.StationTierTooLow));
        }

        [Test]
        public void StartCraft_InsufficientFocusGoldMaterialsAndInvalidQuantity_AreRejected()
        {
            Harness noFocus = CreateHarness(blacksmithFocus: 0);
            Assert.That(
                Assert.Throws<CraftingCommandException>(() => noFocus.Service.StartCraft(
                    CraftingProfession.Blacksmith, SwordRecipe, 1)).Code,
                Is.EqualTo(CraftingEligibilityCode.InsufficientFocus));
            Harness noGold = CreateHarness(initialGold: 0);
            Assert.That(
                Assert.Throws<CraftingCommandException>(() => noGold.Service.StartCraft(
                    CraftingProfession.Blacksmith, SwordRecipe, 1)).Code,
                Is.EqualTo(CraftingEligibilityCode.InsufficientGold));
            Harness noMaterials = CreateHarness(seedInventory: false);
            Assert.That(
                Assert.Throws<CraftingCommandException>(() => noMaterials.Service.StartCraft(
                    CraftingProfession.Blacksmith, SwordRecipe, 1)).Code,
                Is.EqualTo(CraftingEligibilityCode.InsufficientMaterials));
            Assert.That(
                Assert.Throws<CraftingCommandException>(() => noMaterials.Service.StartCraft(
                    CraftingProfession.Blacksmith, SwordRecipe, 0)).Code,
                Is.EqualTo(CraftingEligibilityCode.InvalidQuantity));
        }

        [Test]
        public void StartCraft_LockedSelectedCatalyst_IsRejected()
        {
            Harness harness = CreateHarness(
                blacksmithLevel: 100,
                blacksmithStation: ItemTier.Tier9,
                blacksmithFocus: 100,
                knownBlacksmithRecipes: new[] { MythicRecipe },
                allowTestRecipe: true);
            string catalystId = AddCatalyst(harness, 1);
            harness.Inventory.Lock(catalystId, harness.Clock.UtcNowUnixMilliseconds);
            CraftingCommandException exception = Assert.Throws<CraftingCommandException>(() =>
                harness.Service.StartCraft(
                    CraftingProfession.Blacksmith, MythicRecipe, 1,
                    selectedCatalystInstanceId: catalystId));
            Assert.That(exception.Code, Is.EqualTo(CraftingEligibilityCode.ItemUnavailable));
        }

        [Test]
        public void CraftingQueue_BaseSlotsRejectThirdActiveJob()
        {
            Harness harness = CreateHarness();
            harness.Service.StartCraft(CraftingProfession.Blacksmith, IronRecipe, 1);
            harness.Service.StartCraft(CraftingProfession.Blacksmith, SwordRecipe, 1);
            Assert.That(
                Assert.Throws<CraftingCommandException>(() => harness.Service.StartCraft(
                    CraftingProfession.Tailor, "recipe_treated_leather_t1", 1)).Code,
                Is.EqualTo(CraftingEligibilityCode.QueueFull));
        }

        [Test]
        public void CancelCraft_ReturnsMaterialsAndAppliesConfiguredRefundPolicy()
        {
            Harness harness = CreateHarness();
            long goldBefore = harness.Service.GoldBalance;
            int focusBefore = harness.Service.GetProgress(CraftingProfession.Blacksmith).FocusCurrent;
            CraftingJob job = harness.Service.StartCraft(
                CraftingProfession.Blacksmith, SwordRecipe, 1);
            CraftingCancellationResult result = harness.Service.CancelCraft(job.JobId);
            Assert.That(job.State, Is.EqualTo(CraftingJobStatus.Cancelled));
            Assert.That(result.MaterialRefunds, Is.Not.Empty);
            foreach (ReservedItemReference reference in job.ReservedItemReferences)
                Assert.That(
                    harness.Inventory.GetItem(reference.ItemInstanceId).State,
                    Is.EqualTo(InventoryItemState.Owned));
            Assert.That(
                harness.Service.GetProgress(CraftingProfession.Blacksmith).FocusCurrent,
                Is.EqualTo(focusBefore - 1));
            Assert.That(harness.Service.GoldBalance, Is.LessThan(goldBefore));
        }

        [Test]
        public void CompleteCraft_IsIdempotentAndCreatesProvenancedInventoryOutput()
        {
            Harness harness = CreateHarness();
            CraftingJob job = harness.Service.StartCraft(
                CraftingProfession.Blacksmith, SwordRecipe, 1);
            harness.Clock.AdvanceMilliseconds(60000);
            CraftingResult first = harness.Service.CompleteCraft(job.JobId);
            int itemCount = harness.Inventory.Items.Count;
            CraftingResult second = harness.Service.CompleteCraft(job.JobId);
            Assert.That(second, Is.SameAs(first));
            Assert.That(harness.Inventory.Items.Count, Is.EqualTo(itemCount));
            Assert.That(first.Outputs, Has.Count.EqualTo(1));
            ItemInstance output = harness.Inventory.GetItem(first.Outputs[0].InstanceId);
            Assert.That(output.Provenance.SourceType, Is.EqualTo("crafting_job"));
            Assert.That(output.OriginTransactionId, Is.EqualTo($"craft:{job.JobId}"));
            Assert.That(job.OutputInstanceIds[0], Is.EqualTo($"{job.JobId}_output_0000"));
            Assert.Throws<InvalidOperationException>(() => harness.Service.CancelCraft(job.JobId));
        }

        [Test]
        public void CraftingService_Restart_DoesNotReuseJobOrOutputInstanceIds()
        {
            Harness harness = CreateHarness();
            CraftingJob firstJob = harness.Service.StartCraft(
                CraftingProfession.Blacksmith, SwordRecipe, 1);
            harness.Clock.AdvanceMilliseconds(60000);
            CraftingResult firstResult = harness.Service.CompleteCraft(firstJob.JobId);
            int itemCountAfterFirst = harness.Inventory.Items.Count;

            LocalCraftingService restarted = LocalCraftingPrototypeFactory.Create(
                PlayerId,
                harness.Inventory,
                harness.Catalog,
                Progression,
                harness.Tuning,
                harness.Clock,
                100000);
            CraftingJob secondJob = restarted.StartCraft(
                CraftingProfession.Blacksmith, SwordRecipe, 1);
            harness.Clock.AdvanceMilliseconds(60000);
            CraftingResult secondResult = restarted.CompleteCraft(secondJob.JobId);

            Assert.That(secondJob.JobId, Is.Not.EqualTo(firstJob.JobId));
            Assert.That(secondResult.Outputs[0].InstanceId,
                Is.Not.EqualTo(firstResult.Outputs[0].InstanceId));
            Assert.That(harness.Inventory.Items.Count,
                Is.EqualTo(itemCountAfterFirst + 1));
            Assert.That(
                harness.Inventory.GetItem(secondResult.Outputs[0].InstanceId)
                    .OriginTransactionId,
                Is.EqualTo($"craft:{secondJob.JobId}"));
        }

        [Test]
        public void CompleteCraft_GrantsXpAndSeparatesQualityFromRarity()
        {
            Harness harness = CreateHarness();
            ProfessionProgress progress = harness.Service.GetProgress(CraftingProfession.Blacksmith);
            long xpBefore = progress.Experience;
            CraftingJob job = harness.Service.StartCraft(
                CraftingProfession.Blacksmith, SwordRecipe, 1);
            harness.Clock.AdvanceMilliseconds(60000);
            CraftingResult result = harness.Service.CompleteCraft(job.JobId);
            Assert.That(result.ExperienceGranted, Is.GreaterThan(0));
            Assert.That(progress.Experience, Is.GreaterThan(xpBefore));
            Assert.That(result.QualityScore, Is.InRange(0, 100));
            Assert.That(Enum.IsDefined(typeof(GameRarity), result.Rarity), Is.True);
        }

        [Test]
        public void Experience_LowTierRecipeIsReducedAtTierNine()
        {
            var tuning = new CraftingRuntimeTuning();
            RecipeDefinition recipe = CreateCatalog().GetRecipe(SwordRecipe);
            ProfessionProgress tierOne = CreateProgress(
                CraftingProfession.Blacksmith, 1, ItemTier.Tier1, 100,
                new[] { SwordRecipe }, ProfessionSpecialization.None);
            ProfessionProgress tierNine = CreateProgress(
                CraftingProfession.Blacksmith, 100, ItemTier.Tier9, 100,
                new[] { SwordRecipe }, ProfessionSpecialization.None);
            long full = CraftingRuntimeRules.CalculateExperience(recipe, tierOne, 1, tuning);
            long reduced = CraftingRuntimeRules.CalculateExperience(recipe, tierNine, 1, tuning);
            Assert.That(reduced, Is.LessThan(full));
        }

        [Test]
        public void RarityTables_EveryTierAndBandSumsExactlyTenThousandBasisPoints()
        {
            var tuning = new CraftingRuntimeTuning();
            for (int tier = 1; tier <= 9; tier++)
            for (int band = 0; band <= (int)CraftingQualityBand.Divine; band++)
            {
                RarityWeightSet weights = CraftingRules.BuildConfiguredRarityWeights(
                    (ItemTier)tier, (CraftingQualityBand)band, tier == 9, 0, tuning);
                int sum = 0;
                for (int rarity = 0; rarity <= (int)GameRarity.Mythic; rarity++)
                    sum += weights.GetWeight((GameRarity)rarity);
                Assert.That(sum, Is.EqualTo(10000), $"T{tier}/{(CraftingQualityBand)band}");
            }
        }

        [Test]
        public void MythicPity_NonEligibleAttemptDoesNotIncrement()
        {
            Harness harness = CreateHarness();
            ProfessionProgress progress = harness.Service.GetProgress(CraftingProfession.Blacksmith);
            CraftingJob job = harness.Service.StartCraft(
                CraftingProfession.Blacksmith, IronRecipe, 1);
            harness.Clock.AdvanceMilliseconds(30000);
            CraftingResult result = harness.Service.CompleteCraft(job.JobId);
            Assert.That(result.PityBefore, Is.Zero);
            Assert.That(result.PityAfter, Is.Zero);
            Assert.That(progress.MythicPityCounter, Is.Zero);
        }

        [Test]
        public void MythicPity_FiftyFailuresEnableSoftPityOnFiftyFirstAttempt()
        {
            var tuning = new CraftingRuntimeTuning();
            RarityWeightSet attempt50 = CraftingRules.BuildConfiguredRarityWeights(
                ItemTier.Tier9, CraftingQualityBand.Divine, true, 49, tuning);
            RarityWeightSet attempt51 = CraftingRules.BuildConfiguredRarityWeights(
                ItemTier.Tier9, CraftingQualityBand.Divine, true, 50, tuning);
            Assert.That(attempt50.GetWeight(GameRarity.Mythic), Is.EqualTo(100));
            Assert.That(attempt51.GetWeight(GameRarity.Mythic), Is.EqualTo(105));
        }

        [Test]
        public void MythicPity_HundredthEligibleAttemptIsGuaranteedAndResetsOnlyProfession()
        {
            Harness harness = CreateHarness(
                blacksmithLevel: 100,
                blacksmithStation: ItemTier.Tier9,
                blacksmithFocus: 5000,
                blacksmithPity: 99,
                tailorPity: 17,
                knownBlacksmithRecipes: new[] { MythicRecipe },
                allowTestRecipe: true,
                seedGenerator: new NonMythicSeedGenerator());
            string catalystId = AddCatalyst(harness, 1);
            CraftingJob job = harness.Service.StartCraft(
                CraftingProfession.Blacksmith, MythicRecipe, 1,
                selectedCatalystInstanceId: catalystId);
            harness.Clock.AdvanceMilliseconds(1000);
            CraftingResult result = harness.Service.CompleteCraft(job.JobId);
            Assert.That(result.MythicTriggered, Is.True);
            Assert.That(result.Rarity, Is.EqualTo(GameRarity.Mythic));
            Assert.That(result.PityBefore, Is.EqualTo(99));
            Assert.That(result.PityAfter, Is.Zero);
            Assert.That(
                harness.Service.GetProgress(CraftingProfession.Tailor).MythicPityCounter,
                Is.EqualTo(17));
        }

        [Test]
        public void MythicPity_SimulationGuaranteesExactlyHundredthEligibleAttempt()
        {
            Harness harness = CreateHarness(
                blacksmithLevel: 100,
                blacksmithStation: ItemTier.Tier9,
                blacksmithFocus: 5000,
                knownBlacksmithRecipes: new[] { MythicRecipe },
                allowTestRecipe: true,
                seedGenerator: new NonMythicSeedGenerator());
            string primaryCatalystId = AddCatalyst(harness, 99);
            string finalCatalystId = AddCatalyst(harness, 1);
            for (int attempt = 1; attempt <= 100; attempt++)
            {
                CraftingJob job = harness.Service.StartCraft(
                    CraftingProfession.Blacksmith, MythicRecipe, 1,
                    selectedCatalystInstanceId: attempt < 100
                        ? primaryCatalystId
                        : finalCatalystId);
                harness.Clock.AdvanceMilliseconds(1000);
                CraftingResult result = harness.Service.CompleteCraft(job.JobId);
                Assert.That(result.MythicTriggered, Is.EqualTo(attempt == 100),
                    $"Tentativa elegível {attempt}");
            }
            Assert.That(
                harness.Service.GetProgress(CraftingProfession.Blacksmith).MythicPityCounter,
                Is.Zero);
        }

        [Test]
        public void MythicPity_EligibleFailureIncrementsOnlySelectedProfession()
        {
            Harness harness = CreateHarness(
                blacksmithLevel: 100,
                blacksmithStation: ItemTier.Tier9,
                blacksmithFocus: 100,
                blacksmithPity: 50,
                tailorPity: 9,
                knownBlacksmithRecipes: new[] { MythicRecipe },
                allowTestRecipe: true,
                seedGenerator: new NonMythicSeedGenerator());
            string catalystId = AddCatalyst(harness, 1);
            CraftingJob job = harness.Service.StartCraft(
                CraftingProfession.Blacksmith, MythicRecipe, 1,
                selectedCatalystInstanceId: catalystId);
            harness.Clock.AdvanceMilliseconds(1000);
            CraftingResult result = harness.Service.CompleteCraft(job.JobId);
            Assert.That(result.MythicTriggered, Is.False);
            Assert.That(result.PityAfter, Is.EqualTo(51));
            Assert.That(
                harness.Service.GetProgress(CraftingProfession.Tailor).MythicPityCounter,
                Is.EqualTo(9));
        }

        private static Harness CreateHarness(
            int blacksmithLevel = 1,
            ItemTier blacksmithStation = ItemTier.Tier1,
            int blacksmithFocus = 100,
            int blacksmithPity = 0,
            int tailorPity = 0,
            string[] knownBlacksmithRecipes = null,
            bool allowTestRecipe = false,
            bool seedInventory = true,
            long initialGold = 100000,
            ICraftingSeedGenerator seedGenerator = null)
        {
            ContentCatalogLookup catalog = CreateCatalog();
            var inventory = new PlayerInventory();
            InventorySnapshotData empty = InventorySnapshotData.CreateEmpty(PlayerId);
            inventory.ApplyServerSnapshot(empty, catalog);
            var clock = new ManualServerClock(empty.GeneratedAtUnixMilliseconds);
            if (seedInventory)
            {
                DevelopmentInventorySeeder.SeedIfEmpty(
                    inventory, catalog, PlayerId, clock.UtcNowUnixMilliseconds);
                clock.Set(inventory.CaptureSnapshotForCache().GeneratedAtUnixMilliseconds);
            }
            var tuning = new CraftingRuntimeTuning();
            var progress = new List<ProfessionProgress>();
            foreach (CraftingProfession profession in new[]
                     {
                         CraftingProfession.Blacksmith,
                         CraftingProfession.Tailor,
                         CraftingProfession.Enchanter,
                         CraftingProfession.Alchemist,
                         CraftingProfession.Gatherer
                     })
            {
                int level = profession == CraftingProfession.Blacksmith ? blacksmithLevel : 1;
                ItemTier station = profession == CraftingProfession.Blacksmith
                    ? blacksmithStation
                    : ItemTier.Tier1;
                int focus = profession == CraftingProfession.Blacksmith ? blacksmithFocus : 100;
                int pity = profession == CraftingProfession.Blacksmith
                    ? blacksmithPity
                    : profession == CraftingProfession.Tailor ? tailorPity : 0;
                string[] recipes = profession == CraftingProfession.Blacksmith
                    ? knownBlacksmithRecipes ?? new[] { IronRecipe, SwordRecipe }
                    : GetKnownRecipes(catalog, profession);
                progress.Add(CreateProgress(
                    profession, level, station, focus, recipes,
                    profession == CraftingProfession.Blacksmith
                        ? ProfessionSpecialization.Primary
                        : ProfessionSpecialization.None,
                    pity,
                    Math.Max(100, focus)));
            }
            var service = new LocalCraftingService(
                PlayerId, inventory, catalog, clock,
                new LocalGoldEconomyService(initialGold),
                seedGenerator ?? new NonMythicSeedGenerator(),
                Progression, tuning, progress, allowTestRecipe);
            return new Harness(service, inventory, catalog, clock, tuning);
        }

        private static ProfessionProgress CreateProgress(
            CraftingProfession profession,
            int level,
            ItemTier station,
            int focus,
            IEnumerable<string> recipes,
            ProfessionSpecialization specialization,
            int pity = 0,
            int focusMaximum = 100)
        {
            return new ProfessionProgress(
                profession,
                level,
                ProfessionProgression.GetCumulativeExperienceForLevel(level, Progression),
                ProfessionProgression.GetRankForLevel(level, Progression),
                ProfessionProgression.GetMaximumUnlockedTier(level, Progression),
                0,
                0,
                Array.Empty<string>(),
                specialization,
                station,
                focus,
                focusMaximum,
                pity,
                recipes,
                0,
                Progression);
        }

        private static string[] GetKnownRecipes(
            ContentCatalogLookup catalog,
            CraftingProfession profession)
        {
            var result = new List<string>();
            foreach (RecipeDefinition recipe in catalog.Catalog.Recipes)
                if (recipe.EnabledForNormalGameplay &&
                    recipe.Profession.ToLegacyProfession() == profession)
                    result.Add(recipe.RecipeId);
            return result.ToArray();
        }

        private static string AddCatalyst(Harness harness, long quantity)
        {
            ItemDefinition definition = harness.Catalog.GetItem("material_divine_catalyst_t9");
            string id = $"catalyst_{Guid.NewGuid():N}";
            long now = harness.Clock.UtcNowUnixMilliseconds;
            var item = new ItemInstance(
                id, definition.DefinitionId, PlayerId, InventoryItemKind.Material,
                GameRarity.Common, ItemTier.Tier9, quantity, true,
                InventoryItemState.Owned, ItemBinding.Unbound, string.Empty,
                string.Empty, string.Empty, 0, Array.Empty<RolledStatData>(), 0,
                -1, -1, false, 0, now, now,
                new ItemProvenanceData("test_authority", "task008", $"test:{id}"));
            harness.Inventory.AddAuthorizedItem(item, definition, now);
            return id;
        }

        private static ContentCatalogLookup CreateCatalog()
        {
            return new ContentCatalogLookup(ContentCatalogDemoFactory.Create());
        }

        private sealed class NonMythicSeedGenerator : ICraftingSeedGenerator
        {
            private readonly long seed;

            public NonMythicSeedGenerator()
            {
                for (long candidate = 1; candidate < 100000; candidate++)
                {
                    if (new DeterministicCraftingRandom(candidate).NextBasisPoints() < 100)
                    {
                        seed = candidate;
                        return;
                    }
                }
                throw new InvalidOperationException("Seed determinística de teste não encontrada.");
            }

            public long CreateSeed(string jobId, long serverTime)
            {
                return seed;
            }
        }

        private sealed class Harness
        {
            public Harness(
                LocalCraftingService service,
                PlayerInventory inventory,
                ContentCatalogLookup catalog,
                ManualServerClock clock,
                CraftingRuntimeTuning tuning)
            {
                Service = service;
                Inventory = inventory;
                Catalog = catalog;
                Clock = clock;
                Tuning = tuning;
            }

            public LocalCraftingService Service { get; }
            public PlayerInventory Inventory { get; }
            public ContentCatalogLookup Catalog { get; }
            public ManualServerClock Clock { get; }
            public CraftingRuntimeTuning Tuning { get; }
        }
    }
}
