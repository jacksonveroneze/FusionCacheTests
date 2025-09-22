using Refit;

namespace FusionCacheTests;

public interface IExternalQuotation
{
    [Get("/quotations/{tickerId}")]
    Task<Quotation> GetValueAsync(
        [AliasAs("tickerId")] string ticker,
        CancellationToken cancellationToken=default);
}