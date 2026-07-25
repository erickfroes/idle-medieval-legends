using System;

namespace IdleMedievalLegends.Infrastructure.Backend
{
    [Serializable]
    public sealed class EmptyRequestDto
    {
    }

    [Serializable]
    public sealed class SessionBootstrapRequestDto
    {
        public string locale = string.Empty;
        public string timeZone = string.Empty;
        public string installId = string.Empty;
        public string attestationChallengeId = string.Empty;
        public string attestationAssertion = string.Empty;
    }

    [Serializable]
    public sealed class SessionRefreshRequestDto
    {
        public string refreshToken = string.Empty;
        public string deviceSessionId = string.Empty;
        public string attestationAssertion = string.Empty;
    }

    [Serializable]
    public sealed class HeroUnlockRequestDto
    {
        public string heroDefinitionId = string.Empty;
    }

    [Serializable]
    public sealed class HeroLevelUpRequestDto
    {
        public string heroInstanceId = string.Empty;
        public int levels = 1;
    }

    [Serializable]
    public sealed class HeroInstanceRequestDto
    {
        public string heroInstanceId = string.Empty;
    }

    [Serializable]
    public sealed class HeroItemRequestDto
    {
        public string heroInstanceId = string.Empty;
        public string itemInstanceId = string.Empty;
        public string slotId = string.Empty;
    }

    [Serializable]
    public sealed class TeamUpdateRequestDto
    {
        public string teamId = string.Empty;
        public string[] heroInstanceIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ItemInstanceRequestDto
    {
        public string itemInstanceId = string.Empty;
    }

    [Serializable]
    public sealed class SalvageItemsRequestDto
    {
        public string[] itemInstanceIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class SplitStackRequestDto
    {
        public string itemInstanceId = string.Empty;
        public long quantity;
    }

    [Serializable]
    public sealed class MergeStackRequestDto
    {
        public string targetItemInstanceId = string.Empty;
        public string sourceItemInstanceId = string.Empty;
        public long quantity;
    }

    [Serializable]
    public sealed class CampaignStartStageRequestDto
    {
        public string stageId = string.Empty;
        public string teamId = string.Empty;
    }

    [Serializable]
    public sealed class BattleCompletionEvidenceDto
    {
        public string rulesVersion = string.Empty;
        public string eventLogHash = string.Empty;
        public string compactReplay = string.Empty;
    }

    [Serializable]
    public sealed class CampaignCompleteStageRequestDto
    {
        public string battleId = string.Empty;
        public BattleCompletionEvidenceDto completionEvidence =
            new BattleCompletionEvidenceDto();
    }

    [Serializable]
    public sealed class DungeonCompleteRunRequestDto
    {
        public BattleCompletionEvidenceDto completionEvidence =
            new BattleCompletionEvidenceDto();
    }

    [Serializable]
    public sealed class IdleClaimRequestDto
    {
        public string reportId = string.Empty;
    }

    [Serializable]
    public sealed class DungeonRunCreateRequestDto
    {
        public string dungeonId = string.Empty;
        public string difficultyId = string.Empty;
        public string teamId = string.Empty;
    }

    [Serializable]
    public sealed class GachaPullRequestDto
    {
        public string bannerId = string.Empty;
        public int quantity = 1;
    }

    [Serializable]
    public sealed class CraftingCommissionCreateRequestDto
    {
        public string recipeId = string.Empty;
        public int quantity = 1;
        public long serviceFeeGems;
        public long expiresInSeconds;
    }

    [Serializable]
    public sealed class CraftingCommissionAcceptRequestDto
    {
        public string selectedToolInstanceId = string.Empty;
        public string selectedCatalystInstanceId = string.Empty;
    }
}
