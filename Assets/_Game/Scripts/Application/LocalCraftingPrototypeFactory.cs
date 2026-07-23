using System;
using System.Collections.Generic;
using System.Diagnostics;
using IdleMedievalLegends.Domain.Common;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Inventory;

namespace IdleMedievalLegends.Application
{
    public sealed class LocalPrototypeServerClock : IServerClock
    {
        private readonly long initialUnixMilliseconds;
        private readonly Stopwatch elapsed = Stopwatch.StartNew();

        public LocalPrototypeServerClock(long initialUnixMilliseconds)
        {
            if (initialUnixMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(initialUnixMilliseconds));
            this.initialUnixMilliseconds = initialUnixMilliseconds;
        }

        public long UtcNowUnixMilliseconds => checked(
            initialUnixMilliseconds + elapsed.ElapsedMilliseconds);
    }

    public static class LocalCraftingPrototypeFactory
    {
        public static LocalCraftingService Create(
            string playerId,
            PlayerInventory inventory,
            ContentCatalogLookup catalog,
            ProfessionProgressionTuning progressionTuning,
            CraftingRuntimeTuning runtimeTuning,
            IServerClock clock = null,
            long initialGold = 50000)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS)
            throw new PlatformNotSupportedException(
                "O crafting local existe somente no Editor e em development builds.");
#else
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            clock ??= new LocalPrototypeServerClock(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var knownByProfession = new Dictionary<CraftingProfession, List<string>>();
            foreach (CraftingProfession profession in new[]
                     {
                         CraftingProfession.Blacksmith,
                         CraftingProfession.Tailor,
                         CraftingProfession.Enchanter,
                         CraftingProfession.Alchemist,
                         CraftingProfession.Gatherer
                     })
                knownByProfession.Add(profession, new List<string>());
            for (int i = 0; i < catalog.Catalog.Recipes.Count; i++)
            {
                RecipeDefinition recipe = catalog.Catalog.Recipes[i];
                if (recipe.EnabledForNormalGameplay)
                    knownByProfession[recipe.Profession.ToLegacyProfession()].Add(recipe.RecipeId);
            }

            var progress = new List<ProfessionProgress>(5);
            foreach (KeyValuePair<CraftingProfession, List<string>> entry in knownByProfession)
            {
                progress.Add(new ProfessionProgress(
                    entry.Key,
                    1,
                    0,
                    ProfessionRank.Apprentice,
                    ItemTier.Tier1,
                    0,
                    0,
                    Array.Empty<string>(),
                    entry.Key == CraftingProfession.Blacksmith
                        ? ProfessionSpecialization.Primary
                        : ProfessionSpecialization.None,
                    ItemTier.Tier1,
                    runtimeTuning.baseFocusMaximum,
                    runtimeTuning.baseFocusMaximum,
                    0,
                    entry.Value,
                    0,
                    progressionTuning));
            }
            return new LocalCraftingService(
                playerId,
                inventory,
                catalog,
                clock,
                new LocalGoldEconomyService(initialGold),
                new LocalCraftingSeedGenerator(),
                progressionTuning,
                runtimeTuning,
                progress);
#endif
        }
    }
}
