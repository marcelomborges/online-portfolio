# Comandos locais — online-portfolio

Referência rápida para desenvolvimento.

**Convenção de paths:** todos os caminhos são **relativos à raiz do repositório** (pasta que contém `docker-compose.yml`, `backend/`, `frontend/`, `scripts/`). Em cada seção, **Pasta** indica de onde rodar os comandos daquele bloco.

**Pré-requisitos:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Compose) · [.NET 10 SDK](https://dotnet.microsoft.com/download) · [Node.js LTS](https://nodejs.org/) (frontend nativo) · `dotnet-ef` (migrations)

---

## URLs locais

| Serviço | URL |
|---|---|
| Frontend (Nuxt) | http://localhost:3000 |
| API + Swagger | http://localhost:8080/swagger |
| Health | http://localhost:8080/health |
| Postgres | `localhost:5432` — user `portfolio`, db `portfolio_dev`, senha `portfolio_dev` |

---

## Docker Compose (stack completa)

**Pasta:** raiz do repo

```cmd
docker compose up --build
```

Background:

```cmd
docker compose up --build -d
```

Parar (mantém dados do Postgres):

```cmd
docker compose down
```

Rebuild sem apagar banco (`down` + `up --build` **não** exige migration de novo):

```cmd
docker compose down
docker compose up --build
```

Reset total (apaga volume + re-roda `scripts/seed-dev.sql` na 1ª subida do `db`):

```cmd
docker compose down -v
docker compose up --build
```

Depois de `down -v`, reaplique migrations EF — ver [Migrations](#ef-core-migrations).

Só Postgres (API/frontend nativos):

```cmd
docker compose up -d db
```

Logs:

```cmd
docker compose logs -f api
docker compose logs -f db
```

Status:

```cmd
docker compose ps
```

---

## Backend — nativo (sem Docker na API)

**Pasta:** `backend/OnlinePortfolio.Api/`

Primeira vez:

```cmd
cd backend\OnlinePortfolio.Api
copy .env.example .env
dotnet restore
```

Postgres precisa estar acessível (na raiz do repo: `docker compose up -d db`).

```cmd
dotnet run --launch-profile http
```

Abre http://localhost:8080/swagger (perfil `http` no `launchSettings.json`).

Build:

```cmd
dotnet build
```

---

## Backend — Visual Studio

**Arquivo:** `backend/OnlinePortfolio.Api.slnx` (abrir na IDE)

1. Perfil **`http`** — API nativa + Swagger
2. Perfil **`Docker (Development)`** — API em container (`Dockerfile.dev`); exige Docker Desktop + workload Container Tools

---

## Frontend — nativo

**Pasta:** `frontend/`

Primeira vez:

```cmd
cd frontend
copy .env.example .env
npm install
```

Dev server:

```cmd
npm run dev
```

→ http://localhost:3000

Build (valida preset Vercel):

```cmd
npm run build
```

Env: `NUXT_PUBLIC_API_BASE`, `NUXT_PUBLIC_PLATFORM_HOST`, `NUXT_PUBLIC_APP_HOST`, `NUXT_API_INTERNAL_BASE` — ver `frontend/.env.example`.

**Simular hosts em localhost** — copie `frontend/.env.example` → `.env`. Comentários detalhados de cada `DEV_SURFACE` estão no `.env.example`.

**Pasta:** `frontend/`

```cmd
set NUXT_PUBLIC_DEV_SURFACE=tenant
set NUXT_PUBLIC_DEV_TENANT_SLUG=ana
npm run dev
```

| `NUXT_PUBLIC_DEV_SURFACE` | Simula | Preview |
|---|---|---|
| `dev` (default) | — | Skeleton DEV-005 (**dark**) |
| `tenant` | `{slug}.onlineportfolio.com.br` | Site do slug — **ana** ou **joao** (tema artista) |
| `platform` | `onlineportfolio.com.br` | Marketing (**dark**) |
| `app` | `app.onlineportfolio.com.br` | Admin/login (**dark**) |

Detalhes: [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md) · [ADR-016](./ARCHITECTURE.md#adr-016-três-superfícies-e-ui-por-tenant).

---

## EF Core migrations

**Pasta (migrations):** `backend/OnlinePortfolio.Api/`  
**Pasta (Docker / psql):** raiz do repo  
**Banco local:** Compose Postgres (`localhost:5432`).

| Connection string | Uso |
|---|---|
| `ConnectionStrings__Default` | API em runtime |
| `ConnectionStrings__Migration` | `dotnet ef` + CI — **direct `:5432`** (local ou Supabase direct; **não** pooler `:6543`) |

Config em `appsettings.Development.json` e `.env.example`.

### Ferramenta (uma vez)

Rode em qualquer pasta (instala global):

```cmd
dotnet tool install --global dotnet-ef
```

Atualizar: `dotnet tool update --global dotnet-ef`

### Postgres no ar

**Pasta:** raiz do repo

```cmd
docker compose up -d db
```

### Criar nova migration

**Pasta:** `backend/OnlinePortfolio.Api/`

```cmd
cd backend\OnlinePortfolio.Api
```

**CMD:**

```cmd
set ASPNETCORE_ENVIRONMENT=Development
dotnet ef migrations add NomeDaMigration
```

**PowerShell:**

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef migrations add NomeDaMigration
```

### Aplicar migrations pendentes

**Pasta:** `backend/OnlinePortfolio.Api/`

```cmd
dotnet ef database update
```

Com connection explícita (CMD):

```cmd
set ConnectionStrings__Migration=Host=localhost;Port=5432;Database=portfolio_dev;Username=portfolio;Password=portfolio_dev
dotnet ef database update --connection %ConnectionStrings__Migration%
```

### Verificar migrations aplicadas

**Pasta:** raiz do repo

```cmd
docker compose exec db psql -U portfolio -d portfolio_dev -c "SELECT migration_id FROM \"__EFMigrationsHistory\";"
```

Listar tabelas:

```cmd
docker compose exec db psql -U portfolio -d portfolio_dev -c "\dt"
```

### Outros comandos úteis

**Pasta:** `backend/OnlinePortfolio.Api/`

```cmd
dotnet ef migrations list
dotnet ef migrations remove
```

`remove` — só a **última** migration, e **antes** de ir para produção.

Rollback local para migration anterior:

```cmd
dotnet ef database update NomeDaMigrationAnterior
```

### Quando rodar `database update` de novo?

| Situação | Precisa? |
|---|---|
| `docker compose up` / `down` + `up --build` | **Não** |
| `docker compose down -v` (volume apagado) | **Sim** |
| Nova migration no código (`migrations add …`) | **Sim** |
| Deploy prod | CI (`deploy-backend.yml`) no Supabase — não na startup da API |

A API **não** executa `Migrate()` na startup em produção.

---

## Postgres — consultas úteis

**Pasta:** raiz do repo

Seed DEV-002 (tenants `ana` / `joao` — se `scripts/seed-dev.sql` rodou):

```cmd
docker compose exec db psql -U portfolio -d portfolio_dev -c "SELECT slug, display_name FROM tenants ORDER BY slug;"
```

Shell interativo:

```cmd
docker compose exec db psql -U portfolio -d portfolio_dev
```

---

## Testes (backend)

**Pasta:** `backend/`

```cmd
cd backend
dotnet test OnlinePortfolio.Api.slnx
```

Com cobertura (arquivo Cobertura em `backend/TestResults/` — **gitignored**):

```cmd
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults
```

**Relatório HTML visual** (testes + Coverlet + ReportGenerator; abre o browser no Windows). Cada execução **apaga** `backend/TestResults/` e gera tudo de novo — não acumula runs antigos.

**Pasta:** raiz do repo

```cmd
powershell -ExecutionPolicy Bypass -File scripts\coverage-backend.ps1
```

Linux/macOS:

```cmd
bash scripts/coverage-backend.sh
```

Saída: `backend/TestResults/CoverageReport/index.html` — pasta `TestResults/` está no `.gitignore`.

Stack: xUnit · Moq · FluentAssertions · Coverlet — [ARCHITECTURE §23](./ARCHITECTURE.md#23-testing).

---

## Build imagem de produção (API)

**Pasta:** raiz do repo

```cmd
docker build -t online-portfolio-api -f backend\OnlinePortfolio.Api\Dockerfile backend\OnlinePortfolio.Api
```

---

## Documentação relacionada

| Doc | Conteúdo |
|---|---|
| [README.md](../README.md) | Visão geral |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | Decisões de sistema |
| [DATABASE.md](./DATABASE.md) | Schema + PK uuid |
| [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) | Supabase, Render, Vercel |
