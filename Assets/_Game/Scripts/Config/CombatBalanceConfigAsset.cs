using IdleMedievalLegends.Domain.Combat;
using UnityEngine;

namespace IdleMedievalLegends.Config
{
    [CreateAssetMenu(
        fileName = "CombatBalanceConfig",
        menuName = "Idle Medieval Legends/Balance/Combat Balance Config")]
    public sealed class CombatBalanceConfigAsset : ScriptableObject
    {
        [SerializeField]
        private CombatBalanceTuning tuning = new CombatBalanceTuning();

        public CombatBalanceTuning Tuning => tuning;

        public void EnsureInitialized()
        {
            if (tuning == null)
                tuning = new CombatBalanceTuning();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }
    }
}
