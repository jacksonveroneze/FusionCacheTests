using FusionCacheTests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Prometheus;
using Refit;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateSlimBuilder(args);

var url = builder.Configuration.GetValue<string>("QuotationServerUrl");

builder.Services
    .AddRefitClient<IExternalQuotation>()
    .ConfigureHttpClient(client => { client.BaseAddress = new Uri(url!); })
    .AddStandardResilienceHandler();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "FusionCacheTests";
});

builder.Services.AddFusionCache()
    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
    .WithDistributedCache(serviceProvider =>
        serviceProvider.GetRequiredService<IDistributedCache>())
    .WithDefaultEntryOptions(new FusionCacheEntryOptions()
    {
        SkipMemoryCacheRead = false,
        SkipMemoryCacheWrite = false
    });

builder.Services.AddMemoryCache();

builder.Services.AddTransient<QuotationService>();

var app = builder.Build();

app.MapGet("/quotation/{tickerId}", async (
    [FromServices] QuotationService quotationService,
    string tickerId,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(tickerId))
        return Results.BadRequest("tickerId is required.");

    var result = await quotationService.GetByTickerIdAsync(
        tickerId, cancellationToken)!;

    return Results.Ok(result);
});
// ... existing code ...

app.UseHttpMetrics();
app.UseMetricServer();

await app.RunAsync();