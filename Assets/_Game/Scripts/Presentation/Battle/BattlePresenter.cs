using System;
using IdleMedievalLegends.Domain.Combat;

namespace IdleMedievalLegends.Presentation.Battle
{
    public sealed class BattleSpeedController
    {
        private static readonly int[] SupportedSpeeds = { 1, 2, 3 };
        private int speedIndex;

        public event Action<int> SpeedChanged;

        public int Speed => SupportedSpeeds[speedIndex];

        public int Cycle()
        {
            speedIndex = (speedIndex + 1) % SupportedSpeeds.Length;
            SpeedChanged?.Invoke(Speed);
            return Speed;
        }

        public void Set(int speed)
        {
            for (int i = 0; i < SupportedSpeeds.Length; i++)
            {
                if (SupportedSpeeds[i] != speed)
                    continue;

                speedIndex = i;
                SpeedChanged?.Invoke(Speed);
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(speed));
        }
    }

    public sealed class BattlePresenter
    {
        public BattlePresenter(BattleSpeedController speedController = null)
        {
            SpeedController = speedController ?? new BattleSpeedController();
        }

        public BattlePresentationState State { get; private set; } =
            BattlePresentationState.Uninitialized;
        public BattleResult Result { get; private set; }
        public BattleSpeedController SpeedController { get; }

        public void Prepare(BattleResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (State != BattlePresentationState.Uninitialized)
                throw new InvalidOperationException("A apresentação já foi inicializada.");

            Result = result;
            State = BattlePresentationState.Preparing;
        }

        public void Play()
        {
            RequireState(BattlePresentationState.Preparing, BattlePresentationState.Paused);
            State = BattlePresentationState.Playing;
        }

        public void Pause()
        {
            RequireState(BattlePresentationState.Playing);
            State = BattlePresentationState.Paused;
        }

        public bool BeginSkip()
        {
            if (State == BattlePresentationState.Completed ||
                State == BattlePresentationState.Faulted ||
                State == BattlePresentationState.Skipping)
            {
                return false;
            }

            if (Result == null)
                return false;

            State = BattlePresentationState.Skipping;
            return true;
        }

        public void Complete()
        {
            if (Result == null)
                throw new InvalidOperationException("Não há resultado para concluir.");
            if (State == BattlePresentationState.Faulted)
                throw new InvalidOperationException("Uma apresentação com falha não conclui.");

            State = BattlePresentationState.Completed;
        }

        public void Fault()
        {
            State = BattlePresentationState.Faulted;
        }

        private void RequireState(params BattlePresentationState[] allowed)
        {
            for (int i = 0; i < allowed.Length; i++)
            {
                if (State == allowed[i])
                    return;
            }

            throw new InvalidOperationException(
                $"Transição inválida a partir do estado {State}.");
        }
    }
}
