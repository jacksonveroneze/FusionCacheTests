using FusionCacheTests.Domain;
using Refit;

namespace FusionCacheTests.Infra;

public interface IExternalService
{
    [Get("/quotations/{tickerId}")]
    Task<Quotation> GetTickerByIdAsync(
        [AliasAs("tickerId")] string ticker,
        CancellationToken cancellationToken = default);

    [Get("/cms/{contentId}")]
    Task<Cms> GetContentByIdAsync(
        [AliasAs("contentId")] string contentId,
        [Query("faultMode")] string faultMode,
        CancellationToken cancellationToken = default);
}