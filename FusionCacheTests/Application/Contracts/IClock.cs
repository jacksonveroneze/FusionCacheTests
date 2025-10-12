namespace FusionCacheTests.Application.Contracts;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateTimeOffset NowInSaoPaulo();
}