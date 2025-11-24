# ⚠️ CRITICAL: Return Type Reference

## The Correct Return Types for MediatR Requests

### Commands that return nothing (just success/failure):
```csharp
public sealed record MyCommand(...) : IRequest<r>;
//                                              ↑ Returns Result (no value)

public async Task<r> Handle(...) 
//            ↑ Returns Result
{
    return Result.Success();  // or Result.Failure(error)
}
```

### Commands that return a value:
```csharp
public sealed record MyCommand(...) : IRequest<Result<int>>;
//                                              ↑ Returns Result<int>

public async Task<Result<int>> Handle(...) 
//            ↑ Returns Result<int>
{
    return Result.Success(42);  // or Result.Failure<int>(error)
}
```

### Queries always return a value:
```csharp
public sealed record MyQuery(...) : IRequest<Result<MyDto>>;
//                                            ↑ Returns Result<MyDto>

public async Task<Result<MyDto>> Handle(...) 
//            ↑ Returns Result<MyDto>
{
    return Result.Success(myDto);
}
```

---

## In Your AlarmInsight Commands:

### CreateAlarmCommand (returns alarm ID):
```csharp
: IRequest<Result<int>>
```

### ProcessAlarmCommand (returns only success/failure):
```csharp
: IRequest<r>
```

### ResolveAlarmCommand (returns only success/failure):
```csharp
: IRequest<r>
```

### GetAlarmQuery (returns alarm DTO):
```csharp
: IRequest<Result<AlarmDto>>
```

### GetActiveAlarmsQuery (returns list of DTOs):
```csharp
: IRequest<Result<IEnumerable<AlarmSummaryDto>>>
```

---

## ✅ If You Get Errors

If you see: **"The type or namespace name 'Result' could not be found"**

Add this using statement:
```csharp
using BahyWay.SharedKernel.Domain.Primitives;
```

---

This is the definitive reference for all return types!
