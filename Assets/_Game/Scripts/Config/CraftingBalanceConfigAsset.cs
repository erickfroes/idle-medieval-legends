using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Equipment;
using UnityEngine;

namespace IdleMedievalLegends.Config
{
    [CreateAssetMenu(
        fileName = "CraftingBalanceConfig",
        menuName = "Idle Medieval Legends/Balance/Crafting Balance Config")]
    public sealed class CraftingBalanceConfigAsset : ScriptableObject
    {
        [SerializeField]
        private ProfessionProgressionTuning professionProgression =
            new ProfessionProgressionTuning();

        [SerializeField]
        private EquipmentBalanceTuning equipmentBalance =
            new EquipmentBalanceTuning();

        [SerializeField]
        private CraftingPityTuning craftingPity = new CraftingPityTuning();

        public ProfessionProgressionTuning ProfessionProgression => professionProgression;
        public EquipmentBalanceTuning EquipmentBalance => equipmentBalance;
        public CraftingPityTuning CraftingPity => craftingPity;
    }
}
