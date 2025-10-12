using FusionCacheTests.Domain;
using Refit;

namespace FusionCacheTests.Infra;

public interface IExternalQuotation
{
    [Get("/quotations/{tickerId}")]
    Task<Quotation> GetByTickerIdAsync(
        [AliasAs("tickerId")] string ticker,
        CancellationToken cancellationToken=default);
}