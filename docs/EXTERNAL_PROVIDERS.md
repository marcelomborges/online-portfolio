# External Provider Configuration Guide

Step-by-step configuration for every third-party service required to run the artist portfolio platform in production.

**Related:** [ARCHITECTURE.md](./ARCHITECTURE.md) · [BACKLOG.md](./BACKLOG.md)  
**Platform domain:** `onlineportfolio.com.br` — **registered** at [Registro.br](https://registro.br)  
**Email v1:** Resend `mail@onlineportfolio.com.br` — transactional send only, no mailbox; Reply-To for replies ([ADR-017](./ADR-017-resend-transactional-email.md))  
**Project management:** [Linear](https://linear.app) — issues from BACKLOG (`DEV-xxx`)  
**Operator inbox (future):** Google Workspace — after launch  
**Billing:** **optional** — v1 **no billing**; tenants provisioned manually by the operator

---

## Table of contents

**Reading order = setup order.** Phase 4 and future services at the end.

1. [Overview](#1-overview)
2. [Setup order](#2-setup-order)
3. [Registro.br — domain](#3-registrobr--domain)
4. [GitHub & CI/CD](#4-github-cicd--actions)
5. [Linear](#5-linear-project-management)
6. [Supabase](#6-supabase)
7. [Render (API)](#7-render-api)
8. [Resend (Email)](#8-resend-email)
9. [Vercel (Frontend)](#9-vercel-frontend)
10. [DNS at deploy](#10-dns-at-deploy)
11. [QuestPDF (no external account)](#11-questpdf-no-external-account)
12. [Custom domains per tenant (Phase 4)](#12-custom-domains-per-tenant-phase-4)
13. [Stripe — SaaS billing (optional)](#13-stripe--saas-billing-phase-4-optional)
14. [Google Workspace (future)](#14-google-workspace-operator-mailbox--future)
15. [Secrets & environment map](#15-secrets--environment-variable-map)
16. [Phase checklists](#16-phase-checklists)
17. [Troubleshooting](#17-troubleshooting)
18. [Quick reference — URLs](#quick-reference--urls-to-bookmark)

---

## 1. Overview

### Services involved

| Provider | Purpose | Accounts | Guide |
|---|---|---|---|
| **Registro.br** | Domain `onlineportfolio.com.br` | ✅ 1 (done) | section 3 |
| **GitHub** | Monorepo + CI/CD | 1 repo | section 4 |
| **Linear** | Issues, sprints (`DEV-xxx`) | 1 workspace | section 5 |
| **Supabase** | PostgreSQL, Storage | 1 prod | section 6 |
| **Render** | ASP.NET Core API (Docker) | 1 web service | section 7 |
| **Resend** | Transactional email (`mail@`) | 1 account | section 8 |
| **Vercel** | Nuxt frontend | 1 project | section 9 |
| **Stripe** | SaaS billing (**optional**, future) | ⏸️ not in use yet | section 13 |
| **Google Workspace** | Operator inbox (future) | ⏸️ post-launch | section 14 |
| **QuestPDF** | PDF (NuGet) | No account | section 11 |

### 1.1 Master checklist — create accounts

Use this table to open each service in order. Check off as you complete each step.

| # | Provider | Create account | Dashboard | Doc in this repo | Status |
|---|---|---|---|---|---|
| 1 | **Registro.br** | [registro.br](https://registro.br) | [NIC Panel](https://registro.br/login/) | section 3.1 | ✅ **Done** — domain purchased |
| 2 | **GitHub** | [github.com/signup](https://github.com/signup) | [github.com](https://github.com) | section 4 | ✅ Repo `online-portfolio` + Actions |
| 3 | **Linear** | [linear.app/signup](https://linear.app/signup) | [linear.app](https://linear.app) | section 5 | ✅ Backlog imported |
| 4 | **Supabase** | [supabase.com/dashboard](https://supabase.com/dashboard) | [Dashboard](https://supabase.com/dashboard) | section 6 | ✅ `online-portfolio-db-prod` + optional `online-portfolio-db-dev` |
| 5 | **Render** | [dashboard.render.com/register](https://dashboard.render.com/register) | [Render](https://dashboard.render.com) | section 7 | ✅ API prod + `api.onlineportfolio.com.br` |
| 6 | **Resend** | [resend.com/signup](https://resend.com/signup) | [Resend](https://resend.com/domains) | section 8 | ✅ domain verified + `mail@` on Render |
| 7 | **Vercel** | [vercel.com/signup](https://vercel.com/signup) | [Vercel](https://vercel.com/dashboard) | section 9 | ✅ `online-portfolio-web` + prod domains |
| 8 | **Google Workspace** | [workspace.google.com](https://workspace.google.com/) | [Admin](https://admin.google.com) | section 14 | ⏸️ After launch |
| 9 | **Stripe** | [dashboard.stripe.com/register](https://dashboard.stripe.com/register) | [Stripe](https://dashboard.stripe.com) | section 13 | ⏸️ **Optional** — only if/when billing |

**What to store in password manager / GitHub Secrets:** see section 15.

**Recommended order:** section 2.

### Domain layout (production)

Full map: [docs/ARCHITECTURE.md](./ARCHITECTURE.md)

| Host | Points to | Purpose |
|---|---|---|
| `onlineportfolio.com.br` | Vercel | **Platform** landing (SaaS) |
| `www.onlineportfolio.com.br` | Vercel | Redirect or alias to apex |
| `{slug}.onlineportfolio.com.br` | Vercel | Tenant **public** site (landing + posts) — e.g. `ana.`, `mark.` |
| `*.onlineportfolio.com.br` | Vercel | Wildcard for tenant subdomains |
| `app.onlineportfolio.com.br` | Vercel | **Standardized admin** — single login; proxy → API |
| `api.onlineportfolio.com.br` | Render | .NET API (access via Nuxt proxy, not directly from browser) |
| `{artist-domain}.com` | Vercel | Tenant custom domain — public site (Phase 4) |

```text
                    ┌─────────────┐
  DNS (registrar)   │ yourplatform │
                    └──────┬──────┘
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
      ┌─────────┐    ┌──────────┐    ┌──────────┐
      │ Vercel  │    │  Render  │    │ Supabase │
      │  Nuxt   │───▶│   API    │───▶│ PG/Store │
      │         │    │          │    │ Storage  │
      └─────────┘    └────┬─────┘    └──────────┘
                          │
                          ▼
                     ┌──────────┐
                     │ Resend │
                     └──────────┘
```

---

## 2. Setup order

Follow this order to avoid circular dependencies. Sections **3–9** of this file follow the same sequence.

| Step | Provider | Action | Section |
|---|---|---|---|
| 1 | **Registro.br** | ✅ Domain registered | section 3 |
| 2 | **GitHub** | Monorepo + push | section 4 |
| 3 | **Linear** | Workspace + BACKLOG (can be early) | section 5 |
| 4 | **Supabase** | `online-portfolio-db-prod` (+ optional `online-portfolio-db-dev` for local) | section 6 |
| 5 | **Render** | Account + web service (after DEV-001) | section 7 |
| 6 | **Resend** | Account + API key | section 8 |
| 7 | **Vercel** | Account + Nuxt project (after DEV-001) | section 9 |
| 8 | **DNS** | Vercel NS, `api.`, Resend DKIM/DMARC | section 10 | ✅ DEV-011 |
| 9 | **GitHub Actions** | Workflows + secrets | section 4.3 | ✅ CI + deploy (DEV-006/007/007b) |
| — | **Per tenant** | Artist custom domain | section 12 |
| — | **Stripe** | Billing — **optional, not now** | section 13 |
| — | **Google Workspace** | Operator inbox — **future** | section 14 |

**Cloud deploy:** **production only** ([docs/ARCHITECTURE.md](./ARCHITECTURE.md)).

**Backlog issues:** each step above has a `DEV-xxx` issue — see [docs/BACKLOG.md](./BACKLOG.md).

---

## 3. Registro.br — domain

### 3.1 Domain `onlineportfolio.com.br`

**Status:** ✅ **Registered** — active holder at Registro.br. Production DNS on Vercel (NS `ns1`/`ns2.vercel-dns.com`) — DEV-011 ✅.

| Item | Value |
|---|---|
| **Domain** | `onlineportfolio.com.br` |
| **Registrar** | [Registro.br](https://registro.br) — official `.br` registrar |
| **Dashboard** | [registro.br/login](https://registro.br/login/) |
| **Renewal** | ~R$ 40/year (Pix, boleto, or card) |
| **Holder** | Your CPF/CNPJ — does not transfer to Vercel |
| **DNS now** | ✅ Vercel nameservers (`ns1`/`ns2.vercel-dns.com`) |
| **DNS at deploy** | ✅ Done — section 10 |

| Check | Result |
|---|---|
| `onlineportfolio.com` | **Unavailable** — registered by third parties |
| `onlineportfolio.com.br` | ✅ **Yours** — registered |

#### Verify in Registro.br dashboard

- [x] Domain `onlineportfolio.com.br` with **Active** / **Published** status
- [ ] Note expiration date / auto-renewal
- [ ] Store Registro.br login in password manager
- [x] DNS: nameservers → Vercel (`ns1`/`ns2.vercel-dns.com`) — DEV-011

#### Registro.br references

| Resource | URL |
|---|---|
| Dashboard / login | https://registro.br/login/ |
| DNS help | https://registro.br/ajuda |
| WHOIS | https://registro.br/tecnologia/ferramentas/whois |
| Change nameservers | Dashboard → domain → **Alterar servidores DNS** |

#### Why `.com.br` is still fine

- Your first two artists are in Brazil — `.com.br` is trusted locally
- Vercel, Render, Supabase, Resend, and Google Workspace all support `.com.br` custom domains
- Same architecture: `app.`, `api.`, `{slug}.` subdomains work identically
- Fixed price: **R$ 40/year** with no renewal surprises
- Payment: Pix, boleto, or card — no international card required

**Trade-off:** Less “global SaaS” than `.com` — fine for Brazil v1.

#### Registration complete — what to do now vs at deploy

| When | Action |
|---|---|
| **Now (after purchase)** | Confirm **Active** in dashboard; store credentials; optional: create GitHub, Linear, Supabase accounts |
| **Epic 0 (DEV-001)** | Push monorepo to GitHub |
| **Deploy** | ✅ Vercel nameservers + domains (section 10) |
| **Post-deploy** | ✅ Resend DKIM/DMARC + `mail@` in Vercel DNS (section 10.4) |

#### Registro.br + Vercel nameservers (holder stays at Registro.br)

Changing nameservers to Vercel **does not transfer** the domain. You still own it, renew at Registro.br (R$ 40/year), and manage holder (CPF/CNPJ) there. Vercel only hosts DNS records.

```text
Registro.br  →  holder, renewal, NS → ns1/ns2.vercel-dns.com
Vercel DNS   →  apex, app, *.onlineportfolio.com.br + SSL
Render       →  api.onlineportfolio.com.br (CNAME in Vercel DNS)
```

**Recommended for multi-tenant:** Vercel nameservers + wildcard domain `*.onlineportfolio.com.br` in Vercel project.

**Alternative:** keep DNS at Registro.br and add each subdomain manually — works for few tenants, not ideal at scale.

#### HTTPS / TLS per provider

No separate SSL purchase. Certificates are free and auto-renewed.

| Provider | Your domains | HTTPS | Requirement |
|---|---|---|---|
| **Vercel** | apex, `app.`, `*.onlineportfolio.com.br` | Auto | Wildcard SSL needs Vercel nameservers |
| **Render** | `api.onlineportfolio.com.br` | Auto | CNAME + verify in Render dashboard |
| **Supabase** | `*.supabase.co` (default) | Auto | Custom domain optional later |
| **Registro.br** | — | No | DNS only |
| **Resend** | — | N/A | SPF/DKIM for email, not web |

All providers accept `.com.br` and subdomains.

#### Future: global `.com` (optional)

`onlineportfolio.com` is taken. If needed later, consider monitoring it for expiry or registering an alternative (e.g. `portfolioonline.com`, `getonlineportfolio.com`) and pointing it to the same Vercel app as an alias.

---

## 4. GitHub (CI/CD) & Actions

| | |
|---|---|
| **Create account** | [github.com/signup](https://github.com/signup) |
| **New repository** | [github.com/new](https://github.com/new) — suggested name: `online-portfolio` |
| **Actions secrets** | Repo → **Settings → Secrets and variables → Actions** |
| **Actions docs** | [docs.github.com/actions](https://docs.github.com/en/actions) |

**Decision:** GitHub Actions is the **pipeline orchestrator**. Vercel and Render are deploy targets — not a substitute for tests and migrations.

### 4.1 Repository

- [x] Code hosted on GitHub
- [x] Branch protection on `main` (recommended)
- [x] Vercel connected — PR previews on; **production auto-deploy off**
- [x] Render connected — **After CI Checks Pass** + root `backend` (section 4.6)

### 4.2 GitHub Actions secrets

| Secret | Purpose |
|---|---|
| `SUPABASE_MIGRATION_CONNECTION_STRING` | Supabase **Session pooler** URI (port **5432**, IPv4) for EF migrations in CI |
| `VERCEL_TOKEN` | `deploy-frontend.yml` — `vercel deploy --prod` |
| `VERCEL_ORG_ID` | Vercel CLI org/team ID |
| `VERCEL_PROJECT_ID` | Vercel project ID (root `frontend`) |
| `RENDER_DEPLOY_HOOK_URL` | Optional — if Render auto-deploy disabled |

### 4.3 Workflow files

```text
.github/workflows/
  ci-backend.yml       # ✅ pull_request + push main: dotnet test + build (paths backend/**)
  ci-frontend.yml      # ✅ pull_request + push main: npm run lint (nuxi typecheck)
  deploy-backend.yml   # ✅ push main: test → EF migrate → gate Render (DEV-007)
  deploy-frontend.yml  # ✅ push main: lint/test → vercel deploy --prod (DEV-007b)
```

**Decision:** **Separate** CI and deploy; **production only** ([docs/ARCHITECTURE.md](./ARCHITECTURE.md)).

### 4.4 Git flow — cross-stack review (front + back)

Beyond tests, verify front↔back pairing before merge. Reference: [docs/ARCHITECTURE.md](./ARCHITECTURE.md) · [docs/AGENT_GUIDE.md](./AGENT_GUIDE.md).

### 4.5 GitHub checklist

- [ ] Repo created and pushed
- [x] `ci-backend.yml`, `ci-frontend.yml` (PR CI — DEV-006)
- [x] `deploy-backend.yml` (DEV-007) — migrate prod + gate Render
- [x] `deploy-frontend.yml` (DEV-007b)
- [x] `SUPABASE_MIGRATION_CONNECTION_STRING` configured; Render **After CI Checks Pass** enabled

### 4.6 Monorepo — isolated deploy per stack

A push to `main` in the monorepo **must not** redeploy the stack that did not change. Three layers work together:

| Layer | `frontend/**` only | `backend/**` only |
|--------|------------------|-----------------|
| **GitHub Actions** | `ci-frontend.yml` + `deploy-frontend.yml` | `ci-backend.yml` + `deploy-backend.yml` |
| **Render (API)** | No autodeploy | Autodeploy after **Backend Deploy** green |
| **Vercel (frontend)** | Prod via `deploy-frontend.yml` | No production deploy |

**GitHub:** `paths` in deploy workflows (`deploy-backend.yml`, `deploy-frontend.yml`) and in CIs on **push**; on **PR**, workflows always report status (internal skip with `paths-filter` — see `ci-backend.yml` / `ci-frontend.yml`).

**Render** ([monorepo support](https://render.com/docs/monorepo-support)):

| Setting | Value |
|---------|--------|
| **Root directory** | **`backend`** (required) — changes outside `backend/` **do not** trigger autodeploy |
| **Build Filters → Included paths** (optional, reinforcement) | `backend/**`, `.github/workflows/deploy-backend.yml`, `docs/DATABASE.md` |
| **After CI Checks Pass** | On — wait for commit checks (incl. **Backend Deploy**) |

**Vercel** ([monorepo](https://vercel.com/docs/monorepos)):

| Setting | Value |
|---------|--------|
| **Root directory** | **`frontend`** — changes outside `frontend/` **do not** trigger build/deploy for this project |
| **Production auto-deploy** | **Off** — production only via `deploy-frontend.yml` (paths `frontend/**`, …) |
| **PR previews** | On — previews only when the PR changes files under `frontend/` |

```
push main with frontend/ only
  → Frontend CI (push) · deploy-frontend
  → Render API: no
  → Vercel prod: yes (via Actions)

push main with backend/ only
  → Backend CI · Backend Deploy · Render API (after checks)
  → Vercel prod: no
```

---

## 5. Linear (project management)

Issue and sprint management. Each `DEV-xxx` from [BACKLOG.md](./BACKLOG.md) becomes a Linear issue.

| | |
|---|---|
| **Create account** | [linear.app/signup](https://linear.app/signup) |
| **Dashboard** | [linear.app](https://linear.app) |
| **Docs** | [linear.app/docs](https://linear.app/docs) |
| **Import CSV** | Linear → **Settings → Import** (optional) |
| **Backlog source** | [docs/BACKLOG.md](./BACKLOG.md) |

**Cost:** **Free** plan covers a small workspace — confirm at [linear.app/pricing](https://linear.app/pricing).

### 5.1 Create workspace

1. Go to [linear.app/signup](https://linear.app/signup) — sign in with GitHub (recommended) or Google
2. **Create a workspace** — suggested name: `Online Portfolio`
3. Choose **Software development** template (or blank)

### 5.2 Suggested structure (mirrors BACKLOG.md)

| BACKLOG.md | Linear |
|---|---|
| `## Epic 0 — Foundation` etc. | **Project** or **Initiative** |
| `### DEV-xxx` | **Issue** (title: `DEV-xxx — …`) |
| `Area` field | **Label** (`backend`, `frontend`, `infra`, …) |
| `Priority` field (P0–P3) | **Priority** (Urgent / High / Medium / Low) |
| `Depends on` | **Blocked by** relation |
| Acceptance criteria | Checklist in issue description |

**Labels to create:** `infra` · `backend` · `frontend` · `database` · `devops` · `docs` · `security`

### 5.3 Import backlog (Epic 0)

**Manual (recommended):** DEV-000 ✅ Done → create **DEV-001** as next issue; copy acceptance criteria from BACKLOG.

**CSV:** columns `ID`, `Title`, `Description`, `Priority`, `Labels`, `Epic`.

### 5.4 GitHub integration (optional)

Linear → **Settings → Integrations → GitHub** → repo `online-portfolio`. PRs link if title/commit mentions `DEV-001`.

### 5.5 Linear checklist

- [ ] Workspace created
- [ ] `Area` labels configured
- [ ] DEV-000 Done; DEV-001 active
- [ ] (Optional) GitHub integration

---

## 6. Supabase

| | |
|---|---|
| **Create account** | [supabase.com/dashboard](https://supabase.com/dashboard) (GitHub or email) |
| **Dashboard** | [supabase.com/dashboard](https://supabase.com/dashboard) |
| **Docs** | [supabase.com/docs](https://supabase.com/docs) |
| **Pricing** | [supabase.com/pricing](https://supabase.com/pricing) — free tier ok for v1 |

### 6.1 Create project

| Setting | Recommendation |
|---|---|
| **Organization** | Your org |
| **Project name** | `online-portfolio-db-prod` |
| **Region** | Closest to most users (e.g. EU West if artists in Europe) |
| **Database password** | Strong password — store in password manager |

Wait for project provisioning (~2 minutes).

### 6.2 Database — connection strings for EF Core

Go to **Project Settings → Database → Connection string** (or **Connect** in the dashboard).

The project uses **two** Supabase strings in production — **not** direct:

| Use | Mode in Supabase UI | Port | Used by |
|---|---|---|---|
| **Runtime (API)** | Connection pooling → **Transaction** | `6543` | Render `ConnectionStrings__Default` |
| **Migrations (CI)** | Connection pooling → **Session** | `5432` | GitHub secret `SUPABASE_MIGRATION_CONNECTION_STRING` |

**Local dev:** Postgres in Docker Compose (`localhost:5432`) — see [docs/DEV_COMMANDS.md](./DEV_COMMANDS.md). Do not point day-to-day dev at Supabase prod.

**Direct (`db.[ref].supabase.co:5432`):** do not configure in repo or secrets. IPv6 host; GitHub Actions / Render / Vercel are IPv4-only → `Network is unreachable`. Optional only for manual tools (pg_dump, DBeaver) if your network has IPv6 or Supabase IPv4 add-on.

**Why Session pooler in CI?** Same pooler host as runtime, port **5432** (Session), user `postgres.[project-ref]` — IPv4-compatible and works with `dotnet ef`.

**Format (Npgsql / URI):**

```text
# Runtime — Render (Transaction)
Host=aws-1-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.[project-ref];Password=...;SSL Mode=Require;Trust Server Certificate=true

# Migrations — GitHub Actions (Session)
Host=aws-1-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.[project-ref];Password=...;SSL Mode=Require;Trust Server Certificate=true
```

**Settings to verify:**

- [ ] Runtime API: **Transaction** on port **6543** (never use for `dotnet ef`)
- [ ] CI migrate: **Session** on port **5432** (copy from **Connect → Session pooler**)
- [ ] **Do not** store `db.[ref].supabase.co` in migrate secret
- [ ] Enable **SSL** — `SSL Mode=Require` in Npgsql connection string
- [ ] Do **not** expose these strings in frontend or git

**Dev / staging Supabase — out of v1 scope:**

Do not create a second deployed Supabase project for staging. Development uses **local Postgres** (Docker Compose). Decision: [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**DEV-008b (future):** remote Supabase dev project — strings and flow TBD; until then, local = `localhost`.

### 6.3 API keys

Go to **Project Settings → API**.

| Key | Where it goes | Never put in |
|---|---|---|
| **Project URL** | `https://[ref].supabase.co` → Render `Supabase__Url` (Storage, Phase 3+) | — |
| **service_role** | Render `Supabase__ServiceRoleKey` only | Frontend, git, browser |

The **service role** bypasses Storage policies — treat like a root password. **Anon key is not used in v1** (no client-side Supabase SDK).

### 6.4 Authentication — **does not use Supabase Auth**

Admin login uses **ASP.NET Identity + JWT** on the .NET API. Supabase provides **Postgres + Storage** only.

| Concern | Where |
|---|---|
| Passwords | Postgres via EF Identity |
| JWT signing | Render: `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` |
| Login UI | `app.onlineportfolio.com.br/login` → Nuxt proxy → API |
| Invite emails | Resend (API sends on invite) |

No Supabase Auth dashboard configuration in v1.

### 6.5 Storage (Phase 3+)

Go to **Storage → New bucket**.

| Bucket | Public | Purpose |
|---|---|---|
| `artworks-public` | Yes | Web images, thumbnails |
| `artworks-originals` | No | High-res originals (signed URLs) |

**Access model (v1):**

- **Public read:** gallery pages use CDN URLs in `<img>` (no auth)
- **Admin write:** API only, using service role key — no browser → Storage uploads

Path prefix: `tenants/{tenantId}/...`

### 6.6 Network / security (optional hardening)

| Setting | Location | Note |
|---|---|---|
| **Network restrictions** | Database settings | Optional IP allowlist for prod DB (Render egress IPs change on free tier — often skip on free) |
| **RLS on Postgres tables** | SQL | Optional defense-in-depth; API uses service role or direct connection — primary isolation remains in EF |

### 6.7 Supabase checklist summary

**Phase 1 (database only):**

- [ ] Project created
- [ ] Transaction (6543) + Session (5432) pooler strings saved — **not** direct `db.*.supabase.co` for CI
- [ ] Service role key saved (Storage, Phase 3+)
- [ ] Migrations applied via CI or local EF

**Phase 1.5 (auth — API, not Supabase):**

- [ ] Render: `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`
- [ ] Resend: invite + transactional email
- [ ] Test login via Nuxt proxy at `app.onlineportfolio.com.br/login`

**Phase 3 (storage):**

- [ ] Buckets created (public read)
- [ ] API upload path tested (multipart via proxy)
- [ ] Gallery `<img>` CDN URLs working

---

## 7. Render (API)

| | |
|---|---|
| **Create account** | [dashboard.render.com/register](https://dashboard.render.com/register) |
| **Dashboard** | [dashboard.render.com](https://dashboard.render.com) |
| **Docs** | [render.com/docs](https://render.com/docs) |
| **GitHub App** | [Render → Account Settings → GitHub](https://dashboard.render.com) (connect after login) |
| **Pricing** | [render.com/pricing](https://render.com/pricing) — free tier with cold start |

### 7.1 Create web service

| Setting | Value |
|---|---|
| **Type** | Web Service |
| **Source** | Connect GitHub repo |
| **Root directory** | **`backend`** — required in monorepo; push in `frontend/` only does not trigger API (section 4.6) |
| **Runtime** | Docker |
| **Dockerfile path** | `OnlinePortfolio.Api/Dockerfile` (relative to root `backend`) |
| **Branch** | `main` |
| **Region** | Same as Supabase when possible |
| **Instance type** | Free |

### 7.2 Service configuration

| Setting | Value |
|---|---|
| **Health check path** | `/health` |
| **Auto-deploy** | Yes (on push to `main` that changes `backend/`) |
| **After CI Checks Pass** | **On** — wait for commit checks (incl. **Backend Deploy** from `deploy-backend.yml`) |
| **Build Filters** (optional) | Included: `backend/**`, `.github/workflows/deploy-backend.yml`, `docs/DATABASE.md` |
| **Build command** | (Docker handles build) |
| **Start command** | (from Dockerfile `ENTRYPOINT`) |

**Monorepo:** with **Root directory** = `backend`, changes in `frontend/` only **do not** redeploy the API. See section 4.6.

**Free tier behavior:**

- Spins down after ~15 min idle
- Cold starts 5–30+ seconds
- No persistent disk — do not store uploads locally

### 7.3 Custom domain

1. **Settings → Custom Domains → Add** `api.onlineportfolio.com.br`
2. Add CNAME at DNS provider (see section 10.3)
3. Wait for SSL certificate provisioning

### 7.4 Environment variables

Set in **Environment → Environment Variables** (or `render.yaml`):

| Variable | Value | Secret |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | No |
| `ASPNETCORE_URLS` | `http://+:8080` | No |
| `ConnectionStrings__Default` | Supabase pooler URI (`6543`) | Yes |
| `Jwt__Secret` | API JWT signing key | Yes |
| `Jwt__Issuer` | `OnlinePortfolio.Api` | No |
| `Jwt__Audience` | `OnlinePortfolio.Admin` | No |
| `Supabase__Url` | `https://[ref].supabase.co` (Storage, Phase 3+) | No |
| `Supabase__ServiceRoleKey` | Supabase service role | Yes |
| `Resend__ApiKey` | Resend API token (`re_...`) | Yes |
| `Resend__FromEmail` | `mail@onlineportfolio.com.br` | No |
| `Resend__FromName` | Your platform name | No |

**Render UI:** after saving, values are masked (eye icon to reveal). There is no separate “secret” toggle in the current dashboard — still treat `Resend__ApiKey`, `Jwt__Secret`, connection strings, and `service_role` as credentials: password manager + never in git.

**Do not set** `ConnectionStrings__Migration` on Render unless you intentionally run migrations from the container (not recommended — use CI instead).

### 7.5 Render checklist

- [x] Web service created (Docker)
- [x] **Root directory** = `backend` (monorepo — section 4.6)
- [x] **After CI Checks Pass** enabled
- [x] Health check returns 200 at `/health` (`*.onrender.com`)
- [x] All env vars set (DB pooler, `Jwt__*`, `Resend__*`, `ASPNETCORE_*`)
- [x] Custom domain `api.onlineportfolio.com.br` verified + HTTPS (DEV-011)
- [x] Logs visible in Render dashboard
- [x] Test: `GET https://api.onlineportfolio.com.br/health` → Healthy (DEV-011)
- [ ] Test: public API endpoint returns data

---

## 8. Resend (Email)

**v1 decision:** `mail@onlineportfolio.com.br` — send only (contact, invites). No mailbox; **Reply-To** for replies. See [ADR-017](./ADR-017-resend-transactional-email.md) (replaces SendGrid — permanent free tier ended May 2025).

| | |
|---|---|
| **Create account** | [resend.com/signup](https://resend.com/signup) |
| **Dashboard** | [resend.com/domains](https://resend.com/domains) |
| **Docs** | [resend.com/docs](https://resend.com/docs) |
| **API Keys** | Dashboard → **API Keys** |
| **Domain (prod)** | **Domains** → add `onlineportfolio.com.br` — DNS in section 10.4 |
| **.NET SDK** | [resend.com/docs/send-with-dotnet](https://resend.com/docs/send-with-dotnet) · NuGet `Resend` |
| **Pricing** | [resend.com/pricing](https://resend.com/pricing) — **3,000 emails/month** free (max 100/day) |

### 8.1 v1 scope

| Included | Out of v1 scope |
|---|---|
| Contact form, invites | Mailbox / MX |
| `mail@onlineportfolio.com.br` | Resend in frontend |
| Templates in `EmailTemplates/` (repo) | Visual editor in dashboard |

### 8.2 API key (DEV-014)

1. Create Resend account + verify email — ✅
2. **API Keys** → Create → name `portfolio-api-prod` — ✅
3. Copy key `re_...` (shown only once) → **password manager** (never in git)
4. Render → **Environment** → variables below (`Resend__ApiKey` masked in dashboard after save)
5. Local smoke test (section 8.5) — optional before Render

| Render env | Value |
|---|---|
| `Resend__ApiKey` | `re_...` (sensitive — masked on Render) |
| `Resend__FromEmail` | `mail@onlineportfolio.com.br` |
| `Resend__FromName` | Online Portfolio |

Local equivalent: `Resend` section in `appsettings.Development.json` or `Resend__*` via env (see `backend/OnlinePortfolio.Api/appsettings.json`).

### 8.5 Smoke test — first email (before DEV-011)

Validates account + API key **without** custom domain: Resend allows sending from `onboarding@resend.dev` until you verify `onlineportfolio.com.br` (DEV-011).

1. Install SDK (test project or future DEV-107):

```bash
dotnet add package Resend
```

2. Replace `re_xxxxxxxxx` with the real API key (**local only** — never commit).

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

Console.WriteLine(resp);
```

3. **PowerShell** (env var, no hardcode in code):

```powershell
$env:Resend__ApiKey = "re_xxxxxxxxx"   # paste from password manager
```

4. After **DEV-011** (domain verified), smoke test with custom domain — see section 8.6.

Production reference (DI in `Program.cs` — implementation in DEV-107):

```csharp
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
    o.ApiToken = builder.Configuration["Resend:ApiKey"]!);
builder.Services.AddTransient<IResend, ResendClient>();
```

### 8.6 Smoke test — verified domain (after DEV-011)

Validates DKIM/SPF/DMARC + `mail@` sender. Can use Insomnia, curl, or the SDK.

**HTTP** — `POST https://api.resend.com/emails` (no trailing slash in URL)

| Header | Value |
|---|---|
| `Authorization` | `Bearer re_...` |
| `Content-Type` | `application/json` |

**Body (example):**

```json
{
  "from": "Online Portfolio <mail@onlineportfolio.com.br>",
  "to": ["seu-email@exemplo.com"],
  "reply_to": "seu-email@exemplo.com",
  "subject": "hello world",
  "html": "<p>it works!</p>"
}
```

- `reply_to`: email you read (operator v1) — the **Reply** button in the email client goes to this address, not to `mail@`.
- Contact form (DEV-107): `reply_to` = **visitor** email (artist replies directly).
- Resend dashboard → **Emails** → **Delivered** status.

**.NET SDK** (equivalent):

```csharp
await resend.EmailSendAsync(new EmailMessage()
{
    From = "Online Portfolio <mail@onlineportfolio.com.br>",
    To = "seu-email@exemplo.com",
    ReplyTo = "seu-email@exemplo.com",
    Subject = "hello world",
    HtmlBody = "<p>it works!</p>",
});
```

### 8.3 Domain and sender

- **DEV-011 (prod):** domain verified in Resend — DNS records in Vercel (section 10.4), including `_dmarc`; `mail@` sender on Render
- Until DKIM verified: tests with Resend onboarding domain or dev key (do not use in prod)
- `mail@` has no inbox in v1 — always set **Reply-To** when replies make sense

### 8.4 Resend checklist

- [x] Resend account created
- [x] API key `portfolio-api-prod` created
- [x] API key in password manager
- [x] `Resend__*` on Render (`Resend__FromEmail` = `mail@onlineportfolio.com.br`)
- [x] Smoke test sent with verified domain (section 8.6)
- [x] Domain `onlineportfolio.com.br` verified (DEV-011)
- [ ] `IEmailService` + `ResendEmailService` (DEV-107)

---

## 9. Vercel (Frontend)

| | |
|---|---|
| **Create account** | [vercel.com/signup](https://vercel.com/signup) |
| **Dashboard** | [vercel.com/dashboard](https://vercel.com/dashboard) |
| **Docs** | [vercel.com/docs](https://vercel.com/docs) |
| **GitHub App** | Import project → connect GitHub repository |
| **Domains** | [vercel.com/docs/projects/domains](https://vercel.com/docs/projects/domains) |

### 9.1 Create project

| Setting | Value |
|---|---|
| **Import** | GitHub repository |
| **Root directory** | **`frontend`** — required in monorepo; push in `backend/` only does not trigger this project (section 4.6) |
| **Framework preset** | Nuxt.js (auto-detected) |
| **Production auto-deploy** | **Off** — prod only via `deploy-frontend.yml` (paths `frontend/**`; section 4.3) |

**Monorepo:** with **Root directory** = `frontend` + production auto-deploy off, changes in `backend/` only **do not** publish Nuxt to production. PR previews remain limited to changes under `frontend/`.

### 9.2 Environment variables

| Variable | Production example | Secret |
|---|---|---|
| `NUXT_PUBLIC_API_BASE` | `/api` (Nuxt proxy) | No |
| `NUXT_PUBLIC_PLATFORM_HOST` | `onlineportfolio.com.br` | No |
| `NUXT_PUBLIC_APP_HOST` | `app.onlineportfolio.com.br` | No |
| `NUXT_API_INTERNAL_BASE` | `https://api.onlineportfolio.com.br` | No |

### 9.3 Domains

Add in **Project → Settings → Domains** (DNS details in section 10):

| Domain | Purpose |
|---|---|
| `onlineportfolio.com.br` | Marketing |
| `app.onlineportfolio.com.br` | Admin |
| `*.onlineportfolio.com.br` | Tenant subdomains |

Custom domains per artist → section 12.

### 9.4 Vercel checklist

- [x] Project `online-portfolio-web` connected; **Root directory** = `frontend` (monorepo — section 4.6)
- [x] Env vars set; PR previews on (tested)
- [x] Production auto-deploy **off** (`Only build pre-production`; prod via `deploy-frontend.yml` — DEV-007b)
- [x] `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` in password manager
- [x] Same three secrets in **GitHub Actions** (DEV-007b)
- [x] Custom domains + `NUXT_API_INTERNAL_BASE` → `https://api.onlineportfolio.com.br` (DEV-011)
- [ ] Web Analytics enabled in dashboard + code [DEV-110](./BACKLOG.md#dev-110--vercel-web-analytics) (optional; prod domains ✅)

### 9.5 Web Analytics (optional — DEV-110)

Aggregated page view metrics in the Vercel dashboard — **does not** replace error tracking (Sentry etc.).

| | |
|---|---|
| **Docs** | [vercel.com/docs/analytics](https://vercel.com/docs/analytics) |
| **Package** | `@vercel/analytics` + Nuxt module |
| **When** | Prod domains ✅ — ideal once public sites are live (Epic 1) |
| **Priority** | P2 — optional |

**Enable in dashboard**

1. Vercel → project `online-portfolio-web` → **Analytics** → **Enable Web Analytics**
2. In repo (DEV-110): `npm i @vercel/analytics` in `frontend/`
3. `nuxt.config.ts`: `modules: ['@vercel/analytics']`
4. **Conditional plugin:** load analytics **only** if `surface === 'platform'` or `surface === 'tenant'` — **not** on `app.*` (admin/login), aligned with [ADR-018](./ADR-018-frontend-ui-motion-stack.md)
5. Prod deploy: `deploy-frontend.yml` (or **Run workflow**)
6. Validate: browse pages on `onlineportfolio.com.br` or `{slug}.` → data in dashboard (~30s)

**v1 limitations**

| Item | Note |
|---|---|
| Per-tenant separation | Vercel dashboard aggregates the deployment; filter by host/URL manually |
| Admin | Exclude `app.*` in code — avoids inflating metrics with operator usage |
| LGPD | Artist public sites → privacy policy when you have real customers |
| Adblock | May undercount visitors |

**Do not confuse with:** Speed Insights (Core Web Vitals) — another Vercel product; adopt only if there is a separate ticket.

---

## 10. DNS at deploy

Configure **after** Render (section 7) and Vercel (section 9) exist.

### 10.1 DNS strategy (recommended — Vercel nameservers)

1. Registro.br → **Alterar servidores DNS**:
   ```text
   ns1.vercel-dns.com
   ns2.vercel-dns.com
   ```
2. Vercel → **Settings → Domains** → add:
   ```text
   onlineportfolio.com.br
   app.onlineportfolio.com.br
   *.onlineportfolio.com.br
   ```
3. Vercel DNS → CNAME `api` → `your-service.onrender.com`
4. Wait for SSL (minutes after propagation)

**Alternative:** manual DNS at Registro.br — no easy wildcard SSL; see records below.

### 10.2 DNS records reference (Vercel)

| Type | Name | Value | Purpose |
|---|---|---|---|
| `A` | `@` | Vercel IP (dashboard) | Apex |
| `CNAME` | `www` | `cname.vercel-dns.com` | www |
| `CNAME` | `app` | `cname.vercel-dns.com` | Admin |
| `CNAME` | `*` | `cname.vercel-dns.com` | Wildcard tenants |

### 10.3 Records for Render (API)

| Type | Name | Value | Purpose |
|---|---|---|---|
| `CNAME` | `api` | Render hostname (`xxx.onrender.com`) | API custom domain |

Enable **HTTPS** on Render after DNS verification.

### 10.4 Email DNS — Resend (v1, send only)

In **Vercel DNS** (no MX for mailbox — send only):

1. Resend → **Domains** → Add `onlineportfolio.com.br`
2. Copy exact records from dashboard (DKIM, SPF — account-generated values)
3. Add in Vercel DNS as indicated (e.g. `resend._domainkey`, `send` subdomain, `_dmarc`, etc.)
4. **Verify DNS Records** in Resend

**TTL on Vercel:** when creating or editing records manually, leave **TTL = Auto** (Vercel default ≈ 60s). If the Resend→Vercel integration created records with `3600`, edit each one (⋯ → Edit) and set TTL to **Auto**. Not required if the domain is already Verified — aligns with Vercel/Resend recommendation.

| Typical type | Notes |
|---|---|
| `TXT` / `CNAME` DKIM | E.g. `resend._domainkey` — exact value from dashboard |
| `TXT` SPF | `send` subdomain (or as Resend specifies) |
| `MX` (bounce) | If Resend specifies for send subdomain |
| `TXT` `_dmarc` | E.g. `v=DMARC1; p=none; rua=mailto:seu-email@...` — see [Resend DMARC](https://resend.com/docs/dashboard/domains/dmarc). **Required** for good deliverability in prod. |

Do not reuse DNS records from other providers (e.g. `sendgrid.net`) — use only Resend-generated records. See [ADR-017](./ADR-017-resend-transactional-email.md).

When adding Google Workspace later, **merge SPF** per Resend + Google docs.

---

## 11. QuestPDF (no external account)

QuestPDF is a **NuGet package** in the .NET API — no external account or API key.

| Item | Action |
|---|---|
| **License** | Set `LicenseType.Community` in code at startup |
| **Eligibility** | Free if org revenue < $1M USD/year |
| **Configuration** | None — optional layout/assets in repo |

See [QuestPDF license](https://www.questpdf.com/license/).

---

## 12. Custom domains per tenant (Phase 4)

When an artist uses their own domain (e.g. `ana-art.com`) instead of `{slug}.onlineportfolio.com.br`.

### 12.1 Platform (your side)

1. Add domain in **Vercel → Project → Domains**
2. Vercel shows DNS records for artist to configure
3. After verification, store in database: `Tenant.CustomDomain`, `CustomDomainVerifiedAt`
4. Nuxt middleware resolves tenant from `Host` header

### 12.2 Artist (their side)

| Record | Value |
|---|---|
| `CNAME` `www` | `cname.vercel-dns.com` |
| `A` `@` or ALIAS | Vercel apex records (dashboard) |

Admin stays at `app.onlineportfolio.com.br` — artist domain is public gallery only.

### 12.3 Subdomain tenants (default)

`ana.onlineportfolio.com.br` — wildcard `*.onlineportfolio.com.br` on Vercel; `Slug = "ana"` in database.

---

## 13. Stripe — SaaS billing (Phase 4, **optional**)

**Status:** ⏸️ **Out of initial scope** — **no billing in v1**; **do not create Stripe account** until you decide to bill ([DEV-403](./BACKLOG.md), P3 / skip).

### v1 decision

| Item | v1 (now) | Future (if/when billing) |
|---|---|---|
| Automatic billing | **No** | Checkout + subscription |
| Stripe / PSP account | **Do not create** | Evaluate when needed |
| Plans / limits | Operator sets manually (or no hard limit) | `subscriptions` + webhooks |
| Artist onboarding | Manual by operator | Can stay manual |

### What it is for (when implemented)

**Payment processor** for **monthly subscription** from artists (tenants). Only if you start charging for the platform.

| Function | What it does |
|---|---|
| **Recurring subscription** | Artist pays monthly (Basic, Pro, … plans) |
| **Checkout** | Payment link/page |
| **Customer Portal** | Artist updates payment, views invoices, cancels |
| **Webhooks** | API receives events and updates `subscriptions` in Postgres |
| **Plan limits** | E.g. artwork count, custom domain on Pro only |

**Does not replace:** Resend, Vercel/Render, Identity (login).

```text
Artist → Checkout → webhook → API .NET → Postgres (subscription) → tenant limits
```

### Brazil vs. international expansion

| Scenario | Preference | Reason |
|---|---|---|
| **Brazil only** | Asaas, Iugu, or similar | Recurring PIX, boleto, BR tax workflow |
| **Expand outside Brazil** | **Stripe** (or Adyen) | Multi-currency, global cards, same API in multiple countries |
| **Brazil now + global later** | Stripe or hybrid (decide in ADR) | Stripe covers BR (PIX/card) and scales internationally |

**Summary:** if the goal includes **artists outside Brazil**, Stripe (or equivalent global gateway) tends to be **better** than Brazil-only PSP. For **100% Brazil**, local players may be more practical. **Open decision** — implement only when billing.

| | |
|---|---|
| **Create account** | [dashboard.stripe.com/register](https://dashboard.stripe.com/register) |
| **Dashboard** | [dashboard.stripe.com](https://dashboard.stripe.com) |
| **Docs** | [docs.stripe.com](https://docs.stripe.com/) |

### Planned configuration (if DEV-403 is done)

| Item | Where |
|---|---|
| Products / Prices | Chosen PSP dashboard |
| Webhook URL | `https://api.onlineportfolio.com.br/api/v1/webhooks/stripe` (or equivalent) |
| `Stripe__WebhookSecret` | Render env |
| `Stripe__SecretKey` | Render env — **never** in frontend |

### Stripe checklist (only when billing)

- [ ] PSP decision documented (Stripe vs. local vs. hybrid)
- [ ] Account verified; products/prices created
- [ ] Webhook + handler in API
- [ ] Customer Portal enabled (if Stripe)
- [ ] Test with test cards/methods

---

## 14. Google Workspace (operator mailbox — future)

**Status:** ⏸️ Out of v1 scope. Resend covers **send**; Google Workspace covers **inbox** (`hello@`, `marcelo@`, …).

| | Resend (v1) | Google Workspace (future) |
|---|---|---|
| **Function** | App **sends** (`mail@`, contact, invites) | You **read/reply** |
| **Mailbox** | No | Yes |
| **Cost** | Free tier (100/day) | ~US$ 6–7/user/month |

| | |
|---|---|
| **Create account** | [workspace.google.com](https://workspace.google.com/) |
| **Dashboard** | [admin.google.com](https://admin.google.com) |

When adding Google, **merge SPF** per Resend + Google documentation (do not copy `include:sendgrid.net`).

---

## 15. Secrets & environment variable map

| Secret / config | Configured in | Used by |
|---|---|---|
| Supabase Transaction pooler (`:6543`) | Render | API runtime (EF) |
| Supabase Session pooler (`:5432`) | GitHub Actions secret `SUPABASE_MIGRATION_CONNECTION_STRING` | EF migrations (CI only) |
| Supabase service role key | Render env | API Storage writes (Phase 3+) |
| `Jwt__Secret` | Render env | API-issued JWT signing |
| Resend API key | Render env | API email (invites + contact) |
| Resend from email/name | Render env | API email headers |
| `NUXT_PUBLIC_*` vars | Vercel env | Nuxt client + build |
| Stripe keys (optional) | Render env | Billing webhooks — only if DEV-403 |
| Database password | Supabase (set at create) | Embedded in connection strings |

**Never commit:** connection strings, `service_role`, `Jwt__Secret`, Resend/Stripe keys. Frontend: `frontend/.env.example` placeholders only; backend: `appsettings` templates + Render env.

---

## 16. Phase checklists

### Phase 0 — Foundation

| Provider | Configure |
|---|---|
| **Registro.br** | ✅ Domain — DNS at deploy section 10 |
| **GitHub** | Repo + workflows section 4 |
| **Linear** | Workspace section 5 |
| **Supabase** | `online-portfolio-db-prod` (Render/CI) + optional `online-portfolio-db-dev` (local `appsettings` only) |

### Phase 1 — Public portfolio + contact

| Provider | Configure |
|---|---|
| Render, Resend, Vercel | section 7 · section 8 · section 9 |
| DNS | section 10 |

### Phase 4 — Custom domains (+ optional billing)

| Provider | Configure |
|---|---|
| Vercel | Domains per tenant section 12 |
| Stripe / PSP | **Optional** — only if billing section 13 |
| Google Workspace | Operator inbox (optional) section 14 |

---

## 17. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| API 500 on DB connect | Wrong connection string or pooler mode | Transaction pooler on 6543; check SSL |
| EF migrations fail in CI (`Network is unreachable`, IPv6) | Secret uses `db.*.supabase.co` (direct, IPv6) | Change secret to **Session pooler** `:5432` (`aws-*-*.pooler.supabase.com`, user `postgres.[ref]`) |
| EF migrations fail | Transaction pooler (`6543`) for migrate | Use **Session** pooler `:5432` |
| Login 401 | Wrong password or inactive user | Identity seed; proxy forwards body |
| CORS error | Direct browser → Render | Nuxt `/api` proxy only |
| Resend emails not arriving | Domain not authenticated | DKIM/SPF section 10.4 |
| Wildcard SSL fail | DNS wildcard missing | section 10.2 |
| JWT invalid | Wrong `Jwt__Secret` | Verify Render env |

---

## Quick reference — URLs to bookmark

**Setup order:** Registro.br → GitHub → Linear → Supabase → Render → Resend → Vercel → DNS.

| Service | Create account | Dashboard | Docs |
|---|---|---|---|
| **Registro.br** | [registro.br](https://registro.br) | [Login](https://registro.br/login/) | [Help](https://registro.br/ajuda) |
| **GitHub** | [Signup](https://github.com/signup) | [github.com](https://github.com) | [Actions](https://docs.github.com/en/actions) |
| **Linear** | [Signup](https://linear.app/signup) | [linear.app](https://linear.app) | [Docs](https://linear.app/docs) |
| **Supabase** | [Dashboard](https://supabase.com/dashboard) | [Dashboard](https://supabase.com/dashboard) | [Docs](https://supabase.com/docs) |
| **Render** | [Register](https://dashboard.render.com/register) | [Dashboard](https://dashboard.render.com) | [Domains](https://render.com/docs/custom-domains) |
| **Resend** | [Signup](https://resend.com/signup) | [Dashboard](https://resend.com/domains) | [Domain setup](https://resend.com/docs/dashboard/domains/introduction) |
| **Vercel** | [Signup](https://vercel.com/signup) | [Dashboard](https://vercel.com/dashboard) | [Domains](https://vercel.com/docs/projects/domains) |
| **Stripe** (optional) | [Register](https://dashboard.stripe.com/register) | [Dashboard](https://dashboard.stripe.com) | [Webhooks](https://docs.stripe.com/webhooks) |
| **Google Workspace** | [workspace.google.com](https://workspace.google.com/) | [Admin](https://admin.google.com) | — |
| **QuestPDF** | — | — | [License](https://www.questpdf.com/license/) |
| **BACKLOG** | — | — | [BACKLOG.md](./BACKLOG.md) |
