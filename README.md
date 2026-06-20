# Session 3 - Async Programming

## Topics

- async
- await
- Task<T>
- Task.WhenAll
- Exception Handling

## Golden Rule

If method returns Task:

```csharp
await method();
```

Never:

```csharp
.Result
.Wait()
```

## Learning Outcome

Build responsive and scalable applications.
