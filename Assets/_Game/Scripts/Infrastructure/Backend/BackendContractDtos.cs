using System;
using System.Globalization;

namespace IdleMedievalLegends.Infrastructure.Backend
{
    public static class BackendContractVersions
    {
        public const int SnapshotSchemaVersion = 1;
    }

    public enum BackendPlatform
    {
        Unknown = 0,
        Android = 1,
        Ios = 2,
        Editor = 3
    }

    public enum BackendRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5
    }

    public enum BackendItemState
    {
        Owned = 0,
        Equipped = 1,
        Escrow = 2,
        Reserved = 3,
        Consumed = 4,
        Destroyed = 5
    }

    public enum BackendMarketListingState
    {
        Draft = 0,
        Active = 1,
        Reserved = 2,
        Sold = 3,
        Cancelled = 4,
        Expired = 5,
        Failed = 6
    }

    public enum BackendProfession
    {
        None = 0,
        Blacksmith = 1,
        Tailor = 2,
        Enchanter = 3,
        Alchemist = 4,
        Gatherer = 5
    }

    public static class BackendWireValues
    {
        public static string ToPlatform(BackendPlatform platform)
        {
            switch (platform)
            {
                case BackendPlatform.Android:
                    return "android";
                case BackendPlatform.Ios:
                    return "ios";
                case BackendPlatform.Editor:
                    return "editor";
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform));
            }
        }

        public static string ToProfessionId(BackendProfession profession)
        {
            switch (profession)
            {
                case BackendProfession.Blacksmith:
                    return "blacksmith";
                case BackendProfession.Tailor:
                    return "tailor";
                case BackendProfession.Enchanter:
                    return "enchanter";
                case BackendProfession.Alchemist:
                    return "alchemist";
                case BackendProfession.Gatherer:
                    return "gatherer";
                default:
                    throw new ArgumentOutOfRangeException(nameof(profession));
            }
        }
    }

    public sealed class BackendContractValidationException : InvalidOperationException
    {
        public BackendContractValidationException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    public sealed class CommandEnvelope<TPayload>
        where TPayload : class
    {
        public string commandId = string.Empty;
        public string requestId = string.Empty;
        public string clientVersion = string.Empty;
        public string platform = string.Empty;
        public string deviceSessionId = string.Empty;
        public long expectedRevision;
        public string sentAtClientTime = CreateClientTime();
        public TPayload payload;
        public string idempotencyKey = string.Empty;
        public string correlationId = CreateCorrelationId();

        public CommandEnvelope()
        {
        }

        public CommandEnvelope(
            string commandId,
            string requestId,
            string clientVersion,
            BackendPlatform platform,
            string deviceSessionId,
            long expectedRevision,
            TPayload payload,
            string idempotencyKey,
            string correlationId = null,
            string sentAtClientTime = null)
        {
            this.commandId = commandId ?? string.Empty;
            this.requestId = requestId ?? string.Empty;
            this.clientVersion = clientVersion ?? string.Empty;
            this.platform = BackendWireValues.ToPlatform(platform);
            this.deviceSessionId = deviceSessionId ?? string.Empty;
            this.expectedRevision = expectedRevision;
            this.sentAtClientTime = string.IsNullOrWhiteSpace(sentAtClientTime)
                ? CreateClientTime()
                : sentAtClientTime;
            this.payload = payload;
            this.idempotencyKey = idempotencyKey ?? string.Empty;
            this.correlationId = string.IsNullOrWhiteSpace(correlationId)
                ? CreateCorrelationId()
                : correlationId;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(sentAtClientTime))
            {
                sentAtClientTime = CreateClientTime();
            }

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = CreateCorrelationId();
            }

            Require(commandId, nameof(commandId));
            Require(requestId, nameof(requestId));
            Require(clientVersion, nameof(clientVersion));
            Require(deviceSessionId, nameof(deviceSessionId));
            Require(idempotencyKey, nameof(idempotencyKey));

            if (platform != "android" && platform != "ios" && platform != "editor")
            {
                throw new BackendContractValidationException("platform inválida.");
            }

            if (expectedRevision < 0)
            {
                throw new BackendContractValidationException(
                    "expectedRevision não pode ser negativa.");
            }

            if (payload == null)
            {
                throw new BackendContractValidationException("payload é obrigatório.");
            }

            if (!DateTimeOffset.TryParse(
                    sentAtClientTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _))
            {
                throw new BackendContractValidationException(
                    "sentAtClientTime deve ser RFC 3339 ou omitido.");
            }

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                throw new BackendContractValidationException(
                    "correlationId deve ser não vazio.");
            }
        }

        private static void Require(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BackendContractValidationException(
                    fieldName + " é obrigatório.");
            }
        }

        private static string CreateClientTime()
        {
            return DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }

        private static string CreateCorrelationId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }

    [Serializable]
    public sealed class BackendFieldErrorDto
    {
        public string field = string.Empty;
        public string reason = string.Empty;
    }

    [Serializable]
    public sealed class BackendErrorDetailsDto
    {
        public string resource = string.Empty;
        public BackendFieldErrorDto[] fieldErrors = Array.Empty<BackendFieldErrorDto>();
    }

    [Serializable]
    public sealed class BackendErrorDto
    {
        public string code = string.Empty;
        public string messageKey = string.Empty;
        public string safeMessage = string.Empty;
        public BackendErrorDetailsDto details = new BackendErrorDetailsDto();
        public bool retryable;
        public long expectedRevision;
        public long actualRevision;
        public string correlationId = string.Empty;

        public bool IsEmpty =>
            string.IsNullOrEmpty(code) &&
            string.IsNullOrEmpty(messageKey) &&
            string.IsNullOrEmpty(safeMessage) &&
            string.IsNullOrEmpty(correlationId);

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(messageKey) ||
                string.IsNullOrWhiteSpace(safeMessage) ||
                string.IsNullOrWhiteSpace(correlationId))
            {
                throw new BackendContractValidationException(
                    "Erro de backend possui campo obrigatório vazio.");
            }

            if (details == null || expectedRevision < 0 || actualRevision < 0)
            {
                throw new BackendContractValidationException(
                    "Erro de backend possui details ou revisão inválida.");
            }
        }
    }

    [Serializable]
    public sealed class SnapshotPatchOperationDto
    {
        public string op = string.Empty;
        public string path = string.Empty;
        public string valueJson;

        public void Validate()
        {
            if ((op != "set" && op != "remove") ||
                string.IsNullOrWhiteSpace(path) ||
                !path.StartsWith("/", StringComparison.Ordinal))
            {
                throw new BackendContractValidationException(
                    "Operação de patch possui op/path inválido.");
            }

            if (op == "set" && string.IsNullOrWhiteSpace(valueJson))
            {
                throw new BackendContractValidationException(
                    "Operação set exige valueJson.");
            }
        }
    }

    [Serializable]
    public sealed class SnapshotPatchDto
    {
        public long baseRevision;
        public long newRevision;
        public SnapshotPatchOperationDto[] operations =
            Array.Empty<SnapshotPatchOperationDto>();

        public bool IsEmpty =>
            baseRevision == 0 &&
            newRevision == 0 &&
            (operations == null || operations.Length == 0);

        public void Validate()
        {
            if (baseRevision < 0 || newRevision < baseRevision ||
                operations == null || operations.Length == 0)
            {
                throw new BackendContractValidationException(
                    "Patch de snapshot possui revisão/operações inválidas.");
            }

            for (int i = 0; i < operations.Length; i++)
            {
                if (operations[i] == null)
                {
                    throw new BackendContractValidationException(
                        "Patch de snapshot possui operação nula.");
                }

                operations[i].Validate();
            }
        }
    }

    [Serializable]
    public sealed class BackendResponse<T>
        where T : class
    {
        public string requestId = string.Empty;
        public string correlationId = string.Empty;
        public bool success;
        public string serverTime = string.Empty;
        public long newRevision;
        public T result;
        public BackendErrorDto error;
        public SnapshotPatchDto snapshotPatch;
        public bool retryable;
        public string rulesVersion = string.Empty;

        public void Validate()
        {
            if (error != null && error.IsEmpty)
            {
                error = null;
            }

            if (snapshotPatch != null && snapshotPatch.IsEmpty)
            {
                snapshotPatch = null;
            }

            if (string.IsNullOrWhiteSpace(requestId) ||
                string.IsNullOrWhiteSpace(correlationId) ||
                string.IsNullOrWhiteSpace(serverTime) ||
                string.IsNullOrWhiteSpace(rulesVersion) ||
                newRevision < 0)
            {
                throw new BackendContractValidationException(
                    "Resposta possui campo obrigatório vazio ou revisão inválida.");
            }

            if (success)
            {
                if ((result == null && snapshotPatch == null) ||
                    error != null || retryable)
                {
                    throw new BackendContractValidationException(
                        "Resposta de sucesso possui result/error/retryable inconsistente.");
                }
            }
            else
            {
                // JsonUtility materializes an empty generic result object when a
                // missing result field is deserialized. success/error remain the
                // wire discriminators, so result is ignored on failures.
                if (error == null || retryable != error.retryable)
                {
                    throw new BackendContractValidationException(
                        "Resposta de erro possui result/error/retryable inconsistente.");
                }

                error.Validate();
            }

            if (snapshotPatch != null)
            {
                snapshotPatch.Validate();
                if (snapshotPatch.newRevision != newRevision)
                {
                    throw new BackendContractValidationException(
                        "Patch e resposta possuem newRevision divergente.");
                }
            }
        }
    }

    [Serializable]
    public sealed class PlayerProfileSnapshotDto
    {
        public string playerId = string.Empty;
        public string displayName = string.Empty;
        public long accountPower;
        public long teamPower;
    }

    [Serializable]
    public sealed class WalletSnapshotDto
    {
        public long gold;
        public long gemsAvailable;
        public long gemsHeld;
        public long revision;
    }

    [Serializable]
    public sealed class HeroSnapshotDto
    {
        public string heroInstanceId = string.Empty;
        public string heroDefinitionId = string.Empty;
        public int level = 1;
        public long experience;
        public BackendRarity rarity;
        public int ascension;
        public long fragmentBalance;
        public long power;
        public long version;
    }

    [Serializable]
    public sealed class ItemSnapshotDto
    {
        public string itemInstanceId = string.Empty;
        public string definitionId = string.Empty;
        public long quantity;
        public int tier = 1;
        public BackendRarity rarity;
        public BackendItemState state;
        public long version;
    }

    [Serializable]
    public sealed class ProfessionSnapshotDto
    {
        public BackendProfession profession;
        public int level = 1;
        public long totalExperience;
        public int stationTier = 1;
        public int focusAvailable;
        public int focusMaximum;
        public int mythicPityCounter;
        public long revision;
    }

    [Serializable]
    public sealed class CampaignSnapshotDto
    {
        public string currentStageId = string.Empty;
        public string highestClearedStageId = string.Empty;
        public string pendingOfflineReportId = string.Empty;
        public long revision;
    }

    [Serializable]
    public sealed class EnergySnapshotDto
    {
        public int current;
        public int maximum;
        public string regenerationAnchorTime = string.Empty;
        public long revision;
    }

    [Serializable]
    public sealed class CraftingJobSnapshotDto
    {
        public string jobId = string.Empty;
        public string recipeId = string.Empty;
        public int quantity;
        public string status = string.Empty;
        public string completesAt = string.Empty;
        public long version;
    }

    [Serializable]
    public sealed class PityStateSnapshotDto
    {
        public string system = string.Empty;
        public string groupId = string.Empty;
        public string trackId = string.Empty;
        public long counter;
        public long revision;
    }

    [Serializable]
    public sealed class FeatureFlagSnapshotDto
    {
        public string key = string.Empty;
        public string valueJson = string.Empty;
        public long version;
    }

    [Serializable]
    public sealed class PlayerSnapshotDto
    {
        public int schemaVersion = BackendContractVersions.SnapshotSchemaVersion;
        public long revision;
        public string catalogVersion = string.Empty;
        public PlayerProfileSnapshotDto playerProfile = new PlayerProfileSnapshotDto();
        public WalletSnapshotDto wallet = new WalletSnapshotDto();
        public HeroSnapshotDto[] heroes = Array.Empty<HeroSnapshotDto>();
        public ItemSnapshotDto[] inventory = Array.Empty<ItemSnapshotDto>();
        public ProfessionSnapshotDto[] professions = Array.Empty<ProfessionSnapshotDto>();
        public CampaignSnapshotDto campaign = new CampaignSnapshotDto();
        public EnergySnapshotDto energy = new EnergySnapshotDto();
        public CraftingJobSnapshotDto[] craftingJobs =
            Array.Empty<CraftingJobSnapshotDto>();
        public PityStateSnapshotDto[] pityStates = Array.Empty<PityStateSnapshotDto>();
        public FeatureFlagSnapshotDto[] featureFlags =
            Array.Empty<FeatureFlagSnapshotDto>();

        public void Validate()
        {
            if (schemaVersion != BackendContractVersions.SnapshotSchemaVersion)
            {
                throw new BackendContractValidationException(
                    "schemaVersion de snapshot não suportada.");
            }

            if (revision < 0 || string.IsNullOrWhiteSpace(catalogVersion) ||
                playerProfile == null || wallet == null || campaign == null ||
                energy == null || heroes == null || inventory == null ||
                professions == null || craftingJobs == null ||
                pityStates == null || featureFlags == null)
            {
                throw new BackendContractValidationException(
                    "Snapshot possui campo obrigatório ausente ou revisão inválida.");
            }

            if (string.IsNullOrWhiteSpace(playerProfile.playerId) ||
                playerProfile.accountPower < 0 || playerProfile.teamPower < 0)
            {
                throw new BackendContractValidationException(
                    "Snapshot de profile possui identidade ou Poder inválido.");
            }

            if (wallet.gold < 0 || wallet.gemsAvailable < 0 ||
                wallet.gemsHeld < 0 || wallet.revision < 0)
            {
                throw new BackendContractValidationException(
                    "Snapshot de wallet possui saldo ou revisão inválida.");
            }
        }
    }

    [Serializable]
    public sealed class SessionTokensDto
    {
        public string accessToken = string.Empty;
        public string refreshToken = string.Empty;
        public string accessExpiresAt = string.Empty;
        public string refreshExpiresAt = string.Empty;
        public string deviceSessionId = string.Empty;
    }

    [Serializable]
    public sealed class SessionBootstrapResultDto
    {
        public string serverTime = string.Empty;
        public SessionTokensDto session = new SessionTokensDto();
        public PlayerSnapshotDto snapshot = new PlayerSnapshotDto();
    }

    [Serializable]
    public sealed class SessionRefreshResultDto
    {
        public string serverTime = string.Empty;
        public SessionTokensDto session = new SessionTokensDto();
    }

    [Serializable]
    public sealed class CommandResultDto
    {
        public string resourceId = string.Empty;
        public string state = string.Empty;
        public long goldDelta;
        public long gemsDelta;
        public string[] affectedItemInstanceIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class InventoryPageDto
    {
        public ItemSnapshotDto[] items = Array.Empty<ItemSnapshotDto>();
        public string nextCursor = string.Empty;
        public long revision;
    }

    [Serializable]
    public sealed class CraftingJobsPageDto
    {
        public CraftingJobSnapshotDto[] jobs = Array.Empty<CraftingJobSnapshotDto>();
        public string nextCursor = string.Empty;
        public string serverTime = string.Empty;
        public long revision;
    }

    [Serializable]
    public sealed class OfflineRewardReportDto
    {
        public string reportId = string.Empty;
        public long eligibleDurationSeconds;
        public long gold;
        public string[] itemRewardsJson = Array.Empty<string>();
        public bool claimed;
    }

    [Serializable]
    public sealed class DungeonCatalogDto
    {
        public string[] dungeonsJson = Array.Empty<string>();
        public EnergySnapshotDto energy = new EnergySnapshotDto();
        public long revision;
    }

    [Serializable]
    public sealed class GachaBannerPageDto
    {
        public string[] bannersJson = Array.Empty<string>();
        public long revision;
    }

    [Serializable]
    public sealed class GachaHistoryPageDto
    {
        public string[] entriesJson = Array.Empty<string>();
        public string nextCursor = string.Empty;
    }

    [Serializable]
    public sealed class MarketListingDto
    {
        public string listingId = string.Empty;
        public string itemInstanceId = string.Empty;
        public BackendMarketListingState state;
        public long priceGems;
        public int feeBasisPoints = 1000;
        public string expiresAt = string.Empty;
    }

    [Serializable]
    public sealed class MarketListingPageDto
    {
        public MarketListingDto[] listings = Array.Empty<MarketListingDto>();
        public string nextCursor = string.Empty;
    }

    [Serializable]
    public sealed class MarketHistoryPageDto
    {
        public string[] transactionsJson = Array.Empty<string>();
        public string nextCursor = string.Empty;
    }
}
