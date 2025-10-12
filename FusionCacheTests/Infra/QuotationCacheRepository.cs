using FusionCacheTests.Application.Contracts;
using FusionCacheTests.Application.Interfaces;
using FusionCacheTests.Application.Policies;
using FusionCacheTests.Domain;
using JacksonVeroneze.NET.Cache.Interfaces;
using ZiggyCreatures.Caching.Fusion;

namespace FusionCacheTests.Infra;

public class QuotationCacheRepository(
    IFusionCache cache,
    ICacheService cacheService,
    IExternalQuotation externalQuotation) : IQuotationCacheRepository
{
    private readonly TimeSpan _duration = TimeSpan.FromSeconds(10);

    public ValueTask<Quotation> GetByTickerIdAsync(
        string tickerId,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(tickerId);

        return cache.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                var external = await externalQuotation
                    .GetByTickerIdAsync(tickerId, ct);

                return external;
            },
            options =>
            {
                options
                    // ⏳ Tempo de vida principal do item no cache.
                    // Após esse período o item expira e precisa ser renovado.
                    .SetDuration(ttl)

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
                    .SetEagerRefresh(0.5f)

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
                        maxDuration: TimeSpan.FromMinutes(30), // até 1m usando stale
                        throttleDuration: TimeSpan.FromSeconds(30) // tenta de novo no máx. a cada 60s
                    )
                    .SetDistributedCacheTimeouts(
                        softTimeout: TimeSpan.FromMilliseconds(200),
                        hardTimeout: TimeSpan.FromMilliseconds(600)
                    );
            },
            token: cancellationToken);
    }

    public Task<Quotation?> GetByTickerIdWithoutFusionAsync(
        string tickerId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(tickerId);

        cacheService.WithPrefixKey("quotation_cache");

        return cacheService.GetOrCreateAsync(
            cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _duration;

                var external = await externalQuotation
                    .GetByTickerIdAsync(tickerId, cancellationToken);

                return external;
            }, cancellationToken);
    }

    private static string GetCacheKey(string tickerId) =>
        $"quotation:{tickerId.ToUpper()}";
}