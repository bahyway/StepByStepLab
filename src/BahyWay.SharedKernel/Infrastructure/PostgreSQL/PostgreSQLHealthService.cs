using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using BahyWay.SharedKernel.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace BahyWay.SharedKernel.Infrastructure.PostgreSQL;

/// <summary>
/// Service for executing BahyWay PostgreSQL HA PowerShell module functions.
/// Provides comprehensive health monitoring for PostgreSQL HA clusters.
/// </summary>
public class PostgreSQLHealthService : IPostgreSQLHealthService, IDisposable
{
    private readonly ILogger<PostgreSQLHealthService> _logger;
    private readonly string _modulePath;
    private Runspace _runspace;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the PostgreSQLHealthService.
    /// </summary>
    /// <param name="logger">Logger instance for service logging.</param>
    public PostgreSQLHealthService(ILogger<PostgreSQLHealthService> logger)
    {
        _logger = logger;
        _modulePath = GetModulePath();
        InitializeRunspace();
    }

    /// <summary>
    /// Gets the PowerShell module path based on deployment environment.
    /// </summary>
    private string GetModulePath()
    {
        // Option 1: Module in output directory (recommended for deployment)
        var outputPath = Path.Combine(
            AppContext.BaseDirectory,
            "PowerShellModules",
            "BahyWay.PostgreSQLHA",
            "BahyWay.PostgreSQLHA.psd1"
        );

        if (File.Exists(outputPath))
        {
            _logger.LogInformation("Found PowerShell module at: {ModulePath}", outputPath);
            return outputPath;
        }

        // Option 2: Module in repository structure (for development)
        var repoPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..",
            "infrastructure",
            "postgresql-ha",
            "powershell-module",
            "BahyWay.PostgreSQLHA",
            "BahyWay.PostgreSQLHA.psd1"
        );

        if (File.Exists(repoPath))
        {
            _logger.LogInformation("Found PowerShell module at: {ModulePath}", repoPath);
            return Path.GetFullPath(repoPath);
        }

        // Option 3: Direct path in infrastructure folder
        var directPath = Path.Combine(
            "/home/user/StepByStepLab",
            "infrastructure",
            "postgresql-ha",
            "powershell-module",
            "BahyWay.PostgreSQLHA",
            "BahyWay.PostgreSQLHA.psd1"
        );

        if (File.Exists(directPath))
        {
            _logger.LogInformation("Found PowerShell module at: {ModulePath}", directPath);
            return directPath;
        }

        throw new FileNotFoundException(
            "PowerShell module not found. Searched paths: " +
            $"{outputPath}, {repoPath}, {directPath}"
        );
    }

    /// <summary>
    /// Initializes PowerShell runspace and imports the BahyWay PostgreSQL HA module.
    /// </summary>
    private void InitializeRunspace()
    {
        try
        {
            var initialSessionState = InitialSessionState.CreateDefault();

            // Set execution policy to allow module import
            initialSessionState.ExecutionPolicy =
                Microsoft.PowerShell.ExecutionPolicy.RemoteSigned;

            _runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            _runspace.Open();

            // Import the BahyWay PostgreSQL HA module
            using var pipeline = _runspace.CreatePipeline();
            pipeline.Commands.AddScript($"Import-Module '{_modulePath}' -Force");
            pipeline.Invoke();

            _logger.LogInformation("PowerShell runspace initialized and module imported successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PowerShell runspace");
            throw;
        }
    }

    /// <summary>
    /// Gets comprehensive cluster health status.
    /// </summary>
    public async Task<Dictionary<string, object>> GetClusterHealthAsync(
        bool includeHAProxy = false,
        bool includeBarman = false)
    {
        var parameters = new Dictionary<string, object>();

        if (includeHAProxy)
            parameters["IncludeHAProxy"] = true;

        if (includeBarman)
            parameters["IncludeBarman"] = true;

        var result = await InvokePowerShellAsync("Get-ClusterHealth", parameters);
        return ConvertPSObjectToDictionary(result.FirstOrDefault());
    }

    /// <summary>
    /// Tests Docker environment health.
    /// </summary>
    public async Task<Dictionary<string, object>> TestDockerEnvironmentAsync()
    {
        var result = await InvokePowerShellAsync("Test-DockerEnvironment");
        return ConvertPSObjectToDictionary(result.FirstOrDefault());
    }

    /// <summary>
    /// Tests PostgreSQL primary node health.
    /// </summary>
    public async Task<Dictionary<string, object>> TestPrimaryNodeAsync(
        string containerName = null)
    {
        var parameters = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(containerName))
            parameters["ContainerName"] = containerName;

        var result = await InvokePowerShellAsync("Test-PostgreSQLPrimary", parameters);
        return ConvertPSObjectToDictionary(result.FirstOrDefault());
    }

    /// <summary>
    /// Tests PostgreSQL replica node health.
    /// </summary>
    public async Task<Dictionary<string, object>> TestReplicaNodeAsync(
        string containerName = null)
    {
        var parameters = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(containerName))
            parameters["ContainerName"] = containerName;

        var result = await InvokePowerShellAsync("Test-PostgreSQLReplica", parameters);
        return ConvertPSObjectToDictionary(result.FirstOrDefault());
    }

    /// <summary>
    /// Tests replication status between primary and replica.
    /// </summary>
    public async Task<Dictionary<string, object>> TestReplicationStatusAsync()
    {
        var result = await InvokePowerShellAsync("Test-PostgreSQLReplication");
        return ConvertPSObjectToDictionary(result.FirstOrDefault());
    }

    /// <summary>
    /// Gets replication lag metrics.
    /// </summary>
    public async Task<Dictionary<string, object>> GetReplicationLagAsync()
    {
        var result = await InvokePowerShellAsync("Get-ReplicationLag");
        return ConvertPSObjectToDictionary(result.FirstOrDefault());
    }

    /// <summary>
    /// Gets database size information.
    /// </summary>
    public async Task<Dictionary<string, object>> GetDatabaseSizeAsync(string databaseName = null)
    {
        var parameters = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(databaseName))
            parameters["DatabaseName"] = databaseName;

        var result = await InvokePowerShellAsync("Get-DatabaseSize", parameters);
        return ConvertPSObjectToDictionary(result.FirstOrDefault());
    }

    /// <summary>
    /// Gets active connection count.
    /// </summary>
    public async Task<Dictionary<string, object>> GetConnectionCountAsync()
    {
        var result = await InvokePowerShellAsync("Get-ConnectionCount");
        return ConvertPSObjectToDictionary(result.FirstOrDefault());
    }

    /// <summary>
    /// Gets all health alarms from the module.
    /// </summary>
    public async Task<List<Dictionary<string, object>>> GetHealthAlarmsAsync()
    {
        var result = await InvokePowerShellAsync("Get-HealthAlarms");
        return result.Select(ConvertPSObjectToDictionary).ToList();
    }

    /// <summary>
    /// Clears all health alarms.
    /// </summary>
    public async Task<Dictionary<string, object>> ClearHealthAlarmsAsync()
    {
        var result = await InvokePowerShellAsync("Clear-HealthAlarms");
        return ConvertPSObjectToDictionary(result.FirstOrDefault());
    }

    /// <summary>
    /// Invokes any PowerShell command from the module.
    /// </summary>
    public async Task<Collection<PSObject>> InvokePowerShellAsync(
        string command,
        Dictionary<string, object> parameters = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var pipeline = _runspace.CreatePipeline();

                var cmd = new Command(command);

                // Add parameters if provided
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param.Key, param.Value);
                    }
                }

                pipeline.Commands.Add(cmd);

                _logger.LogDebug("Executing PowerShell command: {Command}", command);

                var results = pipeline.Invoke();

                // Check for errors
                if (pipeline.Error.Count > 0)
                {
                    var errors = new List<string>();
                    foreach (var error in pipeline.Error.ReadToEnd())
                    {
                        errors.Add(error.ToString());
                    }

                    _logger.LogWarning(
                        "PowerShell command '{Command}' completed with errors: {Errors}",
                        command,
                        string.Join("; ", errors)
                    );
                }

                _logger.LogDebug(
                    "PowerShell command '{Command}' completed. Results: {ResultCount}",
                    command,
                    results.Count
                );

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing PowerShell command: {Command}", command);
                throw;
            }
        });
    }

    /// <summary>
    /// Converts PSObject to Dictionary for easier C# consumption.
    /// </summary>
    private Dictionary<string, object> ConvertPSObjectToDictionary(PSObject psObject)
    {
        if (psObject == null)
            return new Dictionary<string, object>();

        var dict = new Dictionary<string, object>();

        foreach (var property in psObject.Properties)
        {
            var value = property.Value;

            // Handle nested PSObjects
            if (value is PSObject nestedPsObject)
            {
                value = ConvertPSObjectToDictionary(nestedPsObject);
            }
            // Handle arrays
            else if (value is object[] array)
            {
                value = array.Select(item =>
                    item is PSObject pso
                        ? ConvertPSObjectToDictionary(pso)
                        : item
                ).ToList();
            }

            dict[property.Name] = value;
        }

        return dict;
    }

    /// <summary>
    /// Disposes the PowerShell runspace.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _runspace?.Dispose();
                _logger.LogInformation("PowerShell runspace disposed");
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer for PostgreSQLHealthService.
    /// </summary>
    ~PostgreSQLHealthService()
    {
        Dispose(false);
    }
}
