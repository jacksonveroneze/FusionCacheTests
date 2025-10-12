using FusionCacheTests.Application.Contracts;
using FusionCacheTests.Application.Enums;

namespace FusionCacheTests.Application.Policies;

public sealed class MarketTtlCalculator(
    ITradingCalendar calendar) : ICacheTtlPolicy
{
    public async Task<TtlComputeResult> ComputeAsync(
        DateTimeOffset currentTime,
        TimeSpan defaultOpenedTtl,
        CancellationToken cancellationToken)
    {
        var schedule = await calendar.GetScheduleAsync(currentTime, cancellationToken);
        var isBusinessDay = await calendar.IsBusinessDayAsync(currentTime, cancellationToken);
        var nextOpenLocal = await calendar.GetNextMarketDateAsync(currentTime, cancellationToken);

        var tod = currentTime.TimeOfDay;
        var isOpenByClock = isBusinessDay
                            && tod >= schedule.MarketOpen
                            && tod < schedule.MarketClose;

        if (isOpenByClock)
        {
            var ttl = NormalizeTtl(defaultOpenedTtl);

            return new TtlComputeResult(
                ttl,
                nextOpenLocal,
                MarketClockState.OpenByClock
            );
        }

        var delta = nextOpenLocal - currentTime;

        var closedTtl = NormalizeTtl(delta);

        return new TtlComputeResult(
            closedTtl,
            nextOpenLocal,
            MarketClockState.ClosedByClock
        );
    }

    private static TimeSpan NormalizeTtl(TimeSpan ttl)
    {
        return (ttl > TimeSpan.FromSeconds(1)) ? ttl : TimeSpan.FromSeconds(1);
    }
}