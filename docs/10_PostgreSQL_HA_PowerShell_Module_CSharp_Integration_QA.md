# PostgreSQL HA PowerShell Module - C# Integration Q&A



---



## Question01:



**How can I add the PowerShell Module for PostgreSQL Replication to my C# (.NET) BahyWay Solution?**



---



## Answer01:



The BahyWay PostgreSQL HA PowerShell module can be integrated into your C# .NET solution using the **Microsoft.PowerShell.SDK** package. This allows you to execute PowerShell commands directly from C# code.



---



## 📦 Module Information



**Module Name:** `BahyWay.PostgreSQLHA`

**Version:** 1.0.0

**Author:** Bahaa Fadam - BahyWay Solutions

**Location:** `infrastructure/postgresql-ha/powershell-module/BahyWay.PostgreSQLHA/`



**Files:**

- `BahyWay.PostgreSQLHA.psd1` (Module Manifest)

- `BahyWay.PostgreSQLHA.psm1` (Module Script - 1165 lines)



---



## ⚡ Quick Integration Steps



### Step 1: Add NuGet Package



Add to your `AlarmInsight.Infrastructure` project:



```powershell

Install-Package Microsoft.PowerShell.SDK -Version 7.2.18

```



Or add to `.csproj`:

```xml

<PackageReference Include="Microsoft.PowerShell.SDK" Version="7.2.18" />

```



**Important:** Force specific versions to resolve conflicts:



```xml

<ItemGroup>

  <PackageReference Include="Microsoft.PowerShell.SDK" Version="7.2.18" />



  <!-- Force specific versions to resolve conflicts -->

  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.5.0" />

  <PackageReference Include="Microsoft.CodeAnalysis.Common" Version="4.5.0" />

  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.5.0" />

</ItemGroup>

```



---



### Step 2: Add PowerShell Module to Project



**Location in BahyWay Solution:**



```

AlarmInsight.Infrastructure

├── Dependencies

├── Migrations

├── Persistence

├── PowerShellModules/           ⭐ CREATE THIS

│   └── BahyWay.PostgreSQLHA/

│       ├── BahyWay.PostgreSQLHA.psd1

│       └── BahyWay.PostgreSQLHA.psm1

├── Services/

└── ...

```



**Steps:**



1. In Visual Studio → Right-click `AlarmInsight.Infrastructure` project

2. Add → New Folder → Name: `PowerShellModules`

3. Add → New Folder → Name: `BahyWay.PostgreSQLHA`

4. Add → Existing Item → Browse to module files

5. Select both `.psd1` and `.psm1` files

6. **Set Properties** (F4):

   - **Build Action:** `Content`

   - **Copy to Output Directory:** `Copy if newer`



---



### Step 3: Create C# Service Wrapper



Create `Services/PostgreSQLHealthService.cs`:



```csharp

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

    public interface IPostgreSQLHealthService

    {

        Task<Dictionary<string, object>> GetClusterHealthAsync(

            bool includeHAProxy = false,

            bool includeBarman = false);



        Task<Dictionary<string, object>> TestDockerEnvironmentAsync();

        Task<Dictionary<string, object>> TestPrimaryNodeAsync(string containerName = null);

        Task<Dictionary<string, object>> TestReplicaNodeAsync(string containerName = null);

        Task<Dictionary<string, object>> TestReplicationStatusAsync();

        Task<List<Dictionary<string, object>>> GetHealthAlarmsAsync();



        Task<Collection<PSObject>> InvokePowerShellAsync(

            string command,

            Dictionary<string, object> parameters = null);

    }



    public class PostgreSQLHealthService : IPostgreSQLHealthService, IDisposable

    {

        private readonly ILogger<PostgreSQLHealthService> _logger;

        private readonly string _modulePath;

        private Runspace _runspace;

        private bool _disposed = false;



        public PostgreSQLHealthService(ILogger<PostgreSQLHealthService> logger)

        {

            _logger = logger;

            _modulePath = GetModulePath();

            InitializeRunspace();

        }



        private string GetModulePath()

        {

            // Option 1: Module in output directory (deployment)

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



            // Option 2: Module in repository (development)

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

                "PowerShell module not found. Searched: " +

                $"{outputPath}, {repoPath}"

            );

        }



        private void InitializeRunspace()

        {

            try

            {

                var initialSessionState = InitialSessionState.CreateDefault();

                initialSessionState.ExecutionPolicy =

                    Microsoft.PowerShell.ExecutionPolicy.RemoteSigned;



                _runspace = RunspaceFactory.CreateRunspace(initialSessionState);

                _runspace.Open();



                using var pipeline = _runspace.CreatePipeline();

                pipeline.Commands.AddScript($"Import-Module '{_modulePath}' -Force");

                pipeline.Invoke();



                _logger.LogInformation("PowerShell runspace initialized");

            }

            catch (Exception ex)

            {

                _logger.LogError(ex, "Failed to initialize PowerShell runspace");

                throw;

            }

        }



        public async Task<Dictionary<string, object>> GetClusterHealthAsync(

            bool includeHAProxy = false,

            bool includeBarman = false)

        {

            var parameters = new Dictionary<string, object>();

            if (includeHAProxy) parameters["IncludeHAProxy"] = true;

            if (includeBarman) parameters["IncludeBarman"] = true;



            var result = await InvokePowerShellAsync("Get-ClusterHealth", parameters);

            return ConvertPSObjectToDictionary(result.FirstOrDefault());

        }



        public async Task<Dictionary<string, object>> TestDockerEnvironmentAsync()

        {

            var result = await InvokePowerShellAsync("Test-DockerEnvironment");

            return ConvertPSObjectToDictionary(result.FirstOrDefault());

        }



        public async Task<Dictionary<string, object>> TestPrimaryNodeAsync(

            string containerName = null)

        {

            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(containerName))

                parameters["ContainerName"] = containerName;



            var result = await InvokePowerShellAsync("Test-PostgreSQLPrimary", parameters);

            return ConvertPSObjectToDictionary(result.FirstOrDefault());

        }



        public async Task<Dictionary<string, object>> TestReplicaNodeAsync(

            string containerName = null)

        {

            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(containerName))

                parameters["ContainerName"] = containerName;



            var result = await InvokePowerShellAsync("Test-PostgreSQLReplica", parameters);

            return ConvertPSObjectToDictionary(result.FirstOrDefault());

        }



        public async Task<Dictionary<string, object>> TestReplicationStatusAsync()

        {

            var result = await InvokePowerShellAsync("Test-PostgreSQLReplication");

            return ConvertPSObjectToDictionary(result.FirstOrDefault());

        }



        public async Task<List<Dictionary<string, object>>> GetHealthAlarmsAsync()

        {

            var result = await InvokePowerShellAsync("Get-HealthAlarms");

            return result.Select(ConvertPSObjectToDictionary).ToList();

        }



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



                    if (parameters != null)

                    {

                        foreach (var param in parameters)

                        {

                            cmd.Parameters.Add(param.Key, param.Value);

                        }

                    }



                    pipeline.Commands.Add(cmd);

                    _logger.LogDebug($"Executing PowerShell: {command}");



                    var results = pipeline.Invoke();



                    if (pipeline.Error.Count > 0)

                    {

                        var errors = pipeline.Error.ReadToEnd()

                            .Select(e => e.ToString()).ToList();

                        _logger.LogWarning(

                            $"PowerShell '{command}' completed with errors: " +

                            string.Join("; ", errors)

                        );

                    }



                    return results;

                }

                catch (Exception ex)

                {

                    _logger.LogError(ex, $"Error executing PowerShell: {command}");

                    throw;

                }

            });

        }



        private Dictionary<string, object> ConvertPSObjectToDictionary(PSObject psObject)

        {

            if (psObject == null)

                return new Dictionary<string, object>();



            var dict = new Dictionary<string, object>();



            foreach (var property in psObject.Properties)

            {

                var value = property.Value;



                if (value is PSObject nestedPsObject)

                {

                    value = ConvertPSObjectToDictionary(nestedPsObject);

                }

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

```



---



### Step 4: Register Service in DI



**For .NET 6+ (Program.cs):**



```csharp

using AlarmInsight.Infrastructure.Services;



var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();



// Add PostgreSQL Health Service

builder.Services.AddScoped<IPostgreSQLHealthService, PostgreSQLHealthService>();



var app = builder.Build();

app.MapControllers();

app.Run();

```



**For .NET 5 and earlier (Startup.cs):**



```csharp

public void ConfigureServices(IServiceCollection services)

{

    services.AddControllers();

    services.AddScoped<IPostgreSQLHealthService, PostgreSQLHealthService>();

}

```



---



### Step 5: Create API Controller (Optional)



Create `Controllers/PostgreSQLHealthController.cs`:



```csharp

using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Logging;

using System;

using System.Threading.Tasks;

using AlarmInsight.Infrastructure.Services;



namespace AlarmInsight.API.Controllers

{

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

        /// GET: api/postgresql/health

        /// </summary>

        [HttpGet("health")]

        public async Task<IActionResult> GetClusterHealth(

            [FromQuery] bool includeHAProxy = false,

            [FromQuery] bool includeBarman = false)

        {

            try

            {

                var health = await _healthService.GetClusterHealthAsync(

                    includeHAProxy, includeBarman);



                var isHealthy = health.TryGetValue("IsHealthy", out var status)

                    && status is bool healthy && healthy;



                return Ok(new

                {

                    timestamp = DateTime.UtcNow,

                    healthy = isHealthy,

                    details = health

                });

            }

            catch (Exception ex)

            {

                _logger.LogError(ex, "Error getting cluster health");

                return StatusCode(500, new { error = ex.Message });

            }

        }



        /// <summary>

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

                _logger.LogError(ex, "Error testing Docker");

                return StatusCode(500, new { error = ex.Message });

            }

        }



        /// <summary>

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

                _logger.LogError(ex, "Error testing primary");

                return StatusCode(500, new { error = ex.Message });

            }

        }



        /// <summary>

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

                _logger.LogError(ex, "Error testing replica");

                return StatusCode(500, new { error = ex.Message });

            }

        }



        /// <summary>

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

        /// GET: api/postgresql/alarms

        /// </summary>

        [HttpGet("alarms")]

        public async Task<IActionResult> GetAlarms()

        {

            try

            {

                var alarms = await _healthService.GetHealthAlarmsAsync();

                return Ok(new { count = alarms.Count, alarms });

            }

            catch (Exception ex)

            {

                _logger.LogError(ex, "Error getting alarms");

                return StatusCode(500, new { error = ex.Message });

            }

        }



        /// <summary>

        /// GET: api/postgresql/healthz

        /// </summary>

        [HttpGet("healthz")]

        public async Task<IActionResult> HealthCheck()

        {

            try

            {

                var health = await _healthService.GetClusterHealthAsync();

                var isHealthy = health.TryGetValue("IsHealthy", out var status)

                    && status is bool healthy && healthy;



                if (isHealthy)

                    return Ok(new { status = "healthy" });

                else

                    return StatusCode(503, new

                    {

                        status = "unhealthy",

                        issues = health.TryGetValue("AllIssues", out var issues)

                            ? issues : null

                    });

            }

            catch (Exception ex)

            {

                _logger.LogError(ex, "Health check failed");

                return StatusCode(503, new { status = "error", message = ex.Message });

            }

        }

    }

}

```



---



## 🚀 Available PowerShell Functions



### Health Check Functions

- `Test-DockerEnvironment` - Validates Docker installation and daemon

- `Test-PostgreSQLPrimary` - Checks primary node health

- `Test-PostgreSQLReplica` - Checks replica node health

- `Test-PostgreSQLReplication` - Validates replication status

- `Get-ClusterHealth` - Comprehensive cluster health check



### Monitoring Functions

- `Get-ReplicationStatus` - Current replication state

- `Get-ReplicationLag` - Replication lag in seconds

- `Get-DatabaseSize` - Database size metrics

- `Get-ConnectionCount` - Active database connections



### Alarm Functions

- `Get-HealthAlarms` - Retrieve all health alarms

- `Clear-HealthAlarms` - Clear alarm history

- `Send-HealthAlarm` - Create custom alarm



---



## 📊 API Endpoints



After implementing the controller:



| Endpoint | Method | Description |

|----------|--------|-------------|

| `/api/postgresql/health` | GET | Full cluster health status |

| `/api/postgresql/docker` | GET | Docker environment check |

| `/api/postgresql/primary` | GET | Primary node status |

| `/api/postgresql/replica` | GET | Replica node status |

| `/api/postgresql/replication` | GET | Replication metrics |

| `/api/postgresql/alarms` | GET | Active alarms |

| `/api/postgresql/healthz` | GET | Simple health endpoint |



---



## 💡 Usage Examples



### Example 1: Basic Health Check



```csharp

public class MonitoringService

{

    private readonly IPostgreSQLHealthService _health;



    public MonitoringService(IPostgreSQLHealthService health)

    {

        _health = health;

    }



    public async Task CheckHealth()

    {

        var health = await _health.GetClusterHealthAsync();



        if (!(bool)health["IsHealthy"])

        {

            // Alert team

            await SendAlert("Database cluster unhealthy!");

        }

    }

}

```



### Example 2: Background Monitoring



```csharp

public class DatabaseMonitor : BackgroundService

{

    private readonly IServiceProvider _services;

    private readonly ILogger<DatabaseMonitor> _logger;



    public DatabaseMonitor(IServiceProvider services, ILogger<DatabaseMonitor> logger)

    {

        _services = services;

        _logger = logger;

    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)

    {

        while (!stoppingToken.IsCancellationRequested)

        {

            using var scope = _services.CreateScope();

            var healthService = scope.ServiceProvider

                .GetRequiredService<IPostgreSQLHealthService>();



            var health = await healthService.GetClusterHealthAsync();



            if (!(bool)health["IsHealthy"])

            {

                _logger.LogError("Cluster unhealthy!");

            }



            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        }

    }

}

```



---



## 🐛 Troubleshooting



### Issue 1: Module Not Found



**Error:** `FileNotFoundException: PowerShell module not found`



**Solution:**

- Verify files in `bin/Debug/net6.0/PowerShellModules/BahyWay.PostgreSQLHA/`

- Check file properties: Copy to Output Directory = `Copy if newer`

- Rebuild solution



### Issue 2: Execution Policy



**Error:** `Execution policy restriction`



**Solution:**

```powershell

Set-ExecutionPolicy RemoteSigned -Scope CurrentUser

```



### Issue 3: Docker Not Running



**Error:** `Docker daemon is not running`



**Solution:**

- Start Docker Desktop

- Test: `docker ps`



### Issue 4: Package Version Conflicts



**Error:** `NU1107` or `NU1608` version conflicts



**Solution:**



Add to **both** `AlarmInsight.Infrastructure.csproj` AND `AlarmInsight.API.csproj`:



```xml

<ItemGroup>

  <PackageReference Include="Microsoft.PowerShell.SDK" Version="7.2.18" />



  <!-- Force specific versions -->

  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.5.0" />

  <PackageReference Include="Microsoft.CodeAnalysis.Common" Version="4.5.0" />

  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.5.0" />

</ItemGroup>

```



Then:

1. Build → Clean Solution

2. Close Visual Studio

3. Delete `bin` and `obj` folders in all projects

4. Reopen Visual Studio

5. Rebuild Solution



---



## ✅ Integration Checklist



- [ ] NuGet package `Microsoft.PowerShell.SDK` version 7.2.18 installed

- [ ] Package version conflicts resolved (CodeAnalysis 4.5.0)

- [ ] PowerShell module files added to project

- [ ] File properties set correctly (Content, Copy if newer)

- [ ] `PostgreSQLHealthService.cs` created in Services folder

- [ ] Service registered in DI container (Program.cs or Startup.cs)

- [ ] Solution builds without errors

- [ ] API controller added (optional)

- [ ] Docker accessible and running

- [ ] PowerShell execution policy set



---



## 📚 Module Features



✅ Cross-platform (Windows WSL2, Linux, macOS)

✅ Comprehensive health monitoring

✅ Alarm detection system

✅ Replication lag tracking

✅ Docker validation

✅ HAProxy & Barman support

✅ Try-Catch-Finally error handling

✅ Structured logging



---



## 🎯 Next Steps After Integration



1. **Test the service** - Run the application and call the health endpoints

2. **Configure monitoring** - Set up background service for continuous monitoring

3. **Add alerting** - Integrate with your alarm system

4. **Create dashboards** - Display health metrics in UI

5. **Deploy** - Test in staging environment



---



**Copyright © 2025 BahyWay Solutions. All rights reserved.**
