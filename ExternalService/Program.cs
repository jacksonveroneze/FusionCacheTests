using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using ExternalService;
using JacksonVeroneze.NET.Logging.Util;
using Microsoft.AspNetCore.Http.Timeouts;
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

// builder.Services.AddRequestTimeouts(
//     conf => conf.DefaultPolicy = new RequestTimeoutPolicy()
// {
//     Timeout = TimeSpan.FromSeconds(1)
// });

var app = builder.Build();

app.MapGet("/quotations/{quotationId:required}", async (
    [FromServices] ILogger<Program> logger,
    [FromServices] DefaultDbContext dbContext,
    [FromRoute] string quotationId,
    [FromQuery(Name = "timeout")] int timeout,
    CancellationToken cancellationToken) =>
{
    cancellationToken.Register(() =>
        logger.LogWarning("CancellationToken foi cancelado (RequestAborted)."));
    
    await dbContext.Quotations
        .AsNoTracking()
        .FirstOrDefaultAsync(opt => opt.TickerId == quotationId, cancellationToken);
    
    logger.LogInformation("Antes do delay. IsCancellationRequested: {IsCancellationRequested}",
        cancellationToken.IsCancellationRequested);    
    
        await Task.Delay(TimeSpan.FromMilliseconds(timeout), cancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken);
    

    logger.LogInformation("Depois do delay. IsCancellationRequested: {IsCancellationRequested}",
        cancellationToken.IsCancellationRequested);    
    
    try
    {
        var quotation = await dbContext.Quotations
            .AsNoTracking()
            .FirstOrDefaultAsync(opt => opt.TickerId == quotationId, cancellationToken);

        return quotation is null ? Results.NotFound() : Results.Ok(quotation);
    }
    catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested)
    {
        logger.LogWarning(oce, "Consulta cancelada porque o cliente abortou a request.");
        throw; // deixa o ASP.NET finalizar como request abortada (499)
    }

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

app.UseRouting();
// app.UseRequestTimeouts();
app.UseHttpMetrics();
app.UseMetricServer();

await app.RunAsync();