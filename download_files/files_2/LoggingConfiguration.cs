using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace SharedKernel.Infrastructure.Observability.Logging;

/// <summary>
/// Centralized Serilog configuration for all BahyWay projects
/// Provides structured logging with enrichment, multiple sinks, and correlation tracking
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configures Serilog as the logging provider with standard BahyWay settings
    /// </summary>
    /// <param name="builder">WebApplicationBuilder instance</param>
    /// <param name="configuration">Application configuration</param>
    public static void ConfigureSerilog(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        Log.Logger = CreateLogger(configuration, builder.Environment);
        
        builder.Host.UseSerilog(Log.Logger, dispose: true);
    }

    /// <summary>
    /// Creates and configures the Serilog logger
    /// </summary>
    private static ILogger CreateLogger(IConfiguration configuration, IHostEnvironment environment)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("Application", GetApplicationName(configuration))
            .Enrich.WithProperty("Environment", environment.EnvironmentName);

        // Console output (always enabled for local development)
        ConfigureConsole(loggerConfiguration, environment);

        // File output (for all environments)
        ConfigureFile(loggerConfiguration, configuration);

        // Seq output (centralized logging - optional)
        ConfigureSeq(loggerConfiguration, configuration);

        // Elasticsearch output (production logging - optional)
        ConfigureElasticsearch(loggerConfiguration, configuration);

        // Set minimum level
        var minimumLevel = configuration.GetValue<LogEventLevel>("Logging:MinimumLevel", LogEventLevel.Information);
        loggerConfiguration.MinimumLevel.Is(minimumLevel);

        // Override Microsoft and System logs to Warning (reduce noise)
        loggerConfiguration
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning);

        return loggerConfiguration.CreateLogger();
    }

    private static void ConfigureConsole(LoggerConfiguration loggerConfiguration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            // Human-readable format for development
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }
        else
        {
            // Structured JSON for production (easier to parse)
            loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
        }
    }

    private static void ConfigureFile(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        var logPath = configuration.GetValue<string>("Logging:File:Path") ?? "logs/log-.txt";
        var retainedFileCount = configuration.GetValue<int>("Logging:File:RetainedFileCount", 31);

        loggerConfiguration.WriteTo.File(
            new CompactJsonFormatter(),
            logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: retainedFileCount,
            fileSizeLimitBytes: 100_000_000, // 100MB per file
            rollOnFileSizeLimit: true);
    }

    private static void ConfigureSeq(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        var seqUrl = configuration.GetValue<string>("Logging:Seq:ServerUrl");
        var seqApiKey = configuration.GetValue<string>("Logging:Seq:ApiKey");

        if (!string.IsNullOrWhiteSpace(seqUrl))
        {
            loggerConfiguration.WriteTo.Seq(
                seqUrl,
                apiKey: seqApiKey,
                restrictedToMinimumLevel: LogEventLevel.Information);
        }
    }

    private static void ConfigureElasticsearch(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        var elasticUrl = configuration.GetValue<string>("Logging:Elasticsearch:NodeUris");
        var indexFormat = configuration.GetValue<string>("Logging:Elasticsearch:IndexFormat") 
            ?? "bahyway-logs-{0:yyyy.MM.dd}";

        if (!string.IsNullOrWhiteSpace(elasticUrl))
        {
            loggerConfiguration.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticUrl))
            {
                IndexFormat = indexFormat,
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = Serilog.Sinks.Elasticsearch.AutoRegisterTemplateVersion.ESv7,
                MinimumLogEventLevel = LogEventLevel.Information
            });
        }
    }

    private static string GetApplicationName(IConfiguration configuration)
    {
        return configuration.GetValue<string>("Application:Name") 
            ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name 
            ?? "BahyWay";
    }

    /// <summary>
    /// Flushes and closes the logger (call on application shutdown)
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}

/// <summary>
/// Extension methods for structured logging
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Logs operation start with timing
    /// </summary>
    public static IDisposable BeginTimedOperation(this ILogger logger, string operationName, params object[] args)
    {
        return logger.ForContext("OperationName", operationName)
            .BeginTimedOperation(operationName, args);
    }

    /// <summary>
    /// Logs with correlation ID
    /// </summary>
    public static ILogger WithCorrelationId(this ILogger logger, string correlationId)
    {
        return logger.ForContext("CorrelationId", correlationId);
    }

    /// <summary>
    /// Logs with user context
    /// </summary>
    public static ILogger WithUser(this ILogger logger, string userId, string? userName = null)
    {
        var contextLogger = logger.ForContext("UserId", userId);
        
        if (!string.IsNullOrWhiteSpace(userName))
        {
            contextLogger = contextLogger.ForContext("UserName", userName);
        }

        return contextLogger;
    }

    /// <summary>
    /// Logs with request context
    /// </summary>
    public static ILogger WithRequest(this ILogger logger, string method, string path)
    {
        return logger
            .ForContext("RequestMethod", method)
            .ForContext("RequestPath", path);
    }
}
