using System;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Infrastructure.Save;

namespace IdleMedievalLegends.Application
{
    public static class GameBootstrapDependencies
    {
        public static void Validate(
            PlayerStateRepositoryBehaviour cachedStateRepository,
            CombatBalanceConfigAsset combatBalanceConfig,
            CraftingBalanceConfigAsset craftingBalanceConfig,
            ContentCatalogAsset contentCatalogAsset)
        {
            if (cachedStateRepository == null)
            {
                throw new InvalidOperationException(
                    "GameManager requer um PlayerStateRepositoryBehaviour.");
            }

            if (combatBalanceConfig == null || combatBalanceConfig.Tuning == null)
            {
                throw new InvalidOperationException(
                    "GameManager requer uma CombatBalanceConfig válida.");
            }

            if (craftingBalanceConfig == null ||
                craftingBalanceConfig.ProfessionProgression == null ||
                craftingBalanceConfig.EquipmentBalance == null ||
                craftingBalanceConfig.CraftingPity == null)
            {
                throw new InvalidOperationException(
                    "GameManager requer uma CraftingBalanceConfig válida.");
            }

            if (contentCatalogAsset == null)
            {
                throw new InvalidOperationException(
                    "GameManager requer um ContentCatalogAsset.");
            }

            if (!contentCatalogAsset.ValidateCatalog().IsValid)
            {
                throw new InvalidOperationException(
                    "GameManager requer um ContentCatalogAsset válido.");
            }
        }
    }
}
