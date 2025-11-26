# BahyWay Ecosystem - Sprint 1 Action Plan (Week 1-2)

**Sprint Goal**: Establish ETLWay microservices foundation and deploy WPDD to production

---

## 🎯 Sprint Objectives

By the end of this 2-week sprint, you will have:
- ✅ ETLWay solution structure created
- ✅ RabbitMQ message bus running
- ✅ Orchestrator service skeleton
- ✅ First source service (Bourse)
- ✅ WPDD deployed and tested on Debian VDI

---

## 📅 Day-by-Day Plan

### **Day 1 (Monday): Architecture Finalization & Setup**

#### Morning: Review & Confirm
```
□ Read BahyWay_Ecosystem_Master_Plan.md (30 min)
□ Confirm priorities:
  - Priority 1: ETLWay (financial reconciliation need)
  - Priority 2: WPDD deployment
  - Priority 3: NajafCemetery
□ Confirm message bus choice: RabbitMQ (recommended)
□ Confirm first data source: Iraqi Bourse data
```

#### Afternoon: Environment Prep
```powershell
# On Windows development machine

# 1. Create workspace
cd C:\Users\Bahaa\source\repos\BahyWay
git pull  # Get latest SharedKernel

# 2. Install Avalonia templates (for future SSISight)
dotnet new install Avalonia.Templates

# 3. Create ETLWay workspace
mkdir -p src\ETLWay
cd src\ETLWay

# 4. Verify Docker is running
docker ps

# 5. Start RabbitMQ
docker run -d --name bahyway-rabbitmq `
  -p 5672:5672 `
  -p 15672:15672 `
  -e RABBITMQ_DEFAULT_USER=etlway `
  -e RABBITMQ_DEFAULT_PASS=etlway_dev_password `
  rabbitmq:3-management

# 6. Verify RabbitMQ is running
# Open browser: http://localhost:15672
# Login: etlway / etlway_dev_password
```

**Deliverable**: Development environment ready with RabbitMQ running

---

### **Day 2 (Tuesday): ETLWay Solution Structure**

#### Morning: Create Projects
```powershell
cd C:\Users\Bahaa\source\repos\BahyWay\src\ETLWay

# 1. Shared contracts and DTOs
dotnet new classlib -n ETLWay.Contracts -f net8.0
dotnet new classlib -n ETLWay.Common -f net8.0

# 2. Orchestrator service
dotnet new webapi -n ETLWay.Orchestrator -f net8.0
cd ETLWay.Orchestrator
dotnet add package MediatR
dotnet add package MassTransit.RabbitMQ
dotnet add package Serilog.AspNetCore
cd ..

# 3. Source service for Bourse data
dotnet new webapi -n ETLWay.Source.Bourse -f net8.0
cd ETLWay.Source.Bourse
dotnet add package MassTransit.RabbitMQ
dotnet add package CsvHelper
cd ..

# 4. Create solution file
dotnet new sln -n ETLWay
dotnet sln add ETLWay.Contracts/ETLWay.Contracts.csproj
dotnet sln add ETLWay.Common/ETLWay.Common.csproj
dotnet sln add ETLWay.Orchestrator/ETLWay.Orchestrator.csproj
dotnet sln add ETLWay.Source.Bourse/ETLWay.Source.Bourse.csproj

# 5. Add SharedKernel reference
cd ETLWay.Orchestrator
dotnet add reference ..\..\SharedKernel\BahyWay.SharedKernel.csproj
cd ..

cd ETLWay.Source.Bourse
dotnet add reference ..\..\SharedKernel\BahyWay.SharedKernel.csproj
cd ..
```

#### Afternoon: Define Contracts
```csharp
// Create file: ETLWay.Contracts/Messages/PipelineStartedEvent.cs
namespace ETLWay.Contracts.Messages;

public record PipelineStartedEvent(
    Guid PipelineId,
    string PipelineName,
    DateTime StartedAt,
    Dictionary<string, string> Configuration
);

// Create file: ETLWay.Contracts/Messages/DataExtractedEvent.cs
public record DataExtractedEvent(
    Guid PipelineId,
    string SourceName,
    int RowCount,
    string DataPayloadUrl  // URL to blob storage or embedded JSON
);

// Create file: ETLWay.Contracts/DTOs/PipelineDefinition.cs
public class PipelineDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ComponentDefinition> Components { get; set; } = new();
    public Dictionary<string, string> Configuration { get; set; } = new();
}

public class ComponentDefinition
{
    public string Type { get; set; } = string.Empty; // "source", "transform", "load"
    public string Name { get; set; } = string.Empty; // "bourse", "cleansing", "hub"
    public Dictionary<string, string> Config { get; set; } = new();
}
```

**Deliverable**: ETLWay solution with 4 projects, builds successfully

---

### **Day 3 (Wednesday): Orchestrator Service Implementation**

#### Morning: Setup Orchestrator
```csharp
// ETLWay.Orchestrator/Program.cs
using MassTransit;
using ETLWay.Contracts.Messages;

var builder = WebApplication.CreateBuilder(args);

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("etlway");
            h.Password("etlway_dev_password");
        });
        
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
```

```csharp
// ETLWay.Orchestrator/Controllers/PipelineController.cs
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using ETLWay.Contracts.Messages;
using ETLWay.Contracts.DTOs;

namespace ETLWay.Orchestrator.Controllers;

[ApiController]
[Route("api/pipelines")]
public class PipelineController : ControllerBase
{
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<PipelineController> _logger;

    public PipelineController(IPublishEndpoint publisher, ILogger<PipelineController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartPipeline([FromBody] PipelineDefinition definition)
    {
        var pipelineId = Guid.NewGuid();
        
        _logger.LogInformation("Starting pipeline {PipelineName} with ID {PipelineId}", 
            definition.Name, pipelineId);

        // Publish pipeline started event
        await _publisher.Publish(new PipelineStartedEvent(
            pipelineId,
            definition.Name,
            DateTime.UtcNow,
            definition.Configuration
        ));

        return Ok(new { pipelineId, status = "started" });
    }

    [HttpGet("{pipelineId}/status")]
    public IActionResult GetStatus(Guid pipelineId)
    {
        // TODO: Query actual status from database
        return Ok(new { pipelineId, status = "running", progress = 0 });
    }
}
```

#### Afternoon: Test Orchestrator
```powershell
# Run orchestrator
cd ETLWay.Orchestrator
dotnet run

# In another terminal, test the API
curl -X POST http://localhost:5000/api/pipelines/start `
  -H "Content-Type: application/json" `
  -d '{
    "name": "test-pipeline",
    "components": [],
    "configuration": {}
  }'

# Check RabbitMQ management UI to see the message
# http://localhost:15672 → Queues tab
```

**Deliverable**: Working orchestrator that publishes events to RabbitMQ

---

### **Day 4 (Thursday): Source Service (Bourse)**

#### Morning: Implement Bourse Source
```csharp
// ETLWay.Source.Bourse/Consumers/PipelineStartedConsumer.cs
using MassTransit;
using ETLWay.Contracts.Messages;

namespace ETLWay.Source.Bourse.Consumers;

public class PipelineStartedConsumer : IConsumer<PipelineStartedEvent>
{
    private readonly ILogger<PipelineStartedConsumer> _logger;

    public PipelineStartedConsumer(ILogger<PipelineStartedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PipelineStartedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Bourse source received pipeline start: {PipelineId}", evt.PipelineId);

        // Simulate data extraction
        await Task.Delay(2000); // Simulate fetching data

        // In real implementation:
        // - Connect to Bourse data source
        // - Extract data based on configuration
        // - Transform to standard format
        // - Publish DataExtractedEvent

        var sampleData = new[]
        {
            new { Symbol = "IBSD", Price = 1250.0, Volume = 1000000 },
            new { Symbol = "BMBI", Price = 850.5, Volume = 500000 }
        };

        await context.Publish(new DataExtractedEvent(
            evt.PipelineId,
            "BourseSource",
            sampleData.Length,
            System.Text.Json.JsonSerializer.Serialize(sampleData)
        ));

        _logger.LogInformation("Bourse source extracted {RowCount} rows for pipeline {PipelineId}", 
            sampleData.Length, evt.PipelineId);
    }
}
```

```csharp
// ETLWay.Source.Bourse/Program.cs
using MassTransit;
using ETLWay.Source.Bourse.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    // Register consumer
    x.AddConsumer<PipelineStartedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("etlway");
            h.Password("etlway_dev_password");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();
app.Run();
```

#### Afternoon: End-to-End Test
```powershell
# Terminal 1: Run Orchestrator
cd ETLWay.Orchestrator
dotnet run

# Terminal 2: Run Bourse Source
cd ETLWay.Source.Bourse
dotnet run

# Terminal 3: Start a pipeline
curl -X POST http://localhost:5000/api/pipelines/start `
  -H "Content-Type: application/json" `
  -d '{
    "name": "bourse-extraction",
    "components": [{"type": "source", "name": "bourse"}],
    "configuration": {}
  }'

# Watch logs in both terminals
# Should see:
# - Orchestrator publishes PipelineStartedEvent
# - Bourse Source receives event
# - Bourse Source publishes DataExtractedEvent
```

**Deliverable**: End-to-end message flow working (Orchestrator → Source)

---

### **Day 5 (Friday): Docker Compose + WPDD Deployment**

#### Morning: ETLWay Docker Compose
```yaml
# Create file: src/ETLWay/docker-compose.yml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: etlway-rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: etlway
      RABBITMQ_DEFAULT_PASS: etlway_dev_password
    networks:
      - etlway-network

  orchestrator:
    build:
      context: .
      dockerfile: ETLWay.Orchestrator/Dockerfile
    container_name: etlway-orchestrator
    ports:
      - "5100:80"
    depends_on:
      - rabbitmq
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      RabbitMQ__Host: rabbitmq
    networks:
      - etlway-network

  source-bourse:
    build:
      context: .
      dockerfile: ETLWay.Source.Bourse/Dockerfile
    container_name: etlway-source-bourse
    depends_on:
      - rabbitmq
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      RabbitMQ__Host: rabbitmq
    networks:
      - etlway-network

networks:
  etlway-network:
    driver: bridge
```

```dockerfile
# Create file: src/ETLWay/ETLWay.Orchestrator/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ETLWay.Orchestrator/ETLWay.Orchestrator.csproj", "ETLWay.Orchestrator/"]
COPY ["ETLWay.Contracts/ETLWay.Contracts.csproj", "ETLWay.Contracts/"]
RUN dotnet restore "ETLWay.Orchestrator/ETLWay.Orchestrator.csproj"
COPY . .
WORKDIR "/src/ETLWay.Orchestrator"
RUN dotnet build "ETLWay.Orchestrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ETLWay.Orchestrator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ETLWay.Orchestrator.dll"]
```

#### Afternoon: WPDD Deployment
```bash
# On Debian 12 VDI (SSH or direct)

# 1. Create directory
mkdir -p /opt/bahyway/wpdd
cd /opt/bahyway/wpdd

# 2. Copy WPDD files from Windows to VDI
# Use SCP, SFTP, or shared folder
# Copy entire wpdd_advanced_complete_integration/ directory

# 3. Make setup script executable
chmod +x setup.sh

# 4. Run setup
./setup.sh

# 5. Wait for all services to start (5-10 minutes)

# 6. Verify services
docker ps | grep wpdd
# Should see:
# - wpdd-ml-service
# - wpdd-postgres
# - wpdd-cassandra
# - wpdd-janusgraph
# - wpdd-redis

# 7. Test ML service
curl http://localhost:8000/health

# Expected response:
# {
#   "status": "healthy",
#   "services": {
#     "yolo": "ready",
#     "spectral": "ready",
#     "graph": "connected"
#   }
# }

# 8. Test with sample detection
# (Upload a sample satellite image via API)
```

**Deliverable**: 
- ETLWay containerized and running
- WPDD deployed and verified on Debian VDI

---

## 📋 Week 1 Checklist

```
Sprint 1 - Week 1 Completion Checklist:
─────────────────────────────────────────

Environment Setup:
☐ RabbitMQ running locally
☐ Docker Desktop functional
☐ .NET 8 SDK verified
☐ Visual Studio 2022 ready

ETLWay Foundation:
☐ Solution structure created (4 projects)
☐ Contracts defined (messages, DTOs)
☐ Orchestrator service running
☐ Bourse source service running
☐ End-to-end message flow verified
☐ Docker Compose configuration created

WPDD Deployment:
☐ Files copied to Debian VDI
☐ Setup script executed successfully
☐ All 5 services running (docker ps)
☐ ML service health check passing
☐ Sample detection tested

Documentation:
☐ Architecture decisions documented
☐ Setup procedures recorded
☐ Issues/blockers logged

Git:
☐ ETLWay committed to BahyWay repo
☐ Pushed to remote
☐ PR created (if using feature branches)
```

---

## 🛠️ Week 2 Plan Preview

**Goals**:
- Add PostgreSQL for orchestrator state
- Implement transform service (data cleansing)
- Create load service skeleton (Hub table)
- Design Data Vault 2.0 schema
- WPDD: Test with real satellite imagery

**By End of Week 2**: Complete data flow from Source → Transform → Load → Database

---

## 🚨 Common Issues & Solutions

### Issue 1: RabbitMQ Connection Failed
```
Error: "Connection refused to localhost:5672"

Solution:
1. Check Docker: docker ps | grep rabbitmq
2. Check logs: docker logs bahyway-rabbitmq
3. Verify port: netstat -an | findstr 5672
4. Restart: docker restart bahyway-rabbitmq
```

### Issue 2: MassTransit Configuration Error
```
Error: "Unable to resolve service for type 'MassTransit.IPublishEndpoint'"

Solution:
1. Verify AddMassTransit() called in Program.cs
2. Ensure UsingRabbitMq() configured
3. Check NuGet packages installed:
   - MassTransit
   - MassTransit.RabbitMQ
```

### Issue 3: Bourse Source Not Receiving Events
```
Error: Consumer not receiving PipelineStartedEvent

Solution:
1. Check consumer is registered: x.AddConsumer<PipelineStartedConsumer>()
2. Verify RabbitMQ queue created (Management UI)
3. Check exchange bindings
4. Restart source service
```

### Issue 4: WPDD Setup.sh Fails
```
Error: "Docker daemon not accessible"

Solution:
1. Start Docker: sudo systemctl start docker
2. Add user to docker group: sudo usermod -aG docker $USER
3. Logout and login again
4. Test: docker ps
```

---

## 📊 Success Metrics - Week 1

By Friday evening, you should have:
- ✅ 2 services communicating via RabbitMQ
- ✅ ETLWay foundation (4 projects building)
- ✅ WPDD running on production VDI
- ✅ Clear understanding of microservices pattern
- ✅ Confidence to proceed with Week 2

**Lines of Code**: ~500-600 lines (mostly boilerplate, contracts, config)

**Time Investment**: 30-40 hours (full-time)

**Learning**: RabbitMQ, MassTransit, microservices communication

---

## 🎯 Week 1 Demo

**Friday Demo to Yourself**:

1. **Start All Services**:
   ```powershell
   cd src\ETLWay
   docker-compose up -d
   ```

2. **Trigger Pipeline**:
   ```powershell
   curl -X POST http://localhost:5100/api/pipelines/start `
     -H "Content-Type: application/json" `
     -d '{"name": "demo-pipeline", "components": [], "configuration": {}}'
   ```

3. **Watch RabbitMQ**: Open http://localhost:15672, see messages flowing

4. **Check WPDD**: SSH to Debian VDI, `curl http://localhost:8000/health`

**If all 4 tests pass → Sprint 1 Week 1 SUCCESS! 🎉**

---

## 📞 Need Help?

**Stuck on something?** Ask these questions:
1. "How do I troubleshoot RabbitMQ connection issues?"
2. "Show me example MassTransit consumer code"
3. "How do I test WPDD with sample satellite image?"
4. "Explain Data Vault 2.0 Hub/Link/Satellite pattern"

---

## 🚀 Ready to Start?

**First Command**:
```powershell
cd C:\Users\Bahaa\source\repos\BahyWay\src
mkdir ETLWay
cd ETLWay
dotnet new classlib -n ETLWay.Contracts
```

**Let's build the BahyWay Ecosystem! 💪**
