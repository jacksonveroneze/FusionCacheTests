using FusionCacheTests;
using FusionCacheTests.Application.Contracts;
using FusionCacheTests.Application.Interfaces;
using FusionCacheTests.Application.Policies;
using FusionCacheTests.Application.UseCases;
using FusionCacheTests.Infra;
using JacksonVeroneze.NET.DistributedCache.Extensions;
using JacksonVeroneze.NET.HttpClient.Configuration;
using JacksonVeroneze.NET.HttpClient.Extensions;
using JacksonVeroneze.NET.Logging.Util;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Refit;
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

var url = builder.Configuration.GetValue<string>("QuotationServerUrl");

HttpClientConfiguration config = new()
{
    Name = "ExternalQuotation",
    Address = url,
    TimeOutPolicy = new TimeOutPolicyConfiguration
    {
        TimeOutMs = 3000
    }
};

builder.Services.RefitClientBuilder<IExternalQuotation>(config)
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
    .AddFusionCache(cacheName: "Teste")
    .WithRegisteredSerializer()
    .WithRegisteredDistributedCache()
    .WithOptions(options => { options.DisableTagging = true; })
    .WithCacheKeyPrefix("KeyPrefix:Teste:")
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

builder.Services.AddDistributedCacheService();

builder.Services.AddTransient<QuotationCacheRepository>();
builder.Services.AddHealthChecks();

builder.Services.AddScoped<IClock, SystemClock>();
builder.Services.AddScoped<ICacheTtlPolicy, MarketTtlCalculator>();
builder.Services.AddScoped<IQuotationCacheRepository, QuotationCacheRepository>();
builder.Services.AddScoped<ITickersQuotationUseCase, TickersQuotationUseCase>();
builder.Services.AddScoped<ITickersQuotationUseCase, TickersQuotationUseCase>();
builder.Services.AddScoped<ITradingCalendar, ConfigTradingCalendar>();

var app = builder.Build();

app.MapGet("/quotation-fusion", async (
    [FromServices] ITickersQuotationUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var result = await useCase
        .GetDataAsync(cancellationToken);

    return Results.Ok(result);
});

app.MapGet("/quotation-distrib/{tickerId}", async (
    [FromServices] QuotationCacheRepository quotationCacheRepository,
    string tickerId,
    CancellationToken cancellationToken) =>
{
    var result = await quotationCacheRepository
        .GetByTickerIdWithoutFusionAsync(
            tickerId, cancellationToken)!;

    return Results.Ok(result);
});

app.UseHealthChecks("/health");
app.UseHttpMetrics();
app.UseMetricServer();

await app.RunAsync();