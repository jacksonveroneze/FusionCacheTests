using FusionCacheTests.Domain;

namespace FusionCacheTests.Application.Interfaces;

public interface IExternalCacheRepository
{
    ValueTask<Quotation?> GetByTickerIdWithFusionAsync(
        string tickerId,
        CancellationToken cancellationToken = default);

    Task<Quotation?> GetByTickerIdWithoutFusionAsync(
        string tickerId,
        CancellationToken cancellationToken = default);

    ValueTask<Cms?> GetContentByIdWithFusionAsync(
        string contentId,
        string faultMode,
        CancellationToken cancellationToken = default);

    Task<Cms?> GetContentByIdWithoutFusionAsync(
        string contentId,
        string faultMode,
        CancellationToken cancellationToken = default);
}