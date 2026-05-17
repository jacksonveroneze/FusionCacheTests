using FusionCacheTests;
using FusionCacheTests.Application.Interfaces;
using FusionCacheTests.Domain;
using FusionCacheTests.Factories;
using FusionCacheTests.Infra;
using JacksonVeroneze.NET.DistributedCache.Extensions;
using JacksonVeroneze.NET.HttpClient.Configuration;
using JacksonVeroneze.NET.HttpClient.Extensions;
using JacksonVeroneze.NET.Logging.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Telemetry;
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
    .UseHttpClientMetrics()
    .AddResilienceHandler("custom", pipeline =>
    {
        pipeline.AddTimeout(TimeSpan.FromSeconds(1));

        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 1,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromMilliseconds(100)
        });

        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions()
        {
            FailureRatio = 0.1,
            SamplingDuration = TimeSpan.FromSeconds(1),
            MinimumThroughput = 3,
            BreakDuration = TimeSpan.FromSeconds(30)
        });

        var telemetryOptions = new TelemetryOptions
        {
            LoggerFactory = LoggerFactory.Create(builder1 => builder1.AddConsole())
        };

        pipeline.ConfigureTelemetry(telemetryOptions);
    });

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "172.17.0.1:16379";
    options.InstanceName = "FusionCacheTests";
});

builder.Services
    .AddFusionCacheSystemTextJsonSerializer();

var cacheQuotationSettings =
    builder.Configuration
        .GetSection("Cache:Quotation")
        .Get<FusionCacheEntryOptionsSettings>()
    ?? new FusionCacheEntryOptionsSettings();

var cacheCmsSettings =
    builder.Configuration
        .GetSection("Cache:Cms")
        .Get<FusionCacheEntryOptionsSettings>()
    ?? new FusionCacheEntryOptionsSettings();

builder.Services
    .AddFusionCache(cacheName: "Quotation")
    .WithRegisteredSerializer()
    .WithRegisteredDistributedCache()
    .WithOptions(options => { options.DisableTagging = true; })
    .WithCacheKeyPrefix("KeyPrefix:Quotation:")
    .WithDefaultEntryOptions(options => options.ConfigureOptions(cacheQuotationSettings));

builder.Services
    .AddFusionCache(cacheName: "Cms")
    .WithRegisteredSerializer()
    .WithRegisteredDistributedCache()
    .WithOptions(options => { options.DisableTagging = true; })
    .WithCacheKeyPrefix("KeyPrefix:Content:")
    .WithDefaultEntryOptions(options => options.ConfigureOptions(cacheCmsSettings));

builder.Services
    .AddFusionCache()
    .WithRegisteredSerializer()
    .WithRegisteredDistributedCache()
    .WithOptions(options => { options.DisableTagging = true; })
    .WithDefaultEntryOptions(options => options.ConfigureOptions(new FusionCacheEntryOptionsSettings()));

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
    ValueTask<Quotation?> task = externalCacheRepository
        .GetByTickerIdWithFusionAsync(
            tickerId, cancellationToken)!;

    var result = await task;

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
    [FromQuery] string skipCache,
    CancellationToken cancellationToken) =>
{
    if (useFusion.Equals("false"))
    {
        var result1 = await externalCacheRepository
            .GetContentByIdWithoutFusionAsync(
                contentId, faultMode, cancellationToken)!;

        return Results.Ok(result1);
    }

    Task.Delay(TimeSpan.FromMilliseconds(2000)).Wait();
    
    var result2 = externalCacheRepository
        .GetContentByIdWithFusionAsync(
            contentId, faultMode, skipCache, cancellationToken)!.Result;

    return Results.Ok(result2);
});

app.MapGet("/bff-content-direct", async (
    [FromServices] IExternalService externalService,
    [FromQuery] string contentId,
    [FromQuery] string faultMode,
    [FromQuery] string useFusion,
    CancellationToken cancellationToken) =>
{
    var result= await externalService.GetContentByIdAsync(contentId, faultMode, cancellationToken);

    return Results.Ok(result);
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
// app.UseHttpMetrics();
// app.UseMetricServer();
app.UseDeveloperExceptionPage();
app.UseOpenTelemetryPrometheusScrapingEndpoint("metrics-open");

await app.RunAsync();