<div align="center">

# NoobGg

### Player matching · Live chat · Community — in one place

**Create rooms, build squads, send DMs, get notifications.**  
A full-stack **ASP.NET Core 8** + **React 19** application with **SignalR** for real-time features, **MongoDB** for persistence, and **Redis** for scaling SignalR and presence-related workloads.

<br/>

[![.NET](https://img.shields.io/badge/.NET_8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React_19-61DAFB?style=for-the-badge&logo=react&logoColor=222222)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![MongoDB](https://img.shields.io/badge/MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white)](https://www.mongodb.com/)
[![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)](https://redis.io/)
[![Vite](https://img.shields.io/badge/Vite_6-646CFF?style=for-the-badge&logo=vite&logoColor=white)](https://vitejs.dev/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_v4-06B6D4?style=for-the-badge&logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)

<br/>

[Features](#-features) · [Architecture](#-architecture) · [Quick start](#-quick-start) · [Configuration](#-configuration) · [API surface](#-rest-api-overview) · [Documentation](#-documentation)

</div>

---

## Table of contents

- [Overview](#-overview)
- [Features](#-features)
- [Architecture](#-architecture)
- [Technology stack](#-technology-stack)
- [Repository structure](#-repository-structure)
- [Prerequisites](#-prerequisites)
- [Quick start](#-quick-start)
- [Configuration](#-configuration)
- [REST API overview](#-rest-api-overview)
- [SignalR real-time layer](#-signalr-real-time-layer)
- [Background services](#-background-services)
- [Frontend application](#-frontend-application)
- [Development workflows](#-development-workflows)
- [Docker reference](#-docker-reference)
- [Documentation](#-documentation)
- [Security notes](#-security-notes)

---

## Overview

**NoobGg** is a **gaming community and matchmaking** web platform. Players can discover games and each other, manage rich profiles (including per-game profiles), create and join **rooms**, chat in real time, exchange **direct messages (DMs)**, receive **in-app notifications**, manage **friends / blocks / favorites**, and work with **subscription plans**. Role-based users can access **reports** and **moderation** flows.

The backend follows a **layered, clean-style** split: **Domain** → **Application** (MediatR features) → **Infrastructure** (MongoDB, Redis, email, external APIs) → **API** (HTTP controllers, SignalR hubs, middleware). The client is a **Vite-powered SPA** organized by **features** and shared UI primitives, using **TanStack Query** for server state and **Zustand** for authentication state.

---

## Features

| Area | Description |
|:---|:---|
| **Smart recommendations** | Authenticated **player** and **room** recommendations based on shared games, region, experience, languages, LFT signals, online presence, and room metadata (see `docs/NoobGg-Proje-Raporu.md` for scoring rules). |
| **Rooms** | Create, join, leave, invite; room metadata; in-room presence and chat via SignalR. |
| **Real-time messaging** | **SignalR** hubs for **chat**, **room** events, **DMs**, and **notifications**; Redis-backed scaling for SignalR where configured. |
| **Profiles** | Player profile, multiple **game profiles**, avatar and banner uploads (static file hosting under the API `wwwroot`). |
| **Social graph** | Friend requests and lists, blocking, favorites. |
| **Notifications** | In-app notification feed; real-time delivery through the notification hub. |
| **Subscriptions** | Plans and subscription management (seeded plans on startup where applicable). |
| **Moderation** | Reports and moderation endpoints for elevated roles. |
| **Elo / competitive** | Match recording, leaderboards, history, and session results (`/api/elo`). |
| **Guilds** | Guild-related HTTP API (see `GuildsController`). |
| **Auth & security** | **JWT** access tokens with refresh flow; email verification; protected routes on the client (`ProtectedRoute`, `RequireProfile`); moderator/admin gating where applicable. |
| **Game catalog** | Game search and detail backed by persistence; optional **RAWG** sync job for catalog enrichment (API key required). |

---

## Architecture

High-level data flow:

```mermaid
flowchart LR
  subgraph client["Client"]
    R[React 19 + Vite 6]
    T[TanStack Query]
    Z[Zustand]
    SR[@microsoft/signalr]
  end

  subgraph api["NoobGg.Api"]
    C[Controllers / MediatR]
    H[SignalR Hubs]
    MW[Middleware]
  end

  subgraph data["Data & cache"]
    M[(MongoDB)]
    RD[(Redis)]
  end

  R -->|HTTP / JSON| C
  SR -->|WebSockets| H
  C --> M
  H --> M
  H --> RD
```

**Solution projects**

| Project | Responsibility |
|--------|----------------|
| `NoobGg.Domain` | Entities, enums, core domain types. |
| `NoobGg.Application` | Commands/queries, **MediatR** handlers, DTOs, **FluentValidation**, application interfaces. |
| `NoobGg.Infrastructure` | MongoDB context, Redis, JWT and password hashing (**BCrypt**), **MailKit** email, HTTP clients (e.g. RAWG). |
| `NoobGg.Api` | ASP.NET Core host, controllers, SignalR hubs, **Serilog** logging, **Swagger** (development), CORS, static files, hosted background jobs. |

---

## Technology stack

### Backend

| Concern | Technology |
|--------|------------|
| Runtime | **.NET 8** |
| API style | REST + SignalR |
| Application layer | **MediatR**, **FluentValidation** |
| Database | **MongoDB** (official C# driver) |
| Cache / SignalR scale-out | **Redis** (`StackExchange.Redis`, `Microsoft.AspNetCore.SignalR.StackExchangeRedis`) |
| Auth | **JWT Bearer** |
| Logging | **Serilog** (console, rolling file, MongoDB BSON sink) |
| API docs | **Swashbuckle** (Swagger UI at `/swagger` in Development) |

### Frontend

| Concern | Technology |
|--------|------------|
| UI library | **React 19** |
| Build tool | **Vite 6** |
| Language | **TypeScript** |
| Routing | **React Router 7** |
| Server state | **TanStack Query** (+ devtools) |
| Client state | **Zustand** (auth) |
| HTTP | **Axios** (with refresh-token handling in `client/src/lib/api.ts`) |
| Real-time | **@microsoft/signalr** |
| Forms & validation | **react-hook-form**, **Zod**, **@hookform/resolvers** |
| Styling | **Tailwind CSS v4** (`@tailwindcss/vite`) |
| Motion | **Framer Motion** |
| Charts | **Recharts** (where used) |

---

## Repository structure

```
├── client/                      # React SPA (Vite, TypeScript, Tailwind v4)
│   ├── src/
│   │   ├── app/                 # Router, layouts, route guards
│   │   ├── components/          # Shared UI (ui, layout, common, chat, …)
│   │   ├── features/            # Feature modules (api, hooks, types, schemas)
│   │   ├── lib/                 # api.ts, signalr.ts, queryKeys, utils
│   │   ├── pages/               # Route-level pages
│   │   └── providers/           # DM, room, notification hub providers
│   └── .env.example             # VITE_API_URL template
├── src/
│   ├── NoobGg.Api/              # Web host, controllers, hubs, middleware, Dockerfile
│   ├── NoobGg.Application/
│   ├── NoobGg.Domain/
│   └── NoobGg.Infrastructure/
├── docs/                        # Deep-dive reports (Turkish + references)
├── docker-compose.yml           # API + MongoDB + Redis
├── docker-compose.override.yml  # Dev overrides (e.g. log volume)
└── NoobGg.sln                   # Visual Studio / dotnet CLI solution
```

---

## Prerequisites

- **[.NET 8 SDK](https://dotnet.microsoft.com/download)** — build and run the API locally
- **[Node.js](https://nodejs.org/) 20+** — install and run the Vite client
- **[Docker](https://www.docker.com/)** (optional but recommended) — one-command API + MongoDB + Redis

---

## Quick start

### Option A — Full stack with Docker (API + databases)

From the repository root:

```bash
docker compose up --build
```

This starts:

| Service | Port | Notes |
|--------|------|--------|
| **API** | **5000** | `ASPNETCORE_URLS=http://+:5000`, Development environment |
| **MongoDB** | **27017** | Database name `noobgg` (via `MongoDb__DatabaseName`) |
| **Redis** | **6379** | Used for SignalR backplane and related infrastructure |

Point the SPA at the API:

```bash
cd client
cp .env.example .env
# Set VITE_API_URL=http://localhost:5000 when using Docker Compose
npm install
npm run dev
```

The Vite dev server defaults to **http://localhost:5173**; ensure this origin is allowed in API **CORS** settings (see [Configuration](#-configuration)).

### Option B — Run the API with `dotnet run` (local SDK)

1. Start **MongoDB** and **Redis** locally (or use Docker only for those services).
2. Update `src/NoobGg.Api/appsettings.json` (or user secrets / environment variables) so `MongoDb:ConnectionString` and `Redis:ConnectionString` match your machines.
3. From the solution root:

```bash
dotnet build NoobGg.sln
dotnet run --project src/NoobGg.Api/NoobGg.Api.csproj
```

The **http** profile in `launchSettings.json` uses **http://localhost:5071** by default. Then set:

```env
VITE_API_URL=http://localhost:5071
```

in `client/.env` so the browser targets the same origin the API actually uses.

### Client production build

```bash
cd client
npm run build
npm run preview   # optional: local preview of the production bundle
```

---

## Configuration

Configuration is read from `appsettings.json`, `appsettings.{Environment}.json`, environment variables, and command-line arguments (standard ASP.NET Core). For local secrets, prefer **[User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)** or environment variables rather than committing credentials.

### Important settings (keys only)

| Section | Purpose |
|--------|---------|
| `MongoDb:ConnectionString` | MongoDB server URL |
| `MongoDb:DatabaseName` | Database name (default `noobgg`) |
| `Redis:ConnectionString` | Redis host/port |
| `Redis:InstanceName` | Key prefix for Redis (default `NoobGg:`) |
| `Jwt:Secret` | Signing key — **must be a long, random secret in production** |
| `Jwt:Issuer` / `Jwt:Audience` | JWT claim validation |
| `Jwt:AccessTokenExpirationMinutes` / `RefreshTokenExpirationDays` | Token lifetimes |
| `Cors:AllowedOrigins` | Browser origins allowed to call the API (e.g. `http://localhost:5173`) |
| `Email:*` | SMTP settings for verification and transactional email |
| `Rawg:*` | [RAWG Video Games Database API](https://rawg.io/apidocs) for optional game catalog sync (`ApiKey`, rate limits, sync toggles) |

### Frontend environment

| Variable | Description |
|----------|-------------|
| `VITE_API_URL` | Base URL of the API **without** a trailing slash (used for REST and SignalR in `client/src/lib/api.ts` and `client/src/lib/signalr.ts`). |

**Port alignment:** Docker Compose exposes the API on **5000**; local `dotnet run` may use **5071** per `Properties/launchSettings.json`. Your `VITE_API_URL` must match whichever you run.

---

## REST API overview

Controllers live under `src/NoobGg.Api/Controllers/`. Typical base path: `/api/...` (see each controller’s `[Route]` attribute; `HealthController` uses `api/health`).

| Controller | Role |
|------------|------|
| `AuthController` | Register, login, refresh, logout, `me`, email verification, resend verification |
| `UsersController` | User discovery, presence-related endpoints |
| `ProfilesController` | Profile and game profiles, avatar/banner |
| `GamesController` | Game catalog, search, detail |
| `RoomsController` | Rooms: CRUD-style flows, join/leave, invites (some actions may allow anonymous access as annotated) |
| `ChatController` | Room messages over HTTP (complements real-time hub) |
| `DirectMessagesController` | Conversations and DM messages |
| `FriendsController` | Friend requests and lists |
| `BlocksController` | Blocking |
| `FavoritesController` | Favorites |
| `NotificationsController` | Notification feed |
| `SettingsController` | User settings |
| `SubscriptionsController` | Plans and subscriptions |
| `ReportsController` | User reports |
| `ModerationController` | Moderation actions (role-gated) |
| `RecommendationsController` | `GET` recommended players and rooms (authorized) |
| `EloController` | Elo match recording, leaderboard, history, session results |
| `GuildsController` | Guild-related operations |
| `HealthController` | Liveness-style JSON payload (`status`, `timestamp`, `version`) |

In **Development**, open **Swagger UI** at `/swagger` for an interactive contract.

---

## SignalR real-time layer

Hubs are mapped in `Program.cs`:

| Path | Hub | Notes |
|------|-----|--------|
| `/hubs/chat` | `ChatHub` | Room chat; authorized connections |
| `/hubs/room` | `RoomHub` | Room lifecycle / presence style events |
| `/hubs/dm` | `DirectMessageHub` | Direct messages |
| `/hubs/notifications` | `NotificationHub` | Push notifications to clients |

The React app builds hub URLs from `VITE_API_URL` (see `client/src/lib/signalr.ts`) and attaches the JWT the same way as HTTP calls. Hub methods and client contracts are defined server-side under `NoobGg.Api/Hubs` and consumed from providers such as `dmProvider.tsx`, `roomProvider.tsx`, and `notificationProvider.tsx`.

---

## Background services

Hosted services registered in `Program.cs` include (non-exhaustive behavior — see source for conditions):

| Service | Purpose |
|---------|---------|
| `DatabaseMigrationRunner` | Applies database migration logic on startup |
| `MongoIndexInitializer` | Ensures MongoDB indexes |
| `PlanSeedInitializer` | Seeds subscription plans |
| `FakeDataSeeder` | Development-oriented fake data (see `docs/FakeDataSeeder-Referans-Rapor.md`) |
| `EloDataSeeder` | Elo-related seed data |
| `RoomMemberProfileSeeder` | Room member profile seeding |
| `GameCatalogSyncJob` | Periodic RAWG sync when enabled and configured |

**Tip:** Before sharing a database or running against production-like data, review seeder and sync jobs so you do not accidentally populate or hit external APIs unexpectedly.

---

## Frontend application

- **Routing and guards:** `client/src/app/router.tsx` — `ProtectedRoute` and `RequireProfile` protect most app routes; `/subscriptions` may omit full profile requirement; `/moderation` uses `RequireRole` for moderator/admin.
- **Features:** Under `client/src/features/*` — each feature typically exposes `api.ts`, `hooks.ts`, and `types.ts` (e.g. `recommendations` integrates with `GET /api/recommendations/players` and `/rooms`).
- **API client:** `client/src/lib/api.ts` — Axios instance, interceptors, refresh token flow.
- **Query keys:** `client/src/lib/queryKeys.ts` — centralized TanStack Query keys.

---

## Development workflows

```bash
# Build all .NET projects
dotnet build NoobGg.sln

# Frontend lint
cd client && npm run lint
```

Logs (API): Serilog writes to the console, rolling files under `logs/`, and a **MongoDB** `logs` collection (TTL configured in `Program.cs`). With `docker-compose.override.yml`, `./logs` on the host may be mounted into the container for file sinks.

---

## Docker reference

- **Build context:** `./src` with Dockerfile at `NoobGg.Api/Dockerfile`
- **Base image:** `mcr.microsoft.com/dotnet/aspnet:8.0`
- **Exposed port:** `5000`
- **Dependencies:** API service `depends_on` MongoDB and Redis; connection strings are wired via compose environment variables (`MongoDb__ConnectionString`, `Redis__ConnectionString`).

---

## Documentation

| Document | Content |
|----------|---------|
| [docs/NoobGg-Proje-Raporu.md](docs/NoobGg-Proje-Raporu.md) | Detailed product and technical report (Turkish), API and recommendation logic |
| [docs/FakeDataSeeder-Referans-Rapor.md](docs/FakeDataSeeder-Referans-Rapor.md) | Fake data seeder reference |
| [docs/Musteri-Urun-Envanteri-NoobGg.md](docs/Musteri-Urun-Envanteri-NoobGg.md) | Additional product/inventory-style notes |
| [NoobGgProductAnalysisReport.md](NoobGgProductAnalysisReport.md) | Product analysis report |

---

## Security notes

- **Never commit** production JWT secrets, SMTP passwords, or third-party API keys. Use environment variables or secret managers.
- If any credential ever appeared in tracked configuration files, **rotate** those credentials and remove them from history where possible.
- Restrict `Cors:AllowedOrigins` to known front-end origins in production.
- Keep MongoDB and Redis **network-isolated** in production; do not expose them publicly without authentication and TLS.

---

<div align="center">

**NoobGg** — *Not “noob” — part of the squad.*

If this project helps you, consider starring the repository.

</div>
