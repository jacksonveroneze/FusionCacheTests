using ZiggyCreatures.Caching.Fusion;

namespace FusionCacheTests.Factories;

public static class FusionCacheFactory
{
    public static FusionCacheEntryOptions ConfigureOptions(
        this FusionCacheEntryOptions options,
        FusionCacheEntryOptionsSettings settings)
    {
        options.IsFailSafeEnabled = settings.IsFailSafeEnabled;

        options.Duration = settings.Duration;
        options.DistributedCacheDuration = settings.DistributedCacheDuration;
        options.FailSafeMaxDuration = settings.FailSafeMaxDuration;
        options.FailSafeThrottleDuration = settings.FailSafeThrottleDuration;

        options.EagerRefreshThreshold = settings.EagerRefreshThreshold;

        options.JitterMaxDuration = settings.JitterMaxDuration;

        options.FactorySoftTimeout = settings.FactorySoftTimeout;
        options.FactoryHardTimeout = settings.FactoryHardTimeout;
        options.DistributedCacheSoftTimeout = settings.DistributedCacheSoftTimeout;
        options.DistributedCacheHardTimeout = settings.DistributedCacheHardTimeout;

        options.AllowBackgroundDistributedCacheOperations =
            settings.AllowBackgroundDistributedCacheOperations;

        options.AllowTimedOutFactoryBackgroundCompletion =
            settings.AllowTimedOutFactoryBackgroundCompletion;

        return options;
    }
}

public sealed class FusionCacheEntryOptionsSettings
{
    public bool IsFailSafeEnabled { get; init; } = true;

    public TimeSpan Duration { get; init; } = TimeSpan.FromMilliseconds(10);
    public TimeSpan DistributedCacheDuration { get; init; } = TimeSpan.FromMilliseconds(20);
    public TimeSpan FailSafeMaxDuration { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan FailSafeThrottleDuration { get; init; } = TimeSpan.FromMinutes(11);

    public float EagerRefreshThreshold { get; init; } = 0.9f;

    public TimeSpan JitterMaxDuration { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan FactorySoftTimeout { get; init; } = TimeSpan.FromSeconds(13);
    public TimeSpan FactoryHardTimeout { get; init; } = TimeSpan.FromSeconds(14);
    public TimeSpan DistributedCacheSoftTimeout { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan DistributedCacheHardTimeout { get; init; } = TimeSpan.FromSeconds(16);

    public bool AllowBackgroundDistributedCacheOperations { get; init; } = true;
    public bool AllowTimedOutFactoryBackgroundCompletion { get; init; } = true;
}