using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AlarmInsight.Infrastructure.Services
{
    /// <summary>
    /// Interface for PostgreSQL Health Service
    /// </summary>
    public interface IPostgreSQLHealthService
    {
        Task<Dictionary<string, object>> GetClusterHealthAsync(
            bool includeHAProxy = false,
            bool includeBarman = false);

        Task<Dictionary<string, object>> TestDockerEnvironmentAsync();

        Task<Dictionary<string, object>> TestPrimaryNodeAsync(string containerName = null);

        Task<Dictionary<string, object>> TestReplicaNodeAsync(string containerName = null);

        Task<Dictionary<string, object>> TestReplicationStatusAsync();

        Task<Collection<PSObject>> InvokePowerShellAsync(
            string command,
            Dictionary<string, object> parameters = null);

        Task<List<Dictionary<string, object>>> GetHealthAlarmsAsync();
    }

    /// <summary>
    /// Service for executing BahyWay PostgreSQL HA PowerShell module functions
    /// </summary>
    public class PostgreSQLHealthService : IPostgreSQLHealthService, IDisposable
    {
        private readonly ILogger<PostgreSQLHealthService> _logger;
        private readonly string _modulePath;
        private Runspace _runspace;
        private bool _disposed = false;

        public PostgreSQLHealthService(ILogger<PostgreSQLHealthService> logger)
        {
            _logger = logger;

            // Determine module path based on deployment
            _modulePath = GetModulePath();

            // Initialize PowerShell runspace
            InitializeRunspace();
        }

        /// <summary>
        /// Gets the PowerShell module path
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
                _logger.LogInformation($"Found PowerShell module at: {outputPath}");
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
                _logger.LogInformation($"Found PowerShell module at: {repoPath}");
                return Path.GetFullPath(repoPath);
            }

            throw new FileNotFoundException(
                "PowerShell module not found. Searched paths: " +
                $"{outputPath}, {repoPath}"
            );
        }

        /// <summary>
        /// Initializes PowerShell runspace and imports module
        /// </summary>
        private void InitializeRunspace()
        {
            try
            {
                var initialSessionState = InitialSessionState.CreateDefault();

                // Set execution policy
                initialSessionState.ExecutionPolicy =
                    Microsoft.PowerShell.ExecutionPolicy.RemoteSigned;

                _runspace = RunspaceFactory.CreateRunspace(initialSessionState);
                _runspace.Open();

                // Import the BahyWay PostgreSQL HA module
                using var pipeline = _runspace.CreatePipeline();
                pipeline.Commands.AddScript($"Import-Module '{_modulePath}' -Force");
                pipeline.Invoke();

                _logger.LogInformation("PowerShell runspace initialized and module imported");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize PowerShell runspace");
                throw;
            }
        }

        /// <summary>
        /// Gets comprehensive cluster health status
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
        /// Tests Docker environment health
        /// </summary>
        public async Task<Dictionary<string, object>> TestDockerEnvironmentAsync()
        {
            var result = await InvokePowerShellAsync("Test-DockerEnvironment");
            return ConvertPSObjectToDictionary(result.FirstOrDefault());
        }

        /// <summary>
        /// Tests PostgreSQL primary node health
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
        /// Tests PostgreSQL replica node health
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
        /// Tests replication status between primary and replica
        /// </summary>
        public async Task<Dictionary<string, object>> TestReplicationStatusAsync()
        {
            var result = await InvokePowerShellAsync("Test-PostgreSQLReplication");
            return ConvertPSObjectToDictionary(result.FirstOrDefault());
        }

        /// <summary>
        /// Gets all health alarms from the module
        /// </summary>
        public async Task<List<Dictionary<string, object>>> GetHealthAlarmsAsync()
        {
            var result = await InvokePowerShellAsync("Get-HealthAlarms");
            return result.Select(ConvertPSObjectToDictionary).ToList();
        }

        /// <summary>
        /// Invokes any PowerShell command from the module
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

                    _logger.LogDebug($"Executing PowerShell command: {command}");

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
                            $"PowerShell command '{command}' completed with errors: " +
                            string.Join("; ", errors)
                        );
                    }

                    _logger.LogDebug(
                        $"PowerShell command '{command}' completed. " +
                        $"Results: {results.Count}"
                    );

                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing PowerShell command: {command}");
                    throw;
                }
            });
        }

        /// <summary>
        /// Converts PSObject to Dictionary for easier C# consumption
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
        /// Disposes the PowerShell runspace
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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

        ~PostgreSQLHealthService()
        {
            Dispose(false);
        }
    }
}
