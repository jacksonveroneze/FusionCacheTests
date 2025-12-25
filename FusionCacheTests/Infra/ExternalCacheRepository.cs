using FusionCacheTests.Application.Interfaces;
using FusionCacheTests.Domain;
using JacksonVeroneze.NET.Cache.Interfaces;
using ZiggyCreatures.Caching.Fusion;

namespace FusionCacheTests.Infra;

public class ExternalCacheRepository(
    IFusionCache cacheInstance,
    ICacheService cacheService,
    IExternalService externalService) : IExternalCacheRepository
{
    private readonly TimeSpan _duration = TimeSpan.FromSeconds(30);

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
            options =>
            {
                options
                    .SetDuration(duration: TimeSpan.FromMilliseconds(200))
                    .SetJittering(TimeSpan.FromSeconds(1))
                    .SetEagerRefresh(0.5f)
                    .SetFailSafe(
                        isEnabled: true,
                        maxDuration: TimeSpan.FromMinutes(30),
                        throttleDuration: TimeSpan.FromSeconds(30)
                    )
                    .SetFactoryTimeouts(
                        softTimeout: TimeSpan.FromMilliseconds(200),
                        hardTimeout: TimeSpan.FromMilliseconds(600)
                    )
                    .SetDistributedCacheTimeouts(
                        softTimeout: TimeSpan.FromMilliseconds(200),
                        hardTimeout: TimeSpan.FromMilliseconds(600)
                    );
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
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCmsCacheKey(contentId);

        return cacheInstance.GetOrSetAsync(
            cacheKey,
            ct =>
            {
                var external = externalService
                    .GetContentByIdAsync(contentId, faultMode, ct);

                return external;
            },
            options =>
            {
                options
                    .SetDuration(duration: TimeSpan.FromSeconds(30))
                    .SetJittering(TimeSpan.FromSeconds(1))
                    .SetEagerRefresh(0.8f)
                    .SetFailSafe(
                        isEnabled: true,
                        maxDuration: TimeSpan.FromMinutes(5),
                        throttleDuration: TimeSpan.FromSeconds(10)
                    )
                    .SetFactoryTimeouts(
                        softTimeout: TimeSpan.FromMilliseconds(200),
                        hardTimeout: TimeSpan.FromMilliseconds(2_000)
                    )
                    .SetDistributedCacheTimeouts(
                        softTimeout: TimeSpan.FromMilliseconds(500),
                        hardTimeout: TimeSpan.FromMilliseconds(1_000)
                    );
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