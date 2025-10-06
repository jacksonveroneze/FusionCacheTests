using FusionCacheTests;
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

builder.Services.AddTransient<QuotationService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/quotation-fusion/{tickerId}", async (
    [FromServices] QuotationService quotationService,
    string tickerId,
    CancellationToken cancellationToken) =>
{
    var result = await quotationService
        .GetByTickerIdAsync(
            tickerId, cancellationToken)!;

    return Results.Ok(result);
});

app.MapGet("/quotation-distrib/{tickerId}", async (
    [FromServices] QuotationService quotationService,
    string tickerId,
    CancellationToken cancellationToken) =>
{
    var result = await quotationService
        .GetByTickerIdWithoutFusionAsync(
            tickerId, cancellationToken)!;

    return Results.Ok(result);
});

app.UseHealthChecks("/health");
app.UseHttpMetrics();
app.UseMetricServer();

await app.RunAsync();