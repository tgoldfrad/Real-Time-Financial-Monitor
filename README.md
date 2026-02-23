# Real-Time Financial Monitor

A full-stack MVP for real-time financial transaction monitoring, built with
**.NET 9** (backend) and **React + TypeScript** (frontend).

Transactions are ingested via REST API, stored in-memory, and instantly
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
   localhost:5173                                       │  │  InMemoryStore │  │
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
| Storage | `ConcurrentDictionary` (in-memory, Singleton) |
| Validation | Service-level + DataAnnotations on DTO |
| Error handling | Global middleware → consistent JSON errors |
| Testing | xUnit + Moq + FluentAssertions (35 tests) |

### Frontend

| Layer | Technology |
|-------|-----------|
| Framework | React 19 + TypeScript 5.9 |
| Bundler | Vite 7 |
| Routing | react-router-dom (2 routes) |
| Real-time | @microsoft/signalr client |
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
# 35 tests: Storage (11), Service (11), Controller (9), Validation (4)
```

---

## Running with Docker

```bash
# From project root
docker compose up --build

# Backend  → http://localhost:5000
# Frontend → http://localhost:80
# Redis    → localhost:6379 (internal)
```

The `docker-compose.yml` wires up three services:
- **backend** — .NET 9 API with Redis backplane enabled
- **frontend** — Nginx serving the Vite build, proxying API/SignalR to backend
- **redis** — Redis 7 Alpine for SignalR pub/sub

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

- `deployment.yaml` — 3 replicas with health probes and resource limits
- `service.yaml` — ClusterIP Service for internal load balancing
- `redis.yaml` — Redis pod + service for the backplane

---

## Project Structure

```
task/
├── README.md
├── docker-compose.yml
├── k8s/
│   ├── deployment.yaml
│   ├── service.yaml
│   └── redis.yaml
├── backend/
│   ├── FinancialMonitor.sln
│   ├── Dockerfile
│   ├── docs/adr/001-distributed-signalr.md
│   └── FinancialMonitor.Api/
│       ├── Controllers/
│       ├── Hubs/
│       ├── Middleware/
│       ├── Models/
│       ├── Services/
│       └── Storage/
└── frontend/
    ├── Dockerfile
    ├── nginx.conf
    └── src/
        ├── components/
        ├── hooks/
        ├── pages/
        ├── services/
        └── types/
```
