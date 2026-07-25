#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IdleMedievalLegends.Infrastructure.Backend
{
    /// <summary>
    /// In-memory development adapter. It performs no authentication, transport,
    /// secure RNG, durable idempotency, transaction, ledger or anti-fraud work.
    /// Never treat its results as authoritative or ship it as production security.
    /// </summary>
    public sealed class MockGameBackendClient : IGameBackendClient
    {
        public const string MockRulesVersion = "mock_rules_no_authority";

        private PlayerSnapshotDto snapshot;

        public MockGameBackendClient(PlayerSnapshotDto initialSnapshot)
        {
            SetSnapshotForDevelopment(initialSnapshot);
        }

        public bool IsInsecureDevelopmentMock => true;

        public void SetSnapshotForDevelopment(PlayerSnapshotDto newSnapshot)
        {
            if (newSnapshot == null)
            {
                throw new ArgumentNullException(nameof(newSnapshot));
            }

            newSnapshot.Validate();
            snapshot = newSnapshot;
        }

        public Task<BackendResponse<SessionBootstrapResultDto>> BootstrapSessionAsync(
            CommandEnvelope<SessionBootstrapRequestDto> command,
            CancellationToken cancellationToken)
        {
            Validate(command, cancellationToken);
            var result = new SessionBootstrapResultDto
            {
                serverTime = UtcNow(),
                session = CreateInsecureSession(command.deviceSessionId),
                snapshot = snapshot
            };
            return Completed(Success(command.requestId, result));
        }

        public Task<BackendResponse<SessionRefreshResultDto>> RefreshSessionAsync(
            CommandEnvelope<SessionRefreshRequestDto> command,
            CancellationToken cancellationToken)
        {
            Validate(command, cancellationToken);
            var result = new SessionRefreshResultDto
            {
                serverTime = UtcNow(),
                session = CreateInsecureSession(command.payload.deviceSessionId)
            };
            return Completed(Success(command.requestId, result));
        }

        public Task<BackendResponse<PlayerSnapshotDto>> GetPlayerSnapshotAsync(
            long afterRevision,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (afterRevision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(afterRevision));
            }

            return Completed(Success("mock-get-player-snapshot", snapshot));
        }

        public Task<BackendResponse<CommandResultDto>> UnlockHeroAsync(
            CommandEnvelope<HeroUnlockRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> LevelUpHeroAsync(
            CommandEnvelope<HeroLevelUpRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> AscendHeroAsync(
            CommandEnvelope<HeroInstanceRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> PromoteHeroRarityAsync(
            CommandEnvelope<HeroInstanceRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> EquipHeroItemAsync(
            CommandEnvelope<HeroItemRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> UnequipHeroItemAsync(
            CommandEnvelope<HeroItemRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> UpdateTeamAsync(
            CommandEnvelope<TeamUpdateRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<InventoryPageDto>> GetInventoryAsync(
            string cursor,
            int limit,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (limit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }

            var result = new InventoryPageDto
            {
                items = snapshot.inventory,
                revision = snapshot.revision
            };
            return Completed(Success("mock-get-inventory", result));
        }

        public Task<BackendResponse<CommandResultDto>> LockInventoryItemAsync(
            CommandEnvelope<ItemInstanceRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> UnlockInventoryItemAsync(
            CommandEnvelope<ItemInstanceRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> SalvageInventoryItemsAsync(
            CommandEnvelope<SalvageItemsRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> SplitInventoryStackAsync(
            CommandEnvelope<SplitStackRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> MergeInventoryStackAsync(
            CommandEnvelope<MergeStackRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> CreateCraftingJobAsync(
            CommandEnvelope<StartCraftRequest> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> CancelCraftingJobAsync(
            string jobId,
            CommandEnvelope<CancelCraftJobRequest> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> ClaimCraftingJobAsync(
            string jobId,
            CommandEnvelope<EmptyRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CraftingJobsPageDto>> GetCraftingJobsAsync(
            string cursor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new CraftingJobsPageDto
            {
                jobs = snapshot.craftingJobs,
                revision = snapshot.revision,
                serverTime = UtcNow()
            };
            return Completed(Success("mock-get-crafting-jobs", result));
        }

        public Task<BackendResponse<CommandResultDto>>
            SelectProfessionSpecializationAsync(
                CommandEnvelope<SelectPrimaryProfessionRequest> command,
                CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> UpgradeProfessionStationAsync(
            CommandEnvelope<UpgradeProfessionStationRequest> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> StartCampaignStageAsync(
            CommandEnvelope<CampaignStartStageRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> CompleteCampaignStageAsync(
            CommandEnvelope<CampaignCompleteStageRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<OfflineRewardReportDto>> GetIdleReportAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return NotConfigured<OfflineRewardReportDto>("mock-get-idle-report");
        }

        public Task<BackendResponse<CommandResultDto>> ClaimIdleReportAsync(
            CommandEnvelope<IdleClaimRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<DungeonCatalogDto>> GetDungeonsAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new DungeonCatalogDto
            {
                energy = snapshot.energy,
                revision = snapshot.revision
            };
            return Completed(Success("mock-get-dungeons", result));
        }

        public Task<BackendResponse<CommandResultDto>> CreateDungeonRunAsync(
            CommandEnvelope<DungeonRunCreateRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> CompleteDungeonRunAsync(
            string runId,
            CommandEnvelope<DungeonCompleteRunRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> ClaimDungeonRunAsync(
            string runId,
            CommandEnvelope<EmptyRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<GachaBannerPageDto>> GetGachaBannersAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Completed(Success(
                "mock-get-gacha-banners",
                new GachaBannerPageDto { revision = snapshot.revision }));
        }

        public Task<BackendResponse<CommandResultDto>> PullGachaAsync(
            CommandEnvelope<GachaPullRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<GachaHistoryPageDto>> GetGachaHistoryAsync(
            string cursor,
            int limit,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Completed(Success(
                "mock-get-gacha-history",
                new GachaHistoryPageDto()));
        }

        public Task<BackendResponse<MarketListingPageDto>> GetMarketListingsAsync(
            string cursor,
            int limit,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Completed(Success(
                "mock-get-market-listings",
                new MarketListingPageDto()));
        }

        public Task<BackendResponse<MarketListingDto>> GetMarketListingAsync(
            string listingId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return NotConfigured<MarketListingDto>("mock-get-market-listing");
        }

        public Task<BackendResponse<CommandResultDto>> CreateMarketListingAsync(
            CommandEnvelope<CreateMarketListingRequest> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> CancelMarketListingAsync(
            string listingId,
            CommandEnvelope<EmptyMarketListingRequest> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> BuyMarketListingAsync(
            string listingId,
            CommandEnvelope<EmptyMarketListingRequest> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<MarketListingPageDto>> GetMyMarketListingsAsync(
            string cursor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Completed(Success(
                "mock-get-my-market-listings",
                new MarketListingPageDto()));
        }

        public Task<BackendResponse<MarketHistoryPageDto>> GetMarketHistoryAsync(
            string cursor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Completed(Success(
                "mock-get-market-history",
                new MarketHistoryPageDto()));
        }

        public Task<BackendResponse<CommandResultDto>> CreateCraftingCommissionAsync(
            CommandEnvelope<CreateCraftingCommissionRequest> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> AcceptCraftingCommissionAsync(
            string commissionId,
            CommandEnvelope<AcceptCraftingCommissionRequest> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> CancelCraftingCommissionAsync(
            string commissionId,
            CommandEnvelope<EmptyRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        public Task<BackendResponse<CommandResultDto>> ClaimCraftingCommissionAsync(
            string commissionId,
            CommandEnvelope<EmptyRequestDto> command,
            CancellationToken cancellationToken) =>
            NotConfigured(command, cancellationToken);

        private Task<BackendResponse<CommandResultDto>> NotConfigured<TPayload>(
            CommandEnvelope<TPayload> command,
            CancellationToken cancellationToken)
            where TPayload : class
        {
            Validate(command, cancellationToken);
            return NotConfigured<CommandResultDto>(command.requestId);
        }

        private Task<BackendResponse<T>> NotConfigured<T>(string requestId)
            where T : class
        {
            string correlationId = "mock-correlation";
            var error = new BackendErrorDto
            {
                code = "MOCK_NOT_CONFIGURED",
                messageKey = "backend.mock.not_configured",
                safeMessage =
                    "O mock de desenvolvimento não executa mutações econômicas.",
                details = new BackendErrorDetailsDto
                {
                    resource = "development_mock"
                },
                retryable = false,
                expectedRevision = snapshot.revision,
                actualRevision = snapshot.revision,
                correlationId = correlationId
            };
            var response = new BackendResponse<T>
            {
                requestId = requestId,
                correlationId = correlationId,
                success = false,
                serverTime = UtcNow(),
                newRevision = snapshot.revision,
                error = error,
                retryable = false,
                rulesVersion = MockRulesVersion
            };
            response.Validate();
            return Completed(response);
        }

        private BackendResponse<T> Success<T>(string requestId, T result)
            where T : class
        {
            var response = new BackendResponse<T>
            {
                requestId = requestId,
                correlationId = "mock-correlation",
                success = true,
                serverTime = UtcNow(),
                newRevision = snapshot.revision,
                result = result,
                retryable = false,
                rulesVersion = MockRulesVersion
            };
            response.Validate();
            return response;
        }

        private static void Validate<TPayload>(
            CommandEnvelope<TPayload> command,
            CancellationToken cancellationToken)
            where TPayload : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.Validate();
        }

        private static SessionTokensDto CreateInsecureSession(string deviceSessionId)
        {
            return new SessionTokensDto
            {
                accessToken = "development-mock-no-authentication",
                refreshToken = "development-mock-no-authentication",
                accessExpiresAt = UtcNow(),
                refreshExpiresAt = UtcNow(),
                deviceSessionId = deviceSessionId
            };
        }

        private static Task<T> Completed<T>(T value)
        {
            return Task.FromResult(value);
        }

        private static string UtcNow()
        {
            return DateTimeOffset.UtcNow.ToString("O");
        }
    }
}
#endif
