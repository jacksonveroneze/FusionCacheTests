namespace FusionCacheTests.Application.Contracts;

public sealed record TtlPolicyOptions(
    TimeSpan MarketOpen,
    TimeSpan MarketClose,
    TimeSpan OpenTtl);