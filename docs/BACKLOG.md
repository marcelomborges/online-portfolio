# Development Backlog — Online Portfolio Platform

Detailed activity list for building the multi-tenant artist portfolio SaaS.

**Execução:** issues no [Linear](https://linear.app) (import feito). **Documentação:** este arquivo permanece fonte de verdade para agentes e PRs — mantenha `DEV-xxx` nos commits e descrições.

**Related:** [ARCHITECTURE.md](./ARCHITECTURE.md) · [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) · [DATABASE.md](./DATABASE.md)

**Domínio da plataforma:** `onlineportfolio.com.br` — ✅ registrado (Registro.br)

**Mapa de domínios:** [ARCHITECTURE.md §2.1](./ARCHITECTURE.md#21-mapa-de-domínios-e-superfícies-do-produto) — site público por tenant (`ana.`, `mark.`), admin padronizado em `app.`, BFF via Nuxt → API .NET.

---

## How to use with Linear

| BACKLOG.md | Linear |
|---|---|
| `## Epic` / Phase heading | **Project** |
| `### DEV-xxx` | **Issue** (título `DEV-xxx — …`) |
| `Area` | **Labels** (`backend`, `frontend`, `infra`, …) |
| `Priority` P0–P3 | **Priority** |
| `Depends on` | **Blocked by** |
| Acceptance criteria | Descrição / checklist |

**Sincronizar:** ao concluir trabalho, marque Done no Linear e atualize checkboxes aqui quando fizer sentido para docs.

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

Atividades de **conta e configuração** alinhadas à [ordem de setup em EXTERNAL_PROVIDERS §2](./EXTERNAL_PROVIDERS.md#2-ordem-de-setup). Código local (DEV-001–005) pode correr em paralelo às contas (DEV-012–014).

| Ordem | Provider | Issue | Quando |
|---|---|---|---|
| 1 | Registro.br | [DEV-000](#dev-000--register-domain-) | ✅ domínio comprado |
| 2 | GitHub | [DEV-012](#dev-012--github-repository--platform-integrations) | Após DEV-001 (push) |
| 3 | Linear | [DEV-013](#dev-013--linear-workspace) | Cedo — gestão de issues |
| 4 | Supabase | [DEV-008](#dev-008--supabase-production-project) | Antes do deploy API |
| 5 | Render | [DEV-009](#dev-009--render-api-deployment) | Após DEV-003 + DEV-008 |
| 6 | SendGrid | [DEV-014](#dev-014--sendgrid-account--api-key) → [DEV-107](#dev-107--contact-form--sendgrid) | Conta antes do Render; código Epic 1 |
| 7 | Vercel | [DEV-010](#dev-010--vercel-frontend-deployment) | Após DEV-005 |
| 8 | DNS + email DNS | [DEV-011](#dev-011--dns--https-production) | Após Render + Vercel |
| 9 | GitHub Actions | [DEV-006](#dev-006--github-actions-ci-pr--workflows-separados) · [DEV-007](#dev-007--github-actions-deploy-pipeline-backend) · [DEV-007b](#dev-007b--github-actions-deploy-pipeline-frontend) | Secrets de DEV-008/010/012 |
| — | Google Workspace | [DEV-404](#dev-404--google-workspace-operator-inbox) | Opcional, pós-lançamento |
| — | Stripe / billing | [DEV-403](#dev-403--billing--subscriptions-optional--skip-until-charging) | **Opcional** — skip no v1 |

---

### DEV-000 — Register domain ✅

| Field | Value |
|---|---|
| **Phase** | 0 — Foundation |
| **Area** | infra |
| **Priority** | P0 |
| **Depends on** | — |
| **Status** | ✅ **Done** — domínio comprado no Registro.br |

**Description:** Register `onlineportfolio.com.br` at Registro.br. DNS configuration deferred until [DEV-011](#dev-011--dns--https-production). Runbook: [EXTERNAL_PROVIDERS §3](./EXTERNAL_PROVIDERS.md#3-registrobr--domínio).

**Acceptance criteria:**
- [x] Domain status **Ativo** in Registro.br panel
- [ ] Renewal date noted; renewal reminder configured
- [ ] Titular (CPF/CNPJ) and login credentials saved securely (password manager)
- [ ] DNS left at Registro.br default until deploy (DEV-011)

---

### DEV-001 — Monorepo scaffold

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | infra |
| **Priority** | P0 |
| **Depends on** | — |

**Description:** Create repository structure per architecture doc.

**Acceptance criteria:**
- [ ] `frontend/` (Nuxt 3), `backend/` (ASP.NET Web API), `docs/`
- [ ] Root `README.md` with local dev instructions (stub OK)
- [ ] `.gitignore` for Node, .NET, env files
- [ ] `.env.example` in `frontend/` and `backend/`

---

### DEV-002 — Docker Compose (local dev)

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | infra |
| **Priority** | P0 |
| **Depends on** | DEV-001 |

**Description:** One-command local stack: Postgres + API + Nuxt with volume mounts for hot reload.

**Acceptance criteria:**
- [ ] `docker compose up` starts Postgres, backend, frontend
- [ ] API reachable at documented local URL
- [ ] Nuxt dev server with HMR
- [ ] Seed script creates **2 tenants** for isolation testing

---

### DEV-003 — Backend API skeleton

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | backend |
| **Priority** | P0 |
| **Depends on** | DEV-001 |

**Description:** ASP.NET Core Web API with health check, Swagger, Serilog, global exception handler.

**Acceptance criteria:**
- [ ] `GET /health` returns 200
- [ ] OpenAPI at `/swagger` (dev only or configurable)
- [ ] Structured JSON error responses
- [ ] Dockerfile (multi-stage) in `backend/`
- [ ] `Program.cs` reads config from environment variables

---

### DEV-004 — EF Core + PostgreSQL setup

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | database |
| **Priority** | P0 |
| **Depends on** | DEV-003, DEV-002 |

**Description:** DbContext, Npgsql provider, initial migration infrastructure. Local connection to Compose Postgres.

**Acceptance criteria:**
- [ ] `ApplicationDbContext` registered in DI
- [ ] Connection string from config (`ConnectionStrings__Default`)
- [ ] `dotnet ef migrations add Initial` works locally
- [ ] `dotnet ef database update` applies against Compose Postgres
- [ ] Separate migration connection string documented for CI (direct port 5432)

---

### DEV-005 — Nuxt 3 frontend skeleton

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | frontend |
| **Priority** | P0 |
| **Depends on** | DEV-001 |

**Description:** Nuxt 3 app with TypeScript, basic layout, env config for API base URL.

**Acceptance criteria:**
- [ ] Nuxt 3 + TypeScript runs locally and in Docker
- [ ] `NUXT_PUBLIC_API_BASE` and platform host env vars defined
- [ ] Default layout + error page
- [ ] Nitro preset compatible with Vercel

---

### DEV-012 — GitHub repository & platform integrations

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | infra, devops |
| **Priority** | P0 |
| **Depends on** | DEV-001 |

**Description:** Criar repo `online-portfolio` no GitHub, primeiro push do monorepo, conectar Render e Vercel via GitHub App, preparar secrets para Actions. Runbook: [EXTERNAL_PROVIDERS §4](./EXTERNAL_PROVIDERS.md#4-github-cicd--actions).

**Acceptance criteria:**
- [ ] Repo criado e código do monorepo em `main`
- [ ] Render GitHub App instalado com acesso ao repo
- [ ] Vercel GitHub App instalado — PR previews **on**; production auto-deploy **off**
- [ ] GitHub Actions secrets preparados (placeholders OK até DEV-008/010): `SUPABASE_MIGRATION_CONNECTION_STRING`, `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID`
- [ ] (Recomendado) Branch protection em `main` exigindo Backend CI + Frontend CI

---

### DEV-013 — Linear workspace

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | docs, infra |
| **Priority** | P2 |
| **Depends on** | — |

**Description:** Workspace Linear para issues `DEV-xxx`. Runbook: [EXTERNAL_PROVIDERS §5](./EXTERNAL_PROVIDERS.md#5-linear-project-management).

**Acceptance criteria:**
- [ ] Conta + workspace criados ([linear.app/signup](https://linear.app/signup))
- [ ] Labels de `Area` configuradas (`backend`, `frontend`, `infra`, …)
- [ ] DEV-000 marcado **Done**; DEV-001 (ou próxima issue ativa) criada
- [ ] (Opcional) Integração GitHub → repo `online-portfolio`

---

### DEV-014 — SendGrid account & API key

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | infra |
| **Priority** | P1 |
| **Depends on** | — |

**Description:** Conta SendGrid e API key para envio transacional (`noreply@onlineportfolio.com.br`). **Só configuração de conta** — integração na API em [DEV-107](#dev-107--contact-form--sendgrid); autenticação de domínio (DKIM) em [DEV-011](#dev-011--dns--https-production). Runbook: [EXTERNAL_PROVIDERS §8](./EXTERNAL_PROVIDERS.md#8-sendgrid-email).

**Acceptance criteria:**
- [ ] Conta SendGrid criada e verificada
- [ ] API key `portfolio-api-prod` com permissão **Mail Send** only
- [ ] Key guardada no password manager (Render env em DEV-009)
- [ ] Remetente dev: single sender **ou** domínio prod adiado até DEV-011
- [ ] `SendGrid__FromEmail` = `noreply@onlineportfolio.com.br` documentado

---

### DEV-006 — GitHub Actions CI (PR) — workflows separados

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | devops |
| **Priority** | P1 |
| **Depends on** | DEV-003, DEV-005, DEV-012 |

**Description:** Dois workflows de CI no PR — **separados** por stack (não um `ci.yml` único): `ci-backend.yml` e `ci-frontend.yml`, com path filters.

**Acceptance criteria:**
- [ ] `ci-backend.yml` runs on `pull_request` to `main` (paths: `backend/**`, `docs/DATABASE.md`, …)
- [ ] `ci-frontend.yml` runs on `pull_request` to `main` (paths: `frontend/**`, …)
- [ ] Backend: `dotnet test` (+ build)
- [ ] Frontend: `npm run lint` (and test if configured)
- [ ] Status checks **Backend CI** and **Frontend CI** visible on PR
- [ ] Documented cross-stack component review before merge ([AGENT_GUIDE § Git flow](./AGENT_GUIDE.md#git-flow-ci-and-cross-stack-review))

---

### DEV-007 — GitHub Actions deploy pipeline (backend)

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | devops |
| **Priority** | P1 |
| **Depends on** | DEV-006, DEV-004 |

**Description:** `deploy-backend.yml` on push to `main` — backend test → EF migrate (Supabase direct) → pass status for Render Wait for CI.

**Acceptance criteria:**
- [ ] `SUPABASE_MIGRATION_CONNECTION_STRING` in GitHub Secrets
- [ ] Migrations run before deploy status succeeds
- [ ] Render **Wait for CI** documented and enabled (waits on `deploy-backend.yml`)
- [ ] Branch protection on `main` requires Backend CI + Frontend CI (recommended)

---

### DEV-007b — GitHub Actions deploy pipeline (frontend)

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | devops |
| **Priority** | P1 |
| **Depends on** | DEV-006, DEV-005, DEV-010 |

**Description:** `deploy-frontend.yml` on push to `main` — frontend lint/test → `vercel deploy --prod`. Disable Vercel production auto-deploy; PR previews stay on Vercel GitHub App.

**Acceptance criteria:**
- [ ] `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` in GitHub Secrets
- [ ] Workflow runs on push to `main` (paths: `frontend/**`)
- [ ] Production deploy only via Actions (Vercel dashboard auto-deploy **off** for prod)
- [ ] PR preview deploys still work via Vercel integration

---

### DEV-008 — Supabase production project

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | infra |
| **Priority** | P1 |
| **Depends on** | — |

**Description:** Create Supabase **production** project only; store pooler + direct connection strings and API keys. No separate deployed dev/staging Supabase in v1 ([ADR-015](./ARCHITECTURE.md#adr-015-deploy-somente-em-production)). Runbook: [EXTERNAL_PROVIDERS §6](./EXTERNAL_PROVIDERS.md#6-supabase).

**Acceptance criteria:**
- [ ] Single prod project `portfolio-prod` in chosen region
- [ ] Pooler URI (6543) for API runtime
- [ ] Direct URI (5432) for migrations/CI only
- [ ] Anon key, service role key, JWT secret stored in password manager / secrets
- [ ] Documented in `EXTERNAL_PROVIDERS.md` checklist

---

### DEV-009 — Render API deployment

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | devops |
| **Priority** | P1 |
| **Depends on** | DEV-003, DEV-008, DEV-007, DEV-014 |

**Description:** Deploy backend Docker image to Render free tier; connect GitHub repo. Runbook: [EXTERNAL_PROVIDERS §7](./EXTERNAL_PROVIDERS.md#7-render-api).

**Acceptance criteria:**
- [ ] Conta Render criada; web service Docker (`backend/Dockerfile`) conectado ao repo
- [ ] Web service live on Render default URL
- [ ] Health check `/health` configured
- [ ] Production env vars set (DB pooler, `Jwt__Secret`, `SendGrid__ApiKey` de DEV-014)
- [ ] **Wait for CI** habilitado (gate em `deploy-backend.yml`)
- [ ] Custom domain `api.onlineportfolio.com.br` (pode aguardar DEV-011 — OK com URL `*.onrender.com` primeiro)

---

### DEV-010 — Vercel frontend deployment

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | devops |
| **Priority** | P1 |
| **Depends on** | DEV-005, DEV-012 |

**Description:** Connect repo to Vercel; root directory `frontend`; PR previews enabled; **production deploy via `deploy-frontend.yml`** (DEV-007b). Runbook: [EXTERNAL_PROVIDERS §9](./EXTERNAL_PROVIDERS.md#9-vercel-frontend).

**Acceptance criteria:**
- [ ] Conta Vercel criada; projeto importado do GitHub (`frontend/` root)
- [ ] PR preview deploys enabled
- [ ] Production auto-deploy **disabled** in Vercel (prod = Actions)
- [ ] Env vars configured in Vercel dashboard (`NUXT_PUBLIC_*`, `NUXT_API_INTERNAL_BASE`)
- [ ] Custom domains deferred until [DEV-011](#dev-011--dns--https-production)

---

### DEV-011 — DNS & HTTPS (production)

| Field | Value |
|---|---|
| **Phase** | 0 |
| **Area** | infra |
| **Priority** | P1 |
| **Depends on** | DEV-000, DEV-009, DEV-010 |

**Description:** DNS de produção: Registro.br nameservers → Vercel; domínios apex/`app.`/wildcard; CNAME `api.` → Render; registros SendGrid (DKIM/SPF). Runbook: [EXTERNAL_PROVIDERS §10](./EXTERNAL_PROVIDERS.md#10-dns-no-deploy) · email [§10.4](./EXTERNAL_PROVIDERS.md#104-dns-de-email--sendgrid-v1-só-envio).

**Acceptance criteria:**
- [ ] Nameservers `ns1.vercel-dns.com` / `ns2.vercel-dns.com` at Registro.br
- [ ] Domains added in Vercel: apex, `app.`, `*.onlineportfolio.com.br`
- [ ] HTTPS active on Vercel domains (auto)
- [ ] `api.onlineportfolio.com.br` verified on Render with HTTPS (auto)
- [ ] SendGrid domain authentication: CNAMEs DKIM (+ SPF TXT se indicado) na Vercel DNS
- [ ] SendGrid dashboard mostra domínio `onlineportfolio.com.br` autenticado
- [ ] Checklist [EXTERNAL_PROVIDERS §10](./EXTERNAL_PROVIDERS.md#10-dns-no-deploy) marcado

---

## Epic 1 — Site público por tenant (Phase 1)

**Objetivo:** landing + posts/galeria em `{slug}.onlineportfolio.com.br` (ex.: `ana.`, `mark.`). Admin fica no Epic 1.5 (`app.`). Ver [ARCHITECTURE.md §2.1](./ARCHITECTURE.md#21-mapa-de-domínios-e-superfícies-do-produto).

### DEV-100 — Domain model: Tenant, Plan, TenantSettings

| Field | Value |
|---|---|
| **Phase** | 1 |
| **Area** | database, backend |
| **Priority** | P0 |
| **Depends on** | DEV-004 |

**Description:** EF entities for platform-scoped tenant tables. **Superseded by Epic 1.5** if login MVP is built first — see DEV-150/151 and [DATABASE.md](./DATABASE.md).

**Acceptance criteria:**
- [ ] `Tenant`, `Plan`, `TenantSettings` entities
- [ ] Unique index on `Tenant.Slug`, unique `CustomDomain` where not null
- [ ] Migration applied locally and documented for CI
- [ ] Seed: 2 tenants with distinct slugs

---

### DEV-101 — Domain model: Artwork (+ optional Category)

| Field | Value |
|---|---|
| **Phase** | 1 |
| **Area** | database, backend |
| **Priority** | P0 |
| **Depends on** | DEV-100 |

**Description:** Tenant-scoped artwork entity with publish flag and slug scoped per tenant.

**Acceptance criteria:**
- [ ] `Artwork` with `TenantId`, `IsPublished`, `PublishedAt`, `SortOrder`
- [ ] Unique `(TenantId, Slug)` on Artwork
- [ ] Optional `Category` entity if included in v1
- [ ] EF global query filter on `TenantId` (foundation)

---

### DEV-102 — TenantContext middleware

| Field | Value |
|---|---|
| **Phase** | 1 |
| **Area** | backend |
| **Priority** | P0 |
| **Depends on** | DEV-100 |

**Description:** Resolve tenant from route slug; expose `ITenantContext` for request scope.

**Acceptance criteria:**
- [ ] Middleware or endpoint filter resolves tenant by slug
- [ ] 404 for unknown/inactive tenant
- [ ] `TenantId` available to services and EF filters

---

### DEV-103 — Public API: tenant profile & artworks

| Field | Value |
|---|---|
| **Phase** | 1 |
| **Area** | backend |
| **Priority** | P0 |
| **Depends on** | DEV-101, DEV-102 |

**Description:** Read-only public endpoints per architecture §10.

**Acceptance criteria:**
- [ ] `GET /api/v1/tenants/{slug}/profile`
- [ ] `GET /api/v1/tenants/{slug}/artworks` (paginated, published only)
- [ ] `GET /api/v1/tenants/{slug}/artworks/{id}` (published only)
- [ ] OpenAPI documented
- [ ] No draft/unpublished data leaked

---

### DEV-104 — Nuxt tenant resolution middleware

| Field | Value |
|---|---|
| **Phase** | 1 |
| **Area** | frontend |
| **Priority** | P0 |
| **Depends on** | DEV-005, DEV-103 |

**Description:** Resolve tenant pelo `Host` — subdomínio `{slug}.onlineportfolio.com.br` ou domínio custom. Host `app.*` e apex da plataforma **não** são tenants.

**Acceptance criteria:**
- [ ] Middleware: `{slug}.onlineportfolio.com.br` → tenant slug
- [ ] Middleware: `app.*` → modo admin (sem tenant público)
- [ ] Middleware: `onlineportfolio.com.br` → landing da plataforma
- [ ] Dev local: slug configurável via host ou env
- [ ] Tenant desconhecido → página 404
- [ ] Composable expõe contexto do tenant nas páginas públicas

---

### DEV-105 — Páginas públicas do tenant (landing + posts/galeria)

| Field | Value |
|---|---|
| **Phase** | 1 |
| **Area** | frontend |
| **Priority** | P0 |
| **Depends on** | DEV-104, DEV-103 |

**Description:** Site público por tenant em `{slug}.onlineportfolio.com.br` — home/landing, listagem de posts/obras, detalhe, about. Dados via proxy Nuxt → API. Layout base compartilhado; tema por tenant pode vir depois.

**Acceptance criteria:**
- [ ] Landing/home por tenant
- [ ] Listagem de posts/obras
- [ ] Página de detalhe
- [ ] About/contato a partir de `TenantSettings`
- [ ] SSG ou ISR com cache key incluindo slug do tenant
- [ ] Layout responsivo (mobile-first)
- [ ] **Sem** rotas de admin/login neste host

---

### DEV-106 — Nuxt server proxy to API

| Field | Value |
|---|---|
| **Phase** | 1 |
| **Area** | frontend |
| **Priority** | P1 |
| **Depends on** | DEV-105 |

**Description:** Rotas server fazem proxy de **todas** as chamadas (público + admin) para a API — padrão BFF. Browser não acessa Render direto.

**Acceptance criteria:**
- [ ] `server/api/**` faz proxy para `api.onlineportfolio.com.br`
- [ ] Páginas públicas e admin usam proxy ou fetch server-side
- [ ] Cookies de auth repassados no proxy (admin)
- [ ] URL base da API não hardcoded no bundle client para paths sensíveis

---

### DEV-107 — Contact form + SendGrid

| Field | Value |
|---|---|
| **Phase** | 1 |
| **Area** | backend, frontend |
| **Priority** | P1 |
| **Depends on** | DEV-103, DEV-014 |

**Description:** Formulário de contato POST → API → SendGrid (`noreply@`) → `ContactEmail` do tenant. Requer [DEV-014](#dev-014--sendgrid-account--api-key) (conta) e DKIM em [DEV-011](#dev-011--dns--https-production) para prod. Runbook: [EXTERNAL_PROVIDERS §8](./EXTERNAL_PROVIDERS.md#8-sendgrid-email).

**Acceptance criteria:**
- [ ] `POST /api/v1/tenants/{slug}/contact` with validation
- [ ] `IEmailService` + `SendGridEmailService` implementation
- [ ] `SendGrid__FromEmail` = `noreply@onlineportfolio.com.br`
- [ ] Reply-To = email do visitante (artista responde direto)
- [ ] Rate limiting on contact endpoint (basic)
- [ ] Contact form UI on public site
- [ ] Dev: single sender OK; prod: domínio autenticado via [DEV-011](#dev-011--dns--https-production)
- [ ] Honeypot or basic anti-spam field

---

### DEV-108 — Platform admin: seed / create tenants (API)

| Field | Value |
|---|---|
| **Phase** | 1 |
| **Area** | backend |
| **Priority** | P1 |
| **Depends on** | DEV-100 |

**Description:** Minimal platform admin endpoints or seed-only for first 2 artists (full PlatformAdmin auth in Phase 2).

**Acceptance criteria:**
- [ ] `POST /api/v1/platform/tenants` (protected — API key or temporary auth for v1)
- [ ] Creates Tenant + TenantSettings defaults
- [ ] Document manual provisioning for first customers
- [ ] Seed data script for local dev (2 tenants + sample artworks)

---

### DEV-109 — Platform marketing page (apex)

| Field | Value |
|---|---|
| **Phase** | 1 |
| **Area** | frontend |
| **Priority** | P2 |
| **Depends on** | DEV-104 |

**Description:** Landing page at `onlineportfolio.com.br` when host is apex (not tenant subdomain).

**Acceptance criteria:**
- [ ] Apex host shows platform marketing content
- [ ] Subdomain hosts show tenant gallery
- [ ] Clear CTA for artists (contact / waitlist)

---

## Epic 1.5 — Multi-tenant DB + admin login + add user (MVP)

**Goal:** Database + admin com **duas funções essenciais** (sem gallery/artwork ainda):

| # | Função | Quem | Entrega |
|---|---|---|---|
| 1 | **Login / logout** | PlatformAdmin + tenant users | DEV-154–158 |
| 2 | **Add user** (convite por tenant) | PlatformAdmin only | DEV-159, DEV-161, DEV-162 |

Sem CRUD de obras, settings completos ou site público neste epic.

**Domínios:** login e admin só em `app.onlineportfolio.com.br`; sites `{slug}.onlineportfolio.com.br` vêm no Epic 1 (público).

**Schema reference:** [DATABASE.md](./DATABASE.md) — migration `InitialMultiTenantAndUsers`

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

| Field | Value |
|---|---|
| **Phase** | 1.5 — Login + add user MVP |
| **Area** | database, backend |
| **Priority** | P0 |
| **Depends on** | DEV-004 |

**Description:** First migration per DATABASE.md — `plans`, `tenants`, `tenant_settings`, `users` only (no artworks).

**Acceptance criteria:**
- [ ] Migration `InitialMultiTenantAndUsers` created
- [ ] Tables match DATABASE.md §4 (columns, FKs, checks)
- [ ] Indexes: `tenants.slug`, `users.email` UNIQUE, partial unique on `custom_domain`
- [ ] `dotnet ef database update` works on Compose Postgres
- [ ] Rollback (`dotnet ef migrations remove`) tested locally

---

### DEV-151 — EF entities and configurations

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | database, backend |
| **Priority** | P0 |
| **Depends on** | DEV-150 |

**Description:** C# entities `Plan`, `Tenant`, `TenantSettings`, `User` with Fluent API / snake_case naming.

**Acceptance criteria:**
- [ ] Entities in `backend/Data/Entities/`
- [ ] `ApplicationDbContext` DbSets registered
- [ ] `ApplicationUser : IdentityUser<Guid>` com `TenantId`, `InvitedByUserId`, `IsActive` (sem coluna `Role` — ver AspNetRoles)
- [ ] `ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`
- [ ] Seed roles: `PlatformAdmin`, `Owner`, `Editor` via `RoleManager`
- [ ] Constantes ou enum espelhando nomes das roles Identity
- [ ] No global query filter on `User` that hides PlatformAdmin

---

### DEV-152 — Dev seed: plans + two tenants

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | database, backend |
| **Priority** | P0 |
| **Depends on** | DEV-151 |

**Description:** Seed `Starter` plan + tenants `ana` and `joao` with empty `tenant_settings`.

**Acceptance criteria:**
- [ ] Seed runs on local `docker compose up` or explicit `dotnet run --seed`
- [ ] Two active tenants with distinct slugs
- [ ] No artwork rows (table does not exist yet)
- [ ] Seed PlatformAdmin user with password (dev credentials documented)

---

### DEV-153 — ASP.NET Identity completo + JWT

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | backend, security |
| **Priority** | P0 |
| **Depends on** | DEV-151 |

**Description:** Identity **completo** — `AddIdentity`, `RoleManager`, stores EF, JWT na API. Sem Supabase Auth.

**Acceptance criteria:**
- [ ] `AddIdentity<ApplicationUser, IdentityRole<Guid>>()` + `AddEntityFrameworkStores` + `AddDefaultTokenProviders`
- [ ] `RoleManager` + seed `PlatformAdmin`, `Owner`, `Editor`
- [ ] Política de senha (mín. 8 chars) + lockout
- [ ] `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` + middleware JwtBearer com **role claims**
- [ ] Auto-cadastro desabilitado (invite-only)
- [ ] `[Authorize(Roles = "...")]` nos endpoints platform
- [ ] Regra API: PlatformAdmin => `tenant_id` NULL; Owner/Editor => `tenant_id` obrigatório
- [ ] Documentado em ARCHITECTURE §9

---

### DEV-154 — API: auth endpoints + `GET /auth/me`

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | backend, security |
| **Priority** | P0 |
| **Depends on** | DEV-151, DEV-153 |

**Description:** Login, logout, accept-invite, and current user — all in API.

**Acceptance criteria:**
- [ ] `POST /api/v1/auth/login` — `SignInManager` → JWT com role claims
- [ ] Invite usa `UserManager.AddToRoleAsync`
- [ ] `POST /api/v1/auth/logout` — clear session/cookie
- [ ] `POST /api/v1/auth/accept-invite` — token + password for pending invite
- [ ] `GET /api/v1/auth/me` returns `{ user, tenant }` for Owner/Editor
- [ ] PlatformAdmin: `tenant` null, role in response
- [ ] Updates `users.last_login_at`
- [ ] Inactive user or inactive tenant → 403
- [ ] OpenAPI documented

---

### DEV-155 — Nuxt: BFF proxy + `app.` host routing

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | frontend |
| **Priority** | P0 |
| **Depends on** | DEV-005 |

**Description:** Server routes proxy `/api/**` to Render API. Route `app.localhost` / `app.onlineportfolio.com.br` to admin app. **No Supabase client.**

**Acceptance criteria:**
- [ ] Catch-all server route forwards to `NUXT_API_INTERNAL_BASE` / Render URL
- [ ] Forwards cookies and auth headers to API
- [ ] Middleware: host `app.*` → admin layout; `{slug}.*` → public (stub OK)
- [ ] Browser never calls `api.onlineportfolio.com.br` directly (admin)

---

### DEV-156 — Admin login page

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | frontend |
| **Priority** | P0 |
| **Depends on** | DEV-155, DEV-154 |

**Description:** Login UI at `/login` on **centralized** `app.{host}` — form posts via Nuxt proxy to `POST /auth/login`.

**Acceptance criteria:**
- [ ] Page at `app.{host}/login` only (not on tenant subdomains)
- [ ] Email + password → proxy → API login
- [ ] Error messages for invalid credentials (no user enumeration)
- [ ] Redirect: Owner/Editor → `/admin`; PlatformAdmin → `/platform/tenants`
- [ ] Already authenticated → redirect per role

---

### DEV-157 — Admin session: logout + cookie forwarding

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | frontend, backend |
| **Priority** | P0 |
| **Depends on** | DEV-156, DEV-154 |

**Description:** Logout via API; composable `useAuth` calls `/auth/me` through proxy.

**Acceptance criteria:**
- [ ] Logout → proxy → `POST /auth/logout` + redirect to `/login`
- [ ] Composable `useAuth` wraps `/auth/me` via server proxy
- [ ] All admin API calls go through Nuxt proxy (no direct Render from browser)
- [ ] JWT/cookie never exposed to client JS if using httpOnly cookie

---

### DEV-158 — Protected admin shell (empty dashboard)

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | frontend |
| **Priority** | P0 |
| **Depends on** | DEV-157 |

**Description:** `/admin` for tenant users; PlatformAdmin also has link to `/platform/tenants` for add user.

**Acceptance criteria:**
- [ ] Unauthenticated access → redirect `/login`
- [ ] Shows logged-in email, role, tenant name (from `/auth/me`)
- [ ] Logout control visible
- [ ] **PlatformAdmin:** nav link to `/platform/tenants` (add user flow)
- [ ] **Owner/Editor:** placeholder "Dashboard em construção" — no add user UI

---

### DEV-159 — Platform admin seed + invite tenant users

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | backend, infra |
| **Priority** | P0 |
| **Depends on** | DEV-154, DEV-152 |

**Description:** Seed your PlatformAdmin user; thin wrapper for first invite (full add-user API in DEV-161).

**Acceptance criteria:**
- [ ] Seed PlatformAdmin: `UserManager.CreateAsync` + `AddToRoleAsync("PlatformAdmin")` + senha (dev)
- [ ] First tenant user invite works end-to-end via DEV-161 endpoint
- [ ] Manual test: invite Owner for `ana` and `joao`; each logs in via login flow (DEV-156–157)

---

### DEV-161 — Platform API: list & add user (invite) per tenant

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | backend, security |
| **Priority** | P0 |
| **Depends on** | DEV-154, DEV-159 |

**Description:** **Add user** — PlatformAdmin lists and invites users to any tenant. Core MVP alongside login.

**Acceptance criteria:**
- [ ] `GET /api/v1/platform/tenants/{tenantId}/users` — list users (email, role, is_active, last_login_at)
- [ ] `POST .../users/invite` → `CreateAsync` + `AddToRoleAsync(role)` + SendGrid
- [ ] `PATCH /api/v1/platform/tenants/{tenantId}/users/{userId}` — deactivate or change role (Owner/Editor)
- [ ] Duplicate invite to same email on same tenant → clear error
- [ ] Tenant users (Owner/Editor) receive **403** on all `/platform/*` routes
- [ ] Cannot deactivate last Owner without replacement (business rule)
- [ ] OpenAPI documented

---

### DEV-162 — Platform admin UI: add user + list users

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | frontend |
| **Priority** | P0 |
| **Depends on** | DEV-158, DEV-161 |

**Description:** UI for **add user** — PlatformAdmin picks tenant, invites by email, sees user list. Required for MVP.

**Acceptance criteria:**
- [ ] Route `/platform/tenants` — list tenants (PlatformAdmin only)
- [ ] Route `/platform/tenants/{id}/users` — user list + **Add user** form (email, role Owner/Editor)
- [ ] Success feedback after invite; error for duplicate/invalid email
- [ ] Deactivate user action with confirmation
- [ ] Owner/Editor visiting `/platform/*` → 403 or redirect to `/admin`
- [ ] Owner `/admin` — dashboard placeholder only (no add user in v1)

---

### DEV-160 — Login MVP: integration & unit tests

| Field | Value |
|---|---|
| **Phase** | 1.5 |
| **Area** | unit-test, integration-test, security |
| **Priority** | P1 |
| **Depends on** | DEV-154, DEV-158 |

**Description:** Test coverage for login, add user, and JWT (UT-005, UT-012, IT-011–013).

**Acceptance criteria:**
- [ ] UT-005 JWT handler tests green
- [ ] UT-012 User bootstrap unit tests green
- [ ] IT-011 `/auth/me` integration tests green
- [ ] IT-012 login tenant isolation tests green
- [ ] IT-013 add user invite/list/deactivate tests green

---

## Epic 2 — Admin features (post-login)

**Requires:** Epic 1.5 complete (login + add user working). Adds artwork CRUD, settings, PDF.

> Auth tasks DEV-200–202 → Epic 1.5 (DEV-153–158 login, DEV-161–162 add user).

### DEV-200 — ~~Supabase Auth~~ → ver DEV-153 (Identity completo)

### DEV-201 — ~~User + JWT Supabase~~ → ver DEV-153/154

### DEV-202 — ~~Nuxt + Supabase login~~ → ver DEV-155–158 (BFF + proxy)

---

### DEV-203 — Admin API: Artwork CRUD

| Field | Value |
|---|---|
| **Phase** | 2 |
| **Area** | backend |
| **Priority** | P0 |
| **Depends on** | DEV-154, DEV-101 |

**Description:** Authenticated CRUD for artworks; tenant from JWT user, not request body.

**Acceptance criteria:**
- [ ] `POST/PUT/DELETE /api/v1/artworks`
- [ ] `GET /api/v1/artworks` includes drafts for admin
- [ ] `TenantId` from user context; IDOR checks on `{id}`
- [ ] Publish/unpublish via `IsPublished` / `PublishedAt`

---

### DEV-204 — Admin UI: artwork management

| Field | Value |
|---|---|
| **Phase** | 2 |
| **Area** | frontend |
| **Priority** | P0 |
| **Depends on** | DEV-158, DEV-203 |

**Description:** Admin pages to list, create, edit, delete, publish artworks.

**Acceptance criteria:**
- [ ] Artwork list (draft + published)
- [ ] Create/edit form with validation
- [ ] Publish toggle
- [ ] Delete with confirmation
- [ ] Sort order control (basic)

---

### DEV-205 — Admin UI: tenant settings

| Field | Value |
|---|---|
| **Phase** | 2 |
| **Area** | frontend, backend |
| **Priority** | P1 |
| **Depends on** | DEV-158 |

**Description:** Edit bio, contact email, social links, theme placeholders.

**Acceptance criteria:**
- [ ] `GET/PUT` tenant settings for authenticated owner
- [ ] Settings form in admin
- [ ] Changes reflected on public profile

---

### DEV-206 — Tenant provisioning flow (PlatformAdmin)

| Field | Value |
|---|---|
| **Phase** | 2 |
| **Area** | backend |
| **Priority** | P1 |
| **Depends on** | DEV-159, DEV-161 |

**Description:** End-to-end: PlatformAdmin creates tenant → invites one or more admin users per tenant.

**Acceptance criteria:**
- [ ] `POST /api/v1/platform/tenants` creates tenant + settings
- [ ] Invite flow for first Owner + additional users on same tenant
- [ ] Tenant live at `{slug}.onlineportfolio.com.br`

---

### DEV-207 — Portfolio PDF export (QuestPDF)

| Field | Value |
|---|---|
| **Phase** | 2 |
| **Area** | backend, frontend |
| **Priority** | P2 |
| **Depends on** | DEV-203 |

**Description:** Public and admin PDF catalog endpoints using QuestPDF Community license.

**Acceptance criteria:**
- [ ] `GET /api/v1/tenants/{slug}/portfolio.pdf` (published only)
- [ ] `GET /api/v1/portfolio/export.pdf` (authenticated)
- [ ] `IPdfService` + document layout in `backend/Pdf/`
- [ ] `LicenseType.Community` registered at startup
- [ ] Download button on public gallery / admin

---

## Epic 3 — Image uploads (Phase 3)

### DEV-300 — Supabase Storage buckets (API-only writes)

| Field | Value |
|---|---|
| **Phase** | 3 |
| **Area** | infra, security |
| **Priority** | P0 |
| **Depends on** | DEV-008 |

**Description:** Create `artworks-public` bucket; path prefix `tenants/{tenantId}/`. Public read; writes via API service role only.

**Acceptance criteria:**
- [ ] Buckets created per architecture
- [ ] Public read for gallery CDN URLs; no browser upload to Storage
- [ ] API validates tenant path before write
- [ ] Documented in EXTERNAL_PROVIDERS checklist

---

### DEV-301 — ArtworkImage entity + migration

| Field | Value |
|---|---|
| **Phase** | 3 |
| **Area** | database, backend |
| **Priority** | P0 |
| **Depends on** | DEV-101, DEV-300 |

**Description:** Store image metadata in EF; binaries in Storage only.

**Acceptance criteria:**
- [ ] `ArtworkImage` entity with `StoragePath`, `PublicUrl`, `SortOrder`, dimensions
- [ ] `TenantId` on entity; global filter applied
- [ ] Migration applied

---

### DEV-302 — Upload via API (multipart through Nuxt proxy)

| Field | Value |
|---|---|
| **Phase** | 3 |
| **Area** | frontend, backend |
| **Priority** | P0 |
| **Depends on** | DEV-301, DEV-158 |

**Description:** Admin uploads via Nuxt proxy → API multipart → Storage (service role).

**Acceptance criteria:**
- [ ] Upload from admin UI through `/api/**` proxy
- [ ] API validates tenant membership + path prefix before Storage write
- [ ] File type whitelist (jpeg, png, webp)
- [ ] Max file size enforced (configurable)

---

### DEV-303 — Gallery displays uploaded images

| Field | Value |
|---|---|
| **Phase** | 3 |
| **Area** | frontend |
| **Priority** | P0 |
| **Depends on** | DEV-302, DEV-105 |

**Description:** Public gallery and detail pages show images from Supabase public URLs.

**Acceptance criteria:**
- [ ] Hero/thumbnail on list and detail
- [ ] Alt text from metadata
- [ ] Fallback when no image

---

### DEV-304 — PDF includes artwork thumbnails

| Field | Value |
|---|---|
| **Phase** | 3 |
| **Area** | backend |
| **Priority** | P2 |
| **Depends on** | DEV-207, DEV-303 |

**Description:** QuestPDF catalog embeds thumbnail URLs from Storage.

**Acceptance criteria:**
- [ ] PDF uses thumbnail URLs, not originals
- [ ] Graceful fallback if image missing

---

### DEV-305 — Image processing strategy (optional)

| Field | Value |
|---|---|
| **Phase** | 3 |
| **Area** | backend |
| **Priority** | P3 |
| **Depends on** | DEV-302 |

**Description:** Decide and implement thumbnail/web variant generation (ImageSharp on API or manual).

**Acceptance criteria:**
- [ ] Document chosen approach in ARCHITECTURE open decisions
- [ ] Web-optimized variant stored alongside original
- [ ] Size limits per plan (future) considered in design

---

## Epic 4 — Custom domains (+ billing opcional)

**v1:** sem cobrança automática — tenants criados manualmente. Domínios custom (DEV-400+) podem ser feitos **sem** billing. DEV-402/403 são **opcionais** até decidir cobrar.

### DEV-400 — Tenant custom domain fields + resolution

| Field | Value |
|---|---|
| **Phase** | 4 |
| **Area** | backend, frontend |
| **Priority** | P1 |
| **Depends on** | DEV-104 |

**Description:** Store `CustomDomain`, `CustomDomainVerifiedAt`; Nuxt resolves tenant from Host.

**Acceptance criteria:**
- [ ] Host lookup: CustomDomain → tenant, then subdomain slug
- [ ] www vs apex normalization
- [ ] Admin UI to request custom domain (DNS instructions)

---

### DEV-401 — Vercel custom domain per tenant

| Field | Value |
|---|---|
| **Phase** | 4 |
| **Area** | infra, devops |
| **Priority** | P1 |
| **Depends on** | DEV-400 |

**Description:** Manual add in Vercel dashboard for first tenants; document Vercel Domains API for scale.

**Acceptance criteria:**
- [ ] At least one tenant custom domain verified on Vercel
- [ ] HTTPS auto on tenant domain
- [ ] Runbook for artist DNS (CNAME instructions)

---

### DEV-402 — Plan entity + limits enforcement (optional)

| Field | Value |
|---|---|
| **Phase** | 4 |
| **Area** | backend |
| **Priority** | P2 |
| **Depends on** | DEV-100 |

**Description:** Enforce `MaxArtworks`, `MaxStorageMb`, `CustomDomainAllowed` per plan. **Opcional no v1** — pode operar sem planos rígidos ou atribuir plano manualmente no banco.

**Acceptance criteria:**
- [ ] Plan seeded (Basic / Pro or similar)
- [ ] API rejects over-limit operations with clear error
- [ ] Tenant assigned to plan on provisioning (manual OK)

---

### DEV-403 — Billing / subscriptions (optional — skip until charging)

| Field | Value |
|---|---|
| **Phase** | 4 |
| **Area** | backend, infra |
| **Priority** | P3 |
| **Depends on** | DEV-402 (if limits tied to paid plans) |

**Description:** Checkout + webhook for tenant billing. **Fora do escopo inicial — não implementar enquanto não cobrar.** Provider TBD: **Stripe** se expandir fora do BR (multi-moeda, cartões globais); **Asaas/Iugu** se permanecer só Brasil (PIX, fiscal). Ver [EXTERNAL_PROVIDERS §13](./EXTERNAL_PROVIDERS.md#13-stripe--cobrança-saas-fase-4-opcional).

**Acceptance criteria:**
- [ ] PSP account + products/prices configured
- [ ] Webhook `POST /api/v1/webhooks/...` on Render
- [ ] Webhook secret in Render env
- [ ] Plan updated on successful subscription events

---

### DEV-404 — Google Workspace operator inbox

| Field | Value |
|---|---|
| **Phase** | 4 |
| **Area** | infra |
| **Priority** | P3 |
| **Depends on** | DEV-000, DEV-011 |

**Description:** Configure Google Workspace for `hello@onlineportfolio.com.br` when needed. Runbook: [EXTERNAL_PROVIDERS §14](./EXTERNAL_PROVIDERS.md#14-google-workspace-caixa-postal-operador--futuro).

**Acceptance criteria:**
- [ ] MX + SPF merged with SendGrid
- [ ] Test send/receive
- [ ] Documented in EXTERNAL_PROVIDERS §8

---

## Unit tests

Activities for isolated, fast tests (no external services). Run in CI on every PR.

### UT-001 — Backend: TenantContext unit tests

| Field | Value |
|---|---|
| **Area** | unit-test, backend |
| **Priority** | P1 |
| **Depends on** | DEV-102 |

**Acceptance criteria:**
- [ ] Tests for valid slug, inactive tenant, missing tenant
- [ ] Mock `ITenantProvider` where applicable

---

### UT-002 — Backend: EF global query filters

| Field | Value |
|---|---|
| **Area** | unit-test, backend |
| **Priority** | P0 |
| **Depends on** | DEV-101 |

**Acceptance criteria:**
- [ ] In-memory or test DB verifies Artwork queries never cross tenants
- [ ] Filter applied automatically on tenant-scoped entities

---

### UT-003 — Backend: Contact form validation

| Field | Value |
|---|---|
| **Area** | unit-test, backend |
| **Priority** | P1 |
| **Depends on** | DEV-107 |

**Acceptance criteria:**
- [ ] Invalid email, empty message, oversized input rejected
- [ ] Honeypot field triggers silent reject or 400

---

### UT-004 — Backend: SendGrid email service (mocked)

| Field | Value |
|---|---|
| **Area** | unit-test, backend |
| **Priority** | P1 |
| **Depends on** | DEV-107 |

**Acceptance criteria:**
- [ ] `IEmailService` mock verifies To, From, Reply-To
- [ ] SendGrid client not called when validation fails

---

### UT-012 — Backend: accept-invite and login

| Field | Value |
|---|---|
| **Area** | unit-test, backend |
| **Priority** | P0 |
| **Depends on** | DEV-154 |

**Acceptance criteria:**
- [ ] Valid invite token + password → user active, email_confirmed
- [ ] Invalid/expired token → 400
- [ ] Login with correct password → JWT issued; updates last_login_at
- [ ] Login with wrong password → 401 (no user enumeration)

---

### UT-005 — Backend: JWT authorization handler

| Field | Value |
|---|---|
| **Area** | unit-test, backend, security |
| **Priority** | P0 |
| **Depends on** | DEV-154 |

**Acceptance criteria:**
- [ ] Valid token → user context populated
- [ ] Expired/missing token → 401
- [ ] User not in DB → 403

---

### UT-006 — Backend: Artwork slug uniqueness per tenant

| Field | Value |
|---|---|
| **Area** | unit-test, backend |
| **Priority** | P1 |
| **Depends on** | DEV-203 |

**Acceptance criteria:**
- [ ] Same slug allowed across different tenants
- [ ] Duplicate slug within tenant rejected

---

### UT-007 — Backend: QuestPDF document builder

| Field | Value |
|---|---|
| **Area** | unit-test, backend |
| **Priority** | P2 |
| **Depends on** | DEV-207 |

**Acceptance criteria:**
- [ ] PDF generation returns non-empty stream
- [ ] Contains tenant name and artwork count in output (snapshot or byte length check)

---

### UT-008 — Backend: Plan limit validator

| Field | Value |
|---|---|
| **Area** | unit-test, backend |
| **Priority** | P2 |
| **Depends on** | DEV-402 |

**Acceptance criteria:**
- [ ] Max artworks enforced at service layer
- [ ] Clear exception or result type for limit exceeded

---

### UT-009 — Frontend: tenant resolution composable

| Field | Value |
|---|---|
| **Area** | unit-test, frontend |
| **Priority** | P1 |
| **Depends on** | DEV-104 |

**Acceptance criteria:**
- [ ] Vitest tests for host → slug mapping
- [ ] Apex vs subdomain vs unknown host cases

---

### UT-010 — Frontend: contact form component

| Field | Value |
|---|---|
| **Area** | unit-test, frontend |
| **Priority** | P2 |
| **Depends on** | DEV-107 |

**Acceptance criteria:**
- [ ] Client validation before submit
- [ ] Submit disabled while loading
- [ ] Success/error states rendered

---

### UT-011 — Frontend: admin artwork form validation

| Field | Value |
|---|---|
| **Area** | unit-test, frontend |
| **Priority** | P2 |
| **Depends on** | DEV-204 |

**Acceptance criteria:**
- [ ] Required fields enforced
- [ ] Slug format validation

---

## Integration tests

Activities for cross-layer tests with real or containerized dependencies.

### IT-001 — Test infrastructure setup

| Field | Value |
|---|---|
| **Area** | integration-test, devops |
| **Priority** | P0 |
| **Depends on** | DEV-004 |

**Description:** Test project(s) with WebApplicationFactory, Testcontainers Postgres (or CI service container).

**Acceptance criteria:**
- [ ] `backend/tests/IntegrationTests` project
- [ ] Runs in GitHub Actions CI
- [ ] DB migrated/seeded per test collection or fixture
- [ ] Isolated from production Supabase

---

### IT-011 — GET /auth/me integration

| Field | Value |
|---|---|
| **Area** | integration-test, security |
| **Priority** | P0 |
| **Depends on** | IT-001, DEV-154 |

**Acceptance criteria:**
- [ ] Owner receives correct tenant in response
- [ ] PlatformAdmin receives null tenant
- [ ] Missing Authorization header → 401
- [ ] Invalid JWT → 401

---

### IT-012 — Login tenant isolation

| Field | Value |
|---|---|
| **Area** | integration-test, security |
| **Priority** | P0 |
| **Depends on** | IT-011, DEV-152 |

**Acceptance criteria:**
- [ ] User A (tenant ana) `/auth/me` never returns tenant joao data
- [ ] Dois usuários no mesmo tenant compartilham tenant_id; usuário de outro tenant não vê dados cruzados

---

### IT-013 — PlatformAdmin tenant user management

| Field | Value |
|---|---|
| **Area** | integration-test, security |
| **Priority** | P1 |
| **Depends on** | IT-011, DEV-161 |

**Acceptance criteria:**
- [ ] PlatformAdmin convida usuário no tenant A → linha ativa após accept-invite
- [ ] Owner of tenant A cannot call invite/list endpoints (403)
- [ ] PlatformAdmin can list all users for tenant A and B separately
- [ ] Deactivated user gets 403 on `/auth/me`

---

### IT-002 — Public API: tenant isolation (read)

| Field | Value |
|---|---|
| **Area** | integration-test, security |
| **Priority** | P0 |
| **Depends on** | IT-001, DEV-103 |

**Acceptance criteria:**
- [ ] Tenant A slug returns only A's published artworks
- [ ] Tenant B slug returns only B's data
- [ ] Cross-tenant artwork ID via wrong slug returns 404

---

### IT-003 — Public API: unpublished content hidden

| Field | Value |
|---|---|
| **Area** | integration-test |
| **Priority** | P0 |
| **Depends on** | IT-002 |

**Acceptance criteria:**
- [ ] Draft artwork not in public list or detail
- [ ] Published artwork visible

---

### IT-004 — Admin API: IDOR prevention

| Field | Value |
|---|---|
| **Area** | integration-test, security |
| **Priority** | P0 |
| **Depends on** | IT-001, DEV-203 |

**Acceptance criteria:**
- [ ] User of tenant A cannot GET/PUT/DELETE tenant B artwork by ID
- [ ] Returns 403 or 404 (consistent policy documented)

---

### IT-005 — Admin API: JWT + role enforcement

| Field | Value |
|---|---|
| **Area** | integration-test, security |
| **Priority** | P0 |
| **Depends on** | IT-001, DEV-154 |

**Acceptance criteria:**
- [ ] Unauthenticated request → 401
- [ ] Valid JWT for user without User row → 403
- [ ] Owner can CRUD own tenant artworks

---

### IT-006 — Contact form end-to-end (mock SendGrid)

| Field | Value |
|---|---|
| **Area** | integration-test |
| **Priority** | P1 |
| **Depends on** | IT-001, DEV-107 |

**Acceptance criteria:**
- [ ] POST contact → email service invoked with tenant ContactEmail
- [ ] Rate limit returns 429 after threshold (if implemented)

---

### IT-007 — EF migrations apply cleanly

| Field | Value |
|---|---|
| **Area** | integration-test, database |
| **Priority** | P1 |
| **Depends on** | IT-001 |

**Acceptance criteria:**
- [ ] Fresh DB + `dotnet ef database update` succeeds in CI
- [ ] Idempotent re-run documented

---

### IT-008 — Storage upload metadata flow

| Field | Value |
|---|---|
| **Area** | integration-test |
| **Priority** | P1 |
| **Depends on** | DEV-302, IT-001 |

**Acceptance criteria:**
- [ ] API rejects metadata for path outside `tenants/{tenantId}/`
- [ ] Valid path persisted with correct ArtworkId link
- [ ] Optional: Supabase local stack or mocked Storage client

---

### IT-009 — Nuxt server proxy integration

| Field | Value |
|---|---|
| **Area** | integration-test, frontend |
| **Priority** | P2 |
| **Depends on** | DEV-106 |

**Acceptance criteria:**
- [ ] Server route returns API response for tenant slug
- [ ] Error from API propagated correctly

---

### IT-010 — Multi-tenant subdomain routing (E2E smoke)

| Field | Value |
|---|---|
| **Area** | integration-test |
| **Priority** | P2 |
| **Depends on** | DEV-105, DEV-011 |

**Acceptance criteria:**
- [ ] Playwright or similar: two tenant URLs show different content
- [ ] Runs against preview/staging environment (not required on every PR)

---

## Security

Dedicated security activities (beyond tests). Cross-reference ARCHITECTURE §18.

### SEC-001 — Secrets management audit

| Field | Value |
|---|---|
| **Area** | security, devops |
| **Priority** | P0 |
| **Depends on** | DEV-001 |

**Acceptance criteria:**
- [ ] No secrets in git history or `.env` committed
- [ ] Service role key only on Render
- [ ] Anon key only in Vercel public env
- [ ] `.env.example` has placeholders only
- [ ] GitHub Secrets documented in EXTERNAL_PROVIDERS

---

### SEC-002 — CORS and API exposure

| Field | Value |
|---|---|
| **Area** | security, backend |
| **Priority** | P0 |
| **Depends on** | DEV-003, DEV-106 |

**Acceptance criteria:**
- [ ] CORS restricted to known origins OR Nuxt proxy is primary path
- [ ] Swagger disabled in production (or auth-protected)
- [ ] No stack traces in production error responses

---

### SEC-003 — TenantId injection prevention

| Field | Value |
|---|---|
| **Area** | security, backend |
| **Priority** | P0 |
| **Depends on** | DEV-102, DEV-203 |

**Acceptance criteria:**
- [ ] Admin endpoints ignore client-supplied `TenantId` for authorization
- [ ] Tenant always from JWT + User row
- [ ] Code review checklist item documented

---

### SEC-004 — Input validation & output encoding

| Field | Value |
|---|---|
| **Area** | security, backend, frontend |
| **Priority** | P1 |
| **Depends on** | DEV-103, DEV-203 |

**Acceptance criteria:**
- [ ] FluentValidation or DataAnnotations on all write DTOs
- [ ] Max length on text fields (bio, description, contact message)
- [ ] Nuxt escapes user content in templates (Vue default + audit)

---

### SEC-005 — Rate limiting

| Field | Value |
|---|---|
| **Area** | security, backend |
| **Priority** | P1 |
| **Depends on** | DEV-107 |

**Acceptance criteria:**
- [ ] Contact endpoint rate limited by IP (and/or tenant slug)
- [ ] Public read endpoints have sensible limits (optional CDN/cache first)
- [ ] 429 response shape consistent

---

### SEC-006 — Supabase Storage RLS review

| Field | Value |
|---|---|
| **Area** | security, infra |
| **Priority** | P0 |
| **Depends on** | DEV-300 |

**Acceptance criteria:**
- [ ] Anonymous cannot write to any bucket
- [ ] Authenticated user cannot write outside own tenant path
- [ ] RLS policies documented in repo (`supabase/policies.sql` or docs)

---

### SEC-007 — Auth hardening (Identity + JWT)

| Field | Value |
|---|---|
| **Area** | security, backend |
| **Priority** | P1 |
| **Depends on** | DEV-153 |

**Acceptance criteria:**
- [ ] Public sign-up endpoints disabled (invite-only)
- [ ] Password policy + lockout (Identity options)
- [ ] Roles só via `RoleManager` / `AspNetUserRoles` (sem role duplicada em coluna)
- [ ] JWT expiry and refresh policy documented
- [ ] `Jwt__Secret` rotation procedure documented

---

### SEC-008 — Dependency scanning in CI

| Field | Value |
|---|---|
| **Area** | security, devops |
| **Priority** | P1 |
| **Depends on** | DEV-006 |

**Acceptance criteria:**
- [ ] `dotnet list package --vulnerable` or Dependabot enabled
- [ ] `npm audit` in CI (warn or fail on high severity — policy documented)
- [ ] GitHub Dependabot alerts enabled on repo

---

### SEC-009 — Security headers (frontend)

| Field | Value |
|---|---|
| **Area** | security, frontend |
| **Priority** | P2 |
| **Depends on** | DEV-010 |

**Acceptance criteria:**
- [ ] `vercel.json` or Nuxt route rules set CSP baseline, X-Frame-Options, etc.
- [ ] Verified on production deploy

---

### SEC-010 — Audit log for platform admin (optional)

| Field | Value |
|---|---|
| **Area** | security, backend |
| **Priority** | P3 |
| **Depends on** | DEV-108 |

**Acceptance criteria:**
- [ ] Platform tenant create/update logged with timestamp and actor
- [ ] Logs to structured stdout (Render) or dedicated table

---

### SEC-011 — Pre-release security checklist

| Field | Value |
|---|---|
| **Area** | security, docs |
| **Priority** | P1 |
| **Depends on** | SEC-001 through SEC-008 |

**Acceptance criteria:**
- [ ] All ARCHITECTURE §18 mandatory items checked
- [ ] IT-004 IDOR tests green
- [ ] Manual smoke: two tenants cannot see each other's admin data
- [ ] Sign-off recorded before first paying customer

---

## Suggested implementation order (first sprints)

### Sprint 0 — Bootstrap
DEV-000 → DEV-001 → DEV-002 → DEV-003 → DEV-004 → DEV-005 → DEV-012 → DEV-013

### Sprint 1 — Multi-tenant DB + login MVP
DEV-150 → DEV-151 → DEV-152 → DEV-153 → DEV-154 → DEV-155 → DEV-156 → DEV-157 → DEV-158 → DEV-159 → DEV-161 → DEV-162 → UT-012 → IT-011 → IT-012 → IT-013

### Sprint 2 — Public gallery (local)
DEV-101 → DEV-102 → DEV-103 → DEV-104 → DEV-105 → UT-002 → IT-001 → IT-002

### Sprint 3 — Contact + cloud providers + deploy
DEV-014 → DEV-008 → DEV-107 → DEV-009 → DEV-010 → DEV-006 → DEV-007 → DEV-007b → SEC-001 → UT-003 → IT-006

### Sprint 4 — DNS + hardening
DEV-011 → SEC-007 → SEC-003 → UT-005 → SEC-011 (partial)

### Sprint 5 — Admin CRUD (post-login)
DEV-203 → DEV-204 → DEV-205 → IT-004 → IT-005

### Sprint 6 — Uploads
DEV-300 → DEV-301 → DEV-302 → DEV-303 → SEC-006 → IT-008

### Later
DEV-207, DEV-400+, DEV-403, DEV-404, IT-010, SEC-009, SEC-010

---

*Last updated: 2025-06-21 — Linear import done; BACKLOG syncs with Linear + EXTERNAL_PROVIDERS*
