using Microsoft.Extensions.Caching.Memory;
using ZiggyCreatures.Caching.Fusion;

namespace FusionCacheTests;

public class QuotationService(
    IFusionCache cache,
    IExternalQuotation client,
    IMemoryCache memoryCache)
{
    public ValueTask<Quotation> GetByTickerIdAsync(
        string tickerId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"quotation:{tickerId.ToUpper()}";

        var duration = IsOpenMarket()
            ? TimeSpan.FromSeconds(10)
            : TimeSpan.FromMinutes(60);

        return cache.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                var external = await client
                    .GetValueAsync(tickerId, ct);

                return external;
            },
            options =>
            {
                options
                    // ⏳ Tempo de vida principal do item no cache.
                    // Após esse período o item expira e precisa ser renovado.
                    .SetDuration(duration)

                    // ⏱️ Define os timeouts de execução do factory (requisição externa).
                    // - softTimeout: tempo máximo de espera "ideal".
                    //   Se o factory não terminar nesse tempo:
                    //     • Se existe cache válido ou stale (fail-safe) → retorna imediatamente esse valor.
                    //     • Se não existe nenhum valor → o FusionCache continua esperando até o hardTimeout.
                    // - hardTimeout: tempo máximo absoluto de execução do factory.
                    //   Se for atingido, o factory é cancelado.
                    .SetFactoryTimeouts(
                        softTimeout: TimeSpan.FromMilliseconds(200),
                        hardTimeout: TimeSpan.FromMilliseconds(600)
                    )

                    // 🔄 Eager Refresh: define uma fração do TTL após a qual o FusionCache
                    // dispara um refresh em background para manter o cache quente.
                    // Exemplo: 0.8f = 80% → se Duration=30s, aos 24s o cache já dispara refresh.
                    .SetEagerRefresh(0.8f)

                    // 🎲 Jittering: adiciona variação aleatória no TTL, positiva ou negativa.
                    // Exemplo: Duration=30s, Jitter=±1s → expira entre 29s e 31s.
                    // Benefício: evita expirações sincronizadas em massa (cache stampede).
                    // Não é simplesmente "acrescentar tempo", é um deslocamento aleatório.
                    .SetJittering(TimeSpan.FromSeconds(1))

                    // 🛡️ Fail-Safe: ativa a devolução de valores stale quando o factory falhar
                    // ou ultrapassar o soft timeout.
                    // - isEnabled: ativa/desativa o fail-safe.
                    // - maxDuration: tempo máximo que um valor stale pode ser usado.
                    // - throttleDuration: tempo mínimo entre tentativas de buscar a fonte externa
                    //   quando ela está instável.
                    .SetFailSafe(
                        isEnabled: true,
                        maxDuration: TimeSpan.FromMinutes(120), // até 2h usando stale
                        throttleDuration: TimeSpan.FromSeconds(60) // tenta de novo no máx. a cada 60s
                    );

            },
            token: cancellationToken);
    }

    private static bool IsOpenMarket()
    {
        var now = DateTime.Now;

        var mercadoAberto = now.DayOfWeek is not DayOfWeek.Saturday
                            && now.DayOfWeek is not DayOfWeek.Sunday
                            && now.TimeOfDay >= TimeSpan.FromHours(9)
                            && now.TimeOfDay < TimeSpan.FromHours(20);

        return mercadoAberto;
    }

    public Task<Quotation?> GetByTickerIdWithFusionAsync(
        string tickerId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"quotation:{tickerId.ToUpper()}";

        return memoryCache.GetOrCreateAsync(
            cacheKey, async entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(10));

                var external = await client
                    .GetValueAsync(tickerId, cancellationToken);

                return external;
            });
    }
}