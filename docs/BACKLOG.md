# Development Backlog — Online Portfolio Platform

Detailed activity list for building the multi-tenant artist portfolio SaaS.

**Execution:** issues in [Linear](https://linear.app) (import done). **Documentation:** this file remains the source of truth for agents and PRs — keep `DEV-xxx` in commits and descriptions. **Language:** English for backlog text; pt-BR only for quoted product UI — [LANGUAGE.md](./LANGUAGE.md).

**Related:** [docs/ARCHITECTURE.md](./ARCHITECTURE.md) · [docs/FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md) · [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) · [docs/DATABASE.md](./DATABASE.md)

**Frontend (surfaces + tenant UI):** [docs/ARCHITECTURE.md](./ARCHITECTURE.md) — scope in DEV-005, DEV-104, DEV-105, DEV-109, [DEV-110](#dev-110--vercel-web-analytics) and Epic 1.5 (DEV-155–158).

**Platform domain:** `onlineportfolio.com.br` — ✅ registered + **prod live** (apex, `app.`, `api.`, wildcard `*.`, Resend DKIM/DMARC)

**Domain map:** [docs/ARCHITECTURE.md](./ARCHITECTURE.md) — public site per tenant (`ana.`, `mark.`), standardized admin at `app.`, BFF via Nuxt → .NET API.

**Current phase:** Epic 0 ✅ complete — next: **Epic 1.5** (login MVP at `app.`).

---

## How to use with Linear

| BACKLOG.md | Linear |
|---|---|
| `## Epic` / Phase heading | **Project** |
| `### DEV-xxx` | **Issue** (title `DEV-xxx — …`) |
| **Notes** → `Area` | **Labels** (`backend`, `frontend`, `infra`, …) |
| **Notes** → `Priority` | **Priority** |
| **Notes** → `Depends on` | **Blocked by** |
| **Acceptance criteria** | Checklist in description |

**Sync:** when work is done, mark Done in Linear and update checkboxes here when useful for docs.

### Standard issue format

All issues (`DEV-`, `UT-`, `IT-`, `SEC-`) **always** follow this structure (4 blocks):

```markdown
### DEV-xxx — Short title

**Description:**
One or more sentences: what, why, links to runbooks/ADRs.

**Acceptance criteria:**
- [ ] Verifiable item 1
- [ ] Verifiable item 2

**Notes:**
- **Phase:** 0
- **Area:** backend
- **Priority:** P1
- **Depends on:** DEV-001
- **Status:** ✅ Done

**Notes:**





(closing notes, follow-ups, out-of-scope for the ticket, etc.)
```

| Block | Linear |
|---|---|
| `### DEV-xxx — …` | **Title** |
| **Description** | Issue body (opening) |
| **Acceptance criteria** | Checklist in description |
| **Notes** | Metadata (`Phase`, `Area`, …) + notes; map to labels / blocked by |

**Re-export CSV (optional):** `python scripts/generate-linear-import.py` → `docs/BACKLOG_LINEAR.csv` (gitignored). CSV labels use `", "` between values — required by the Linear importer.

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

## Epic 0 — Foundation & tooling ✅

**Status:** complete (2026-06-23) — monorepo, Docker, CI/deploy Actions, Supabase prod, API Render, Vercel, DNS/HTTPS, Resend `mail@`. **Next:** [Epic 1.5](#epic-15--multi-tenant-db--admin-login--add-user-mvp).

### Provider setup index

**Account and configuration** activities aligned with [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md). Local code (DEV-001–005) can run in parallel with accounts (DEV-012–014).

| Order | Provider | Issue | When |
|---|---|---|---|
| 1 | Registro.br | [DEV-000](#dev-000--register-domain-) | ✅ domain purchased |
| 2 | GitHub | [DEV-012](#dev-012--github-repository--platform-integrations) | ✅ Done |
| 3 | Linear | [DEV-013](#dev-013--linear-workspace) | ✅ Done |
| 4 | Supabase | [DEV-008](#dev-008--supabase-production-project) · [DEV-008b](#dev-008b--supabase-dev-project) | ✅ prod (`online-portfolio-db-prod`); dev optional |
| 5 | Render | [DEV-009](#dev-009--render-api-deployment) | ✅ Done — `api.onlineportfolio.com.br` |
| 6 | Resend | [DEV-014](#dev-014--resend-account--api-key) → [DEV-107](#dev-107--contact-form--resend) | ✅ account + verified domain + `mail@` on Render; code → Epic 1 |
| 7 | Vercel | [DEV-010](#dev-010--vercel-frontend-deployment) | ✅ Done — `online-portfolio-web` |
| 8 | DNS + email DNS | [DEV-011](#dev-011--dns--https-production) | ✅ Done — apex/`app.`/`api.` + Resend DKIM/DMARC |
| 9 | GitHub Actions | [DEV-006](#dev-006--github-actions-ci-pr--separate-workflows) ✅ · [DEV-007](#dev-007--github-actions-deploy-pipeline-backend) ✅ · [DEV-007b](#dev-007b--github-actions-deploy-pipeline-frontend) ✅ | ✅ CI + deploy backend + frontend |
| — | Google Workspace | [DEV-404](#dev-404--google-workspace-operator-inbox) | Optional, post-launch |
| — | Stripe / billing | [DEV-403](#dev-403--billing--subscriptions-optional--skip-until-charging) | **Optional** — skip in v1 |

---

### DEV-000 — Register domain ✅

**Description:**
Register `onlineportfolio.com.br` at Registro.br. Production DNS configured in [DEV-011](#dev-011--dns--https-production). Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Acceptance criteria:**
- [x] Domain status **Active** in Registro.br panel
- [ ] Renewal date noted; renewal reminder configured
- [ ] Registrant (CPF/CNPJ) and login credentials saved securely (password manager)
- [x] DNS: nameservers Registro.br → Vercel (`ns1`/`ns2.vercel-dns.com`) — [DEV-011](#dev-011--dns--https-production)

**Notes:**
- **Phase:** 0 — Foundation
- **Area:** infra
- **Priority:** P0
- **Status:** ✅ **Done** — domain purchased at Registro.br

---

### DEV-001 — Monorepo scaffold ✅

**Description:**
Create repository structure per architecture doc.

**Acceptance criteria:**
- [x] `frontend/` (Nuxt 3), `backend/` (ASP.NET Web API), `docs/`
- [x] Root `README.md` with local dev instructions (stub OK)
- [x] `.gitignore` for Node, .NET, env files
- [x] `.env.example` in `frontend/`

**Notes:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P0
- **Status:** ✅ Done

---

### DEV-002 — Docker Compose (local dev) ✅

**Description:**
One-command local stack: Postgres + API + Nuxt with volume mounts for hot reload.

**Acceptance criteria:**
- [x] `docker compose up` starts Postgres, backend, frontend
- [x] API reachable at documented local URL
- [x] Nuxt dev server with HMR
- [x] Seed script creates **2 tenants** for isolation testing

**Notes:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P0
- **Depends on:** DEV-001
- **Status:** ✅ Done

---

### DEV-003 — Backend API skeleton ✅

**Description:**
ASP.NET Core Web API with health check, Swagger, Serilog, global exception handler.

**Acceptance criteria:**
- [x] `GET /health` returns 200
- [x] OpenAPI at `/swagger` (dev only or configurable)
- [x] Structured JSON error responses
- [x] Dockerfile (multi-stage) in `backend/`
- [x] `Program.cs` reads config from environment variables

**Notes:**
- **Phase:** 0
- **Area:** backend
- **Priority:** P0
- **Depends on:** DEV-001
- **Status:** ✅ Done

---

### DEV-004 — EF Core + PostgreSQL setup ✅

**Description:**
DbContext, Npgsql provider, initial migration infrastructure. Local connection to Compose Postgres.

**Acceptance criteria:**
- [x] `ApplicationDbContext` registered in DI
- [x] Connection string from config (`ConnectionStrings__Default`)
- [x] `ApplicationDbContextFactory` + `ConnectionStrings:Migration` for `dotnet ef` (session pooler `:5432` prod / `localhost` local)
- [ ] `dotnet ef migrations add Initial` works locally *(run manually — see README)*
- [x] `dotnet ef database update` applies against Compose Postgres *(local/prod — migration `Initial` applied in prod via DEV-008)*

**Notes:**
- **Phase:** 0
- **Area:** database
- **Priority:** P0
- **Depends on:** DEV-003, DEV-002
- **Status:** ✅ **Done** — `Initial` migration + DbContext/factory; applied in prod (DEV-008)

### DEV-005 — Nuxt 3 frontend skeleton ✅

**Description:**
Nuxt 3 app with TypeScript, basic layout, env config for API base URL. Includes foundation per [docs/ARCHITECTURE.md](./ARCHITECTURE.md): `app/` / `platform/` / `public/` folders, layouts per surface, `resolve-host` middleware, tenant composables.

**Acceptance criteria:**
- [x] Nuxt 3 + TypeScript runs locally and in Docker
- [x] `NUXT_PUBLIC_API_BASE` and platform host env vars defined
- [x] Default layout + error page
- [x] Nitro preset compatible with Vercel
- [x] Component structure per surface (ADR-016) — see [docs/FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md)
- [x] Structural dark mode (`surface-dark`, `assets/css/surfaces/dark.css`) — everything except public tenant site

**Notes:**
- **Phase:** 0
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-001
- **Status:** ✅ Done

---

### DEV-005b — Backend test project scaffold ✅

**Description:**
Create `OnlinePortfolio.Api.Tests` xUnit project with standard .NET test stack. Folders `Unit/` and `Integration/` already exist; wire project into solution.

**Acceptance criteria:**
- [x] `OnlinePortfolio.Api.Tests.csproj` in `backend/OnlinePortfolio.Api.Tests/`
- [x] Packages: **xUnit**, **Moq**, **FluentAssertions**, **coverlet.collector**
- [x] Project reference → `OnlinePortfolio.Api`
- [x] Added to `OnlinePortfolio.Api.slnx`
- [x] At least one smoke test green
- [x] `dotnet test` from `backend/` succeeds
- [x] Stack documented in [docs/ARCHITECTURE.md](./ARCHITECTURE.md)
- [x] Script `scripts/coverage-backend.ps1` (HTML report); `TestResults/` gitignored

**Notes:**
- **Phase:** 0
- **Area:** backend, unit-test
- **Priority:** P0
- **Depends on:** DEV-003
- **Status:** ✅ **Done**

---

### DEV-012 — GitHub repository & platform integrations ✅

**Description:**
Create `online-portfolio` repo on GitHub, first monorepo push, connect Render and Vercel via GitHub App, prepare secrets for Actions. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Acceptance criteria:**
- [x] Repo created and monorepo code on `main`
- [x] Render GitHub App installed with repo access
- [x] Vercel GitHub App installed — PR previews **on**; production auto-deploy **off** (Only build pre-production)
- [x] GitHub Actions secrets prepared (placeholders OK until DEV-008/010): `SUPABASE_MIGRATION_CONNECTION_STRING`, `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID`
- [x] (Recommended) Branch protection on `main` requiring Backend CI + Frontend CI — closed in [DEV-006](#dev-006--github-actions-ci-pr--separate-workflows)

**Notes:**
- **Phase:** 0
- **Area:** infra, devops
- **Priority:** P0
- **Depends on:** DEV-001
- **Status:** ✅ **Done**

---

### DEV-013 — Linear workspace ✅

**Description:**
Linear workspace for `DEV-xxx` issues. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Acceptance criteria:**
- [x] Account + workspace created ([linear.app/signup](https://linear.app/signup))
- [x] `Area` labels configured (`backend`, `frontend`, `infra`, …)
- [x] DEV-000 marked **Done**; DEV-001 (or next active issue) created
- [ ] (Optional) GitHub integration → `online-portfolio` repo

**Notes:**
- **Phase:** 0
- **Area:** docs, infra
- **Priority:** P2
- **Status:** ✅ **Done**

---

### DEV-014 — Resend account & API key ✅

**Description:**
Resend account and API key for transactional send (`mail@onlineportfolio.com.br`). **Account setup only** — API integration in [DEV-107](#dev-107--contact-form--resend); verified domain in [DEV-011](#dev-011--dns--https-production). Decision: [ADR-017](./ADR-017-resend-transactional-email.md). Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) section 8.6 (smoke test prod).

**Acceptance criteria:**
- [x] Resend account created and verified
- [x] API key `portfolio-api-prod` created
- [x] Key stored in password manager
- [x] `Resend__ApiKey`, `Resend__FromEmail`, `Resend__FromName` on Render (`mail@` confirmed in prod)
- [x] Domain `onlineportfolio.com.br` verified — DKIM/SPF/MX/DMARC (DEV-011)
- [x] `Resend__FromEmail` = `mail@onlineportfolio.com.br` documented and applied on Render
- [x] Smoke test sent with verified domain (section 8.6 EXTERNAL_PROVIDERS)

**Notes:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P1
- **Status:** ✅ **Done** — account, API key, verified domain, `mail@` on Render, prod smoke test

---

### DEV-006 — GitHub Actions CI (PR) — separate workflows ✅

**Description:**
Two CI workflows on PR — **separate** by stack (not a single `ci.yml`): `ci-backend.yml` and `ci-frontend.yml`, with path filters. **Scope:** CI only on PR/push; deploy pipelines = [DEV-007](#dev-007--github-actions-deploy-pipeline-backend) / [DEV-007b](#dev-007b--github-actions-deploy-pipeline-frontend).

**Acceptance criteria:**
- [x] `ci-backend.yml` runs on `pull_request` to `main` (paths: `backend/**`, `docs/DATABASE.md`, …)
- [x] `ci-frontend.yml` runs on `pull_request` to `main` (paths: `frontend/**`, …)
- [x] `push` → `main` with path filters + skip via `dorny/paths-filter` when the commit does not touch the stack
- [x] Backend: `dotnet test` (+ build); uses xUnit stack ([docs/ARCHITECTURE.md](./ARCHITECTURE.md))
- [x] Frontend: `npm run lint` + `npm run test:coverage` (Vitest; expand in UT-009+)
- [x] Status checks **Backend CI** and **Frontend CI** visible on PR
- [x] PR comments: **Backend coverage** / **Frontend coverage** (sticky; only when the respective workflow runs)
- [x] Documented cross-stack component review before merge ([docs/AGENT_GUIDE.md](./AGENT_GUIDE.md))
- [x] (Recommended) Branch protection on `main` requiring **Backend CI** + **Frontend CI**
- [x] Document manual redeploy via Actions (`workflow_dispatch` on deploy pipelines) in [docs/DEV_COMMANDS.md](./DEV_COMMANDS.md)

**Notes:**
- **Phase:** 0
- **Area:** devops
- **Priority:** P1
- **Depends on:** DEV-003, DEV-005, DEV-012
- **Status:** ✅ **Done**

**Done notes (2026-06-23):**
- `ci-backend.yml` + `ci-frontend.yml` with `dorny/paths-filter` on PR and push → `main`
- Branch protection on `main` (Backend CI + Frontend CI)
- Manual redeploy documented in DEV_COMMANDS (`workflow_dispatch` on DEV-007 / DEV-007b)

---

### DEV-007 — GitHub Actions deploy pipeline (backend) ✅

**Description:**
`deploy-backend.yml` on push to `main` — backend test → EF migrate (Supabase session pooler `:5432`) → pass status for Render After CI Checks Pass.

**Acceptance criteria:**
- [x] `SUPABASE_MIGRATION_CONNECTION_STRING` in GitHub Secrets (Session pooler `:5432`, IPv4)
- [x] Migrations run before deploy status succeeds
- [x] Render **After CI Checks Pass** documented and enabled (gate on **Backend Deploy** check)
- [x] `workflow_dispatch` on `deploy-backend.yml` (manual redeploy: test + migrate without empty commit)

**Notes:**
- **Phase:** 0
- **Area:** devops
- **Priority:** P1
- **Depends on:** DEV-006, DEV-004
- **Status:** ✅ **Done**

**Done notes (2025-06-21):**
- Workflow `.github/workflows/deploy-backend.yml` — trigger `push` → `main` (paths `backend/**`, `docs/DATABASE.md`)
- Job **Backend Deploy**: `dotnet test` → `dotnet ef database update` (secret `SUPABASE_MIGRATION_CONNECTION_STRING`)
- `dotnet-ef` 10.0.4 in `backend/.config/dotnet-tools.json`
- Render: Auto-Deploy On + **After CI Checks Pass**; Root Directory `backend`; first prod deploy green
- Fix CI: `ci-backend.yml` / `ci-frontend.yml` report status on every PR (`dorny/paths-filter` + internal skip)
- Docs: connection strings (Transaction `:6543` runtime, Session `:5432` migrate, no direct in pipeline) — [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) section 6.2 · [docs/ARCHITECTURE.md](./ARCHITECTURE.md) section 7

---

### DEV-007b — GitHub Actions deploy pipeline (frontend) ✅

**Description:**
`deploy-frontend.yml` on push to `main` — frontend lint/test → `vercel deploy --prod`. Disable Vercel production auto-deploy; PR previews stay on Vercel GitHub App.

**Acceptance criteria:**
- [x] `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` in GitHub Secrets
- [x] `deploy-frontend.yml` — lint/test → `vercel pull` + `vercel env pull` → `npm run build` → stage `.vercel/output` → `vercel deploy --prebuilt --prod`
- [x] Workflow runs on push to `main` (paths: `frontend/**`, workflow file)
- [x] Production auto-deploy **disabled** on Vercel (`Only build pre-production`; DEV-010)
- [x] First **Frontend Deploy** green on `main`
- [x] PR preview deploys still work via Vercel integration (regression check)
- [x] `workflow_dispatch` on `deploy-frontend.yml` (manual redeploy after changing env on Vercel — avoids dashboard Ignored Build Step)

**Notes:**
- **Phase:** 0
- **Area:** devops
- **Priority:** P1
- **Depends on:** DEV-006, DEV-005, DEV-010
- **Status:** ✅ **Done** — prod `online-portfolio-web-xi.vercel.app` via Actions; previews OK (e.g. `*-git-*-marcelomborges-dev.vercel.app`)

---

### DEV-008 — Supabase production project ✅

**Description:**
Create Supabase **production** project `online-portfolio-db-prod`; store Transaction + Session pooler strings and API keys for Render + CI. API/front deploy remains **prod only** ([docs/ARCHITECTURE.md](./ARCHITECTURE.md)) — dev DB is [DEV-008b](#dev-008b--supabase-dev-project). Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Acceptance criteria:**
- [x] Prod project `online-portfolio-db-prod` in **US East** (aligned with Render Virginia)
- [x] Pooler URI (6543, Transaction) → Render `ConnectionStrings__Default`
- [x] Session pooler URI (5432) → GitHub secret `SUPABASE_MIGRATION_CONNECTION_STRING` (CI migrate prod; IPv4)
- [x] Project URL + **service_role** / secret API key in password manager (Render env; Storage Phase 3+)
- [x] Migration `Initial` applied in prod (`dotnet ef database update` locally)
- [x] Checklist [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) (prod)

**Notes:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P1
- **Status:** ✅ **Done** — project `online-portfolio-db-prod` (US East)

**Done notes (2025-06-21):**
- Project ref: `mfxuthlwrfjscxnnjeud` · US East region · pooler Transaction `aws-1-us-east-1.pooler.supabase.com:6543`
- Password-free templates in `backend/OnlinePortfolio.Api/appsettings.json` (`Default` = Transaction pooler `:6543`, `Migration` = Session pooler `:5432`)
- `__EFMigrationsHistory` in prod: `20260622012938_Initial` (EF Core 10.0.4)
- GitHub secret `SUPABASE_MIGRATION_CONNECTION_STRING` configured — used by [DEV-007](#dev-007--github-actions-deploy-pipeline-backend) ✅
- `Supabase__Url` / `Supabase__ServiceRoleKey` on Render **deferred** until Storage (Phase 3)

---

### DEV-008b — Supabase dev project

**Description:**
Second Supabase project **`online-portfolio-db-dev`** — cloud Postgres for **optional** local development (alternative to Docker Compose Postgres). Does **not** deploy dev API/front; only replaces the local database when you point `appsettings.Development.json` / `appsettings.json` to dev. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Acceptance criteria:**
- [ ] Dev project `online-portfolio-db-dev` created (same region as prod, e.g. US East)
- [ ] Session pooler (5432, migrate) + Transaction pooler (6543, runtime) stored in **password manager**
- [ ] `appsettings.Development.json` / `appsettings.json` documented: switch `ConnectionStrings` between **local Docker** and **Supabase dev**
- [ ] Migrations applied on local dev (`dotnet ef database update` against `localhost` / Compose)
- [ ] [docs/DEV_COMMANDS.md](./DEV_COMMANDS.md) describes both modes (Compose vs Supabase dev)

**Notes:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P2
- **Depends on:** DEV-008 (recommended — same org/region)

**Note:** CI and Render always use prod. Dev Supabase is for the developer machine only.

---

### DEV-009 — Render API deployment ✅

**Description:**
Deploy backend Docker image to Render free tier; connect GitHub repo. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Acceptance criteria:**
- [x] Render account created; Docker web service (`backend/OnlinePortfolio.Api/Dockerfile`) connected to repo
- [x] Web service live on Render default URL
- [x] Health check `/health` configured and returning 200
- [x] Production env vars set (`ConnectionStrings__Default`, `Jwt__*`, `Resend__*` from DEV-014, `ASPNETCORE_*`)
- [x] **After CI Checks Pass** enabled (gate on `deploy-backend.yml` — DEV-007)
- [x] Custom domain `api.onlineportfolio.com.br` verified + HTTPS ([DEV-011](#dev-011--dns--https-production))

**Notes:**
- **Phase:** 0
- **Area:** devops
- **Priority:** P1
- **Depends on:** DEV-003, DEV-008, DEV-007, DEV-014
- **Status:** ✅ **Done** — prod deploy + domain `api.onlineportfolio.com.br`

---

### DEV-010 — Vercel frontend deployment ✅

**Description:**
Connect repo to Vercel; root directory `frontend`; PR previews enabled; **production deploy via `deploy-frontend.yml`** (DEV-007b). Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Acceptance criteria:**
- [x] Vercel account created; `online-portfolio-web` project imported from GitHub (`frontend/` root)
- [x] PR preview deploys enabled (tested — Vercel comment on PR)
- [x] Production auto-deploy **disabled** in Vercel (`Only build pre-production`; prod = Actions DEV-007b)
- [x] Env vars configured in Vercel dashboard (`NUXT_PUBLIC_*`, `NUXT_API_INTERNAL_BASE` → Render `*.onrender.com`)
- [x] Custom domains: apex, `app.`, wildcard `*.` — [DEV-011](#dev-011--dns--https-production)
- [x] `NUXT_API_INTERNAL_BASE` = `https://api.onlineportfolio.com.br` (post-DEV-011)

**Notes:**
- **Phase:** 0
- **Area:** devops
- **Priority:** P1
- **Depends on:** DEV-005, DEV-012
- **Status:** ✅ **Done** — deploy Ready; prod domains + `NUXT_API_INTERNAL_BASE` → `api.`; secrets in password manager (GitHub Secrets → DEV-007b)

---

### DEV-011 — DNS & HTTPS (production) ✅

**Description:**
Production DNS: Registro.br nameservers → Vercel; apex/`app.`/wildcard domains; CNAME `api.` → Render; Resend records (DKIM/SPF/MX) + DMARC on Vercel DNS; sender `mail@onlineportfolio.com.br`. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) sections 7.5, 8.6, 9, 10.4.

**Acceptance criteria:**
- [x] Nameservers `ns1.vercel-dns.com` / `ns2.vercel-dns.com` at Registro.br
- [x] Domains on Vercel with **Valid** status: apex `onlineportfolio.com.br`, `app.`, wildcard `*.onlineportfolio.com.br`
- [x] Test tenant subdomains (`ana.`, `joao.`) resolve via wildcard
- [x] HTTPS active on Vercel domains (automatic)
- [x] `api.onlineportfolio.com.br` on Render — verified domain + HTTPS
- [x] `GET https://api.onlineportfolio.com.br/health` → **Healthy**
- [x] `NUXT_API_INTERNAL_BASE` = `https://api.onlineportfolio.com.br` on Vercel + frontend redeploy
- [x] Resend: domain `onlineportfolio.com.br` **Verified** (region `sa-east-1`; DNS via Vercel integration)
- [x] Email DNS records on Vercel: `resend._domainkey` (DKIM), `send` (SPF), `send` (MX bounce), `_dmarc` (TXT)
- [x] Smoke test send with `mail@onlineportfolio.com.br` + `reply_to` (Resend API — section 8.6 EXTERNAL_PROVIDERS)
- [x] `Resend__FromEmail` = `mail@onlineportfolio.com.br` on Render
- [x] Checklist [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) sections 7.5, 8.4, 9, 10.4 updated

**Manual validation (expected behavior):**
- `dev.onlineportfolio.com.br` → unknown tenant error (slug `dev` not in seed) — confirms wildcard, not a DNS failure

**Notes:**
- **Phase:** 0
- **Area:** infra
- **Priority:** P1
- **Depends on:** DEV-000, DEV-009, DEV-010
- **Status:** ✅ **Done** — prod at `onlineportfolio.com.br` / `app.` / `{slug}.` (Vercel) + `api.` (Render) + email DKIM/DMARC + `mail@` on Render

**Done notes (2026-06-23):**
- `Resend__FromEmail` = `mail@onlineportfolio.com.br` confirmed on Render
- Resend API smoke test with `reply_to` (Insomnia) — Delivered
- DMARC `TXT` `_dmarc` on Vercel DNS

---

## Epic 1 — Public site per tenant (Phase 1)

**Goal:** landing + posts/gallery at `{slug}.onlineportfolio.com.br` (e.g. `ana.`, `mark.`). Admin is in Epic 1.5 (`app.`). See [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

---

### DEV-100 — Domain model: Tenant, Plan, TenantSettings

**Description:**
EF entities for platform-scoped tenant tables. **Superseded by Epic 1.5** if login MVP is built first — see DEV-150/151 and [docs/DATABASE.md](./DATABASE.md).

**Acceptance criteria:**
- [ ] `Tenant`, `Plan`, `TenantSettings` entities
- [ ] Unique index on `Tenant.Slug`, unique `CustomDomain` where not null
- [ ] Migration applied locally and documented for CI
- [ ] Seed: 2 tenants with distinct slugs

**Notes:**
- **Phase:** 1
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-004

---

### DEV-101 — Domain model: Artwork (+ optional Category)

**Description:**
Tenant-scoped artwork entity with publish flag and slug scoped per tenant.

**Acceptance criteria:**
- [ ] `Artwork` with `TenantId`, `IsPublished`, `PublishedAt`, `SortOrder`
- [ ] Unique `(TenantId, Slug)` on Artwork
- [ ] Optional `Category` entity if included in v1
- [ ] EF global query filter on `TenantId` (foundation)

**Notes:**
- **Phase:** 1
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-100

---

### DEV-102 — TenantContext middleware

**Description:**
Resolve tenant from route slug; expose `ITenantContext` for request scope.

**Acceptance criteria:**
- [ ] Middleware or endpoint filter resolves tenant by slug
- [ ] 404 for unknown/inactive tenant
- [ ] `TenantId` available to services and EF filters

**Notes:**
- **Phase:** 1
- **Area:** backend
- **Priority:** P0
- **Depends on:** DEV-100

---

### DEV-103 — Public API: tenant profile & artworks

**Description:**
Read-only public endpoints per [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**Acceptance criteria:**
- [ ] `GET /api/v1/tenants/{slug}/profile`
- [ ] `GET /api/v1/tenants/{slug}/artworks` (paginated, published only)
- [ ] `GET /api/v1/tenants/{slug}/artworks/{id}` (published only)
- [ ] OpenAPI documented
- [ ] No draft/unpublished data leaked

**Notes:**
- **Phase:** 1
- **Area:** backend
- **Priority:** P0
- **Depends on:** DEV-101, DEV-102

---

### DEV-104 — Nuxt tenant resolution middleware

**Description:**
Resolve tenant by `Host` — subdomain `{slug}.onlineportfolio.com.br` or custom domain. Host `app.*` and platform apex are **not** tenants. Surface architecture: [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**Acceptance criteria:**
- [x] Middleware: `{slug}.onlineportfolio.com.br` → tenant slug (`resolve-host.global.ts`)
- [x] Middleware: `app.*` → admin mode (no public tenant)
- [x] Middleware: `onlineportfolio.com.br` → platform landing
- [x] Dev local: slug configurable via env (`NUXT_PUBLIC_DEV_SURFACE`, `NUXT_PUBLIC_DEV_TENANT_SLUG`)
- [ ] Unknown tenant → 404 page (validate slug via API — DEV-103)
- [x] Composable exposes tenant context on public pages (`useRequestSurface`, `useTenantContext`)

**Notes:**
- **Phase:** 1
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-005, DEV-103
- **Status:** 🟡 Partial — missing 404 for unknown slug (API validation)

---

### DEV-105 — Public tenant pages (landing + posts/gallery)

**Description:**
Public site per tenant at `{slug}.onlineportfolio.com.br` — home/landing, post/artwork listing, detail, about. Data via Nuxt → API proxy. Shared base layout; per-tenant overrides in `components/public/tenants/{slug}/` — [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**Acceptance criteria:**
- [x] Landing/home per tenant (skeleton: `useTenantComponent('LandingHero')`, Ana/João examples, CSS themes)
- [ ] Post/artwork listing
- [ ] Detail page
- [ ] About/contact from `TenantSettings` (stub `/contact` + `PublicContactSection` exists)
- [ ] SSG or ISR with cache key including tenant slug
- [ ] Responsive layout (mobile-first)
- [ ] **No** admin/login routes on this host

**Notes:**
- **Phase:** 1
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-104, DEV-103
- **Status:** 🟡 Partial — landing skeleton + themes; real data and gallery pending (DEV-103)

---

### DEV-106 — Nuxt server proxy to API

**Description:**
Server routes proxy **all** calls (public + admin) to the API — BFF pattern. Browser does not access Render directly.

**Acceptance criteria:**
- [ ] `server/api/**` proxies to `api.onlineportfolio.com.br`
- [ ] Public and admin pages use proxy or server-side fetch
- [ ] Auth cookies forwarded in proxy (admin)
- [ ] API base URL not hardcoded in client bundle for sensitive paths

**Notes:**
- **Phase:** 1
- **Area:** frontend
- **Priority:** P1
- **Depends on:** DEV-105

---

### DEV-107 — Contact form + Resend

**Description:**
Contact form POST → API → Resend (`mail@`) → tenant `ContactEmail`. Requires [DEV-014](#dev-014--resend-account--api-key) (account) and verified domain in [DEV-011](#dev-011--dns--https-production) for prod. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) · [ADR-017](./ADR-017-resend-transactional-email.md).

**Acceptance criteria:**
- [ ] `POST /api/v1/tenants/{slug}/contact` with validation
- [ ] `IEmailService` + `ResendEmailService` (NuGet `Resend`)
- [ ] Templates in `EmailTemplates/` (contact, invite)
- [ ] `Resend__FromEmail` = `mail@onlineportfolio.com.br`
- [ ] Reply-To = visitor email (artist replies directly)
- [ ] Rate limiting on contact endpoint (basic)
- [ ] Contact form UI on public site (`pages/contact.vue` + `useTenantComponent('ContactSection')` stub; per-tenant override in `public/tenants/{slug}/`)
- [x] Prod: authenticated domain via [DEV-011](#dev-011--dns--https-production) ✅
- [ ] Honeypot or basic anti-spam field

**Notes:**
- **Phase:** 1
- **Area:** backend, frontend
- **Priority:** P1
- **Depends on:** DEV-103, DEV-014

---

### DEV-108 — Platform admin: seed / create tenants (API)

**Description:**
Minimal platform admin endpoints or seed-only for first 2 artists (full PlatformAdmin auth in Phase 2).

**Acceptance criteria:**
- [ ] `POST /api/v1/platform/tenants` (protected — API key or temporary auth for v1)
- [ ] Creates Tenant + TenantSettings defaults
- [ ] Document manual provisioning for first customers
- [ ] Seed data script for local dev (2 tenants + sample artworks)

**Notes:**
- **Phase:** 1
- **Area:** backend
- **Priority:** P1
- **Depends on:** DEV-100

---

### DEV-109 — Platform marketing page (apex)

**Description:**
Landing page at `onlineportfolio.com.br` when host is apex (not tenant subdomain). Content in `components/platform/` — [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**Acceptance criteria:**
- [x] Apex host shows platform marketing content (stub: `PlatformLandingHero` via `index.vue` + surface `platform`)
- [ ] Subdomain hosts show tenant gallery (depends on DEV-105)
- [ ] Clear CTA for artists (contact / waitlist)

**Notes:**
- **Phase:** 1
- **Area:** frontend
- **Priority:** P2
- **Depends on:** DEV-104

---

### DEV-110 — Vercel Web Analytics

**Description:**
Basic traffic metrics (page views) via [Vercel Web Analytics](https://vercel.com/docs/analytics) + `@vercel/analytics` package in Nuxt. **Optional** in v1 — does not block DEV-011. Measure visitors on **platform** (apex) and **public tenant sites** (`{slug}.*`); **exclude admin/login** (`app.*`) so internal use is not mixed with public traffic. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) section 9.5 · [ADR-018](./ADR-018-frontend-ui-motion-stack.md).

**Acceptance criteria:**
- [ ] **Vercel** → Project → **Analytics** → Web Analytics enabled on `online-portfolio-web` project
- [ ] `npm i @vercel/analytics` in `frontend/`; module registered in `nuxt.config.ts`
- [ ] Script loads **only** when `surface` is `platform` or `tenant` (conditional plugin — **not** on `app` / `/admin` / `/login`)
- [ ] Prod deploy via `deploy-frontend.yml`; page views appear in Vercel dashboard (wait ~30s; test navigation between routes)
- [ ] LGPD note: when client public sites are live, privacy policy must mention analytics (legal follow-up — outside this ticket's technical scope)

**Notes:**
- **Phase:** 1
- **Area:** frontend, infra
- **Priority:** P2
- **Depends on:** DEV-011, DEV-104
- **Do not use** for per-tenant product metrics in v1 (Vercel dashboard aggregates by deployment; manual filter by host/URL). Advanced per-tenant analytics → future phase (Plausible/Umami/events).

**Suggested implementation:**

```ts
// nuxt.config.ts — after adoption
modules: ['@vercel/analytics']
```

`.client.ts` plugin that mounts analytics only if `useRequestSurface().surface` ∈ `platform` | `tenant`.

---

## Epic 1.5 — Multi-tenant DB + admin login + add user (MVP)

**Goal:** Database + admin with **two essential features** (no gallery/artwork yet):

| # | Function | Who | Deliverable |
|---|---|---|---|
| 1 | **Login / logout** | PlatformAdmin + tenant users | DEV-154–158 |
| 2 | **Add user** (invite per tenant) | PlatformAdmin only | DEV-159, DEV-161, DEV-162 |

No artwork CRUD, full settings, or public site in this epic.

**Domains:** login and admin only at `app.onlineportfolio.com.br`; `{slug}.onlineportfolio.com.br` sites come in Epic 1 (public).

**Schema reference:** [docs/DATABASE.md](./DATABASE.md) — migration `InitialMultiTenantAndUsers`

### Admin MVP — functional scope

```text
PlatformAdmin (you)
  ├── Login → /admin or /platform
  ├── List tenants
  ├── Per tenant: list users
  └── Per tenant: invite user (email + role Owner/Editor)

Tenant user (Owner/Editor)
  ├── Login → /admin
  └── Tenant dashboard (placeholder — no add user in v1)
```

---

### DEV-150 — EF migration: multi-tenant + users ✅

**Description:**
First migration per DATABASE.md — `plans`, `tenants`, `tenant_settings`, `users` only (no artworks).

**Acceptance criteria:**
- [x] Migration `InitialMultiTenantAndUsers` created
- [x] Tables match [docs/DATABASE.md](./DATABASE.md) (columns, FKs, checks)
- [x] Indexes: `tenants.slug`, `users.email` UNIQUE, partial unique on `custom_domain`
- [x] `dotnet ef database update` works on Compose Postgres
- [x] Rollback (`dotnet ef migrations remove`) tested locally

**Notes:**
- **Phase:** 1.5 — Login + add user MVP
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-004
- **Status:** ✅ **Done** (2026-06-25)

**Done notes (2026-06-25):**
- Migration `20260625220843_InitialMultiTenantAndUsers` — `plans`, `tenants`, `tenant_settings` + Identity (`AspNetUsers`, `AspNetRoles`, …)
- Entities/Fluent API config delivered together (overlap DEV-151 — see issue)
- Removed `scripts/seed-dev.sql` mount in Compose (conflicted with EF); local reset = `docker compose down -v` + `dotnet ef database update`
- Validated locally: `database update`, `\dt`, rollback (`database update Initial` → reapply)

---

### DEV-151 — EF entities and configurations ✅

**Description:**
C# entities `Plan`, `Tenant`, `TenantSettings`, `User` with Fluent API / snake_case naming.

**Acceptance criteria:**
- [x] Entities in `backend/OnlinePortfolio.Api/Data/Entities/`
- [x] `ApplicationDbContext` DbSets registered
- [x] `ApplicationUser : IdentityUser<Guid>` with `TenantId`, `InvitedByUserId`, `IsActive` (no `Role` column — see AspNetRoles)
- [x] `ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`
- [x] Seed roles: `PlatformAdmin`, `Owner`, `Editor` via `RoleManager`
- [x] Constants or enum mirroring Identity role names
- [x] No global query filter on `User` that hides PlatformAdmin

**Notes:**
- **Phase:** 1.5
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-150
- **Status:** ✅ **Done** (2026-06-25)

**Done notes (2026-06-25):**
- Entities + Fluent API delivered in DEV-150; DEV-151 closes Identity DI + role seed
- `AddApplicationIdentity()` + idempotent `IdentityRoleSeeder` on startup (`Program.cs`)
- `AppRoles` — constants `PlatformAdmin`, `Owner`, `Editor`
- UT: `IdentityRoleSeederTests`

---

### DEV-152 — Dev seed: plans + two tenants ✅

**Description:**
Seed `Starter` plan + tenants `ana` and `joao` with empty `tenant_settings`.

**Acceptance criteria:**
- [x] Seed runs on local `docker compose up` or explicit `dotnet run --seed`
- [x] Two active tenants with distinct slugs
- [x] No artwork rows (table does not exist yet)
- [x] Seed PlatformAdmin user with password (dev credentials documented)

**Notes:**
- **Phase:** 1.5
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-151
- **Status:** ✅ **Done** (2026-06-26)

**Done notes (2026-06-26):**
- `DevDataSeeder` — seeder idempotente com dois overloads: dependências explícitas (testes) e entry point via `IServiceProvider` (Program.cs)
- Dados seedados: plano `Starter` (500 MB, 50 artworks), tenants `ana` / `joao` + `TenantSettings` vazio, usuário `PlatformAdmin`
- IDs fixos e determinísticos (`000...001`, `000...010`, `000...011`) — evitam drift entre resets locais
- Credenciais dev em `appsettings.Development.json`: `admin@onlineportfolio.com.br` / `Dev@12345`
- Seed chamado apenas em `Development` em `Program.cs`, após `IdentityRoleSeeder`
- `Microsoft.EntityFrameworkCore.InMemory` adicionado ao projeto de testes; 4 testes unitários verdes (`DevDataSeederTests`)

---

### DEV-153 — ASP.NET Identity full + JWT ✅

**Description:**
Full Identity — `AddIdentity`, `RoleManager`, EF stores, JWT on API. No Supabase Auth.

**Acceptance criteria:**
- [x] `AddIdentity<ApplicationUser, IdentityRole<Guid>>()` + `AddEntityFrameworkStores` + `AddDefaultTokenProviders`
- [x] `RoleManager` + seed `PlatformAdmin`, `Owner`, `Editor`
- [x] Password policy (min. 8 chars, digit, uppercase, non-alphanumeric) + lockout (5 attempts / 15 min)
- [x] `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` + `JwtBearer` middleware with token validation
- [x] Self-registration disabled (invite-only — `RequireConfirmedAccount = false`, confirmed via accept-invite flow)
- [ ] `[Authorize(Roles = "...")]` on platform endpoints — deferred to DEV-154+
- [ ] API rule: PlatformAdmin => `tenant_id` NULL; Owner/Editor => `tenant_id` required — deferred to DEV-154+
- [ ] Documented in [docs/ARCHITECTURE.md](./ARCHITECTURE.md) — deferred

**Done:**
- `IdentityServiceCollectionExtensions.cs` — `AddApplicationIdentity()` with password policy + lockout
- `JwtServiceCollectionExtensions.cs` — `AddApplicationJwt()`: reads `Jwt:Secret/Issuer/Audience`, throws if secret missing, configures `JwtBearer` with signing key, issuer/audience validation, lifetime + 30 s clock skew
- `appsettings.json` — `Jwt` section (secret blank; filled by Render env var `Jwt__Secret`)
- `appsettings.Development.json` — dev-only secret + `DevSeed` credentials
- `Program.cs` — `AddApplicationJwt`, `UseAuthentication`, `UseAuthorization`, Swagger Bearer security definition (compatible with `Microsoft.OpenApi 2.x` / Swashbuckle 10)
- `Unit/JwtConfigTests.cs` — 3 tests: missing secret throws, whitespace throws, valid secret registers
- All 11 unit tests passing

**Notes:**
- **Phase:** 1.5
- **Area:** backend, security
- **Priority:** P0
- **Depends on:** DEV-151

---

### DEV-154 — API: auth endpoints + `GET /auth/me` ✅

**Description:**
Login, logout, accept-invite, and current user — all in API.

**Acceptance criteria:**
- [x] `POST /api/v1/auth/login` — `SignInManager` → JWT with role claims
- [x] Invite uses `UserManager.AddToRoleAsync` (via `AcceptInviteAsync`)
- [x] `POST /api/v1/auth/logout` — stateless 204; BFF cookie clear in DEV-157
- [x] `POST /api/v1/auth/accept-invite` — token + password for pending invite
- [x] `GET /api/v1/auth/me` returns `{ user, tenant }` for Owner/Editor
- [x] PlatformAdmin: `tenant` null, role in response
- [x] Updates `users.last_login_at`
- [x] Inactive user or inactive tenant → 401 (user enumeration prevention; not 403)
- [x] OpenAPI documented

**Notes:**
- **Phase:** 1.5
- **Area:** backend, security
- **Priority:** P0
- **Depends on:** DEV-151, DEV-153
- **Status:** ✅ **Done** (2026-07-01)

**Done notes (2026-07-01):**
- `AuthController` — 4 endpoints: login (200/401/423/429), logout (204), accept-invite (204/400/429), me (200/401/403)
- `AuthService` — Result pattern throughout; `IAuthService` interface; registered via `AddApplicationServices()`
- `IUserRepository` / `UserRepository` — `FindByIdWithTenantAsync` eager-loads `Tenant` in one query
- Security hardening: user enumeration prevention (all login failures return identical 401), invite token reuse prevention (`EmailConfirmed` guard), failed login + lockout audit logging via `ILogger`
- Rate limiting: `AddApplicationRateLimiter()` — `auth:login` (10 req/min/IP), `auth:invite` (5 req/min/IP); `UseForwardedHeaders()` first in pipeline for correct IP behind Render proxy
- Root cause fix: `AddIdentity` → `AddIdentityCore` (avoids cookie scheme overriding JWT Bearer on `GET /auth/me`)
- JWT minimum secret length: 32 chars enforced at startup; `AddApplicationJwt_ThrowsWhenSecretTooShort` test added
- 9 `AuthServiceTests` + 4 `JwtConfigTests` = 22 total unit tests passing

---

### DEV-155 — Nuxt: BFF proxy + `app.` host routing

**Description:**
Server routes proxy `/api/**` to Render API. Route `app.localhost` / `app.onlineportfolio.com.br` to admin app. **No Supabase client.** Partial host routing already in DEV-104 (`resolve-host.global.ts`); this ticket focuses on **BFF proxy** and auth guards.

**Acceptance criteria:**
- [ ] Catch-all server route forwards to `NUXT_API_INTERNAL_BASE` / Render URL
- [ ] Forwards cookies and auth headers to API
- [x] Middleware: host `app.*` → admin layout; `{slug}.*` → public (stub — DEV-104)
- [ ] Browser never calls `api.onlineportfolio.com.br` directly (admin)

**Notes:**
- **Phase:** 1.5
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-005

---

### DEV-156 — Admin login page

**Description:**
Login UI at `/login` on **centralized** `app.{host}` — form posts via Nuxt proxy to `POST /auth/login`. UI in `components/app/` (standardized, no per-tenant variants — ADR-016). **Dark mode** (`surface-dark`) — see [docs/FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md).

**Acceptance criteria:**
- [x] Page at `app.{host}/login` only (stub `pages/login.vue` + guard; not on tenant subdomains)
- [x] Dark mode (`surface-dark`) on app surface
- [ ] Email + password → proxy → API login
- [ ] Error messages for invalid credentials (no user enumeration)
- [ ] Redirect: Owner/Editor → `/admin`; PlatformAdmin → `/platform/tenants`
- [ ] Already authenticated → redirect per role

**Notes:**
- **Phase:** 1.5
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-155, DEV-154

---

### DEV-157 — Admin session: logout + cookie forwarding

**Description:**
Logout via API; composable `useAuth` calls `/auth/me` through proxy.

**Acceptance criteria:**
- [ ] Logout → proxy → `POST /auth/logout` + redirect to `/login`
- [ ] Composable `useAuth` wraps `/auth/me` via server proxy
- [ ] All admin API calls go through Nuxt proxy (no direct Render from browser)
- [ ] JWT/cookie never exposed to client JS if using httpOnly cookie

**Notes:**
- **Phase:** 1.5
- **Area:** frontend, backend
- **Priority:** P0
- **Depends on:** DEV-156, DEV-154

---

### DEV-158 — Protected admin shell (empty dashboard)

**Description:**
`/admin` for tenant users; PlatformAdmin also has link to `/platform/tenants` for add user. Inherits structural **dark mode** (`surface-dark`, `layouts/app.vue`).

**Acceptance criteria:**
- [ ] Unauthenticated access → redirect `/login`
- [x] Dark layout and tokens (`surface-dark`) — same pattern as login, platform, and error
- [ ] Shows logged-in email, role, tenant name (from `/auth/me`)
- [ ] Logout control visible
- [ ] **PlatformAdmin:** nav link to `/platform/tenants` (add user flow)
- [ ] **Owner/Editor:** placeholder (pt-BR product copy: "Dashboard em construção") — no add user UI

**Notes:**
- **Phase:** 1.5
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-157

---

### DEV-159 — Platform admin seed + invite tenant users

**Description:**
Seed your PlatformAdmin user; thin wrapper for first invite (full add-user API in DEV-161).

**Acceptance criteria:**
- [ ] Seed PlatformAdmin: `UserManager.CreateAsync` + `AddToRoleAsync("PlatformAdmin")` + password (dev)
- [ ] First tenant user invite works end-to-end via DEV-161 endpoint
- [ ] Manual test: invite Owner for `ana` and `joao`; each logs in via login flow (DEV-156–157)

**Notes:**
- **Phase:** 1.5
- **Area:** backend, infra
- **Priority:** P0
- **Depends on:** DEV-154, DEV-152

---

### DEV-161 — Platform API: list & add user (invite) per tenant

**Description:**
**Add user** — PlatformAdmin lists and invites users to any tenant. Core MVP alongside login.

**Acceptance criteria:**
- [ ] `GET /api/v1/platform/tenants/{tenantId}/users` — list users (email, role, is_active, last_login_at)
- [ ] `POST .../users/invite` → `CreateAsync` + `AddToRoleAsync(role)` + Resend
- [ ] `PATCH /api/v1/platform/tenants/{tenantId}/users/{userId}` — deactivate or change role (Owner/Editor)
- [ ] Duplicate invite to same email on same tenant → clear error
- [ ] Tenant users (Owner/Editor) receive **403** on all `/platform/*` routes
- [ ] Cannot deactivate last Owner without replacement (business rule)
- [ ] OpenAPI documented

**Notes:**
- **Phase:** 1.5
- **Area:** backend, security
- **Priority:** P0
- **Depends on:** DEV-154, DEV-159

---

### DEV-162 — Platform admin UI: add user + list users

**Description:**
UI for **add user** — PlatformAdmin picks tenant, invites by email, sees user list. Required for MVP.

**Acceptance criteria:**
- [ ] Route `/platform/tenants` — list tenants (PlatformAdmin only)
- [ ] Route `/platform/tenants/{id}/users` — user list + **Add user** form (email, role Owner/Editor)
- [ ] Success feedback after invite; error for duplicate/invalid email
- [ ] Deactivate user action with confirmation
- [ ] Owner/Editor visiting `/platform/*` → 403 or redirect to `/admin`
- [ ] Owner `/admin` — dashboard placeholder only (no add user in v1)

**Notes:**
- **Phase:** 1.5
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-158, DEV-161

---

### DEV-160 — Login MVP: integration & unit tests

**Description:**
Test coverage for login, add user, and JWT (UT-005, UT-012, IT-011–013).

**Acceptance criteria:**
- [ ] UT-005 JWT handler tests green
- [ ] UT-012 User bootstrap unit tests green
- [ ] IT-011 `/auth/me` integration tests green
- [ ] IT-012 login tenant isolation tests green
- [ ] IT-013 add user invite/list/deactivate tests green

**Notes:**
- **Phase:** 1.5
- **Area:** unit-test, integration-test, security
- **Priority:** P1
- **Depends on:** DEV-154, DEV-158

---

## Epic 2 — Admin features (post-login)

**Requires:** Epic 1.5 complete (login + add user working). Adds artwork CRUD, settings, PDF.

> Auth tasks DEV-200–202 → Epic 1.5 (DEV-153–158 login, DEV-161–162 add user).

---

### DEV-200 — ~~Supabase Auth~~ → see DEV-153 (full Identity)

**Description:**
—

**Acceptance criteria:**
- [ ] *(TBD)*

**Notes:**
- *(none)*

---

### DEV-201 — ~~User + JWT Supabase~~ → see DEV-153/154

**Description:**
—

**Acceptance criteria:**
- [ ] *(TBD)*

**Notes:**
- *(none)*

---

### DEV-202 — ~~Nuxt + Supabase login~~ → see DEV-155–158 (BFF + proxy)

**Description:**
—

**Acceptance criteria:**
- [ ] *(TBD)*

**Notes:**
**Notes:**

---

### DEV-203 — Admin API: Artwork CRUD

**Description:**
Authenticated CRUD for artworks; tenant from JWT user, not request body.

**Acceptance criteria:**
- [ ] `POST/PUT/DELETE /api/v1/artworks`
- [ ] `GET /api/v1/artworks` includes drafts for admin
- [ ] `TenantId` from user context; IDOR checks on `{id}`
- [ ] Publish/unpublish via `IsPublished` / `PublishedAt`

**Notes:**
- **Phase:** 2
- **Area:** backend
- **Priority:** P0
- **Depends on:** DEV-154, DEV-101

---

### DEV-204 — Admin UI: artwork management

**Description:**
Admin pages to list, create, edit, delete, publish artworks.

**Acceptance criteria:**
- [ ] Artwork list (draft + published)
- [ ] Create/edit form with validation
- [ ] Publish toggle
- [ ] Delete with confirmation
- [ ] Sort order control (basic)

**Notes:**
- **Phase:** 2
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-158, DEV-203

---

### DEV-205 — Admin UI: tenant settings

**Description:**
Edit bio, contact email, social links, theme placeholders.

**Acceptance criteria:**
- [ ] `GET/PUT` tenant settings for authenticated owner
- [ ] Settings form in admin
- [ ] Changes reflected on public profile

**Notes:**
- **Phase:** 2
- **Area:** frontend, backend
- **Priority:** P1
- **Depends on:** DEV-158

---

### DEV-206 — Tenant provisioning flow (PlatformAdmin)

**Description:**
End-to-end: PlatformAdmin creates tenant → invites one or more admin users per tenant.

**Acceptance criteria:**
- [ ] `POST /api/v1/platform/tenants` creates tenant + settings
- [ ] Invite flow for first Owner + additional users on same tenant
- [ ] Tenant live at `{slug}.onlineportfolio.com.br`

**Notes:**
- **Phase:** 2
- **Area:** backend
- **Priority:** P1
- **Depends on:** DEV-159, DEV-161

---

### DEV-207 — Portfolio PDF export (QuestPDF)

**Description:**
Public and admin PDF catalog endpoints using QuestPDF Community license.

**Acceptance criteria:**
- [ ] `GET /api/v1/tenants/{slug}/portfolio.pdf` (published only)
- [ ] `GET /api/v1/portfolio/export.pdf` (authenticated)
- [ ] `IPdfService` + document layout in `backend/OnlinePortfolio.Api/Pdf/`
- [ ] `LicenseType.Community` registered at startup
- [ ] Download button on public gallery / admin

**Notes:**
- **Phase:** 2
- **Area:** backend, frontend
- **Priority:** P2
- **Depends on:** DEV-203

---

## Epic 3 — Image uploads (Phase 3)

---

### DEV-300 — Supabase Storage buckets (API-only writes)

**Description:**
Create `artworks-public` bucket; path prefix `tenants/{tenantId}/`. Public read; writes via API service role only.

**Acceptance criteria:**
- [ ] Buckets created per architecture
- [ ] Public read for gallery CDN URLs; no browser upload to Storage
- [ ] API validates tenant path before write
- [ ] Documented in EXTERNAL_PROVIDERS checklist

**Notes:**
- **Phase:** 3
- **Area:** infra, security
- **Priority:** P0
- **Depends on:** DEV-008

---

### DEV-301 — ArtworkImage entity + migration

**Description:**
Store image metadata in EF; binaries in Storage only.

**Acceptance criteria:**
- [ ] `ArtworkImage` entity with `StoragePath`, `PublicUrl`, `SortOrder`, dimensions
- [ ] `TenantId` on entity; global filter applied
- [ ] Migration applied

**Notes:**
- **Phase:** 3
- **Area:** database, backend
- **Priority:** P0
- **Depends on:** DEV-101, DEV-300

---

### DEV-302 — Upload via API (multipart through Nuxt proxy)

**Description:**
Admin uploads via Nuxt proxy → API multipart → Storage (service role).

**Acceptance criteria:**
- [ ] Upload from admin UI through `/api/**` proxy
- [ ] API validates tenant membership + path prefix before Storage write
- [ ] File type whitelist (jpeg, png, webp)
- [ ] Max file size enforced (configurable)

**Notes:**
- **Phase:** 3
- **Area:** frontend, backend
- **Priority:** P0
- **Depends on:** DEV-301, DEV-158

---

### DEV-303 — Gallery displays uploaded images

**Description:**
Public gallery and detail pages show images from Supabase public URLs.

**Acceptance criteria:**
- [ ] Hero/thumbnail on list and detail
- [ ] Alt text from metadata
- [ ] Fallback when no image

**Notes:**
- **Phase:** 3
- **Area:** frontend
- **Priority:** P0
- **Depends on:** DEV-302, DEV-105

---

### DEV-304 — PDF includes artwork thumbnails

**Description:**
QuestPDF catalog embeds thumbnail URLs from Storage.

**Acceptance criteria:**
- [ ] PDF uses thumbnail URLs, not originals
- [ ] Graceful fallback if image missing

**Notes:**
- **Phase:** 3
- **Area:** backend
- **Priority:** P2
- **Depends on:** DEV-207, DEV-303

---

### DEV-305 — Image processing strategy (optional)

**Description:**
Decide and implement thumbnail/web variant generation (ImageSharp on API or manual).

**Acceptance criteria:**
- [ ] Document chosen approach in ARCHITECTURE open decisions
- [ ] Web-optimized variant stored alongside original
- [ ] Size limits per plan (future) considered in design

**Notes:**
- **Phase:** 3
- **Area:** backend
- **Priority:** P3
- **Depends on:** DEV-302

---

## Epic 4 — Custom domains (+ optional billing)

**v1:** no automatic billing — tenants created manually. Custom domains (DEV-400+) can be done **without** billing. DEV-402/403 are **optional** until you decide to charge.

---

### DEV-400 — Tenant custom domain fields + resolution

**Description:**
Store `CustomDomain`, `CustomDomainVerifiedAt`; Nuxt resolves tenant from Host.

**Acceptance criteria:**
- [ ] Host lookup: CustomDomain → tenant, then subdomain slug
- [ ] www vs apex normalization
- [ ] Admin UI to request custom domain (DNS instructions)

**Notes:**
- **Phase:** 4
- **Area:** backend, frontend
- **Priority:** P1
- **Depends on:** DEV-104

---

### DEV-401 — Vercel custom domain per tenant

**Description:**
Manual add in Vercel dashboard for first tenants; document Vercel Domains API for scale.

**Acceptance criteria:**
- [ ] At least one tenant custom domain verified on Vercel
- [ ] HTTPS auto on tenant domain
- [ ] Runbook for artist DNS (CNAME instructions)

**Notes:**
- **Phase:** 4
- **Area:** infra, devops
- **Priority:** P1
- **Depends on:** DEV-400

---

### DEV-402 — Plan entity + limits enforcement (optional)

**Description:**
Enforce `MaxArtworks`, `MaxStorageMb`, `CustomDomainAllowed` per plan. **Optional in v1** — can operate without strict plans or assign plan manually in the database.

**Acceptance criteria:**
- [ ] Plan seeded (Basic / Pro or similar)
- [ ] API rejects over-limit operations with clear error
- [ ] Tenant assigned to plan on provisioning (manual OK)

**Notes:**
- **Phase:** 4
- **Area:** backend
- **Priority:** P2
- **Depends on:** DEV-100

---

### DEV-403 — Billing / subscriptions (optional — skip until charging)

**Description:**
Checkout + webhook for tenant billing. **Out of initial scope — do not implement until charging.** Provider TBD: **Stripe** if expanding outside BR (multi-currency, global cards); **Asaas/Iugu** if staying Brazil-only (PIX, tax). See [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Acceptance criteria:**
- [ ] PSP account + products/prices configured
- [ ] Webhook `POST /api/v1/webhooks/...` on Render
- [ ] Webhook secret in Render env
- [ ] Plan updated on successful subscription events

**Notes:**
- **Phase:** 4
- **Area:** backend, infra
- **Priority:** P3
- **Depends on:** DEV-402 (if limits tied to paid plans)

---

### DEV-404 — Google Workspace operator inbox

**Description:**
Configure Google Workspace for `hello@onlineportfolio.com.br` when needed. Runbook: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Acceptance criteria:**
- [ ] MX + SPF merged with Resend (when Google Workspace added)
- [ ] Test send/receive
- [ ] Documented in [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md)

**Notes:**
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

**Description:**
—

**Acceptance criteria:**
- [ ] Tests for valid slug, inactive tenant, missing tenant
- [ ] Mock `ITenantProvider` where applicable

**Notes:**
- **Area:** unit-test, backend
- **Priority:** P1
- **Depends on:** DEV-102

---

### UT-002 — Backend: EF global query filters

**Description:**
—

**Acceptance criteria:**
- [ ] In-memory or test DB verifies Artwork queries never cross tenants
- [ ] Filter applied automatically on tenant-scoped entities

**Notes:**
- **Area:** unit-test, backend
- **Priority:** P0
- **Depends on:** DEV-101

---

### UT-003 — Backend: Contact form validation

**Description:**
—

**Acceptance criteria:**
- [ ] Invalid email, empty message, oversized input rejected
- [ ] Honeypot field triggers silent reject or 400

**Notes:**
- **Area:** unit-test, backend
- **Priority:** P1
- **Depends on:** DEV-107

---

### UT-004 — Backend: Resend email service (mocked)

**Description:**
—

**Acceptance criteria:**
- [ ] `IResend` / `IEmailService` mock verifies To, From, Reply-To
- [ ] Resend client not called when validation fails

**Notes:**
- **Area:** unit-test, backend
- **Priority:** P1
- **Depends on:** DEV-107

---

### UT-012 — Backend: accept-invite and login

**Description:**
—

**Acceptance criteria:**
- [ ] Valid invite token + password → user active, email_confirmed
- [ ] Invalid/expired token → 400
- [ ] Login with correct password → JWT issued; updates last_login_at
- [ ] Login with wrong password → 401 (no user enumeration)

**Notes:**
- **Area:** unit-test, backend
- **Priority:** P0
- **Depends on:** DEV-154

---

### UT-005 — Backend: JWT authorization handler

**Description:**
—

**Acceptance criteria:**
- [ ] Valid token → user context populated
- [ ] Expired/missing token → 401
- [ ] User not in DB → 403

**Notes:**
- **Area:** unit-test, backend, security
- **Priority:** P0
- **Depends on:** DEV-154

---

### UT-006 — Backend: Artwork slug uniqueness per tenant

**Description:**
—

**Acceptance criteria:**
- [ ] Same slug allowed across different tenants
- [ ] Duplicate slug within tenant rejected

**Notes:**
- **Area:** unit-test, backend
- **Priority:** P1
- **Depends on:** DEV-203

---

### UT-007 — Backend: QuestPDF document builder

**Description:**
—

**Acceptance criteria:**
- [ ] PDF generation returns non-empty stream
- [ ] Contains tenant name and artwork count in output (snapshot or byte length check)

**Notes:**
- **Area:** unit-test, backend
- **Priority:** P2
- **Depends on:** DEV-207

---

### UT-008 — Backend: Plan limit validator

**Description:**
—

**Acceptance criteria:**
- [ ] Max artworks enforced at service layer
- [ ] Clear exception or result type for limit exceeded

**Notes:**
- **Area:** unit-test, backend
- **Priority:** P2
- **Depends on:** DEV-402

---

### UT-009 — Frontend: tenant resolution composable

**Description:**
—

**Acceptance criteria:**
- [ ] Vitest tests for host → slug mapping
- [ ] Apex vs subdomain vs unknown host cases

**Notes:**
- **Area:** unit-test, frontend
- **Priority:** P1
- **Depends on:** DEV-104

---

### UT-010 — Frontend: contact form component

**Description:**
—

**Acceptance criteria:**
- [ ] Client validation before submit
- [ ] Submit disabled while loading
- [ ] Success/error states rendered

**Notes:**
- **Area:** unit-test, frontend
- **Priority:** P2
- **Depends on:** DEV-107

---

### UT-011 — Frontend: admin artwork form validation

**Description:**
—

**Acceptance criteria:**
- [ ] Required fields enforced
- [ ] Slug format validation

**Notes:**
- **Area:** unit-test, frontend
- **Priority:** P2
- **Depends on:** DEV-204

---

## Integration tests

Activities for cross-layer tests with real or containerized dependencies.

---

### IT-001 — Test infrastructure setup

**Description:**
Integration test infrastructure on top of DEV-005b ([docs/BACKLOG.md](./BACKLOG.md)): WebApplicationFactory, Testcontainers Postgres (or CI service container).

**Acceptance criteria:**
- [ ] `OnlinePortfolio.Api.Tests` project scaffolded (DEV-005b): xUnit, Moq, FluentAssertions, coverlet.collector
- [ ] `Microsoft.AspNetCore.Mvc.Testing` + `WebApplicationFactory` configured
- [ ] Testcontainers Postgres (local) or GitHub Actions Postgres service (CI)
- [ ] Runs in GitHub Actions CI via `dotnet test`
- [ ] DB migrated/seeded per test collection or fixture
- [ ] Isolated from production Supabase

**Notes:**
- **Area:** integration-test, devops
- **Priority:** P0
- **Depends on:** DEV-005b, DEV-004

---

### IT-011 — GET /auth/me integration

**Description:**
—

**Acceptance criteria:**
- [ ] Owner receives correct tenant in response
- [ ] PlatformAdmin receives null tenant
- [ ] Missing Authorization header → 401
- [ ] Invalid JWT → 401

**Notes:**
- **Area:** integration-test, security
- **Priority:** P0
- **Depends on:** IT-001, DEV-154

---

### IT-012 — Login tenant isolation

**Description:**
—

**Acceptance criteria:**
- [ ] User A (tenant ana) `/auth/me` never returns tenant joao data
- [ ] Two users in the same tenant share tenant_id; user from another tenant cannot see cross-tenant data

**Notes:**
- **Area:** integration-test, security
- **Priority:** P0
- **Depends on:** IT-011, DEV-152

---

### IT-013 — PlatformAdmin tenant user management

**Description:**
—

**Acceptance criteria:**
- [ ] PlatformAdmin invites user on tenant A → active row after accept-invite
- [ ] Owner of tenant A cannot call invite/list endpoints (403)
- [ ] PlatformAdmin can list all users for tenant A and B separately
- [ ] Deactivated user gets 403 on `/auth/me`

**Notes:**
- **Area:** integration-test, security
- **Priority:** P1
- **Depends on:** IT-011, DEV-161

---

### IT-002 — Public API: tenant isolation (read)

**Description:**
—

**Acceptance criteria:**
- [ ] Tenant A slug returns only A's published artworks
- [ ] Tenant B slug returns only B's data
- [ ] Cross-tenant artwork ID via wrong slug returns 404

**Notes:**
- **Area:** integration-test, security
- **Priority:** P0
- **Depends on:** IT-001, DEV-103

---

### IT-003 — Public API: unpublished content hidden

**Description:**
—

**Acceptance criteria:**
- [ ] Draft artwork not in public list or detail
- [ ] Published artwork visible

**Notes:**
- **Area:** integration-test
- **Priority:** P0
- **Depends on:** IT-002

---

### IT-004 — Admin API: IDOR prevention

**Description:**
—

**Acceptance criteria:**
- [ ] User of tenant A cannot GET/PUT/DELETE tenant B artwork by ID
- [ ] Returns 403 or 404 (consistent policy documented)

**Notes:**
- **Area:** integration-test, security
- **Priority:** P0
- **Depends on:** IT-001, DEV-203

---

### IT-005 — Admin API: JWT + role enforcement

**Description:**
—

**Acceptance criteria:**
- [ ] Unauthenticated request → 401
- [ ] Valid JWT for user without User row → 403
- [ ] Owner can CRUD own tenant artworks

**Notes:**
- **Area:** integration-test, security
- **Priority:** P0
- **Depends on:** IT-001, DEV-154

---

### IT-006 — Contact form end-to-end (mock Resend)

**Description:**
—

**Acceptance criteria:**
- [ ] POST contact → email service invoked with tenant ContactEmail
- [ ] Rate limit returns 429 after threshold (if implemented)

**Notes:**
- **Area:** integration-test
- **Priority:** P1
- **Depends on:** IT-001, DEV-107

---

### IT-007 — EF migrations apply cleanly

**Description:**
—

**Acceptance criteria:**
- [ ] Fresh DB + `dotnet ef database update` succeeds in CI
- [ ] Idempotent re-run documented

**Notes:**
- **Area:** integration-test, database
- **Priority:** P1
- **Depends on:** IT-001

---

### IT-008 — Storage upload metadata flow

**Description:**
—

**Acceptance criteria:**
- [ ] API rejects metadata for path outside `tenants/{tenantId}/`
- [ ] Valid path persisted with correct ArtworkId link
- [ ] Optional: Supabase local stack or mocked Storage client

**Notes:**
- **Area:** integration-test
- **Priority:** P1
- **Depends on:** DEV-302, IT-001

---

### IT-009 — Nuxt server proxy integration

**Description:**
—

**Acceptance criteria:**
- [ ] Server route returns API response for tenant slug
- [ ] Error from API propagated correctly

**Notes:**
- **Area:** integration-test, frontend
- **Priority:** P2
- **Depends on:** DEV-106

---

### IT-010 — Multi-tenant subdomain routing (E2E smoke)

**Description:**
—

**Acceptance criteria:**
- [ ] Playwright or similar: two tenant URLs show different content
- [ ] Runs against preview/staging environment (not required on every PR)

**Notes:**
- **Area:** integration-test
- **Priority:** P2
- **Depends on:** DEV-105, DEV-011

---

## Security

Dedicated security activities (beyond tests). Cross-reference [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

---

### SEC-001 — Secrets management audit

**Description:**
—

**Acceptance criteria:**
- [ ] No secrets in git history or `.env` committed
- [ ] Service role key only on Render
- [ ] Anon key only in Vercel public env
- [ ] `frontend/.env.example` has placeholders only
- [ ] GitHub Secrets documented in EXTERNAL_PROVIDERS

**Notes:**
- **Area:** security, devops
- **Priority:** P0
- **Depends on:** DEV-001

---

### SEC-002 — CORS and API exposure

**Description:**
—

**Acceptance criteria:**
- [ ] CORS restricted to known origins OR Nuxt proxy is primary path
- [ ] Swagger disabled in production (or auth-protected)
- [ ] No stack traces in production error responses

**Notes:**
- **Area:** security, backend
- **Priority:** P0
- **Depends on:** DEV-003, DEV-106

---

### SEC-003 — TenantId injection prevention

**Description:**
—

**Acceptance criteria:**
- [ ] Admin endpoints ignore client-supplied `TenantId` for authorization
- [ ] Tenant always from JWT + User row
- [ ] Code review checklist item documented

**Notes:**
- **Area:** security, backend
- **Priority:** P0
- **Depends on:** DEV-102, DEV-203

---

### SEC-004 — Input validation & output encoding

**Description:**
—

**Acceptance criteria:**
- [ ] FluentValidation or DataAnnotations on all write DTOs
- [ ] Max length on text fields (bio, description, contact message)
- [ ] Nuxt escapes user content in templates (Vue default + audit)

**Notes:**
- **Area:** security, backend, frontend
- **Priority:** P1
- **Depends on:** DEV-103, DEV-203

---

### SEC-005 — Rate limiting

**Description:**
—

**Acceptance criteria:**
- [ ] Contact endpoint rate limited by IP (and/or tenant slug)
- [ ] Public read endpoints have sensible limits (optional CDN/cache first)
- [ ] 429 response shape consistent

**Notes:**
- **Area:** security, backend
- **Priority:** P1
- **Depends on:** DEV-107

---

### SEC-006 — Supabase Storage RLS review

**Description:**
—

**Acceptance criteria:**
- [ ] Anonymous cannot write to any bucket
- [ ] Authenticated user cannot write outside own tenant path
- [ ] RLS policies documented in repo (`supabase/policies.sql` or docs)

**Notes:**
- **Area:** security, infra
- **Priority:** P0
- **Depends on:** DEV-300

---

### SEC-007 — Auth hardening (Identity + JWT)

**Description:**
—

**Acceptance criteria:**
- [ ] Public sign-up endpoints disabled (invite-only)
- [ ] Password policy + lockout (Identity options)
- [ ] Roles only via `RoleManager` / `AspNetUserRoles` (no duplicate role column)
- [ ] JWT expiry and refresh policy documented
- [ ] `Jwt__Secret` rotation procedure documented

**Notes:**
- **Area:** security, backend
- **Priority:** P1
- **Depends on:** DEV-153

---

### SEC-008 — Dependency scanning in CI

**Description:**
—

**Acceptance criteria:**
- [ ] `dotnet list package --vulnerable` or Dependabot enabled
- [ ] `npm audit` in CI (warn or fail on high severity — policy documented)
- [ ] GitHub Dependabot alerts enabled on repo

**Notes:**
- **Area:** security, devops
- **Priority:** P1
- **Depends on:** DEV-006

---

### SEC-009 — Security headers (frontend)

**Description:**
—

**Acceptance criteria:**
- [ ] `vercel.json` or Nuxt route rules set CSP baseline, X-Frame-Options, etc.
- [ ] Verified on production deploy

**Notes:**
- **Area:** security, frontend
- **Priority:** P2
- **Depends on:** DEV-010

---

### SEC-010 — Audit log for platform admin (optional)

**Description:**
—

**Acceptance criteria:**
- [ ] Platform tenant create/update logged with timestamp and actor
- [ ] Logs to structured stdout (Render) or dedicated table

**Notes:**
- **Area:** security, backend
- **Priority:** P3
- **Depends on:** DEV-108

---

### SEC-011 — Pre-release security checklist

**Description:**
—

**Acceptance criteria:**
- [ ] All [docs/ARCHITECTURE.md](./ARCHITECTURE.md) mandatory items checked
- [ ] IT-004 IDOR tests green
- [ ] Manual smoke: two tenants cannot see each other's admin data
- [ ] Sign-off recorded before first paying customer

**Notes:**
- **Area:** security, docs
- **Priority:** P1
- **Depends on:** SEC-001 through SEC-008

---

## Sprint plan (fixed 2-week cycles)

**Cadence:** **14-day** sprints, starting every **Sunday**.  
**Sprint 1 started:** 2026-06-21 (Sunday).

| Sprint | Period | Focus |
|--------|---------|------|
| **1** | 2026-06-21 → 2026-07-04 | Foundation + bootstrap cloud (Epic 0) ✅ |
| **2** | 2026-07-05 → 2026-07-18 | Epic 0 closed (DNS, Resend, prod domains) ✅ |
| **3** | 2026-07-19 → 2026-08-01 | Login MVP (Epic 1.5) ← **current** |
| **4** | 2026-08-02 → 2026-08-15 | Public site per tenant (Epic 1) |
| **5** | 2026-08-16 → 2026-08-29 | Contact + hardening |
| **6** | 2026-08-30 → 2026-09-12 | Admin CRUD post-login |
| **7** | 2026-09-13 → 2026-09-26 | Uploads (Phase 3) |

---

### Sprint 1 — 2026-06-21 → 2026-07-04 — Foundation & prod bootstrap

**Goal:** monorepo, local dev, backend CI/CD, Supabase prod, API on Render, Vercel previews.

#### ✅ Completed (sprint 1 start — through 06/21)

| Issue | Notes |
|-------|--------|
| DEV-000 | Domain `onlineportfolio.com.br` |
| DEV-001 | Monorepo scaffold |
| DEV-002 | Docker Compose |
| DEV-003 | API skeleton + health + Dockerfile |
| DEV-004 | EF Core + DbContext + factory migrate *(migration `Initial` applied)* |
| DEV-005 | Nuxt 3 + hosts + structural dark mode |
| DEV-005b | xUnit test project + coverage script |
| DEV-012 | Repo GitHub + apps Render/Vercel + branch protection |
| DEV-013 | Workspace Linear |
| DEV-006 | `ci-backend.yml` + `ci-frontend.yml` + branch protection + manual redeploy doc |
| DEV-007 | `deploy-backend.yml` + migrate prod + Render After CI Checks Pass |
| DEV-008 | Supabase prod `online-portfolio-db-prod` |
| DEV-014 | Resend — account + API key + Render env |
| DEV-009 | Render API prod — env vars, `/health`, After CI Checks Pass |
| DEV-010 | Vercel `online-portfolio-web` — previews, env vars, prod auto-deploy off |
| DEV-007b | `deploy-frontend.yml` + GitHub Secrets `VERCEL_*` + `workflow_dispatch` |
| DEV-011 | DNS apex + `app.` + `api.` + Resend DKIM/DMARC + `mail@` on Render |

#### ✅ Sprint 1 complete (through 07/04)

_Epic 0 foundation closed — see Sprint 2._

**Out of sprint 1:** DEV-008b (optional Supabase dev) · Epic 1+

---

### Sprint 2 — 2026-07-05 → 2026-07-18 — Epic 0 done + features

**Goal:** production closed (front + API + domains) ✅ — ready for Epic 1.5 / Epic 1.

DEV-011 ✅ → SEC-001 → UT-003 → IT-006

**Exit criteria:** ✅ `onlineportfolio.com.br` + `app.` on Vercel, `api.` on Render, email DKIM/DMARC OK, deploys only via Actions, `mail@` on Render.

**Next:** SEC-001 → UT-003 → IT-006 (or start Epic 1.5 in parallel per priority).

---

### Sprint 3 — 2026-07-19 → 2026-08-01 — Login MVP (Epic 1.5)

DEV-150 → DEV-151 → DEV-152 → DEV-153 → DEV-154 → DEV-155 → DEV-156 → DEV-157 → DEV-158 → DEV-159 → DEV-161 → DEV-162 → DEV-160 → UT-012 → IT-011 → IT-012 → IT-013

---

### Sprint 4 — 2026-08-02 → 2026-08-15 — Public site per tenant (Epic 1)

DEV-100 → DEV-101 → DEV-102 → DEV-103 → DEV-104 → DEV-105 → DEV-106 → UT-002 → IT-001 → IT-002

---

### Sprint 5 — 2026-08-16 → 2026-08-29 — Contact + hardening

DEV-107 → DEV-109 → DEV-110 → SEC-007 → SEC-003 → UT-005 → SEC-011 *(partial)*

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

*Last updated: 2026-07-01 — DEV-150/151/152/153/154 done; próximo: DEV-155 (Nuxt BFF proxy + `app.` host routing)*

---

