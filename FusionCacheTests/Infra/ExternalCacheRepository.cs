using FusionCacheTests.Application.Interfaces;
using FusionCacheTests.Domain;
using JacksonVeroneze.NET.Cache.Interfaces;
using ZiggyCreatures.Caching.Fusion;

namespace FusionCacheTests.Infra;

public class ExternalCacheRepository(
    IFusionCache cacheInstance,
    IFusionCacheProvider cacheInstanceProvider,
    ICacheService cacheService,
    IExternalService externalService) : IExternalCacheRepository
{
    private readonly TimeSpan _duration = TimeSpan.FromMilliseconds(100);

    #region Quotation

    public ValueTask<Quotation?> GetByTickerIdWithFusionAsync(
        string tickerId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetQuotationCacheKey(tickerId);

        return cacheInstance.GetOrSetAsync(
            cacheKey,
            ct =>
            {
                var external = externalService
                    .GetTickerByIdAsync(tickerId, ct);

                return external;
            },
            token: cancellationToken)!;
    }

    public Task<Quotation?> GetByTickerIdWithoutFusionAsync(
        string tickerId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetQuotationCacheKey(tickerId);

        cacheService.WithPrefixKey("quotation_cache");

        return cacheService.GetOrCreateAsync(
            cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _duration;

                var external = await externalService
                    .GetTickerByIdAsync(tickerId, cancellationToken);

                return external;
            }, cancellationToken);
    }

    private static string GetQuotationCacheKey(string tickerId) =>
        $"quotation:{tickerId.ToUpper()}";

    #endregion

    #region Cms

    public ValueTask<Cms?> GetContentByIdWithFusionAsync(
        string contentId,
        string faultMode,
        string skipCache,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCmsCacheKey(contentId);
        
        var instanceFusionCache = cacheInstanceProvider.GetCache("Cms");
        
        return instanceFusionCache.GetOrSetAsync(
            cacheKey,
            ct =>
            {
                var external = externalService
                    .GetContentByIdAsync(contentId, faultMode, ct);

                return external;
            },
            token: cancellationToken)!;
    }

    public Task<Cms?> GetContentByIdWithoutFusionAsync(
        string contentId,
        string faultMode,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCmsCacheKey(contentId);

        cacheService.WithPrefixKey("content_cache");

        return cacheService.GetOrCreateAsync(
            cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _duration;

                var external = await externalService
                    .GetContentByIdAsync(contentId, faultMode, cancellationToken);

                return external;
            }, cancellationToken);
    }

    private static string GetCmsCacheKey(string contentId) =>
        $"content:{contentId.ToUpper()}";

    #endregion
}