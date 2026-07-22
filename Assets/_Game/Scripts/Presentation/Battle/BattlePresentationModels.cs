using System;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using UnityEngine;

namespace IdleMedievalLegends.Presentation.Battle
{
    public enum BattlePresentationState
    {
        Uninitialized = 0,
        Preparing = 1,
        Playing = 2,
        Paused = 3,
        Skipping = 4,
        Completed = 5,
        Faulted = 6
    }

    [CreateAssetMenu(
        fileName = "BattlePresentationConfig",
        menuName = "Idle Medieval Legends/Presentation/Battle Presentation Config")]
    public sealed class BattlePresentationConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float battleStartDuration = 0.2f;
        [SerializeField, Min(0f)] private float selectionDuration = 0.12f;
        [SerializeField, Min(0f)] private float approachDuration = 0.18f;
        [SerializeField, Min(0f)] private float impactDuration = 0.15f;
        [SerializeField, Min(0f)] private float returnDuration = 0.18f;
        [SerializeField, Min(0f)] private float battleEndDuration = 0.2f;
        [SerializeField, Min(0.1f)] private float attackStopDistance = 1.25f;

        public float BattleStartDuration => battleStartDuration;
        public float SelectionDuration => selectionDuration;
        public float ApproachDuration => approachDuration;
        public float ImpactDuration => impactDuration;
        public float ReturnDuration => returnDuration;
        public float BattleEndDuration => battleEndDuration;
        public float AttackStopDistance => attackStopDistance;

        public void EnsureValid()
        {
            battleStartDuration = Math.Max(0f, battleStartDuration);
            selectionDuration = Math.Max(0f, selectionDuration);
            approachDuration = Math.Max(0f, approachDuration);
            impactDuration = Math.Max(0f, impactDuration);
            returnDuration = Math.Max(0f, returnDuration);
            battleEndDuration = Math.Max(0f, battleEndDuration);
            attackStopDistance = Math.Max(0.1f, attackStopDistance);
        }

        private void OnValidate()
        {
            EnsureValid();
        }
    }

    public sealed class BattleDebugScenario
    {
        public BattleDebugScenario(
            ContentCatalogLookup catalog,
            BattleRequest request,
            BattleResult result)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

        public ContentCatalogLookup Catalog { get; }
        public BattleRequest Request { get; }
        public BattleResult Result { get; }
    }

    public static class BattlePresentationMath
    {
        public static float NormalizeHealth(long currentHealth, long maximumHealth)
        {
            if (maximumHealth <= 0)
                return 0f;

            double ratio = currentHealth / (double)maximumHealth;
            if (ratio <= 0d)
                return 0f;
            return ratio >= 1d ? 1f : (float)ratio;
        }
    }
}
