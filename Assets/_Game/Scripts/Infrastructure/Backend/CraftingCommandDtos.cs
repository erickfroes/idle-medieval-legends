using System;

namespace IdleMedievalLegends.Infrastructure.Backend
{
    /// <summary>
    /// DTOs enviam somente intenção. O cliente não define XP recebido, raridade,
    /// roll, duração final, IDs de saída ou custo efetivo.
    /// </summary>
    [Serializable]
    public sealed class StartCraftRequest
    {
        public string recipeId = string.Empty;
        public int quantity = 1;
        public string selectedToolInstanceId = string.Empty;
        public string selectedCatalystInstanceId = string.Empty;
    }

    [Serializable]
    public sealed class CancelCraftJobRequest
    {
    }

    [Serializable]
    public sealed class SelectPrimaryProfessionRequest
    {
        public string professionId = string.Empty;

        public SelectPrimaryProfessionRequest()
        {
        }

        public SelectPrimaryProfessionRequest(BackendProfession profession)
        {
            professionId = BackendWireValues.ToProfessionId(profession);
        }
    }

    [Serializable]
    public sealed class UpgradeProfessionStationRequest
    {
        public string professionId = string.Empty;

        public UpgradeProfessionStationRequest()
        {
        }

        public UpgradeProfessionStationRequest(BackendProfession profession)
        {
            professionId = BackendWireValues.ToProfessionId(profession);
        }
    }

    [Serializable]
    public sealed class LearnRecipeRequest
    {
        public string diagramInstanceId = string.Empty;
    }

    [Serializable]
    public sealed class CreateCraftingCommissionRequest
    {
        public string recipeId = string.Empty;
        public int quantity = 1;
        public long serviceFeeGems;
        public long expiresInSeconds;
    }

    [Serializable]
    public sealed class AcceptCraftingCommissionRequest
    {
        public string selectedToolInstanceId = string.Empty;
        public string selectedCatalystInstanceId = string.Empty;
    }
}
