using FusionCacheTests;
using FusionCacheTests.Application.Interfaces;
using FusionCacheTests.Infra;
using JacksonVeroneze.NET.DistributedCache.Extensions;
using JacksonVeroneze.NET.HttpClient.Configuration;
using JacksonVeroneze.NET.HttpClient.Extensions;
using JacksonVeroneze.NET.Logging.Util;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Serilog;
using ZiggyCreatures.Caching.Fusion;

Log.Logger = BootstrapLogger.CreateLogger();

var builder = WebApplication.CreateSlimBuilder(args);

builder.Host.UseSerilog((hostingContext,
    services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(hostingContext.Configuration)
        .ReadFrom.Services(services);
});

var url = builder.Configuration.GetValue<string>("ExternalServiceUrl");

HttpClientConfiguration config = new()
{
    Name = "ExternalService",
    Address = url,
    TimeOutPolicy = new TimeOutPolicyConfiguration
    {
        TimeOutMs = 3000
    }
};

builder.Services.RefitClientBuilder<IExternalService>(config)
    .UseHttpClientMetrics();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "172.17.0.1:6379";
    options.InstanceName = "FusionCacheTests";
});

builder.Services
    .AddFusionCacheSystemTextJsonSerializer()
    .AddFusionCache(cacheName: "Quotation")
    .WithRegisteredSerializer()
    .WithRegisteredDistributedCache()
    .WithOptions(options => { options.DisableTagging = true; })
    .WithCacheKeyPrefix("KeyPrefix:Quotation:")
    .WithDefaultEntryOptions(options =>
    {
        options.IsFailSafeEnabled = true;
        options.Duration = TimeSpan.FromMinutes(5);
        options.FailSafeMaxDuration = TimeSpan.FromMinutes(10);
        options.FailSafeThrottleDuration = TimeSpan.FromMinutes(11);

        options.EagerRefreshThreshold = 0.9f;

        options.JitterMaxDuration = TimeSpan.FromSeconds(12);

        options.FactorySoftTimeout = TimeSpan.FromSeconds(13);
        options.FactoryHardTimeout = TimeSpan.FromSeconds(14);
        options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(15);
        options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(16);

        options.AllowBackgroundDistributedCacheOperations = true;
        options.AllowTimedOutFactoryBackgroundCompletion = true;
    });

builder.Services
    .AddFusionCacheSystemTextJsonSerializer()
    .AddFusionCache(cacheName: "Cms")
    .WithRegisteredSerializer()
    .WithRegisteredDistributedCache()
    .WithOptions(options => { options.DisableTagging = true; })
    .WithCacheKeyPrefix("KeyPrefix:Content:")
    .WithDefaultEntryOptions(options =>
    {
        options.IsFailSafeEnabled = true;
        options.Duration = TimeSpan.FromMinutes(1);
        options.FailSafeMaxDuration = TimeSpan.FromMinutes(10);
        options.FailSafeThrottleDuration = TimeSpan.FromMinutes(11);

        options.EagerRefreshThreshold = 0.9f;

        options.JitterMaxDuration = TimeSpan.FromSeconds(2);

        options.FactorySoftTimeout = TimeSpan.FromMilliseconds(3_000);
        options.FactoryHardTimeout = TimeSpan.FromMilliseconds(14);
        options.DistributedCacheSoftTimeout = TimeSpan.FromSeconds(1);
        options.DistributedCacheHardTimeout = TimeSpan.FromSeconds(1);

        options.AllowBackgroundDistributedCacheOperations = true;
        options.AllowTimedOutFactoryBackgroundCompletion = true;
    });

builder.Services
    .AddFusionCacheSystemTextJsonSerializer()
    .AddFusionCache()
    .WithRegisteredSerializer()
    .WithRegisteredDistributedCache()
    .WithOptions(options => { options.DisableTagging = true; });

builder.Services.AddDistributedCacheService();

builder.Services.AddTransient<ExternalCacheRepository>();
builder.Services.AddHealthChecks();
builder.Services.AddOpenTelemetryApp();

builder.Services.AddScoped<IExternalCacheRepository, ExternalCacheRepository>();

var app = builder.Build();

app.MapGet("/quotation-with-fusion/{tickerId}", async (
    [FromServices] IExternalCacheRepository externalCacheRepository,
    string tickerId,
    CancellationToken cancellationToken) =>
{
    var result = await externalCacheRepository
        .GetByTickerIdWithFusionAsync(
            tickerId, cancellationToken)!;

    return Results.Ok(result);
});

app.MapGet("/quotation-without-fusion/{tickerId}", async (
    [FromServices] IExternalCacheRepository externalCacheRepository,
    string tickerId,
    CancellationToken cancellationToken) =>
{
    var result = await externalCacheRepository
        .GetByTickerIdWithoutFusionAsync(
            tickerId, cancellationToken)!;

    return Results.Ok(result);
});

app.MapGet("/bff-content", async (
    [FromServices] IExternalCacheRepository externalCacheRepository,
    [FromQuery] string contentId,
    [FromQuery] string faultMode,
    [FromQuery] string useFusion,
    CancellationToken cancellationToken) =>
{
    if (useFusion.Equals("false"))
    {
        var result1 = await externalCacheRepository
            .GetContentByIdWithoutFusionAsync(
                contentId, faultMode, cancellationToken)!;

        return Results.Ok(result1);
    }

    var result2 = await externalCacheRepository
        .GetContentByIdWithFusionAsync(
            contentId, faultMode, cancellationToken)!;

    return Results.Ok(result2);
});

app.MapGet("/content-without-fusion/{contentId}", async (
    [FromServices] IExternalCacheRepository externalCacheRepository,
    [FromRoute] string contentId,
    [FromQuery] string faultMode,
    CancellationToken cancellationToken) =>
{
    var result = await externalCacheRepository
        .GetContentByIdWithoutFusionAsync(
            contentId, faultMode, cancellationToken)!;

    return Results.Ok(result);
});

app.UseHealthChecks("/health");
app.UseHttpMetrics();
app.UseMetricServer();
app.UseDeveloperExceptionPage();
app.UseOpenTelemetryPrometheusScrapingEndpoint("metrics-open");

await app.RunAsync();