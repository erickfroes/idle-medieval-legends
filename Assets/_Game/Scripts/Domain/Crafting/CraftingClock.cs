namespace IdleMedievalLegends.Domain.Crafting
{
    /// <summary>
    /// Porta de tempo usada pelo crafting. Produção deve fornecer uma implementação
    /// baseada no relógio autoritativo do servidor.
    /// </summary>
    public interface IServerClock
    {
        long UtcNowUnixMilliseconds { get; }
    }

    public sealed class ManualServerClock : IServerClock
    {
        public ManualServerClock(long initialUnixMilliseconds)
        {
            if (initialUnixMilliseconds < 0)
                throw new System.ArgumentOutOfRangeException(nameof(initialUnixMilliseconds));
            UtcNowUnixMilliseconds = initialUnixMilliseconds;
        }

        public long UtcNowUnixMilliseconds { get; private set; }

        public void AdvanceMilliseconds(long milliseconds)
        {
            if (milliseconds < 0)
                throw new System.ArgumentOutOfRangeException(nameof(milliseconds));
            UtcNowUnixMilliseconds = checked(UtcNowUnixMilliseconds + milliseconds);
        }

        public void Set(long unixMilliseconds)
        {
            if (unixMilliseconds < UtcNowUnixMilliseconds)
                throw new System.InvalidOperationException("O relógio não pode regredir.");
            UtcNowUnixMilliseconds = unixMilliseconds;
        }
    }
}
