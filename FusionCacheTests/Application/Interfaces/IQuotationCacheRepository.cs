using FusionCacheTests.Domain;

namespace FusionCacheTests.Application.Interfaces;

public interface IQuotationCacheRepository
{
    ValueTask<Quotation> GetByTickerIdAsync(
        string tickerId,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);
}