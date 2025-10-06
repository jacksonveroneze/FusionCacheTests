using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using QuotationService;

var builder = WebApplication.CreateSlimBuilder(args);

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
    string quotationId) =>
{
    await Task.Delay(TimeSpan.FromMilliseconds(100));
    
    var quotation = await dbContext.Quotations
        .AsNoTracking()
        .FirstOrDefaultAsync(opt => opt.TickerId == quotationId);

    return quotation is null 
        ? Results.NotFound() 
        : Results.Ok(quotation);
});

app.UseHttpMetrics();
app.UseMetricServer();

await app.RunAsync();