using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Threading.Tasks;

namespace BahyWay.SharedKernel.Application.Abstractions;

/// <summary>
/// Interface for PostgreSQL HA cluster health monitoring service.
/// Provides methods to check health status, monitor replication, and manage alarms.
/// </summary>
public interface IPostgreSQLHealthService
{
    /// <summary>
    /// Gets comprehensive health status of the PostgreSQL cluster.
    /// </summary>
    /// <param name="includeHAProxy">Include HAProxy health check in results.</param>
    /// <param name="includeBarman">Include Barman backup health check in results.</param>
    /// <returns>Dictionary containing health status information.</returns>
    Task<Dictionary<string, object>> GetClusterHealthAsync(
        bool includeHAProxy = false,
        bool includeBarman = false);

    /// <summary>
    /// Tests if Docker environment is properly configured and accessible.
    /// </summary>
    /// <returns>Dictionary containing Docker environment test results.</returns>
    Task<Dictionary<string, object>> TestDockerEnvironmentAsync();

    /// <summary>
    /// Tests PostgreSQL primary node health.
    /// </summary>
    /// <param name="containerName">Optional container name. If null, uses default.</param>
    /// <returns>Dictionary containing primary node health status.</returns>
    Task<Dictionary<string, object>> TestPrimaryNodeAsync(string containerName = null);

    /// <summary>
    /// Tests PostgreSQL replica node health.
    /// </summary>
    /// <param name="containerName">Optional container name. If null, uses default.</param>
    /// <returns>Dictionary containing replica node health status.</returns>
    Task<Dictionary<string, object>> TestReplicaNodeAsync(string containerName = null);

    /// <summary>
    /// Tests replication status between primary and replica nodes.
    /// </summary>
    /// <returns>Dictionary containing replication status information.</returns>
    Task<Dictionary<string, object>> TestReplicationStatusAsync();

    /// <summary>
    /// Gets replication lag metrics.
    /// </summary>
    /// <returns>Dictionary containing replication lag information.</returns>
    Task<Dictionary<string, object>> GetReplicationLagAsync();

    /// <summary>
    /// Gets database size information.
    /// </summary>
    /// <param name="databaseName">Optional database name. If null, gets all databases.</param>
    /// <returns>Dictionary containing database size information.</returns>
    Task<Dictionary<string, object>> GetDatabaseSizeAsync(string databaseName = null);

    /// <summary>
    /// Gets active connection count for the cluster.
    /// </summary>
    /// <returns>Dictionary containing connection count information.</returns>
    Task<Dictionary<string, object>> GetConnectionCountAsync();

    /// <summary>
    /// Gets all health alarms from the monitoring system.
    /// </summary>
    /// <returns>List of health alarms.</returns>
    Task<List<Dictionary<string, object>>> GetHealthAlarmsAsync();

    /// <summary>
    /// Clears all health alarms.
    /// </summary>
    /// <returns>Result of the clear operation.</returns>
    Task<Dictionary<string, object>> ClearHealthAlarmsAsync();

    /// <summary>
    /// Invokes any PowerShell command from the BahyWay PostgreSQL HA module.
    /// </summary>
    /// <param name="command">The PowerShell command to execute.</param>
    /// <param name="parameters">Optional parameters for the command.</param>
    /// <returns>Collection of PowerShell objects returned by the command.</returns>
    Task<Collection<PSObject>> InvokePowerShellAsync(
        string command,
        Dictionary<string, object> parameters = null);
}
