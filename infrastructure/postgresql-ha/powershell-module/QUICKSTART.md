# 🚀 Quick Start Guide: Integrating PowerShell Module into BahyWay C# Solution

## ⚡ 5-Minute Integration

Follow these steps to integrate the BahyWay PostgreSQL HA PowerShell module into your Visual Studio solution.

---

## Step 1: Add NuGet Package (2 minutes)

### Option A: Using Package Manager Console
1. Open **Package Manager Console** in Visual Studio
2. Select **AlarmInsight.Infrastructure** project
3. Run:
   ```powershell
   Install-Package Microsoft.PowerShell.SDK -Version 7.4.0
   ```

### Option B: Using NuGet Package Manager UI
1. Right-click **AlarmInsight.Infrastructure** project
2. Click **Manage NuGet Packages**
3. Search for: `Microsoft.PowerShell.SDK`
4. Install version **7.4.0** or later

---

## Step 2: Add PowerShell Module Files to Solution (1 minute)

### Method 1: Add as Content Files (Recommended)

1. In Visual Studio **Solution Explorer**
2. Right-click **AlarmInsight.Infrastructure** project
3. **Add** → **New Folder** → Name it: `PowerShellModules`
4. Right-click `PowerShellModules` → **Add** → **New Folder** → Name it: `BahyWay.PostgreSQLHA`
5. Right-click `BahyWay.PostgreSQLHA` → **Add** → **Existing Item**
6. Browse to: `infrastructure/postgresql-ha/powershell-module/BahyWay.PostgreSQLHA/`
7. Select **both** files:
   - `BahyWay.PostgreSQLHA.psd1`
   - `BahyWay.PostgreSQLHA.psm1`
8. Click **Add**

9. **Configure file properties** (IMPORTANT):
   - Select both files in Solution Explorer
   - Press **F4** to open Properties window
   - Set:
     - **Build Action**: `Content`
     - **Copy to Output Directory**: `Copy if newer`

### Method 2: Add as Link (Alternative)

If you want to keep files in the infrastructure folder and reference them:

1. Right-click **AlarmInsight.Infrastructure** project
2. **Add** → **Existing Item**
3. Browse to the module files
4. Click dropdown on **Add** button → Select **Add As Link**
5. Set properties as above

---

## Step 3: Add C# Service Class (1 minute)

1. In **AlarmInsight.Infrastructure** project
2. Create folder: `Services` (if it doesn't exist)
3. Right-click `Services` → **Add** → **Class**
4. Name: `PostgreSQLHealthService.cs`
5. Copy the entire code from: `infrastructure/postgresql-ha/powershell-module/CSharpIntegrationExample.cs`
6. Paste into `PostgreSQLHealthService.cs`
7. Ensure namespace matches your project: `AlarmInsight.Infrastructure.Services`

---

## Step 4: Register Service in DI Container (30 seconds)

### For .NET 6+ (Program.cs):

```csharp
using AlarmInsight.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// ⭐ Add this line:
builder.Services.AddScoped<IPostgreSQLHealthService, PostgreSQLHealthService>();

var app = builder.Build();

app.MapControllers();
app.Run();
```

### For .NET 5 and earlier (Startup.cs):

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();

    // ⭐ Add this line:
    services.AddScoped<IPostgreSQLHealthService, PostgreSQLHealthService>();
}
```

---

## Step 5: Add API Controller (Optional - 1 minute)

1. In **AlarmInsight.API** project
2. Right-click `Controllers` folder → **Add** → **Class**
3. Name: `PostgreSQLHealthController.cs`
4. Copy code from: `infrastructure/postgresql-ha/powershell-module/ExampleAPIController.cs`
5. Paste and save

---

## Step 6: Test Integration (30 seconds)

### Build the Solution
1. Press **Ctrl+Shift+B** to build
2. Verify no errors

### Run and Test
1. Press **F5** to run
2. Navigate to: `https://localhost:XXXX/api/postgresql/health`
3. You should see cluster health data

---

## 🎯 Quick Usage Examples

### Example 1: Inject into a Service

```csharp
public class AlarmService
{
    private readonly IPostgreSQLHealthService _healthService;

    public AlarmService(IPostgreSQLHealthService healthService)
    {
        _healthService = healthService;
    }

    public async Task CheckDatabaseHealth()
    {
        var health = await _healthService.GetClusterHealthAsync();

        if (!(bool)health["IsHealthy"])
        {
            // Send alert
            await SendAlert("Database cluster is unhealthy!");
        }
    }
}
```

### Example 2: Use in Controller

```csharp
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IPostgreSQLHealthService _dbHealth;

    public HealthController(IPostgreSQLHealthService dbHealth)
    {
        _dbHealth = dbHealth;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var health = await _dbHealth.GetClusterHealthAsync();
        return Ok(health);
    }
}
```

### Example 3: Background Service Monitoring

```csharp
public class DatabaseMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMonitoringService> _logger;

    public DatabaseMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var healthService = scope.ServiceProvider
                .GetRequiredService<IPostgreSQLHealthService>();

            var health = await healthService.GetClusterHealthAsync();

            if (!(bool)health["IsHealthy"])
            {
                _logger.LogError("Database cluster unhealthy!");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

---

## 📊 Available API Endpoints

After adding the controller, you'll have these endpoints:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/postgresql/health` | GET | Comprehensive cluster health |
| `/api/postgresql/docker` | GET | Docker environment status |
| `/api/postgresql/primary` | GET | Primary node health |
| `/api/postgresql/replica` | GET | Replica node health |
| `/api/postgresql/replication` | GET | Replication status |
| `/api/postgresql/alarms` | GET | Health alarms |
| `/api/postgresql/healthz` | GET | Simple health check |

---

## 🧪 Testing the Integration

### Unit Test Example

```csharp
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

public class PostgreSQLHealthServiceTests
{
    [Fact]
    public async Task GetClusterHealth_ShouldReturnHealthData()
    {
        // Arrange
        var logger = new Mock<ILogger<PostgreSQLHealthService>>();
        var service = new PostgreSQLHealthService(logger.Object);

        // Act
        var result = await service.GetClusterHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("IsHealthy"));
    }
}
```

---

## 🐛 Troubleshooting

### Issue 1: Module Not Found

**Error**: `FileNotFoundException: PowerShell module not found`

**Solution**:
- Verify files are in `bin/Debug/net6.0/PowerShellModules/BahyWay.PostgreSQLHA/`
- Check file properties: **Copy to Output Directory** = `Copy if newer`
- Rebuild solution

### Issue 2: PowerShell Execution Policy

**Error**: `Execution policy restriction`

**Solution**:
- Run PowerShell as Administrator:
  ```powershell
  Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
  ```

### Issue 3: Docker Not Accessible

**Error**: `Docker daemon is not running`

**Solution**:
- Start Docker Desktop
- Verify: `docker ps` in terminal
- Ensure Docker is accessible from PowerShell

### Issue 4: Permission Denied on Linux

**Error**: `Permission denied` when executing module

**Solution**:
```bash
chmod +x infrastructure/postgresql-ha/powershell-module/BahyWay.PostgreSQLHA/*.psm1
```

---

## 📝 Project File Reference

Your `AlarmInsight.Infrastructure.csproj` should include:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.PowerShell.SDK" Version="7.4.0" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="PowerShellModules\BahyWay.PostgreSQLHA\*.psd1">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
    <Content Include="PowerShellModules\BahyWay.PostgreSQLHA\*.psm1">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

---

## 🎉 Next Steps

1. **Configure monitoring**: Set up background service for continuous health monitoring
2. **Add alerting**: Integrate with your alarm system
3. **Create dashboards**: Display health metrics in UI
4. **Add logging**: Configure structured logging for all health checks
5. **Deploy**: Test in staging environment

---

## 📚 Additional Resources

- **Full Documentation**: See `README.md` in this folder
- **C# Integration Example**: `CSharpIntegrationExample.cs`
- **API Controller Example**: `ExampleAPIController.cs`
- **DI Setup**: `DependencyInjectionSetup.cs`

---

## ✅ Checklist

- [ ] NuGet package `Microsoft.PowerShell.SDK` installed
- [ ] PowerShell module files added to project
- [ ] File properties set (Build Action: Content, Copy: Copy if newer)
- [ ] `PostgreSQLHealthService.cs` added to Infrastructure project
- [ ] Service registered in DI container
- [ ] Solution builds without errors
- [ ] API endpoints tested and working
- [ ] Docker is running and accessible

---

**Need help?** Check the full README.md or contact the BahyWay team.

**Copyright © 2025 BahyWay Solutions. All rights reserved.**
