using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using AlarmInsight.Infrastructure.Services;

namespace AlarmInsight.API.Controllers
{
    /// <summary>
    /// API Controller for PostgreSQL HA Health Monitoring
    /// Add this to your AlarmInsight.API project
    /// </summary>
    [ApiController]
    [Route("api/postgresql")]
    public class PostgreSQLHealthController : ControllerBase
    {
        private readonly IPostgreSQLHealthService _healthService;
        private readonly ILogger<PostgreSQLHealthController> _logger;

        public PostgreSQLHealthController(
            IPostgreSQLHealthService healthService,
            ILogger<PostgreSQLHealthController> logger)
        {
            _healthService = healthService;
            _logger = logger;
        }

        /// <summary>
        /// Gets comprehensive cluster health status
        /// GET: api/postgresql/health
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetClusterHealth(
            [FromQuery] bool includeHAProxy = false,
            [FromQuery] bool includeBarman = false)
        {
            try
            {
                _logger.LogInformation("Getting PostgreSQL cluster health status");

                var health = await _healthService.GetClusterHealthAsync(
                    includeHAProxy,
                    includeBarman
                );

                var isHealthy = health.TryGetValue("IsHealthy", out var healthStatus)
                    && healthStatus is bool healthy
                    && healthy;

                if (!isHealthy)
                {
                    _logger.LogWarning(
                        "PostgreSQL cluster health check returned unhealthy status"
                    );
                }

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    healthy = isHealthy,
                    details = health
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PostgreSQL cluster health");
                return StatusCode(500, new
                {
                    error = "Failed to get cluster health",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Tests Docker environment
        /// GET: api/postgresql/docker
        /// </summary>
        [HttpGet("docker")]
        public async Task<IActionResult> TestDocker()
        {
            try
            {
                var result = await _healthService.TestDockerEnvironmentAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Docker environment");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Tests primary PostgreSQL node
        /// GET: api/postgresql/primary
        /// </summary>
        [HttpGet("primary")]
        public async Task<IActionResult> TestPrimary(
            [FromQuery] string containerName = null)
        {
            try
            {
                var result = await _healthService.TestPrimaryNodeAsync(containerName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing primary node");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Tests replica PostgreSQL node
        /// GET: api/postgresql/replica
        /// </summary>
        [HttpGet("replica")]
        public async Task<IActionResult> TestReplica(
            [FromQuery] string containerName = null)
        {
            try
            {
                var result = await _healthService.TestReplicaNodeAsync(containerName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing replica node");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets replication status
        /// GET: api/postgresql/replication
        /// </summary>
        [HttpGet("replication")]
        public async Task<IActionResult> GetReplicationStatus()
        {
            try
            {
                var result = await _healthService.TestReplicationStatusAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting replication status");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets all health alarms
        /// GET: api/postgresql/alarms
        /// </summary>
        [HttpGet("alarms")]
        public async Task<IActionResult> GetAlarms()
        {
            try
            {
                var alarms = await _healthService.GetHealthAlarmsAsync();
                return Ok(new
                {
                    count = alarms.Count,
                    alarms = alarms
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health alarms");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint for monitoring systems
        /// GET: api/postgresql/healthz
        /// </summary>
        [HttpGet("healthz")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var health = await _healthService.GetClusterHealthAsync();

                var isHealthy = health.TryGetValue("IsHealthy", out var healthStatus)
                    && healthStatus is bool healthy
                    && healthy;

                if (isHealthy)
                {
                    return Ok(new { status = "healthy" });
                }
                else
                {
                    return StatusCode(503, new
                    {
                        status = "unhealthy",
                        issues = health.TryGetValue("AllIssues", out var issues)
                            ? issues
                            : null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(503, new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }
    }
}
