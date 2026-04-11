# BlueBeard.Events

BlueBeard.Events is a typed event bus with `[Flags]` enum masking for efficient subscriber filtering. Publish and subscribe to domain events across a plugin without tight coupling between producers and consumers.

## Features

- **Typed buses** -- `EventBus<TAction>` is scoped to a single `Enum`-typed action namespace. Separate enums give separate buses automatically.
- **Bitmask subscriptions** -- Subscribers register interest in `A | B | C`; dispatch fires the handler whenever any overlapping bit matches.
- **Carried context** -- `EventContext<TAction>` ships the specific action, an optional player, a UTC timestamp, and arbitrary key/value payload.
- **Advisory cancellation** -- Subscribers set `Cancelled = true`; publishers inspect the flag after `Publish` returns to decide whether to proceed.
- **Snapshot-on-publish** -- The subscriber list is copied before iteration, so handlers can add or remove subscriptions mid-dispatch without breaking the loop.
- **No swallowed exceptions** -- Subscriber exceptions propagate to the publisher; wrap `Publish` if you need isolation.
- **Manager registry** -- `EventBusManager` caches one bus per action enum for the lifetime of the plugin.

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Project setup, defining an action enum, publish/subscribe basics |
| [Event Bus](Event-Bus.md) | `EventBus<T>` and `EventBusManager` API reference |
| [Contexts](Contexts.md) | `EventContext<T>`, the `Cancelled` flag, payload conventions |
| [Examples](Examples.md) | Faction events, zone events, cancellation patterns |

## Source Classes

| Class / Interface | Role |
|-------------------|------|
| `EventBus<TAction>` | Generic typed bus; owns the subscriber list and dispatches events |
| `IEventBus` | Non-generic marker used by `EventBusManager` to clear buses during shutdown |
| `EventContext<TAction>` | Base context carried with every event (player, timestamp, payload, cancellation) |
| `Subscription` | Opaque handle returned by `Subscribe`, passed back to `Unsubscribe` |
| `EventBusManager` | `IManager` that caches one `EventBus<T>` per action enum type |
