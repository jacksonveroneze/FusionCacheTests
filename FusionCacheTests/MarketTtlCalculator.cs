namespace FusionCacheTests;

public sealed class MarketTtlCalculator(TtlPolicyOptions options)
{
    
    
    private readonly TtlPolicyOptions _opt =
        options ?? throw new ArgumentNullException(nameof(options));

    public TtlComputeResult Compute(DateTimeOffset nowLocal)
    {
        var isBusinessDay = nowLocal.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
        var tod = nowLocal.TimeOfDay;
        var isOpenByClock = isBusinessDay && tod >= _opt.MarketOpen && tod < _opt.MarketClose;

        var nextOpenLocal = ComputeNextOpen(nowLocal);

        if (isOpenByClock)
        {
            // Dentro do pregão: TTL fixo de 15s (sem jitter)
            var ttl = _opt.OpenTtl;
            if (ttl < TimeSpan.FromSeconds(1)) ttl = TimeSpan.FromSeconds(1);

            return new TtlComputeResult(
                ttl,
                nextOpenLocal,
                MarketClockState.OpenByClock,
                $"Open:{_opt.OpenTtl.TotalSeconds:F0}s"
            );
        }

        // Fora do pregão: TTL = tempo até a próxima abertura (sem jitter)
        var delta = nextOpenLocal - nowLocal;
        var closedTtl = delta <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : delta;

        return new TtlComputeResult(
            closedTtl,
            nextOpenLocal,
            MarketClockState.ClosedByClock,
            "Closed:UntilNextOpen"
        );
    }

    private DateTimeOffset ComputeNextOpen(DateTimeOffset nowLocal)
    {
        // Se hoje é dia útil e ainda não abriu → hoje às 10:00
        var isBusinessDay = nowLocal.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
        if (isBusinessDay && nowLocal.TimeOfDay < _opt.MarketOpen)
            return AtLocalTime(nowLocal.Date, _opt.MarketOpen, nowLocal.Offset);

        // Caso contrário (após 18:00 ou fim de semana), procurar próximo dia útil e usar 10:00
        var date = nowLocal.Date;
        do
        {
            date = date.AddDays(1);
        } while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);

        return AtLocalTime(date, _opt.MarketOpen, nowLocal.Offset);
    }

    private static DateTimeOffset AtLocalTime(DateTime dateLocal, TimeSpan timeLocal, TimeSpan offset)
    {
        var localUnspecified = dateLocal + timeLocal; // DateTimeKind.Unspecified
        return new DateTimeOffset(localUnspecified, offset);
    }
}