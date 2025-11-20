using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace BahyWay.SharedKernel.Infrastructure.Logging;

/// <summary>
/// Configures Serilog with best practices for BahyWay applications.
/// Includes structured logging, enrichers, and multiple sinks.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Configures Serilog for the BahyWay application.
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="applicationName">Name of the application (e.g., "AlarmInsight", "ETLway")</param>
    /// <param name="environment">Hosting environment</param>
    /// <returns>Configured LoggerConfiguration</returns>
    public static LoggerConfiguration ConfigureBahyWayLogging(
        IConfiguration configuration,
        string applicationName,
        IHostEnvironment environment)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            
            // Enrich logs with additional context
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithExceptionDetails() // Requires Serilog.Exceptions
            .Enrich.WithProperty("Application", applicationName)
            
            // Console output for development
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        // Add file logging for production
        if (environment.IsProduction())
        {
            loggerConfig.WriteTo.File(
                new CompactJsonFormatter(),
                path: $"logs/{applicationName.ToLower()}-log-.json",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 100_000_000, // 100MB
                rollOnFileSizeLimit: true);
        }
        
        // Add Seq sink if configured (excellent for development and production monitoring)
        var seqServerUrl = configuration["Serilog:SeqServerUrl"];
        if (!string.IsNullOrEmpty(seqServerUrl))
        {
            loggerConfig.WriteTo.Seq(seqServerUrl);
        }

        // Add Elasticsearch if configured (for production)
        var elasticsearchUrl = configuration["Serilog:ElasticsearchUrl"];
        if (!string.IsNullOrEmpty(elasticsearchUrl))
        {
            loggerConfig.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
            {
                AutoRegisterTemplate = true,
                IndexFormat = $"{applicationName.ToLower()}-logs-{{0:yyyy.MM.dd}}",
                NumberOfShards = 2,
                NumberOfReplicas = 1
            });
        }

        return loggerConfig;
    }

    /// <summary>
    /// Creates a bootstrap logger for startup errors before full configuration is loaded.
    /// </summary>
    public static ILogger CreateBootstrapLogger(string applicationName)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("Application", applicationName)
            .WriteTo.Console()
            .CreateBootstrapLogger();
    }
}
