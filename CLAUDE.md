# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## AI instruction files — keep in sync (MANDATORY)

This project has multiple AI instruction files. **Any time you change architecture, patterns, conventions, or project rules in one file, you MUST update all others in the same operation:**

| File | AI tool |
|---|---|
| `CLAUDE.md` (this file) | Claude Code |
| `.github/copilot-instructions.md` | GitHub Copilot |
| `docs/ARCHITECTURE.md` | Source of truth — always update this first |
| `docs/AGENT_GUIDE.md` | General agent guidance |

Never leave the files out of sync. A rule that exists only in one file will be ignored by the other tools.

---

## What this project is

Multi-tenant SaaS platform for artist portfolios. One Nuxt 3 app + one ASP.NET Core 10 API serve all tenants.

- **Frontend (Nuxt 3):** Vercel — `frontend/`
- **Backend (.NET 10):** Render (Docker) — `backend/OnlinePortfolio.Api/`
- **Database:** Supabase PostgreSQL via EF Core
- **Domain:** `onlineportfolio.com.br` (prod live)

Full architecture: `docs/ARCHITECTURE.md` · Schema: `docs/DATABASE.md` · Design patterns: `docs/DESIGN_PATTERNS.md`

---

## Commands

All paths relative to repo root unless noted.

### Full stack (recommended)

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| Frontend (Nuxt HMR) | http://localhost:3000 |
| API + Swagger | http://localhost:8080/swagger |
| Postgres | `localhost:5432` — user `portfolio`, db `portfolio_dev`, pw `portfolio_dev` |

### Backend only (Postgres via Docker)

```bash
docker compose up -d db
cd backend/OnlinePortfolio.Api
dotnet run --launch-profile http
```

### Frontend only

```bash
cd frontend
cp .env.example .env   # first time only
npm install            # first time only
npm run dev
```

### Simulate a specific surface on localhost

```bash
# in frontend/
set NUXT_PUBLIC_DEV_SURFACE=tenant
set NUXT_PUBLIC_DEV_TENANT_SLUG=ana
npm run dev
```

| `NUXT_PUBLIC_DEV_SURFACE` | Simulates |
|---|---|
| `dev` (default) | dev skeleton |
| `app` | `app.onlineportfolio.com.br` (admin) |
| `platform` | `onlineportfolio.com.br` (marketing) |
| `tenant` | `{slug}.onlineportfolio.com.br` |

### Backend tests

```bash
cd backend
dotnet test OnlinePortfolio.Api.slnx
```

Single test project:
```bash
dotnet test OnlinePortfolio.Api.Tests/OnlinePortfolio.Api.Tests.csproj
```

With coverage (Cobertura in `backend/TestResults/`):
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults
```

HTML coverage report (Windows, from repo root):
```bash
powershell -ExecutionPolicy Bypass -File scripts/coverage-backend.ps1
```

### Frontend lint + tests

```bash
cd frontend
npm run lint
npm run test:coverage
```

### EF Core migrations

Run from `backend/OnlinePortfolio.Api/`:
```bash
dotnet ef database update              # apply all pending
dotnet ef migrations add <Name>        # create from model changes
dotnet ef migrations list              # list + applied/pending status
dotnet ef migrations remove            # remove last (local dev only)
```

First time / after `docker compose down -v`:
```bash
docker compose down -v
docker compose up -d db
cd backend
dotnet tool restore
dotnet ef database update --project OnlinePortfolio.Api
```

After `database update`, start the API once so Identity roles seed (`PlatformAdmin`, `Owner`, `Editor`).

Full migration reference: `docs/DEV_COMMANDS.md`

---

## Architecture

### Backend layer pattern (mandatory)

| Layer | Folder | Rule |
|---|---|---|
| **Controller** | `Controllers/` | HTTP in/out only. No business logic. Calls services. |
| **Service** | `Services/` | All business rules and authorization. Calls repositories + `IUnitOfWork`. Never calls `DbContext` directly. |
| **Repository** | `Data/Repositories/` | All and only database access. No business rules. Never calls `SaveChanges`. |

Never skip a layer. No EF Core outside repositories.

### Design patterns (mandatory)

**Options Pattern** — every config block has a typed class in `Options/` registered via `services.Configure<T>()`. No raw `configuration["Key"]` access in services or controllers.

**Result Pattern** — services return `Result<T>` (from `Common/Result.cs`) for all expected outcomes. Exceptions only for unexpected failures. Controllers use `.Match(onSuccess, onFailure)` to map to HTTP responses. Use `Error.NotFound()`, `Error.Conflict()`, `Error.Unauthorized()`, `Error.Forbidden()`, `Error.Validation()` factory methods.

**Unit of Work** — `IUnitOfWork.CommitAsync()` is called only in services. Repositories only manipulate the EF change tracker. `ApplicationDbContext` implements `IUnitOfWork`.

### Multi-tenant model

All tenants share one Postgres DB. Row isolation via `TenantId` enforced in three layers:
1. **EF global query filters** — `entity.TenantId == _tenantContext.TenantId`
2. **API middleware** — tenant context + membership check on every protected request
3. **Supabase Storage paths** — `tenants/{tenantId}/...`; writes via service role key only

Never trust `TenantId` from request body/query. Always resolve tenant from the authenticated user's JWT.

### BFF pattern (mandatory)

Browser → Nuxt server routes (`/api/**` proxy) → ASP.NET API on Render.
The browser **never** calls Render or Supabase directly.

**Only exception:** public gallery `<img>` tags may use Supabase Storage CDN URLs (read-only).

```
Browser → {any-host}/api/...   (Nuxt server route)
        → api.onlineportfolio.com.br/api/v1/...   (Render)
```

### Host-based routing — three surfaces

The same Nuxt deployment serves three surfaces, resolved in `middleware/resolve-host.global.ts` via composable `useRequestSurface()`:

| Host | Surface | Layout | Components |
|---|---|---|---|
| `app.{platform}` | admin | `layouts/app.vue` | `components/app/` — standardized, no customization |
| `{platform}` / `www.` | platform | `layouts/platform.vue` | `components/platform/` |
| `{slug}.{platform}` or custom domain | tenant public | `layouts/tenant.vue` | `components/public/tenants/{slug}/` |

**Strict rule:** `components/app/` must not import from `components/public/tenants/`.

Per-tenant customization: component folder `components/public/tenants/{slug}/` + CSS theme `assets/css/themes/{slug}.css`. Discovered at runtime via `import.meta.glob` (`tenantComponentResolver.ts`). No manual registry needed — just add the folder.

### Auth

Full ASP.NET Identity + JWT on the API. No Supabase Auth anywhere.

- Login only at `app.onlineportfolio.com.br/login` — not on tenant subdomains
- JWT issued and validated by the API (`Jwt__Secret` on Render)
- Roles: `PlatformAdmin` (platform-wide, `TenantId = null`), `Owner` / `Editor` (tenant-scoped)
- In v1, only `PlatformAdmin` invites users; owners cannot self-serve

### API design

REST, URL versioned `/api/v1/...`, OpenAPI/Swagger. Endpoints:
- **Public:** `GET /api/v1/tenants/{slug}/...` — published content only
- **Admin:** authenticated by JWT role claims + tenant membership check
- **Platform:** `[Authorize(Roles = "PlatformAdmin")]`

### Database

- **PKs:** `uuid` / `Guid` on all domain entities; public tenant URLs use `slug`
- **Columns:** snake_case in Postgres
- **Migrations:** `dotnet ef database update` locally; CI on `main` (session pooler `:5432`)
- **Never** run `Migrate()` on API startup in production

### CI/CD

Four separate workflows (never combine into one):

| Workflow | Trigger | What runs |
|---|---|---|
| `ci-backend.yml` | PR + push `backend/**` | `dotnet test` + coverage comment |
| `ci-frontend.yml` | PR + push `frontend/**` | lint + `test:coverage` + coverage comment |
| `deploy-backend.yml` | push `main` `backend/**` | test → EF migrate → gates Render |
| `deploy-frontend.yml` | push `main` `frontend/**` | lint/test → `vercel deploy --prebuilt --prod` |

Production only — no deployed staging environment (ADR-015). Dev = Docker Compose; PR = Vercel preview.

---

## Language rules

| Layer | Language |
|---|---|
| Code, comments, logs, tests, commits, docs | **English** |
| Vue `<template>` user-visible text, API `message` fields for users, transactional email bodies | **pt-BR** |

In `.vue` files: English in `<script>`/`<script setup>`, pt-BR in `<template>`. Never mirror template copy into script completions.

Full guide: `docs/LANGUAGE.md`

---

## Security (non-negotiable)

- Every protected endpoint: `[Authorize(Roles)]` + tenant membership check
- Never trust `TenantId` from request body; always resolve from JWT user row
- No IDOR: `resource.TenantId == user.TenantId` before every response
- Secrets (`Jwt__Secret`, Supabase service role, Resend key) — Render env only, never in git or `NUXT_PUBLIC_*`
- Public API returns only `IsPublished == true` content
- Rate-limit public write endpoints (contact, auth)

---

## Git commits

Conventional Commits format: `<type>(<scope>): <description>`

Types: `feat` · `fix` · `docs` · `style` · `refactor` · `perf` · `test` · `build` · `ci` · `chore` · `revert`

Scopes: `frontend` · `backend` · `docs` · `db` · `infra` · `docker` · `deps`

**Do not** run `git commit`, `git push`, or `git merge` unless the user explicitly asks.

Full guide: `docs/CONVENTIONAL_COMMITS.md`

---

## Before merging a PR

When an API contract, schema, auth, or env var changes, both sides of the stack must be updated in the same PR:

| Changed | Backend | Frontend |
|---|---|---|
| Endpoint / DTO | Controller, service, `[Authorize]` | `server/api/**` proxy, composables, TS types |
| EF schema | Migration + seed if needed | API consumers, forms |
| Auth / JWT | Identity, policies | Proxy headers/cookies, login flow |
| Env var | `appsettings` + Render | `runtimeConfig`, Vercel, `.env.example` |

Also update `docs/DATABASE.md` on schema changes and `frontend/.env.example` on new Nuxt env vars.

---

## Never do

- Supabase Auth or `@nuxtjs/supabase` in the frontend
- `users.role` column (use Identity roles in `AspNetRoles`)
- Login page on tenant subdomains
- Direct browser → Supabase Storage uploads
- `Migrate()` on API startup in prod
- Secrets in git or `NUXT_PUBLIC_*` env vars
- Single combined `ci.yml` for both stacks
- Trust client-supplied `tenantId` for authorization
- Import motion libraries (`GSAP`, `Lenis`, etc.) inside `components/app/`
