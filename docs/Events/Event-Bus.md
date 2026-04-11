# Event Bus Reference

## EventBus&lt;TAction&gt;

Generic event channel scoped to a specific `Enum` type. One bus handles every action defined by that enum.

### Type parameter

`TAction : struct, Enum` -- should be decorated with `[Flags]` for masking semantics.

### Members

| Signature | Notes |
|-----------|-------|
| `Subscription Subscribe(TAction mask, Action<TAction, EventContext<TAction>> handler)` | Register a handler. The handler fires whenever a published action has any bit in common with `mask`. |
| `void Unsubscribe(Subscription subscription)` | Remove a specific subscription. No-op if the handle is `null` or already removed. |
| `void Publish(TAction action, EventContext<TAction> context)` | Fire a single action. The context's `Action` property is set automatically. |
| `void Clear()` | Remove all subscribers. Called by `EventBusManager.Unload`. |
| `int SubscriberCount` | Diagnostics -- the current subscriber count. |

### Bitmask dispatch

Subscribers receive the event when `(subscriberMask & action) != 0`. This is implemented via `Convert.ToInt64` so any underlying integer type is handled correctly.

| Mask | Published | Fires? |
|------|-----------|--------|
| `A \| B` | `A` | yes |
| `A \| B` | `B` | yes |
| `A \| B` | `C` | no |
| `A \| B \| C` | `A` | yes |

### Snapshot-on-publish

Before iteration, `Publish` snapshots the internal subscriber list. A handler that calls `Subscribe` or `Unsubscribe` during dispatch mutates the backing list but does not invalidate the iteration.

Subscribers added during dispatch are **not** seen by the current `Publish` call -- they will see subsequent publishes.

### Exception handling

`EventBus<T>` does NOT catch exceptions thrown by subscribers. An unhandled exception propagates out of `Publish` and back to the caller. If a domain needs per-handler isolation, wrap the `Publish` call:

```csharp
try { bus.Publish(action, ctx); }
catch (Exception ex) { Logger.LogException(ex, "Event dispatch failed"); }
```

## EventBusManager

Registry that caches one bus per action enum type.

### Members

| Signature | Notes |
|-----------|-------|
| `EventBus<T> GetOrCreate<T>()` | Returns the bus for enum `T`, creating it on first call. The same instance is returned on every subsequent call. |
| `void Load()` | No-op. Buses are created lazily on the first `GetOrCreate`. |
| `void Unload()` | Calls `Clear()` on every cached bus and drops the cache. |

### Typical lifecycle

```csharp
var manager = new EventBusManager();
manager.Load();

// First call constructs the bus:
var bus1 = manager.GetOrCreate<FactionAction>();
// Second call returns the same instance:
var bus2 = manager.GetOrCreate<FactionAction>();
// bus1 and bus2 are the same reference.

manager.Unload();
// All buses cleared; subscribers on bus1/bus2 no longer fire.
```
