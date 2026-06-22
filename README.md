# online-portfolio

Multi-tenant SaaS platform for artist portfolios (Nuxt 3.21 + Vue 3.5 + .NET 10 LTS).

**Domain:** `onlineportfolio.com.br`

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

### Docker Compose (recomendado — DEV-002)

**Onde rodar:** raiz do repo (`c:\Projects\online-portfolio` ou `./online-portfolio`).

**Pré-requisito:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) em execução.

```bash
docker compose up --build
```

| Serviço | URL |
|---|---|
| **Frontend (Nuxt + HMR)** | http://localhost:3000 |
| **API (.NET watch)** | http://localhost:8080 |
| **Postgres** | `localhost:5432` — user `portfolio`, db `portfolio_dev`, senha `portfolio_dev` |

Parar: `Ctrl+C` ou, noutro terminal na mesma pasta: `docker compose down`.

Reset completo (apaga dados + re-roda seed dos 2 tenants):

```bash
docker compose down -v
docker compose up --build
```

Verificar seed:

```bash
docker compose exec db psql -U portfolio -d portfolio_dev -c "SELECT slug, display_name FROM tenants;"
```

### Sem Docker (só um serviço)

```bash
# Frontend
cd frontend && cp .env.example .env && npm install && npm run dev
```

→ http://localhost:3000 · erro customizado em `/__nuxt_error` ou rota inexistente (404)

```bash
# Backend
cd backend/OnlinePortfolio.Api && cp .env.example .env && dotnet run
```

Stack completa local = Docker Compose acima.

### EF Core migrations (DEV-004+)

Ver **[docs/DEV_COMMANDS.md](./docs/DEV_COMMANDS.md#ef-core-migrations)** — comandos Docker vs nativo, migrations, quando reaplicar `database update`.

### Testes (backend)

```bash
cd backend
dotnet test OnlinePortfolio.Api.slnx
```

Stack: **xUnit** · **Moq** · **FluentAssertions** · **Coverlet** · `dotnet test` — detalhes em [ARCHITECTURE §23](docs/ARCHITECTURE.md#23-testing).

## Documentation

| Doc | Description |
|---|---|
| [DEV_COMMANDS.md](docs/DEV_COMMANDS.md) | **Comandos locais** (Docker, nativo, migrations) |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System design and decisions (ADR-015, ADR-016, …) |
| [FRONTEND_COMPONENTS.md](docs/FRONTEND_COMPONENTS.md) | Nuxt surfaces, tenant UI, composables |
| [DATABASE.md](docs/DATABASE.md) | PostgreSQL schema |
| [BACKLOG.md](docs/BACKLOG.md) | Development tasks (Linear) |
| [EXTERNAL_PROVIDERS.md](docs/EXTERNAL_PROVIDERS.md) | Third-party setup |
| [AGENT_GUIDE.md](docs/AGENT_GUIDE.md) | AI agent rules |

## Commits

[Conventional Commits](docs/CONVENTIONAL_COMMITS.md) — e.g. `feat(infra): add monorepo scaffold (DEV-001)`.
