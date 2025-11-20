# 📘 C# For BahyWay - Part 5: Final Patterns & Complete Summary

## 🎯 What You'll Learn

Final patterns and complete reference:
- **Result Pattern**: Error handling without exceptions
- **Domain Events**: Event-driven architecture
- **Complete Quick Reference**: All patterns at a glance
- **Decision Trees**: When to use what

---

## Chapter 18: Result Pattern

### **What Is Result Pattern?**

Result Pattern = return success/failure instead of throwing exceptions. Your domain never throws!

### **Your Result<T> Implementation**

```csharp
// SharedKernel/Result.cs
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have error");
        
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have error");
        
        IsSuccess = isSuccess;
        Error = error;
    }
    
    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}

// Generic version with value
public class Result<T> : Result
{
    public T Value { get; }
    
    private Result(T value) : base(true, Error.None)
    {
        Value = value;
    }
    
    private Result(Error error) : base(false, error)
    {
        Value = default!;
    }
    
    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error error) => new(error);
}

// Error type
public record Error(string Code, string Description)
{
    public static Error None => new(string.Empty, string.Empty);
}
```

---

### **Why Result Pattern?**

**❌ WITHOUT Result (Exceptions):**
```csharp
public class Graph
{
    public Node AddNode(string name, NodeType type, Position position)
    {
        // Problem 1: Exceptions are slow
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name is required");
        
        // Problem 2: Caller doesn't know what can fail
        if (_nodes.Any(n => n.Name == name))
            throw new InvalidOperationException("Duplicate name");
        
        // Problem 3: Exceptions for flow control is bad practice
        if (_nodes.Count >= 1000)
            throw new InvalidOperationException("Too many nodes");
        
        var node = Node.Create(name, type, position);
        _nodes.Add(node);
        return node;
    }
}

// Caller must guess what can fail:
try
{
    var node = graph.AddNode("Test", NodeType.Source, position);
    // Success! But what could have gone wrong?
}
catch (ArgumentException ex) { /* which argument? */ }
catch (InvalidOperationException ex) { /* which operation? */ }
catch (Exception ex) { /* what happened? */ }
```

**✅ WITH Result Pattern:**
```csharp
public class Graph
{
    public Result<Node> AddNode(string name, NodeType type, Position position)
    {
        // Explicit, typed errors
        if (string.IsNullOrEmpty(name))
        {
            return Result<Node>.Failure(
                new Error("Graph.InvalidName", "Node name is required"));
        }
        
        if (_nodes.Any(n => n.Name == name))
        {
            return Result<Node>.Failure(
                new Error("Graph.DuplicateName", $"Node '{name}' already exists"));
        }
        
        if (_nodes.Count >= 1000)
        {
            return Result<Node>.Failure(
                new Error("Graph.TooManyNodes", "Cannot exceed 1000 nodes"));
        }
        
        var node = Node.Create(name, type, position);
        _nodes.Add(node);
        
        return Result<Node>.Success(node);
    }
}

// Caller knows exactly what can happen:
var result = graph.AddNode("Test", NodeType.Source, position);

if (result.IsSuccess)
{
    Node node = result.Value;
    Console.WriteLine($"Added: {node.Name}");
}
else
{
    // Handle specific error
    switch (result.Error.Code)
    {
        case "Graph.InvalidName":
            ShowError("Please enter a valid name");
            break;
        case "Graph.DuplicateName":
            ShowError($"Name already exists: {result.Error.Description}");
            break;
        case "Graph.TooManyNodes":
            ShowError("Graph is full");
            break;
    }
}
```

---

### **Result Pattern in Your Code**

**Domain Layer (Entities)**
```csharp
public class Graph : AggregateRoot<GraphId>
{
    // ALL mutating methods return Result
    public Result<Node> AddNode(string name, NodeType type, Position position)
    {
        // Business rule validation
        if (result.IsFailure)
            return Result<Node>.Failure(error);
        
        return Result<Node>.Success(node);
    }
    
    public Result<Edge> CreateEdge(/*...*/)
    {
        // More validation
        return Result<Edge>.Success(edge);
    }
    
    public Result RemoveNode(NodeId nodeId)
    {
        // Can fail
        return Result.Success();
    }
    
    public Result UpdateName(string newName)
    {
        // Can fail
        return Result.Success();
    }
}
```

**Application Layer (Handlers)**
```csharp
public class AddNodeCommandHandler 
    : IRequestHandler<AddNodeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        AddNodeCommand request,
        CancellationToken cancellationToken)
    {
        // Get aggregate
        var graph = await _repository.GetByIdAsync(graphId);
        
        if (graph == null)
        {
            return Result<Guid>.Failure(
                new Error("Graph.NotFound", "Graph not found"));
        }
        
        // Execute domain logic (returns Result)
        var result = graph.AddNode(/*...*/);
        
        // Propagate failure
        if (result.IsFailure)
            return Result<Guid>.Failure(result.Error);
        
        // Save
        await _repository.UpdateAsync(graph);
        
        // Return success with ID
        return Result<Guid>.Success(result.Value.Id.Value);
    }
}
```

**Presentation Layer (ViewModels)**
```csharp
public class GraphEditorViewModel : ViewModelBase
{
    public async Task AddNode()
    {
        var command = new AddNodeCommand(/*...*/);
        var result = await _mediator.Send(command);
        
        if (result.IsSuccess)
        {
            // Show success message
            var nodeId = result.Value;
            await LoadGraph();  // Refresh
            ShowSuccess($"Node added successfully!");
        }
        else
        {
            // Show error message
            ShowError(result.Error.Description);
        }
    }
}
```

---

### **Result Pattern Benefits**

1. **Explicit** - API signature shows method can fail
2. **Type-safe** - compiler forces you to handle both cases
3. **Fast** - no exception throwing/catching overhead
4. **Testable** - easy to test failure paths
5. **Composable** - chain operations easily

---

### **Composing Results**

```csharp
// Multiple operations, stop on first failure
public async Task<Result> ProcessGraph(GraphId graphId)
{
    var graph = await _repository.GetByIdAsync(graphId);
    
    if (graph == null)
        return Result.Failure(new Error("Graph.NotFound", "Not found"));
    
    // Add node
    var nodeResult = graph.AddNode("Start", NodeType.Source, position);
    if (nodeResult.IsFailure)
        return Result.Failure(nodeResult.Error);
    
    var node = nodeResult.Value;
    
    // Add another node
    var node2Result = graph.AddNode("End", NodeType.Sink, position2);
    if (node2Result.IsFailure)
        return Result.Failure(node2Result.Error);
    
    var node2 = node2Result.Value;
    
    // Connect them
    var edgeResult = graph.CreateEdge(
        node.Id, null, node2.Id, null, EdgeType.DataFlow);
    if (edgeResult.IsFailure)
        return Result.Failure(edgeResult.Error);
    
    // Save
    await _repository.UpdateAsync(graph);
    
    return Result.Success();
}

// Or with helper extension:
public static class ResultExtensions
{
    public static Result<T> OnSuccess<T>(
        this Result result,
        Func<Result<T>> func)
    {
        return result.IsFailure 
            ? Result<T>.Failure(result.Error) 
            : func();
    }
}

// Now you can chain:
public Result ProcessGraph()
{
    return ValidateGraph()
        .OnSuccess(() => AddNodes())
        .OnSuccess(() => ConnectNodes())
        .OnSuccess(() => SaveGraph());
}
```

---

## Chapter 19: Domain Events

### **What Are Domain Events?**

Domain Event = something significant happened in your domain. "GraphCreated", "NodeAdded", etc.

### **Your Domain Event Base**

```csharp
// SharedKernel/IDomainEvent.cs
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

// SharedKernel/DomainEvent.cs
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
    }
}
```

---

### **Your Domain Events**

```csharp
// Domain/Events/GraphCreatedEvent.cs
public record GraphCreatedEvent(
    GraphId GraphId,
    string Name) : DomainEvent;

// Domain/Events/NodeAddedEvent.cs
public record NodeAddedEvent(
    GraphId GraphId,
    NodeId NodeId) : DomainEvent;

// Domain/Events/EdgeCreatedEvent.cs
public record EdgeCreatedEvent(
    GraphId GraphId,
    EdgeId EdgeId) : DomainEvent;

// Domain/Events/NodeRemovedEvent.cs
public record NodeRemovedEvent(
    GraphId GraphId,
    NodeId NodeId) : DomainEvent;

// Domain/Events/GraphDeletedEvent.cs
public record GraphDeletedEvent(
    GraphId GraphId) : DomainEvent;
```

---

### **Raising Events in Aggregates**

```csharp
public class Graph : AggregateRoot<GraphId>
{
    // Inherited from AggregateRoot:
    // - List<IDomainEvent> _domainEvents
    // - AddDomainEvent(IDomainEvent)
    // - ClearDomainEvents()
    
    public static Graph Create(string name, GraphType type)
    {
        var graph = new Graph(GraphId.Create(), name, type);
        
        // Raise event when graph is created
        graph.AddDomainEvent(new GraphCreatedEvent(graph.Id, name));
        
        return graph;
    }
    
    public Result<Node> AddNode(string name, NodeType type, Position position)
    {
        // ... validation ...
        
        var node = Node.Create(name, type, position);
        _nodes.Add(node);
        
        // Raise event when node is added
        AddDomainEvent(new NodeAddedEvent(Id, node.Id));
        
        return Result.Success(node);
    }
    
    public Result<Edge> CreateEdge(/*...*/)
    {
        // ... validation ...
        
        var edge = Edge.Create(/*...*/);
        _edges.Add(edge);
        
        // Raise event when edge is created
        AddDomainEvent(new EdgeCreatedEvent(Id, edge.Id));
        
        return Result.Success(edge);
    }
    
    public Result RemoveNode(NodeId nodeId)
    {
        // ... remove node and edges ...
        
        // Raise event when node is removed
        AddDomainEvent(new NodeRemovedEvent(Id, nodeId));
        
        return Result.Success();
    }
}
```

---

### **Publishing Events**

```csharp
// Infrastructure/Persistence/Repositories/GraphRepository.cs
public class GraphRepository : IGraphRepository
{
    private readonly KGEditorDbContext _context;
    private readonly IMediator _mediator;
    
    public async Task UpdateAsync(
        Graph graph,
        CancellationToken cancellationToken = default)
    {
        // 1. Save to database
        _context.Graphs.Update(graph);
        await _context.SaveChangesAsync(cancellationToken);
        
        // 2. Publish domain events
        foreach (var domainEvent in graph.DomainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        
        // 3. Clear events
        graph.ClearDomainEvents();
    }
}
```

---

### **Handling Events**

```csharp
// Application/EventHandlers/GraphCreatedEventHandler.cs
public class GraphCreatedEventHandler 
    : INotificationHandler<GraphCreatedEvent>
{
    private readonly ILogger<GraphCreatedEventHandler> _logger;
    
    public GraphCreatedEventHandler(ILogger<GraphCreatedEventHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task Handle(
        GraphCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Graph created: {GraphId} - {Name}",
            notification.GraphId.Value,
            notification.Name);
        
        // Could also:
        // - Send notification
        // - Update read model
        // - Trigger workflow
        // - etc.
    }
}

// Application/EventHandlers/NodeAddedEventHandler.cs
public class NodeAddedEventHandler 
    : INotificationHandler<NodeAddedEvent>
{
    private readonly IGraphAnalyticsService _analytics;
    
    public async Task Handle(
        NodeAddedEvent notification,
        CancellationToken cancellationToken)
    {
        // Update analytics
        await _analytics.IncrementNodeCount(notification.GraphId);
    }
}
```

---

### **Why Domain Events?**

**Benefits:**
1. **Decoupling** - different parts of system don't know about each other
2. **Extensibility** - add new behavior without modifying existing code
3. **Audit trail** - know what happened and when
4. **Integration** - trigger external systems
5. **Eventual consistency** - update read models asynchronously

**Use Cases in BahyWay:**

```csharp
// 1. Logging
public class AuditLogEventHandler : INotificationHandler<DomainEvent>
{
    public async Task Handle(DomainEvent evt, CancellationToken ct)
    {
        await _auditLog.LogAsync(
            $"Event: {evt.GetType().Name} at {evt.OccurredAt}");
    }
}

// 2. Notifications
public class GraphCreatedEventHandler : INotificationHandler<GraphCreatedEvent>
{
    public async Task Handle(GraphCreatedEvent evt, CancellationToken ct)
    {
        await _notificationService.NotifyAsync(
            $"New graph created: {evt.Name}");
    }
}

// 3. Analytics
public class NodeCountEventHandler : INotificationHandler<NodeAddedEvent>
{
    public async Task Handle(NodeAddedEvent evt, CancellationToken ct)
    {
        await _analytics.IncrementNodeCount(evt.GraphId);
    }
}

// 4. Read Model Updates
public class GraphReadModelUpdater : INotificationHandler<NodeAddedEvent>
{
    public async Task Handle(NodeAddedEvent evt, CancellationToken ct)
    {
        await _readModelRepo.UpdateNodeCountAsync(evt.GraphId);
    }
}

// 5. Integration
public class ExternalSystemNotifier : INotificationHandler<GraphDeletedEvent>
{
    public async Task Handle(GraphDeletedEvent evt, CancellationToken ct)
    {
        await _externalApi.NotifyGraphDeletionAsync(evt.GraphId);
    }
}
```

---

## 🎯 Complete Quick Reference

### **Class Types Decision Tree**

```
What am I creating?
│
├─ Has identity & lifecycle?
│  ├─ YES → Is it a root of consistency boundary?
│  │        ├─ YES → Inherit from AggregateRoot<TId>
│  │        │        Examples: Graph, Animation
│  │        │
│  │        └─ NO → Inherit from Entity<TId>
│  │                 Examples: Node, Edge, Scene
│  │
│  └─ NO → Is it identified by values?
│           └─ YES → Inherit from ValueObject
│                    Examples: Position, Color, Duration
│
├─ Is it a contract/abstraction?
│  └─ YES → Create Interface
│           Examples: IGraphRepository, IGraphExporter
│
├─ Is it an error/failure?
│  └─ YES → Use Error record
│           Example: Error("Graph.NotFound", "Graph not found")
│
├─ Is it something that happened?
│  └─ YES → Create Domain Event
│           Examples: GraphCreatedEvent, NodeAddedEvent
│
└─ Is it a command to do something?
   └─ YES → Create Command record
            Examples: AddNodeCommand, CreateGraphCommand
```

---

### **Method Return Types Guide**

| Return Type | When to Use | Example |
|-------------|-------------|---------|
| `void` | Fire-and-forget, can't fail | `UpdateInternalState()` |
| `Result` | Can fail, no value to return | `DeleteGraph()` |
| `Result<T>` | Can fail, returns value | `AddNode()` → `Result<Node>` |
| `T` | Always succeeds, returns value | `GetNodeName()` → `string` |
| `T?` | Might not find, never fails | `FindNodeById()` → `Node?` |
| `Task` | Async, no result | `async Task SaveAsync()` |
| `Task<Result>` | Async, can fail, no value | `async Task<Result> DeleteAsync()` |
| `Task<Result<T>>` | Async, can fail, returns value | `async Task<Result<Graph>> CreateAsync()` |
| `Task<T>` | Async query (never fails) | `async Task<GraphDto> GetAsync()` |
| `Task<T?>` | Async query (might not find) | `async Task<Graph?> FindAsync()` |

---

### **Collection Types Guide**

| Collection | Use When | Example |
|------------|----------|---------|
| `List<T>` | Order matters, need index access | `List<Node> _nodes` |
| `Dictionary<K,V>` | Need fast lookup by key | `Dictionary<NodeId, Node>` |
| `HashSet<T>` | Need uniqueness + fast Contains | `HashSet<string> _usedNames` |
| `IReadOnlyList<T>` | Expose list read-only | `IReadOnlyList<Node> Nodes` |
| `IReadOnlyCollection<T>` | Expose collection read-only | Generic collections |
| `IEnumerable<T>` | Just iterate, no count/index | Query results |

---

### **LINQ Operations Cheat Sheet**

```csharp
// Filtering
.Where(n => n.Type == NodeType.Source)
.Where(n => n.Name.Contains("test"))

// Projection
.Select(n => n.Name)
.Select(n => new NodeDto { Id = n.Id, Name = n.Name })

// Finding
.First(n => n.Id == id)              // Throws if not found
.FirstOrDefault(n => n.Id == id)     // Returns null if not found
.Single(n => n.Name == "Root")       // Throws if 0 or >1
.SingleOrDefault(n => n.Name == "Root") // null if 0, throws if >1

// Checking
.Any(n => n.Type == NodeType.Source) // At least one matches?
.All(n => n.IsValid)                 // All match?
.Contains(node)                      // Collection contains item?

// Sorting
.OrderBy(n => n.Name)                // Ascending
.OrderByDescending(n => n.CreatedAt) // Descending
.ThenBy(n => n.Type)                 // Secondary sort

// Aggregating
.Count()                             // How many items
.Count(n => n.Type == NodeType.Source) // How many match
.Sum(n => n.Value)                   // Sum of values
.Average(n => n.Value)               // Average
.Min(n => n.Value) / .Max(n => n.Value) // Min/Max

// Grouping
.GroupBy(n => n.Type)                // Group by property
.ToDictionary(n => n.Id, n => n)     // Convert to dictionary

// Set operations
.Distinct()                          // Remove duplicates
.Union(otherList)                    // Combine, remove duplicates
.Intersect(otherList)                // Common items
.Except(otherList)                   // Items in first but not second

// Transforming
.ToList()                            // To List<T>
.ToArray()                           // To T[]
.ToDictionary(n => n.Id)             // To Dictionary<K,V>
.ToHashSet()                         // To HashSet<T>
```

---

### **Async Patterns**

```csharp
// Database operations - always async
Task<Graph?> GetByIdAsync(GraphId id, CancellationToken ct)
Task AddAsync(Graph graph, CancellationToken ct)
Task UpdateAsync(Graph graph, CancellationToken ct)

// File operations - always async
Task<string> ReadFileAsync(string path)
Task WriteFileAsync(string path, string content)

// Network operations - always async
Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)

// Multiple async operations
// Sequential (one after another)
var graph1 = await repo.GetByIdAsync(id1);
var graph2 = await repo.GetByIdAsync(id2);

// Parallel (all at once)
var tasks = ids.Select(id => repo.GetByIdAsync(id));
var graphs = await Task.WhenAll(tasks);

// With cancellation
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(30));
var graph = await repo.GetByIdAsync(id, cts.Token);
```

---

### **Your Complete Architecture**

```
Presentation Layer (Desktop/Web)
├── ViewModels (MVVM)
│   └── Uses MediatR to send commands/queries
│
↓
Application Layer (Use Cases)
├── Commands
│   ├── AddNodeCommand + Handler + Validator
│   └── Returns Result<T>
├── Queries
│   ├── GetGraphQuery + Handler
│   └── Returns DTO
└── Services (Interfaces)
    ├── IGraphRepository
    ├── IGraphExporter
    └── IGraphLayout
│
↓
Domain Layer (Business Logic)
├── Aggregates
│   ├── Graph (Root)
│   ├── Node (Entity)
│   └── Edge (Entity)
├── Value Objects
│   ├── Position
│   ├── Color
│   └── Duration
├── Domain Events
│   ├── GraphCreatedEvent
│   └── NodeAddedEvent
└── SharedKernel
    ├── Entity<TId>
    ├── AggregateRoot<TId>
    ├── ValueObject
    └── Result<T>
│
↓
Infrastructure Layer (External)
├── Persistence
│   ├── DbContext
│   ├── Repositories (implement IGraphRepository)
│   └── Configurations
├── Services
│   ├── JsonGraphExporter
│   └── PngGraphExporter
└── External
    └── ApacheAgeClient
```

---

## 🎉 Congratulations!

You now understand C# through YOUR actual BahyWay code!

### **What You've Learned:**

**Part 1: Fundamentals**
✅ Classes, Properties, Methods, Constructors, Access Modifiers

**Part 2: Object-Oriented Programming**
✅ Inheritance, Interfaces, Abstract Classes, Polymorphism

**Part 3: Advanced C#**
✅ Generics, Collections, LINQ, Async/Await

**Part 4: Design Patterns**
✅ Value Objects, Entities, Aggregates, Repository, CQRS

**Part 5: Advanced Patterns**
✅ Result Pattern, Domain Events

### **You Can Now:**
- ✅ Create domain models (Graph, Node, Edge)
- ✅ Implement business logic (validation, rules)
- ✅ Use CQRS (commands, queries, handlers)
- ✅ Handle errors properly (Result pattern)
- ✅ Work with collections (List, Dictionary, LINQ)
- ✅ Make async calls (database, file I/O)
- ✅ Raise and handle events (domain events)
- ✅ Follow Clean Architecture

### **Next Steps:**

1. **Practice**: Implement the patterns in your projects
2. **Reference**: Use this manual when stuck
3. **Extend**: Add new features using these patterns
4. **Refine**: Improve as you learn more

---

## 📚 File Summary

All 5 parts available:
1. [Part 1: Fundamentals](computer:///mnt/user-data/outputs/CSharp-For-BahyWay-Part1-Fundamentals.md)
2. [Part 2: OOP](computer:///mnt/user-data/outputs/CSharp-For-BahyWay-Part2-OOP.md)
3. [Part 3: Advanced C#](computer:///mnt/user-data/outputs/CSharp-For-BahyWay-Part3-Advanced.md)
4. [Part 4: Design Patterns](computer:///mnt/user-data/outputs/CSharp-For-BahyWay-Part4-Patterns.md)
5. [Part 5: Final Summary](computer:///mnt/user-data/outputs/CSharp-For-BahyWay-Part5-Final.md) (This file)

---

**Happy Coding with BahyWay!** 🚀✨

© 2025 - C# For BahyWay Learning Manual
