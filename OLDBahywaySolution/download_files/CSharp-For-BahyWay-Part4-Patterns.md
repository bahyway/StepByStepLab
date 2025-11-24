# 📘 C# For BahyWay - Part 4: Design Patterns You Actually Use

## 🎯 What You'll Learn

The exact patterns in YOUR code, why they exist, and alternatives:
- **Value Objects**: Position, Color, Duration
- **Entities & Aggregates**: Node, Graph, Animation
- **Repository Pattern**: IGraphRepository
- **CQRS**: Commands & Queries
- **Result Pattern**: Success/Failure handling
- **Domain Events**: Event-driven architecture

---

## Chapter 14: Value Objects

### **What Is a Value Object?**

Value Object = identified by its VALUE, not identity. Two positions at (100, 200) are THE SAME.

### **Your Real Value Object: Position**

```csharp
// Domain/ValueObjects/Position.cs
public class Position : ValueObject
{
    public double X { get; private set; }
    public double Y { get; private set; }
    
    private Position(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    // Factory method with validation
    public static Position Create(double x, double y)
    {
        if (x < 0) throw new ArgumentException("X cannot be negative");
        if (y < 0) throw new ArgumentException("Y cannot be negative");
        
        return new Position(x, y);
    }
    
    // Equality based on VALUES
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }
    
    // Business logic belongs here!
    public double DistanceTo(Position other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
    
    public Position MoveBy(double deltaX, double deltaY)
    {
        return new Position(X + deltaX, Y + deltaY);
    }
}
```

**Why Value Object?**

```csharp
// ❌ WITHOUT Value Object (old way)
public class Node
{
    public double X { get; set; }
    public double Y { get; set; }
}

// Problems:
node.X = -100;  // Can set invalid values!
node.Y = -100;  // No validation
// How do you calculate distance? Logic scattered everywhere

// ✅ WITH Value Object (your way)
public class Node
{
    public Position Position { get; private set; }
}

// Benefits:
var pos = Position.Create(100, 200);  // Validated!
node.UpdatePosition(pos);  // Can't be invalid
var distance = pos.DistanceTo(other); // Logic encapsulated
```

**Key Characteristics:**
1. **Immutable** - can't change after creation
2. **Value equality** - compared by values, not reference
3. **Self-validating** - can't create invalid value objects
4. **Behavior** - contains related logic

---

### **Your Other Value Objects**

**Color**
```csharp
public class Color : ValueObject
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }
    
    private Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    public static Color FromRgb(byte r, byte g, byte b) => new(r, g, b);
    public static Color FromArgb(byte a, byte r, byte g, byte b) => new(r, g, b, a);
    
    // Pre-defined colors
    public static Color Red => new(255, 0, 0);
    public static Color Green => new(0, 255, 0);
    public static Color Blue => new(0, 0, 255);
    public static Color White => new(255, 255, 255);
    public static Color Black => new(0, 0, 0);
    
    // Equality
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return R;
        yield return G;
        yield return B;
        yield return A;
    }
    
    // Behavior
    public Color WithAlpha(byte alpha) => new(R, G, B, alpha);
    
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";
}

// Usage:
var nodeColor = Color.Blue;
var transparentBlue = nodeColor.WithAlpha(128);
var hexCode = nodeColor.ToHex(); // "#0000FF"
```

**Duration**
```csharp
public class Duration : ValueObject
{
    public int Milliseconds { get; }
    
    private Duration(int milliseconds)
    {
        if (milliseconds < 0) 
            throw new ArgumentException("Duration cannot be negative");
        
        Milliseconds = milliseconds;
    }
    
    public static Duration FromMilliseconds(int ms) => new(ms);
    public static Duration FromSeconds(double seconds) => new((int)(seconds * 1000));
    public static Duration FromMinutes(double minutes) => new((int)(minutes * 60000));
    
    public static Duration Zero => new(0);
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Milliseconds;
    }
    
    // Behavior
    public double TotalSeconds => Milliseconds / 1000.0;
    public double TotalMinutes => Milliseconds / 60000.0;
    
    public Duration Add(Duration other) => new(Milliseconds + other.Milliseconds);
    public Duration Subtract(Duration other) => new(Milliseconds - other.Milliseconds);
    
    // Operators
    public static Duration operator +(Duration left, Duration right) 
        => left.Add(right);
    
    public static Duration operator -(Duration left, Duration right) 
        => left.Subtract(right);
}

// Usage in SimulateWay:
var scene1Duration = Duration.FromSeconds(2.5);
var scene2Duration = Duration.FromSeconds(3.0);
var totalDuration = scene1Duration + scene2Duration; // 5.5 seconds
```

**Metadata**
```csharp
public class Metadata : ValueObject
{
    private readonly Dictionary<string, string> _properties;
    
    public IReadOnlyDictionary<string, string> Properties => _properties;
    
    private Metadata(Dictionary<string, string> properties)
    {
        _properties = properties;
    }
    
    public static Metadata Empty => new(new Dictionary<string, string>());
    
    public static Metadata Create(Dictionary<string, string> properties) 
        => new(properties);
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var kvp in _properties.OrderBy(x => x.Key))
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }
    
    // Behavior
    public Metadata WithProperty(string key, string value)
    {
        var newProperties = new Dictionary<string, string>(_properties)
        {
            [key] = value
        };
        return new Metadata(newProperties);
    }
    
    public string? GetProperty(string key)
    {
        return _properties.TryGetValue(key, out var value) ? value : null;
    }
}

// Usage:
var metadata = Metadata.Empty
    .WithProperty("author", "Bahaa Fadam")
    .WithProperty("version", "1.0")
    .WithProperty("created", DateTime.UtcNow.ToString());

var author = metadata.GetProperty("author");
```

---

### **Value Object vs Entity**

| Value Object | Entity |
|--------------|--------|
| Identified by VALUES | Identified by ID |
| Immutable | Can change |
| No identity | Has unique ID |
| Examples: Position, Color, Duration | Examples: Node, Graph, Animation |

**Decision Tree:**
```
Does it have a lifecycle (created, modified, deleted)?
├─ YES → Entity
│        Examples: Graph, Node, User
│
└─ NO → Does equality depend on values?
         └─ YES → Value Object
                  Examples: Position, Color, Email
```

---

## Chapter 15: Entities & Aggregates

### **What Are Entities?**

Entity = has IDENTITY. Two nodes with same name are DIFFERENT if IDs differ.

### **Your Entity: Node**

```csharp
// Domain/Aggregates/Node/Node.cs
public class Node : Entity<NodeId>
{
    // Identity from base class
    public NodeId Id { get; } // From Entity<NodeId>
    
    // Mutable properties
    public string Name { get; private set; }
    public NodeType Type { get; private set; }
    public Position Position { get; private set; }
    public Color Color { get; private set; }
    public Metadata Metadata { get; private set; }
    
    // Private constructor
    private Node(NodeId id, string name, NodeType type, Position position) 
        : base(id)
    {
        Name = Guard.Against.NullOrEmpty(name, nameof(name));
        Type = Guard.Against.Null(type, nameof(type));
        Position = Guard.Against.Null(position, nameof(position));
        Color = type.DefaultColor;
        Metadata = Metadata.Empty;
    }
    
    // Factory method
    public static Node Create(string name, NodeType type, Position position)
    {
        return new Node(NodeId.Create(), name, type, position);
    }
    
    // Behavior (commands that change state)
    public Result UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(new Error("Node.InvalidName", "Name cannot be empty"));
        
        Name = newName;
        return Result.Success();
    }
    
    public void UpdatePosition(Position newPosition)
    {
        Position = Guard.Against.Null(newPosition, nameof(newPosition));
    }
    
    public void SetColor(Color color)
    {
        Color = Guard.Against.Null(color, nameof(color));
    }
    
    public void AddMetadata(string key, string value)
    {
        Metadata = Metadata.WithProperty(key, value);
    }
}
```

**Entity Characteristics:**
1. **Has ID** - unique identifier
2. **Mutable** - can change over time
3. **Identity equality** - equal if IDs match
4. **Has lifecycle** - created, modified, deleted

---

### **What Are Aggregates?**

Aggregate = cluster of entities with one root. Graph is root, contains Nodes and Edges.

### **Your Aggregate: Graph**

```csharp
// Domain/Aggregates/Graph/Graph.cs
public class Graph : AggregateRoot<GraphId>
{
    // Aggregate Root responsibilities:
    // 1. Enforce invariants (business rules)
    // 2. Control access to entities inside
    // 3. Raise domain events
    
    // Private collections - ONLY Graph can modify
    private readonly List<Node> _nodes = new();
    private readonly List<Edge> _edges = new();
    
    // Public read-only access
    public GraphId Id { get; } // From AggregateRoot
    public string Name { get; private set; }
    public GraphType Type { get; private set; }
    public IReadOnlyList<Node> Nodes => _nodes.AsReadOnly();
    public IReadOnlyList<Edge> Edges => _edges.AsReadOnly();
    
    // Factory method
    public static Graph Create(string name, GraphType type)
    {
        var graph = new Graph(GraphId.Create(), name, type);
        graph.AddDomainEvent(new GraphCreatedEvent(graph.Id, name));
        return graph;
    }
    
    // Aggregate enforces business rules!
    public Result<Node> AddNode(string name, NodeType type, Position position)
    {
        // Rule 1: Name must be unique
        if (_nodes.Any(n => n.Name == name))
        {
            return Result.Failure<Node>(
                new Error("Graph.DuplicateNodeName", 
                    $"Node with name '{name}' already exists"));
        }
        
        // Rule 2: Maximum nodes
        if (_nodes.Count >= 1000)
        {
            return Result.Failure<Node>(
                new Error("Graph.TooManyNodes", 
                    "Cannot exceed 1000 nodes"));
        }
        
        // Create and add
        var node = Node.Create(name, type, position);
        _nodes.Add(node);
        
        // Raise event
        AddDomainEvent(new NodeAddedEvent(Id, node.Id));
        
        return Result.Success(node);
    }
    
    public Result<Edge> CreateEdge(
        NodeId sourceId, 
        Port? sourcePort,
        NodeId targetId, 
        Port? targetPort,
        EdgeType type)
    {
        // Rule 1: Nodes must exist
        var source = _nodes.FirstOrDefault(n => n.Id == sourceId);
        var target = _nodes.FirstOrDefault(n => n.Id == targetId);
        
        if (source == null || target == null)
        {
            return Result.Failure<Edge>(
                new Error("Graph.NodeNotFound", "Source or target node not found"));
        }
        
        // Rule 2: Can't connect to self
        if (sourceId == targetId)
        {
            return Result.Failure<Edge>(
                new Error("Graph.SelfConnection", "Cannot connect node to itself"));
        }
        
        // Rule 3: Connection must not already exist
        if (_edges.Any(e => e.SourceNodeId == sourceId && e.TargetNodeId == targetId))
        {
            return Result.Failure<Edge>(
                new Error("Graph.DuplicateEdge", "Connection already exists"));
        }
        
        // Create and add
        var edge = Edge.Create(sourceId, sourcePort, targetId, targetPort, type);
        _edges.Add(edge);
        
        // Raise event
        AddDomainEvent(new EdgeCreatedEvent(Id, edge.Id));
        
        return Result.Success(edge);
    }
    
    public Result RemoveNode(NodeId nodeId)
    {
        var node = _nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null)
        {
            return Result.Failure(
                new Error("Graph.NodeNotFound", "Node not found"));
        }
        
        // Rule: Remove connected edges first
        var connectedEdges = _edges
            .Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
            .ToList();
        
        foreach (var edge in connectedEdges)
        {
            _edges.Remove(edge);
        }
        
        _nodes.Remove(node);
        
        AddDomainEvent(new NodeRemovedEvent(Id, nodeId));
        
        return Result.Success();
    }
}
```

**Aggregate Rules:**
1. **One Root** - Graph is the root
2. **Enforce Invariants** - all business rules in root
3. **Transaction Boundary** - save entire aggregate at once
4. **External Access** - only through root

**❌ What NOT to do:**
```csharp
// Don't expose nodes for external modification
public List<Node> Nodes { get; } // ❌ Anyone can modify!

// Don't allow direct edge creation
var edge = new Edge(...);  // ❌ Bypasses validation!
graph.Edges.Add(edge);     // ❌ No business rules checked!

// Don't cross aggregate boundaries
graph1.Nodes.Add(graph2.Nodes[0]); // ❌ Sharing entities!
```

**✅ What TO do:**
```csharp
// Expose read-only
public IReadOnlyList<Node> Nodes => _nodes.AsReadOnly();

// Provide methods with business rules
public Result<Edge> CreateEdge(...) { /* validation */ }

// Keep aggregates separate
var node = graph1.AddNode(...);  // Create in graph1
graph2.AddNode(...);             // Create separately in graph2
```

---

### **Aggregate Boundaries**

```
Graph Aggregate (Root)
├── Graph (Root Entity)
├── Node (Entity - part of aggregate)
├── Node (Entity - part of aggregate)
├── Edge (Entity - part of aggregate)
└── Edge (Entity - part of aggregate)

Animation Aggregate (Root)
├── Animation (Root Entity)
├── Scene (Entity - part of aggregate)
├── Scene (Entity - part of aggregate)
└── Timeline (Value Object)
```

**Cross-Aggregate References:**
```csharp
// ❌ Don't: Reference entity directly
public class Animation
{
    public Graph Graph { get; set; } // ❌ Wrong aggregate!
}

// ✅ Do: Reference by ID
public class Animation
{
    public GraphId GraphId { get; set; } // ✅ Just ID
}

// Load separately:
var animation = await animationRepo.GetByIdAsync(animationId);
var graph = await graphRepo.GetByIdAsync(animation.GraphId);
```

---

## Chapter 16: Repository Pattern

### **What Is Repository Pattern?**

Repository = abstraction over data storage. Application doesn't know if it's PostgreSQL, MongoDB, or in-memory.

### **Your Repository Interface**

```csharp
// Application/Services/IGraphRepository.cs
public interface IGraphRepository
{
    // Get single by ID
    Task<Graph?> GetByIdAsync(
        GraphId id, 
        CancellationToken cancellationToken = default);
    
    // Get all
    Task<List<Graph>> GetAllAsync(
        CancellationToken cancellationToken = default);
    
    // Query with filter
    Task<List<Graph>> FindAsync(
        Func<Graph, bool> predicate,
        CancellationToken cancellationToken = default);
    
    // Add new
    Task AddAsync(
        Graph graph, 
        CancellationToken cancellationToken = default);
    
    // Update existing
    Task UpdateAsync(
        Graph graph, 
        CancellationToken cancellationToken = default);
    
    // Delete
    Task DeleteAsync(
        GraphId id, 
        CancellationToken cancellationToken = default);
    
    // Check existence
    Task<bool> ExistsAsync(
        GraphId id,
        CancellationToken cancellationToken = default);
}
```

---

### **Your Repository Implementation (PostgreSQL)**

```csharp
// Infrastructure/Persistence/Repositories/GraphRepository.cs
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
            .Include(g => g.Nodes)     // Load nodes
            .Include(g => g.Edges)     // Load edges
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }
    
    public async Task<List<Graph>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Graphs
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .ToListAsync(cancellationToken);
    }
    
    public async Task AddAsync(
        Graph graph, 
        CancellationToken cancellationToken = default)
    {
        await _context.Graphs.AddAsync(graph, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Publish domain events after saving
        await PublishDomainEventsAsync(graph, cancellationToken);
    }
    
    public async Task UpdateAsync(
        Graph graph, 
        CancellationToken cancellationToken = default)
    {
        _context.Graphs.Update(graph);
        await _context.SaveChangesAsync(cancellationToken);
        
        await PublishDomainEventsAsync(graph, cancellationToken);
    }
    
    public async Task DeleteAsync(
        GraphId id, 
        CancellationToken cancellationToken = default)
    {
        var graph = await GetByIdAsync(id, cancellationToken);
        if (graph != null)
        {
            _context.Graphs.Remove(graph);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
    
    public async Task<bool> ExistsAsync(
        GraphId id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Graphs
            .AnyAsync(g => g.Id == id, cancellationToken);
    }
    
    private async Task PublishDomainEventsAsync(
        Graph graph,
        CancellationToken cancellationToken)
    {
        // Publish events through MediatR
        foreach (var domainEvent in graph.DomainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        
        graph.ClearDomainEvents();
    }
}
```

---

### **Alternative: In-Memory Repository (Testing)**

```csharp
// Tests/Fakes/InMemoryGraphRepository.cs
public class InMemoryGraphRepository : IGraphRepository
{
    private readonly List<Graph> _graphs = new();
    
    public Task<Graph?> GetByIdAsync(
        GraphId id, 
        CancellationToken cancellationToken = default)
    {
        var graph = _graphs.FirstOrDefault(g => g.Id == id);
        return Task.FromResult(graph);
    }
    
    public Task<List<Graph>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_graphs.ToList());
    }
    
    public Task AddAsync(
        Graph graph, 
        CancellationToken cancellationToken = default)
    {
        _graphs.Add(graph);
        return Task.CompletedTask;
    }
    
    public Task UpdateAsync(
        Graph graph, 
        CancellationToken cancellationToken = default)
    {
        var existing = _graphs.FirstOrDefault(g => g.Id == graph.Id);
        if (existing != null)
        {
            _graphs.Remove(existing);
            _graphs.Add(graph);
        }
        return Task.CompletedTask;
    }
    
    public Task DeleteAsync(
        GraphId id, 
        CancellationToken cancellationToken = default)
    {
        var graph = _graphs.FirstOrDefault(g => g.Id == id);
        if (graph != null)
        {
            _graphs.Remove(graph);
        }
        return Task.CompletedTask;
    }
    
    public Task<bool> ExistsAsync(
        GraphId id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_graphs.Any(g => g.Id == id));
    }
}
```

---

### **Why Repository Pattern?**

**Benefits:**
1. **Testability** - swap with in-memory for tests
2. **Flexibility** - change database without changing application
3. **Abstraction** - application doesn't know about EF Core
4. **Centralization** - all data access in one place

**Using in Tests:**
```csharp
[Fact]
public async Task AddNode_WithValidData_ShouldSucceed()
{
    // Arrange
    var repository = new InMemoryGraphRepository(); // Fast, no database!
    var handler = new AddNodeCommandHandler(repository);
    
    var graph = Graph.Create("Test Graph", GraphType.Process);
    await repository.AddAsync(graph);
    
    var command = new AddNodeCommand(
        graph.Id.Value,
        "Test Node",
        "Source",
        100,
        200);
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

---

## Chapter 17: CQRS (Commands & Queries)

### **What Is CQRS?**

CQRS = Command Query Responsibility Segregation. Separate reads from writes.

**Commands** = Change state (AddNode, DeleteGraph)  
**Queries** = Read state (GetGraph, ListGraphs)

### **Your Command: AddNodeCommand**

```csharp
// Application/Commands/AddNode/AddNodeCommand.cs
public record AddNodeCommand(
    Guid GraphId,
    string Name,
    string NodeType,
    double X,
    double Y) : IRequest<Result<Guid>>;
//              👆 Returns Result with new Node ID

// Application/Commands/AddNode/AddNodeCommandHandler.cs
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
        
        // 4. Return new node ID
        return Result.Success(result.Value.Id.Value);
    }
}

// Application/Commands/AddNode/AddNodeCommandValidator.cs
public class AddNodeCommandValidator : AbstractValidator<AddNodeCommand>
{
    public AddNodeCommandValidator()
    {
        RuleFor(x => x.GraphId)
            .NotEmpty().WithMessage("Graph ID is required");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Node name is required")
            .MaximumLength(100).WithMessage("Name too long");
        
        RuleFor(x => x.NodeType)
            .NotEmpty().WithMessage("Node type is required");
        
        RuleFor(x => x.X)
            .GreaterThanOrEqualTo(0).WithMessage("X cannot be negative");
        
        RuleFor(x => x.Y)
            .GreaterThanOrEqualTo(0).WithMessage("Y cannot be negative");
    }
}
```

---

### **Your Query: GetGraphQuery**

```csharp
// Application/Queries/GetGraph/GetGraphQuery.cs
public record GetGraphQuery(Guid GraphId) : IRequest<GraphDto>;
//                                                     👆 Returns DTO, not domain object

// Application/Queries/GetGraph/GraphDto.cs
public class GraphDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Type { get; init; }
    public List<NodeDto> Nodes { get; init; } = new();
    public List<EdgeDto> Edges { get; init; } = new();
}

public class NodeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Type { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
}

// Application/Queries/GetGraph/GetGraphQueryHandler.cs
public class GetGraphQueryHandler 
    : IRequestHandler<GetGraphQuery, GraphDto>
{
    private readonly KGEditorDbContext _context; // Direct database access for queries!
    
    public GetGraphQueryHandler(KGEditorDbContext context)
    {
        _context = context;
    }
    
    public async Task<GraphDto> Handle(
        GetGraphQuery request,
        CancellationToken cancellationToken)
    {
        // Direct EF Core query - optimized for reading
        var graphDto = await _context.Graphs
            .Where(g => g.Id == GraphId.From(request.GraphId))
            .Select(g => new GraphDto
            {
                Id = g.Id.Value,
                Name = g.Name,
                Type = g.Type.Name,
                Nodes = g.Nodes.Select(n => new NodeDto
                {
                    Id = n.Id.Value,
                    Name = n.Name,
                    Type = n.Type.Name,
                    X = n.Position.X,
                    Y = n.Position.Y
                }).ToList(),
                Edges = g.Edges.Select(e => new EdgeDto
                {
                    Id = e.Id.Value,
                    SourceNodeId = e.SourceNodeId.Value,
                    TargetNodeId = e.TargetNodeId.Value
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        return graphDto;
    }
}
```

---

### **CQRS Pattern Summary**

**Commands:**
- Change state
- Load aggregate
- Execute business logic
- Save aggregate
- Return Result<T>
- Can fail

**Queries:**
- Read state
- Direct database query
- Map to DTO
- Return DTO
- Never fail (return null if not found)
- Optimized for performance

**Why Separate?**
1. **Different needs** - writes need business rules, reads need speed
2. **Optimization** - optimize queries differently than commands
3. **Scalability** - can scale reads and writes separately
4. **Security** - different permissions for read vs write

---

**Continue to Chapter 18-19: Result Pattern & Domain Events?** 🚀
