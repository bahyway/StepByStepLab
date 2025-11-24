using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BahyWay.SharedKernel.Infrastructure.Hangfire;

/// <summary>
/// Background service that initializes Hangfire health monitoring jobs on application startup
/// </summary>
internal class HangfireHealthMonitoringInitializer : IHostedService
{
    private readonly ILogger<HangfireHealthMonitoringInitializer> _logger;

    public HangfireHealthMonitoringInitializer(
        ILogger<HangfireHealthMonitoringInitializer> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing Hangfire health monitoring jobs");

            // Note: The actual job scheduling should be done by the application
            // This class just ensures the infrastructure is ready

            _logger.LogInformation("Hangfire health monitoring initialization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Hangfire health monitoring");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Hangfire health monitoring");
        return Task.CompletedTask;
    }
}
