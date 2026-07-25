using System;
using System.Threading;
using System.Threading.Tasks;
using IdleMedievalLegends.Infrastructure.Backend;
using NUnit.Framework;
using UnityEngine;

namespace IdleMedievalLegends.Tests.EditMode
{
    public sealed class ContractSerializationTests
    {
        [Test]
        public void CommandEnvelope_RoundTrip_PreservesRequestAndRevision()
        {
            var command = new CommandEnvelope<CreateMarketListingRequest>(
                "command-013",
                "request-013",
                "0.13.0",
                BackendPlatform.Android,
                "device-session-013",
                481,
                new CreateMarketListingRequest("item-013", 9_223_372_036_854L),
                "idempotency-013");

            command.Validate();
            string json = JsonUtility.ToJson(command);
            CommandEnvelope<CreateMarketListingRequest> restored =
                JsonUtility.FromJson<CommandEnvelope<CreateMarketListingRequest>>(json);

            restored.Validate();
            Assert.That(restored.requestId, Is.EqualTo("request-013"));
            Assert.That(restored.expectedRevision, Is.EqualTo(481));
            Assert.That(restored.payload.itemInstanceId, Is.EqualTo("item-013"));
            Assert.That(restored.payload.priceGems, Is.EqualTo(9_223_372_036_854L));
            Assert.That(restored.platform, Is.EqualTo("android"));
            Assert.That(json, Does.Contain("\"platform\":\"android\""));
            Assert.That(json, Does.Not.Contain("\"platform\":1"));
            Assert.That(json, Does.Contain("\"sentAtClientTime\":\""));
            Assert.That(json, Does.Contain("\"correlationId\":\""));
            Assert.That(
                DateTimeOffset.TryParse(restored.sentAtClientTime, out _),
                Is.True);
            Assert.That(restored.correlationId, Is.Not.Empty);
            Assert.That(json, Does.Not.Contain("playerId"));
        }

        [Test]
        public void CommandEnvelope_WithOptionalValues_EmitsSchemaValidStrings()
        {
            var command = new CommandEnvelope<EmptyRequestDto>(
                "command-optional",
                "request-optional",
                "0.13.0",
                BackendPlatform.Ios,
                "device-session-optional",
                12,
                new EmptyRequestDto(),
                "idempotency-optional",
                "correlation-optional",
                "2026-07-25T15:31:22Z");

            command.Validate();
            string json = JsonUtility.ToJson(command);

            Assert.That(json, Does.Contain("\"platform\":\"ios\""));
            Assert.That(
                json,
                Does.Contain("\"sentAtClientTime\":\"2026-07-25T15:31:22Z\""));
            Assert.That(
                json,
                Does.Contain("\"correlationId\":\"correlation-optional\""));
        }

        [Test]
        public void CommandEnvelope_InvalidClientTime_IsRejected()
        {
            var command = new CommandEnvelope<EmptyRequestDto>(
                "command-time",
                "request-time",
                "0.13.0",
                BackendPlatform.Editor,
                "device-session-time",
                0,
                new EmptyRequestDto(),
                "idempotency-time",
                null,
                "not-a-date");

            Assert.Throws<BackendContractValidationException>(command.Validate);
        }

        [Test]
        public void CommandEnvelope_MissingRequiredField_IsRejected()
        {
            var command = new CommandEnvelope<EmptyRequestDto>(
                "command-013",
                string.Empty,
                "0.13.0",
                BackendPlatform.Ios,
                "device-session-013",
                0,
                new EmptyRequestDto(),
                "idempotency-013");

            Assert.Throws<BackendContractValidationException>(command.Validate);
        }

        [Test]
        public void CommandEnvelope_MissingPayload_IsRejected()
        {
            var command = new CommandEnvelope<EmptyRequestDto>(
                "command-013",
                "request-013",
                "0.13.0",
                BackendPlatform.Ios,
                "device-session-013",
                0,
                null,
                "idempotency-013");

            Assert.Throws<BackendContractValidationException>(command.Validate);
        }

        [Test]
        public void WalletSerialization_UsesInt64BeyondInt32()
        {
            var wallet = new WalletSnapshotDto
            {
                gold = (long)int.MaxValue + 100,
                gemsAvailable = 5_000_000_000L,
                gemsHeld = 4_000_000_000L,
                revision = 77
            };

            string json = JsonUtility.ToJson(wallet);
            WalletSnapshotDto restored = JsonUtility.FromJson<WalletSnapshotDto>(json);

            Assert.That(restored.gold, Is.EqualTo((long)int.MaxValue + 100));
            Assert.That(restored.gemsAvailable, Is.EqualTo(5_000_000_000L));
            Assert.That(restored.gemsHeld, Is.EqualTo(4_000_000_000L));
        }

        [Test]
        public void PersistedContractEnums_HaveExplicitStableValues()
        {
            Assert.That((int)BackendRarity.Common, Is.EqualTo(0));
            Assert.That((int)BackendRarity.Uncommon, Is.EqualTo(1));
            Assert.That((int)BackendRarity.Rare, Is.EqualTo(2));
            Assert.That((int)BackendRarity.Epic, Is.EqualTo(3));
            Assert.That((int)BackendRarity.Legendary, Is.EqualTo(4));
            Assert.That((int)BackendRarity.Mythic, Is.EqualTo(5));

            Assert.That((int)BackendMarketListingState.Draft, Is.EqualTo(0));
            Assert.That((int)BackendMarketListingState.Active, Is.EqualTo(1));
            Assert.That((int)BackendMarketListingState.Reserved, Is.EqualTo(2));
            Assert.That((int)BackendMarketListingState.Sold, Is.EqualTo(3));
            Assert.That((int)BackendMarketListingState.Cancelled, Is.EqualTo(4));
            Assert.That((int)BackendMarketListingState.Expired, Is.EqualTo(5));
            Assert.That((int)BackendMarketListingState.Failed, Is.EqualTo(6));
        }

        [Test]
        public void ErrorResponse_RoundTrip_PreservesSafeContract()
        {
            var error = new BackendErrorDto
            {
                code = "REVISION_CONFLICT",
                messageKey = "backend.error.revision_conflict",
                safeMessage = "O estado mudou.",
                details = new BackendErrorDetailsDto
                {
                    resource = "player",
                    fieldErrors = new[]
                    {
                        new BackendFieldErrorDto
                        {
                            field = "expectedRevision",
                            reason = "stale"
                        }
                    }
                },
                retryable = false,
                expectedRevision = 10,
                actualRevision = 11,
                correlationId = "correlation-013"
            };
            var response = new BackendResponse<CommandResultDto>
            {
                requestId = "request-013",
                correlationId = "correlation-013",
                success = false,
                serverTime = "2026-07-25T15:31:23Z",
                newRevision = 11,
                error = error,
                retryable = false,
                rulesVersion = "economy_rules_v1"
            };

            response.Validate();
            string json = JsonUtility.ToJson(response);
            BackendResponse<CommandResultDto> restored =
                JsonUtility.FromJson<BackendResponse<CommandResultDto>>(json);

            restored.Validate();
            Assert.That(restored.error.code, Is.EqualTo("REVISION_CONFLICT"));
            Assert.That(restored.error.actualRevision, Is.EqualTo(11));
            Assert.That(restored.error.details.fieldErrors[0].field,
                Is.EqualTo("expectedRevision"));
        }

        [Test]
        public void SnapshotResponse_PatchOnlySuccess_IsValid()
        {
            var response = new BackendResponse<PlayerSnapshotDto>
            {
                requestId = "request-patch",
                correlationId = "correlation-patch",
                success = true,
                serverTime = "2026-07-25T15:31:23Z",
                newRevision = 482,
                snapshotPatch = new SnapshotPatchDto
                {
                    baseRevision = 481,
                    newRevision = 482,
                    operations = new[]
                    {
                        new SnapshotPatchOperationDto
                        {
                            op = "set",
                            path = "/wallet/gold",
                            valueJson = "8000000001"
                        }
                    }
                },
                retryable = false,
                rulesVersion = "economy_rules_v1"
            };

            response.Validate();
            string json = JsonUtility.ToJson(response);
            BackendResponse<PlayerSnapshotDto> restored =
                JsonUtility.FromJson<BackendResponse<PlayerSnapshotDto>>(json);

            restored.Validate();
            Assert.That(restored.snapshotPatch, Is.Not.Null);
            Assert.That(restored.snapshotPatch.operations, Has.Length.EqualTo(1));
            Assert.That(
                restored.snapshotPatch.operations[0].path,
                Is.EqualTo("/wallet/gold"));
        }

        [Test]
        public void SnapshotPatch_RoundTrip_PreservesOperationObjects()
        {
            var patch = new SnapshotPatchDto
            {
                baseRevision = 10,
                newRevision = 11,
                operations = new[]
                {
                    new SnapshotPatchOperationDto
                    {
                        op = "set",
                        path = "/energy/current",
                        valueJson = "90"
                    },
                    new SnapshotPatchOperationDto
                    {
                        op = "remove",
                        path = "/featureFlags/legacy_flag"
                    }
                }
            };

            patch.Validate();
            string json = JsonUtility.ToJson(patch);
            SnapshotPatchDto restored = JsonUtility.FromJson<SnapshotPatchDto>(json);

            restored.Validate();
            Assert.That(json, Does.Contain("\"operations\":[{"));
            Assert.That(json, Does.Not.Contain("operationsJson"));
            Assert.That(restored.operations, Has.Length.EqualTo(2));
            Assert.That(restored.operations[0].path, Is.EqualTo("/energy/current"));
            Assert.That(restored.operations[1].op, Is.EqualTo("remove"));
        }

        [Test]
        public void CampaignCompletion_SerializesNestedEvidence()
        {
            var payload = new CampaignCompleteStageRequestDto
            {
                battleId = "battle-013",
                completionEvidence = new BattleCompletionEvidenceDto
                {
                    rulesVersion = "combat_rules_v1",
                    eventLogHash = "hash-013",
                    compactReplay = "replay-013"
                }
            };

            string json = JsonUtility.ToJson(payload);

            Assert.That(json, Does.Contain("\"battleId\":\"battle-013\""));
            Assert.That(json, Does.Contain("\"completionEvidence\":{"));
            Assert.That(
                json,
                Does.Contain("\"rulesVersion\":\"combat_rules_v1\""));
        }

        [Test]
        public void DungeonCompletion_SerializesEvidenceWithoutBattleId()
        {
            var payload = new DungeonCompleteRunRequestDto
            {
                completionEvidence = new BattleCompletionEvidenceDto
                {
                    rulesVersion = "combat_rules_v1",
                    eventLogHash = "hash-013"
                }
            };

            string json = JsonUtility.ToJson(payload);

            Assert.That(json, Does.Contain("\"completionEvidence\":{"));
            Assert.That(json, Does.Not.Contain("battleId"));
        }

        [Test]
        public void ProfessionCommands_SerializeProfessionId()
        {
            var specialization =
                new SelectPrimaryProfessionRequest(BackendProfession.Blacksmith);
            var station =
                new UpgradeProfessionStationRequest(BackendProfession.Enchanter);

            string specializationJson = JsonUtility.ToJson(specialization);
            string stationJson = JsonUtility.ToJson(station);

            Assert.That(
                specializationJson,
                Is.EqualTo("{\"professionId\":\"blacksmith\"}"));
            Assert.That(
                stationJson,
                Is.EqualTo("{\"professionId\":\"enchanter\"}"));
        }

        [Test]
        public void Snapshot_RoundTrip_PreservesRequiredSectionsAndSchema()
        {
            PlayerSnapshotDto snapshot = CreateSnapshot();

            snapshot.Validate();
            string json = JsonUtility.ToJson(snapshot);
            PlayerSnapshotDto restored = JsonUtility.FromJson<PlayerSnapshotDto>(json);

            restored.Validate();
            Assert.That(restored.schemaVersion,
                Is.EqualTo(BackendContractVersions.SnapshotSchemaVersion));
            Assert.That(restored.revision, Is.EqualTo(481));
            Assert.That(restored.wallet.gold, Is.EqualTo(8_000_000_000L));
            Assert.That(restored.heroes, Has.Length.EqualTo(1));
            Assert.That(restored.inventory, Has.Length.EqualTo(1));
            Assert.That(restored.professions, Has.Length.EqualTo(1));
            Assert.That(restored.craftingJobs, Has.Length.EqualTo(1));
            Assert.That(restored.pityStates, Has.Length.EqualTo(1));
            Assert.That(restored.featureFlags, Has.Length.EqualTo(1));
        }

        [Test]
        public void Snapshot_FutureSchemaVersion_IsRejected()
        {
            PlayerSnapshotDto snapshot = CreateSnapshot();
            snapshot.schemaVersion =
                BackendContractVersions.SnapshotSchemaVersion + 1;

            Assert.Throws<BackendContractValidationException>(snapshot.Validate);
        }

        [Test]
        public async Task MockClient_IsExplicitlyInsecureAndDoesNotMutateEconomy()
        {
            PlayerSnapshotDto snapshot = CreateSnapshot();
            var client = new MockGameBackendClient(snapshot);
            var command = new CommandEnvelope<GachaPullRequestDto>(
                "command-pull",
                "request-pull",
                "0.13.0",
                BackendPlatform.Editor,
                "mock-device",
                snapshot.revision,
                new GachaPullRequestDto
                {
                    bannerId = "banner-development",
                    quantity = 1
                },
                "idempotency-pull");

            BackendResponse<CommandResultDto> response =
                await client.PullGachaAsync(command, CancellationToken.None);

            Assert.That(client.IsInsecureDevelopmentMock, Is.True);
            Assert.That(response.success, Is.False);
            Assert.That(response.error.code, Is.EqualTo("MOCK_NOT_CONFIGURED"));
            Assert.That(response.newRevision, Is.EqualTo(snapshot.revision));
        }

        private static PlayerSnapshotDto CreateSnapshot()
        {
            return new PlayerSnapshotDto
            {
                schemaVersion = BackendContractVersions.SnapshotSchemaVersion,
                revision = 481,
                catalogVersion = "catalog_v1",
                playerProfile = new PlayerProfileSnapshotDto
                {
                    playerId = "player-013",
                    displayName = "Contrato",
                    accountPower = 1234,
                    teamPower = 1000
                },
                wallet = new WalletSnapshotDto
                {
                    gold = 8_000_000_000L,
                    gemsAvailable = 500,
                    gemsHeld = 25,
                    revision = 20
                },
                heroes = new[]
                {
                    new HeroSnapshotDto
                    {
                        heroInstanceId = "hero-013",
                        heroDefinitionId = "hero_paladin",
                        level = 10,
                        experience = 1000,
                        rarity = BackendRarity.Epic,
                        ascension = 1,
                        fragmentBalance = 25,
                        power = 3000,
                        version = 3
                    }
                },
                inventory = new[]
                {
                    new ItemSnapshotDto
                    {
                        itemInstanceId = "item-013",
                        definitionId = "equipment_sword_iron_t1",
                        quantity = 1,
                        tier = 1,
                        rarity = BackendRarity.Rare,
                        state = BackendItemState.Owned,
                        version = 2
                    }
                },
                professions = new[]
                {
                    new ProfessionSnapshotDto
                    {
                        profession = BackendProfession.Blacksmith,
                        level = 10,
                        totalExperience = 900,
                        stationTier = 2,
                        focusAvailable = 80,
                        focusMaximum = 100,
                        mythicPityCounter = 0,
                        revision = 4
                    }
                },
                campaign = new CampaignSnapshotDto
                {
                    currentStageId = "stage_002",
                    highestClearedStageId = "stage_001",
                    revision = 5
                },
                energy = new EnergySnapshotDto
                {
                    current = 90,
                    maximum = 100,
                    regenerationAnchorTime = "2026-07-25T15:00:00Z",
                    revision = 6
                },
                craftingJobs = new[]
                {
                    new CraftingJobSnapshotDto
                    {
                        jobId = "job-013",
                        recipeId = "recipe_sword_iron_t1",
                        quantity = 1,
                        status = "Running",
                        completesAt = "2026-07-25T16:00:00Z",
                        version = 1
                    }
                },
                pityStates = new[]
                {
                    new PityStateSnapshotDto
                    {
                        system = "gacha",
                        groupId = "standard",
                        trackId = "mythic",
                        counter = 10,
                        revision = 2
                    }
                },
                featureFlags = new[]
                {
                    new FeatureFlagSnapshotDto
                    {
                        key = "market_enabled",
                        valueJson = "false",
                        version = 1
                    }
                }
            };
        }
    }
}
