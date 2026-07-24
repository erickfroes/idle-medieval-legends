using System;
using System.Diagnostics;
using IdleMedievalLegends.Domain.Campaign;

namespace IdleMedievalLegends.Application
{
    /// <summary>
    /// Relógio UTC local do protótipo. Não é autoridade econômica de produção.
    /// </summary>
    public sealed class LocalGameClock : IGameClock
    {
        private readonly long initialUnixMilliseconds;
        private readonly Stopwatch elapsed = Stopwatch.StartNew();

        public LocalGameClock()
            : this(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        {
        }

        public LocalGameClock(long initialUnixMilliseconds)
        {
            if (initialUnixMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(initialUnixMilliseconds));
            this.initialUnixMilliseconds = initialUnixMilliseconds;
        }

        public long UtcNowUnixMilliseconds =>
            checked(initialUnixMilliseconds + elapsed.ElapsedMilliseconds);
        public bool IsAuthoritative => false;
        public string Source => "local_utc_monotonic";
    }

    /// <summary>
    /// Relógio manual permitido apenas em Editor, Development Build e testes.
    /// </summary>
    public sealed class DevelopmentGameClock : IGameClock
    {
        private long nowUnixMilliseconds;

        public DevelopmentGameClock(long initialUnixMilliseconds)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS)
            throw new PlatformNotSupportedException(
                "Simulação temporal disponível somente em development builds.");
#else
            if (initialUnixMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(initialUnixMilliseconds));
            nowUnixMilliseconds = initialUnixMilliseconds;
#endif
        }

        public long UtcNowUnixMilliseconds => nowUnixMilliseconds;
        public bool IsAuthoritative => false;
        public string Source => "development_manual";

        public void AdvanceHours(long hours)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS)
            throw new PlatformNotSupportedException(
                "Simulação temporal disponível somente em development builds.");
#else
            if (hours < 0) throw new ArgumentOutOfRangeException(nameof(hours));
            nowUnixMilliseconds = checked(nowUnixMilliseconds + hours * 60L * 60L * 1000L);
#endif
        }

        public void SetForTest(long unixMilliseconds)
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS)
            throw new PlatformNotSupportedException(
                "Simulação temporal disponível somente em development builds.");
#else
            if (unixMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(unixMilliseconds));
            nowUnixMilliseconds = unixMilliseconds;
#endif
        }
    }
}
