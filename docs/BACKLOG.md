# Development Backlog — Online Portfolio Platform

Detailed activity list for building the multi-tenant artist portfolio SaaS.

**Execução:** issues no [Linear](https://linear.app) (import feito). **Documentação:** este arquivo permanece fonte de verdade para agentes e PRs — mantenha `DEV-xxx` nos commits e descrições.

**Related:** [docs/ARCHITECTURE.md](./ARCHITECTURE.md) · [docs/FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md) · [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) · [docs/DATABASE.md](./DATABASE.md)

**Frontend (superfícies + UI por tenant):** [docs/ARCHITECTURE.md](./ARCHITECTURE.md) — não exige ticket novo; escopo distribuído em DEV-005, DEV-104, DEV-105, DEV-109 e Epic 1.5 (DEV-155–158).

**Domínio da plataforma:** `onlineportfolio.com.br` — ✅ registrado (Registro.br)

**Mapa de domínios:** [docs/ARCHITECTURE.md](./ARCHITECTURE.md) — site público por tenant (`ana.`, `mark.`), admin padronizado em `app.`, BFF via Nuxt → API .NET.

---

## How to use with Linear

| BACKLOG.md | Linear |
|---|---|
| `## Epic` / Phase heading | **Project** |
| `### DEV-xxx` | **Issue** (título `DEV-xxx — …`) |
| **Observações** → `Area` | **Labels** (`backend`, `frontend`, `infra`, …) |
| **Observações** → `Priority` | **Priority** |
| **Observações** → `Depends on` | **Blocked by** |
| **Critérios de aceitação** | Checklist na descrição |

**Sincronizar:** ao concluir trabalho, marque Done no Linear e atualize checkboxes aqui quando fizer sentido para docs.

### Formato padrão de cada issue

Todas as issues (`DEV-`, `UT-`, `IT-`, `SEC-`) seguem **sempre** esta estrutura (4 blocos):

```markdown
### DEV-xxx — Título curto

**Descrição:**
Uma ou mais frases: o quê, por quê, links para runbooks/ADRs.

**Critérios de aceitação:**
- [ ] Item verificável 1
- [ ] Item verificável 2

**Observações:**
- **Phase:** 0
- **Area:** backend
- **Priority:** P1
- **Depends on:** DEV-001
- **Status:** ✅ Done

**Observações:**





(notas de fechamento, próximos passos, escopo fora do ticket, etc.)
```

| Bloco | Linear |
|---|---|
| `### DEV-xxx — …` | **Title** |
| **Descrição** | Corpo da issue (início) |
| **Critérios de aceitação** | Checklist na descrição |
| **Observações** | Metadata (`Phase`, `Area`, …) + notas; mapear para labels / blocked by |

**Re-export CSV (opcional):** `python scripts/generate-linear-import.py` → `docs/BACKLOG_LINEAR.csv` (gitignored). Labels no CSV usam `", "` entre valores — exigido pelo importador Linear.

### Priority legend

| Priority | Meaning |
|---|---|
| **P0** | Blocker — must be done first |
| **P1** | Core MVP for current phase |
| **P2** | Important but can follow MVP |
| **P3** | Nice to have / later |

### Area labels

`infra` · `backend` · `frontend` · `database` · `devops` · `docs` · `unit-test` · `integration-test` · `security`

---

## Epic 0 — Foundation & tooling

### Provider setup index

Atividades de **conta e configuração** alinhadas à [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md). Código local (DEV-001–005) pode correr em paralelo às contas (DEV-012–014).

| Ordem | Provider | Issue | Quando |
|---|---|---|---|
| 1 | Registro.br | [DEV-000](#dev-000--register-domain-) | ✅ domínio comprado |
| 2 | GitHub | [DEV-012](#dev-012--github-repository--platform-integrations) | ✅ Done |
| 3 | Linear | [DEV-013](#dev-013--linear-workspace) | ✅ Done |
| 4 | Supabase | [DEV-008](#dev-008--supabase-production-project) · [DEV-008b](#dev-008b--supabase-dev-project) | ✅ prod (`online-portfolio-db-prod`); dev opcional |
| 5 | Render | [DEV-009](#dev-009--render-api-deployment) | ✅ Done — API prod `*.onrender.com` |
| 6 | Resend | [DEV-014](#dev-014--resend-account--api-key) → [DEV-107](#dev-107--contact-form--resend) | ✅ conta + Render env; código Epic 1 |
| 7 | Vercel | [DEV-010](#dev-010--vercel-frontend-deployment) | ✅ Done — `online-portfolio-web` |
| 8 | DNS + email DNS | [DEV-011](#dev-011--dns--https-production) | Após Render + Vercel |
| 9 | GitHub Actions | [DEV-006](#dev-006--github-actions-ci-pr--workflows-separados) ✅ · [DEV-007](#dev-007--github-actions-deploy-pipeline-backend) ✅ · [DEV-007b](#dev-007b--github-actions-deploy-pipeline-frontend) ✅ | ✅ CI + deploy backend + frontend |
| — | Google Workspace | [DEV-404](#dev-404--google-workspace-operator-inbox) | Opcional, pós-lançamento |
| — | Stripe / billing | [DEV-403](#dev-403--billing--subscriptions-optional--skip-until-charging) | **Opcional** — skip no v1 |

---

### DEV-000 — Register domain ✅

**Descrição:**
Register `onlineportfolio.com.br` at Registro.br. DNS configuration deferred until [DEV-011](#dev-011--dns--https-production). Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Critérios de aceitação:**
- [x] Domain status **Ativo** in Registro.br panel
- [ ] Renewal date noted; renewal reminder configured
- [ ] Titular (CPF/CNPJ) and login credentials saved securely (password manager)
- [ ] DNS left at Registro.br default until deploy (DEV-011)

**Observações:**
- **Phase:** 0 — Foundation
- **Area:** infra
- **Priority:** P0
- **Status:** ✅ **Done** — domínio comprado no Registro.br

---

### DEV-001 — Monorepo scaffold ✅

**Descrição:**
Create repository structure per architecture doc.

**Critérios de aceitação:**
- [x] `frontend/` (Nuxt 3), `backend/` (ASP.NET Web API), `docs/`
- [x] Root `README.md` with local dev instructions (stub OK)
- [x] `.gitignore` for Node, .NET, env files
- [x] `.env.example` in `frontend/`

**Observações:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P0
- **Status:** ✅ Done

---

### DEV-002 — Docker Compose (local dev) ✅

**Descrição:**
One-command local stack: Postgres + API + Nuxt with volume mounts for hot reload.

**Critérios de aceitação:**
- [x] `docker compose up` starts Postgres, backend, frontend
- [x] API reachable at documented local URL
- [x] Nuxt dev server with HMR
- [x] Seed script creates **2 tenants** for isolation testing

**Observações:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P0
- **Depends on:** DEV-001
- **Status:** ✅ Done

---

### DEV-003 — Backend API skeleton ✅

**Descrição:**
ASP.NET Core Web API with health check, Swagger, Serilog, global exception handler.

**Critérios de aceitação:**
- [x] `GET /health` returns 200
- [x] OpenAPI at `/swagger` (dev only or configurable)
- [x] Structured JSON error responses
- [x] Dockerfile (multi-stage) in `backend/`
- [x] `Program.cs` reads config from environment variables

**Observações:**
- **Phase:** 0
- **Area:** backend
- **Priority:** P0
- **Depends on:** DEV-001
- **Status:** ✅ Done

---

### DEV-004 — EF Core + PostgreSQL setup

**Descrição:**
DbContext, Npgsql provider, initial migration infrastructure. Local connection to Compose Postgres.

**Critérios de aceitação:**
- [x] `ApplicationDbContext` registered in DI
- [x] Connection string from config (`ConnectionStrings__Default`)
- [x] `ApplicationDbContextFactory` + `ConnectionStrings:Migration` for `dotnet ef` (session pooler `:5432` prod / `localhost` local)
- [ ] `dotnet ef migrations add Initial` works locally *(run manually — see README)*
- [ ] `dotnet ef database update` applies against Compose Postgres *(run manually)*

**Observações:**
- **Phase:** 0
- **Area:** database
- **Priority:** P0
- **Depends on:** DEV-003, DEV-002

---

### DEV-005 — Nuxt 3 frontend skeleton ✅

**Descrição:**
Nuxt 3 app with TypeScript, basic layout, env config for API base URL. Inclui fundação [docs/ARCHITECTURE.md](./ARCHITECTURE.md): pastas `app/` / `platform/` / `public/`, layouts por superfície, middleware `resolve-host`, composables de tenant.

**Critérios de aceitação:**
- [x] Nuxt 3 + TypeScript runs locally and in Docker
- [x] `NUXT_PUBLIC_API_BASE` and platform host env vars defined
- [x] Default layout + error page
- [x] Nitro preset compatible with Vercel
- [x] Estrutura de componentes por superfície (ADR-016) — ver [docs/FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md)
- [x] Dark mode estrutural (`surface-dark`, `assets/css/surfaces/dark.css`) — tudo exceto site público tenant

**Observações:**
- **Phase:** 0
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-001
- **Status:** ✅ Done

---

### DEV-005b — Backend test project scaffold

**Descrição:**
Create `OnlinePortfolio.Api.Tests` xUnit project with standard .NET test stack. Folders `Unit/` and `Integration/` already exist; wire project into solution.

**Critérios de aceitação:**
- [x] `OnlinePortfolio.Api.Tests.csproj` in `backend/OnlinePortfolio.Api.Tests/`
- [x] Packages: **xUnit**, **Moq**, **FluentAssertions**, **coverlet.collector**
- [x] Project reference → `OnlinePortfolio.Api`
- [x] Added to `OnlinePortfolio.Api.slnx`
- [x] At least one smoke test green
- [x] `dotnet test` from `backend/` succeeds
- [x] Stack documented in [docs/ARCHITECTURE.md](./ARCHITECTURE.md)
- [x] Script `scripts/coverage-backend.ps1` (HTML report); `TestResults/` gitignored

**Observações:**
- **Phase:** 0
- **Area:** backend, unit-test
- **Priority:** P0
- **Depends on:** DEV-003

---

### DEV-012 — GitHub repository & platform integrations ✅

**Descrição:**
Criar repo `online-portfolio` no GitHub, primeiro push do monorepo, conectar Render e Vercel via GitHub App, preparar secrets para Actions. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Critérios de aceitação:**
- [x] Repo criado e código do monorepo em `main`
- [x] Render GitHub App instalado com acesso ao repo
- [x] Vercel GitHub App instalado — PR previews **on**; production auto-deploy **off** (Only build pre-production)
- [x] GitHub Actions secrets preparados (placeholders OK até DEV-008/010): `SUPABASE_MIGRATION_CONNECTION_STRING`, `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID`
- [x] (Recomendado) Branch protection em `main` exigindo Backend CI + Frontend CI — fechado em [DEV-006](#dev-006--github-actions-ci-pr--workflows-separados)

**Observações:**
- **Phase:** 0
- **Area:** infra, devops
- **Priority:** P0
- **Depends on:** DEV-001
- **Status:** ✅ **Done**

---

### DEV-013 — Linear workspace ✅

**Descrição:**
Workspace Linear para issues `DEV-xxx`. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Critérios de aceitação:**
- [x] Conta + workspace criados ([linear.app/signup](https://linear.app/signup))
- [x] Labels de `Area` configuradas (`backend`, `frontend`, `infra`, …)
- [x] DEV-000 marcado **Done**; DEV-001 (ou próxima issue ativa) criada
- [ ] (Opcional) Integração GitHub → repo `online-portfolio`

**Observações:**
- **Phase:** 0
- **Area:** docs, infra
- **Priority:** P2
- **Status:** ✅ **Done**

---

### DEV-014 — Resend account & API key

**Descrição:**
Conta Resend e API key para envio transacional (`noreply@onlineportfolio.com.br`). **Só configuração de conta** — integração na API em [DEV-107](#dev-107--contact-form--resend); verificação de domínio (DKIM) em [DEV-011](#dev-011--dns--https-production). Decisão: [ADR-017](./ADR-017-resend-transactional-email.md). Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) seção 8.5 (smoke test).

**Critérios de aceitação:**
- [x] Conta Resend criada e verificada
- [x] API key `portfolio-api-prod` criada
- [x] Key guardada no password manager
- [x] `Resend__ApiKey`, `Resend__FromEmail`, `Resend__FromName` no Render
- [x] Domínio prod adiado até DEV-011 (DKIM na Vercel DNS)
- [x] `Resend__FromEmail` = `noreply@onlineportfolio.com.br` documentado
- [ ] Smoke test (seção 8.5 EXTERNAL_PROVIDERS) — opcional

**Observações:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P1
- **Status:** ✅ **Done** — conta, API key, password manager, Render env

---

### DEV-006 — GitHub Actions CI (PR) — workflows separados ✅

**Descrição:**
Dois workflows de CI no PR — **separados** por stack (não um `ci.yml` único): `ci-backend.yml` e `ci-frontend.yml`, com path filters. **Escopo:** só CI em PR/push; deploy pipelines = [DEV-007](#dev-007--github-actions-deploy-pipeline-backend) / [DEV-007b](#dev-007b--github-actions-deploy-pipeline-frontend).

**Critérios de aceitação:**
- [x] `ci-backend.yml` runs on `pull_request` to `main` (paths: `backend/**`, `docs/DATABASE.md`, …)
- [x] `ci-frontend.yml` runs on `pull_request` to `main` (paths: `frontend/**`, …)
- [x] `push` → `main` com path filters + skip via `dorny/paths-filter` quando o commit não toca a stack
- [x] Backend: `dotnet test` (+ build); uses xUnit stack ([docs/ARCHITECTURE.md](./ARCHITECTURE.md))
- [x] Frontend: `npm run lint` + `npm run test:coverage` (Vitest; expand in UT-009+)
- [x] Status checks **Backend CI** and **Frontend CI** visible on PR
- [x] PR comments: **Backend coverage** / **Frontend coverage** (sticky; só quando o workflow respectivo roda)
- [x] Documented cross-stack component review before merge ([docs/AGENT_GUIDE.md](./AGENT_GUIDE.md))
- [x] (Recomendado) Branch protection em `main` exigindo **Backend CI** + **Frontend CI**
- [x] Documentar redeploy manual via Actions (`workflow_dispatch` nos deploy pipelines) em [docs/DEV_COMMANDS.md](./DEV_COMMANDS.md)

**Observações:**
- **Phase:** 0
- **Area:** devops
- **Priority:** P1
- **Depends on:** DEV-003, DEV-005, DEV-012
- **Status:** ✅ **Done**

**Done notes (2026-06-23):**
- `ci-backend.yml` + `ci-frontend.yml` com `dorny/paths-filter` em PR e push → `main`
- Branch protection em `main` (Backend CI + Frontend CI)
- Redeploy manual documentado em DEV_COMMANDS (`workflow_dispatch` em DEV-007 / DEV-007b)

---

### DEV-007 — GitHub Actions deploy pipeline (backend) ✅

**Descrição:**
`deploy-backend.yml` on push to `main` — backend test → EF migrate (Supabase session pooler `:5432`) → pass status for Render After CI Checks Pass.

**Critérios de aceitação:**
- [x] `SUPABASE_MIGRATION_CONNECTION_STRING` in GitHub Secrets (Session pooler `:5432`, IPv4)
- [x] Migrations run before deploy status succeeds
- [x] Render **After CI Checks Pass** documented and enabled (gate em check **Backend Deploy**)
- [x] `workflow_dispatch` em `deploy-backend.yml` (redeploy manual: test + migrate sem commit vazio)

**Observações:**
- **Phase:** 0
- **Area:** devops
- **Priority:** P1
- **Depends on:** DEV-006, DEV-004
- **Status:** ✅ **Done**

**Done notes (2025-06-21):**
- Workflow `.github/workflows/deploy-backend.yml` — trigger `push` → `main` (paths `backend/**`, `docs/DATABASE.md`)
- Job **Backend Deploy**: `dotnet test` → `dotnet ef database update` (secret `SUPABASE_MIGRATION_CONNECTION_STRING`)
- `dotnet-ef` 10.0.4 em `backend/.config/dotnet-tools.json`
- Render: Auto-Deploy On + **After CI Checks Pass**; Root Directory `backend`; primeiro deploy prod verde
- Fix CI: `ci-backend.yml` / `ci-frontend.yml` reportam status em todo PR (`dorny/paths-filter` + skip interno)
- Docs: connection strings (Transaction `:6543` runtime, Session `:5432` migrate, sem direct no pipeline) — [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) seção 6.2 · [docs/ARCHITECTURE.md](./ARCHITECTURE.md) seção 7

---

### DEV-007b — GitHub Actions deploy pipeline (frontend) ✅

**Descrição:**
`deploy-frontend.yml` on push to `main` — frontend lint/test → `vercel deploy --prod`. Disable Vercel production auto-deploy; PR previews stay on Vercel GitHub App.

**Critérios de aceitação:**
- [x] `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` in GitHub Secrets
- [x] `deploy-frontend.yml` — lint/test → `vercel pull` + `vercel env pull` → `npm run build` → stage `.vercel/output` → `vercel deploy --prebuilt --prod`
- [x] Workflow runs on push to `main` (paths: `frontend/**`, workflow file)
- [x] Production auto-deploy **disabled** on Vercel (`Only build pre-production`; DEV-010)
- [x] 1º **Frontend Deploy** green na `main`
- [x] PR preview deploys still work via Vercel integration (regression check)
- [x] `workflow_dispatch` em `deploy-frontend.yml` (redeploy manual após mudar env na Vercel — evita Ignored Build Step do dashboard)

**Observações:**
- **Phase:** 0
- **Area:** devops
- **Priority:** P1
- **Depends on:** DEV-006, DEV-005, DEV-010
- **Status:** ✅ **Done** — prod `online-portfolio-web-xi.vercel.app` via Actions; previews OK (ex. `*-git-*-marcelomborges-dev.vercel.app`)

---

### DEV-008 — Supabase production project ✅

**Descrição:**
Create Supabase **production** project `online-portfolio-db-prod`; store Transaction + Session pooler strings and API keys for Render + CI. Deploy de API/front continua **somente prod** ([docs/ARCHITECTURE.md](./ARCHITECTURE.md)) — DB dev é [DEV-008b](#dev-008b--supabase-dev-project). Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Critérios de aceitação:**
- [x] Prod project `online-portfolio-db-prod` in **US East** (alinhado ao Render Virginia)
- [x] Pooler URI (6543, Transaction) → Render `ConnectionStrings__Default`
- [x] Session pooler URI (5432) → GitHub secret `SUPABASE_MIGRATION_CONNECTION_STRING` (CI migrate prod; IPv4)
- [x] Project URL + **service_role** / secret API key no password manager (Render env; Storage Fase 3+)
- [x] Migration `Initial` aplicada em prod (`dotnet ef database update` local)
- [x] Checklist [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) (prod)

**Observações:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P1
- **Status:** ✅ **Done** — projeto `online-portfolio-db-prod` (US East)

**Done notes (2025-06-21):**
- Project ref: `mfxuthlwrfjscxnnjeud` · região US East · pooler Transaction `aws-1-us-east-1.pooler.supabase.com:6543`
- Templates sem senha em `backend/OnlinePortfolio.Api/appsettings.json` (`Default` = Transaction pooler `:6543`, `Migration` = Session pooler `:5432`)
- `__EFMigrationsHistory` em prod: `20260622012938_Initial` (EF Core 10.0.4)
- GitHub secret `SUPABASE_MIGRATION_CONNECTION_STRING` configurado — usado por [DEV-007](#dev-007--github-actions-deploy-pipeline-backend) ✅
- `Supabase__Url` / `Supabase__ServiceRoleKey` no Render **deferidos** até Storage (Fase 3)

---

### DEV-008b — Supabase dev project

**Descrição:**
Segundo projeto Supabase **`online-portfolio-db-dev`** — Postgres na nuvem para desenvolvimento local **opcional** (alternativa ao Postgres do Docker Compose). **Não** deploya API/front dev; só substitui o banco local quando você apontar `appsettings.Development.json` / `appsettings.json` para o dev. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Critérios de aceitação:**
- [ ] Dev project `online-portfolio-db-dev` criado (mesma região que prod, ex. US East)
- [ ] Session pooler (5432, migrate) + Transaction pooler (6543, runtime) guardados no **password manager**
- [ ] `appsettings.Development.json` / `appsettings.json` documentado: alternar `ConnectionStrings` entre **Docker local** e **Supabase dev**
- [ ] Migrations aplicadas em dev local (`dotnet ef database update` contra `localhost` / Compose)
- [ ] [docs/DEV_COMMANDS.md](./DEV_COMMANDS.md) descreve os dois modos (Compose vs Supabase dev)

**Observações:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P2
- **Depends on:** DEV-008 (recomendado — mesma org/região)

**Nota:** CI e Render usam **sempre** prod. Dev Supabase é só para máquina do desenvolvedor.

---

### DEV-009 — Render API deployment ✅

**Descrição:**
Deploy backend Docker image to Render free tier; connect GitHub repo. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Critérios de aceitação:**
- [x] Conta Render criada; web service Docker (`backend/OnlinePortfolio.Api/Dockerfile`) conectado ao repo
- [x] Web service live on Render default URL
- [x] Health check `/health` configured e retornando 200
- [x] Production env vars set (`ConnectionStrings__Default`, `Jwt__*`, `Resend__*` de DEV-014, `ASPNETCORE_*`)
- [x] **After CI Checks Pass** habilitado (gate em `deploy-backend.yml` — DEV-007)
- [ ] Custom domain `api.onlineportfolio.com.br` (adiado — DEV-011; OK com `*.onrender.com`)

**Observações:**
- **Phase:** 0
- **Area:** devops
- **Priority:** P1
- **Depends on:** DEV-003, DEV-008, DEV-007, DEV-014
- **Status:** ✅ **Done** — deploy prod ok com env vars; domínio custom → DEV-011

---

### DEV-010 — Vercel frontend deployment ✅

**Descrição:**
Connect repo to Vercel; root directory `frontend`; PR previews enabled; **production deploy via `deploy-frontend.yml`** (DEV-007b). Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Critérios de aceitação:**
- [x] Conta Vercel criada; projeto `online-portfolio-web` importado do GitHub (`frontend/` root)
- [x] PR preview deploys enabled (testado — comentário da Vercel no PR)
- [x] Production auto-deploy **disabled** in Vercel (`Only build pre-production`; prod = Actions DEV-007b)
- [x] Env vars configured in Vercel dashboard (`NUXT_PUBLIC_*`, `NUXT_API_INTERNAL_BASE` → Render `*.onrender.com`)
- [x] Custom domains deferred until [DEV-011](#dev-011--dns--https-production)

**Observações:**
- **Phase:** 0
- **Area:** devops
- **Priority:** P1
- **Depends on:** DEV-005, DEV-012
- **Status:** ✅ **Done** — deploy Ready; `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` no password manager (GitHub Secrets → DEV-007b)

---

### DEV-011 — DNS & HTTPS (production)

**Descrição:**
DNS de produção: Registro.br nameservers → Vercel; domínios apex/`app.`/wildcard; CNAME `api.` → Render; registros Resend (DKIM/SPF). Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Critérios de aceitação:**
- [ ] Nameservers `ns1.vercel-dns.com` / `ns2.vercel-dns.com` at Registro.br
- [ ] Domains added in Vercel: apex, `app.`, `*.onlineportfolio.com.br`
- [ ] HTTPS active on Vercel domains (auto)
- [ ] `api.onlineportfolio.com.br` verified on Render with HTTPS (auto)
- [ ] Resend domain verification: DNS records na Vercel DNS (seção 10.4)
- [ ] Resend dashboard mostra domínio `onlineportfolio.com.br` verificado
- [ ] Checklist [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) marcado

**Observações:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P1
- **Depends on:** DEV-000, DEV-009, DEV-010

---

## Epic 1 — Site público por tenant (Phase 1)

**Objetivo:** landing + posts/galeria em `{slug}.onlineportfolio.com.br` (ex.: `ana.`, `mark.`). Admin fica no Epic 1.5 (`app.`). Ver [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

---

### DEV-100 — Domain model: Tenant, Plan, TenantSettings

**Descrição:**
EF entities for platform-scoped tenant tables. **Superseded by Epic 1.5** if login MVP is built first — see DEV-150/151 and [docs/DATABASE.md](./DATABASE.md).

**Critérios de aceitação:**
- [ ] `Tenant`, `Plan`, `TenantSettings` entities
- [ ] Unique index on `Tenant.Slug`, unique `CustomDomain` where not null
- [ ] Migration applied locally and documented for CI
- [ ] Seed: 2 tenants with distinct slugs

**Observações:**
- **Phase:** 1
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-004

---

### DEV-101 — Domain model: Artwork (+ optional Category)

**Descrição:**
Tenant-scoped artwork entity with publish flag and slug scoped per tenant.

**Critérios de aceitação:**
- [ ] `Artwork` with `TenantId`, `IsPublished`, `PublishedAt`, `SortOrder`
- [ ] Unique `(TenantId, Slug)` on Artwork
- [ ] Optional `Category` entity if included in v1
- [ ] EF global query filter on `TenantId` (foundation)

**Observações:**
- **Phase:** 1
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-100

---

### DEV-102 — TenantContext middleware

**Descrição:**
Resolve tenant from route slug; expose `ITenantContext` for request scope.

**Critérios de aceitação:**
- [ ] Middleware or endpoint filter resolves tenant by slug
- [ ] 404 for unknown/inactive tenant
- [ ] `TenantId` available to services and EF filters

**Observações:**
- **Phase:** 1
- **Area:** backend
- **Priority:** P0
- **Depends on:** DEV-100

---

### DEV-103 — Public API: tenant profile & artworks

**Descrição:**
Read-only public endpoints per [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**Critérios de aceitação:**
- [ ] `GET /api/v1/tenants/{slug}/profile`
- [ ] `GET /api/v1/tenants/{slug}/artworks` (paginated, published only)
- [ ] `GET /api/v1/tenants/{slug}/artworks/{id}` (published only)
- [ ] OpenAPI documented
- [ ] No draft/unpublished data leaked

**Observações:**
- **Phase:** 1
- **Area:** backend
- **Priority:** P0
- **Depends on:** DEV-101, DEV-102

---

### DEV-104 — Nuxt tenant resolution middleware

**Descrição:**
Resolve tenant pelo `Host` — subdomínio `{slug}.onlineportfolio.com.br` ou domínio custom. Host `app.*` e apex da plataforma **não** são tenants. Arquitetura de superfícies: [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**Critérios de aceitação:**
- [x] Middleware: `{slug}.onlineportfolio.com.br` → tenant slug (`resolve-host.global.ts`)
- [x] Middleware: `app.*` → modo admin (sem tenant público)
- [x] Middleware: `onlineportfolio.com.br` → landing da plataforma
- [x] Dev local: slug configurável via env (`NUXT_PUBLIC_DEV_SURFACE`, `NUXT_PUBLIC_DEV_TENANT_SLUG`)
- [ ] Tenant desconhecido → página 404 (validar slug na API — DEV-103)
- [x] Composable expõe contexto do tenant nas páginas públicas (`useRequestSurface`, `useTenantContext`)

**Observações:**
- **Phase:** 1
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-005, DEV-103
- **Status:** 🟡 Parcial — falta 404 para slug desconhecido (validação API)

---

### DEV-105 — Páginas públicas do tenant (landing + posts/galeria)

**Descrição:**
Site público por tenant em `{slug}.onlineportfolio.com.br` — home/landing, listagem de posts/obras, detalhe, about. Dados via proxy Nuxt → API. Layout base compartilhado; overrides por tenant em `components/public/tenants/{slug}/` — [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**Critérios de aceitação:**
- [x] Landing/home por tenant (skeleton: `useTenantComponent('LandingHero')`, exemplos Ana/João, temas CSS)
- [ ] Listagem de posts/obras
- [ ] Página de detalhe
- [ ] About/contato a partir de `TenantSettings` (stub `/contact` + `PublicContactSection` existe)
- [ ] SSG ou ISR com cache key incluindo slug do tenant
- [ ] Layout responsivo (mobile-first)
- [ ] **Sem** rotas de admin/login neste host

**Observações:**
- **Phase:** 1
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-104, DEV-103
- **Status:** 🟡 Parcial — landing skeleton + temas; dados reais e galeria pendentes (DEV-103)

---

### DEV-106 — Nuxt server proxy to API

**Descrição:**
Rotas server fazem proxy de **todas** as chamadas (público + admin) para a API — padrão BFF. Browser não acessa Render direto.

**Critérios de aceitação:**
- [ ] `server/api/**` faz proxy para `api.onlineportfolio.com.br`
- [ ] Páginas públicas e admin usam proxy ou fetch server-side
- [ ] Cookies de auth repassados no proxy (admin)
- [ ] URL base da API não hardcoded no bundle client para paths sensíveis

**Observações:**
- **Phase:** 1
- **Area:** frontend
- **Priority:** P1
- **Depends on:** DEV-105

---

### DEV-107 — Contact form + Resend

**Descrição:**
Formulário de contato POST → API → Resend (`noreply@`) → `ContactEmail` do tenant. Requer [DEV-014](#dev-014--resend-account--api-key) (conta) e domínio verificado em [DEV-011](#dev-011--dns--https-production) para prod. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) · [ADR-017](./ADR-017-resend-transactional-email.md).

**Critérios de aceitação:**
- [ ] `POST /api/v1/tenants/{slug}/contact` with validation
- [ ] `IEmailService` + `ResendEmailService` (NuGet `Resend`)
- [ ] Templates em `EmailTemplates/` (contact, invite)
- [ ] `Resend__FromEmail` = `noreply@onlineportfolio.com.br`
- [ ] Reply-To = email do visitante (artista responde direto)
- [ ] Rate limiting on contact endpoint (basic)
- [ ] Contact form UI on public site (`pages/contact.vue` + `useTenantComponent('ContactSection')` stub; override por tenant em `public/tenants/{slug}/`)
- [ ] Prod: domínio autenticado via [DEV-011](#dev-011--dns--https-production)
- [ ] Honeypot or basic anti-spam field

**Observações:**
- **Phase:** 1
- **Area:** backend, frontend
- **Priority:** P1
- **Depends on:** DEV-103, DEV-014

---

### DEV-108 — Platform admin: seed / create tenants (API)

**Descrição:**
Minimal platform admin endpoints or seed-only for first 2 artists (full PlatformAdmin auth in Phase 2).

**Critérios de aceitação:**
- [ ] `POST /api/v1/platform/tenants` (protected — API key or temporary auth for v1)
- [ ] Creates Tenant + TenantSettings defaults
- [ ] Document manual provisioning for first customers
- [ ] Seed data script for local dev (2 tenants + sample artworks)

**Observações:**
- **Phase:** 1
- **Area:** backend
- **Priority:** P1
- **Depends on:** DEV-100

---

### DEV-109 — Platform marketing page (apex)

**Descrição:**
Landing page at `onlineportfolio.com.br` when host is apex (not tenant subdomain). Conteúdo em `components/platform/` — [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**Critérios de aceitação:**
- [x] Apex host shows platform marketing content (stub: `PlatformLandingHero` via `index.vue` + surface `platform`)
- [ ] Subdomain hosts show tenant gallery (depende DEV-105)
- [ ] Clear CTA for artists (contact / waitlist)

**Observações:**
- **Phase:** 1
- **Area:** frontend
- **Priority:** P2
- **Depends on:** DEV-104

---

## Epic 1.5 — Multi-tenant DB + admin login + add user (MVP)

**Goal:** Database + admin com **duas funções essenciais** (sem gallery/artwork ainda):

| # | Função | Quem | Entrega |
|---|---|---|---|
| 1 | **Login / logout** | PlatformAdmin + tenant users | DEV-154–158 |
| 2 | **Add user** (convite por tenant) | PlatformAdmin only | DEV-159, DEV-161, DEV-162 |

Sem CRUD de obras, settings completos ou site público neste epic.

**Domínios:** login e admin só em `app.onlineportfolio.com.br`; sites `{slug}.onlineportfolio.com.br` vêm no Epic 1 (público).

**Schema reference:** [docs/DATABASE.md](./DATABASE.md) — migration `InitialMultiTenantAndUsers`

### Admin MVP — escopo funcional

```text
PlatformAdmin (você)
  ├── Login → /admin ou /platform
  ├── Listar tenants
  ├── Por tenant: listar usuários
  └── Por tenant: convidar usuário (email + role Owner/Editor)

Tenant user (Owner/Editor)
  ├── Login → /admin
  └── Dashboard do tenant (placeholder — sem add user no v1)
```

---

### DEV-150 — EF migration: multi-tenant + users

**Descrição:**
First migration per DATABASE.md — `plans`, `tenants`, `tenant_settings`, `users` only (no artworks).

**Critérios de aceitação:**
- [ ] Migration `InitialMultiTenantAndUsers` created
- [ ] Tables match [docs/DATABASE.md](./DATABASE.md) (columns, FKs, checks)
- [ ] Indexes: `tenants.slug`, `users.email` UNIQUE, partial unique on `custom_domain`
- [ ] `dotnet ef database update` works on Compose Postgres
- [ ] Rollback (`dotnet ef migrations remove`) tested locally

**Observações:**
- **Phase:** 1.5 — Login + add user MVP
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-004

---

### DEV-151 — EF entities and configurations

**Descrição:**
C# entities `Plan`, `Tenant`, `TenantSettings`, `User` with Fluent API / snake_case naming.

**Critérios de aceitação:**
- [ ] Entities in `backend/OnlinePortfolio.Api/Data/Entities/`
- [ ] `ApplicationDbContext` DbSets registered
- [ ] `ApplicationUser : IdentityUser<Guid>` com `TenantId`, `InvitedByUserId`, `IsActive` (sem coluna `Role` — ver AspNetRoles)
- [ ] `ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`
- [ ] Seed roles: `PlatformAdmin`, `Owner`, `Editor` via `RoleManager`
- [ ] Constantes ou enum espelhando nomes das roles Identity
- [ ] No global query filter on `User` that hides PlatformAdmin

**Observações:**
- **Phase:** 1.5
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-150

---

### DEV-152 — Dev seed: plans + two tenants

**Descrição:**
Seed `Starter` plan + tenants `ana` and `joao` with empty `tenant_settings`.

**Critérios de aceitação:**
- [ ] Seed runs on local `docker compose up` or explicit `dotnet run --seed`
- [ ] Two active tenants with distinct slugs
- [ ] No artwork rows (table does not exist yet)
- [ ] Seed PlatformAdmin user with password (dev credentials documented)

**Observações:**
- **Phase:** 1.5
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-151

---

### DEV-153 — ASP.NET Identity completo + JWT

**Descrição:**
Identity **completo** — `AddIdentity`, `RoleManager`, stores EF, JWT na API. Sem Supabase Auth.

**Critérios de aceitação:**
- [ ] `AddIdentity<ApplicationUser, IdentityRole<Guid>>()` + `AddEntityFrameworkStores` + `AddDefaultTokenProviders`
- [ ] `RoleManager` + seed `PlatformAdmin`, `Owner`, `Editor`
- [ ] Política de senha (mín. 8 chars) + lockout
- [ ] `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` + middleware JwtBearer com **role claims**
- [ ] Auto-cadastro desabilitado (invite-only)
- [ ] `[Authorize(Roles = "...")]` nos endpoints platform
- [ ] Regra API: PlatformAdmin => `tenant_id` NULL; Owner/Editor => `tenant_id` obrigatório
- [ ] Documentado em [docs/ARCHITECTURE.md](./ARCHITECTURE.md)

**Observações:**
- **Phase:** 1.5
- **Area:** backend, security
- **Priority:** P0
- **Depends on:** DEV-151

---

### DEV-154 — API: auth endpoints + `GET /auth/me`

**Descrição:**
Login, logout, accept-invite, and current user — all in API.

**Critérios de aceitação:**
- [ ] `POST /api/v1/auth/login` — `SignInManager` → JWT com role claims
- [ ] Invite usa `UserManager.AddToRoleAsync`
- [ ] `POST /api/v1/auth/logout` — clear session/cookie
- [ ] `POST /api/v1/auth/accept-invite` — token + password for pending invite
- [ ] `GET /api/v1/auth/me` returns `{ user, tenant }` for Owner/Editor
- [ ] PlatformAdmin: `tenant` null, role in response
- [ ] Updates `users.last_login_at`
- [ ] Inactive user or inactive tenant → 403
- [ ] OpenAPI documented

**Observações:**
- **Phase:** 1.5
- **Area:** backend, security
- **Priority:** P0
- **Depends on:** DEV-151, DEV-153

---

### DEV-155 — Nuxt: BFF proxy + `app.` host routing

**Descrição:**
Server routes proxy `/api/**` to Render API. Route `app.localhost` / `app.onlineportfolio.com.br` to admin app. **No Supabase client.** Host routing parcial já em DEV-104 (`resolve-host.global.ts`); este ticket foca **proxy BFF** e guards de auth.

**Critérios de aceitação:**
- [ ] Catch-all server route forwards to `NUXT_API_INTERNAL_BASE` / Render URL
- [ ] Forwards cookies and auth headers to API
- [x] Middleware: host `app.*` → admin layout; `{slug}.*` → public (stub — DEV-104)
- [ ] Browser never calls `api.onlineportfolio.com.br` directly (admin)

**Observações:**
- **Phase:** 1.5
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-005

---

### DEV-156 — Admin login page

**Descrição:**
Login UI at `/login` on **centralized** `app.{host}` — form posts via Nuxt proxy to `POST /auth/login`. UI em `components/app/` (padronizada, sem variantes por tenant — ADR-016). **Dark mode** (`surface-dark`) — ver [docs/FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md).

**Critérios de aceitação:**
- [x] Page at `app.{host}/login` only (stub `pages/login.vue` + guard; not on tenant subdomains)
- [x] Dark mode (`surface-dark`) na superfície app
- [ ] Email + password → proxy → API login
- [ ] Error messages for invalid credentials (no user enumeration)
- [ ] Redirect: Owner/Editor → `/admin`; PlatformAdmin → `/platform/tenants`
- [ ] Already authenticated → redirect per role

**Observações:**
- **Phase:** 1.5
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-155, DEV-154

---

### DEV-157 — Admin session: logout + cookie forwarding

**Descrição:**
Logout via API; composable `useAuth` calls `/auth/me` through proxy.

**Critérios de aceitação:**
- [ ] Logout → proxy → `POST /auth/logout` + redirect to `/login`
- [ ] Composable `useAuth` wraps `/auth/me` via server proxy
- [ ] All admin API calls go through Nuxt proxy (no direct Render from browser)
- [ ] JWT/cookie never exposed to client JS if using httpOnly cookie

**Observações:**
- **Phase:** 1.5
- **Area:** frontend, backend
- **Priority:** P0
- **Depends on:** DEV-156, DEV-154

---

### DEV-158 — Protected admin shell (empty dashboard)

**Descrição:**
`/admin` for tenant users; PlatformAdmin also has link to `/platform/tenants` for add user. Herda **dark mode** estrutural (`surface-dark`, `layouts/app.vue`).

**Critérios de aceitação:**
- [ ] Unauthenticated access → redirect `/login`
- [x] Layout e tokens dark (`surface-dark`) — mesmo padrão do login, platform e error
- [ ] Shows logged-in email, role, tenant name (from `/auth/me`)
- [ ] Logout control visible
- [ ] **PlatformAdmin:** nav link to `/platform/tenants` (add user flow)
- [ ] **Owner/Editor:** placeholder "Dashboard em construção" — no add user UI

**Observações:**
- **Phase:** 1.5
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-157

---

### DEV-159 — Platform admin seed + invite tenant users

**Descrição:**
Seed your PlatformAdmin user; thin wrapper for first invite (full add-user API in DEV-161).

**Critérios de aceitação:**
- [ ] Seed PlatformAdmin: `UserManager.CreateAsync` + `AddToRoleAsync("PlatformAdmin")` + senha (dev)
- [ ] First tenant user invite works end-to-end via DEV-161 endpoint
- [ ] Manual test: invite Owner for `ana` and `joao`; each logs in via login flow (DEV-156–157)

**Observações:**
- **Phase:** 1.5
- **Area:** backend, infra
- **Priority:** P0
- **Depends on:** DEV-154, DEV-152

---

### DEV-161 — Platform API: list & add user (invite) per tenant

**Descrição:**
**Add user** — PlatformAdmin lists and invites users to any tenant. Core MVP alongside login.

**Critérios de aceitação:**
- [ ] `GET /api/v1/platform/tenants/{tenantId}/users` — list users (email, role, is_active, last_login_at)
- [ ] `POST .../users/invite` → `CreateAsync` + `AddToRoleAsync(role)` + Resend
- [ ] `PATCH /api/v1/platform/tenants/{tenantId}/users/{userId}` — deactivate or change role (Owner/Editor)
- [ ] Duplicate invite to same email on same tenant → clear error
- [ ] Tenant users (Owner/Editor) receive **403** on all `/platform/*` routes
- [ ] Cannot deactivate last Owner without replacement (business rule)
- [ ] OpenAPI documented

**Observações:**
- **Phase:** 1.5
- **Area:** backend, security
- **Priority:** P0
- **Depends on:** DEV-154, DEV-159

---

### DEV-162 — Platform admin UI: add user + list users

**Descrição:**
UI for **add user** — PlatformAdmin picks tenant, invites by email, sees user list. Required for MVP.

**Critérios de aceitação:**
- [ ] Route `/platform/tenants` — list tenants (PlatformAdmin only)
- [ ] Route `/platform/tenants/{id}/users` — user list + **Add user** form (email, role Owner/Editor)
- [ ] Success feedback after invite; error for duplicate/invalid email
- [ ] Deactivate user action with confirmation
- [ ] Owner/Editor visiting `/platform/*` → 403 or redirect to `/admin`
- [ ] Owner `/admin` — dashboard placeholder only (no add user in v1)

**Observações:**
- **Phase:** 1.5
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-158, DEV-161

---

### DEV-160 — Login MVP: integration & unit tests

**Descrição:**
Test coverage for login, add user, and JWT (UT-005, UT-012, IT-011–013).

**Critérios de aceitação:**
- [ ] UT-005 JWT handler tests green
- [ ] UT-012 User bootstrap unit tests green
- [ ] IT-011 `/auth/me` integration tests green
- [ ] IT-012 login tenant isolation tests green
- [ ] IT-013 add user invite/list/deactivate tests green

**Observações:**
- **Phase:** 1.5
- **Area:** unit-test, integration-test, security
- **Priority:** P1
- **Depends on:** DEV-154, DEV-158

---

## Epic 2 — Admin features (post-login)

**Requires:** Epic 1.5 complete (login + add user working). Adds artwork CRUD, settings, PDF.

> Auth tasks DEV-200–202 → Epic 1.5 (DEV-153–158 login, DEV-161–162 add user).

---

### DEV-200 — ~~Supabase Auth~~ → ver DEV-153 (Identity completo)

**Descrição:**
—

**Critérios de aceitação:**
- [ ] *(definir)*

**Observações:**
- *(nenhuma)*

---

### DEV-201 — ~~User + JWT Supabase~~ → ver DEV-153/154

**Descrição:**
—

**Critérios de aceitação:**
- [ ] *(definir)*

**Observações:**
- *(nenhuma)*

---

### DEV-202 — ~~Nuxt + Supabase login~~ → ver DEV-155–158 (BFF + proxy)

**Descrição:**
—

**Critérios de aceitação:**
- [ ] *(definir)*

**Observações:**
**Observações:**

---

### DEV-203 — Admin API: Artwork CRUD

**Descrição:**
Authenticated CRUD for artworks; tenant from JWT user, not request body.

**Critérios de aceitação:**
- [ ] `POST/PUT/DELETE /api/v1/artworks`
- [ ] `GET /api/v1/artworks` includes drafts for admin
- [ ] `TenantId` from user context; IDOR checks on `{id}`
- [ ] Publish/unpublish via `IsPublished` / `PublishedAt`

**Observações:**
- **Phase:** 2
- **Area:** backend
- **Priority:** P0
- **Depends on:** DEV-154, DEV-101

---

### DEV-204 — Admin UI: artwork management

**Descrição:**
Admin pages to list, create, edit, delete, publish artworks.

**Critérios de aceitação:**
- [ ] Artwork list (draft + published)
- [ ] Create/edit form with validation
- [ ] Publish toggle
- [ ] Delete with confirmation
- [ ] Sort order control (basic)

**Observações:**
- **Phase:** 2
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-158, DEV-203

---

### DEV-205 — Admin UI: tenant settings

**Descrição:**
Edit bio, contact email, social links, theme placeholders.

**Critérios de aceitação:**
- [ ] `GET/PUT` tenant settings for authenticated owner
- [ ] Settings form in admin
- [ ] Changes reflected on public profile

**Observações:**
- **Phase:** 2
- **Area:** frontend, backend
- **Priority:** P1
- **Depends on:** DEV-158

---

### DEV-206 — Tenant provisioning flow (PlatformAdmin)

**Descrição:**
End-to-end: PlatformAdmin creates tenant → invites one or more admin users per tenant.

**Critérios de aceitação:**
- [ ] `POST /api/v1/platform/tenants` creates tenant + settings
- [ ] Invite flow for first Owner + additional users on same tenant
- [ ] Tenant live at `{slug}.onlineportfolio.com.br`

**Observações:**
- **Phase:** 2
- **Area:** backend
- **Priority:** P1
- **Depends on:** DEV-159, DEV-161

---

### DEV-207 — Portfolio PDF export (QuestPDF)

**Descrição:**
Public and admin PDF catalog endpoints using QuestPDF Community license.

**Critérios de aceitação:**
- [ ] `GET /api/v1/tenants/{slug}/portfolio.pdf` (published only)
- [ ] `GET /api/v1/portfolio/export.pdf` (authenticated)
- [ ] `IPdfService` + document layout in `backend/OnlinePortfolio.Api/Pdf/`
- [ ] `LicenseType.Community` registered at startup
- [ ] Download button on public gallery / admin

**Observações:**
- **Phase:** 2
- **Area:** backend, frontend
- **Priority:** P2
- **Depends on:** DEV-203

---

## Epic 3 — Image uploads (Phase 3)

---

### DEV-300 — Supabase Storage buckets (API-only writes)

**Descrição:**
Create `artworks-public` bucket; path prefix `tenants/{tenantId}/`. Public read; writes via API service role only.

**Critérios de aceitação:**
- [ ] Buckets created per architecture
- [ ] Public read for gallery CDN URLs; no browser upload to Storage
- [ ] API validates tenant path before write
- [ ] Documented in EXTERNAL_PROVIDERS checklist

**Observações:**
- **Phase:** 3
- **Area:** infra, security
- **Priority:** P0
- **Depends on:** DEV-008

---

### DEV-301 — ArtworkImage entity + migration

**Descrição:**
Store image metadata in EF; binaries in Storage only.

**Critérios de aceitação:**
- [ ] `ArtworkImage` entity with `StoragePath`, `PublicUrl`, `SortOrder`, dimensions
- [ ] `TenantId` on entity; global filter applied
- [ ] Migration applied

**Observações:**
- **Phase:** 3
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-101, DEV-300

---

### DEV-302 — Upload via API (multipart through Nuxt proxy)

**Descrição:**
Admin uploads via Nuxt proxy → API multipart → Storage (service role).

**Critérios de aceitação:**
- [ ] Upload from admin UI through `/api/**` proxy
- [ ] API validates tenant membership + path prefix before Storage write
- [ ] File type whitelist (jpeg, png, webp)
- [ ] Max file size enforced (configurable)

**Observações:**
- **Phase:** 3
- **Area:** frontend, backend
- **Priority:** P0
- **Depends on:** DEV-301, DEV-158

---

### DEV-303 — Gallery displays uploaded images

**Descrição:**
Public gallery and detail pages show images from Supabase public URLs.

**Critérios de aceitação:**
- [ ] Hero/thumbnail on list and detail
- [ ] Alt text from metadata
- [ ] Fallback when no image

**Observações:**
- **Phase:** 3
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-302, DEV-105

---

### DEV-304 — PDF includes artwork thumbnails

**Descrição:**
QuestPDF catalog embeds thumbnail URLs from Storage.

**Critérios de aceitação:**
- [ ] PDF uses thumbnail URLs, not originals
- [ ] Graceful fallback if image missing

**Observações:**
- **Phase:** 3
- **Area:** backend
- **Priority:** P2
- **Depends on:** DEV-207, DEV-303

---

### DEV-305 — Image processing strategy (optional)

**Descrição:**
Decide and implement thumbnail/web variant generation (ImageSharp on API or manual).

**Critérios de aceitação:**
- [ ] Document chosen approach in ARCHITECTURE open decisions
- [ ] Web-optimized variant stored alongside original
- [ ] Size limits per plan (future) considered in design

**Observações:**
- **Phase:** 3
- **Area:** backend
- **Priority:** P3
- **Depends on:** DEV-302

---

## Epic 4 — Custom domains (+ billing opcional)

**v1:** sem cobrança automática — tenants criados manualmente. Domínios custom (DEV-400+) podem ser feitos **sem** billing. DEV-402/403 são **opcionais** até decidir cobrar.

---

### DEV-400 — Tenant custom domain fields + resolution

**Descrição:**
Store `CustomDomain`, `CustomDomainVerifiedAt`; Nuxt resolves tenant from Host.

**Critérios de aceitação:**
- [ ] Host lookup: CustomDomain → tenant, then subdomain slug
- [ ] www vs apex normalization
- [ ] Admin UI to request custom domain (DNS instructions)

**Observações:**
- **Phase:** 4
- **Area:** backend, frontend
- **Priority:** P1
- **Depends on:** DEV-104

---

### DEV-401 — Vercel custom domain per tenant

**Descrição:**
Manual add in Vercel dashboard for first tenants; document Vercel Domains API for scale.

**Critérios de aceitação:**
- [ ] At least one tenant custom domain verified on Vercel
- [ ] HTTPS auto on tenant domain
- [ ] Runbook for artist DNS (CNAME instructions)

**Observações:**
- **Phase:** 4
- **Area:** infra, devops
- **Priority:** P1
- **Depends on:** DEV-400

---

### DEV-402 — Plan entity + limits enforcement (optional)

**Descrição:**
Enforce `MaxArtworks`, `MaxStorageMb`, `CustomDomainAllowed` per plan. **Opcional no v1** — pode operar sem planos rígidos ou atribuir plano manualmente no banco.

**Critérios de aceitação:**
- [ ] Plan seeded (Basic / Pro or similar)
- [ ] API rejects over-limit operations with clear error
- [ ] Tenant assigned to plan on provisioning (manual OK)

**Observações:**
- **Phase:** 4
- **Area:** backend
- **Priority:** P2
- **Depends on:** DEV-100

---

### DEV-403 — Billing / subscriptions (optional — skip until charging)

**Descrição:**
Checkout + webhook for tenant billing. **Fora do escopo inicial — não implementar enquanto não cobrar.** Provider TBD: **Stripe** se expandir fora do BR (multi-moeda, cartões globais); **Asaas/Iugu** se permanecer só Brasil (PIX, fiscal). Ver [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Critérios de aceitação:**
- [ ] PSP account + products/prices configured
- [ ] Webhook `POST /api/v1/webhooks/...` on Render
- [ ] Webhook secret in Render env
- [ ] Plan updated on successful subscription events

**Observações:**
- **Phase:** 4
- **Area:** backend, infra
- **Priority:** P3
- **Depends on:** DEV-402 (if limits tied to paid plans)

---

### DEV-404 — Google Workspace operator inbox

**Descrição:**
Configure Google Workspace for `hello@onlineportfolio.com.br` when needed. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Critérios de aceitação:**
- [ ] MX + SPF merged with Resend (when Google Workspace added)
- [ ] Test send/receive
- [ ] Documented in [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md)

**Observações:**
- **Phase:** 4
- **Area:** infra
- **Priority:** P3
- **Depends on:** DEV-000, DEV-011

---

## Unit tests

Activities for isolated, fast tests (no external services). Run in CI on every PR.

**Backend stack:** xUnit · Moq · FluentAssertions · Coverlet · `dotnet test` — see [docs/ARCHITECTURE.md](./ARCHITECTURE.md). **Frontend stack:** Vitest (UT-009+).

---

### UT-001 — Backend: TenantContext unit tests

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Tests for valid slug, inactive tenant, missing tenant
- [ ] Mock `ITenantProvider` where applicable

**Observações:**
- **Area:** unit-test, backend
- **Priority:** P1
- **Depends on:** DEV-102

---

### UT-002 — Backend: EF global query filters

**Descrição:**
—

**Critérios de aceitação:**
- [ ] In-memory or test DB verifies Artwork queries never cross tenants
- [ ] Filter applied automatically on tenant-scoped entities

**Observações:**
- **Area:** unit-test, backend
- **Priority:** P0
- **Depends on:** DEV-101

---

### UT-003 — Backend: Contact form validation

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Invalid email, empty message, oversized input rejected
- [ ] Honeypot field triggers silent reject or 400

**Observações:**
- **Area:** unit-test, backend
- **Priority:** P1
- **Depends on:** DEV-107

---

### UT-004 — Backend: Resend email service (mocked)

**Descrição:**
—

**Critérios de aceitação:**
- [ ] `IResend` / `IEmailService` mock verifies To, From, Reply-To
- [ ] Resend client not called when validation fails

**Observações:**
- **Area:** unit-test, backend
- **Priority:** P1
- **Depends on:** DEV-107

---

### UT-012 — Backend: accept-invite and login

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Valid invite token + password → user active, email_confirmed
- [ ] Invalid/expired token → 400
- [ ] Login with correct password → JWT issued; updates last_login_at
- [ ] Login with wrong password → 401 (no user enumeration)

**Observações:**
- **Area:** unit-test, backend
- **Priority:** P0
- **Depends on:** DEV-154

---

### UT-005 — Backend: JWT authorization handler

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Valid token → user context populated
- [ ] Expired/missing token → 401
- [ ] User not in DB → 403

**Observações:**
- **Area:** unit-test, backend, security
- **Priority:** P0
- **Depends on:** DEV-154

---

### UT-006 — Backend: Artwork slug uniqueness per tenant

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Same slug allowed across different tenants
- [ ] Duplicate slug within tenant rejected

**Observações:**
- **Area:** unit-test, backend
- **Priority:** P1
- **Depends on:** DEV-203

---

### UT-007 — Backend: QuestPDF document builder

**Descrição:**
—

**Critérios de aceitação:**
- [ ] PDF generation returns non-empty stream
- [ ] Contains tenant name and artwork count in output (snapshot or byte length check)

**Observações:**
- **Area:** unit-test, backend
- **Priority:** P2
- **Depends on:** DEV-207

---

### UT-008 — Backend: Plan limit validator

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Max artworks enforced at service layer
- [ ] Clear exception or result type for limit exceeded

**Observações:**
- **Area:** unit-test, backend
- **Priority:** P2
- **Depends on:** DEV-402

---

### UT-009 — Frontend: tenant resolution composable

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Vitest tests for host → slug mapping
- [ ] Apex vs subdomain vs unknown host cases

**Observações:**
- **Area:** unit-test, frontend
- **Priority:** P1
- **Depends on:** DEV-104

---

### UT-010 — Frontend: contact form component

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Client validation before submit
- [ ] Submit disabled while loading
- [ ] Success/error states rendered

**Observações:**
- **Area:** unit-test, frontend
- **Priority:** P2
- **Depends on:** DEV-107

---

### UT-011 — Frontend: admin artwork form validation

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Required fields enforced
- [ ] Slug format validation

**Observações:**
- **Area:** unit-test, frontend
- **Priority:** P2
- **Depends on:** DEV-204

---

## Integration tests

Activities for cross-layer tests with real or containerized dependencies.

---

### IT-001 — Test infrastructure setup

**Descrição:**
Integration test infrastructure on top of DEV-005b ([docs/BACKLOG.md](./BACKLOG.md)): WebApplicationFactory, Testcontainers Postgres (or CI service container).

**Critérios de aceitação:**
- [ ] `OnlinePortfolio.Api.Tests` project scaffolded (DEV-005b): xUnit, Moq, FluentAssertions, coverlet.collector
- [ ] `Microsoft.AspNetCore.Mvc.Testing` + `WebApplicationFactory` configured
- [ ] Testcontainers Postgres (local) or GitHub Actions Postgres service (CI)
- [ ] Runs in GitHub Actions CI via `dotnet test`
- [ ] DB migrated/seeded per test collection or fixture
- [ ] Isolated from production Supabase

**Observações:**
- **Area:** integration-test, devops
- **Priority:** P0
- **Depends on:** DEV-005b, DEV-004

---

### IT-011 — GET /auth/me integration

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Owner receives correct tenant in response
- [ ] PlatformAdmin receives null tenant
- [ ] Missing Authorization header → 401
- [ ] Invalid JWT → 401

**Observações:**
- **Area:** integration-test, security
- **Priority:** P0
- **Depends on:** IT-001, DEV-154

---

### IT-012 — Login tenant isolation

**Descrição:**
—

**Critérios de aceitação:**
- [ ] User A (tenant ana) `/auth/me` never returns tenant joao data
- [ ] Dois usuários no mesmo tenant compartilham tenant_id; usuário de outro tenant não vê dados cruzados

**Observações:**
- **Area:** integration-test, security
- **Priority:** P0
- **Depends on:** IT-011, DEV-152

---

### IT-013 — PlatformAdmin tenant user management

**Descrição:**
—

**Critérios de aceitação:**
- [ ] PlatformAdmin convida usuário no tenant A → linha ativa após accept-invite
- [ ] Owner of tenant A cannot call invite/list endpoints (403)
- [ ] PlatformAdmin can list all users for tenant A and B separately
- [ ] Deactivated user gets 403 on `/auth/me`

**Observações:**
- **Area:** integration-test, security
- **Priority:** P1
- **Depends on:** IT-011, DEV-161

---

### IT-002 — Public API: tenant isolation (read)

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Tenant A slug returns only A's published artworks
- [ ] Tenant B slug returns only B's data
- [ ] Cross-tenant artwork ID via wrong slug returns 404

**Observações:**
- **Area:** integration-test, security
- **Priority:** P0
- **Depends on:** IT-001, DEV-103

---

### IT-003 — Public API: unpublished content hidden

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Draft artwork not in public list or detail
- [ ] Published artwork visible

**Observações:**
- **Area:** integration-test
- **Priority:** P0
- **Depends on:** IT-002

---

### IT-004 — Admin API: IDOR prevention

**Descrição:**
—

**Critérios de aceitação:**
- [ ] User of tenant A cannot GET/PUT/DELETE tenant B artwork by ID
- [ ] Returns 403 or 404 (consistent policy documented)

**Observações:**
- **Area:** integration-test, security
- **Priority:** P0
- **Depends on:** IT-001, DEV-203

---

### IT-005 — Admin API: JWT + role enforcement

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Unauthenticated request → 401
- [ ] Valid JWT for user without User row → 403
- [ ] Owner can CRUD own tenant artworks

**Observações:**
- **Area:** integration-test, security
- **Priority:** P0
- **Depends on:** IT-001, DEV-154

---

### IT-006 — Contact form end-to-end (mock Resend)

**Descrição:**
—

**Critérios de aceitação:**
- [ ] POST contact → email service invoked with tenant ContactEmail
- [ ] Rate limit returns 429 after threshold (if implemented)

**Observações:**
- **Area:** integration-test
- **Priority:** P1
- **Depends on:** IT-001, DEV-107

---

### IT-007 — EF migrations apply cleanly

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Fresh DB + `dotnet ef database update` succeeds in CI
- [ ] Idempotent re-run documented

**Observações:**
- **Area:** integration-test, database
- **Priority:** P1
- **Depends on:** IT-001

---

### IT-008 — Storage upload metadata flow

**Descrição:**
—

**Critérios de aceitação:**
- [ ] API rejects metadata for path outside `tenants/{tenantId}/`
- [ ] Valid path persisted with correct ArtworkId link
- [ ] Optional: Supabase local stack or mocked Storage client

**Observações:**
- **Area:** integration-test
- **Priority:** P1
- **Depends on:** DEV-302, IT-001

---

### IT-009 — Nuxt server proxy integration

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Server route returns API response for tenant slug
- [ ] Error from API propagated correctly

**Observações:**
- **Area:** integration-test, frontend
- **Priority:** P2
- **Depends on:** DEV-106

---

### IT-010 — Multi-tenant subdomain routing (E2E smoke)

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Playwright or similar: two tenant URLs show different content
- [ ] Runs against preview/staging environment (not required on every PR)

**Observações:**
- **Area:** integration-test
- **Priority:** P2
- **Depends on:** DEV-105, DEV-011

---

## Security

Dedicated security activities (beyond tests). Cross-reference [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

---

### SEC-001 — Secrets management audit

**Descrição:**
—

**Critérios de aceitação:**
- [ ] No secrets in git history or `.env` committed
- [ ] Service role key only on Render
- [ ] Anon key only in Vercel public env
- [ ] `frontend/.env.example` has placeholders only
- [ ] GitHub Secrets documented in EXTERNAL_PROVIDERS

**Observações:**
- **Area:** security, devops
- **Priority:** P0
- **Depends on:** DEV-001

---

### SEC-002 — CORS and API exposure

**Descrição:**
—

**Critérios de aceitação:**
- [ ] CORS restricted to known origins OR Nuxt proxy is primary path
- [ ] Swagger disabled in production (or auth-protected)
- [ ] No stack traces in production error responses

**Observações:**
- **Area:** security, backend
- **Priority:** P0
- **Depends on:** DEV-003, DEV-106

---

### SEC-003 — TenantId injection prevention

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Admin endpoints ignore client-supplied `TenantId` for authorization
- [ ] Tenant always from JWT + User row
- [ ] Code review checklist item documented

**Observações:**
- **Area:** security, backend
- **Priority:** P0
- **Depends on:** DEV-102, DEV-203

---

### SEC-004 — Input validation & output encoding

**Descrição:**
—

**Critérios de aceitação:**
- [ ] FluentValidation or DataAnnotations on all write DTOs
- [ ] Max length on text fields (bio, description, contact message)
- [ ] Nuxt escapes user content in templates (Vue default + audit)

**Observações:**
- **Area:** security, backend, frontend
- **Priority:** P1
- **Depends on:** DEV-103, DEV-203

---

### SEC-005 — Rate limiting

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Contact endpoint rate limited by IP (and/or tenant slug)
- [ ] Public read endpoints have sensible limits (optional CDN/cache first)
- [ ] 429 response shape consistent

**Observações:**
- **Area:** security, backend
- **Priority:** P1
- **Depends on:** DEV-107

---

### SEC-006 — Supabase Storage RLS review

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Anonymous cannot write to any bucket
- [ ] Authenticated user cannot write outside own tenant path
- [ ] RLS policies documented in repo (`supabase/policies.sql` or docs)

**Observações:**
- **Area:** security, infra
- **Priority:** P0
- **Depends on:** DEV-300

---

### SEC-007 — Auth hardening (Identity + JWT)

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Public sign-up endpoints disabled (invite-only)
- [ ] Password policy + lockout (Identity options)
- [ ] Roles só via `RoleManager` / `AspNetUserRoles` (sem role duplicada em coluna)
- [ ] JWT expiry and refresh policy documented
- [ ] `Jwt__Secret` rotation procedure documented

**Observações:**
- **Area:** security, backend
- **Priority:** P1
- **Depends on:** DEV-153

---

### SEC-008 — Dependency scanning in CI

**Descrição:**
—

**Critérios de aceitação:**
- [ ] `dotnet list package --vulnerable` or Dependabot enabled
- [ ] `npm audit` in CI (warn or fail on high severity — policy documented)
- [ ] GitHub Dependabot alerts enabled on repo

**Observações:**
- **Area:** security, devops
- **Priority:** P1
- **Depends on:** DEV-006

---

### SEC-009 — Security headers (frontend)

**Descrição:**
—

**Critérios de aceitação:**
- [ ] `vercel.json` or Nuxt route rules set CSP baseline, X-Frame-Options, etc.
- [ ] Verified on production deploy

**Observações:**
- **Area:** security, frontend
- **Priority:** P2
- **Depends on:** DEV-010

---

### SEC-010 — Audit log for platform admin (optional)

**Descrição:**
—

**Critérios de aceitação:**
- [ ] Platform tenant create/update logged with timestamp and actor
- [ ] Logs to structured stdout (Render) or dedicated table

**Observações:**
- **Area:** security, backend
- **Priority:** P3
- **Depends on:** DEV-108

---

### SEC-011 — Pre-release security checklist

**Descrição:**
—

**Critérios de aceitação:**
- [ ] All [docs/ARCHITECTURE.md](./ARCHITECTURE.md) mandatory items checked
- [ ] IT-004 IDOR tests green
- [ ] Manual smoke: two tenants cannot see each other's admin data
- [ ] Sign-off recorded before first paying customer

**Observações:**
- **Area:** security, docs
- **Priority:** P1
- **Depends on:** SEC-001 through SEC-008

---

## Sprint plan (ciclos fixos de 2 semanas)

**Cadência:** sprints de **14 dias**, início todo **domingo**.  
**Sprint 1 começou:** 2026-06-21 (domingo).

| Sprint | Período | Foco |
|--------|---------|------|
| **1** | 2026-06-21 → 2026-07-04 | Foundation + bootstrap cloud (Epic 0) |
| **2** | 2026-07-05 → 2026-07-18 | Fechar Epic 0 (deploy front, DNS, Resend) |
| **3** | 2026-07-19 → 2026-08-01 | Login MVP (Epic 1.5) |
| **4** | 2026-08-02 → 2026-08-15 | Site público por tenant (Epic 1) |
| **5** | 2026-08-16 → 2026-08-29 | Contato + hardening |
| **6** | 2026-08-30 → 2026-09-12 | Admin CRUD pós-login |
| **7** | 2026-09-13 → 2026-09-26 | Uploads (Fase 3) |

---

### Sprint 1 — 2026-06-21 → 2026-07-04 — Foundation & prod bootstrap

**Objetivo:** monorepo, dev local, CI/CD backend, Supabase prod, API no Render, Vercel previews.

#### ✅ Concluído (início sprint 1 — até 21/06)

| Issue | Notas |
|-------|--------|
| DEV-000 | Domínio `onlineportfolio.com.br` |
| DEV-001 | Monorepo scaffold |
| DEV-002 | Docker Compose |
| DEV-003 | API skeleton + health + Dockerfile |
| DEV-004 | EF Core + DbContext + factory migrate *(migration `Initial` aplicada)* |
| DEV-005 | Nuxt 3 + hosts + dark mode estrutural |
| DEV-005b | Projeto de testes xUnit + coverage script |
| DEV-012 | Repo GitHub + apps Render/Vercel + branch protection |
| DEV-013 | Workspace Linear |
| DEV-006 | `ci-backend.yml` + `ci-frontend.yml` + branch protection + doc redeploy manual |
| DEV-007 | `deploy-backend.yml` + migrate prod + Render After CI Checks Pass |
| DEV-008 | Supabase prod `online-portfolio-db-prod` |
| DEV-014 | Resend — conta + API key + Render env |
| DEV-009 | API Render prod — env vars, `/health`, After CI Checks Pass |
| DEV-010 | Vercel `online-portfolio-web` — previews, env vars, prod auto-deploy off |
| DEV-007b | `deploy-frontend.yml` + GitHub Secrets `VERCEL_*` + `workflow_dispatch` |

#### ⬜ Restante Sprint 1 (até 04/07)

| Issue | Prioridade |
|-------|------------|
| DEV-011 | *(stretch)* DNS apex + `api.` + DKIM Resend |

**Fora do sprint 1:** DEV-008b (Supabase dev opcional) · Epic 1+

---

### Sprint 2 — 2026-07-05 → 2026-07-18 — Epic 0 done + DNS

**Objetivo:** produção fechada (front + API + domínios), pronto para features.

DEV-011 → SEC-001 → UT-003 → IT-006

**Critério de saída:** `onlineportfolio.com.br` + `app.` no Vercel, `api.` no Render, e-mail DKIM OK, deploys só via Actions.

---

### Sprint 3 — 2026-07-19 → 2026-08-01 — Login MVP (Epic 1.5)

DEV-150 → DEV-151 → DEV-152 → DEV-153 → DEV-154 → DEV-155 → DEV-156 → DEV-157 → DEV-158 → DEV-159 → DEV-161 → DEV-162 → DEV-160 → UT-012 → IT-011 → IT-012 → IT-013

---

### Sprint 4 — 2026-08-02 → 2026-08-15 — Site público por tenant (Epic 1)

DEV-100 → DEV-101 → DEV-102 → DEV-103 → DEV-104 → DEV-105 → DEV-106 → UT-002 → IT-001 → IT-002

---

### Sprint 5 — 2026-08-16 → 2026-08-29 — Contato + hardening

DEV-107 → DEV-109 → SEC-007 → SEC-003 → UT-005 → SEC-011 *(parcial)*

---

### Sprint 6 — 2026-08-30 → 2026-09-12 — Admin CRUD

DEV-203 → DEV-204 → DEV-205 → IT-004 → IT-005

---

### Sprint 7 — 2026-09-13 → 2026-09-26 — Uploads

DEV-300 → DEV-301 → DEV-302 → DEV-303 → SEC-006 → IT-008

---

### Later (backlog)

DEV-008b · DEV-207 · DEV-400+ · DEV-403 · DEV-404 · IT-010 · SEC-009 · SEC-010 · DEV-108 · DEV-206 · DEV-304 · DEV-305

---

*Last updated: 2026-06-21 — Sprint 1 desde 21/06; DEV-010 done*

---

