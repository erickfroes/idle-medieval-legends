using System;
using IdleMedievalLegends.Domain.Common;

namespace IdleMedievalLegends.Infrastructure.Backend
{
    /// <summary>
    /// DTOs enviam somente intenção. O cliente não define XP recebido, raridade,
    /// roll, duração final, IDs de saída ou custo efetivo.
    /// </summary>
    [Serializable]
    public sealed class StartCraftRequest
    {
        public string requestId = string.Empty;
        public string recipeId = string.Empty;
        public int quantity = 1;
        public string selectedToolInstanceId = string.Empty;
        public string selectedCatalystInstanceId = string.Empty;
    }

    [Serializable]
    public sealed class CancelCraftJobRequest
    {
        public string requestId = string.Empty;
        public string jobId = string.Empty;
    }

    [Serializable]
    public sealed class SelectPrimaryProfessionRequest
    {
        public string requestId = string.Empty;
        public CraftingProfession profession = CraftingProfession.None;
    }

    [Serializable]
    public sealed class UpgradeProfessionStationRequest
    {
        public string requestId = string.Empty;
        public CraftingProfession profession = CraftingProfession.None;
    }

    [Serializable]
    public sealed class LearnRecipeRequest
    {
        public string requestId = string.Empty;
        public string diagramInstanceId = string.Empty;
    }

    [Serializable]
    public sealed class CreateCraftingCommissionRequest
    {
        public string requestId = string.Empty;
        public string recipeId = string.Empty;
        public int quantity = 1;
        public long serviceFeeGems;
        public long expiresAtUnixMilliseconds;
    }

    [Serializable]
    public sealed class AcceptCraftingCommissionRequest
    {
        public string requestId = string.Empty;
        public string commissionId = string.Empty;
        public string selectedToolInstanceId = string.Empty;
        public string selectedCatalystInstanceId = string.Empty;
    }
}
