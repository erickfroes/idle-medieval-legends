using System;
using System.Collections.Generic;
using System.Linq;
using IdleMedievalLegends.Application;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Gacha;
using IdleMedievalLegends.Domain.Heroes;
using NUnit.Framework;
using UnityEngine;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class GachaSimulatorTests
    {
        private static readonly HashSet<string> HeroIds =
            GachaDevelopmentContent.GetDemoHeroIds();
        private readonly CombatBalanceTuning tuning = new CombatBalanceTuning();

        [Test]
        public void ProbabilityValidator_DemoWeights_AreValid()
        {
            GachaValidationReport report = GachaProbabilityValidator.Validate(
                GachaDevelopmentContent.CreateMainBanner(),
                HeroIds);

            Assert.That(report.IsValid, Is.True, string.Join(" | ", report.Errors));
            Assert.That(
                GachaDevelopmentContent.CreateMainBanner().PoolEntries.Sum(x => x.Weight),
                Is.EqualTo(10000));
        }

        [Test]
        public void ProbabilityValidator_EmptyPool_IsRejected()
        {
            GachaBannerDefinition banner = CreateBanner(
                poolEntries: Array.Empty<GachaPoolEntry>());

            GachaValidationReport report =
                GachaProbabilityValidator.Validate(banner, HeroIds);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Errors.Any(x => x.Contains("Pool vazio")), Is.True);
        }

        [Test]
        public void ProbabilityValidator_NegativeWeight_IsRejected()
        {
            GachaBannerDefinition banner = CreateBanner(
                poolEntries: CreateEntries(-1, 6000, 4001));

            GachaValidationReport report =
                GachaProbabilityValidator.Validate(banner, HeroIds);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Errors.Any(x => x.Contains("Peso inválido")), Is.True);
        }

        [Test]
        public void ProbabilityValidator_InvalidConfiguration_IsRejected()
        {
            var entries = new[]
            {
                new GachaPoolEntry(
                    "bad",
                    GachaRewardType.HeroFragments,
                    "missing_hero",
                    0,
                    Rarity.Epic,
                    1,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "valid",
                    GachaRewardType.HeroFragments,
                    "hero_archer_001",
                    1,
                    Rarity.Rare,
                    1,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "aux",
                    GachaRewardType.AuxiliaryDevelopmentReward,
                    string.Empty,
                    1,
                    Rarity.Common,
                    1,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null)
            };
            GachaBannerDefinition banner = CreateBanner(
                costSingle: 0,
                softPityStart: 5,
                hardPityThreshold: 4,
                poolEntries: entries);

            GachaValidationReport report =
                GachaProbabilityValidator.Validate(banner, HeroIds);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Errors.Any(x => x.Contains("Custos")), Is.True);
            Assert.That(report.Errors.Any(x => x.Contains("hard pity")), Is.True);
            Assert.That(report.Errors.Any(x => x.Contains("inexistente")), Is.True);
            Assert.That(report.Errors.Any(x => x.Contains("fragmentos inválida")), Is.True);
        }

        [Test]
        public void ProbabilityValidator_NegativeMinimumProgressOtherThanSentinel_IsRejected()
        {
            var entries = new[]
            {
                new GachaPoolEntry(
                    "high",
                    GachaRewardType.HeroFragments,
                    "hero_paladin_001",
                    10,
                    Rarity.Epic,
                    1000,
                    true,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "invalid_progress",
                    GachaRewardType.HeroFragments,
                    "hero_archer_001",
                    5,
                    Rarity.Rare,
                    6000,
                    false,
                    -2,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "aux",
                    GachaRewardType.AuxiliaryDevelopmentReward,
                    string.Empty,
                    1,
                    Rarity.Common,
                    3000,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null)
            };
            GachaBannerDefinition banner = CreateBanner(poolEntries: entries);

            GachaValidationReport report =
                GachaProbabilityValidator.Validate(banner, HeroIds);

            Assert.That(entries[1].MinimumPlayerProgress, Is.EqualTo(-2));
            Assert.That(report.IsValid, Is.False);
            Assert.That(
                report.Errors.Any(x => x.Contains("minimumPlayerProgress inválido")),
                Is.True);
        }

        [Test]
        public void ProbabilityValidator_UndefinedRewardType_IsRejected()
        {
            var entries = new[]
            {
                new GachaPoolEntry(
                    "high",
                    GachaRewardType.HeroFragments,
                    "hero_paladin_001",
                    10,
                    Rarity.Epic,
                    1000,
                    true,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "low",
                    GachaRewardType.HeroFragments,
                    "hero_archer_001",
                    5,
                    Rarity.Rare,
                    6000,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "unknown",
                    (GachaRewardType)999,
                    string.Empty,
                    1,
                    Rarity.Common,
                    3000,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null)
            };
            GachaBannerDefinition banner = CreateBanner(poolEntries: entries);

            GachaValidationReport report =
                GachaProbabilityValidator.Validate(banner, HeroIds);

            Assert.That(report.IsValid, Is.False);
            Assert.That(
                report.Errors.Any(x => x.Contains("rewardType inválido")),
                Is.True);
        }

        [Test]
        public void Simulator_SameSeedAndState_IsDeterministic()
        {
            GachaBannerDefinition banner = GachaDevelopmentContent.CreateMainBanner();
            GachaSimulator first = CreateSimulator();
            GachaSimulator second = CreateSimulator();
            GachaPullRequest request = Request(banner, "deterministic", 10, 77123);

            GachaPullResult firstResult = first.Execute(banner, request);
            GachaPullResult secondResult = second.Execute(banner, request);

            Assert.That(
                firstResult.Rewards.Select(x => x.EntryId),
                Is.EqualTo(secondResult.Rewards.Select(x => x.EntryId)));
            Assert.That(
                firstResult.PityAfter.PullsSinceHighRarity,
                Is.EqualTo(secondResult.PityAfter.PullsSinceHighRarity));
        }

        [Test]
        public void Simulator_SinglePull_ChargesAndRecordsReward()
        {
            GachaBannerDefinition banner = GachaDevelopmentContent.CreateMainBanner();
            GachaSimulator simulator = CreateSimulator(1000);

            GachaPullResult result =
                simulator.Execute(banner, Request(banner, "single", 1, 1));

            Assert.That(result.Rewards, Has.Count.EqualTo(1));
            Assert.That(result.History, Has.Count.EqualTo(1));
            Assert.That(result.ChargedCost, Is.EqualTo(banner.CostSingle));
            Assert.That(result.RemainingDevelopmentCurrency, Is.EqualTo(900));
            Assert.That(result.History[0].Sequence, Is.Zero);
        }

        [Test]
        public void Simulator_MultiPull_UsesOneOrderedSequenceAndContinuousPity()
        {
            GachaBannerDefinition banner = GachaDevelopmentContent.CreateMainBanner();
            GachaSimulator simulator = CreateSimulator(1000);

            GachaPullResult result =
                simulator.Execute(banner, Request(banner, "multi", 10, 2));

            Assert.That(result.Rewards, Has.Count.EqualTo(10));
            Assert.That(result.History.Select(x => x.Sequence),
                Is.EqualTo(Enumerable.Range(0, 10)));
            for (int i = 1; i < result.History.Count; i++)
            {
                Assert.That(
                    result.History[i].PityBefore.Revision,
                    Is.EqualTo(result.History[i - 1].PityAfter.Revision));
            }
            Assert.That(result.PityAfter.TotalPulls, Is.EqualTo(10));
            Assert.That(result.ChargedCost, Is.EqualTo(banner.CostMulti));
        }

        [Test]
        public void Simulator_SoftPity_IncreasesHighRarityFrequency()
        {
            GachaBannerDefinition withoutSoft = CreateBanner(
                softPityStart: 1000000,
                hardPityThreshold: 1000000,
                highWeight: 1,
                lowWeight: 6000,
                auxiliaryWeight: 3999);
            GachaBannerDefinition withSoft = CreateBanner(
                id: "soft",
                softPityStart: 1,
                softPityIncreaseBasisPoints: 10000,
                hardPityThreshold: 1000000,
                highWeight: 1,
                lowWeight: 6000,
                auxiliaryWeight: 3999);

            GachaSimulationReport baseline = Simulate(withoutSoft, 10000, 404);
            GachaSimulationReport boosted = Simulate(withSoft, 10000, 404);

            Assert.That(CountRarity(boosted, Rarity.Epic),
                Is.GreaterThan(CountRarity(baseline, Rarity.Epic)));
        }

        [Test]
        public void Simulator_HardPity_AppliesExactlyAtThreshold()
        {
            GachaBannerDefinition banner = CreateBanner(
                softPityStart: 3,
                hardPityThreshold: 3,
                softPityIncreaseBasisPoints: 0,
                highWeight: 1,
                lowWeight: 6000,
                auxiliaryWeight: 3999);
            GachaSimulator beforeThreshold = CreateSimulator();
            beforeThreshold.RestorePityState(new GachaPityState(
                banner.PityKey, 1, false, 1, 1));
            GachaSimulator atThreshold = CreateSimulator();
            atThreshold.RestorePityState(new GachaPityState(
                banner.PityKey, 2, false, 2, 2));

            GachaPullResult before = beforeThreshold.Execute(
                banner,
                Request(banner, "before", 1, 17));
            GachaPullResult hard = atThreshold.Execute(
                banner,
                Request(banner, "hard", 1, 17));

            Assert.That(before.Rewards[0].HardPityApplied, Is.False);
            Assert.That(hard.Rewards[0].HardPityApplied, Is.True);
            Assert.That(hard.Rewards[0].Rarity, Is.GreaterThanOrEqualTo(Rarity.Epic));
        }

        [Test]
        public void Simulator_HighRarity_ResetsPityCounter()
        {
            GachaBannerDefinition banner = CreateBanner(
                softPityStart: 3,
                hardPityThreshold: 3,
                highWeight: 1,
                lowWeight: 6000,
                auxiliaryWeight: 3999);
            GachaSimulator simulator = CreateSimulator();
            simulator.RestorePityState(new GachaPityState(
                banner.PityKey, 2, false, 2, 2));

            GachaPullResult result =
                simulator.Execute(banner, Request(banner, "reset", 1, 18));

            Assert.That(result.PityAfter.PullsSinceHighRarity, Is.Zero);
        }

        [Test]
        public void Simulator_BannerPity_IsSeparated()
        {
            GachaBannerDefinition first = CreateBanner(id: "banner_a");
            GachaBannerDefinition second = CreateBanner(id: "banner_b");
            GachaSimulator simulator = CreateSimulator();

            simulator.Execute(first, Request(first, "a", 1, 11));
            simulator.Execute(second, Request(second, "b", 1, 12));

            Assert.That(simulator.GetPityState(first).TotalPulls, Is.EqualTo(1));
            Assert.That(simulator.GetPityState(second).TotalPulls, Is.EqualTo(1));
            Assert.That(first.PityKey, Is.Not.EqualTo(second.PityKey));
        }

        [Test]
        public void Simulator_GroupPity_IsSharedAcrossBanners()
        {
            GachaBannerDefinition first = CreateBanner(
                id: "group_a",
                pityScope: GachaPityScope.Group,
                pityGroupId: "shared");
            GachaBannerDefinition second = CreateBanner(
                id: "group_b",
                pityScope: GachaPityScope.Group,
                pityGroupId: "shared");
            GachaSimulator simulator = CreateSimulator();

            simulator.Execute(first, Request(first, "ga", 1, 11));
            simulator.Execute(second, Request(second, "gb", 1, 12));

            Assert.That(simulator.GetPityState(first).TotalPulls, Is.EqualTo(2));
            Assert.That(simulator.GetPityState(second).TotalPulls, Is.EqualTo(2));
        }

        [Test]
        public void Simulator_RepeatedRequest_IsIdempotentWithoutCurrencyOrFragmentDuplication()
        {
            GachaBannerDefinition banner = GachaDevelopmentContent.CreateMainBanner();
            GachaSimulator simulator = CreateSimulator(1000);
            GachaPullRequest request = Request(banner, "retry", 1, 3);

            GachaPullResult first = simulator.Execute(banner, request);
            long fragmentRevision = simulator.FragmentWallet.Revision;
            GachaPullResult replay = simulator.Execute(banner, request);

            Assert.That(replay.IdempotentReplay, Is.True);
            Assert.That(replay.ChargedCost, Is.Zero);
            Assert.That(simulator.Currency.Balance, Is.EqualTo(900));
            Assert.That(simulator.FragmentWallet.Revision, Is.EqualTo(fragmentRevision));
            Assert.That(replay.Rewards[0].EntryId, Is.EqualTo(first.Rewards[0].EntryId));
            Assert.That(simulator.History, Has.Count.EqualTo(1));
        }

        [Test]
        public void Simulator_ReusedRequestIdWithDifferentProgressOrTimestamp_IsRejected()
        {
            GachaBannerDefinition banner = GachaDevelopmentContent.CreateMainBanner();
            GachaSimulator simulator = CreateSimulator(1000);
            simulator.Execute(
                banner,
                new GachaPullRequest(
                    "payload",
                    banner.BannerId,
                    1,
                    33,
                    0,
                    100));

            Assert.Throws<InvalidOperationException>(
                () => simulator.Execute(
                    banner,
                    new GachaPullRequest(
                        "payload",
                        banner.BannerId,
                        1,
                        33,
                        1,
                        100)));
            Assert.Throws<InvalidOperationException>(
                () => simulator.Execute(
                    banner,
                    new GachaPullRequest(
                        "payload",
                        banner.BannerId,
                        1,
                        33,
                        0,
                        101)));
            Assert.That(simulator.Currency.Balance, Is.EqualTo(900));
            Assert.That(simulator.History, Has.Count.EqualTo(1));
        }

        [Test]
        public void FragmentWallet_RewardsAccumulateAndJsonRoundTrips()
        {
            var wallet = new FragmentWallet();
            wallet.Credit("hero_paladin_001", 10);
            wallet.Credit("hero_paladin_001", 15);
            wallet.Credit("hero_mage_001", 7);

            string json = JsonUtility.ToJson(wallet);
            FragmentWallet restored = JsonUtility.FromJson<FragmentWallet>(json);

            Assert.That(restored.GetBalance("hero_paladin_001"), Is.EqualTo(25));
            Assert.That(restored.GetBalance("hero_mage_001"), Is.EqualTo(7));
            Assert.That(json, Does.Contain("balances"));
            Assert.That(json, Does.Not.Contain("Dictionary"));
        }

        [Test]
        public void BannerEligibility_UnlockConsumesOnlyRequirementAndPreservesExcess()
        {
            var wallet = new FragmentWallet();
            wallet.Credit("hero_paladin_001", 100);
            HeroInstance locked = CreateLocked("hero_paladin_001", Rarity.Epic);

            HeroInstance unlocked =
                BannerEligibilityRules.Unlock(wallet, locked, tuning);

            Assert.That(unlocked.Unlocked, Is.True);
            Assert.That(unlocked.OwnedFragments, Is.Zero);
            Assert.That(wallet.GetBalance("hero_paladin_001"), Is.EqualTo(20));
            Assert.Throws<InvalidOperationException>(
                () => BannerEligibilityRules.Unlock(wallet, unlocked, tuning));
        }

        [Test]
        public void BannerEligibility_AscensionUsesExistingRulesAndPreservesExcess()
        {
            var wallet = new FragmentWallet();
            wallet.Credit("hero_archer_001", 25);
            HeroInstance unlocked = HeroInstance.Restore(
                "hero_archer",
                "hero_archer_001",
                "player",
                1,
                0,
                Rarity.Rare,
                0,
                0,
                null,
                true,
                0,
                0,
                null,
                0,
                tuning);

            HeroInstance ascended =
                BannerEligibilityRules.Ascend(wallet, unlocked, tuning);

            Assert.That(ascended.AscensionLevel, Is.EqualTo(1));
            Assert.That(wallet.GetBalance("hero_archer_001"), Is.EqualTo(5));
        }

        [Test]
        public void Simulator_DisabledAndExpiredBanner_AreRejected()
        {
            GachaBannerDefinition disabled = CreateBanner(enabled: false);
            GachaBannerDefinition expired = CreateBanner(
                id: "expired",
                startTime: 10,
                endTime: 20);

            Assert.Throws<InvalidOperationException>(
                () => CreateSimulator().Execute(
                    disabled,
                    Request(disabled, "disabled", 1, 1)));
            Assert.Throws<InvalidOperationException>(
                () => CreateSimulator().Execute(
                    expired,
                    new GachaPullRequest(
                        "expired",
                        expired.BannerId,
                        1,
                        1,
                        0,
                        20)));
        }

        [Test]
        public void Simulator_InvalidRequest_IsRejected()
        {
            GachaBannerDefinition banner = GachaDevelopmentContent.CreateMainBanner();
            GachaSimulator simulator = CreateSimulator();

            Assert.Throws<InvalidOperationException>(
                () => simulator.Execute(
                    banner,
                    new GachaPullRequest("bad", banner.BannerId, 2, 1, 0, 0)));
            Assert.Throws<InvalidOperationException>(
                () => simulator.Execute(
                    banner,
                    new GachaPullRequest("no_seed", banner.BannerId, 1, null, 0, 0)));
        }

        [Test]
        public void Simulator_FeaturedGuarantee_ForcesNextHighRarityFeatured()
        {
            var entries = new[]
            {
                new GachaPoolEntry(
                    "high",
                    GachaRewardType.HeroFragments,
                    "hero_paladin_001",
                    10,
                    Rarity.Epic,
                    1,
                    true,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "non_featured_high",
                    GachaRewardType.HeroFragments,
                    "hero_mage_001",
                    10,
                    Rarity.Epic,
                    9998,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "low",
                    GachaRewardType.HeroFragments,
                    "hero_archer_001",
                    5,
                    Rarity.Rare,
                    1,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null)
            };
            GachaBannerDefinition banner = CreateBanner(
                featuredGuaranteeEnabled: true,
                softPityStart: 1,
                hardPityThreshold: 1,
                poolEntries: entries);
            GachaSimulator simulator = CreateSimulator();
            simulator.RestorePityState(new GachaPityState(
                banner.PityKey, 0, true, 1, 1));

            GachaPullResult result =
                simulator.Execute(banner, Request(banner, "featured", 1, 91));

            Assert.That(result.Rewards[0].Rarity, Is.GreaterThanOrEqualTo(Rarity.Epic));
            Assert.That(result.Rewards[0].Featured, Is.True);
            Assert.That(result.PityAfter.FeaturedGuarantee, Is.False);
        }

        [Test]
        public void DevelopmentBanner_DirectUnlockExample_IsNotEnabledInMainPool()
        {
            GachaBannerDefinition banner = GachaDevelopmentContent.CreateMainBanner();

            Assert.That(
                banner.PoolEntries.Any(
                    x => x.RewardType == GachaRewardType.DirectHeroUnlock),
                Is.False);
            Assert.That(
                GachaDevelopmentContent.CreateDisabledDirectUnlockExample().RewardType,
                Is.EqualTo(GachaRewardType.DirectHeroUnlock));
        }

        [Test]
        public void Simulator_DirectUnlockDuplicate_ConvertsToConfiguredFragments()
        {
            var entries = new[]
            {
                new GachaPoolEntry(
                    "high",
                    GachaRewardType.DirectHeroUnlock,
                    "hero_paladin_001",
                    0,
                    Rarity.Epic,
                    10000,
                    true,
                    null,
                    GachaDuplicateRule.ConvertDirectUnlockToFragments,
                    null),
                new GachaPoolEntry(
                    "rare_fragments",
                    GachaRewardType.HeroFragments,
                    "hero_archer_001",
                    5,
                    Rarity.Rare,
                    1,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "rare_fragments_2",
                    GachaRewardType.HeroFragments,
                    "hero_mage_001",
                    5,
                    Rarity.Rare,
                    1,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null)
            };
            GachaBannerDefinition banner = CreateBanner(
                softPityStart: 1,
                hardPityThreshold: 1,
                poolEntries: entries);
            var simulator = new GachaSimulator(
                new DevelopmentGachaCurrency(100),
                new FragmentWallet(),
                HeroIds,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "hero_paladin_001"
                });

            GachaPullResult result =
                simulator.Execute(banner, Request(banner, "duplicate", 1, 92));

            Assert.That(
                result.Rewards[0].RewardType,
                Is.EqualTo(GachaRewardType.HeroFragments));
            Assert.That(result.Rewards[0].Quantity, Is.EqualTo(80));
            Assert.That(
                simulator.FragmentWallet.GetBalance("hero_paladin_001"),
                Is.EqualTo(80));
        }

        [Test]
        public void Simulator_DirectUnlockRepeatedWithinMultiPull_ConvertsLaterCopies()
        {
            var entries = new[]
            {
                new GachaPoolEntry(
                    "high",
                    GachaRewardType.DirectHeroUnlock,
                    "hero_paladin_001",
                    0,
                    Rarity.Epic,
                    10000,
                    true,
                    null,
                    GachaDuplicateRule.ConvertDirectUnlockToFragments,
                    null),
                new GachaPoolEntry(
                    "progress_locked_fragments",
                    GachaRewardType.HeroFragments,
                    "hero_archer_001",
                    5,
                    Rarity.Rare,
                    1,
                    false,
                    1,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "progress_locked_fragments_2",
                    GachaRewardType.HeroFragments,
                    "hero_mage_001",
                    5,
                    Rarity.Rare,
                    1,
                    false,
                    1,
                    GachaDuplicateRule.KeepAsFragments,
                    null)
            };
            GachaBannerDefinition banner = CreateBanner(
                softPityStart: 1,
                hardPityThreshold: 1,
                poolEntries: entries);
            GachaSimulator simulator = CreateSimulator(1000);

            GachaPullResult multi = simulator.Execute(
                banner,
                new GachaPullRequest(
                    "direct_multi",
                    banner.BannerId,
                    10,
                    500,
                    0,
                    0));
            GachaPullResult next = simulator.Execute(
                banner,
                new GachaPullRequest(
                    "direct_after_multi",
                    banner.BannerId,
                    1,
                    501,
                    0,
                    20));

            Assert.That(
                multi.Rewards[0].RewardType,
                Is.EqualTo(GachaRewardType.DirectHeroUnlock));
            Assert.That(
                multi.Rewards.Skip(1).All(
                    x => x.RewardType == GachaRewardType.HeroFragments &&
                         x.Quantity == 80),
                Is.True);
            Assert.That(
                next.Rewards[0].RewardType,
                Is.EqualTo(GachaRewardType.HeroFragments));
            Assert.That(next.Rewards[0].Quantity, Is.EqualTo(80));
            Assert.That(
                simulator.FragmentWallet.GetBalance("hero_paladin_001"),
                Is.EqualTo(800));
        }

        [Test]
        public void AggregateSimulation_BaseDistribution_StaysWithinStatisticalTolerance()
        {
            GachaBannerDefinition banner = CreateBanner(
                softPityStart: 1000000,
                hardPityThreshold: 1000000,
                softPityIncreaseBasisPoints: 0,
                highWeight: 1000,
                lowWeight: 6000,
                auxiliaryWeight: 3000);

            GachaSimulationReport report = Simulate(banner, 100000, 5150);

            Assert.That(CountReward(report, "high"),
                Is.InRange(9000, 11000));
            Assert.That(CountReward(report, "low"),
                Is.InRange(58000, 62000));
            Assert.That(CountReward(report, "aux"),
                Is.InRange(28000, 32000));
            Assert.That(report.HardPityActivations, Is.Zero);
        }

        [Test, Timeout(60000)]
        public void AggregateSimulation_OneMillionPulls_CompletesWithoutOverflow()
        {
            GachaSimulationReport report = Simulate(
                GachaDevelopmentContent.CreateMainBanner(),
                1000000,
                11011);

            Assert.That(report.PullCount, Is.EqualTo(1000000));
            Assert.That(
                report.RewardFrequencies.Sum(x => x.Count),
                Is.EqualTo(1000000));
            Assert.That(
                report.RarityFrequencies.Sum(x => x.Count),
                Is.EqualTo(1000000));
            Assert.That(report.MaximumPullsToHighRarity, Is.LessThanOrEqualTo(30));
            Assert.That(report.HighRarityPercentiles, Is.Not.Empty);
            Assert.That(report.AverageCostPerUnlock, Is.GreaterThan(0d));
        }

        private static GachaSimulator CreateSimulator(long balance = 10000000)
        {
            return new GachaSimulator(
                new DevelopmentGachaCurrency(balance),
                new FragmentWallet(),
                HeroIds);
        }

        private static GachaPullRequest Request(
            GachaBannerDefinition banner,
            string requestId,
            int pullCount,
            long seed)
        {
            return new GachaPullRequest(
                requestId,
                banner.BannerId,
                pullCount,
                seed,
                0,
                0);
        }

        private static GachaSimulationReport Simulate(
            GachaBannerDefinition banner,
            long pulls,
            long seed)
        {
            return GachaSimulator.SimulateAggregate(
                banner,
                pulls,
                seed,
                0,
                HeroIds,
                new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    { "hero_paladin_001", 80 },
                    { "hero_archer_001", 50 },
                    { "hero_mage_001", 80 }
                });
        }

        private HeroInstance CreateLocked(string definitionId, Rarity rarity)
        {
            return HeroInstance.CreateLocked(
                "locked_" + definitionId,
                definitionId,
                "player",
                rarity,
                0,
                tuning);
        }

        private static long CountReward(
            GachaSimulationReport report,
            string entryId)
        {
            return report.RewardFrequencies.Single(x => x.Key == entryId).Count;
        }

        private static long CountRarity(
            GachaSimulationReport report,
            Rarity rarity)
        {
            return report.RarityFrequencies.Single(
                x => x.Key == rarity.ToString()).Count;
        }

        private static GachaBannerDefinition CreateBanner(
            string id = "test_banner",
            bool enabled = true,
            long? startTime = null,
            long? endTime = null,
            long costSingle = 10,
            int softPityStart = 20,
            int softPityIncreaseBasisPoints = 0,
            int hardPityThreshold = 30,
            int highWeight = 1000,
            int lowWeight = 6000,
            int auxiliaryWeight = 3000,
            GachaPityScope pityScope = GachaPityScope.Banner,
            string pityGroupId = "",
            bool featuredGuaranteeEnabled = false,
            IEnumerable<GachaPoolEntry> poolEntries = null)
        {
            return new GachaBannerDefinition(
                id,
                "Test Banner",
                "Test only",
                startTime,
                endTime,
                enabled,
                GachaCurrencyType.DevelopmentGachaCurrency,
                costSingle,
                90,
                10,
                poolEntries ?? CreateEntries(highWeight, lowWeight, auxiliaryWeight),
                new[] { "high" },
                softPityStart,
                softPityIncreaseBasisPoints,
                hardPityThreshold,
                Rarity.Epic,
                pityScope,
                pityGroupId,
                featuredGuaranteeEnabled,
                "test_rules_v1",
                new[] { new GachaDuplicateConversionRule(Rarity.Epic, 80) },
                new[] { "test" });
        }

        private static GachaPoolEntry[] CreateEntries(
            int highWeight,
            int lowWeight,
            int auxiliaryWeight)
        {
            return new[]
            {
                new GachaPoolEntry(
                    "high",
                    GachaRewardType.HeroFragments,
                    "hero_paladin_001",
                    10,
                    Rarity.Epic,
                    highWeight,
                    true,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "low",
                    GachaRewardType.HeroFragments,
                    "hero_archer_001",
                    5,
                    Rarity.Rare,
                    lowWeight,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null),
                new GachaPoolEntry(
                    "aux",
                    GachaRewardType.AuxiliaryDevelopmentReward,
                    string.Empty,
                    1,
                    Rarity.Common,
                    auxiliaryWeight,
                    false,
                    null,
                    GachaDuplicateRule.KeepAsFragments,
                    null)
            };
        }
    }
}
