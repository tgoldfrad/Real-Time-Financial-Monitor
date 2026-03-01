# Real-Time Financial Monitor

A full-stack MVP for real-time financial transaction monitoring, built with
**.NET 9** (backend) and **React + TypeScript** (frontend).

Transactions are ingested via REST API, stored in **SQLite**, and instantly
broadcast to all connected dashboards via **SignalR**.

---

## Architecture

```
┌────────────────┝       POST /api/transactions        ┌──────────────────────┝
│                │ ────────────────────────────────────▶ │   ASP.NET Core API   │
│  React + Vite  │                                      │  ┌────────────────┝  │
│  (TypeScript)  │ ◀──── SignalR WebSocket ──────────── │  │ TransactionHub │  │
│                │       "ReceiveTransaction"            │  └────────────────┘  │
└────────────────┘                                      │  ┌────────────────┝  │
   localhost:5173                                       │  │ SQLite (EF9)  │  │
                                                        │  └────────────────┘  │
                                                        └──────────┬───────────┘
                                                                   │ (optional)
                                                        ┌──────────▼───────────┝
                                                        │   Redis Backplane    │
                                                        │  (multi-pod sync)    │
                                                        └──────────────────────┘
```

### Backend

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 9 (Minimal Hosting) |
| Real-time | SignalR with optional Redis backplane |
| Storage | SQLite via EF Core 9 (WAL mode, Scoped) |
| Validation | FluentValidation |
| Error handling | Global middleware → consistent JSON errors |
| Testing | NUnit + Moq + FluentAssertions (15 tests) |

### Frontend

| Layer | Technology |
|-------|-----------|
| Framework | React 19 + TypeScript 5.9 |
| Bundler | Vite 7 |
| Routing | react-router-dom (2 routes) |
| Real-time | @microsoft/signalr client |
| State Management | Redux Toolkit (combineSlices) |
| Styling | CSS Modules |
| Performance | requestAnimationFrame batching for rapid updates |

---

## Quick Start (local development)

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js ≥ 20.19](https://nodejs.org/)

### Backend

```bash
cd backend
dotnet restore
dotnet run --project FinancialMonitor.Api
# → http://localhost:5000
# → Health check: http://localhost:5000/health
```

### Frontend

```bash
cd frontend
npm install
npm run dev
# → http://localhost:5173
```

### Run Tests

```bash
cd backend
dotnet test
# 15 tests: Storage (4), Concurrency (3), Service (4), Validation (4)
```

---

## Running with Docker

```bash
# From project root
docker compose up --build

# Backend  → http://localhost:5000
# Frontend → http://localhost:80
```

---

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_URLS` | `http://localhost:5000` | Kestrel listen address |
| `REDIS_CONNECTION` | *(empty — disabled)* | Redis connection string; enables SignalR backplane |
| `CORS_ORIGINS` | `http://localhost:5173,http://localhost:3000` | Comma-separated allowed origins |

---

## API Endpoints

| Method | Path | Description | Response |
|--------|------|-------------|----------|
| `POST` | `/api/transactions` | Ingest a transaction | `201` / `400` / `409` |
| `GET` | `/api/transactions` | List all (newest first) | `200` |
| `GET` | `/api/transactions/{id}` | Get by ID | `200` / `404` |
| `GET` | `/health` | Health check | `200` |

### SignalR Hub

- **URL:** `/hubs/transactions`
- **Client event:** `ReceiveTransaction` — fired on every new transaction

---

## Distributed Architecture

When deployed to multiple replicas, a **Redis Backplane** ensures all pods share
SignalR broadcasts. See the full architecture decision record:

📄 [ADR-001: Distributed SignalR with Redis Backplane](backend/docs/adr/001-distributed-signalr.md)

---

## Kubernetes Deployment

Example manifests are provided in the `k8s/` directory:

```bash
kubectl apply -f k8s/
```

- `deployment.yaml` — 1 replica with health probes and resource limits
- `service.yaml` — ClusterIP Service for internal load balancing
- `redis.yaml` — Redis pod + service for the backplane
