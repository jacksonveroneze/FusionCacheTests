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
    options.InstanceName = "FusionCacheTestsxxx";
});

builder.Services
    .AddFusionCacheSystemTextJsonSerializer()
    .AddFusionCache()
    .WithRegisteredSerializer()
    .WithRegisteredDistributedCache()
    .WithOptions(options => { options.DisableTagging = true; })
    .WithDefaultEntryOptions(options =>
    {
        options.EagerRefreshThreshold = 0.8f;
        options.JitterMaxDuration = TimeSpan.FromSeconds(2);
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