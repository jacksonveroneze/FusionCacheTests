using FusionCacheTests.Application.Contracts;
using FusionCacheTests.Application.Interfaces;
using FusionCacheTests.Application.Policies;
using FusionCacheTests.Domain;
using JacksonVeroneze.NET.Cache.Interfaces;
using ZiggyCreatures.Caching.Fusion;

namespace FusionCacheTests.Infra;

public class QuotationCacheRepository(
    IFusionCacheProvider cache,
    ICacheService cacheService,
    IExternalQuotation externalQuotation) : IQuotationCacheRepository
{
    private readonly TimeSpan _duration = TimeSpan.FromSeconds(10);

    public async ValueTask<Quotation> GetByTickerIdAsync(
        string tickerId,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(tickerId);

        var cacheService = cache.GetCache("Quotation");
        var cacheService2 = cache.GetCache("Teste");

        await cacheService.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                return new Quotation(tickerId, 100);
            },
            token: cancellationToken);
        
        return await cacheService2.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                return new Quotation(tickerId, 100);
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