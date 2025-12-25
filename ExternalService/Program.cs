using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using ExternalService;
using JacksonVeroneze.NET.Logging.Util;
using Serilog;

Log.Logger = BootstrapLogger.CreateLogger();

var builder = WebApplication.CreateSlimBuilder(args);

builder.Host.UseSerilog((hostingContext,
    services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(hostingContext.Configuration)
        .ReadFrom.Services(services);
});

builder.Services.AddDbContext<DefaultDbContext>(options =>
{
    var connectionString = builder.Configuration
        .GetConnectionString("Default");

    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention();
});

var app = builder.Build();

app.MapGet("/quotations/{quotationId:required}", async (
    [FromServices] DefaultDbContext dbContext,
    [FromRoute] string quotationId) =>
{
    await Task.Delay(TimeSpan.FromMilliseconds(100));

    var quotation = await dbContext.Quotations
        .AsNoTracking()
        .FirstOrDefaultAsync(opt => opt.TickerId == quotationId);

    return quotation is null
        ? Results.NotFound()
        : Results.Ok(quotation);
});

app.MapGet("/cms/{contentId:required}", async (
    IConfiguration config,
    [FromRoute] string contentId,
    [FromQuery] string faultMode) =>
{
    await Task.Delay(TimeSpan.FromMilliseconds(50));

    var enabled = config.GetValue<bool>("Chaos:Enabled");

    return enabled || faultMode.Equals("error")
        ? Results.InternalServerError()
        : Results.Ok(new Cms(contentId, $"Content_{contentId}"));
});

app.UseHttpMetrics();
app.UseMetricServer();

await app.RunAsync();