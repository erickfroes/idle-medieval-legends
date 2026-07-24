using System;
using System.Collections.Generic;

namespace IdleMedievalLegends.Domain.Campaign
{
    public sealed class IdleRewardCalculator
    {
        public TimeValidationResult ValidateTime(
            long startUnixMilliseconds,
            long endUnixMilliseconds,
            long maximumAbsoluteJumpMilliseconds,
            long safeJumpLimitMilliseconds)
        {
            if (maximumAbsoluteJumpMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumAbsoluteJumpMilliseconds));
            if (safeJumpLimitMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(safeJumpLimitMilliseconds));

            if (startUnixMilliseconds <= 0 || endUnixMilliseconds <= 0)
            {
                return new TimeValidationResult(
                    TimeValidationCode.MissingTimestamp,
                    startUnixMilliseconds,
                    endUnixMilliseconds,
                    0,
                    0,
                    "Timestamp ausente; sessão inicializada sem recompensa retroativa.");
            }

            if (endUnixMilliseconds < startUnixMilliseconds)
            {
                return new TimeValidationResult(
                    TimeValidationCode.ClockRegression,
                    startUnixMilliseconds,
                    endUnixMilliseconds,
                    0,
                    0,
                    "Regressão do relógio local detectada; recompensa zerada.");
            }

            long elapsed = checked(endUnixMilliseconds - startUnixMilliseconds);
            if (elapsed > maximumAbsoluteJumpMilliseconds)
            {
                return new TimeValidationResult(
                    TimeValidationCode.ExtremeJumpLimited,
                    startUnixMilliseconds,
                    endUnixMilliseconds,
                    elapsed,
                    Math.Min(elapsed, safeJumpLimitMilliseconds),
                    "Salto extremo do relógio local detectado; limite seguro aplicado.");
            }

            return new TimeValidationResult(
                TimeValidationCode.Valid,
                startUnixMilliseconds,
                endUnixMilliseconds,
                elapsed,
                elapsed,
                string.Empty);
        }

        public OfflineRewardReport Calculate(
            OfflineSession session,
            IdleProductionProfile profile,
            TimeValidationResult timeValidation)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (timeValidation == null) throw new ArgumentNullException(nameof(timeValidation));
            if (!string.Equals(session.StageId, profile.StageId, StringComparison.Ordinal))
                throw new InvalidOperationException("Sessão e perfil usam estágios diferentes.");

            long validated = timeValidation.ValidatedElapsedMilliseconds;
            long eligible = Math.Min(
                validated,
                Math.Min(
                    profile.PlayerOfflineLimitMilliseconds,
                    profile.StageOfflineLimitMilliseconds));
            long discarded = Math.Max(0, timeValidation.ElapsedMilliseconds - eligible);
            long gold = CalculateRate(
                profile.GoldPerMinute,
                eligible,
                profile.AllowedMultipliers);
            long experience = CalculateRate(
                profile.AccountExperiencePerMinute,
                eligible,
                profile.AllowedMultipliers);
            var materials = new List<CampaignMaterialReward>(
                profile.MaterialsPerMinute.Count);
            for (int i = 0; i < profile.MaterialsPerMinute.Count; i++)
            {
                CampaignMaterialReward rate = profile.MaterialsPerMinute[i];
                long quantity = CalculateRate(
                    rate.Quantity,
                    eligible,
                    profile.AllowedMultipliers);
                if (quantity > 0)
                    materials.Add(new CampaignMaterialReward(
                        rate.MaterialDefinitionId,
                        quantity));
            }

            return new OfflineRewardReport(
                session.StartUnixMilliseconds,
                session.EndUnixMilliseconds,
                timeValidation.ElapsedMilliseconds,
                eligible,
                discarded,
                session.StageId,
                gold,
                materials,
                experience,
                profile.AllowedMultipliers,
                session.Revision,
                session.RequestId,
                false,
                timeValidation.Code,
                timeValidation.Warning);
        }

        public static long CalculateRate(
            long ratePerMinute,
            long durationMilliseconds,
            IReadOnlyList<IdleRewardMultiplier> multipliers)
        {
            if (ratePerMinute < 0) throw new ArgumentOutOfRangeException(nameof(ratePerMinute));
            if (durationMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
            if (ratePerMinute == 0 || durationMilliseconds == 0)
                return 0;

            long value = checked(ratePerMinute * durationMilliseconds / 60000L);
            if (multipliers == null)
                return value;
            for (int i = 0; i < multipliers.Count; i++)
            {
                IdleRewardMultiplier multiplier = multipliers[i] ??
                    throw new InvalidOperationException("Multiplicador nulo.");
                value = checked(value * multiplier.BasisPoints / 10000L);
            }
            return value;
        }
    }
}
