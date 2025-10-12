using System.Diagnostics;
using FusionCacheTests.Application.Enums;

namespace FusionCacheTests.Application.Contracts;

[DebuggerDisplay("{Ttl,nq}")]
public sealed record TtlComputeResult(
    TimeSpan Ttl,
    DateTimeOffset NextExpectedOpenLocal,
    MarketClockState MarketState
);