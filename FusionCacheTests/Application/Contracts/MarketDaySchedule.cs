namespace FusionCacheTests.Application.Contracts;

public sealed record MarketDaySchedule(
    TimeSpan MarketOpen,
    TimeSpan MarketClose,
    bool IsHoliday);