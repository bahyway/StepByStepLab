# 📘 C# For BahyWay - Complete Learning Manual

## 🎯 Introduction

**Welcome to "C# For BahyWay"** - Your personalized C# manual using ONLY examples from your actual projects!

**What makes this different:**
- ❌ NO generic "Person" or "Car" examples
- ❌ NO irrelevant tutorials
- ✅ ONLY classes you'll actually create
- ✅ ONLY patterns you'll actually use
- ✅ Real code from KGEditorWay, SimulateWay, etc.

**Learning Path:**
1. Start with basics (what is a class?)
2. Build to your actual domain objects (Graph, Node, Edge)
3. Learn patterns through real use cases
4. Understand alternatives and trade-offs

---

## 📚 Table of Contents

### **Part 1: C# Fundamentals Through BahyWay**
- Chapter 1: Classes & Objects (Your Domain Entities)
- Chapter 2: Properties & Fields (Your Data)
- Chapter 3: Methods (Your Behavior)
- Chapter 4: Constructors (How Objects Are Created)
- Chapter 5: Access Modifiers (Who Can See What)

### **Part 2: Object-Oriented Programming**
- Chapter 6: Inheritance (Base Classes)
- Chapter 7: Interfaces (Contracts)
- Chapter 8: Abstract Classes (Partial Implementations)
- Chapter 9: Polymorphism (Different Implementations)

### **Part 3: Advanced C# For BahyWay**
- Chapter 10: Generics (Reusable Code)
- Chapter 11: Collections (Lists, Dictionaries)
- Chapter 12: LINQ (Querying Data)
- Chapter 13: Async/Await (Non-blocking Operations)

### **Part 4: Design Patterns You'll Use**
- Chapter 14: Value Objects
- Chapter 15: Entities & Aggregates
- Chapter 16: Repository Pattern
- Chapter 17: CQRS (Commands & Queries)
- Chapter 18: Result Pattern
- Chapter 19: Domain Events

---

## 📖 Part 1: C# Fundamentals Through BahyWay

---

## Chapter 1: Classes & Objects

### **What You Need to Know**

In your BahyWay platform, everything is an object. A **class** is a blueprint, an **object** is an instance.

### **Your First Real Class: GraphId**

```csharp
// This is the SIMPLEST class you'll create
// Purpose: Represent a unique identifier for a Graph

namespace BahyWay.KGEditorWay.Domain.Aggregates.Graph;

public class GraphId
{
    // This is a FIELD - stores data privately
    private readonly Guid _value;
    
    // This is a PROPERTY - exposes data publicly
    public Guid Value => _value;
    
    // This is a CONSTRUCTOR - creates the object
    private GraphId(Guid value)
    {
        _value = value;
    }
    
    // These are STATIC METHODS - factory methods
    public static GraphId Create() => new GraphId(Guid.NewGuid());
    public static GraphId From(Guid value) => new GraphId(value);
}
```

**Why This Design?**
1. **Private constructor** - You control HOW objects are created
2. **Static factory methods** - Clear intent: `GraphId.Create()` vs `new GraphId(???)`
3. **Readonly field** - Once created, ID never changes
4. **Value property** - Others can read it, but not change it

**When You Use It:**
```csharp
// Creating new graph
var graph = Graph.Create("My Workflow", GraphType.Process);
Console.WriteLine(graph.Id.Value); // Access the GUID inside

// Loading existing graph
var existingId = GraphId.From(someGuid);
var graph = await repository.GetByIdAsync(existingId);
```

**Alternatives:**
```csharp
// ❌ BAD: Just use Guid directly
public Guid Id { get; set; } // Too generic, easy to mix up IDs

// ❌ BAD: Public constructor
public GraphId(Guid value) { } // Anyone can create invalid IDs

// ✅ GOOD: Your current design!
public class GraphId { /* as above */ }
```

---

### **Your Second Real Class: Position**

```csharp
// Purpose: Represent X,Y coordinates of a Node on canvas

namespace BahyWay.KGEditorWay.Domain.ValueObjects;

public class Position
{
    public double X { get; private set; }
    public double Y { get; private set; }
    
    private Position(double x, double y)
    {
        X = x;
        Y = y;
    }
    
    public static Position Create(double x, double y)
    {
        // Validation happens here!
        if (x < 0) throw new ArgumentException("X cannot be negative");
        if (y < 0) throw new ArgumentException("Y cannot be negative");
        
        return new Position(x, y);
    }
    
    // Business logic: Calculate distance
    public double DistanceTo(Position other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
```

**Why This Design?**
1. **Encapsulation** - X and Y are together (they're always paired)
2. **Validation** - Can't create invalid positions
3. **Business Logic** - Position knows how to calculate distance
4. **Immutability** - Private setters mean position doesn't change randomly

**When You Use It:**
```csharp
// Creating a node at specific position
var position = Position.Create(100, 200);
var node = graph.AddNode("Start", NodeType.Source, position);

// Calculating distance between nodes
var distance = node1.Position.DistanceTo(node2.Position);
if (distance < 50)
{
    Console.WriteLine("Nodes are too close!");
}
```

**Alternatives:**
```csharp
// ❌ BAD: Just two separate properties
public double X { get; set; }
public double Y { get; set; }
// Problem: X and Y are separated, no validation, no business logic

// ❌ BAD: Using built-in Point
public Point Position { get; set; } // From System.Drawing
// Problem: Too UI-specific, brings unnecessary dependencies

// ✅ GOOD: Your domain-specific Position class
```

---

## Chapter 2: Properties & Fields

### **What You Need to Know**

**Fields** = private storage  
**Properties** = public access with optional logic

### **Real Example: Node Class**

```csharp
namespace BahyWay.KGEditorWay.Domain.Aggregates.Node;

public class Node
{
    // FIELDS (private, hidden)
    private Position _position;
    private string _name;
    
    // PROPERTIES (public, exposed)
    
    // Read-only property - others can read, but not set
    public NodeId Id { get; private set; }
    
    // Full property with backing field - you control changes
    public string Name 
    { 
        get => _name;
        private set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Name cannot be empty");
            _name = value;
        }
    }
    
    // Auto-property with private setter - C# creates field automatically
    public NodeType Type { get; private set; }
    
    // Expression-bodied property - calculated on the fly
    public string DisplayName => $"{Type.Icon} {Name}";
    
    // Property that exposes field
    public Position Position => _position;
}
```

**When You Use Each:**

```csharp
// 1. Read-only property (Id)
var id = node.Id;           // ✅ Can read
node.Id = newId;            // ❌ Cannot set (private)

// 2. Property with validation (Name)
node.UpdateName("New Name"); // ✅ Goes through validation
// Direct set is private

// 3. Auto-property (Type)
// C# automatically creates a hidden field _type
public NodeType Type { get; private set; }

// 4. Calculated property (DisplayName)
var display = node.DisplayName; // Returns "🔷 My Node" (calculated each time)
```

**Why Different Types?**

| Type | Use When | Example |
|------|----------|---------|
| **Readonly Property** | Value set once in constructor | `Id`, `CreatedAt` |
| **Property with Field** | Need validation or logic | `Name`, `Email` |
| **Auto-Property** | Simple storage, no logic | `Type`, `Color` |
| **Calculated Property** | Computed from other values | `DisplayName`, `IsValid` |

---

## Chapter 3: Methods

### **What You Need to Know**

Methods = actions your object can perform.

### **Real Example: Graph Class Methods**

```csharp
public class Graph
{
    private readonly List<Node> _nodes = new();
    private readonly List<Edge> _edges = new();
    
    // METHOD 1: Command method (changes state, returns result)
    public Result<Node> AddNode(string name, NodeType type, Position position)
    {
        // 1. Validate
        if (string.IsNullOrEmpty(name))
            return Result.Failure<Node>(new Error("Graph.InvalidName", "Name is required"));
        
        // 2. Check business rules
        if (_nodes.Any(n => n.Name == name))
            return Result.Failure<Node>(new Error("Graph.DuplicateName", "Node name already exists"));
        
        // 3. Create and add
        var node = Node.Create(name, type, position);
        _nodes.Add(node);
        
        // 4. Raise event
        AddDomainEvent(new NodeAddedEvent(Id, node.Id));
        
        // 5. Return success
        return Result.Success(node);
    }
    
    // METHOD 2: Query method (reads state, doesn't change it)
    public Node? FindNodeById(NodeId id)
    {
        return _nodes.FirstOrDefault(n => n.Id == id);
    }
    
    // METHOD 3: Query method with filtering
    public List<Node> FindNodesByType(NodeType type)
    {
        return _nodes.Where(n => n.Type == type).ToList();
    }
    
    // METHOD 4: Business logic method
    public bool CanConnect(NodeId sourceId, NodeId targetId)
    {
        var source = FindNodeById(sourceId);
        var target = FindNodeById(targetId);
        
        if (source == null || target == null)
            return false;
        
        // Business rule: Can't connect node to itself
        if (sourceId == targetId)
            return false;
        
        // Business rule: Connection already exists?
        if (_edges.Any(e => e.SourceNodeId == sourceId && e.TargetNodeId == targetId))
            return false;
        
        return true;
    }
    
    // METHOD 5: Async method (for operations that take time)
    public async Task<Result> SaveToFileAsync(string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(this);
            await File.WriteAllTextAsync(filePath, json);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("Graph.SaveFailed", ex.Message));
        }
    }
}
```

**Method Types You'll Use:**

### **1. Command Methods (Change State)**
```csharp
// Purpose: Modify the object
// Pattern: Return Result<T> to indicate success/failure
public Result<Node> AddNode(string name, NodeType type, Position position)
{
    // Validation + Business Rules + Action + Event
}

// When you use it:
var result = graph.AddNode("Start", NodeType.Source, position);
if (result.IsSuccess)
{
    var node = result.Value;
    Console.WriteLine($"Added node: {node.Name}");
}
else
{
    Console.WriteLine($"Failed: {result.Error.Description}");
}
```

### **2. Query Methods (Read State)**
```csharp
// Purpose: Get information without changing anything
// Pattern: Return the data directly or null if not found
public Node? FindNodeById(NodeId id)
{
    return _nodes.FirstOrDefault(n => n.Id == id);
}

// When you use it:
var node = graph.FindNodeById(nodeId);
if (node != null)
{
    Console.WriteLine($"Found: {node.Name}");
}
```

### **3. Business Logic Methods**
```csharp
// Purpose: Encapsulate business rules
// Pattern: Return bool or Result
public bool CanConnect(NodeId sourceId, NodeId targetId)
{
    // Check all business rules
    return true/false;
}

// When you use it:
if (graph.CanConnect(source.Id, target.Id))
{
    graph.CreateEdge(source.Id, null, target.Id, null, EdgeType.DataFlow);
}
```

### **4. Async Methods (Long Operations)**
```csharp
// Purpose: Operations that take time (File I/O, Network, Database)
// Pattern: async Task<Result> or async Task<Result<T>>
public async Task<Result> SaveToFileAsync(string filePath)
{
    await File.WriteAllTextAsync(filePath, json);
    return Result.Success();
}

// When you use it:
await graph.SaveToFileAsync("graph.json");
```

**Why Different Return Types?**

| Return Type | Use When | Example |
|-------------|----------|---------|
| **void** | Just do it, don't need result | `UpdatePosition(x, y)` |
| **Result** | Might fail, no value returned | `Delete()`, `Save()` |
| **Result<T>** | Might fail, returns value | `AddNode()`, `Create()` |
| **T** | Always succeeds, returns value | Simple getters |
| **T?** | Might not find | `FindNodeById()` |
| **Task** | Async, no result | `async void` (rare) |
| **Task<Result>** | Async, might fail | `SaveAsync()` |
| **Task<Result<T>>** | Async, might fail, returns value | `LoadAsync()` |

---

## Chapter 4: Constructors

### **What You Need to Know**

Constructors create and initialize objects. You'll use PRIVATE constructors + FACTORY methods.

### **Real Example: Graph Constructor**

```csharp
public class Graph : AggregateRoot<GraphId>
{
    // PRIVATE CONSTRUCTOR #1: For EF Core (database loading)
    private Graph() { }
    
    // PRIVATE CONSTRUCTOR #2: Real constructor (only you can call)
    private Graph(GraphId id, string name, GraphType type) : base(id)
    {
        // Validation
        Name = Guard.Against.NullOrEmpty(name, nameof(name));
        Type = Guard.Against.Null(type, nameof(type));
        
        // Initialize collections
        _nodes = new List<Node>();
        _edges = new List<Edge>();
        
        // Set metadata
        CreatedAt = DateTime.UtcNow;
    }
    
    // PUBLIC FACTORY METHOD: How others create graphs
    public static Graph Create(string name, GraphType type)
    {
        // Create with generated ID
        var graph = new Graph(GraphId.Create(), name, type);
        
        // Raise domain event
        graph.AddDomainEvent(new GraphCreatedEvent(graph.Id, name));
        
        return graph;
    }
    
    // PUBLIC FACTORY METHOD: Reconstitute from database
    public static Graph Load(GraphId id, string name, GraphType type, 
        List<Node> nodes, List<Edge> edges)
    {
        var graph = new Graph(id, name, type);
        graph._nodes.AddRange(nodes);
        graph._edges.AddRange(edges);
        return graph;
    }
}
```

**Why This Pattern?**

### **❌ What You DON'T Do:**
```csharp
// Bad: Public constructor
public Graph(string name, GraphType type)
{
    // Problems:
    // 1. Can't validate properly
    // 2. Can't raise domain events
    // 3. Can't control ID generation
    // 4. Everyone creates graphs differently
}

// Usage (confusing):
var graph = new Graph("My Graph", GraphType.Process);
// How do I know the ID? When was it created? Was it validated?
```

### **✅ What You DO:**
```csharp
// Good: Private constructor + Factory methods
private Graph(...) { /* controlled initialization */ }
public static Graph Create(string name, GraphType type) { /* clear intent */ }

// Usage (clear):
var graph = Graph.Create("My Graph", GraphType.Process);
// Clear intent: creating NEW graph
// ID automatically generated
// Events raised
// Everything validated
```

**When You Use Each Constructor Type:**

```csharp
public class MyClass
{
    // 1. Parameterless private constructor - for EF Core
    private MyClass() { }
    
    // 2. Full private constructor - real initialization
    private MyClass(MyId id, string name)
    {
        Id = id;
        Name = Guard.Against.NullOrEmpty(name, nameof(name));
    }
    
    // 3. Static factory for NEW entities
    public static MyClass Create(string name)
    {
        var obj = new MyClass(MyId.Create(), name);
        obj.AddDomainEvent(new MyClassCreatedEvent(obj.Id));
        return obj;
    }
    
    // 4. Static factory for EXISTING entities (from database)
    public static MyClass Load(MyId id, string name)
    {
        return new MyClass(id, name);
        // No events - already exists!
    }
}
```

---

## Chapter 5: Access Modifiers

### **What You Need to Know**

Access modifiers control WHO can see and use your code.

### **Your Access Modifier Strategy:**

```csharp
public class Graph : AggregateRoot<GraphId>
{
    // PRIVATE FIELDS - only this class sees them
    private readonly List<Node> _nodes = new();
    private readonly List<Edge> _edges = new();
    private string _name;
    
    // PUBLIC PROPERTIES (read-only) - everyone can read
    public GraphId Id { get; private set; }
    public string Name => _name;
    public GraphType Type { get; private set; }
    
    // PUBLIC PROPERTIES (read-only collections) - safe to expose
    public IReadOnlyList<Node> Nodes => _nodes.AsReadOnly();
    public IReadOnlyList<Edge> Edges => _edges.AsReadOnly();
    
    // PRIVATE CONSTRUCTOR - only this class creates instances
    private Graph(GraphId id, string name, GraphType type) : base(id)
    {
        // ...
    }
    
    // PUBLIC FACTORY - external code uses this to create
    public static Graph Create(string name, GraphType type)
    {
        return new Graph(GraphId.Create(), name, type);
    }
    
    // PUBLIC METHOD - external code can call this
    public Result<Node> AddNode(string name, NodeType type, Position position)
    {
        return AddNodeInternal(name, type, position);
    }
    
    // PRIVATE METHOD - only used internally
    private Result<Node> AddNodeInternal(string name, NodeType type, Position position)
    {
        // Implementation
    }
    
    // INTERNAL METHOD - only your assembly can call
    internal void ClearNodesForTesting()
    {
        _nodes.Clear();
    }
    
    // PROTECTED METHOD - subclasses can override
    protected virtual void OnNodeAdded(Node node)
    {
        // Hook for subclasses
    }
}
```

**Your Access Modifier Rules:**

| Modifier | Who Sees It | Use For | Example |
|----------|-------------|---------|---------|
| **private** | Only this class | Fields, internal methods | `_nodes`, `ValidateInternal()` |
| **public** | Everyone | API, properties, factory methods | `Create()`, `AddNode()`, `Id` |
| **internal** | Same assembly | Cross-class helpers, testing | `ClearForTesting()` |
| **protected** | Subclasses | Override points | `OnNodeAdded()` |
| **private set** | Property: read by all, set by class only | Most properties | `public string Name { get; private set; }` |

**Real Decision Tree:**

```
Should others modify this directly?
├─ NO → private field + public property/method
│      Example: private List<Node> _nodes
│              public IReadOnlyList<Node> Nodes => _nodes.AsReadOnly();
│
└─ YES → Is it dangerous to expose?
         ├─ YES → public method with validation
         │       Example: public Result<Node> AddNode(...)
         │
         └─ NO → public property
                Example: public string Name { get; }
```

---

**Continue to Part 2: Object-Oriented Programming with your real classes?**

This covers:
- Inheritance (how Entity → AggregateRoot → Graph works)
- Interfaces (IGraphRepository, IGraphExporter)
- Abstract classes (why AggregateRoot is abstract)
- Polymorphism (different NodeTypes behaving differently)

Would you like me to continue with Part 2?
