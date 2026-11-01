# Async Migration Considerations

## Overview

This document covers the considerations for migrating the internal database and file APIs in CPBase to async, while maintaining backward compatibility with existing synchronous addon code.

## Migration Strategy

The recommended approach is:

1. Convert all internal implementation (file I/O, database calls) to async
2. Add new async methods to CPBase (e.g., `SaveAsync`, `ReadAsync`)
3. Keep the existing synchronous methods, but have them delegate to the async versions with a blocking wait

The blocking bridge in the sync methods would look like:

```csharp
// new async method
public async Task<string> ReadAsync(string filename) {
    return await core.cdnFiles.readAsync(filename);
}

// existing sync method, now delegates to async
public string Read(string filename) {
    return ReadAsync(filename).GetAwaiter().GetResult();
}
```

### Why `.GetAwaiter().GetResult()` instead of `.Result` or `.Wait()`

`.GetAwaiter().GetResult()` unwraps the exception directly rather than wrapping it in an `AggregateException`, so existing error handling in addon code continues to work unchanged.

### Where This Is Safe

- **CLI (cc.exe)** — No `SynchronizationContext`, no deadlock risk
- **TaskService** — No `SynchronizationContext`, no deadlock risk
- **WebApi (.NET Core)** — .NET Core has no `SynchronizationContext` by default, no deadlock risk
- **Framework ASPX site (iisDefaultSite)** — Has a single-threaded `SynchronizationContext`, deadlock risk exists (see below)

## Deadlock Risk in Framework ASPX Sites

### How It Happens

The deadlock from sync-over-async with a `SynchronizationContext` is **deterministic, not probabilistic**. When it occurs, it occurs on every request that hits the affected code path — not 1 in 1,000 or 1 in 1,000,000, but 100% of the time.

The mechanism:

1. ASP.NET Framework has a single-threaded `SynchronizationContext` per request
2. The sync method calls `.GetAwaiter().GetResult()`, which blocks the request thread
3. The async method's `await` completes and tries to resume on that same request thread (because the sync context captured it)
4. That thread is blocked waiting for the result
5. Neither side can proceed — classic deadlock

### What the User Sees

The request **freezes (hangs indefinitely)**, never returning a response. It is not a crash or error page. IIS eventually kills the request with a timeout (default 110 seconds), and the user sees a generic timeout error.

### How to Prevent It: `ConfigureAwait(false)`

If all internal async methods use `ConfigureAwait(false)` on their awaits, the continuation will not try to resume on the original sync context, and the deadlock is avoided entirely.

```csharp
// SAFE — won't deadlock when called from sync-over-async bridge
public async Task<string> readAsync(string filename) {
    var bytes = await s3Client.GetObjectAsync(request).ConfigureAwait(false);
    return await processAsync(bytes).ConfigureAwait(false);
}
```

```csharp
// UNSAFE — will deadlock in Framework ASPX
public async Task<string> readAsync(string filename) {
    var bytes = await s3Client.GetObjectAsync(request);
    return await processAsync(bytes);
}
```

This is the standard pattern for library code that may be consumed by callers with a `SynchronizationContext`. Every `await` in the Processor internals (file controllers, database controllers) must use `ConfigureAwait(false)`.

### Alternative: `Task.Run` Wrapper

If `ConfigureAwait(false)` is missed somewhere in the call chain, an alternative for the sync bridge methods is:

```csharp
public string Read(string filename) {
    return Task.Run(() => ReadAsync(filename)).GetAwaiter().GetResult();
}
```

This pushes execution off the request thread entirely, avoiding the sync context capture. The tradeoff is an extra thread pool hop per call.

## Summary

| Approach | Framework ASPX Safe | Performance | Complexity |
|---|---|---|---|
| `ConfigureAwait(false)` everywhere | Yes (if none are missed) | Best | Every `await` must include it |
| `Task.Run` wrapper in sync bridge | Yes (always) | Extra thread hop | Simple but wasteful |
| No mitigation | No — deterministic deadlock | N/A | N/A |

The recommended approach is `ConfigureAwait(false)` on every `await` in the Processor internals. This is tedious but straightforward, and is the standard pattern used by library authors (including Microsoft in EF Core) for code that may be consumed by callers with a sync context.
