namespace FusionCacheTests.Application.Contracts;

public interface ITickersQuotationUseCase
{
    Task<ICollection<TickerQuotation>> GetDataAsync(
        CancellationToken cancellationToken);
}