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

Reset total (apaga volume Postgres; reaplique migrations EF em seguida):

```cmd
docker compose down -v
docker compose up -d db
cd backend
dotnet ef database update --project OnlinePortfolio.Api
```

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
dotnet restore
```

Config local: `appsettings.Development.json` (connection strings, Resend, etc.).

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

**Pasta:** `backend/`  
**Abrir:** `OnlinePortfolio.Api.slnx`

1. **Startup project:** `OnlinePortfolio.Api` (não use o projeto de testes como startup).
2. Perfil **`http`** — API nativa + Swagger em http://localhost:8080/swagger
3. Perfil **`Docker (Development)`** — API em container; exige Docker Desktop + workload *Container Tools*

**Testes:** use CLI (`dotnet test` na pasta `backend/`) ou `scripts/coverage-backend.ps1` — ver [Testes (backend)](#testes-backend).

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

Detalhes: [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md) · [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

---

## EF Core migrations

**Pasta (migrations):** `backend/OnlinePortfolio.Api/`  
**Pasta (Docker / psql):** raiz do repo  
**Banco local:** Compose Postgres (`localhost:5432`) — ver `appsettings.Development.json`.

### Connection strings

| Ambiente | `Default` (runtime) | `Migration` (`dotnet ef`) |
|---|---|---|
| **Local (normal)** | `localhost:5432` | `localhost:5432` |
| **Prod API (Render)** | Transaction pooler `:6543` | *(não configurar)* |
| **Prod migrate (CI)** | — | Session pooler `:5432` → secret GitHub |

- **Local:** desenvolvimento e `dotnet ef` usam **sempre localhost** (Docker Compose).
- **CI:** `SUPABASE_MIGRATION_CONNECTION_STRING` = **Session pooler** (`aws-*-*.pooler.supabase.com:5432`, user `postgres.[ref]`).
- **Direct** (`db.*.supabase.co`): **não** usar no projeto. Opcional só para ferramentas manuais (pg_dump, GUI).
- **Nunca** usar Transaction pooler (`:6543`) para `dotnet ef database update`.

Templates em `appsettings.json` (prod, sem senha); override local em `appsettings.Development.json`.

**DEV-008b (futuro):** Supabase dev remoto — fluxo de strings será rediscutido; até lá, local = Compose.

Config detalhada: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) seção 6.2 · [docs/ARCHITECTURE.md](./ARCHITECTURE.md) seção 7.

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

## Deploy frontend (prod) — DEV-007b

**Workflow:** `.github/workflows/deploy-frontend.yml` — push `main` com mudança em `frontend/**`.

Pipeline: lint/test → `vercel pull` + `vercel env pull` → `npm run build` (Nitro gera `frontend/.vercel/output`) → copia para `.vercel/output` na raiz → `vercel deploy --prebuilt --prod`. Comandos `vercel *` rodam na **raiz do monorepo** (não em `frontend/`); Root Directory `frontend` no dashboard evita path duplicado `frontend/frontend`. Não usa `vercel build` (evita `nuxt: not found` fora do `node_modules/.bin`).

**GitHub Secrets** (repo → Settings → Secrets and variables → Actions):

| Secret | Valor |
|--------|--------|
| `VERCEL_TOKEN` | [vercel.com/account/tokens](https://vercel.com/account/tokens) |
| `VERCEL_ORG_ID` | Vercel → projeto → Settings → General → Team ID |
| `VERCEL_PROJECT_ID` | Vercel → projeto → Settings → General → Project ID |

Vercel dashboard: **Only build pre-production** ON (prod só via Actions). PR previews continuam pela integração Git.

**Redeploy manual:** ver seção [Redeploy manual (production)](#redeploy-manual-production) abaixo.

---

## Deploy backend (prod) — DEV-007

**Workflow:** `.github/workflows/deploy-backend.yml` — push `main` com mudança em `backend/**`, `docs/DATABASE.md` ou no próprio workflow.

Pipeline: `dotnet test` → `dotnet ef database update` (secret `SUPABASE_MIGRATION_CONNECTION_STRING`). O check **Backend Deploy** green libera o autodeploy no Render (**After CI Checks Pass**).

**Redeploy manual:** ver seção abaixo. O workflow **não** dispara deploy no Render — só test + migrate. Para republicar a API: Render dashboard → **Manual Deploy**, ou deploy hook se configurado.

---

## Redeploy manual (production)

Ambos os deploy pipelines aceitam **`workflow_dispatch`** — botão **Run workflow** no GitHub Actions, sem commit vazio.

**Quando usar:**

| Situação | Workflow |
|---|---|
| Mudou env na **Vercel** (`NUXT_*`) | **Frontend Deploy** |
| Quer revalidar test + migrate no **Supabase** | **Backend Deploy** |
| Redeploy Vercel cancelado por *Ignored Build Step* | **Frontend Deploy** (não use Redeploy do dashboard) |

**Passos:**

1. GitHub → **Actions**
2. Escolher **Frontend Deploy** ou **Backend Deploy**
3. **Run workflow** → branch `main` → (opcional) motivo → **Run workflow**
4. Aguardar job verde

**Frontend Deploy** roda deploy completo em produção (`vercel deploy --prebuilt --prod`).

**Backend Deploy** roda só testes + migrations. Para redeploy da API no Render, use o dashboard Render depois (se necessário).

---

## Resend — smoke test (DEV-014)

Valida conta + API key. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) seção 8.5 (onboarding) e **8.6** (domínio verificado — prod).

**Pasta:** qualquer (console temporário) ou futuro `backend/OnlinePortfolio.Api/`

```bash
dotnet add package Resend
```

Substituir `re_xxxxxxxxx` pela key real (password manager — **nunca** commitar):

```csharp
using Resend;

IResend resend = ResendClient.Create("re_xxxxxxxxx");

var resp = await resend.EmailSendAsync(new EmailMessage()
{
    From = "onboarding@resend.dev",
    To = "seu-email@exemplo.com",
    Subject = "Hello World",
    HtmlBody = "<p>Congrats on sending your <strong>first email</strong>!</p>",
});
```

**Smoke test prod (domínio verificado):** `mail@onlineportfolio.com.br` com `Reply-To` — ver [EXTERNAL_PROVIDERS](./EXTERNAL_PROVIDERS.md) seção 8.6.

Antes do domínio verificado, usar `From = "onboarding@resend.dev"` (seção 8.5).

**Exemplo HTTP (domínio verificado):**

```json
{
  "from": "Online Portfolio <mail@onlineportfolio.com.br>",
  "to": ["seu-email@exemplo.com"],
  "reply_to": "seu-email@exemplo.com",
  "subject": "hello world",
  "html": "<p>it works!</p>"
}
```

`POST https://api.resend.com/emails` · `Authorization: Bearer re_...`

**Render (prod):** `Resend__ApiKey`, `Resend__FromEmail`, `Resend__FromName` — ver [EXTERNAL_PROVIDERS](./EXTERNAL_PROVIDERS.md) seção 8.2.

---

## Postgres — consultas úteis

**Pasta:** raiz do repo

Seed DEV-002 (tenants `ana` / `joao` — após DEV-152 / seed EF):

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

Stack: xUnit · Moq · FluentAssertions · Coverlet — [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

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
