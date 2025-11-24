# 📘 C# For BahyWay - Part 3: Advanced C# Features

## 🎯 What You'll Learn

Using ONLY your real BahyWay code:
- **Generics**: Why `Entity<TId>` not just `Entity`
- **Collections**: `List<Node>`, `Dictionary<NodeId, Node>`, etc.
- **LINQ**: Querying and filtering your data
- **Async/Await**: Non-blocking database operations

---

## Chapter 10: Generics (Reusable Code)

### **What You Need to Know**

Generics = write code once, use with ANY type. The `<T>` you see everywhere.

### **Why You Use Generics**

**❌ WITHOUT Generics (Old Way):**
```csharp
// Need separate base class for each ID type
public class GraphEntity
{
    public GraphId Id { get; set; }
    public bool Equals(object? obj)
    {
        if (obj is not GraphEntity other) return false;
        return Id.Equals(other.Id);
    }
}

public class NodeEntity
{
    public NodeId Id { get; set; }
    public bool Equals(object? obj)
    {
        if (obj is not NodeEntity other) return false;
        return Id.Equals(other.Id);
    }
}

public class AnimationEntity
{
    public AnimationId Id { get; set; }
    public bool Equals(object? obj)
    {
        if (obj is not AnimationEntity other) return false;
        return Id.Equals(other.Id);
    }
}

// Problem: Copy-paste code everywhere! 😱
```

**✅ WITH Generics (Your Way):**
```csharp
// Write ONCE, use for ALL entity types!
public abstract class Entity<TId> where TId : EntityId
{
    public TId Id { get; protected set; }
    
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        return Id.Equals(other.Id);
    }
}

// Use it:
public class Graph : Entity<GraphId> { }      // Graph with GraphId
public class Node : Entity<NodeId> { }        // Node with NodeId
public class Animation : Entity<AnimationId> { } // Animation with AnimationId

// Same code, different types! 🎉
```

---

### **Your Real Generic Classes**

**1. Entity<TId>**
```csharp
// SharedKernel/Entity.cs
public abstract class Entity<TId> where TId : EntityId
{
    //          👆 Generic type parameter
    //                       👆 Constraint: must inherit from EntityId
    
    public TId Id { get; protected set; }
    
    protected Entity(TId id)
    {
        Id = id;
    }
}

// How you use it:
public class Graph : Entity<GraphId>
//                           👆 Replace TId with GraphId
{
    // Now Id is specifically GraphId, not just any EntityId
}

public class Node : Entity<NodeId>
//                          👆 Replace TId with NodeId
{
    // Now Id is specifically NodeId
}
```

**Benefits:**
```csharp
var graph = Graph.Create("My Graph", GraphType.Process);
var node = Node.Create("My Node", NodeType.Source, position);

GraphId graphId = graph.Id;  // ✅ Strongly typed!
NodeId nodeId = node.Id;     // ✅ Strongly typed!

// Can't mix them up:
graphId = node.Id;  // ❌ Compile error! NodeId ≠ GraphId
```

---

**2. Result<T>**
```csharp
// SharedKernel/Result.cs
// Purpose: Return either success with value OR failure with error

public class Result<T>
{
    //            👆 Generic: can be Result<Node>, Result<Graph>, etc.
    
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }           // 👈 Type depends on T
    public Error Error { get; }
    
    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = Error.None;
    }
    
    private Result(Error error)
    {
        IsSuccess = false;
        Value = default!;
        Error = error;
    }
    
    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
}

// How you use it:
public Result<Node> AddNode(string name, NodeType type, Position position)
{
    if (string.IsNullOrEmpty(name))
    {
        return Result<Node>.Failure(
            new Error("Graph.InvalidName", "Name is required"));
    }
    
    var node = Node.Create(name, type, position);
    _nodes.Add(node);
    
    return Result<Node>.Success(node);
}

// Calling it:
var result = graph.AddNode("Start", NodeType.Source, position);

if (result.IsSuccess)
{
    Node node = result.Value;  // 👈 Strongly typed! It's a Node
    Console.WriteLine($"Added: {node.Name}");
}
else
{
    Console.WriteLine($"Error: {result.Error.Description}");
}
```

---

**3. IRequestHandler<TRequest, TResponse>**
```csharp
// MediatR generic interface
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

// Your command handler:
public class AddNodeCommandHandler 
    : IRequestHandler<AddNodeCommand, Result<Guid>>
    //                👆 Request type    👆 Response type
{
    public async Task<Result<Guid>> Handle(
        AddNodeCommand request,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}

// Another handler:
public class GetGraphQueryHandler 
    : IRequestHandler<GetGraphQuery, GraphDto>
    //                👆 Request type   👆 Response type
{
    public async Task<GraphDto> Handle(
        GetGraphQuery request,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}

// MediatR knows which handler to call based on request type!
```

---

### **Generic Constraints (the `where` keyword)**

```csharp
// Your actual constraints:

// 1. Must inherit from specific class
public abstract class Entity<TId> where TId : EntityId
//                                       👆 TId must be or inherit from EntityId

// 2. Must implement interface
public class Repository<T> where T : IAggregateRoot
//                                 👆 T must implement IAggregateRoot

// 3. Must be a class (reference type)
public class Factory<T> where T : class
//                              👆 T must be a class, not struct

// 4. Must have parameterless constructor
public class Activator<T> where T : new()
//                                👆 T must have empty constructor

// 5. Multiple constraints
public class MyClass<T> where T : Entity<EntityId>, IValidatable, new()
//                              👆 Must be Entity AND implement IValidatable AND have constructor

// Your real example:
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : EntityId  // 👈 TId must inherit from EntityId
{
    // Now we know TId has .Value property!
    public Guid GetIdValue() => Id.Value;
}
```

---

### **When You Create Generic Classes**

**Repository<T>**
```csharp
// Generic repository for ANY aggregate
public class Repository<T> : IRepository<T> where T : AggregateRoot<EntityId>
{
    private readonly DbContext _context;
    
    public async Task<T?> GetByIdAsync(EntityId id)
    {
        return await _context.Set<T>()
            .FirstOrDefaultAsync(e => e.Id == id);
    }
    
    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }
}

// Use for different types:
Repository<Graph> graphRepo = new(context);
Repository<Animation> animationRepo = new(context);

var graph = await graphRepo.GetByIdAsync(graphId);
var animation = await animationRepo.GetByIdAsync(animationId);
```

---

## Chapter 11: Collections (Lists, Dictionaries)

### **What You Need to Know**

Collections = groups of objects. You use them EVERYWHERE in BahyWay.

### **Your Most Common Collections**

**1. List<T> - Ordered collection**
```csharp
// In Graph class:
public class Graph : AggregateRoot<GraphId>
{
    // Private mutable list
    private readonly List<Node> _nodes = new();
    private readonly List<Edge> _edges = new();
    
    // Public read-only view
    public IReadOnlyList<Node> Nodes => _nodes.AsReadOnly();
    public IReadOnlyList<Edge> Edges => _edges.AsReadOnly();
    
    // Add to list
    public Result<Node> AddNode(string name, NodeType type, Position position)
    {
        var node = Node.Create(name, type, position);
        _nodes.Add(node);  // 👈 Add to list
        return Result.Success(node);
    }
    
    // Remove from list
    public Result RemoveNode(NodeId nodeId)
    {
        var node = _nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null)
            return Result.Failure(new Error("Graph.NodeNotFound", "Node not found"));
        
        _nodes.Remove(node);  // 👈 Remove from list
        return Result.Success();
    }
    
    // Query list
    public List<Node> GetNodesByType(NodeType type)
    {
        var result = new List<Node>();
        
        foreach (var node in _nodes)  // 👈 Iterate list
        {
            if (node.Type == type)
            {
                result.Add(node);
            }
        }
        
        return result;
    }
    
    // Check if list contains
    public bool HasNode(NodeId nodeId)
    {
        return _nodes.Any(n => n.Id == nodeId);  // 👈 Check existence
    }
    
    // Get list count
    public int NodeCount => _nodes.Count;  // 👈 Count items
}
```

**Common List<T> Operations:**
```csharp
var nodes = new List<Node>();

// Add
nodes.Add(node);                          // Add one
nodes.AddRange(otherNodes);              // Add multiple
nodes.Insert(0, node);                   // Add at position

// Remove
nodes.Remove(node);                      // Remove specific item
nodes.RemoveAt(0);                       // Remove at index
nodes.RemoveAll(n => n.Type == NodeType.Source);  // Remove matching
nodes.Clear();                           // Remove all

// Query
var count = nodes.Count;                 // How many items
var first = nodes.First();               // First item (throws if empty)
var firstOrNull = nodes.FirstOrDefault(); // First or null
var contains = nodes.Contains(node);     // Check if contains
var index = nodes.IndexOf(node);         // Find position (-1 if not found)

// Iterate
foreach (var node in nodes)
{
    Console.WriteLine(node.Name);
}

// Convert
var array = nodes.ToArray();             // To array
var readOnly = nodes.AsReadOnly();       // To read-only
```

---

**2. Dictionary<TKey, TValue> - Key-value pairs**
```csharp
// Fast lookup by ID
public class GraphCache
{
    private readonly Dictionary<GraphId, Graph> _graphs = new();
    //                         👆 Key    👆 Value
    
    // Add to dictionary
    public void AddGraph(Graph graph)
    {
        _graphs[graph.Id] = graph;  // 👈 Set by key
        // Or: _graphs.Add(graph.Id, graph);
    }
    
    // Get from dictionary
    public Graph? GetGraph(GraphId id)
    {
        if (_graphs.TryGetValue(id, out var graph))  // 👈 Try get by key
            return graph;
        
        return null;
        
        // Or: _graphs.ContainsKey(id) ? _graphs[id] : null;
    }
    
    // Remove from dictionary
    public bool RemoveGraph(GraphId id)
    {
        return _graphs.Remove(id);  // 👈 Remove by key (returns true if found)
    }
    
    // Check if key exists
    public bool HasGraph(GraphId id)
    {
        return _graphs.ContainsKey(id);  // 👈 Check key existence
    }
    
    // Get all values
    public List<Graph> GetAllGraphs()
    {
        return _graphs.Values.ToList();  // 👈 All values
    }
    
    // Get all keys
    public List<GraphId> GetAllIds()
    {
        return _graphs.Keys.ToList();  // 👈 All keys
    }
    
    // Iterate key-value pairs
    public void PrintAll()
    {
        foreach (var kvp in _graphs)  // 👈 KeyValuePair<GraphId, Graph>
        {
            Console.WriteLine($"ID: {kvp.Key}, Name: {kvp.Value.Name}");
        }
    }
}
```

**When to Use Dictionary vs List:**

| Use List<T> | Use Dictionary<TKey, TValue> |
|-------------|------------------------------|
| Order matters | Fast lookup by key needed |
| Iterate in order | Don't care about order |
| Small collections | Large collections |
| Example: `List<Node> _nodes` | Example: `Dictionary<NodeId, Node> _nodesById` |

**Real Example: Node Lookup Optimization**
```csharp
public class Graph : AggregateRoot<GraphId>
{
    // Option 1: Just list (current approach)
    private readonly List<Node> _nodes = new();
    
    public Node? FindNodeById(NodeId id)
    {
        // O(n) - must scan entire list
        return _nodes.FirstOrDefault(n => n.Id == id);
    }
    
    // Option 2: List + Dictionary (faster lookups)
    private readonly List<Node> _nodes = new();
    private readonly Dictionary<NodeId, Node> _nodesById = new();
    
    public Result<Node> AddNode(string name, NodeType type, Position position)
    {
        var node = Node.Create(name, type, position);
        _nodes.Add(node);
        _nodesById[node.Id] = node;  // 👈 Also add to dictionary
        return Result.Success(node);
    }
    
    public Node? FindNodeById(NodeId id)
    {
        // O(1) - instant lookup!
        return _nodesById.TryGetValue(id, out var node) ? node : null;
    }
}
```

---

**3. HashSet<T> - Unique items, fast lookups**
```csharp
// Use when: Need unique items + fast contains check
public class GraphValidator
{
    private readonly HashSet<string> _usedNodeNames = new();
    
    public bool IsNodeNameUnique(string name)
    {
        return !_usedNodeNames.Contains(name);  // 👈 O(1) lookup!
    }
    
    public void RegisterNodeName(string name)
    {
        _usedNodeNames.Add(name);  // 👈 Automatically ensures uniqueness
    }
}

// Example: Check for cycles
public bool HasCycle(Graph graph)
{
    var visited = new HashSet<NodeId>();
    var recursionStack = new HashSet<NodeId>();
    
    foreach (var node in graph.Nodes)
    {
        if (HasCycleUtil(node, visited, recursionStack))
            return true;
    }
    
    return false;
}
```

---

**4. IReadOnlyList<T> / IReadOnlyCollection<T>**
```csharp
// Your pattern: Expose read-only, modify privately
public class Graph
{
    // PRIVATE: Can modify
    private readonly List<Node> _nodes = new();
    
    // PUBLIC: Cannot modify
    public IReadOnlyList<Node> Nodes => _nodes.AsReadOnly();
    //     👆 Interface: only exposes reading methods
}

// Usage:
var graph = Graph.Create("My Graph", GraphType.Process);

// ✅ Can read
var count = graph.Nodes.Count;
var first = graph.Nodes[0];
foreach (var node in graph.Nodes) { }

// ❌ Cannot modify (compile errors)
graph.Nodes.Add(node);        // Error: method doesn't exist
graph.Nodes.Remove(node);     // Error: method doesn't exist
graph.Nodes.Clear();          // Error: method doesn't exist

// ✅ Must use proper method
graph.AddNode("Node", NodeType.Source, position);
```

---

### **Collection Initialization**

```csharp
// Creating and initializing in one go:

// List
var nodeTypes = new List<NodeType>
{
    NodeType.Source,
    NodeType.Transform,
    NodeType.Sink
};

// Dictionary
var colorMap = new Dictionary<NodeType, Color>
{
    { NodeType.Source, Color.Green },
    { NodeType.Transform, Color.Blue },
    { NodeType.Sink, Color.Red }
};

// Or newer syntax:
var colorMap = new Dictionary<NodeType, Color>
{
    [NodeType.Source] = Color.Green,
    [NodeType.Transform] = Color.Blue,
    [NodeType.Sink] = Color.Red
};

// HashSet
var allowedTypes = new HashSet<NodeType>
{
    NodeType.Source,
    NodeType.Transform
};
```

---

## Chapter 12: LINQ (Language Integrated Query)

### **What You Need to Know**

LINQ = powerful way to query collections. You'll use it EVERYWHERE.

### **Your Real LINQ Queries**

**1. Filtering with Where**
```csharp
public class Graph
{
    private readonly List<Node> _nodes = new();
    
    // Get nodes by type
    public List<Node> GetNodesByType(NodeType type)
    {
        // WITHOUT LINQ (old way):
        var result = new List<Node>();
        foreach (var node in _nodes)
        {
            if (node.Type == type)
                result.Add(node);
        }
        return result;
        
        // WITH LINQ (your way):
        return _nodes.Where(n => n.Type == type).ToList();
        //            👆 Lambda expression: for each node n, check if type matches
    }
    
    // Get nodes in a region
    public List<Node> GetNodesInRegion(double minX, double minY, double maxX, double maxY)
    {
        return _nodes
            .Where(n => n.Position.X >= minX && 
                       n.Position.X <= maxX &&
                       n.Position.Y >= minY && 
                       n.Position.Y <= maxY)
            .ToList();
    }
    
    // Get nodes with specific name pattern
    public List<Node> FindNodesByNamePattern(string pattern)
    {
        return _nodes
            .Where(n => n.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
```

**2. Finding Single Items**
```csharp
// First: Get first matching item (throws if none)
var sourceNode = graph.Nodes.First(n => n.Type == NodeType.Source);

// FirstOrDefault: Get first or null if none
var sourceNode = graph.Nodes.FirstOrDefault(n => n.Type == NodeType.Source);

// Single: Get ONLY item (throws if 0 or more than 1)
var rootNode = graph.Nodes.Single(n => n.Name == "Root");

// SingleOrDefault: Get only item or null (throws if more than 1)
var rootNode = graph.Nodes.SingleOrDefault(n => n.Name == "Root");

// Find by ID
public Node? FindNodeById(NodeId id)
{
    return _nodes.FirstOrDefault(n => n.Id == id);
}

// Find by property
public Node? FindNodeByName(string name)
{
    return _nodes.FirstOrDefault(n => n.Name == name);
}
```

**3. Checking Conditions with Any/All**
```csharp
// Any: Check if at least one matches
public bool HasSourceNode()
{
    return _nodes.Any(n => n.Type == NodeType.Source);
}

public bool HasNodeNamed(string name)
{
    return _nodes.Any(n => n.Name == name);
}

// All: Check if all match
public bool AllNodesNamed()
{
    return _nodes.All(n => !string.IsNullOrEmpty(n.Name));
}

public bool AllNodesHavePosition()
{
    return _nodes.All(n => n.Position != null);
}

// Combining Any/All
public bool CanExecute()
{
    return _nodes.Any(n => n.Type == NodeType.Source) &&  // Has source
           _nodes.Any(n => n.Type == NodeType.Sink) &&    // Has sink
           _nodes.All(n => n.IsValid);                    // All valid
}
```

**4. Transforming with Select**
```csharp
// Select: Transform each item

// Get all node names
public List<string> GetAllNodeNames()
{
    return _nodes.Select(n => n.Name).ToList();
    //            👆 For each node n, take its Name
}

// Get all node IDs
public List<Guid> GetAllNodeIds()
{
    return _nodes.Select(n => n.Id.Value).ToList();
}

// Project to DTO
public List<NodeDto> GetNodeDtos()
{
    return _nodes.Select(n => new NodeDto
    {
        Id = n.Id.Value,
        Name = n.Name,
        Type = n.Type.Name,
        X = n.Position.X,
        Y = n.Position.Y
    }).ToList();
}

// Get unique node types
public List<NodeType> GetUsedNodeTypes()
{
    return _nodes
        .Select(n => n.Type)
        .Distinct()  // 👈 Remove duplicates
        .ToList();
}
```

**5. Ordering with OrderBy/ThenBy**
```csharp
// OrderBy: Sort by property
public List<Node> GetNodesSortedByName()
{
    return _nodes
        .OrderBy(n => n.Name)
        .ToList();
}

// OrderByDescending: Sort descending
public List<Node> GetNodesSortedByNameDescending()
{
    return _nodes
        .OrderByDescending(n => n.Name)
        .ToList();
}

// ThenBy: Sort by multiple properties
public List<Node> GetNodesSortedByTypeAndName()
{
    return _nodes
        .OrderBy(n => n.Type.Name)      // First by type
        .ThenBy(n => n.Name)            // Then by name
        .ToList();
}

// Complex sorting
public List<Node> GetNodesSortedByPosition()
{
    return _nodes
        .OrderBy(n => n.Position.Y)     // First by Y (top to bottom)
        .ThenBy(n => n.Position.X)      // Then by X (left to right)
        .ToList();
}
```

**6. Grouping with GroupBy**
```csharp
// GroupBy: Group items by property

// Group nodes by type
public Dictionary<NodeType, List<Node>> GetNodesByType()
{
    return _nodes
        .GroupBy(n => n.Type)           // Group by type
        .ToDictionary(
            g => g.Key,                  // Type is key
            g => g.ToList()             // List of nodes is value
        );
}

// Count nodes by type
public Dictionary<NodeType, int> CountNodesByType()
{
    return _nodes
        .GroupBy(n => n.Type)
        .ToDictionary(
            g => g.Key,
            g => g.Count()
        );
}

// Using groups
var groupedNodes = _nodes.GroupBy(n => n.Type);
foreach (var group in groupedNodes)
{
    Console.WriteLine($"Type: {group.Key}, Count: {group.Count()}");
    foreach (var node in group)
    {
        Console.WriteLine($"  - {node.Name}");
    }
}
```

**7. Aggregating with Count/Sum/Average/Max/Min**
```csharp
// Count
public int NodeCount => _nodes.Count;
public int SourceNodeCount => _nodes.Count(n => n.Type == NodeType.Source);

// Sum (if nodes had numeric property)
public double TotalNodeX => _nodes.Sum(n => n.Position.X);

// Average
public double AverageNodeX => _nodes.Average(n => n.Position.X);
public double AverageNodeY => _nodes.Average(n => n.Position.Y);

// Max/Min
public double MaxX => _nodes.Max(n => n.Position.X);
public double MinX => _nodes.Min(n => n.Position.X);

// Complex: Get bounding box
public (double MinX, double MinY, double MaxX, double MaxY) GetBoundingBox()
{
    if (!_nodes.Any()) return (0, 0, 0, 0);
    
    return (
        _nodes.Min(n => n.Position.X),
        _nodes.Min(n => n.Position.Y),
        _nodes.Max(n => n.Position.X),
        _nodes.Max(n => n.Position.Y)
    );
}
```

**8. Joining Collections**
```csharp
// Get edges with their source and target nodes
public List<EdgeInfo> GetEdgeInfos()
{
    return _edges
        .Select(e => new EdgeInfo
        {
            Edge = e,
            SourceNode = _nodes.First(n => n.Id == e.SourceNodeId),
            TargetNode = _nodes.First(n => n.Id == e.TargetNodeId)
        })
        .ToList();
}

// Find nodes that have no incoming edges
public List<Node> GetRootNodes()
{
    var nodesWithIncomingEdges = _edges
        .Select(e => e.TargetNodeId)
        .ToHashSet();
    
    return _nodes
        .Where(n => !nodesWithIncomingEdges.Contains(n.Id))
        .ToList();
}

// Find nodes that have no outgoing edges
public List<Node> GetLeafNodes()
{
    var nodesWithOutgoingEdges = _edges
        .Select(e => e.SourceNodeId)
        .ToHashSet();
    
    return _nodes
        .Where(n => !nodesWithOutgoingEdges.Contains(n.Id))
        .ToList();
}
```

**9. Chaining Operations**
```csharp
// Combine multiple LINQ operations
public List<string> GetTransformNodeNames()
{
    return _nodes
        .Where(n => n.Type == NodeType.Transform)  // Filter
        .OrderBy(n => n.Name)                      // Sort
        .Select(n => n.Name)                       // Project
        .ToList();                                 // Execute
}

// Complex query
public List<NodeDto> GetVisibleNodesSortedByPosition()
{
    return _nodes
        .Where(n => n.IsVisible)                   // Filter visible
        .Where(n => n.Position.X >= 0)             // Filter in bounds
        .OrderBy(n => n.Position.Y)                // Sort by Y
        .ThenBy(n => n.Position.X)                 // Then by X
        .Take(10)                                  // Take first 10
        .Select(n => new NodeDto                   // Project to DTO
        {
            Id = n.Id.Value,
            Name = n.Name,
            X = n.Position.X,
            Y = n.Position.Y
        })
        .ToList();
}
```

---

### **LINQ Method vs Query Syntax**

```csharp
// Method syntax (your preferred way):
var result = _nodes
    .Where(n => n.Type == NodeType.Source)
    .OrderBy(n => n.Name)
    .Select(n => n.Name)
    .ToList();

// Query syntax (alternative, looks like SQL):
var result = (from n in _nodes
              where n.Type == NodeType.Source
              orderby n.Name
              select n.Name).ToList();

// Both are identical! Use method syntax - it's more flexible.
```

---

## Chapter 13: Async/Await (Non-blocking Operations)

### **What You Need to Know**

Async = don't block the thread while waiting. Essential for database, file I/O, network calls.

### **Why Async in BahyWay**

**❌ WITHOUT Async (Blocking):**
```csharp
// Blocks UI thread for 2 seconds!
public class GraphRepository
{
    public Graph GetById(GraphId id)
    {
        Thread.Sleep(2000);  // Simulate database query
        return graph;
    }
}

// In your UI:
private void LoadButton_Click(object sender, RoutedEventArgs e)
{
    var graph = repository.GetById(graphId);  // UI FREEZES for 2 seconds! 😱
    DisplayGraph(graph);
}
```

**✅ WITH Async (Non-blocking):**
```csharp
// Doesn't block!
public class GraphRepository
{
    public async Task<Graph> GetByIdAsync(GraphId id)
    {
        await Task.Delay(2000);  // Simulate database query (doesn't block!)
        return graph;
    }
}

// In your UI:
private async void LoadButton_Click(object sender, RoutedEventArgs e)
{
    var graph = await repository.GetByIdAsync(graphId);  // UI stays responsive! ✅
    DisplayGraph(graph);
}
```

---

### **Your Real Async Patterns**

**1. Repository Methods (Database)**
```csharp
public interface IGraphRepository
{
    // All database methods are async
    Task<Graph?> GetByIdAsync(GraphId id, CancellationToken cancellationToken = default);
    Task<List<Graph>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Graph graph, CancellationToken cancellationToken = default);
    Task UpdateAsync(Graph graph, CancellationToken cancellationToken = default);
    Task DeleteAsync(GraphId id, CancellationToken cancellationToken = default);
}

// Implementation:
public class GraphRepository : IGraphRepository
{
    private readonly KGEditorDbContext _context;
    
    public async Task<Graph?> GetByIdAsync(
        GraphId id, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Graphs
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        //  👆 await = don't block while database fetches data
    }
    
    public async Task AddAsync(
        Graph graph, 
        CancellationToken cancellationToken = default)
    {
        await _context.Graphs.AddAsync(graph, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

**2. Command Handlers (Application Layer)**
```csharp
public class AddNodeCommandHandler 
    : IRequestHandler<AddNodeCommand, Result<Guid>>
{
    private readonly IGraphRepository _repository;
    
    public async Task<Result<Guid>> Handle(
        AddNodeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load from database (async)
        var graph = await _repository.GetByIdAsync(
            GraphId.From(request.GraphId), 
            cancellationToken);
        //  👆 await here
        
        if (graph == null)
            return Result.Failure<Guid>(/*...*/);
        
        // 2. Execute domain logic (sync - it's fast)
        var result = graph.AddNode(
            request.Name,
            NodeType.FromName(request.NodeType),
            Position.Create(request.X, request.Y));
        
        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);
        
        // 3. Save to database (async)
        await _repository.UpdateAsync(graph, cancellationToken);
        //  👆 await here
        
        return Result.Success(result.Value.Id.Value);
    }
}
```

**3. File Operations**
```csharp
public class JsonGraphExporter : IGraphExporter
{
    public async Task<string> ExportAsync(Graph graph, string filePath)
    {
        // 1. Serialize (sync - it's fast)
        var json = JsonSerializer.Serialize(graph, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        // 2. Write to file (async - I/O operation)
        await File.WriteAllTextAsync(filePath, json);
        //  👆 await: don't block while writing to disk
        
        return filePath;
    }
    
    public async Task<Graph> ImportAsync(string filePath)
    {
        // 1. Read from file (async - I/O operation)
        var json = await File.ReadAllTextAsync(filePath);
        //           👆 await: don't block while reading from disk
        
        // 2. Deserialize (sync)
        var graph = JsonSerializer.Deserialize<Graph>(json);
        
        return graph;
    }
}
```

**4. Multiple Async Operations**
```csharp
// Sequential: Wait for each one
public async Task<List<Graph>> LoadMultipleGraphsSequential(List<GraphId> ids)
{
    var graphs = new List<Graph>();
    
    foreach (var id in ids)
    {
        var graph = await _repository.GetByIdAsync(id);
        // 👆 Waits for each one to complete before starting next
        if (graph != null)
            graphs.Add(graph);
    }
    
    return graphs;
    // If 10 graphs, each takes 100ms = 1000ms total
}

// Parallel: Start all at once
public async Task<List<Graph>> LoadMultipleGraphsParallel(List<GraphId> ids)
{
    var tasks = ids.Select(id => _repository.GetByIdAsync(id));
    // 👆 Start all tasks immediately (don't await yet!)
    
    var graphs = await Task.WhenAll(tasks);
    // 👆 Wait for ALL to complete
    
    return graphs.Where(g => g != null).ToList();
    // If 10 graphs, each takes 100ms = 100ms total! (10x faster!)
}

// Use parallel when operations are independent
public async Task SaveMultipleGraphs(List<Graph> graphs)
{
    var tasks = graphs.Select(g => _repository.UpdateAsync(g));
    await Task.WhenAll(tasks);  // Save all in parallel
}
```

---

### **Async Rules You Follow**

**Rule 1: Async all the way up**
```csharp
// ✅ Good: async all the way
public async Task MyMethodAsync()
{
    await DoSomethingAsync();  // Async method
}

public async Task DoSomethingAsync()
{
    await _repository.GetByIdAsync(id);  // Async database call
}

// ❌ Bad: mixing sync and async
public void MyMethod()  // Sync method
{
    DoSomethingAsync().Wait();  // BLOCKS! Defeats purpose of async
}
```

**Rule 2: Use CancellationToken**
```csharp
// Allow operations to be cancelled
public async Task<Graph?> GetByIdAsync(
    GraphId id, 
    CancellationToken cancellationToken = default)  // 👈 Accept token
{
    return await _context.Graphs
        .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);  // 👈 Pass token
}

// Usage:
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(5));  // Cancel after 5 seconds

try
{
    var graph = await repository.GetByIdAsync(graphId, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

**Rule 3: Don't use async for fast operations**
```csharp
// ❌ Don't: No point making this async
public async Task<string> GetNodeName(Node node)
{
    return await Task.FromResult(node.Name);  // Just wasted overhead
}

// ✅ Do: Keep it sync (it's instant)
public string GetNodeName(Node node)
{
    return node.Name;
}

// Rule: Only use async for I/O operations (database, file, network)
```

**Rule 4: Name async methods with Async suffix**
```csharp
// ✅ Good naming
public async Task<Graph> GetByIdAsync(GraphId id)
public async Task SaveAsync(Graph graph)
public async Task<List<Graph>> GetAllAsync()

// ❌ Bad naming (confusing)
public async Task<Graph> GetById(GraphId id)  // Missing Async suffix
```

---

### **Async in Your UI (Avalonia)**
```csharp
public class GraphEditorViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    
    public ReactiveCommand<Unit, Unit> LoadGraphCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveGraphCommand { get; }
    
    public GraphEditorViewModel(IMediator mediator)
    {
        _mediator = mediator;
        
        // Commands can be async
        LoadGraphCommand = ReactiveCommand.CreateFromTask(LoadGraphAsync);
        SaveGraphCommand = ReactiveCommand.CreateFromTask(SaveGraphAsync);
    }
    
    private async Task LoadGraphAsync()
    {
        try
        {
            IsLoading = true;  // Show spinner
            
            var query = new GetGraphQuery(SelectedGraphId);
            var graph = await _mediator.Send(query);
            // 👆 UI stays responsive while loading!
            
            CurrentGraph = graph;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;  // Hide spinner
        }
    }
    
    private async Task SaveGraphAsync()
    {
        try
        {
            IsSaving = true;
            
            var command = new UpdateGraphCommand(CurrentGraph.Id, /*...*/);
            await _mediator.Send(command);
            // 👆 UI stays responsive while saving!
            
            ShowSuccessMessage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

---

## 🎯 Summary: Advanced C# for BahyWay

### **Generics:**
- `Entity<TId>` - reusable for all entity types
- `Result<T>` - success with value or failure with error
- `IRepository<T>` - repository for any aggregate

### **Collections:**
- `List<T>` - ordered, most common
- `Dictionary<TKey, TValue>` - fast lookup by key
- `HashSet<T>` - unique items, fast contains
- `IReadOnlyList<T>` - expose read-only

### **LINQ:**
- `Where()` - filter
- `Select()` - transform
- `First/FirstOrDefault()` - find single
- `Any/All()` - check conditions
- `OrderBy/ThenBy()` - sort
- `GroupBy()` - group items
- `Count/Sum/Average/Max/Min()` - aggregate

### **Async:**
- Database calls - always async
- File I/O - always async
- Network - always async
- Domain logic - keep sync
- Name methods with `Async` suffix
- Use `CancellationToken`

---

**Ready for Part 4: Design Patterns?**

Next covers your actual patterns:
- Value Objects (Position, Color)
- Entities & Aggregates (Node, Graph)
- Repository Pattern (IGraphRepository)
- CQRS (Commands, Queries, Handlers)
- Result Pattern (Result<T>)
- Domain Events (GraphCreatedEvent)

Continue? 🚀
