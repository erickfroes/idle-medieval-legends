using System;
using IdleMedievalLegends.Domain.Combat;

namespace IdleMedievalLegends.Presentation.Battle
{
    /// <summary>
    /// Passagem efêmera de um cenário já resolvido para a cena Battle.
    /// Não persiste nem recalcula combate.
    /// </summary>
    public static class BattleScenarioBridge
    {
        private static BattleDebugScenario scenario;
        private static Action<BattleResult> completion;

        public static bool HasScenario => scenario != null;

        public static void Publish(
            BattleDebugScenario value,
            Action<BattleResult> onCompleted)
        {
            if (scenario != null)
                throw new InvalidOperationException("Já existe cenário visual pendente.");
            scenario = value ?? throw new ArgumentNullException(nameof(value));
            completion = onCompleted ??
                throw new ArgumentNullException(nameof(onCompleted));
        }

        public static bool TryGetScenario(out BattleDebugScenario value)
        {
            value = scenario;
            return value != null;
        }

        public static void Complete(BattleResult result)
        {
            if (scenario == null)
                return;
            Action<BattleResult> callback = completion;
            scenario = null;
            completion = null;
            callback?.Invoke(result);
        }

        public static void Clear()
        {
            scenario = null;
            completion = null;
        }
    }
}
