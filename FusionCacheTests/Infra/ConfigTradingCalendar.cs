using FusionCacheTests.Application.Contracts;

namespace FusionCacheTests.Infra;

public sealed class ConfigTradingCalendar : ITradingCalendar
{
    private readonly TimeSpan _open = TimeSpan.FromHours(10);
    private readonly TimeSpan _close = TimeSpan.FromHours(18);

    public Task<MarketDaySchedule> GetScheduleAsync(
        DateTimeOffset localDate, 
        CancellationToken cancellationToken)
        => Task.FromResult(new MarketDaySchedule(_open, _close, IsWeekend(localDate)));

    public Task<bool> IsBusinessDayAsync(
        DateTimeOffset localDate, 
        CancellationToken cancellationToken)
        => Task.FromResult(!IsWeekend(localDate));

    private static bool IsWeekend(DateTimeOffset d)
        => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public Task<DateTimeOffset> GetNextMarketDateAsync(
        DateTimeOffset localDate, 
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(localDate.Date);
        var isWeekday = IsWeekday(today);

        // Se hoje é dia útil e ainda não abriu → HOJE às Open
        if (isWeekday && localDate.TimeOfDay < _open)
            return Task.FromResult(AtLocalTime(localDate.Offset, localDate.Date, _open));

        // Caso contrário → achar o próximo dia útil e retornar às Open
        var probe = today;
        do
        {
            probe = probe.AddDays(1);
        } while (!IsWeekday(probe));

        var nextOpen = AtLocalTime(localDate.Offset, probe.ToDateTime(TimeOnly.MinValue), _open);
        return Task.FromResult(nextOpen);
    }

    private static bool IsWeekday(DateOnly d)
        => d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    private static DateTimeOffset AtLocalTime(TimeSpan offset, DateTime dateLocal, TimeSpan timeLocal)
        => new(dateLocal + timeLocal, offset);
}