# ADR-001: Distributed SignalR with Redis Backplane

**Status:** Accepted | **Date:** 2026-02-23

## Problem

When running multiple server instances (e.g. 3 Kubernetes pods behind a load
balancer), each pod has its own SignalR hub in memory. A broadcast from Pod 1
only reaches clients connected to Pod 1 — clients on other pods miss the message.

## Solution

Use **Redis as a SignalR Backplane**. Every `SendAsync` call publishes to a Redis
Pub/Sub channel. All pods subscribe to that channel and re-broadcast to their
local clients — so every client receives every message regardless of which pod
it's connected to.

Activated conditionally via the `REDIS_CONNECTION` environment variable:

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

- **Local development:** no Redis needed — SignalR works in single-instance mode.
- **Production:** set `REDIS_CONNECTION=redis:6379` to enable the backplane.
- **Storage:** the `ITransactionStore` interface allows replacing the in-memory
  store with a database (e.g. PostgreSQL) in the future without changing any
  business logic.
