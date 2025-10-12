using FusionCacheTests.Application.Contracts;
using FusionCacheTests.Application.Interfaces;

namespace FusionCacheTests.Application.UseCases;

public class TickersQuotationUseCase(
    IClock clock,
    ICacheTtlPolicy ttlPolicy,
    IQuotationCacheRepository cacheRepo) : ITickersQuotationUseCase
{
    private readonly ICollection<string> _tickers =
    [
        "PETR4",
        "VALE3",
        "ITUB4"
    ];

    public async Task<ICollection<TickerQuotation>> GetDataAsync(
        CancellationToken cancellationToken)
    {
        var nowLocal = clock.NowInSaoPaulo();

        var ttlResult = await ttlPolicy.ComputeAsync(
            nowLocal, defaultOpenedTtl: TimeSpan.FromSeconds(15), cancellationToken);

        var tasks = _tickers.Select(tickerId =>
                cacheRepo.GetByTickerIdAsync(
                    tickerId,
                    ttlResult.Ttl,
                    cancellationToken).AsTask())
            .ToArray();

        var quotations = await Task.WhenAll(tasks);

        var result = quotations
            .Select(quotation => new TickerQuotation(
                quotation.TickerId,
                quotation.Value))
            .ToList();

        return result;
    }
}