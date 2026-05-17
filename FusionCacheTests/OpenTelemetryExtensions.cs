using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FusionCacheTests;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryApp(
        this IServiceCollection services)
    {
        services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
        {
            options.Filter = ctx =>
                (!ctx.Request.Path.Value?.StartsWith("/metrics",
                    StringComparison.OrdinalIgnoreCase) ?? false) &&
                ctx.Request.Path != "/health";
        });

        services.AddOpenTelemetry()
            .AddMetrics()
            .AddTracing();

        return services;
    }

    extension(IOpenTelemetryBuilder builder)
    {
        private IOpenTelemetryBuilder AddMetrics()
        {
            builder.WithMetrics(opts => opts
                .AddProcessInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddFusionCacheInstrumentation(conf =>
                {
                    conf.IncludeMemoryLevel = true;
                    conf.IncludeDistributedLevel = true;
                })
                .AddMeter("Polly") 
                .AddPrometheusExporter());

            return builder;
        }

        private IOpenTelemetryBuilder AddTracing()
        {
            builder.WithTracing(conf =>
            {
                conf.AddAspNetCoreInstrumentation(options => { options.RecordException = true; })
                    .AddHttpClientInstrumentation()
                    .AddRedisInstrumentation()
                    .AddFusionCacheInstrumentation(options => { options.IncludeMemoryLevel = true; })
                    .AddSource();
            });

            return builder;
        }
    }
}