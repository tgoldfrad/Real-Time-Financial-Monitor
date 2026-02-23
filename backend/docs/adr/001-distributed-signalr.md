# ADR-001: Distributed SignalR with Redis Backplane

## Status

**Accepted** — Implemented (conditionally activated via environment variable)

## Date

2026-02-23

## Context

The Real-Time Financial Monitor uses **ASP.NET Core SignalR** to broadcast new
transactions to all connected browser clients. In a single-instance deployment
this works out of the box: every WebSocket connection terminates at the same
process, so `IHubContext.Clients.All.SendAsync(...)` reaches every client.

### The Problem

When we scale horizontally — for example deploying **5 Kubernetes pods** behind a
load balancer — each pod runs its own SignalR hub in its own process memory.

```
                  ┌─────────┝
   Client A ───▶  │  Pod 1  │  ↝ has its own hub + in-xxxxxxxxxxxxctions
                  └─────────┘
                  ┌─────────┝
   Client B ───▶  │  Pod 2  │  ↝ different hub, different connections
                  └─────────┘
                       ...
                  ┌─────────┝
   Client E ───▶  │  Pod 5  │
                  └─────────┘
```

If a `POST /api/transactions` request hits **Pod 1**, the `TransactionService`
calls `IHubContext.Clients.All.SendAsync("ReceiveTransaction", dto)`. This only
broadcasts to clixxxxxxxxxxcted **to Pod 1's hub**. Clients B through E, connected
to other pods, **never receive the message**.

Similarly, the `InMemoryTransactionStore` (backed by `ConcurrentDictionary`) is
local to each pod. A `GET /api/transactions` hitting Pod 2 won't include a
transaction that was stored on Pod 1.

## Decision

We solve the SignalR broadcast problem using a **Redis Backplane** and outline the
storage problem for future resolution.

### 1. SignalR Redis Backplane

ASP.NET Core SignalR has built-in support for
[Redis as a backplane](https://learn.microsoft.com/en-us/aspnet/core/signalr/redis-backplane).
When configured, every `SendAsync` call publishes the message to a Redis
Pub/Sub channel. All pods subscribe to that channel and re-broadcast to their
local clients:

```
                  ┌─────────┝
   Client A ───▶  │  Pod 1  │──┝
                  └─────────┘  │
                  ┌─────────┝  │    ┌─────────┝
   Client B ───▶  │  Pod 2  │──┼───▶│  Redis  │ (Pub/Sub)
                  └─────────┘  │    └─────────┘
                  ┌─────────┝  │
   Client C ───▶  │  Pod 3  │──┘
                  └─────────┘
```

**Implementation** (already in `Program.cs`):

```csharp
var redisConnection = builder.Configuration["REDIS_CONNECTION"];
if (!string.IsNullOrEmpty(redisConnection))
{
    signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix =
            StackExchange.Redis.RedisChannel.Literal("FinancialMonitor");
    });
}
```

The backplane is **conditionally activated**: when `REDxxxxxxxxCTION` is not set
(e.g. local development), SignalR operates in single-instance mode with zero
overhead.

### 2. Shared Storage (future work)

The in-memory `ConcurrentDictionary` store must also be replaced for true
multi-pod operation. Options considered:

| Option | Pros | Cons |
|--------|------|------|
| **PostgreSQL / SQL Server** | Durable, ACID, familiar | Higher latency for writes |
| **Redis (shared cache)** | Very fast, same infra as backplane | Data lost if Redis restarts (unless persistence is on) |
| **Event Sourcing + message queue** | Full audit trail, decoupled | High complexity |

**Recommended path:** PostgreSQL for durability + Redis cache for fast reads.
The `ITransactionStore` interface makes this a drop-in replacement — no changes
needed in `TransactionService` or `TransactionsController`.

## Consequences

### Positive

- **Horizontal scalability**: all pods share broadcasts; adding replicas is
  transparent.
- **Zero local-dev overhead**: Redis is only activated when the env var is
  present.
- **Drop-in**: one line of code plus a NuGet package; no changes to hub or
  service logic.
- **Interface-driven storage**: `ITransactionStore` allows swapping the backing
  store without touching business logic.

### Negative

- **Redis dependency in production**: adds an infrastructure component that must
  be monitored and kept available.
- **No message persistence**: if Redis is briefly unavailable, broadcasts during
  that window are lost (mitigated by client-side reconnect + GET refresh).

### Risks

- Redis Pub/Sub is fire-and-forget; if a pod misses a message during reconnect,
  the client will catch up on its next SignalR reconnection (the hook calls
  `fetchTransactions()` on connect).

## Alternatives Considered

1. **Sticky Sessions (Session Affinity)**: route each client to the same pod.
   Rejected because it doesn't solve the broadcast problem (Client A still
   won't see transactions ingested on Pod B) and complicates scaling.

2. **Azure SignalR Service**: fully managed, removes backplane responsibility.
   Rejected for now because it introduces vendor lock-in; Redis is portable
   across any cloud.

3. **Message Queue (RabbitMQ / Kafka)**: each pod consumes from a shared queue.
   Over-engineered for this use case; SignalR's native Redis backplane does
   exactly what we need with less complexity.
