# online-portfolio

Multi-tenant SaaS platform for artist portfolios (Nuxt 3.21 + Vue 3.5 + .NET 10 LTS).

**Domain:** `onlineportfolio.com.br` — **prod live** (`onlineportfolio.com.br`, `app.`, `api.`, `{slug}.`)

**Language:** English for code and docs; **pt-BR** for product UI only — [docs/LANGUAGE.md](docs/LANGUAGE.md).

## Repository layout

```text
online-portfolio/
├── docker-compose.yml
├── frontend/                    # Nuxt 3 → Vercel
├── backend/
│   ├── OnlinePortfolio.Api.slnx
│   ├── OnlinePortfolio.Api/     # ASP.NET Core 10 → Render
│   └── OnlinePortfolio.Api.Tests/  # xUnit (future)
├── docs/
└── scripts/                     # seed-dev.sql, Linear export
```

## Local development

### Docker Compose (recommended — DEV-002)

**Run from:** repository root (`c:\Projects\online-portfolio` or `./online-portfolio`).

**Prerequisite:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) running.

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| **Frontend (Nuxt + HMR)** | http://localhost:3000 |
| **API (.NET watch)** | http://localhost:8080 |
| **Postgres** | `localhost:5432` — user `portfolio`, db `portfolio_dev`, password `portfolio_dev` |

Stop: `Ctrl+C` or, in another terminal at the same path: `docker compose down`.

Full reset (drops Postgres volume; schema via EF migrations):

```bash
docker compose down -v
docker compose up -d db
cd backend && dotnet ef database update --project OnlinePortfolio.Api
```

Verify tenants (after DEV-152 seed):

```bash
docker compose exec db psql -U portfolio -d portfolio_dev -c "SELECT slug, display_name FROM tenants;"
```

### Without Docker (single service)

```bash
# Frontend
cd frontend && cp .env.example .env && npm install && npm run dev
```

→ http://localhost:3000 · custom error at `/__nuxt_error` or unknown routes (404)

```bash
# Backend (Postgres via Docker Compose at repo root: docker compose up -d db)
cd backend/OnlinePortfolio.Api && dotnet run
```

Full local stack = Docker Compose above.

### EF Core migrations (DEV-004+)

See **[docs/DEV_COMMANDS.md](./docs/DEV_COMMANDS.md)** — Docker vs native commands, migrations, when to re-run `database update`.

### Tests (backend)

```bash
cd backend
dotnet test OnlinePortfolio.Api.slnx
```

Stack: **xUnit** · **Moq** · **FluentAssertions** · **Coverlet** · `dotnet test` — details in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Documentation

| Doc | Description |
|---|---|
| [LANGUAGE.md](docs/LANGUAGE.md) | **English vs pt-BR** — code/docs vs product UI |
| [DEV_COMMANDS.md](docs/DEV_COMMANDS.md) | Local commands (Docker, native, migrations) |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System design and decisions (ADR-015–ADR-017, …) |
| [FRONTEND_COMPONENTS.md](docs/FRONTEND_COMPONENTS.md) | Nuxt surfaces, tenant UI, composables |
| [DATABASE.md](docs/DATABASE.md) | PostgreSQL schema |
| [BACKLOG.md](docs/BACKLOG.md) | Development tasks (Linear) |
| [ADR-017-resend-transactional-email.md](docs/ADR-017-resend-transactional-email.md) | Email provider decision (Resend) |
| [EXTERNAL_PROVIDERS.md](docs/EXTERNAL_PROVIDERS.md) | Third-party setup |
| [AGENT_GUIDE.md](docs/AGENT_GUIDE.md) | AI agent rules |

## Commits

[Conventional Commits](docs/CONVENTIONAL_COMMITS.md) — e.g. `feat(infra): add monorepo scaffold (DEV-001)`.
