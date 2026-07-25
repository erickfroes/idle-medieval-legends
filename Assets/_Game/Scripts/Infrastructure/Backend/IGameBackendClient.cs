using System.Threading;
using System.Threading.Tasks;

namespace IdleMedievalLegends.Infrastructure.Backend
{
    /// <summary>
    /// Transport boundary for the authoritative API. Implementations map network
    /// DTOs only and must not expose Domain models as wire contracts.
    /// </summary>
    public interface IGameBackendClient
    {
        Task<BackendResponse<SessionBootstrapResultDto>> BootstrapSessionAsync(
            CommandEnvelope<SessionBootstrapRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<SessionRefreshResultDto>> RefreshSessionAsync(
            CommandEnvelope<SessionRefreshRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<PlayerSnapshotDto>> GetPlayerSnapshotAsync(
            long afterRevision,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> UnlockHeroAsync(
            CommandEnvelope<HeroUnlockRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> LevelUpHeroAsync(
            CommandEnvelope<HeroLevelUpRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> AscendHeroAsync(
            CommandEnvelope<HeroInstanceRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> PromoteHeroRarityAsync(
            CommandEnvelope<HeroInstanceRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> EquipHeroItemAsync(
            CommandEnvelope<HeroItemRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> UnequipHeroItemAsync(
            CommandEnvelope<HeroItemRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> UpdateTeamAsync(
            CommandEnvelope<TeamUpdateRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<InventoryPageDto>> GetInventoryAsync(
            string cursor,
            int limit,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> LockInventoryItemAsync(
            CommandEnvelope<ItemInstanceRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> UnlockInventoryItemAsync(
            CommandEnvelope<ItemInstanceRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> SalvageInventoryItemsAsync(
            CommandEnvelope<SalvageItemsRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> SplitInventoryStackAsync(
            CommandEnvelope<SplitStackRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> MergeInventoryStackAsync(
            CommandEnvelope<MergeStackRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> CreateCraftingJobAsync(
            CommandEnvelope<StartCraftRequest> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> CancelCraftingJobAsync(
            string jobId,
            CommandEnvelope<CancelCraftJobRequest> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> ClaimCraftingJobAsync(
            string jobId,
            CommandEnvelope<EmptyRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CraftingJobsPageDto>> GetCraftingJobsAsync(
            string cursor,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> SelectProfessionSpecializationAsync(
            CommandEnvelope<SelectPrimaryProfessionRequest> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> UpgradeProfessionStationAsync(
            CommandEnvelope<UpgradeProfessionStationRequest> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> StartCampaignStageAsync(
            CommandEnvelope<CampaignStartStageRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> CompleteCampaignStageAsync(
            CommandEnvelope<CampaignCompleteStageRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<OfflineRewardReportDto>> GetIdleReportAsync(
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> ClaimIdleReportAsync(
            CommandEnvelope<IdleClaimRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<DungeonCatalogDto>> GetDungeonsAsync(
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> CreateDungeonRunAsync(
            CommandEnvelope<DungeonRunCreateRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> CompleteDungeonRunAsync(
            string runId,
            CommandEnvelope<DungeonCompleteRunRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> ClaimDungeonRunAsync(
            string runId,
            CommandEnvelope<EmptyRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<GachaBannerPageDto>> GetGachaBannersAsync(
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> PullGachaAsync(
            CommandEnvelope<GachaPullRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<GachaHistoryPageDto>> GetGachaHistoryAsync(
            string cursor,
            int limit,
            CancellationToken cancellationToken);

        Task<BackendResponse<MarketListingPageDto>> GetMarketListingsAsync(
            string cursor,
            int limit,
            CancellationToken cancellationToken);

        Task<BackendResponse<MarketListingDto>> GetMarketListingAsync(
            string listingId,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> CreateMarketListingAsync(
            CommandEnvelope<CreateMarketListingRequest> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> CancelMarketListingAsync(
            string listingId,
            CommandEnvelope<EmptyMarketListingRequest> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> BuyMarketListingAsync(
            string listingId,
            CommandEnvelope<EmptyMarketListingRequest> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<MarketListingPageDto>> GetMyMarketListingsAsync(
            string cursor,
            CancellationToken cancellationToken);

        Task<BackendResponse<MarketHistoryPageDto>> GetMarketHistoryAsync(
            string cursor,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> CreateCraftingCommissionAsync(
            CommandEnvelope<CreateCraftingCommissionRequest> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> AcceptCraftingCommissionAsync(
            string commissionId,
            CommandEnvelope<AcceptCraftingCommissionRequest> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> CancelCraftingCommissionAsync(
            string commissionId,
            CommandEnvelope<EmptyRequestDto> command,
            CancellationToken cancellationToken);

        Task<BackendResponse<CommandResultDto>> ClaimCraftingCommissionAsync(
            string commissionId,
            CommandEnvelope<EmptyRequestDto> command,
            CancellationToken cancellationToken);
    }
}
