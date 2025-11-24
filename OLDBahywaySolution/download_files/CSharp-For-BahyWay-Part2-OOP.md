# 📘 C# For BahyWay - Part 2: Object-Oriented Programming

## 🎯 What You'll Learn

Using ONLY your real BahyWay classes, you'll understand:
- **Inheritance**: How Entity → AggregateRoot → Graph works
- **Interfaces**: IGraphRepository, IGraphExporter, etc.
- **Abstract Classes**: Why Entity and AggregateRoot exist
- **Polymorphism**: Different behaviors for same interface

---

## Chapter 6: Inheritance (Base Classes)

### **What You Need to Know**

Inheritance = one class extends another, inheriting all its members.

### **Your Real Inheritance Hierarchy**

```
                    Object (C# built-in)
                       ↓
               Entity<TId> (SharedKernel)
                       ↓
           AggregateRoot<TId> (SharedKernel)
                       ↓
            ┌──────────┴──────────┐
            ↓                     ↓
      Graph (Domain)        Animation (Domain)
```

Let's build this step by step with YOUR actual code:

---

### **Level 1: Entity<TId> (Base for ALL entities)**

```csharp
// SharedKernel/Entity.cs
// Purpose: Every domain object that has an identity

namespace BahyWay.SharedKernel;

public abstract class Entity<TId> where TId : EntityId
{
    // Every entity HAS an ID
    public TId Id { get; protected set; }
    
    // Protected constructor - only subclasses can create
    protected Entity(TId id)
    {
        Id = id;
    }
    
    // Equality based on ID (two entities are same if IDs match)
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;
        
        return Id.Equals(other.Id);
    }
    
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    
    // Operators
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }
    
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
```

**What This Gives You:**
1. **All entities have an ID** - Graph has GraphId, Node has NodeId
2. **Equality by ID** - Two graphs are same if IDs match (not by name!)
3. **Type safety** - Can't accidentally compare GraphId with NodeId

**How You Use It:**
```csharp
// Node inherits from Entity<NodeId>
public class Node : Entity<NodeId>
{
    // Node automatically gets:
    // - public NodeId Id { get; }
    // - Equality comparisons
    // - ==, != operators
    
    public Node(NodeId id, string name) : base(id)
    {
        // Must call base(id) to set the Id
        Name = name;
    }
}

// Using it:
var node1 = new Node(NodeId.Create(), "Node1");
var node2 = new Node(NodeId.Create(), "Node2");
var node3 = node1; // Same reference

Console.WriteLine(node1 == node2); // False (different IDs)
Console.WriteLine(node1 == node3); // True (same ID)
```

---

### **Level 2: AggregateRoot<TId> (Entities that are roots)**

```csharp
// SharedKernel/AggregateRoot.cs
// Purpose: Entities that are roots of aggregates (have domain events)

namespace BahyWay.SharedKernel;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : EntityId
{
    // Collection of domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    
    // Expose as read-only
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected AggregateRoot(TId id) : base(id)
    {
        // Calls Entity constructor
    }
    
    // Protected method - only subclasses can add events
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    // Clear events after they're published
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

**What This Adds:**
1. **All aggregate roots can raise events**
2. **Inherit everything from Entity** (Id, equality, etc.)
3. **Protected AddDomainEvent** - only the aggregate can raise events

**How You Use It:**
```csharp
// Graph inherits from AggregateRoot<GraphId>
public class Graph : AggregateRoot<GraphId>
{
    // Graph automatically gets:
    // - public GraphId Id { get; } (from Entity)
    // - Equality by ID (from Entity)
    // - Domain events (from AggregateRoot)
    // - AddDomainEvent() (from AggregateRoot)
    
    private Graph(GraphId id, string name) : base(id)
    {
        // Calls AggregateRoot → Entity → sets Id
        Name = name;
    }
    
    public static Graph Create(string name, GraphType type)
    {
        var graph = new Graph(GraphId.Create(), name);
        
        // Can call AddDomainEvent because we inherit from AggregateRoot!
        graph.AddDomainEvent(new GraphCreatedEvent(graph.Id, name));
        
        return graph;
    }
    
    public Result<Node> AddNode(string name, NodeType type, Position position)
    {
        var node = Node.Create(name, type, position);
        _nodes.Add(node);
        
        // Raise event
        AddDomainEvent(new NodeAddedEvent(Id, node.Id));
        
        return Result.Success(node);
    }
}
```

---

### **Level 3: Your Domain Classes (Graph, Animation, etc.)**

```csharp
// Domain/Aggregates/Graph/Graph.cs
public class Graph : AggregateRoot<GraphId>
{
    // YOUR specific properties
    private readonly List<Node> _nodes = new();
    private readonly List<Edge> _edges = new();
    
    public string Name { get; private set; }
    public GraphType Type { get; private set; }
    public IReadOnlyList<Node> Nodes => _nodes.AsReadOnly();
    public IReadOnlyList<Edge> Edges => _edges.AsReadOnly();
    
    // YOUR specific methods
    public Result<Node> AddNode(/*...*/) { }
    public Result<Edge> CreateEdge(/*...*/) { }
}

// Domain/Aggregates/Animation/Animation.cs
public class Animation : AggregateRoot<AnimationId>
{
    // YOUR specific properties
    private readonly List<Scene> _scenes = new();
    private readonly Timeline _timeline;
    
    public string Name { get; private set; }
    public Duration TotalDuration { get; private set; }
    
    // YOUR specific methods
    public Result<Scene> AddScene(/*...*/) { }
}
```

**The Inheritance Chain Means:**

```csharp
Graph graph = Graph.Create("My Graph", GraphType.Process);

// From Graph class:
graph.Name                    // ✅ Your specific property
graph.AddNode(/*...*/)       // ✅ Your specific method

// From AggregateRoot:
graph.DomainEvents           // ✅ Inherited from AggregateRoot
graph.AddDomainEvent(...)    // ✅ Protected, can call internally

// From Entity:
graph.Id                     // ✅ Inherited from Entity
graph.Equals(other)          // ✅ Inherited equality
graph == otherGraph          // ✅ Inherited operator
```

---

### **Why This Hierarchy?**

| Class | Purpose | What It Provides | Who Uses It |
|-------|---------|------------------|-------------|
| **Entity<TId>** | All domain objects with identity | Id, Equality | Node, Edge, Scene |
| **AggregateRoot<TId>** | Entities that are roots | Events, Consistency boundary | Graph, Animation |
| **Your Classes** | Specific business logic | Your domain behavior | Graph, Node, Animation |

---

### **Real Decision Tree:**

```
Do I need domain events?
├─ YES → Inherit from AggregateRoot<TId>
│        Examples: Graph, Animation
│        
└─ NO → Does it have an identity?
         ├─ YES → Inherit from Entity<TId>
         │        Examples: Node, Edge, Scene
         │
         └─ NO → Is it a value?
                  └─ Use ValueObject
                     Examples: Position, Color, Duration
```

---

## Chapter 7: Interfaces (Contracts)

### **What You Need to Know**

Interface = a contract. "If you implement me, you MUST have these methods."

### **Your Real Interface: IGraphRepository**

```csharp
// Application/Services/IGraphRepository.cs
// Purpose: Contract for how to save/load graphs
// Benefit: Infrastructure can change, application doesn't care

namespace BahyWay.KGEditorWay.Application.Services;

public interface IGraphRepository
{
    // Contract: Must be able to get graph by ID
    Task<Graph?> GetByIdAsync(
        GraphId id, 
        CancellationToken cancellationToken = default);
    
    // Contract: Must be able to get all graphs
    Task<List<Graph>> GetAllAsync(
        CancellationToken cancellationToken = default);
    
    // Contract: Must be able to add new graph
    Task AddAsync(
        Graph graph, 
        CancellationToken cancellationToken = default);
    
    // Contract: Must be able to update graph
    Task UpdateAsync(
        Graph graph, 
        CancellationToken cancellationToken = default);
    
    // Contract: Must be able to delete graph
    Task DeleteAsync(
        GraphId id, 
        CancellationToken cancellationToken = default);
}
```

**Why Interface?**

### **WITHOUT Interface:**
```csharp
// ❌ Application layer directly uses PostgreSQL repository
public class AddNodeCommandHandler
{
    private readonly PostgreSqlGraphRepository _repository;
    
    public async Task<Result> Handle(AddNodeCommand command)
    {
        var graph = await _repository.GetByIdAsync(command.GraphId);
        // Problem: Tightly coupled to PostgreSQL!
        // Can't test without real database
        // Can't switch to different database
    }
}
```

### **WITH Interface:**
```csharp
// ✅ Application layer uses interface
public class AddNodeCommandHandler
{
    private readonly IGraphRepository _repository;
    
    public async Task<Result> Handle(AddNodeCommand command)
    {
        var graph = await _repository.GetByIdAsync(command.GraphId);
        // Benefits:
        // - Don't care if PostgreSQL, MongoDB, or In-Memory
        // - Easy to test with fake repository
        // - Can switch implementation anytime
    }
}
```

---

### **Your Repository Implementations**

**Implementation #1: PostgreSQL + EF Core**
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
    
    // ... implement all interface methods
}
```

**Implementation #2: In-Memory (for testing)**
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
    
    public Task AddAsync(
        Graph graph, 
        CancellationToken cancellationToken = default)
    {
        _graphs.Add(graph);
        return Task.CompletedTask;
    }
    
    // ... implement all interface methods
}
```

**Implementation #3: Apache AGE (graph database)**
```csharp
// Infrastructure/GraphDatabase/AgeGraphRepository.cs
public class AgeGraphRepository : IGraphRepository
{
    private readonly IAgeClient _ageClient;
    
    public async Task<Graph?> GetByIdAsync(
        GraphId id, 
        CancellationToken cancellationToken = default)
    {
        // Use Cypher query
        var cypher = @"
            MATCH (g:Graph {id: $graphId})
            OPTIONAL MATCH (g)-[:HAS_NODE]->(n:Node)
            OPTIONAL MATCH (g)-[:HAS_EDGE]->(e:Edge)
            RETURN g, collect(n) as nodes, collect(e) as edges";
        
        var result = await _ageClient.ExecuteAsync(cypher, new { graphId = id.Value });
        return MapToGraph(result);
    }
    
    // ... implement all interface methods
}
```

**Using Them (Dependency Injection):**
```csharp
// Startup configuration
services.AddScoped<IGraphRepository, GraphRepository>(); // Use PostgreSQL
// Or:
services.AddScoped<IGraphRepository, InMemoryGraphRepository>(); // Use In-Memory
// Or:
services.AddScoped<IGraphRepository, AgeGraphRepository>(); // Use Apache AGE

// Your handlers DON'T CHANGE!
// They just use IGraphRepository
```

---

### **Your Other Interfaces**

**IGraphExporter**
```csharp
// Purpose: Contract for exporting graphs to different formats

public interface IGraphExporter
{
    Task<string> ExportToJsonAsync(Graph graph);
    Task<byte[]> ExportToPngAsync(Graph graph);
    Task<string> ExportToGraphMLAsync(Graph graph);
}

// Implementations:
// - JsonGraphExporter (System.Text.Json)
// - PngGraphExporter (SkiaSharp)
// - GraphMLExporter (XDocument)
```

**IGraphLayout**
```csharp
// Purpose: Contract for different layout algorithms

public interface IGraphLayout
{
    Task ApplyLayoutAsync(Graph graph);
    string Name { get; }
}

// Implementations:
// - ForceDirectedLayout
// - HierarchicalLayout
// - GridLayout
// - CircularLayout
```

**IAnimationRenderer**
```csharp
// Purpose: Contract for rendering animation frames

public interface IAnimationRenderer
{
    Task<SKBitmap> RenderFrameAsync(
        Animation animation, 
        Duration currentTime);
    
    Task<List<SKBitmap>> RenderAllFramesAsync(
        Animation animation);
}

// Implementations:
// - SkiaSharpRenderer (for GIF/PNG export)
// - UnityRenderer (for 3D simulation)
```

---

### **When to Use Interfaces in BahyWay**

| Scenario | Interface | Implementations |
|----------|-----------|-----------------|
| **Data Access** | IGraphRepository | PostgreSQL, InMemory, AGE |
| **Export** | IGraphExporter | JSON, PNG, GraphML |
| **Layout** | IGraphLayout | ForceDirected, Hierarchical, Grid |
| **Rendering** | IAnimationRenderer | SkiaSharp, Unity |
| **Validation** | IValidator<T> | Each command validator |

---

## Chapter 8: Abstract Classes (Partial Implementations)

### **What You Need to Know**

Abstract class = like an interface BUT can have implementation. You can't create it directly, must inherit.

### **When to Use Abstract vs Interface**

| Use Abstract Class | Use Interface |
|-------------------|---------------|
| Share common CODE | Just define CONTRACT |
| Has state (fields) | No state |
| Single inheritance | Multiple implementation |
| Examples: Entity, AggregateRoot | Examples: IRepository, IExporter |

### **Your Real Abstract Class: Entity<TId>**

```csharp
// SharedKernel/Entity.cs
public abstract class Entity<TId> where TId : EntityId
{
    // CONCRETE implementation (all entities get this)
    public TId Id { get; protected set; }
    
    protected Entity(TId id)
    {
        Id = id;
    }
    
    // CONCRETE implementation (all entities get this)
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;
        
        return Id.Equals(other.Id);
    }
    
    // All entities get same implementation
    public override int GetHashCode() => Id.GetHashCode();
    
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }
    
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
```

**Why Abstract?**
1. **Can't create Entity directly** - `new Entity()` won't compile
2. **Must inherit** - `class Node : Entity<NodeId>`
3. **Shares implementation** - All entities get same equality logic
4. **Enforces structure** - All entities MUST have an Id

**Compare with Interface:**
```csharp
// ❌ If Entity was an interface
public interface IEntity<TId>
{
    TId Id { get; }
    bool Equals(object? obj);
    int GetHashCode();
}

// Problem: Every entity must implement equality AGAIN!
public class Node : IEntity<NodeId>
{
    public NodeId Id { get; set; }
    
    // Must reimplement equality (code duplication!)
    public override bool Equals(object? obj)
    {
        if (obj is not Node other) return false;
        return Id.Equals(other.Id);
    }
    // ... same code for every entity!
}

// ✅ With abstract class, write once, inherit everywhere!
```

---

### **Your Abstract Class: ValueObject**

```csharp
// SharedKernel/ValueObject.cs
// Purpose: Base class for all value objects

public abstract class ValueObject
{
    // ABSTRACT method - subclasses MUST implement
    protected abstract IEnumerable<object> GetEqualityComponents();
    
    // CONCRETE implementation - all value objects get this
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;
        
        var other = (ValueObject)obj;
        
        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }
    
    // CONCRETE implementation
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
    
    // CONCRETE implementation
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }
    
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}
```

**How You Implement It:**
```csharp
// Position is a VALUE OBJECT
public class Position : ValueObject
{
    public double X { get; }
    public double Y { get; }
    
    public Position(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    // MUST implement this abstract method
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }
    
    // That's it! Equals, GetHashCode, ==, != all work automatically!
}

// Using it:
var pos1 = new Position(100, 200);
var pos2 = new Position(100, 200);
var pos3 = new Position(150, 250);

Console.WriteLine(pos1 == pos2); // True (same values)
Console.WriteLine(pos1 == pos3); // False (different values)
```

**Another Example: Color**
```csharp
public class Color : ValueObject
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }
    
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    // Just list what makes colors equal
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return R;
        yield return G;
        yield return B;
        yield return A;
    }
    
    // Static factory methods
    public static Color Red => new(255, 0, 0);
    public static Color Green => new(0, 255, 0);
    public static Color Blue => new(0, 0, 255);
}
```

---

### **Abstract vs Concrete Methods**

```csharp
public abstract class MyBaseClass
{
    // CONCRETE method - has implementation
    public void ConcreteMethod()
    {
        Console.WriteLine("Base implementation");
    }
    
    // VIRTUAL method - has implementation, can be overridden
    public virtual void VirtualMethod()
    {
        Console.WriteLine("Default behavior");
    }
    
    // ABSTRACT method - NO implementation, MUST override
    public abstract void AbstractMethod();
}

public class MyDerivedClass : MyBaseClass
{
    // Don't need to implement ConcreteMethod (inherited)
    
    // CAN override VirtualMethod (optional)
    public override void VirtualMethod()
    {
        Console.WriteLine("Custom behavior");
        base.VirtualMethod(); // Can call base if needed
    }
    
    // MUST implement AbstractMethod (required)
    public override void AbstractMethod()
    {
        Console.WriteLine("Required implementation");
    }
}
```

**In Your Code:**
```csharp
// Entity is abstract
public abstract class Entity<TId>
{
    // Concrete - all entities get this
    public TId Id { get; protected set; }
    
    // Concrete - all entities get this
    public override bool Equals(object? obj) { /* implementation */ }
}

// AggregateRoot is abstract
public abstract class AggregateRoot<TId> : Entity<TId>
{
    // Concrete - all aggregates get this
    protected void AddDomainEvent(IDomainEvent evt) { /* implementation */ }
}

// Graph is CONCRETE (not abstract)
public class Graph : AggregateRoot<GraphId>
{
    // Can create instances: var graph = Graph.Create(...);
}
```

---

## Chapter 9: Polymorphism (Different Behaviors)

### **What You Need to Know**

Polymorphism = "many forms". Same interface, different implementations.

### **Your Real Example: Graph Exporters**

```csharp
// Different exporters, same interface
List<IGraphExporter> exporters = new()
{
    new JsonGraphExporter(),
    new PngGraphExporter(),
    new GraphMLExporter(),
    new DotGraphExporter()
};

// Polymorphism: call same method, different behavior!
foreach (var exporter in exporters)
{
    await exporter.ExportAsync(graph, $"graph.{exporter.Format}");
    // Each exporter does it differently!
    // - JsonGraphExporter → serializes to JSON
    // - PngGraphExporter → renders to image
    // - GraphMLExporter → converts to XML
    // - DotGraphExporter → generates DOT format
}
```

### **Your Real Example: Layout Algorithms**

```csharp
// User picks layout from menu
var layout = selectedLayout switch
{
    LayoutType.ForceDirected => new ForceDirectedLayout(),
    LayoutType.Hierarchical => new HierarchicalLayout(),
    LayoutType.Grid => new GridLayout(),
    LayoutType.Circular => new CircularLayout(),
    _ => throw new ArgumentException()
};

// Polymorphism: same call, different algorithm!
await layout.ApplyLayoutAsync(graph);
// - ForceDirectedLayout → physics simulation
// - HierarchicalLayout → tree structure
// - GridLayout → uniform grid
// - CircularLayout → nodes in circle
```

### **Your Real Example: Node Types**

```csharp
// Different node types behave differently
public abstract class NodeBehavior
{
    public abstract Color GetColor();
    public abstract string GetIcon();
    public abstract bool CanHaveChildren();
}

public class SourceNodeBehavior : NodeBehavior
{
    public override Color GetColor() => Color.Green;
    public override string GetIcon() => "📄";
    public override bool CanHaveChildren() => false;
}

public class TransformNodeBehavior : NodeBehavior
{
    public override Color GetColor() => Color.Blue;
    public override string GetIcon() => "⚙️";
    public override bool CanHaveChildren() => true;
}

public class SinkNodeBehavior : NodeBehavior
{
    public override Color GetColor() => Color.Red;
    public override string GetIcon() => "📁";
    public override bool CanHaveChildren() => false;
}

// Using polymorphism:
NodeBehavior behavior = node.Type switch
{
    NodeType.Source => new SourceNodeBehavior(),
    NodeType.Transform => new TransformNodeBehavior(),
    NodeType.Sink => new SinkNodeBehavior(),
    _ => throw new ArgumentException()
};

// Same calls, different behavior based on type!
var color = behavior.GetColor();
var icon = behavior.GetIcon();
var canHaveChildren = behavior.CanHaveChildren();
```

---

### **Real Benefits in Your Code:**

**1. Easy to Add New Implementations**
```csharp
// Want to add Excel export? Just implement interface!
public class ExcelGraphExporter : IGraphExporter
{
    public async Task<string> ExportAsync(Graph graph, string filePath)
    {
        // Excel-specific logic
    }
}

// No need to change ANY existing code!
// Just register: services.AddScoped<IGraphExporter, ExcelGraphExporter>();
```

**2. Easy to Test**
```csharp
// Real tests use in-memory repository
public class AddNodeCommandHandlerTests
{
    [Fact]
    public async Task AddNode_WithValidData_ShouldSucceed()
    {
        // Arrange
        IGraphRepository repository = new InMemoryGraphRepository(); // Polymorphism!
        var handler = new AddNodeCommandHandler(repository);
        
        // Act & Assert
        // Test runs fast, no database needed!
    }
}
```

**3. Flexible Configuration**
```csharp
// Development: use in-memory
if (isDevelopment)
{
    services.AddScoped<IGraphRepository, InMemoryGraphRepository>();
}
// Production: use PostgreSQL
else if (isProduction)
{
    services.AddScoped<IGraphRepository, GraphRepository>();
}
// Analytics: use Apache AGE
else if (needsGraphQueries)
{
    services.AddScoped<IGraphRepository, AgeGraphRepository>();
}

// Application code doesn't care which one!
// It just uses IGraphRepository
```

---

## 🎯 Summary: OOP in BahyWay

### **Your Class Hierarchy:**
```
ValueObject          Entity<TId>          Interfaces
    ↓                   ↓                      ↓
Position          AggregateRoot<TId>    IGraphRepository
Color                  ↓                 IGraphExporter
Duration           Graph                IGraphLayout
Metadata          Animation             IAnimationRenderer
                     Node
                     Edge
```

### **When to Use Each:**

| Pattern | Use When | Example |
|---------|----------|---------|
| **Inherit from Entity** | Has identity, part of aggregate | Node, Edge, Scene |
| **Inherit from AggregateRoot** | Root of consistency boundary | Graph, Animation |
| **Inherit from ValueObject** | Identified by values, immutable | Position, Color, Duration |
| **Implement Interface** | Multiple implementations needed | IGraphRepository, IExporter |
| **Abstract Class** | Share code between similar classes | Entity, ValueObject |
| **Sealed Class** | No one should inherit | GraphId, NodeId |

---

**Continue to Part 3: Advanced C# & Collections?**

Next covers:
- Generics (why Entity<TId> and not just Entity)
- Collections (List<Node>, Dictionary<NodeId, Node>)
- LINQ (querying your nodes and edges)
- Async/Await (why all your database methods are async)

Ready for Part 3?
