using System.Diagnostics;

namespace FusionCacheTests;

// Estado calculado apenas por relógio (Seg–Sex, 10:00–18:00)
public enum MarketClockState
{
    OpenByClock,
    ClosedByClock
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed record TtlComputeResult(
    TimeSpan Ttl,
    DateTimeOffset NextExpectedOpenLocal, // timestamp no mesmo offset de nowLocal
    MarketClockState MarketState,
    string PolicyTag
)
{
    public TimeSpan DebuggerDisplay => Ttl;
}

public sealed record TtlPolicyOptions(
    TimeSpan MarketOpen,
    TimeSpan MarketClose,
    TimeSpan OpenTtl);