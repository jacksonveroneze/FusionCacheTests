namespace FusionCacheTests.Application.Contracts;

public interface ITradingCalendar
{
    /// <summary>Retorna a janela de negociação para a data local informada.</summary>
    Task<MarketDaySchedule> GetScheduleAsync(DateTimeOffset localDate, CancellationToken cancellationToken);

    /// <summary>Indica se é dia útil de negociação (não-fim-de-semana e não-feriado).</summary>
    Task<bool> IsBusinessDayAsync(DateTimeOffset localDate, CancellationToken cancellationToken);

    Task<DateTimeOffset> GetNextMarketDateAsync(DateTimeOffset localDate, CancellationToken cancellationToken);
}