# Local commands — online-portfolio

Quick reference for development.

**Path convention:** all paths are **relative to the repository root** (folder containing `docker-compose.yml`, `backend/`, `frontend/`, `scripts/`). In each section, **Folder** indicates where to run that block's commands.

**Prerequisites:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Compose) · [.NET 10 SDK](https://dotnet.microsoft.com/download) · [Node.js LTS](https://nodejs.org/) (native frontend) · `dotnet-ef` (migrations)

---

## Local URLs

| Service | URL |
|---|---|
| Frontend (Nuxt) | http://localhost:3000 |
| API + Swagger | http://localhost:8080/swagger |
| Health | http://localhost:8080/health |
| Postgres | `localhost:5432` — user `portfolio`, db `portfolio_dev`, password `portfolio_dev` |

---

## Docker Compose (full stack)

**Folder:** repo root

```cmd
docker compose up --build
```

Background:

```cmd
docker compose up --build -d
```

Stop (keeps Postgres data):

```cmd
docker compose down
```

Rebuild without wiping database (`down` + `up --build` does **not** require running migrations again):

```cmd
docker compose down
docker compose up --build
```

Full reset (deletes Postgres volume; reapply EF migrations afterward):

```cmd
docker compose down -v
docker compose up -d db
cd backend
dotnet ef database update --project OnlinePortfolio.Api
```

Postgres only (native API/frontend):

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

## Backend — native (no Docker for API)

**Folder:** `backend/OnlinePortfolio.Api/`

First time:

```cmd
cd backend\OnlinePortfolio.Api
dotnet restore
```

Local config: `appsettings.Development.json` (connection strings, Resend, etc.).

Postgres must be reachable (from repo root: `docker compose up -d db`).

```cmd
dotnet run --launch-profile http
```

Opens http://localhost:8080/swagger (`http` profile in `launchSettings.json`).

Build:

```cmd
dotnet build
```

---

## Backend — Visual Studio

**Folder:** `backend/`  
**Open:** `OnlinePortfolio.Api.slnx`

1. **Startup project:** `OnlinePortfolio.Api` (do not use the test project as startup).
2. **`http` profile** — native API + Swagger at http://localhost:8080/swagger
3. **`Docker (Development)` profile** — API in container; requires Docker Desktop + *Container Tools* workload

**Tests:** use CLI (`dotnet test` in `backend/`) or `scripts/coverage-backend.ps1` — see [Tests (backend)](#tests-backend).

---

## Frontend — native

**Folder:** `frontend/`

First time:

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

Build (validates Vercel preset):

```cmd
npm run build
```

Env: `NUXT_PUBLIC_API_BASE`, `NUXT_PUBLIC_PLATFORM_HOST`, `NUXT_PUBLIC_APP_HOST`, `NUXT_API_INTERNAL_BASE` — see `frontend/.env.example`.

**Simulate hosts on localhost** — copy `frontend/.env.example` → `.env`. Detailed comments for each `DEV_SURFACE` are in `.env.example`.

**Folder:** `frontend/`

```cmd
set NUXT_PUBLIC_DEV_SURFACE=tenant
set NUXT_PUBLIC_DEV_TENANT_SLUG=ana
npm run dev
```

| `NUXT_PUBLIC_DEV_SURFACE` | Simulates | Preview |
|---|---|---|
| `dev` (default) | — | DEV-005 skeleton (**dark**) |
| `tenant` | `{slug}.onlineportfolio.com.br` | Slug site — **ana** or **joao** (artist theme) |
| `platform` | `onlineportfolio.com.br` | Marketing (**dark**) |
| `app` | `app.onlineportfolio.com.br` | Admin/login (**dark**) |

Details: [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md) · [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

---

## EF Core migrations

**Folder (migrations):** `backend/OnlinePortfolio.Api/`  
**Folder (Docker / psql):** repo root  
**Local database:** Compose Postgres (`localhost:5432`) — see `appsettings.Development.json`.

### Connection strings

| Environment | `Default` (runtime) | `Migration` (`dotnet ef`) |
|---|---|---|
| **Local (normal)** | `localhost:5432` | `localhost:5432` |
| **Prod API (Render)** | Transaction pooler `:6543` | *(not configured)* |
| **Prod migrate (CI)** | — | Session pooler `:5432` → GitHub secret |

- **Local:** development and `dotnet ef` always use **localhost** (Docker Compose).
- **CI:** `SUPABASE_MIGRATION_CONNECTION_STRING` = **Session pooler** (`aws-*-*.pooler.supabase.com:5432`, user `postgres.[ref]`).
- **Direct** (`db.*.supabase.co`): **do not** use in this project. Optional only for manual tools (pg_dump, GUI).
- **Never** use Transaction pooler (`:6543`) for `dotnet ef database update`.

Templates in `appsettings.json` (prod, no password); local override in `appsettings.Development.json`.

**DEV-008b (future):** remote Supabase dev — connection string flow will be revisited; until then, local = Compose.

Detailed config: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) section 6.2 · [docs/ARCHITECTURE.md](./ARCHITECTURE.md) section 7.

### Tool (one-time)

Run from any folder (global install):

```cmd
dotnet tool install --global dotnet-ef
```

Update: `dotnet tool update --global dotnet-ef`

### Start Postgres

**Folder:** repo root

```cmd
docker compose up -d db
```

### Create new migration

**Folder:** `backend/OnlinePortfolio.Api/`

```cmd
cd backend\OnlinePortfolio.Api
```

**CMD:**

```cmd
set ASPNETCORE_ENVIRONMENT=Development
dotnet ef migrations add MigrationName
```

**PowerShell:**

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef migrations add MigrationName
```

### Apply pending migrations

**Folder:** `backend/OnlinePortfolio.Api/`

```cmd
dotnet ef database update
```

With explicit connection (CMD):

```cmd
set ConnectionStrings__Migration=Host=localhost;Port=5432;Database=portfolio_dev;Username=portfolio;Password=portfolio_dev
dotnet ef database update --connection %ConnectionStrings__Migration%
```

### Verify applied migrations

**Folder:** repo root

```cmd
docker compose exec db psql -U portfolio -d portfolio_dev -c "SELECT migration_id FROM \"__EFMigrationsHistory\";"
```

List tables:

```cmd
docker compose exec db psql -U portfolio -d portfolio_dev -c "\dt"
```

### Other useful commands

**Folder:** `backend/OnlinePortfolio.Api/`

```cmd
dotnet ef migrations list
dotnet ef migrations remove
```

`remove` — only the **last** migration, and **before** going to production.

Local rollback to previous migration:

```cmd
dotnet ef database update PreviousMigrationName
```

### When to run `database update` again?

| Situation | Required? |
|---|---|
| `docker compose up` / `down` + `up --build` | **No** |
| `docker compose down -v` (volume deleted) | **Yes** |
| New migration in code (`migrations add …`) | **Yes** |
| Prod deploy | CI (`deploy-backend.yml`) on Supabase — not on API startup |

The API does **not** run `Migrate()` on startup in production.

---

## Frontend deploy (prod) — DEV-007b

**Workflow:** `.github/workflows/deploy-frontend.yml` — push to `main` with changes in `frontend/**`.

Pipeline: lint/test → `vercel pull` + `vercel env pull` → `npm run build` (Nitro generates `frontend/.vercel/output`) → copy to `.vercel/output` at root → `vercel deploy --prebuilt --prod`. `vercel *` commands run at **monorepo root** (not in `frontend/`); Root Directory `frontend` in dashboard avoids duplicate `frontend/frontend` path. Does not use `vercel build` (avoids `nuxt: not found` outside `node_modules/.bin`).

**GitHub Secrets** (repo → Settings → Secrets and variables → Actions):

| Secret | Value |
|--------|--------|
| `VERCEL_TOKEN` | [vercel.com/account/tokens](https://vercel.com/account/tokens) |
| `VERCEL_ORG_ID` | Vercel → project → Settings → General → Team ID |
| `VERCEL_PROJECT_ID` | Vercel → project → Settings → General → Project ID |

Vercel dashboard: **Only build pre-production** ON (prod only via Actions). PR previews continue via Git integration.

**Manual redeploy:** see [Manual redeploy (production)](#manual-redeploy-production) below.

---

## Backend deploy (prod) — DEV-007

**Workflow:** `.github/workflows/deploy-backend.yml` — push to `main` with changes in `backend/**`, `docs/DATABASE.md`, or the workflow itself.

Pipeline: `dotnet test` → `dotnet ef database update` (secret `SUPABASE_MIGRATION_CONNECTION_STRING`). Green **Backend Deploy** check enables autodeploy on Render (**After CI Checks Pass**).

**Manual redeploy:** see section below. The workflow does **not** trigger deploy on Render — test + migrate only. To republish the API: Render dashboard → **Manual Deploy**, or deploy hook if configured.

---

## Manual redeploy (production)

Both deploy pipelines accept **`workflow_dispatch`** — **Run workflow** button in GitHub Actions, no empty commit required.

**When to use:**

| Situation | Workflow |
|---|---|
| Changed env on **Vercel** (`NUXT_*`) | **Frontend Deploy** |
| Want to revalidate test + migrate on **Supabase** | **Backend Deploy** |
| Vercel redeploy cancelled by *Ignored Build Step* | **Frontend Deploy** (do not use dashboard Redeploy) |

**Steps:**

1. GitHub → **Actions**
2. Choose **Frontend Deploy** or **Backend Deploy**
3. **Run workflow** → branch `main` → (optional) reason → **Run workflow**
4. Wait for green job

**Frontend Deploy** runs full production deploy (`vercel deploy --prebuilt --prod`).

**Backend Deploy** runs tests + migrations only. For API redeploy on Render, use the Render dashboard afterward (if needed).

---

## Resend — smoke test (DEV-014)

Validates account + API key. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) section 8.5 (onboarding) and **8.6** (verified domain — prod).

**Folder:** any (temporary console) or future `backend/OnlinePortfolio.Api/`

```bash
dotnet add package Resend
```

Replace `re_xxxxxxxxx` with the real key (password manager — **never** commit):

```csharp
using Resend;

IResend resend = ResendClient.Create("re_xxxxxxxxx");

var resp = await resend.EmailSendAsync(new EmailMessage()
{
    From = "onboarding@resend.dev",
    To = "your-email@example.com",
    Subject = "Hello World",
    HtmlBody = "<p>Congrats on sending your <strong>first email</strong>!</p>",
});
```

**Prod smoke test (verified domain):** `mail@onlineportfolio.com.br` with `Reply-To` — see [EXTERNAL_PROVIDERS](./EXTERNAL_PROVIDERS.md) section 8.6.

Before domain verification, use `From = "onboarding@resend.dev"` (section 8.5).

**HTTP example (verified domain):**

```json
{
  "from": "Online Portfolio <mail@onlineportfolio.com.br>",
  "to": ["your-email@example.com"],
  "reply_to": "your-email@example.com",
  "subject": "hello world",
  "html": "<p>it works!</p>"
}
```

`POST https://api.resend.com/emails` · `Authorization: Bearer re_...`

**Render (prod):** `Resend__ApiKey`, `Resend__FromEmail`, `Resend__FromName` — see [EXTERNAL_PROVIDERS](./EXTERNAL_PROVIDERS.md) section 8.2.

---

## Postgres — useful queries

**Folder:** repo root

DEV-002 seed (tenants `ana` / `joao` — after DEV-152 / EF seed):

```cmd
docker compose exec db psql -U portfolio -d portfolio_dev -c "SELECT slug, display_name FROM tenants ORDER BY slug;"
```

Interactive shell:

```cmd
docker compose exec db psql -U portfolio -d portfolio_dev
```

---

## Tests (backend)

**Folder:** `backend/`

```cmd
cd backend
dotnet test OnlinePortfolio.Api.slnx
```

With coverage (Cobertura file in `backend/TestResults/` — **gitignored**):

```cmd
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults
```

**Visual HTML report** (tests + Coverlet + ReportGenerator; opens browser on Windows). Each run **deletes** `backend/TestResults/` and regenerates everything — does not accumulate old runs.

**Folder:** repo root

```cmd
powershell -ExecutionPolicy Bypass -File scripts\coverage-backend.ps1
```

Linux/macOS:

```cmd
bash scripts/coverage-backend.sh
```

Output: `backend/TestResults/CoverageReport/index.html` — `TestResults/` folder is in `.gitignore`.

Stack: xUnit · Moq · FluentAssertions · Coverlet — [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

---

## Production image build (API)

**Folder:** repo root

```cmd
docker build -t online-portfolio-api -f backend\OnlinePortfolio.Api\Dockerfile backend\OnlinePortfolio.Api
```

---

## Related documentation

| Doc | Content |
|---|---|
| [README.md](../README.md) | Overview |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | System decisions |
| [DATABASE.md](./DATABASE.md) | Schema + uuid PK |
| [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) | Supabase, Render, Vercel |
