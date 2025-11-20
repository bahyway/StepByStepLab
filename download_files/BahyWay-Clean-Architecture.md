# 🏗️ BahyWay Platform - Clean Architecture Guide

## 📋 Overview

BahyWay Platform follows **Clean Architecture** principles to ensure maintainability, testability, and scalability across all projects.

---

## 🎯 Core Principles

### **1. Dependency Rule**
Dependencies point **inward** - outer layers depend on inner layers, never the reverse.

```
┌─────────────────────────────────────────┐
│         Infrastructure Layer            │ ← External concerns
│  (EF Core, PostgreSQL, Avalonia, etc)  │
├─────────────────────────────────────────┤
│         Application Layer               │ ← Use cases
│    (Commands, Queries, Handlers)       │
├─────────────────────────────────────────┤
│           Domain Layer                  │ ← Business logic
│  (Entities, Aggregates, Value Objects) │
├─────────────────────────────────────────┤
│          SharedKernel                   │ ← Core primitives
│     (Base classes, Interfaces)         │
└─────────────────────────────────────────┘

Direction of dependencies: ⬆️ (Always inward!)
```

---

## 🏛️ Layer Structure

### **1. Domain Layer** (Core Business Logic)

**Purpose:** Contains all business logic, entities, and domain rules.

**Contents:**
- Aggregates (Graph, Node, Edge)
- Entities
- Value Objects
- Domain Events
- Domain Services
- Interfaces (no implementations)

**Dependencies:** NONE (except SharedKernel)

**Example Structure:**
```
Domain/
├── Aggregates/
│   ├── Graph/
│   │   ├── Graph.cs
│   │   ├── GraphId.cs
│   │   └── GraphType.cs
│   ├── Node/
│   │   ├── Node.cs
│   │   ├── NodeId.cs
│   │   └── NodeType.cs
│   └── Edge/
│       ├── Edge.cs
│       ├── EdgeId.cs
│       └── EdgeType.cs
├── ValueObjects/
│   ├── Position.cs
│   ├── Color.cs
│   └── Metadata.cs
├── Events/
│   ├── GraphCreatedEvent.cs
│   ├── NodeAddedEvent.cs
│   └── EdgeCreatedEvent.cs
└── Services/
    └── IGraphValidator.cs
```

**Key Pattern - Aggregate Root:**
```csharp
// Domain/Aggregates/Graph/Graph.cs
using BahyWay.SharedKernel;

namespace BahyWay.KGEditorWay.Domain.Aggregates.Graph;

public class Graph : AggregateRoot<GraphId>
{
    private readonly List<Node> _nodes = new();
    private readonly List<Edge> _edges = new();
    
    public string Name { get; private set; }
    public GraphType Type { get; private set; }
    
    public IReadOnlyList<Node> Nodes => _nodes.AsReadOnly();
    public IReadOnlyList<Edge> Edges => _edges.AsReadOnly();
    
    private Graph() { } // EF Core
    
    private Graph(GraphId id, string name, GraphType type) : base(id)
    {
        Name = Guard.Against.NullOrEmpty(name, nameof(name));
        Type = Guard.Against.Null(type, nameof(type));
    }
    
    public static Graph Create(string name, GraphType type)
    {
        var graph = new Graph(GraphId.Create(), name, type);
        graph.AddDomainEvent(new GraphCreatedEvent(graph.Id, name));
        return graph;
    }
    
    public Result<Node> AddNode(string name, NodeType type, Position position)
    {
        // Business rules
        if (_nodes.Any(n => n.Name == name))
        {
            return Result.Failure<Node>(
                new Error("Graph.DuplicateNode", 
                    $"Node with name '{name}' already exists"));
        }
        
        var node = Node.Create(name, type, position);
        _nodes.Add(node);
        
        AddDomainEvent(new NodeAddedEvent(Id, node.Id));
        
        return Result.Success(node);
    }
    
    // No dependencies on infrastructure!
}
```

---

### **2. Application Layer** (Use Cases)

**Purpose:** Orchestrates domain logic, implements use cases using CQRS pattern.

**Contents:**
- Commands & Handlers (write operations)
- Queries & Handlers (read operations)
- DTOs (Data Transfer Objects)
- Validators (FluentValidation)
- Application Services

**Dependencies:** Domain Layer only

**Example Structure:**
```
Application/
├── Commands/
│   ├── CreateGraph/
│   │   ├── CreateGraphCommand.cs
│   │   ├── CreateGraphCommandHandler.cs
│   │   └── CreateGraphCommandValidator.cs
│   ├── AddNode/
│   │   ├── AddNodeCommand.cs
│   │   ├── AddNodeCommandHandler.cs
│   │   └── AddNodeCommandValidator.cs
│   └── CreateEdge/
│       ├── CreateEdgeCommand.cs
│       └── CreateEdgeCommandHandler.cs
├── Queries/
│   ├── GetGraph/
│   │   ├── GetGraphQuery.cs
│   │   ├── GetGraphQueryHandler.cs
│   │   └── GraphDto.cs
│   └── SearchNodes/
│       ├── SearchNodesQuery.cs
│       └── SearchNodesQueryHandler.cs
└── Services/
    ├── IGraphRepository.cs (interface only!)
    └── IGraphExporter.cs
```

**Key Pattern - Command Handler:**
```csharp
// Application/Commands/AddNode/AddNodeCommand.cs
using MediatR;

public record AddNodeCommand(
    Guid GraphId,
    string Name,
    string NodeType,
    double X,
    double Y) : IRequest<Result<Guid>>;

// Application/Commands/AddNode/AddNodeCommandHandler.cs
using MediatR;

public class AddNodeCommandHandler 
    : IRequestHandler<AddNodeCommand, Result<Guid>>
{
    private readonly IGraphRepository _repository;
    
    public AddNodeCommandHandler(IGraphRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<Result<Guid>> Handle(
        AddNodeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load aggregate
        var graph = await _repository.GetByIdAsync(
            GraphId.From(request.GraphId), 
            cancellationToken);
        
        if (graph == null)
        {
            return Result.Failure<Guid>(
                new Error("Graph.NotFound", "Graph not found"));
        }
        
        // 2. Execute domain logic
        var nodeType = NodeType.FromName(request.NodeType);
        var position = Position.Create(request.X, request.Y);
        
        var result = graph.AddNode(request.Name, nodeType, position);
        
        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);
        
        // 3. Save aggregate
        await _repository.UpdateAsync(graph, cancellationToken);
        
        return Result.Success(result.Value.Id.Value);
    }
}

// Application/Commands/AddNode/AddNodeCommandValidator.cs
using FluentValidation;

public class AddNodeCommandValidator : AbstractValidator<AddNodeCommand>
{
    public AddNodeCommandValidator()
    {
        RuleFor(x => x.GraphId)
            .NotEmpty();
        
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
        
        RuleFor(x => x.NodeType)
            .NotEmpty();
    }
}
```

---

### **3. Infrastructure Layer** (External Concerns)

**Purpose:** Implements interfaces defined in Application layer, handles external dependencies.

**Contents:**
- Repository implementations (EF Core)
- Database context (DbContext)
- External services (Email, Storage, etc.)
- API clients
- File I/O

**Dependencies:** Application + Domain

**Example Structure:**
```
Infrastructure/
├── Persistence/
│   ├── KGEditorDbContext.cs
│   ├── Repositories/
│   │   └── GraphRepository.cs
│   ├── Configurations/
│   │   ├── GraphConfiguration.cs
│   │   ├── NodeConfiguration.cs
│   │   └── EdgeConfiguration.cs
│   └── Migrations/
│       └── (EF Core migrations)
├── Services/
│   ├── JsonGraphExporter.cs
│   ├── PngGraphExporter.cs
│   └── EmailService.cs
└── External/
    └── ApacheAgeClient.cs
```

**Key Pattern - Repository Implementation:**
```csharp
// Infrastructure/Persistence/Repositories/GraphRepository.cs
using Microsoft.EntityFrameworkCore;
using BahyWay.KGEditorWay.Application.Services;
using BahyWay.KGEditorWay.Domain.Aggregates.Graph;

public class GraphRepository : IGraphRepository
{
    private readonly KGEditorDbContext _context;
    
    public GraphRepository(KGEditorDbContext context)
    {
        _context = context;
    }
    
    public async Task<Graph?> GetByIdAsync(
        GraphId id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Graphs
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }
    
    public async Task AddAsync(
        Graph graph,
        CancellationToken cancellationToken = default)
    {
        await _context.Graphs.AddAsync(graph, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task UpdateAsync(
        Graph graph,
        CancellationToken cancellationToken = default)
    {
        _context.Graphs.Update(graph);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

// Infrastructure/Persistence/KGEditorDbContext.cs
using Microsoft.EntityFrameworkCore;

public class KGEditorDbContext : DbContext
{
    public DbSet<Graph> Graphs { get; set; } = null!;
    public DbSet<Node> Nodes { get; set; } = null!;
    public DbSet<Edge> Edges { get; set; } = null!;
    
    public KGEditorDbContext(DbContextOptions<KGEditorDbContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(KGEditorDbContext).Assembly);
    }
}
```

---

### **4. Presentation Layer** (UI)

**Purpose:** User interface, handles user input and displays information.

**Contents:**
- ViewModels (MVVM pattern)
- Views (XAML for Avalonia)
- Controllers (if REST API)
- DTOs for presentation

**Dependencies:** Application layer only (through MediatR)

**Example Structure:**
```
Desktop/
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── GraphEditorViewModel.cs
│   ├── NodeViewModel.cs
│   └── PropertyPanelViewModel.cs
├── Views/
│   ├── MainWindow.axaml
│   ├── GraphEditorView.axaml
│   └── PropertyPanel.axaml
├── Converters/
│   └── ColorToBrushConverter.cs
└── Services/
    └── DialogService.cs
```

**Key Pattern - ViewModel:**
```csharp
// Desktop/ViewModels/GraphEditorViewModel.cs
using ReactiveUI;
using MediatR;

public class GraphEditorViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private Graph? _currentGraph;
    
    public GraphEditorViewModel(IMediator mediator)
    {
        _mediator = mediator;
        
        AddNodeCommand = ReactiveCommand.CreateFromTask(AddNode);
        SaveGraphCommand = ReactiveCommand.CreateFromTask(SaveGraph);
    }
    
    public ReactiveCommand<Unit, Unit> AddNodeCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveGraphCommand { get; }
    
    private async Task AddNode()
    {
        if (_currentGraph == null) return;
        
        // Send command through MediatR
        var command = new AddNodeCommand(
            _currentGraph.Id.Value,
            "New Node",
            "Default",
            100,
            100);
        
        var result = await _mediator.Send(command);
        
        if (result.IsSuccess)
        {
            // Update UI
            await LoadGraph(_currentGraph.Id);
        }
        else
        {
            // Show error
            await ShowError(result.Error.Description);
        }
    }
    
    private async Task LoadGraph(GraphId id)
    {
        var query = new GetGraphQuery(id.Value);
        var result = await _mediator.Send(query);
        
        if (result.IsSuccess)
        {
            _currentGraph = result.Value;
            // Update observable collections
        }
    }
}
```

---

## 🔄 Data Flow

### **Command Flow (Write Operations)**

```
1. User Action (UI)
   ↓
2. ViewModel sends Command (MediatR)
   ↓
3. Command Handler (Application Layer)
   ↓
4. Load Aggregate from Repository
   ↓
5. Execute Domain Logic (Domain Layer)
   ↓
6. Save Aggregate via Repository
   ↓
7. Repository Implementation (Infrastructure)
   ↓
8. Database (PostgreSQL + AGE)
```

**Example:**
```csharp
// 1. User clicks "Add Node" button
// 2. ViewModel creates command
var command = new AddNodeCommand(graphId, "Node1", "Process", 100, 200);

// 3. Send to handler
var result = await _mediator.Send(command);

// 4-6. Handler executes (shown above)

// 7-8. Repository saves to database
await _repository.UpdateAsync(graph);
```

### **Query Flow (Read Operations)**

```
1. User Action (UI)
   ↓
2. ViewModel sends Query (MediatR)
   ↓
3. Query Handler (Application Layer)
   ↓
4. Direct database query (EF Core)
   ↓
5. Map to DTO
   ↓
6. Return to ViewModel
   ↓
7. Update UI
```

**Example:**
```csharp
// 1-2. ViewModel requests data
var query = new GetGraphQuery(graphId);
var graphDto = await _mediator.Send(query);

// 3-5. Handler executes query
public class GetGraphQueryHandler : IRequestHandler<GetGraphQuery, GraphDto>
{
    private readonly KGEditorDbContext _context;
    
    public async Task<GraphDto> Handle(GetGraphQuery request, CancellationToken ct)
    {
        // Direct EF Core query (no aggregate loading)
        return await _context.Graphs
            .Where(g => g.Id == request.GraphId)
            .Select(g => new GraphDto
            {
                Id = g.Id.Value,
                Name = g.Name,
                // ... project to DTO
            })
            .FirstOrDefaultAsync(ct);
    }
}
```

---

## 📐 Project Structure

### **Recommended Solution Structure:**

```
BahyWay.KGEditorWay.sln
│
├── src/
│   ├── BahyWay.SharedKernel/
│   │   ├── Entity.cs
│   │   ├── AggregateRoot.cs
│   │   ├── ValueObject.cs
│   │   └── Result.cs
│   │
│   ├── BahyWay.KGEditorWay.Domain/
│   │   ├── Aggregates/
│   │   ├── ValueObjects/
│   │   ├── Events/
│   │   └── Services/
│   │
│   ├── BahyWay.KGEditorWay.Application/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── Services/
│   │   └── DependencyInjection.cs
│   │
│   ├── BahyWay.KGEditorWay.Infrastructure/
│   │   ├── Persistence/
│   │   ├── Services/
│   │   ├── External/
│   │   └── DependencyInjection.cs
│   │
│   └── BahyWay.KGEditorWay.Desktop/
│       ├── ViewModels/
│       ├── Views/
│       ├── Program.cs
│       └── App.axaml
│
└── tests/
    ├── BahyWay.KGEditorWay.Domain.Tests/
    ├── BahyWay.KGEditorWay.Application.Tests/
    ├── BahyWay.KGEditorWay.Infrastructure.Tests/
    └── BahyWay.KGEditorWay.Desktop.Tests/
```

---

## 🔌 Dependency Injection

### **Application Layer Registration:**

```csharp
// Application/DependencyInjection.cs
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;

namespace BahyWay.KGEditorWay.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // MediatR for CQRS
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });
        
        // FluentValidation
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly);
        
        return services;
    }
}
```

### **Infrastructure Layer Registration:**

```csharp
// Infrastructure/DependencyInjection.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace BahyWay.KGEditorWay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Database
        services.AddDbContext<KGEditorDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        // Repositories
        services.AddScoped<IGraphRepository, GraphRepository>();
        
        // Services
        services.AddScoped<IGraphExporter, JsonGraphExporter>();
        
        return services;
    }
}
```

### **Desktop Application Startup:**

```csharp
// Desktop/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add layers
                services.AddApplication();
                services.AddInfrastructure(
                    context.Configuration.GetConnectionString("Default"));
                
                // Add ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<GraphEditorViewModel>();
            })
            .Build();
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
}
```

---

## ✅ Benefits of Clean Architecture

### **1. Testability**
- Domain logic has zero dependencies
- Easy to unit test business rules
- Mock infrastructure for integration tests

```csharp
// Tests/Domain/GraphTests.cs
[Fact]
public void AddNode_WithValidData_ShouldSucceed()
{
    // Arrange
    var graph = Graph.Create("Test", GraphType.Process);
    var nodeType = NodeType.Source;
    var position = Position.Create(100, 200);
    
    // Act
    var result = graph.AddNode("Node1", nodeType, position);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    graph.Nodes.Should().HaveCount(1);
}
```

### **2. Maintainability**
- Clear separation of concerns
- Each layer has single responsibility
- Easy to understand and modify

### **3. Flexibility**
- Swap implementations easily
- Change database without affecting domain
- Change UI framework without affecting business logic

### **4. Scalability**
- Add new features without modifying existing code
- Grow team with clear boundaries
- Microservices-ready architecture

---

## 🎯 Key Rules

### **✅ DO:**
- Keep domain layer pure (no dependencies)
- Use interfaces in application layer
- Implement in infrastructure layer
- Follow dependency rule (inward only)
- Use Result pattern for error handling
- Raise domain events for important actions

### **❌ DON'T:**
- Reference infrastructure from domain
- Put business logic in application handlers
- Put business logic in ViewModels
- Create circular dependencies
- Use concrete types in application layer
- Skip validation

---

## 📝 Checklist for Each Feature

### **Adding a New Feature:**

- [ ] **Domain Layer**
  - [ ] Create/update aggregate
  - [ ] Add business rules
  - [ ] Create domain events
  - [ ] Write unit tests

- [ ] **Application Layer**
  - [ ] Create command/query
  - [ ] Implement handler
  - [ ] Add validator
  - [ ] Create DTO if needed
  - [ ] Write integration tests

- [ ] **Infrastructure Layer**
  - [ ] Update EF configuration
  - [ ] Add migration
  - [ ] Implement services

- [ ] **Presentation Layer**
  - [ ] Create/update ViewModel
  - [ ] Update View
  - [ ] Add UI tests

---

## 🎓 Learning Resources

### **Recommended Reading:**
1. Clean Architecture by Robert C. Martin
2. Domain-Driven Design by Eric Evans
3. Implementing Domain-Driven Design by Vaughn Vernon

### **Example Projects:**
- See KGEditorWay complete implementation
- Check SimulateWay for CQRS examples

---

© 2025 BahyWay Platform  
**Clean Architecture Implementation Guide** 🏗️
