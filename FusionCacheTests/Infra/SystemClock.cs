using FusionCacheTests.Application.Contracts;

namespace FusionCacheTests.Infra;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;


    public DateTimeOffset NowInSaoPaulo()
    {
        // Nunca use DateTime.Now; use UTC e converta.
        var utcNow = UtcNow;

        // Linux/macOS (Docker): IANA "America/Sao_Paulo"
        // Windows (máquina dev): pode usar IANA também no .NET 8+ em Linux; no Windows clássico, use fallback se necessário.
        TimeZoneInfo tz;

        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }

        var nowSaoPaulo = TimeZoneInfo.ConvertTime(utcNow, tz);

        return nowSaoPaulo;
    }
}