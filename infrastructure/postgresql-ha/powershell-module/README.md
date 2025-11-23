# BahyWay PostgreSQL HA PowerShell Module - C# Integration Guide

## 📁 Module Location

```
infrastructure/postgresql-ha/powershell-module/BahyWay.PostgreSQLHA/
├── BahyWay.PostgreSQLHA.psd1  (Module Manifest)
└── BahyWay.PostgreSQLHA.psm1  (Module Script)
```

## 🎯 Integration into C# .NET Solution

### Step 1: Add NuGet Package to AlarmInsight.Infrastructure

Add the following NuGet package to your `AlarmInsight.Infrastructure` project:

```xml
<PackageReference Include="Microsoft.PowerShell.SDK" Version="7.4.0" />
```

Or via Package Manager Console:
```powershell
Install-Package Microsoft.PowerShell.SDK -Version 7.4.0
```

### Step 2: Add PowerShell Module Files to Your Solution

1. In Visual Studio, right-click on `AlarmInsight.Infrastructure` project
2. **Add** > **New Folder** > Name it `PowerShellModules`
3. Right-click `PowerShellModules` folder > **Add** > **Existing Item**
4. Browse to: `infrastructure/postgresql-ha/powershell-module/BahyWay.PostgreSQLHA/`
5. Select both `.psd1` and `.psm1` files
6. Click **Add**
7. For each file, right-click > **Properties** > Set:
   - **Build Action**: `Content`
   - **Copy to Output Directory**: `Copy if newer`

### Step 3: Create PowerShell Service Wrapper

Create a new C# class in `AlarmInsight.Infrastructure/Services/PostgreSQLHealthService.cs`

See the example code in `CSharpIntegrationExample.cs` in this directory.

### Step 4: Register Service in DI Container

In your `AlarmInsight.Infrastructure` project, register the service:

```csharp
// In your DependencyInjection.cs or Startup.cs
services.AddScoped<IPostgreSQLHealthService, PostgreSQLHealthService>();
```

### Step 5: Use the Service

```csharp
public class YourController : ControllerBase
{
    private readonly IPostgreSQLHealthService _healthService;

    public YourController(IPostgreSQLHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        var health = await _healthService.GetClusterHealthAsync();
        return Ok(health);
    }
}
```

## 🚀 Available Functions

### Health Check Functions
- `Test-DockerEnvironment` - Check Docker availability
- `Test-PostgreSQLPrimary` - Check primary node health
- `Test-PostgreSQLReplica` - Check replica node health
- `Test-PostgreSQLReplication` - Check replication status
- `Get-ClusterHealth` - Comprehensive cluster health check

### Monitoring Functions
- `Get-ReplicationStatus` - Get replication status
- `Get-ReplicationLag` - Get replication lag metrics
- `Get-DatabaseSize` - Get database size
- `Get-ConnectionCount` - Get active connections

### Alarm Functions
- `Get-HealthAlarms` - Get all health alarms
- `Clear-HealthAlarms` - Clear health alarms
- `Send-HealthAlarm` - Send custom alarm

## 📊 Module Features

✅ **Cross-platform support** (Windows WSL2, Linux, macOS)
✅ **Comprehensive health checks** for all cluster components
✅ **Alarm detection and logging** system
✅ **Replication lag monitoring**
✅ **Docker environment validation**
✅ **Try-Catch-Finally error handling**

## 🔧 Configuration

Module configuration is stored in:
- **Linux/Mac**: `/var/log/bahyway/postgresql-ha`
- **Windows**: `%ProgramData%\BahyWay\PostgreSQLHA\Logs`

## 📝 Usage Examples

### Example 1: Get Cluster Health
```csharp
var health = await _healthService.GetClusterHealthAsync();
Console.WriteLine($"Cluster Healthy: {health["IsHealthy"]}");
```

### Example 2: Check Replication Status
```csharp
var replicationStatus = await _healthService.InvokePowerShellAsync(
    "Test-PostgreSQLReplication"
);
```

### Example 3: Get Alarms
```csharp
var alarms = await _healthService.InvokePowerShellAsync("Get-HealthAlarms");
```

## 🐛 Troubleshooting

### PowerShell Module Not Found
Ensure the module path is correct in your C# code:
```csharp
var modulePath = Path.Combine(AppContext.BaseDirectory,
    "PowerShellModules", "BahyWay.PostgreSQLHA", "BahyWay.PostgreSQLHA.psd1");
```

### Permission Issues on Linux
```bash
sudo chmod +x /path/to/module/BahyWay.PostgreSQLHA.psm1
```

### Docker Not Accessible
Ensure Docker daemon is running and accessible from PowerShell:
```powershell
docker ps
```

## 📚 Additional Resources

- [PowerShell SDK Documentation](https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/hosting-application-samples)
- [System.Management.Automation Namespace](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation)

## 🔒 Security Considerations

- The module requires Docker access
- Logs may contain sensitive database information
- Use appropriate access controls for alarm logs
- Consider implementing authentication for PowerShell execution in production

## 📄 License

Copyright (c) 2025 BahyWay Solutions. All rights reserved.
