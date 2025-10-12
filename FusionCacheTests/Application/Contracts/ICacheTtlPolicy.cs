namespace FusionCacheTests.Application.Contracts;

public interface ICacheTtlPolicy
{
    Task<TtlComputeResult> ComputeAsync(
        DateTimeOffset currentTime, 
        TimeSpan defaultOpenedTtl,
        CancellationToken cancellationToken);
}