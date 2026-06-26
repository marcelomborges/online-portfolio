# Architecture Decision Record — Artist Portfolio Platform

Multi-tenant SaaS platform for artist portfolios. One shared application stack serves many artists (tenants), each with an isolated public gallery and admin access.

**Status:** Approved for implementation  
**Platform domain:** `onlineportfolio.com.br` (`onlineportfolio.com` unavailable — registered via Registro.br)  
**Last updated:** 2026-06-23 (ADR-018 frontend UI/motion; ADR-017 Resend; ADR-016 surfaces)

---

## Table of contents

1. [Overview](#1-overview)
2. [Platform domain & operator email](#2-platform-domain--operator-email)
   - [2.1 Domain map and product surfaces](#21-domain-map-and-product-surfaces)
3. [Stack summary](#3-stack-summary)
4. [Multi-tenant model](#4-multi-tenant-model)
5. [Frontend (Nuxt + Vercel)](#5-frontend-nuxt--vercel)
    - [ADR-016: Three surfaces and per-tenant UI](#adr-016-three-surfaces-and-per-tenant-ui)
6. [Backend (ASP.NET Core + Render)](#6-backend-aspnet-core--render)
7. [Database (Supabase PostgreSQL + EF Core)](#7-database-supabase-postgresql--ef-core)
8. [Storage (Supabase Storage)](#8-storage-supabase-storage)
9. [Authentication & authorization](#9-authentication--authorization)
10. [API design](#10-api-design)
11. [Email (Resend)](#11-email-resend)
    - [ADR-017: Transactional email via Resend](#adr-017-transactional-email-via-resend)
12. [PDF generation (QuestPDF)](#12-pdf-generation-questpdf)
13. [Custom domains per tenant](#13-custom-domains-per-tenant)
14. [Local development (Docker Compose)](#14-local-development-docker-compose)
15. [Deployment & CI/CD](#15-deployment--cicd)
    - [ADR-015: Production-only deploy](#adr-015-production-only-deploy)
16. [Tenant provisioning](#16-tenant-provisioning)
17. [Data model (v1)](#17-data-model-v1)
18. [Security requirements](#18-security-requirements)
19. [Phased rollout](#19-phased-rollout)
20. [Repository structure](#20-repository-structure)
21. [Environment variables](#21-environment-variables)
22. [Open decisions / future work](#22-open-decisions--future-work)
23. [Testing](#23-testing)

**Related docs:** [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) · [BACKLOG.md](./BACKLOG.md) · [DATABASE.md](./DATABASE.md)

---

## 1. Overview

### Product goal

Sell portfolio sites to many artists. Each tenant gets:

- A **public site** per domain/subdomain: landing + posts/gallery (e.g. `ana.onlineportfolio.com.br`)
- Optional **visual identity per tenant** on the public site (themes, layouts, components per slug) — see [ADR-016](#adr-016-three-surfaces-and-per-tenant-ui)
- An **admin area** shared and standardized on `app.onlineportfolio.com.br`
- Optional custom domain for the public site (e.g. `ana-art.com` → same content as `ana.`)
- Platform subdomain fallback (e.g. `ana.onlineportfolio.com.br`)

### Architectural principles

| Principle | Decision |
|---|---|
| One app, many tenants | Shared Nuxt app, shared API, shared Supabase project |
| Data isolation | `TenantId` on all tenant-scoped data; enforced in API and Storage |
| API as gatekeeper | All Postgres writes and admin auth go through .NET API + EF Core |
| BFF on frontend | Browser → Nuxt proxy → API; exception: `<img>` with public Storage URL |
| Deploy separately, develop together | Docker Compose locally; Vercel + Render + Supabase in production |
| Start simple, scale deliberately | Manual domain onboarding first; automate with Vercel Domains API later |

### High-level diagram

```text
┌─────────────────────────────────────────────────────────────────────────┐
│                              VISITORS                                    │
│         ana.onlineportfolio.com.br    mark.onlineportfolio.com.br          │
└───────────────────────────────────┬─────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    VERCEL — Nuxt 3 (single deployment)                   │
│  • Resolves tenant from Host header (custom domain or subdomain)         │
│  • SSG/ISR for public tenant sites (landing + posts per tenant)            │
│  • Host `app.*` → standardized admin; Host `{slug}.*` → public site       │
└───────────────────────────────────┬─────────────────────────────────────┘
                                    │
              ┌─────────────────────┴─────────────────────┐
              │                                           │
              ▼                                           ▼
┌──────────────────────────────┐            ┌──────────────────────────────┐
│  app.onlineportfolio.com.br        │            │  api.onlineportfolio.com.br        │
│  Admin UI (Nuxt)             │            │  RENDER — ASP.NET Core API   │
│  single login (centralized)  │            │  Auth + Identity + JWT       │
└──────────────────────────────┘            └──────────────┬───────────────┘
                                                           │
                                                           ▼
                                            ┌──────────────────────────────┐
                                            │  SUPABASE                     │
                                            │  • PostgreSQL (EF + migrations)│
                                            │  • Storage (tenant paths)      │
                                            └──────────────────────────────┘
```

---

## 2. Platform domain & operator email

### Platform domain

| Item | Decision |
|---|---|
| **Primary domain** | `onlineportfolio.com.br` |
| **`.com` status** | `onlineportfolio.com` — **unavailable** (already registered) |
| **Registrar** | [Registro.br](https://registro.br) — official `.br` registry |
| **Cost** | R$ 40/year (fixed; Pix, boleto, or card) |
| **Status** | ✅ **Registered** — active holder at Registro.br; DNS at deploy ([docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md)) |

### Production hostnames

| Host | Purpose |
|---|---|
| `onlineportfolio.com.br` | **Platform** landing (SaaS marketing) |
| `www.onlineportfolio.com.br` | Redirect or alias to apex |
| `{slug}.onlineportfolio.com.br` | Tenant **public** site (landing + posts/gallery) — e.g. `ana.`, `mark.` |
| `app.onlineportfolio.com.br` | **Standardized admin** — single login for all users |
| `api.onlineportfolio.com.br` | .NET API (Render) — not exposed directly to the browser |
| `{custom-domain}` | Tenant public site (Phase 4) — e.g. `ana-art.com` |

Registering the domain **now** protects each `@onlineportfolio.com.br` address for future use — Google Workspace is not required in v1.

### 2.1 Domain map and product surfaces

Reference for what we discussed — **one Nuxt app**, routing by `Host`:

```text
┌─────────────────────────────────────────────────────────────────────────┐
│  onlineportfolio.com.br          →  platform landing (product)        │
├─────────────────────────────────────────────────────────────────────────┤
│  ana.onlineportfolio.com.br      →  Ana's public site (landing+posts)   │
│  mark.onlineportfolio.com.br     →  Mark's public site                  │
│  {slug}.onlineportfolio.com.br   →  one site per tenant                 │
│  ana-art.com (future)            →  same public content as Ana          │
├─────────────────────────────────────────────────────────────────────────┤
│  app.onlineportfolio.com.br      →  STANDARDIZED admin (all tenants)  │
│    /login                        →  single login (Owner, Editor, Ops)   │
│    /admin                        →  tenant panel (after login)          │
│    /platform/tenants             →  tenant management (PlatformAdmin)   │
├─────────────────────────────────────────────────────────────────────────┤
│  api.onlineportfolio.com.br      →  C# backend (proxy via Nuxt on front)│
└─────────────────────────────────────────────────────────────────────────┘
```

| Surface | Per tenant? | Domain | Notes |
|---|---|---|---|
| Landing + posts/gallery | **Yes** | `{slug}.onlineportfolio.com.br` or custom domain | Layout/theme/components per tenant — [ADR-016](#adr-016-three-surfaces-and-per-tenant-ui) |
| Admin (login, CRUD, settings) | **No** — single UI | `app.onlineportfolio.com.br` | Tenant comes from API after login, not from hostname |
| Login | **No** | `app.../login` only | No `ana./login` in v1 |
| API | **No** | `api...` (via proxy) | Browser does not call Render directly |

**Summary flow:**

```text
Visitor   →  ana.onlineportfolio.com.br     →  sees Ana's portfolio
Artist    →  app.onlineportfolio.com.br     →  edits Ana's content (login)
Operator  →  app.../platform/tenants        →  creates tenants, invites users
```

**Frontend → backend (BFF):** all data and admin actions go through Nuxt → .NET API. **Exception:** public images rendered with Storage CDN URL in `<img>`.

**Auth:** full ASP.NET Identity + JWT on API — no Supabase Auth in the browser. See section 9.

### Email v1 — Resend `mail@` (send-only, no mailbox)

**Decision:** transactional sending via **Resend** ([ADR-017](./ADR-017-resend-transactional-email.md)). **Do not** configure mailbox, MX for receiving, Google Workspace, or email at Registro.br.

| Item | v1 |
|---|---|
| **Sender** | `mail@onlineportfolio.com.br` |
| **Provider** | Resend (free tier — 3,000 emails/month, max 100/day) |
| **Who sends** | .NET API (server-side) |
| **Mailbox** | **None** — `mail@` does not receive replies in v1 |
| **Registro.br** | Domain registration only; no email server |

**What Resend sends in v1:**

| Email | Phase |
|---|---|
| Contact form → artist email (`ContactEmail`) | 1 |
| User invite (accept-invite link) | 1.5 |
| Platform notifications | later |

**Important:**

- `mail@` is a **sending identity**, not an inbox. Replies use **Reply-To** (visitor on contact; operator on invites/notifications), not an `@onlineportfolio.com.br` mailbox.
- Domain authentication (SPF/DKIM) in **Vercel DNS** improves deliverability — see [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).
- When you need to **read** email on the domain (`hello@`, `marcelo@`), then use Google Workspace or similar — see below.

### Operator mailbox (Google Workspace — later)

| Item | Decision |
|---|---|
| **Provider** | Google Workspace (paid) |
| **Status** | **Out of v1** — human inbox in the future |
| **Planned addresses** | e.g. `hello@onlineportfolio.com.br`, `marcelo@onlineportfolio.com.br` |
| **When** | After launch or when you need to read/reply on the domain |
| **Estimated cost** | ~US$ 6–7/user/month (Business Starter) |

**Two email systems — do not confuse:**

| System | Function | When |
|---|---|---|
| **Resend** | App **sends** (`mail@`, contact, invites) | v1 |
| **Google Workspace** | You **read/reply** on the domain | future |

Resend (SPF/DKIM) and Google (MX) can coexist on the same domain when Workspace is configured. In v1, **Resend only**.

### Domain purchase (Registro.br)

Buy at [registro.br](https://registro.br) first; **DNS can wait** until deploy.

| Step | Action |
|---|---|
| 1 | Search `onlineportfolio.com.br` → Register |
| 2 | Pay R$ 40/year (Pix, boleto, or card) |
| 3 | Confirm status **Active** in Registro.br panel |
| 4 | Keep default DNS for now — configure when Vercel/Render are ready |

Detailed steps: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md)

### Registro.br + Vercel nameservers (does not transfer ownership)

Using Vercel nameservers **does not change** who owns the domain. Registro.br remains registrar and renewal (R$ 40/year); Vercel only manages DNS records.

```text
Registro.br   →  holder, renewal, nameservers pointing to Vercel
Vercel DNS    →  apex, app, wildcard tenants, SSL records
Render        →  api.onlineportfolio.com.br (CNAME in Vercel DNS)
```

| Question | Answer |
|---|---|
| Lose Registro.br? | **No** — ownership and renewal stay there |
| Change the domain? | **No** — still `onlineportfolio.com.br` |
| Configure at purchase? | **Optional** — can defer DNS until deploy |

**Recommended DNS strategy (multi-tenant):** at deploy time, set Registro.br nameservers to:

```text
ns1.vercel-dns.com
ns2.vercel-dns.com
```

Then in Vercel project → Domains, add:

```text
onlineportfolio.com.br
app.onlineportfolio.com.br
*.onlineportfolio.com.br
```

**Alternative (few tenants, DNS only at Registro.br):** add each subdomain manually (`app.`, `ana.`, …) without wildcard — more work as tenants grow.

### HTTPS / TLS (automatic — no certificate purchase)

All hosting providers issue and renew certificates for free. You do **not** buy SSL separately.

| Provider | Domains | HTTPS | Notes |
|---|---|---|---|
| **Vercel** | `onlineportfolio.com.br`, `app.`, `*.onlineportfolio.com.br` | Auto (Let's Encrypt) | Wildcard SSL requires Vercel nameservers |
| **Render** | `api.onlineportfolio.com.br` | Auto (Let's Encrypt + Google Trust) | Single CNAME; HTTP → HTTPS redirect |
| **Supabase** | `*.supabase.co` (default URL) | Auto | Custom domain optional later (paid) |
| **Registro.br** | — | No | DNS only |
| **Resend** | — | N/A | Email authentication (SPF/DKIM), not web hosting |

Provider acceptance of `.com.br` and subdomains: **yes** — TLD is not a blocker.

---

## 3. Stack summary

| Layer | Technology | Hosting | Notes |
|---|---|---|---|
| **Domain** | `onlineportfolio.com.br` | Registro.br | See section 2 |
| Frontend | Nuxt 3.21 · Vue 3.5 | Vercel (Hobby → Pro as needed) | Single app, multi-tenant routing |
| Backend | ASP.NET Core Web API (.NET 10 LTS) | Render (free tier, Docker) | One API for all tenants |
| Database | PostgreSQL | Supabase | Access only via EF Core |
| File storage | Supabase Storage | Supabase | Image uploads; not on Render disk |
| Auth | full ASP.NET Identity + JWT | API (.NET) | Roles, standard stores and token providers; Supabase = DB + Storage |
| Email (app) | Resend | Resend (free) | `mail@` — transactional send only; Reply-To; no mailbox |
| Email (operator) | Google Workspace | Google (later) | Human inbox — **not v1** |
| PDF | QuestPDF | NuGet in API (no extra host) | Portfolio catalog export; API generates |
| CI/CD | GitHub Actions + Vercel/Render deploy | GitHub (orchestrator) | See section 15 |
| Local dev | Docker Compose | Developer machine | Postgres + API + Nuxt |
| **Backend tests** | xUnit · Moq · FluentAssertions · Coverlet | GitHub Actions (`ci-backend.yml`) | `dotnet test`; see section 23 |
| **Frontend tests** | Vitest | GitHub Actions (`ci-frontend.yml`) | Component/composable unit tests |

### What we explicitly do not do

- **Database per tenant** — too much operational overhead at this scale
- **Supabase project per tenant** — same reason
- **Separate Vercel/Render deployment per artist** — one deployment, tenant routing
- **Store uploaded files on Render** — no persistent disk on free tier
- **Direct browser → Postgres or Supabase Auth** — no Supabase SDK in frontend
- **Admin login per tenant subdomain** — e.g. no `ana./login`; only `app./login`

---

## 4. Multi-tenant model

### Pattern: shared database, shared schema, row-level isolation

All tenants share one PostgreSQL database. Tenant-owned rows include a `TenantId` column. Isolation is enforced in:

1. **EF Core** — global query filters on `TenantId`
2. **API middleware** — tenant context + membership checks
3. **Supabase Storage** — path prefix `tenants/{tenantId}/...`; writes only via API (service role)

### Definitions

| Term | Meaning |
|---|---|
| **Tenant** | One artist or studio account on the platform |
| **User** | Person who logs in (`ApplicationUser` + roles via `AspNetUserRoles`) |
| **Platform admin** | Operator (you) — creates tenants, support; **manual/optional billing in v1** |
| **Public site** | Tenant gallery visible to anonymous visitors |

### Tenant resolution (public requests)

Nuxt resolves the current tenant on each request:

```text
1. Match Host header to Tenant.CustomDomain (exact; handle www vs apex)
2. Else match subdomain to Tenant.Slug (e.g. ana.onlineportfolio.com.br)
3. Else show platform marketing site or 404
```

### Tenant resolution (authenticated / admin requests)

```text
1. Validate API-issued JWT (or session cookie) in middleware
2. Load User by Id from token → get TenantId + roles (Identity claims)
3. Verify user belongs to tenant being accessed
4. Never trust TenantId sent in request body alone
```

### Roles (v1)

| Role | Scope | Permissions |
|---|---|---|
| **PlatformAdmin** | Platform | Create tenants, **add/manage users per tenant**, plans, support |
| **Owner** | Tenant | Full control of **that tenant's** content and settings (artist admin) |
| **Editor** | Tenant | CRUD content, uploads (optional for v1) |

### User management (v1)

| Who | Can add users to tenant? |
|---|---|
| **PlatformAdmin** (you) | **Yes** — any tenant; main operator flow |
| **Owner** (artist) | **No** in v1 — deferred to v2 (self-service invites) |
| **Editor** | No |

Each tenant has **one or more** admin users (`Owner`, later `Editor`). You (PlatformAdmin) invite via API; the invitee sets a password and logs in at **`app.onlineportfolio.com.br/login`**.

**Constraints (unchanged):**
- One login account (email) → one `users` row (unique email globally)
- One user → one tenant in v1 (no user belonging to multiple tenants)
- Tenant users only access **their** tenant in admin; PlatformAdmin accesses platform routes (tenant list, user management)

---

## 5. Frontend (Nuxt + Vercel)

### Responsibilities

- **Public site per tenant:** landing + posts/gallery at `{slug}.onlineportfolio.com.br` (and custom domain later)
- **Platform:** landing at `onlineportfolio.com.br`
- **Standardized admin:** single UI at `app.onlineportfolio.com.br` (login, CRUD, settings)
- Tenant resolution by `Host` **only on the public site**
- **All** admin traffic and **all** app data: Nuxt server routes → API (BFF pattern)
- **Exception:** public image display via Storage CDN URL in `<img>` (read-only)
- **Components:** three isolated surfaces in the monorepo (`app/`, `platform/`, `public/`) — standardized admin/login; customizable public site per tenant — [ADR-016](#adr-016-three-surfaces-and-per-tenant-ui) · operational guide [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md)

### Rendering strategy

| Page type | Strategy |
|---|---|
| Tenant public: landing, posts, gallery, about | SSG or ISR; cache key includes tenant slug/domain |
| Platform marketing (`onlineportfolio.com.br`) | SSG |
| Admin (`app.*`) | SSR or client-side; auth via proxy → API |
| Contact form | Server route → API |

### Frontend → backend rule (BFF)

**Default:** the browser never calls Supabase or holds service keys. It talks only to Nuxt; Nuxt server routes forward to the API.

```text
Browser → app.onlineportfolio.com.br/api/...  (Nuxt server route / proxy)
        → api.onlineportfolio.com.br/api/v1/...  (Render API)
```

Same pattern for public tenant sites (`ana.onlineportfolio.com.br`, custom domains):

```text
Browser → {tenant-host}/api/...  (Nuxt server route)
        → api.onlineportfolio.com.br/...  (Render API)
```

**Exception — image rendering:** gallery pages may use public Storage URLs in `<img src="...">` (CDN). No auth token in the browser for Storage.

**Why proxy:** avoids CORS per custom domain; keeps JWT in httpOnly cookie on the app origin; single integration surface for the frontend.

### Host routing (Nuxt middleware)

| Host pattern | Mode | Example |
|---|---|---|
| `app.{platform}` | Standardized admin | `app.onlineportfolio.com.br/login` |
| `{slug}.{platform}` | Tenant public site | `ana.onlineportfolio.com.br` |
| `{platform}` / `www.` | Platform landing | `onlineportfolio.com.br` |
| Verified custom domain | Tenant public site | `ana-art.com` |

### Vercel configuration notes

- Single Nuxt project serves all tenants
- Wildcard DNS for subdomains: `*.onlineportfolio.com.br`
- Custom domains added per tenant (manual initially, Vercel Domains API later)
- Check plan limits on number of domains per project
- **Web Analytics (optional):** [DEV-110](./BACKLOG.md#dev-110--vercel-web-analytics) — `@vercel/analytics` only on `platform` + `tenant` surfaces; not on `app.*` admin

### ADR-016: Three surfaces and per-tenant UI

| | |
|---|---|
| **Status** | ✅ Accepted |
| **Date** | 2025-06-21 |
| **Context** | A single Nuxt app serves platform marketing, N tenants' public sites, and centralized admin/login at `app.*`. Clients may need **different** landings, contact pages, and palettes, but login and admin panel must remain **identical** for everyone. Risk to avoid: duplicated code between tenants and mixing public UI with admin UI in the same directory. |
| **Decision** | Organize the frontend into **three surfaces** (modes) resolved by `Host`, with **customization per tenant only on the public site**, via component folders + CSS themes. Login/admin **never** varies per tenant. |
| **Rejected alternatives** | (1) Separate Nuxt deploy per artist — operational cost and loss of monorepo. (2) Single `components/` directory without separation — harder maintenance and encourages copy-paste. (3) UI/login per tenant subdomain (`ana./login`) — already rejected in v1 (section 21). (4) UI config only in DB (layout JSON) — defer; versioned code in repo is source of truth in v1. |

**Surfaces (same Vercel deploy):**

| Surface | `Host` | Nuxt layout | Component folder | Custom per tenant? |
|---|---|---|---|---|
| **App** | `app.{platform}` | `layouts/app.vue` | `components/app/` | **No** — standardized UI |
| **Platform** | apex / `www.` | `layouts/platform.vue` | `components/platform/` | N/A (product) |
| **Public tenant** | `{slug}.{platform}` or custom domain | `layouts/tenant.vue` | `components/public/tenants/{slug}/` (+ `shared/` orchestration only) | **Yes** |
| **Dev** | `localhost` | `layouts/default.vue` | `components/dev/` | Simulable via env |

**Host resolution:** global middleware `resolve-host.global.ts` sets `surface` + `tenantSlug` (composable `useRequestSurface`). On localhost: `NUXT_PUBLIC_DEV_SURFACE` and `NUXT_PUBLIC_DEV_TENANT_SLUG`.

**Per-tenant customization (public site):**

1. **Components** — folder `components/public/tenants/{slug}/{Block}.vue` (landing, contact, and other blocks **exclusive per tenant**). Shared orchestration in `shared/` (e.g. `TenantHome`). Discovery via `import.meta.glob` (`tenantComponentResolver.ts`); resolution via `useTenantComponent('LandingHero')`.
2. **Themes** — CSS tokens `--op-*` in `assets/css/main.css`; per-tenant override in `assets/css/themes/{slug}.css` + `theme-{slug}` class on `<html>` (`useTenantTheme()`). Theme CSS loaded by plugin (no `nuxt.config` edit per slug).
3. **Shared routes** — same URLs (`/`, `/contact`) across all tenants; content varies by slug, not duplicated paths.
4. **Global primitives** — `components/ui/` (`UiButton`, `UiCard`) use `--op-*` tokens; on tenant routes they inherit `theme-{slug}`, elsewhere they inherit `surface-dark`.

**Structural dark mode (fixed):**

- **Single exception:** tenant public site (`surface=tenant`) → `theme-{slug}`.
- **Everything else** → `html.surface-dark` + `assets/css/surfaces/dark.css`: admin, login, platform marketing, dev skeleton, `error.vue`, `app`/`platform`/`default`/`structural` layouts, `app/*` components.
- Errors force `surface-dark` even on tenant host.
- Tenants in repo: **ana** and **joao** only.

**Mandatory rules:**

- Code in `components/app/` **must not** import from `components/public/tenants/`.
- New client with custom UI → folder `public/tenants/{slug}/` with `LandingHero.vue`, `ContactSection.vue`, etc. + `themes/{slug}.css` — **no** manual registry
- Tenant without folder in repo → runtime error (each active artist needs versioned UI)

**Consequences:**

| Positive | Trade-off |
|---|---|
| Ana and João can have fully distinct landings/contact/palettes | Each "premium" tenant requires a folder in repo (PR) — not drag-and-drop in admin |
| Single login/admin — Nuxt UI, fast, no heavy motion ([ADR-018](./ADR-018-frontend-ui-motion-stack.md)) | Tenant site may require GSAP/Lenis/WebGL per slug |
| Thin pages + composables — less repetition | `TenantComponentKey` registry grows as new blocks are added |
| Aligns with SSG/ISR per slug (cache key includes tenant) | Packages (`@nuxt/ui`, GSAP, …) — incremental adoption Epic 1+ |

**Implementation (skeleton):** see `frontend/` — layouts `app`, `platform`, `tenant`, `structural`; tenants `ana`, `joao`; guides [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md) · [ADR-018](./ADR-018-frontend-ui-motion-stack.md).

**When to re-evaluate:**

- Many tenants with 100% custom UI → CMS or block config in DB (Phase 2+).
- Full admin white-label — **out of scope** in v1; admin remains single dark mode.

---

## 6. Backend (ASP.NET Core + Render)

### Responsibilities

- All business logic and authorization
- full ASP.NET Identity: login, logout, roles, invites, JWT issuance and validation
- EF Core data access (filtered by tenant)
- Uploads to Storage via service role (multipart via proxy)
- Platform admin endpoints (tenant provisioning, add user)
- Health check endpoint for Render

### Deployment

| Setting | Value |
|---|---|
| Platform | Render (free tier) |
| Packaging | Docker (multi-stage Dockerfile) |
| Public URL | `api.onlineportfolio.com.br` (single domain for all tenants) |
| Custom domain per tenant | **No** — not needed on Render |
| Persistent disk | **No** — files go to Supabase Storage |

### Render free tier considerations

- Service sleeps after ~15 minutes idle → cold starts (5–30+ seconds)
- Acceptable for admin/write operations at low traffic
- Public reads benefit from Vercel caching (SSG/ISR)
- Health check: `GET /health`

### Migrations at deploy

- **Do not** auto-run `Migrate()` on API startup in production
- Run migrations via CI (GitHub Actions) against Supabase **session pooler** (`:5432`, IPv4)
- Document local command: `dotnet ef database update`

---

## 7. Database (Supabase PostgreSQL + EF Core)

### Access pattern

- **All** database access through EF Core in the .NET API
- No Supabase JS client for Postgres
- Postgres RLS optional (defense in depth); primary isolation is in API + EF filters

### Connection strings

Three destinations in the project — **direct (`db.*.supabase.co`) is not used in the pipeline** (manual tools only, optional: pg_dump, GUI, with IPv6 or [IPv4 add-on](https://supabase.com/docs/guides/platform/ipv4-address) from Supabase).

| Where | `ConnectionStrings:Default` | `ConnectionStrings:Migration` |
|---|---|---|
| **Local dev** | `localhost:5432` (Compose) | `localhost:5432` (`appsettings.Development.json`) |
| **API prod (Render)** | Transaction pooler **`:6543`** | *(do not configure on Render)* |
| **Migrate prod (CI)** | — | Session pooler **`:5432`** → secret `SUPABASE_MIGRATION_CONNECTION_STRING` |

Supabase modes (same pooler host `aws-*-*.pooler.supabase.com`, user `postgres.[project-ref]`):

| Mode | Port | Use in this project |
|---|---|---|
| **Transaction** | `6543` | API runtime on Render |
| **Session** | `5432` | `dotnet ef database update` in CI (IPv4 — GitHub Actions, Render, Vercel) |
| **Direct** | `5432` on `db.[ref].supabase.co` | **Not used** in repo; IPv6 by default |

```text
ConnectionStrings__Default      → Transaction pooler :6543 (Render)
ConnectionStrings__Migration    → Session pooler :5432 (GitHub Actions)
```

**Local:** always `localhost` for normal development. Session pooler against Supabase prod **only** for occasional debug (avoid routine use).

**Supabase dev DB (DEV-008b):** out of v1 — dev uses Compose. When a remote dev project exists, re-evaluate strings (likely same pattern: session `:5432` migrate, transaction `:6543` runtime).

### Migrations

- EF Core migrations live in the backend project
- Applied by CI on merge/deploy to `main` (session pooler)
- Local dev: `dotnet ef database update` against **localhost** (Compose)

### EF Core tenant isolation

- Global query filter: `entity.TenantId == _tenantContext.TenantId`
- Scoped indexes: unique on `(TenantId, Slug)` not globally on `Slug`
- All tenant-scoped entities include `TenantId`

### Primary keys

**Decided:** `uuid` / `Guid` PKs and FKs on domain entities; slugs for human-facing tenant URLs. Rationale, performance notes, and anti-patterns: [docs/DATABASE.md](./DATABASE.md).

---

## 8. Storage (Supabase Storage)

### Purpose

Store artwork images (originals, web-optimized versions, thumbnails). Phase 2 feature; schema and paths designed from the start.

### Path convention

```text
tenants/{tenantId}/artworks/{artworkId}/hero.webp
tenants/{tenantId}/artworks/{artworkId}/original.jpg
tenants/{tenantId}/settings/logo.webp
```

Paths are generated server-side. Client never chooses arbitrary paths.

### Bucket strategy

| Bucket | Access |
|---|---|
| `artworks-public` | Public read (CDN URL in gallery); admin write **via API only** |
| `artworks-originals` (optional) | Private read (signed URLs from API); admin write via API |

### Upload flow (v1 — API only)

```text
1. Admin authenticated via API (cookie/JWT from POST /auth/login)
2. Browser → Nuxt proxy → POST /api/v1/artworks/{id}/images (multipart)
3. API validates membership + tenant → writes to Storage (service role key)
4. API saves metadata in EF (path, public URL, dimensions)
```

No direct browser → Supabase Storage uploads. No Supabase Auth session in the browser.

### Public image display (exception)

Gallery SSG/SSR pages may embed public bucket URLs:

```html
<img src="https://[ref].supabase.co/storage/v1/object/public/artworks-public/tenants/{tenantId}/..." />
```

Read-only; no anon key required for public bucket objects.

### Metadata in EF

`ArtworkImage` stores `StoragePath`, public URL, alt text, sort order, dimensions — not the binary.

---

## 9. Authentication & authorization

### Decision: full ASP.NET Identity

Use **full ASP.NET Identity** on the API — standard .NET stack with roles, EF stores, and token providers. Good training as a dev and solid base to evolve (claims, policies, OAuth later).

| Use (full Identity) | Out of v1 |
|---|---|
| `AddIdentity<ApplicationUser, IdentityRole<Guid>>()` | Supabase Auth |
| `UserManager`, `SignInManager`, `RoleManager` | `@nuxtjs/supabase` in Nuxt |
| `AspNetRoles` + `AspNetUserRoles` (Owner, Editor, PlatformAdmin) | Public self-signup |
| `AddDefaultTokenProviders()` — invite, password reset | OAuth Google/GitHub (may come later) |
| JWT with **role claims** from Identity | Manual auth with homegrown hash |
| `[Authorize(Roles = "...")]` + tenant check in API | |

**Roles (seed in migration):** `PlatformAdmin`, `Owner`, `Editor` — one role per user in v1.

**Tenant + role rule (validated in API):**

| Role | `tenant_id` |
|---|---|
| `PlatformAdmin` | NULL |
| `Owner` / `Editor` | required |

**NuGet packages (reference):**

```text
Microsoft.AspNetCore.Identity.EntityFrameworkCore
Microsoft.AspNetCore.Authentication.JwtBearer
```

### Implementation (backend)

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? InvitedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<Tenant> Tenants { get; set; }
    // ...
}
```

```text
DI registration:
  AddIdentity<ApplicationUser, IdentityRole<Guid>>(options => {
      options.Password.RequiredLength = 8;
      options.Lockout.MaxFailedAccessAttempts = 5;
      options.User.RequireUniqueEmail = true;
  })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

  AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(...);

Seed (migration or startup):
  RoleManager.CreateAsync("PlatformAdmin");
  RoleManager.CreateAsync("Owner");
  RoleManager.CreateAsync("Editor");

Flows:
  login         → SignInManager → JWT with claims (sub, email, roles)
  invite        → UserManager.CreateAsync → UserManager.AddToRoleAsync(role)
  accept-invite → UserManager + token provider → ConfirmEmail + password
  authorize     → [Authorize(Roles = "PlatformAdmin")] + tenant check in service
```

Standard Identity tables in EF migration: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserTokens`, etc.

### Why ASP.NET Identity (not Supabase Auth)

| Reason | Detail |
|---|---|
| **Backend-first** | Login, logout, invites, and JWT issuance all in C# — frontend only calls API |
| **No auth in browser** | No `@nuxtjs/supabase`, no anon key for login, no Supabase redirect URLs |
| **Single trust boundary** | API validates credentials and tenant membership in one place |
| **Supabase scope** | Postgres + Storage only; auth tables live in app DB via EF migrations |

### Admin login URL — **unique, not per tenant**

| Host | Login? | Purpose |
|---|---|---|
| `app.onlineportfolio.com.br` | **Yes** — `/login` | **One** admin entry for all users (PlatformAdmin, Owner, Editor) |
| `{slug}.onlineportfolio.com.br` | **No** | Public portfolio only |
| Custom domain (`ana-art.com`) | **No** | Public gallery only (v1) |

After login, **tenant context comes from the API** (`ApplicationUser.TenantId` + Identity roles), not from the hostname:

- **Owner / Editor** → `/admin` (scoped to their tenant in API responses)
- **PlatformAdmin** → `/platform/tenants` (and add-user flows)

There is **no** `{slug}.onlineportfolio.com.br/login` in v1.

### Auth architecture

```text
Admin login UI   → app.onlineportfolio.com.br/login (Nuxt page)
Login request    → Nuxt server route → POST /api/v1/auth/login (API)
Credentials      → ASP.NET Identity (password hash in Postgres)
Session token    → JWT issued by API (httpOnly cookie via Nuxt proxy, v1)
API requests     → Nuxt proxy → API (cookie forwarded or Bearer server-side)
Authorization    → JWT role claims + Load User → TenantId; tenant check in API
Public gallery   → No auth (published content via proxy; images via public CDN URLs)
```

### Auth API endpoints (v1)

```text
POST /api/v1/auth/login           email + password → JWT cookie + user summary
POST /api/v1/auth/logout          clear session
POST /api/v1/auth/refresh         rotate access token (if using refresh tokens)
GET  /api/v1/auth/me              current user + tenant
POST /api/v1/auth/accept-invite   token + new password (complete invite)
```

### Key rules

| Rule | Detail |
|---|---|
| Service role key | API server only; never in Nuxt or browser |
| Supabase anon key | **Not used in v1** (no client-side Supabase SDK) |
| JWT | Issued and validated by **API** (`Jwt__Secret` in Render env) |
| Tenant membership | `ApplicationUser.TenantId` + roles in `AspNetUserRoles` |
| Invite flow | `UserManager` + `AddToRoleAsync` → Resend → `/accept-invite` |

### Custom domains and auth

Public galleries on custom domains do not expose admin login in v1.

Admin stays on **`app.onlineportfolio.com.br`** only.

---

## 10. API design

### Style

- REST over HTTPS
- URL versioning: `/api/v1/...`
- OpenAPI / Swagger (Swashbuckle) for documentation and client generation

### Endpoint patterns

**Public (tenant-scoped):**

```text
GET /api/v1/tenants/{slug}/artworks
GET /api/v1/tenants/{slug}/artworks/{id}
GET /api/v1/tenants/{slug}/profile
POST /api/v1/tenants/{slug}/contact
GET /api/v1/tenants/{slug}/portfolio.pdf
```

Only published content. Tenant resolved by slug (Nuxt passes slug from Host resolution).

Contact form submissions are handled by the API and delivered via Resend (see section 11).

Portfolio PDF is generated by the API using QuestPDF (see section 12).

**Admin (authenticated, tenant from JWT user):**

```text
POST   /api/v1/artworks
PUT    /api/v1/artworks/{id}
DELETE /api/v1/artworks/{id}
POST   /api/v1/artworks/{id}/images
GET    /api/v1/artworks          (includes drafts)
GET    /api/v1/portfolio/export.pdf   (published + optional drafts per query flag)
```

**Platform admin:**

```text
POST   /api/v1/platform/tenants
GET    /api/v1/platform/tenants
PATCH  /api/v1/platform/tenants/{id}
```

### Error handling

- Global exception handler → consistent JSON error shape for Nuxt
- Structured logging (Serilog) → stdout for Render logs

### Pagination

List endpoints paginated from v1.

---

## 11. Email (Resend)

### v1 decision

**Resend + `mail@onlineportfolio.com.br` — send only.** No mailbox, no MX for receiving, no Google Workspace at the start. Replies via **Reply-To**.

Full decision: [ADR-017](./ADR-017-resend-transactional-email.md).

The API sends all transactional email; the frontend never stores the Resend API key.

| Config (Render) | Value |
|---|---|
| `Resend__ApiKey` | server only (API token `re_...`) |
| `Resend__FromEmail` | `mail@onlineportfolio.com.br` |
| `Resend__FromName` | Online Portfolio |

Operator inbox (`hello@`, etc.) → [section 2 — Google Workspace later](#operator-mailbox-google-workspace--later).

### Use cases

| Use case | Phase | Detail |
|---|---|---|
| **Contact form** | Phase 1 | Visitor → API → Resend → tenant `ContactEmail`; Reply-To = visitor |
| **User invite** | Phase 1.5 | Resend with accept-invite link |
| **Platform notifications** | Later | Billing, verified domain, etc. |

### What `mail@` is **not**

- **Not** a mailbox — nobody reads `mail@` in v1.
- **Does not** need MX at Registro.br/Vercel to **receive** mail.
- Contact replies go to the artist's email via Reply-To (visitor); invites use operator Reply-To.

### Contact form flow

```text
1. Visitor submits contact form on tenant public site (Nuxt)
2. Nuxt server route or client POST → POST /api/v1/tenants/{slug}/contact
3. API resolves tenant, validates input, applies rate limiting
4. API renders template + sends via Resend to TenantSettings.ContactEmail
5. Optional: set Reply-To to visitor email for direct artist reply
6. API returns success without exposing Resend details
```

Messages are **not stored in the database** by default (email-only). Add a `ContactMessage` table later if audit/history is needed.

### Resend free tier notes

- **3,000 emails/month**, max **100/day** — permanent free tier (sufficient for contact + invites at the start)
- **1 domain** verified on free tier
- Domain `onlineportfolio.com.br` **verified** in prod (DKIM/SPF/DMARC — DEV-011 ✅)
- Templates in `EmailTemplates/` (Razor or HTML) — versioned in repo

### Implementation (backend)

- NuGet: `Resend` (official SDK)
- `IEmailService` abstraction with `ResendEmailService` implementation
- Config: `Resend__ApiKey`, `Resend__FromEmail`, `Resend__FromName`
- Contact endpoint validates: name, email, message, honeypot/rate limit
- Log send failures; do not leak Resend errors to client

### Local development

- Dev API key in password manager; smoke test with `onboarding@resend.dev` ([EXTERNAL_PROVIDERS](./EXTERNAL_PROVIDERS.md) section 8.5)
- Or log payload in `Development` without sending
- Domain `onlineportfolio.com.br` verified in prod — sender `mail@onlineportfolio.com.br` (DEV-011 ✅)

### ADR-017: Transactional email via Resend

| | |
|---|---|
| **Status** | ✅ Accepted |
| **Date** | 2026-06-21 |
| **Context** | SendGrid (Twilio) removed permanent free tier (May 2025). Project needs transactional .NET email with zero cost at MVP. |
| **Decision** | **Resend** — official .NET SDK, templates in repo, 3k emails/month free. |
| **Alternatives** | SendGrid (paid early), Brevo (more free, verbose SDK), Postmark/SES (cost/complexity). |

Full document: [ADR-017-resend-transactional-email.md](./ADR-017-resend-transactional-email.md).

---

## 12. PDF generation (QuestPDF)

### Decision

Use **QuestPDF** (Community license) for all PDF generation in the .NET API. No separate PDF service or paid SaaS — documents are built and streamed from Render at request time.

### License

QuestPDF is **free under the Community license** for organizations with **less than $1M USD gross annual revenue**. Review [QuestPDF licensing](https://www.questpdf.com/license/) before production; upgrade to Professional if revenue exceeds that threshold.

### Use cases

| Use case | Phase | Audience |
|---|---|---|
| **Portfolio catalog PDF** | Phase 2+ | Public — published artworks, artist bio, contact |
| **Admin export** | Phase 2+ | Authenticated owner — full or draft catalog |
| **Exhibition catalog** | Later | Public or admin — grouped by exhibition |
| **Invoices / receipts** | Phase 4 (optional) | With automated billing (Stripe/PSP), if implemented |

### Flow

```text
1. User clicks "Download portfolio PDF" on tenant public site (or admin export)
2. Nuxt → GET /api/v1/tenants/{slug}/portfolio.pdf  (or authenticated admin endpoint)
3. API loads tenant + published artworks (+ images URLs) from EF
4. QuestPDF document compositor builds PDF in memory
5. API returns application/pdf stream (Content-Disposition: attachment)
```

PDF generation runs **server-side only** — never in the browser, never on Vercel edge.

### Document content (v1 catalog)

- Tenant name, logo (from `TenantSettings`), bio
- Grid or list of published artworks: image thumbnail, title, year, medium, dimensions
- Contact email / website footer
- Optional: QR code linking to public gallery URL (later)

After Phase 3 (Storage), embed or link artwork images from Supabase public URLs in the PDF.

### Implementation (backend)

- NuGet: `QuestPDF` (+ `QuestPDF.Previewer` for local dev optional)
- `IPdfService` abstraction with `QuestPdfPortfolioService` implementation
- Document layout in dedicated classes under `backend/OnlinePortfolio.Api/Pdf/` (e.g. `PortfolioDocument.cs`)
- Register Community license at startup: `QuestPDF.Settings.License = LicenseType.Community`
- Stream response; avoid writing PDFs to Render disk (no persistence on free tier)
- Consider response caching (short TTL per tenant) if generation becomes heavy

### Performance notes

- Render free tier: large catalogs with many high-res images may be slow or memory-heavy
- Prefer **thumbnail URLs** in PDF, not originals
- Set reasonable limits (e.g. max artworks per PDF) or paginate for very large portfolios
- Cold start + PDF generation = slow first request; acceptable for on-demand download

### Local development

- QuestPDF runs in the API container / `dotnet run` with no extra config
- Use `QuestPDF.Previewer` or save to temp file locally for layout iteration

---

## 13. Custom domains per tenant

### Supported

Each tenant can have a custom domain for their **public gallery**, served by the same Vercel Nuxt deployment.

```text
ana-art.com      → Vercel → tenant: ana
joao-sculpt.com  → Vercel → tenant: joao
ana.onlineportfolio.com.br → Vercel → tenant: ana (fallback)
```

### Not on custom domains

| Service | Domain |
|---|---|
| API | `api.onlineportfolio.com.br` only |
| Admin / auth | `app.onlineportfolio.com.br` |
| Supabase | Shared project URL |

### DNS (artist onboarding)

Artist adds records pointing to Vercel:

| Record | Value |
|---|---|
| `www` CNAME | `cname.vercel-dns.com` |
| Apex `@` | Vercel A record or ALIAS (registrar-dependent) |

Platform stores domain on tenant; verifies via Vercel; sets `CustomDomainVerifiedAt`.

### www vs apex

Pick one canonical host; redirect the other (e.g. `www.ana-art.com` → `ana-art.com`).

### Scaling domain management

| Stage | Approach |
|---|---|
| 2 tenants | Manual add in Vercel dashboard |
| Many tenants | Vercel Domains API (Platforms) during onboarding |

Custom domain availability may be a paid plan feature for tenants.

---

## 14. Local development (Docker Compose)

### Approach: hybrid

Docker wraps dependencies and services for one-command startup. Hot reload remains acceptable via volume mounts.

| Service | In Compose | Notes |
|---|---|---|
| PostgreSQL 15/16 | Yes | Local stand-in for Supabase Postgres |
| ASP.NET API | Yes | `dotnet watch` with volume mount |
| Nuxt frontend | Yes | `npm run dev` with volume mount |
| Supabase local stack | Optional | `supabase start` for local Storage (Phase 3); auth stays in Postgres/Identity |

### Local vs production database

| Environment | Database |
|---|---|
| Local Compose | Postgres container; same EF migrations |
| Staging (optional) | Supabase cloud dev project |
| Production | Supabase cloud project |

### Seed data

Seed **at least two tenants** in local dev to catch cross-tenant leaks early.

### Start command

```text
docker compose up
```

---

## 15. Deployment & CI/CD

### Environments

| Environment | Frontend | API | Database | Cloud deploy? |
|---|---|---|---|---|
| **Local** | Docker Compose (Nuxt) | Docker Compose (API) | Postgres container | ❌ |
| **PR preview** | Vercel preview (per PR) | Local or mock — **no** staging API | Local / CI | ❌ (ephemeral, not a "deployed dev") |
| **Production** | Vercel (`main`) | Render (Docker) | Supabase | ✅ **only deployed environment** |

Local Docker Compose is for day-to-day development. **Only production** is deployed to Vercel + Render + Supabase on merge to `main`.

See [ADR-015](#adr-015-production-only-deploy).

### ADR-015: Production-only deploy

| | |
|---|---|
| **Status** | ✅ Accepted |
| **Date** | 2025-06-21 |
| **Context** | Monorepo v1, solo/small-team operator, free tiers (Vercel, Render, Supabase). Question: mirror **dev + prod** deployed (Render dev, Vercel staging, Supabase dev)? |
| **Decision** | **Production only deployed** to the cloud. Do not create a parallel staging/homologation stack deployed in v1. |
| **Rejected alternative** | Full deployed dev pair (dev API + dev front + Supabase dev + `dev.*` DNS + duplicate workflows). |

**Three-layer model (v1):**

```text
Local (docker compose)     → daily development, local Postgres, local API + Nuxt
PR preview (Vercel)        → UI review per pull request — ephemeral, does not replace staging
Production (main)          → only persistent deploy: Vercel + Render + Supabase prod
```

**What is included:**

- `deploy-backend.yml` and `deploy-frontend.yml` run **only against production** (push to `main`).
- One Supabase project **`portfolio-prod`** — migrations via CI on prod after merge.
- Render **one** web service (prod API).
- Vercel **one** project (prod front); native PR preview; **no** second "staging" project.

**What is out of v1:**

- Render dev service / `api-dev.*` API
- Vercel project or fixed "homologation" branch
- Supabase project `portfolio-dev` **deployed** (local Postgres covers dev)
- `deploy-*-staging.yml` workflows or prod/dev matrix
- DNS `dev.onlineportfolio.com.br` / `staging.*`

**Consequences:**

| Positive | Trade-off |
|---|---|
| Fewer accounts, secrets, and DNS to maintain | Risky migrations require local + CI testing before merge |
| Less cognitive cost ("which URL?") | No stable homologation URL for third parties |
| Aligns with solo dev + 1–2 initial artists | Re-evaluate when there are 2+ devs or webhook integrations in homolog |

**Mitigations (without deployed staging):**

- Separate CI (`ci-backend`, `ci-frontend`) on every PR.
- Docker Compose local = stack parity.
- Vercel preview per PR for front.
- Migrations tested locally (`dotnet ef database update`) before merge; CI migrate only on `main`.

**When to re-evaluate (create deployed staging):**

- Second full-time dev or client needing a fixed homologation URL.
- Destructive migration broke prod once.
- Stripe/webhooks/domains requiring a persistent non-prod environment.
- Real traffic or data where "test only locally" is no longer enough.

Until then: **local + preview + prod** — see [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

### Decision: GitHub Actions as pipeline orchestrator

**Preference:** GitHub Actions controls CI/CD. Vercel and Render are **deploy targets**, not the source of truth for the pipeline.

Neither Vercel nor Render **requires** you to rely solely on their dashboard deploy buttons — both support GitHub integration **and** deploy triggered from GitHub Actions. You can (and should) use both together.

| Concern | Owner | Tool |
|---|---|---|
| Tests — **backend** | GitHub Actions | `ci-backend.yml` |
| Tests — **frontend** | GitHub Actions | `ci-frontend.yml` |
| EF migrations (Supabase session pooler) | GitHub Actions | `deploy-backend.yml` |
| Deploy **frontend** (prod) | GitHub Actions | `deploy-frontend.yml` → Vercel CLI |
| Deploy **API** (prod) | Render | Wait for CI in `deploy-backend.yml` |
| Preview deploys (PRs) | Vercel | Native GitHub integration (PR comments) |
| Env vars / secrets | Vercel + Render dashboards + GitHub Secrets | Per platform |

### Decision: **separate CI workflows** (backend + frontend)

**Project preference:** **two CI workflows** in the same monorepo — **not** a single `ci.yml` that runs everything together.

| Approach | When to use | Decision |
|---|---|---|
| **One `ci.yml` with backend + frontend jobs** | Small repo, every PR touches both sides | ❌ No — frontend-only PRs wait for `dotnet restore` unnecessarily |
| **Two CI workflows** (`ci-backend.yml`, `ci-frontend.yml`) | Monorepo with distinct stacks | ✅ **Chosen** |
| **Two deploy workflows** (`deploy-backend.yml`, `deploy-frontend.yml`) | Symmetric prod; Actions orchestrates both | ✅ **Chosen** |
| **Two Git repos** | Fully independent teams/release cycles | ❌ No — see section 20 |

**Why separate (in the monorepo):**

- **Path filters** — PR only in `frontend/` does not trigger .NET build (and vice versa).
- **Clear status checks** on PR: "Backend CI" and "Frontend CI".
- **Aligns with deploy** — Render (API) and Vercel (Nuxt) are already distinct pipelines.
- **Same PR** can trigger both when `backend/` **and** `frontend/` change (e.g. Epic 1.5 auth).

This **does not** contradict the monorepo: still **one repo**, **one PR**; only the **workflow files** are separate.

### Git flow — beyond tests, review paired components

On every PR (human or agent), **after tests** and **before merge**, check whether only one side of the stack changed when the contract requires both:

| If you changed… | Check in **backend** | Check in **frontend** |
|---|---|---|
| Endpoint / DTO / API contract | Controller, service, validation, `[Authorize]`, OpenAPI | Proxy route `server/api/**`, composable, TS types, page/component |
| Schema / EF entity | Migration + seed if needed | API types/consumption; forms if field is exposed |
| Auth / JWT / roles | Identity, policies, tenant middleware | Proxy (cookies/headers), host middleware, login flow on `app.*` |
| Environment variable | `appsettings`, Render env | `runtimeConfig`, Vercel env, `frontend/.env.example` |
| Multi-tenant rule | EF filter + membership check | Never send trusted `tenantId` from client; admin vs public UI |
| Admin feature | Protected API routes | Pages on `app.*` host |
| Public site feature | Public API (published content only) | Pages on `{slug}.*`, tenant middleware |
| Transactional email | Resend service + template | Only if UI exists (e.g. preview) |
| Storage / upload (Phase 3) | Multipart API + service role | Upload proxy; `<img>` CDN URL |

**Quick checklist before merge:**

1. Relevant CI green (`Backend CI` / `Frontend CI` per PR paths).
2. Sticky **Backend coverage** / **Frontend coverage** comments on PR (if that stack's CI ran).
3. **Front↔back pairing** — table above; if API changed, front **or** contract docs updated.
3. `frontend/.env.example` if new Nuxt env vars; `appsettings.json` (+ `Development`) if new API env vars.
4. `docs/DATABASE.md` if schema changed materially.
5. Commits [Conventional Commits](./CONVENTIONAL_COMMITS.md); `frontend` / `backend` scope consistent with paths.

Details for agents: [docs/AGENT_GUIDE.md](./AGENT_GUIDE.md).

### Do Vercel and Render require repo connection?

| Platform | Repo connection | Auto-deploy on push | Alternative |
|---|---|---|---|
| **Vercel** | Recommended (not strictly required) | Default ON when connected | `vercel deploy` from Actions with `VERCEL_TOKEN` |
| **Render** | Recommended for Docker build | Default ON when connected | Deploy hook URL called from Actions |

**Practical setup:** connect both to the GitHub repo (for builds, env vars, PR previews). Use GitHub Actions to **gate** what runs before deploy.

### Recommended pipeline (hybrid)

```text
Pull request:
  ci-backend.yml   → dotnet test (paths: backend/**, …)
  ci-frontend.yml  → npm lint/test (paths: frontend/**, …)
  Manual review    → paired front↔back components (table above)
  Vercel           → preview deployment (native PR integration)

Push to main:
  deploy-backend.yml  → dotnet test → EF migrate (session pooler :5432) → status check
  Render              → deploy API (Wait for CI — after deploy-backend green)
  deploy-frontend.yml → npm lint/test → vercel pull → npm run build → vercel deploy --prebuilt --prod
```

**Symmetric prod:** backend and frontend each have their own deploy workflow. **PR preview** stays on Vercel (native integration).

**Order:** API deploy **must** run after migrations. Frontend **does not** depend on migrate, but **must** pass PR CIs before merge (branch protection).

### Render: Wait for CI

In Render service settings → enable **Wait for CI** (or equivalent). Render waits for GitHub commit status checks from Actions before deploying. Migrations must complete inside the Actions workflow **before** checks pass.

### Vercel: prod via Actions (native preview on PR)

| Mode | Config | Decision |
|---|---|---|
| **Preview (PR)** | Vercel GitHub App — auto preview per PR | ✅ Keep |
| **Production (`main`)** | `deploy-frontend.yml` → `vercel deploy --prod` | ✅ **Chosen** |
| **Production auto-deploy in Vercel dashboard** | Deploy on every push to `main` | ❌ **Disable** — avoids prod deploy without passing workflow |

**Recommendation (updated):** **separate CI + separate deploy** — mirrors Render/backend:

- PR: `ci-backend.yml` + `ci-frontend.yml` + Vercel preview
- `main`: `deploy-backend.yml` (migrate + gate Render) + `deploy-frontend.yml` (Vercel CLI)
- Branch protection: require Backend CI + Frontend CI before merge

### Example workflow structure

```text
.github/workflows/
  ci-backend.yml       # pull_request + push: dotnet test, build (paths backend/**)
  ci-frontend.yml      # pull_request + push: npm lint, test (paths frontend/**)
  deploy-backend.yml   # push main → production: test → EF migrate → gate Render
  deploy-frontend.yml  # push main → production: lint/test → vercel pull → npm run build → vercel deploy --prebuilt --prod
```

**Path filters (example):**

| Workflow | Triggers when these change |
|---|---|
| `ci-backend.yml` | `backend/**`, `docs/DATABASE.md`, `.github/workflows/ci-backend.yml`, `deploy-backend.yml` |
| `ci-frontend.yml` | `frontend/**`, `.github/workflows/ci-frontend.yml`, `deploy-frontend.yml` |
| `deploy-backend.yml` | `push` → `main`; paths `backend/**`, migrations |
| `deploy-frontend.yml` | `push` → `main`; paths `frontend/**` |

PRs **only in `docs/`** may include both workflows (expanded paths) or a light `ci-docs.yml` — see [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Branch protection on `main`:** require status checks **Backend CI** and **Frontend CI** (checks skipped by path filter count as OK on GitHub).

### CI vs Deploy — responsibilities

Each **deploy** workflow runs **only its own stack's tests** (embedded in the job). It **does not** invoke the separate CI workflow or the opposite stack:

| Workflow | What runs before deploy |
|---|---|
| `deploy-backend.yml` | `dotnet test` + `ef database update` |
| `deploy-frontend.yml` | `npm run lint` + `npm run test:coverage` + Vercel CLI |
| `ci-backend.yml` | `dotnet test` + coverage (PR comment) — **does not deploy** |
| `ci-frontend.yml` | lint + test + coverage (PR comment) — **does not deploy** |

**Push to `main` (path filters):**

| Change | Triggers |
|---|---|
| Only `backend/**` | Backend CI + Backend Deploy (Frontend CI **not**) |
| Only `frontend/**` | Frontend CI + Frontend Deploy (Backend CI **not**) |
| Only `docs/**` | Nothing (unless workflow path includes the doc) |
| Both | All 4 workflows (2 CI + 2 deploy) |

**Intentional duplication:** on the same push, CI and deploy for the same stack run tests separately — CI provides a named status check for branch protection; deploy is self-sufficient gate before migrate/Vercel (does not depend on another workflow finishing).

**Optional improvements (future):** reusable workflows `test-backend.yml` / `test-frontend.yml`; `workflow_run` for deploy after CI green; light `ci-docs.yml` for docs-only PRs. None required for the current design.

**GitHub Secrets (Actions):**

| Secret | Purpose |
|---|---|
| `SUPABASE_MIGRATION_CONNECTION_STRING` | Session pooler URI (port 5432, IPv4) |
| `VERCEL_TOKEN` | Prod deploy via `deploy-frontend.yml` |
| `VERCEL_ORG_ID` | Vercel CLI — org/team ID |
| `VERCEL_PROJECT_ID` | Vercel CLI — project ID (`frontend`) |
| `RENDER_DEPLOY_HOOK_URL` | Optional — if Render auto-deploy disabled |

**Do not** run production migrations from Render startup — migrations live in Actions only.

### What each platform still manages in its dashboard

Even with GitHub Actions preference, configure in provider UIs:

| Platform | Dashboard config |
|---|---|
| **Vercel** | Env vars, custom domains, build settings, PR previews |
| **Render** | Env vars, Docker settings, custom domain, health check |
| **GitHub** | Secrets, branch protection, Actions workflows |
| **Supabase** | Connection strings (referenced as secrets) |

### Cost (target: free / minimal)

| Service | Tier |
|---|---|
| Vercel | Hobby → Pro when domain limits require |
| Render | Free (accept cold starts) |
| Supabase | Free tier |
| Domain registrar | R$ 40/year (`onlineportfolio.com.br` at Registro.br) |
| Operator email (future) | Google Workspace ~US$ 6/user/month (not v1) |
| Email (Resend) | Free tier (3,000 emails/month) |
| PDF (QuestPDF) | Free (Community license; revenue threshold applies) |

---

## 16. Tenant provisioning

### Platform admin flow

```text
1. Create Tenant (name, slug, plan)
2. Create TenantSettings defaults
3. PlatformAdmin invites tenant admin(s) via API (email + role) → Resend invite link
4. Invitee sets password via `POST /auth/accept-invite` → `users` row active
5. Tenant public site live at {slug}.onlineportfolio.com.br
6. PlatformAdmin can add more users to the same tenant later (Owner/Editor)
6. (Optional) Artist configures custom domain → DNS instructions → Vercel verify
```

### Self-serve signup

Deferred. Initial customers provisioned manually or via platform admin API.

---

## 17. Data model (v1)

**Detailed schema (login MVP + future tables):** [DATABASE.md](./DATABASE.md)

### Platform-scoped

```text
Plan
  Id, Name, MaxStorageMb, MaxArtworks, CustomDomainAllowed, ...

Tenant
  Id, Slug, DisplayName, CustomDomain, CustomDomainVerifiedAt,
  PlanId, IsActive, CreatedAt, ...

TenantSettings
  TenantId, LogoUrl, Theme, ContactEmail, Bio, SocialLinks, ...
```

### Tenant-scoped

```text
User (ApplicationUser : IdentityUser<Guid>)
  Id, Email, PasswordHash, TenantId, IsActive, InvitedByUserId, LastLoginAt, CreatedAt
  + Identity columns (NormalizedEmail, SecurityStamp, EmailConfirmed, …)
  Role(s) → AspNetUserRoles → AspNetRoles (PlatformAdmin | Owner | Editor)

Artwork
  Id, TenantId, Title, Slug, Description, IsPublished, PublishedAt,
  SortOrder, CreatedAt, UpdatedAt

ArtworkImage
  Id, TenantId, ArtworkId, StoragePath, PublicUrl, AltText,
  SortOrder, Width, Height

Category (optional v1)
  Id, TenantId, Name, Slug, SortOrder
```

### Indexes

- `(TenantId, Slug)` unique on `Artwork`, `Category`
- `Tenant.CustomDomain` unique where not null
- `Tenant.Slug` unique
- `User.Email` unique globally

---

## 18. Security requirements

### Mandatory

- [ ] `TenantId` on all tenant-scoped tables
- [ ] EF global query filters for tenant isolation
- [ ] API verifies user tenant membership on every protected request
- [ ] No IDOR: resource access checks `resource.TenantId == user.TenantId`
- [ ] Service role key only on API server
- [ ] CORS: restrict to known origins (or use Nuxt server proxy)
- [ ] Public API returns only `IsPublished == true` content
- [ ] Storage paths include `tenantId`; RLS enforces write access
- [ ] Integration tests: user of tenant A cannot access tenant B resources

### Recommended

- [ ] Rate limiting on public and contact endpoints
- [ ] Audit log for platform admin actions
- [ ] Input validation on all write endpoints
- [ ] File type and size validation on uploads

---

## 19. Phased rollout

### Phase 1 — Public portfolio (no auth, no uploads)

- Tenant model and routing (subdomain)
- Public gallery CRUD via platform admin or seed data
- SSG gallery pages
- Docker Compose local setup
- API + EF + migrations
- Contact form → Resend → tenant `ContactEmail`

### Phase 1.5 — Admin login + add user (MVP)

- full ASP.NET Identity + JWT on API
- Login UI at **`app.onlineportfolio.com.br/login`** (unique URL)
- Nuxt BFF: all admin calls proxied to API
- **Login / logout** (Owner, Editor, PlatformAdmin)
- **Add user** — PlatformAdmin invites per tenant (Resend + accept-invite)
- Admin shell routes (`/admin`, `/platform/tenants`, `/platform/tenants/{id}/users`)

### Phase 2 — Admin features (post-login)

- Admin CRUD for artworks
- Role-based access (Owner / Editor)
- Portfolio PDF export (QuestPDF; text-only or placeholders until Phase 3 images)

### Phase 3 — Image uploads

- Supabase Storage integration (API writes with service role key)
- Upload via API multipart (Nuxt proxy)
- `ArtworkImage` metadata in EF
- Public gallery renders images via Storage CDN URLs

### Phase 4 — Custom domains (+ optional billing)

- Custom domain onboarding per tenant
- Vercel Domains API automation
- **Billing / subscriptions (optional — skip in v1):** Stripe preferred if expanding **outside Brazil**; Asaas/Iugu if Brazil-only — see [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md)
- Plan limits (DEV-402) — optional; can be manual
- Self-serve signup (optional)
- Google Workspace for operator inbox (optional)

---

## 20. Repository structure

### Decision: **monorepo** (single Git repository)

| | Monorepo (chosen) | Separate repos |
|---|---|---|
| **Local dev** | One `docker compose up` starts Postgres + API + Nuxt | Two clones, two networks, duplicate env |
| **CI/CD** | Separate workflows: `ci-backend`, `ci-frontend`, `deploy-backend`, `deploy-frontend` | Single `ci.yml`; or separate Git repos |
| **Docs / BACKLOG** | Single source of truth (`docs/`, `DEV-xxx`) | Docs diverge between repos |
| **Epic 1.5 (auth)** | Front + back in same PR when API and proxy change together | Coordinated PRs across repos |
| **Deploy** | Same repo: Vercel `root=frontend`, Render `root=backend` | Works, but more operational overhead |
| **Team scale** | Ideal for 1 dev / small team | Makes sense with large teams and independent release cycles |

**Conclusion:** use **one repository** `online-portfolio/` with `frontend/` and `backend/` folders. Vercel and Render point to **subfolders of the same repo** — not separate Git repos.

Separate repos would only be worth reconsidering if frontend and backend later have **fully independent release cycles** and **distinct teams** — not the case in v1.

```text
online-portfolio/          ← one Git repo
├── frontend/              → Vercel (Root Directory: frontend)
├── backend/               → Render (Dockerfile: backend/OnlinePortfolio.Api/Dockerfile)
├── docs/
├── docker-compose.yml
└── .github/workflows/
```

### Layout

```text
online-portfolio/
├── docker-compose.yml
├── docker-compose.override.yml      # local overrides (gitignored optional)
├── docs/
│   ├── ADR-017-resend-transactional-email.md
│   ├── ARCHITECTURE.md              # system design and decisions (ADR-015, ADR-016, ADR-017, …)
│   ├── FRONTEND_COMPONENTS.md       # Nuxt surfaces, tenant UI, composables
│   ├── EXTERNAL_PROVIDERS.md        # third-party setup (Supabase, Vercel, etc.)
│   ├── BACKLOG.md                   # development tasks (Linear-ready)
│   └── DATABASE.md                  # PostgreSQL schema (multi-tenant)
├── frontend/                        # Nuxt 3 → Vercel
│   ├── pages/                       # shared routes; content varies by surface/tenant
│   ├── components/
│   │   ├── ui/                      # global primitives (Ui*)
│   │   ├── app/                     # admin + login (standardized)
│   │   ├── platform/                # marketing apex
│   │   └── public/
│   │       ├── shared/              # tenant defaults (Public*)
│   │       └── tenants/{slug}/      # per-client overrides (Ana*, Joao*, …)
│   ├── layouts/                     # app | platform | tenant | default
│   ├── assets/css/themes/           # per-tenant palettes (.theme-{slug})
│   ├── composables/                 # useRequestSurface, useTenantComponent, …
│   ├── middleware/                  # resolve-host.global.ts
│   └── server/api/                  # proxy to backend API
├── backend/                         # ASP.NET Core → Render
│   ├── OnlinePortfolio.Api.slnx
│   ├── OnlinePortfolio.Api/
│   │   ├── Dockerfile
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Pdf/                     # QuestPDF document layouts
│   │   ├── Data/                    # EF DbContext, migrations
│   │   └── Middleware/              # tenant context, JWT
│   └── OnlinePortfolio.Api.Tests/   # xUnit (unit + integration)
└── .github/workflows/               # ci-backend, ci-frontend, deploy-backend, deploy-frontend
└── README.md
```

---

## 21. Environment variables

### Frontend (Vercel)

```text
NUXT_API_INTERNAL_BASE=http://api:8080          # server-side proxy target (Compose)
NUXT_PUBLIC_API_BASE=/api                       # browser hits Nuxt proxy, not Render directly
NUXT_PUBLIC_PLATFORM_HOST=onlineportfolio.com.br
NUXT_PUBLIC_APP_HOST=app.onlineportfolio.com.br
NUXT_PUBLIC_DEV_SURFACE=dev              # dev | app | platform | tenant (localhost only)
NUXT_PUBLIC_DEV_TENANT_SLUG=ana          # when DEV_SURFACE=tenant
```

See [docs/ARCHITECTURE.md](./ARCHITECTURE.md) and [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md).

No Supabase keys in the frontend in v1.

### Backend (Render)

```text
ConnectionStrings__Default=postgresql://...:6543/...   # pooler
ConnectionStrings__Migration=postgresql://postgres.[ref]:...@aws-*-*.pooler.supabase.com:5432/postgres  # session (CI)
Jwt__Secret=...                                        # API-issued JWT signing key
Jwt__Issuer=OnlinePortfolio.Api
Jwt__Audience=OnlinePortfolio.Admin
Supabase__Url=https://xxx.supabase.co                  # Storage only (Phase 3+)
Supabase__ServiceRoleKey=...                           # server only
ASPNETCORE_ENVIRONMENT=Production
Cors__AllowedOrigins=                                  # optional if all traffic via Nuxt proxy
Resend__ApiKey=re_....                               # server only; invites + contact
Resend__FromEmail=mail@onlineportfolio.com.br
Resend__FromName=Online Portfolio
```

### Local (docker-compose / .env)

```text
# Same keys as above with local/dev values
POSTGRES_USER=portfolio
POSTGRES_PASSWORD=...
POSTGRES_DB=portfolio_dev
```

Use `frontend/.env.example` for Nuxt; backend config via `appsettings` / Render env — never commit secrets.

---

## 22. Open decisions / future work

| Topic | Options | Notes |
|---|---|---|
| Repo layout | Monorepo vs split | **Decided** — monorepo; Vercel + Render same repo, different roots |
| Subdomain vs path fallback | `ana.onlineportfolio.com.br` vs `onlineportfolio.com.br/ana` | Subdomain recommended |
| Self-serve signup | Manual vs automated | Manual for first customers |
| Contact message history | Email only vs store in DB | Email only for v1 (Resend) |
| Operator inbox | Google Workspace | **Decided** — out of v1; v1 = Resend `mail@` + Reply-To only |
| Global `.com` domain | `onlineportfolio.com` taken | Revisit alternative `.com` later if expanding internationally |
| CI/CD approach | GitHub Actions orchestrator | **Decided** — see section 15 |
| Deployed environments | Prod only vs prod + staging | **Decided** — [ADR-015](#adr-015-production-only-deploy); deployed staging **out of v1** |
| Image processing | On upload in API vs external worker | Phase 3 |
| Thumbnail generation | API (ImageSharp) vs Supabase transform | TBD |
| Postgres RLS | Enable as defense in depth | Optional while API-only access |
| Render cold starts | Accept vs upgrade to paid / Fly.io | Revisit after launch |
| Multi-user per tenant | Owner invites Editors | v1: **PlatformAdmin** adds users; v2: Owner self-service |
| Custom domain admin | `ana-art.com/admin` | Defer; use platform app host |
| Primary keys (PK/FK) | `uuid` vs `serial` / hybrid | **Decided** — `uuid`/`Guid` domain PKs; slug in public URLs — [docs/DATABASE.md](./DATABASE.md) |
| Public UI per tenant | Monolith vs deploy per artist vs DB only | **Decided** — same Nuxt app; `public/tenants/{slug}/` folders + CSS themes — [ADR-016 section 5](#adr-016-three-surfaces-and-per-tenant-ui) |

---

## 23. Testing

### Backend stack (industry standard)

Project **`OnlinePortfolio.Api.Tests`** in solution `OnlinePortfolio.Api.slnx`, with `Unit/` and `Integration/` folders.

| Tool | NuGet package | Use |
|---|---|---|
| **xUnit** | `xunit` + `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` | Test framework |
| **Moq** | `Moq` | Interface mocks (`IEmailService`, `ITenantProvider`, etc.) |
| **FluentAssertions** | `FluentAssertions` | Readable assertions (`result.Should().BeOk()`) |
| **Coverlet** | `coverlet.collector` | Code coverage (local + CI) |
| **CLI** | — | `dotnet test` — official runner |

**Integration** (IT-001+, in addition to the stack above):

| Tool | Package | Use |
|---|---|---|
| **WebApplicationFactory** | `Microsoft.AspNetCore.Mvc.Testing` | End-to-end HTTP tests against in-process API |
| **Testcontainers** | `Testcontainers.PostgreSql` (or Postgres service container on GitHub Actions) | Real Postgres isolated per fixture |

### Local commands

```bash
cd backend

# All solution tests
dotnet test OnlinePortfolio.Api.slnx

# Test project only
dotnet test OnlinePortfolio.Api.Tests/OnlinePortfolio.Api.Tests.csproj

# With coverage (Coverlet → file in TestResults/ — gitignored)
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults

# Visual HTML report (Windows — folder: repo root)
powershell -ExecutionPolicy Bypass -File scripts/coverage-backend.ps1
```

Report output: `TestResults/CoverageReport/index.html`. The `TestResults/` folder **never** goes into Git (`.gitignore`).

### CI

| Workflow | Command | When |
|---|---|---|
| `ci-backend.yml` | `dotnet test` (Release) + Coverlet | PR + push to `main`; **Backend coverage** comment on PR |
| `ci-frontend.yml` | `npm run lint` + `npm run test:coverage` | PR + push to `main`; **Frontend coverage** comment on PR |
| `deploy-backend.yml` | `dotnet test` before migrate/deploy | Push to `main` (DEV-007) |

Coverage in CI is **optional in v1**; when enabled, use the same `--collect:"XPlat Code Coverage"` in the workflow.

### Frontend (reference)

Component/composable tests: **Vitest** (UT-009+). Separate stack — see [docs/BACKLOG.md](./BACKLOG.md).

### Related tasks

- Project scaffold: DEV-005b ([docs/BACKLOG.md](./BACKLOG.md))
- Integration infra (WebApplicationFactory, DB, CI): [docs/BACKLOG.md](./BACKLOG.md)
- Test cases: [docs/BACKLOG.md](./BACKLOG.md) · [docs/BACKLOG.md](./BACKLOG.md)

---

## Quick reference

```text
Public site:    {slug}.onlineportfolio.com.br or custom domain  →  Vercel (per tenant)
Platform:       onlineportfolio.com.br                            →  Vercel (marketing)
Admin:          app.onlineportfolio.com.br (standardized)        →  Vercel
API:            api.onlineportfolio.com.br (via Nuxt proxy)       →  Render
Database:       Supabase PostgreSQL                               →  EF Core only
Files:          Supabase Storage                                  →  tenants/{tenantId}/...
Auth:           full ASP.NET Identity + JWT on API           →  Supabase = DB + Storage
Email (app):    Resend + mail@ (send only; Reply-To)             →  contact + invites
Email (ops):    Google Workspace (later)                         →  mailbox
PDF:            QuestPDF                                          →  catalog via API
Domain:         onlineportfolio.com.br (Registro.br)
Isolation:      TenantId + EF filters + Storage paths + API
IDs:            uuid PK/FK (Guid); slug for public tenant URLs — see `docs/DATABASE.md` (Primary keys section)
CI/CD:          GitHub Actions → dotnet test + migrations; deploy **prod only** (ADR-015)
Frontend UI:    3 surfaces (app/platform/tenant); per-tenant public components + CSS themes (ADR-016)
Backend tests:  xUnit · Moq · FluentAssertions · Coverlet · dotnet test
Environments:   local + PR preview + production (no staging deploy v1)
Local dev:      docker compose up
```
