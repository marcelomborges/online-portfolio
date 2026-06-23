# Architecture Decision Record — Artist Portfolio Platform

Multi-tenant SaaS platform for artist portfolios. One shared application stack serves many artists (tenants), each with an isolated public gallery and admin access.

**Status:** Approved for implementation  
**Platform domain:** `onlineportfolio.com.br` (`onlineportfolio.com` unavailable — registered via Registro.br)  
**Last updated:** 2026-06-21 (ADR-017 Resend email; ADR-016 frontend surfaces)

---

## Table of contents

1. [Overview](#1-overview)
2. [Platform domain & operator email](#2-platform-domain--operator-email)
   - [2.1 Mapa de domínios e superfícies do produto](#21-mapa-de-domínios-e-superfícies-do-produto)
3. [Stack summary](#3-stack-summary)
4. [Multi-tenant model](#4-multi-tenant-model)
5. [Frontend (Nuxt + Vercel)](#5-frontend-nuxt--vercel)
    - [ADR-016: Três superfícies e UI por tenant](#adr-016-três-superfícies-e-ui-por-tenant)
6. [Backend (ASP.NET Core + Render)](#6-backend-aspnet-core--render)
7. [Database (Supabase PostgreSQL + EF Core)](#7-database-supabase-postgresql--ef-core)
8. [Storage (Supabase Storage)](#8-storage-supabase-storage)
9. [Authentication & authorization](#9-authentication--authorization)
10. [API design](#10-api-design)
11. [Email (Resend)](#11-email-resend)
    - [ADR-017: Email transacional via Resend](#adr-017-email-transacional-via-resend)
12. [PDF generation (QuestPDF)](#12-pdf-generation-questpdf)
13. [Custom domains per tenant](#13-custom-domains-per-tenant)
14. [Local development (Docker Compose)](#14-local-development-docker-compose)
15. [Deployment & CI/CD](#15-deployment--cicd)
    - [ADR-015: Deploy somente em production](#adr-015-deploy-somente-em-production)
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

- A **public site** per domain/subdomain: landing + posts/galeria (ex.: `ana.onlineportfolio.com.br`)
- Optional **visual identity per tenant** on the public site (themes, layouts, componentes por slug) — ver [ADR-016](#adr-016-três-superfícies-e-ui-por-tenant)
- An **admin area** shared and standardized on `app.onlineportfolio.com.br`
- Optional custom domain for the public site (e.g. `ana-art.com` → same content as `ana.`)
- Platform subdomain fallback (e.g. `ana.onlineportfolio.com.br`)

### Architectural principles

| Principle | Decision |
|---|---|
| One app, many tenants | Shared Nuxt app, shared API, shared Supabase project |
| Data isolation | `TenantId` on all tenant-scoped data; enforced in API and Storage |
| API as gatekeeper | All Postgres writes and admin auth go through .NET API + EF Core |
| BFF no frontend | Browser → Nuxt proxy → API; exceção: `<img>` com URL pública do Storage |
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
│  • SSG/ISR for public tenant sites (landing + posts por tenant)            │
│  • Host `app.*` → admin padronizado; Host `{slug}.*` → site público       │
└───────────────────────────────────┬─────────────────────────────────────┘
                                    │
              ┌─────────────────────┴─────────────────────┐
              │                                           │
              ▼                                           ▼
┌──────────────────────────────┐            ┌──────────────────────────────┐
│  app.onlineportfolio.com.br        │            │  api.onlineportfolio.com.br        │
│  Admin UI (Nuxt)             │            │  RENDER — ASP.NET Core API   │
│  login único (centralizado)  │            │  Auth + Identity + JWT       │
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
| **Status** | ✅ **Registrado** — titular ativo no Registro.br; DNS no deploy ([docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md)) |

### Production hostnames

| Host | Purpose |
|---|---|
| `onlineportfolio.com.br` | Landing da **plataforma** (marketing SaaS) |
| `www.onlineportfolio.com.br` | Redirect ou alias para o apex |
| `{slug}.onlineportfolio.com.br` | Site **público** do tenant (landing + posts/galeria) — ex.: `ana.`, `mark.` |
| `app.onlineportfolio.com.br` | **Admin padronizado** — login único para todos os usuários |
| `api.onlineportfolio.com.br` | API .NET (Render) — não exposta diretamente ao browser |
| `{domínio-customizado}` | Site público do tenant (Fase 4) — ex.: `ana-art.com` |

Registrar o domínio **agora** protege cada `@onlineportfolio.com.br` para uso futuro — Google Workspace não é obrigatório no v1.

### 2.1 Mapa de domínios e superfícies do produto

Referência do que discutimos — **uma app Nuxt**, roteamento pelo `Host`:

```text
┌─────────────────────────────────────────────────────────────────────────┐
│  onlineportfolio.com.br          →  landing da plataforma (produto)     │
├─────────────────────────────────────────────────────────────────────────┤
│  ana.onlineportfolio.com.br      →  site público da Ana (landing+posts) │
│  mark.onlineportfolio.com.br     →  site público do Mark                │
│  {slug}.onlineportfolio.com.br   →  um site por tenant                  │
│  ana-art.com (futuro)            →  mesmo conteúdo público da Ana       │
├─────────────────────────────────────────────────────────────────────────┤
│  app.onlineportfolio.com.br      →  admin PADRONIZADO (todos os tenants)│
│    /login                        →  login único (Owner, Editor, Ops)  │
│    /admin                        →  painel do tenant (após login)     │
│    /platform/tenants             →  gestão de tenants (PlatformAdmin)   │
├─────────────────────────────────────────────────────────────────────────┤
│  api.onlineportfolio.com.br      →  backend C# (proxy via Nuxt no front)│
└─────────────────────────────────────────────────────────────────────────┘
```

| Superfície | Por tenant? | Domínio | Observação |
|---|---|---|---|
| Landing + posts/galeria | **Sim** | `{slug}.onlineportfolio.com.br` ou domínio custom | Layout/tema/componentes por tenant — [ADR-016](#adr-016-três-superfícies-e-ui-por-tenant) |
| Admin (login, CRUD, settings) | **Não** — UI única | `app.onlineportfolio.com.br` | Tenant vem da API após login, não do hostname |
| Login | **Não** | `app.../login` only | Não existe `ana./login` no v1 |
| API | **Não** | `api...` (via proxy) | Browser não chama Render direto |

**Fluxo resumido:**

```text
Visitante   →  ana.onlineportfolio.com.br     →  vê portfolio da Ana
Artista     →  app.onlineportfolio.com.br     →  edita conteúdo da Ana (login)
Operador    →  app.../platform/tenants        →  cria tenants, convida usuários
```

**Frontend → backend (BFF):** todo dado e toda ação admin passam por Nuxt → API .NET. **Exceção:** imagens públicas renderizadas com URL CDN do Storage em `<img>`.

**Auth:** ASP.NET Identity **completo** + JWT na API — sem Supabase Auth no browser. Ver seção 9.

### Email v1 — Resend `noreply@` (só envio, sem caixa postal)

**Decisão:** envio transacional via **Resend** ([ADR-017](./ADR-017-resend-transactional-email.md)). **Não** configurar caixa postal, MX para receber, Google Workspace nem email no Registro.br.

| Item | v1 |
|---|---|
| **Remetente** | `noreply@onlineportfolio.com.br` |
| **Provedor** | Resend (free tier — 3.000 emails/mês, máx. 100/dia) |
| **Quem envia** | API .NET (server-side) |
| **Caixa postal** | **Nenhuma** — `noreply@` não recebe respostas |
| **Registro.br** | Só registro do domínio; sem servidor de email |

**O que o Resend envia no v1:**

| Email | Fase |
|---|---|
| Formulário de contato → email do artista (`ContactEmail`) | 1 |
| Convite de usuário (link accept-invite) | 1.5 |
| Notificações da plataforma | depois |

**Importante:**

- `noreply@` é **identidade de envio**, não inbox. Respostas do visitante no contato usam **Reply-To** (email do visitante ou do tenant), não uma caixa `@onlineportfolio.com.br`.
- Autenticação do domínio (SPF/DKIM) na **Vercel DNS** melhora entrega — ver [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).
- Quando precisar **ler** email no domínio (`hello@`, `marcelo@`), aí sim Google Workspace ou similar — ver abaixo.

### Caixa postal operador (Google Workspace — depois)

| Item | Decisão |
|---|---|
| **Provedor** | Google Workspace (pago) |
| **Status** | **Fora do v1** — inbox humano no futuro |
| **Endereços planejados** | ex.: `hello@onlineportfolio.com.br`, `marcelo@onlineportfolio.com.br` |
| **Quando** | Após lançamento ou quando precisar ler/responder no domínio |
| **Custo estimado** | ~US$ 6–7/usuário/mês (Business Starter) |

**Dois sistemas de email — não confundir:**

| Sistema | Função | Quando |
|---|---|---|
| **Resend** | App **envia** (`noreply@`, contato, convites) | v1 |
| **Google Workspace** | Você **lê/responde** no domínio | futuro |

Resend (SPF/DKIM) e Google (MX) podem coexistir no mesmo domínio quando o Workspace for configurado. No v1, **só Resend**.

### Domain purchase (Registro.br)

Buy at [registro.br](https://registro.br) first; **DNS can wait** until deploy.

| Step | Action |
|---|---|
| 1 | Search `onlineportfolio.com.br` → Register |
| 2 | Pay R$ 40/year (Pix, boleto, or card) |
| 3 | Confirm status **Ativo** in Registro.br panel |
| 4 | Keep default DNS for now — configure when Vercel/Render are ready |

Detailed steps: [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md)

### Registro.br + Vercel nameservers (does not transfer ownership)

Using Vercel nameservers **does not change** who owns the domain. Registro.br remains registrar and renewal (R$ 40/year); Vercel only manages DNS records.

```text
Registro.br   →  titular, renovação, nameservers apontando para Vercel
Vercel DNS    →  apex, app, wildcard tenants, registros para SSL
Render        →  api.onlineportfolio.com.br (CNAME no DNS da Vercel)
```

| Question | Answer |
|---|---|
| Perde o Registro.br? | **No** — ownership and renewal stay there |
| Muda o domínio? | **No** — still `onlineportfolio.com.br` |
| Configurar na compra? | **Optional** — can defer DNS until deploy |

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
| **Domain** | `onlineportfolio.com.br` | Registro.br | See seção 2 |
| Frontend | Nuxt 3.21 · Vue 3.5 | Vercel (Hobby → Pro as needed) | Single app, multi-tenant routing |
| Backend | ASP.NET Core Web API (.NET 10 LTS) | Render (free tier, Docker) | One API for all tenants |
| Database | PostgreSQL | Supabase | Access only via EF Core |
| File storage | Supabase Storage | Supabase | Image uploads; not on Render disk |
| Auth | ASP.NET Identity **completo** + JWT | API (.NET) | Roles, stores e token providers padrão; Supabase = DB + Storage |
| Email (app) | Resend | Resend (free) | `noreply@` — só envio transacional; sem caixa postal |
| Email (operador) | Google Workspace | Google (depois) | Inbox humano — **não v1** |
| PDF | QuestPDF | NuGet in API (no extra host) | Portfolio catalog export; API generates |
| CI/CD | GitHub Actions + Vercel/Render deploy | GitHub (orchestrator) | See seção 15 |
| Local dev | Docker Compose | Developer machine | Postgres + API + Nuxt |
| **Backend tests** | xUnit · Moq · FluentAssertions · Coverlet | GitHub Actions (`ci-backend.yml`) | `dotnet test`; see seção 23 |
| **Frontend tests** | Vitest | GitHub Actions (`ci-frontend.yml`) | Component/composable unit tests |

### What we explicitly do not do

- **Database per tenant** — too much operational overhead at this scale
- **Supabase project per tenant** — same reason
- **Separate Vercel/Render deployment per artist** — one deployment, tenant routing
- **Store uploaded files on Render** — no persistent disk on free tier
- **Direct browser → Postgres or Supabase Auth** — sem SDK Supabase no frontend
- **Login admin por subdomínio de tenant** — ex.: sem `ana./login`; só `app./login`

---

## 4. Multi-tenant model

### Pattern: shared database, shared schema, row-level isolation

All tenants share one PostgreSQL database. Tenant-owned rows include a `TenantId` column. Isolation is enforced in:

1. **EF Core** — global query filters on `TenantId`
2. **API middleware** — tenant context + membership checks
3. **Supabase Storage** — path prefix `tenants/{tenantId}/...`; escrita só via API (service role)

### Definitions

| Term | Meaning |
|---|---|
| **Tenant** | One artist or studio account on the platform |
| **User** | Pessoa que faz login (`ApplicationUser` + roles via `AspNetUserRoles`) |
| **Platform admin** | Operador (você) — cria tenants, suporte; **billing manual/opcional no v1** |
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
| **PlatformAdmin** (você) | **Sim** — qualquer tenant; fluxo principal do operador |
| **Owner** (artist) | **No** in v1 — deferred to v2 (self-service invites) |
| **Editor** | No |

Each tenant has **one or more** admin users (`Owner`, later `Editor`). Você (PlatformAdmin) convida via API; o convidado define senha e faz login em **`app.onlineportfolio.com.br/login`**.

**Constraints (unchanged):**
- One login account (email) → one `users` row (unique email globally)
- One user → one tenant in v1 (no user belonging to multiple tenants)
- Tenant users only access **their** tenant in admin; PlatformAdmin accesses platform routes (tenant list, user management)

---

## 5. Frontend (Nuxt + Vercel)

### Responsibilities

- **Site público por tenant:** landing + posts/galeria em `{slug}.onlineportfolio.com.br` (e domínio custom depois)
- **Plataforma:** landing em `onlineportfolio.com.br`
- **Admin padronizado:** UI única em `app.onlineportfolio.com.br` (login, CRUD, settings)
- Resolução de tenant pelo `Host` **somente no site público**
- **Todo** tráfego admin e **todos** os dados de app: rotas server do Nuxt → API (padrão BFF)
- **Exceção:** exibição de imagens públicas via URL CDN do Storage em `<img>` (somente leitura)
- **Componentes:** três superfícies isoladas no monorepo (`app/`, `platform/`, `public/`) — admin/login padronizado; site público customizável por tenant — [ADR-016](#adr-016-três-superfícies-e-ui-por-tenant) · guia operacional [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md)

### Rendering strategy

| Page type | Strategy |
|---|---|
| Tenant public: landing, posts, galeria, about | SSG or ISR; cache key includes tenant slug/domain |
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

| Host pattern | Modo | Exemplo |
|---|---|---|
| `app.{platform}` | Admin padronizado | `app.onlineportfolio.com.br/login` |
| `{slug}.{platform}` | Site público do tenant | `ana.onlineportfolio.com.br` |
| `{platform}` / `www.` | Landing da plataforma | `onlineportfolio.com.br` |
| Domínio custom verificado | Site público do tenant | `ana-art.com` |

### Vercel configuration notes

- Single Nuxt project serves all tenants
- Wildcard DNS for subdomains: `*.onlineportfolio.com.br`
- Custom domains added per tenant (manual initially, Vercel Domains API later)
- Check plan limits on number of domains per project

### ADR-016: Três superfícies e UI por tenant

| | |
|---|---|
| **Status** | ✅ Aceito |
| **Data** | 2025-06-21 |
| **Contexto** | Uma única app Nuxt serve marketing da plataforma, sites públicos de N tenants e admin/login centralizado em `app.*`. Clientes podem precisar de landings, páginas de contato e paletas **diferentes**, mas login e painel admin devem permanecer **idênticos** para todos. Risco a evitar: código duplicado entre tenants e mistura de UI pública com UI de admin no mesmo diretório. |
| **Decisão** | Organizar o frontend em **três superfícies** (modos) resolvidas pelo `Host`, com **customização por tenant somente no site público**, via pastas de componentes + temas CSS. Login/admin **nunca** varia por tenant. |
| **Alternativas rejeitadas** | (1) Deploy Nuxt separado por artista — custo operacional e perda de monorepo. (2) Um único diretório `components/` sem separação — dificulta manutenção e incentiva copy-paste. (3) Tema/login por subdomínio de tenant (`ana./login`) — já rejeitado no v1 (seção 21). (4) Config de UI só no banco (JSON de layout) — adiar; código versionado no repo é fonte de verdade no v1. |

**Superfícies (mesmo deploy Vercel):**

| Superfície | `Host` | Layout Nuxt | Pasta de componentes | Custom por tenant? |
|---|---|---|---|---|
| **App** | `app.{platform}` | `layouts/app.vue` | `components/app/` | **Não** — UI padronizada |
| **Platform** | apex / `www.` | `layouts/platform.vue` | `components/platform/` | N/A (produto) |
| **Tenant público** | `{slug}.{platform}` ou domínio custom | `layouts/tenant.vue` | `components/public/tenants/{slug}/` (+ `shared/` só orquestração) | **Sim** |
| **Dev** | `localhost` | `layouts/default.vue` | `components/dev/` | Simulável via env |

**Resolução de host:** middleware global `resolve-host.global.ts` define `surface` + `tenantSlug` (composable `useRequestSurface`). Em localhost: `NUXT_PUBLIC_DEV_SURFACE` e `NUXT_PUBLIC_DEV_TENANT_SLUG`.

**Customização por tenant (site público):**

1. **Componentes** — pasta `components/public/tenants/{slug}/{Bloco}.vue` (landing, contato e demais blocos **exclusivos por tenant**). Orquestração compartilhada em `shared/` (ex.: `TenantHome`). Descoberta via `import.meta.glob` (`tenantComponentResolver.ts`); resolução via `useTenantComponent('LandingHero')`.
2. **Temas** — tokens CSS `--op-*` em `assets/css/main.css`; override por tenant em `assets/css/themes/{slug}.css` + classe `theme-{slug}` no `<html>` (`useTenantTheme()`). CSS de temas carregado por plugin (sem editar `nuxt.config` por slug).
3. **Rotas compartilhadas** — URLs iguais (`/`, `/contact`) em todos os tenants; conteúdo varia por slug, não por path duplicado.
4. **Primitivos globais** — `components/ui/` (`UiButton`, `UiCard`) usam tokens `--op-*`; em rotas tenant herdam `theme-{slug}`, no resto herdam `surface-dark`.

**Dark mode estrutural (fixo):**

- **Exceção única:** site público do tenant (`surface=tenant`) → `theme-{slug}`.
- **Todo o resto** → `html.surface-dark` + `assets/css/surfaces/dark.css`: admin, login, platform marketing, dev skeleton, `error.vue`, layouts `app`/`platform`/`default`/`structural`, componentes `app/*`.
- Erros forçam `surface-dark` mesmo em host de tenant.
- Tenants no repo: **ana** e **joao** apenas.

**Regras obrigatórias:**

- Código em `components/app/` **não** importa de `components/public/tenants/`.
- Novo cliente com UI própria → pasta `public/tenants/{slug}/` com `LandingHero.vue`, `ContactSection.vue`, etc. + `themes/{slug}.css` — **sem** registry manual
- Tenant sem pasta no repo → erro em runtime (cada artista ativo precisa de UI versionada)

**Consequências:**

| Positivo | Trade-off |
|---|---|
| Ana e João podem ter landings/contato/paletas totalmente distintas | Cada tenant “premium” exige pasta no repo (PR) — não é drag-and-drop no admin |
| Login/admin único — menos superfície de bugs de auth | `@nuxt/ui` não adotado ainda; primitivos locais em `ui/` |
| Páginas finas + composables — menos repetição | Registry de `TenantComponentKey` cresce conforme novos blocos |
| Alinha com SSG/ISR por slug (cache key inclui tenant) | DEV-104 ainda deve validar slug desconhecido na API (404) |

**Implementação (skeleton):** ver `frontend/` — layouts `app`, `platform`, `tenant`, `structural`; tenants `ana`, `joao`; guia [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md).

**Quando reavaliar:**

- Muitos tenants com UI 100% custom → CMS ou config de blocos no banco (Fase 2+).
- Adoção de `@nuxt/ui` ou Tailwind — registrar ADR filho ou emendar este doc.
- White-label total do admin — **fora de escopo** no v1; admin permanece dark mode único.

---

## 6. Backend (ASP.NET Core + Render)

### Responsibilities

- All business logic and authorization
- ASP.NET Identity **completo**: login, logout, roles, convites, emissão e validação de JWT
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

Três destinos no projeto — **direct (`db.*.supabase.co`) não entra no pipeline** (só ferramentas manuais opcionais: pg_dump, GUI, com IPv6 ou [add-on IPv4](https://supabase.com/docs/guides/platform/ipv4-address) Supabase).

| Onde | `ConnectionStrings:Default` | `ConnectionStrings:Migration` |
|---|---|---|
| **Dev local** | `localhost:5432` (Compose) | `localhost:5432` (`appsettings.Development.json`) |
| **API prod (Render)** | Transaction pooler **`:6543`** | *(não configurar no Render)* |
| **Migrate prod (CI)** | — | Session pooler **`:5432`** → secret `SUPABASE_MIGRATION_CONNECTION_STRING` |

Modos Supabase (mesmo host pooler `aws-*-*.pooler.supabase.com`, user `postgres.[project-ref]`):

| Modo | Porta | Uso neste projeto |
|---|---|---|
| **Transaction** | `6543` | Runtime da API no Render |
| **Session** | `5432` | `dotnet ef database update` no CI (IPv4 — GitHub Actions, Render, Vercel) |
| **Direct** | `5432` em `db.[ref].supabase.co` | **Não usado** no repo; IPv6 por defeito |

```text
ConnectionStrings__Default      → Transaction pooler :6543 (Render)
ConnectionStrings__Migration    → Session pooler :5432 (GitHub Actions)
```

**Local:** sempre `localhost` para desenvolvimento normal. Session pooler contra Supabase prod **só** em debug pontual (evitar rotina).

**Dev DB Supabase (DEV-008b):** fora do v1 — dev usa Compose. Quando existir projeto dev remoto, reavaliar strings (provavelmente mesmo padrão: session `:5432` migrate, transaction `:6543` runtime).

### Migrations

- EF Core migrations live in the backend project
- Applied by CI on merge/deploy to `main` (session pooler)
- Local dev: `dotnet ef database update` contra **localhost** (Compose)

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

### Decisão: ASP.NET Identity completo

Usar **ASP.NET Identity completo** na API — stack padrão .NET com roles, stores EF e token providers. Bom treinamento como dev e base sólida para evoluir (claims, políticas, OAuth depois).

| Usar (Identity completo) | Fora do v1 |
|---|---|
| `AddIdentity<ApplicationUser, IdentityRole<Guid>>()` | Supabase Auth |
| `UserManager`, `SignInManager`, `RoleManager` | `@nuxtjs/supabase` no Nuxt |
| `AspNetRoles` + `AspNetUserRoles` (Owner, Editor, PlatformAdmin) | Auto-cadastro público |
| `AddDefaultTokenProviders()` — convite, reset de senha | OAuth Google/GitHub (pode vir depois) |
| JWT com **role claims** do Identity | Auth manual com hash caseiro |
| `[Authorize(Roles = "...")]` + checagem de tenant na API | |

**Roles (seed na migration):** `PlatformAdmin`, `Owner`, `Editor` — uma role por usuário no v1.

**Regra tenant + role (validada na API):**

| Role | `tenant_id` |
|---|---|
| `PlatformAdmin` | NULL |
| `Owner` / `Editor` | obrigatório |

**Pacotes NuGet (referência):**

```text
Microsoft.AspNetCore.Identity.EntityFrameworkCore
Microsoft.AspNetCore.Authentication.JwtBearer
```

### Implementação (backend)

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
Registro DI:
  AddIdentity<ApplicationUser, IdentityRole<Guid>>(options => {
      options.Password.RequiredLength = 8;
      options.Lockout.MaxFailedAccessAttempts = 5;
      options.User.RequireUniqueEmail = true;
  })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

  AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(...);

Seed (migration ou startup):
  RoleManager.CreateAsync("PlatformAdmin");
  RoleManager.CreateAsync("Owner");
  RoleManager.CreateAsync("Editor");

Fluxos:
  login         → SignInManager → JWT com claims (sub, email, roles)
  invite        → UserManager.CreateAsync → UserManager.AddToRoleAsync(role)
  accept-invite → UserManager + token provider → ConfirmEmail + senha
  authorize     → [Authorize(Roles = "PlatformAdmin")] + tenant check no service
```

Tabelas Identity padrão na migration EF: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserTokens`, etc.

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

After login, **tenant context comes from the API** (`ApplicationUser.TenantId` + roles Identity), not from the hostname:

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
| Tenant membership | `ApplicationUser.TenantId` + roles em `AspNetUserRoles` |
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

Contact form submissions are handled by the API and delivered via Resend (see seção 11).

Portfolio PDF is generated by the API using QuestPDF (see seção 12).

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

### Decisão v1

**Resend + `noreply@onlineportfolio.com.br` — apenas envio.** Sem caixa postal, sem MX para receber, sem Google Workspace no início.

Ver decisão completa: [ADR-017](./ADR-017-resend-transactional-email.md).

A API envia todo email transacional; o frontend nunca guarda a API key do Resend.

| Config (Render) | Valor |
|---|---|
| `Resend__ApiKey` | server only (API token `re_...`) |
| `Resend__FromEmail` | `noreply@onlineportfolio.com.br` |
| `Resend__FromName` | Online Portfolio |

Inbox operador (`hello@`, etc.) → [seção 2 — Google Workspace depois](#caixa-postal-operador-google-workspace--depois).

### Use cases

| Use case | Phase | Detail |
|---|---|---|
| **Contact form** | Phase 1 | Visitante → API → Resend → `ContactEmail` do tenant; Reply-To = visitante |
| **Convite de usuário** | Phase 1.5 | Resend com link accept-invite |
| **Notificações da plataforma** | Depois | Billing, domínio verificado, etc. |

### O que `noreply@` **não** é

- **Não** é caixa postal — ninguém lê `noreply@`.
- **Não** precisa de MX no Registro.br/Vercel para **receber** mail.
- Respostas ao contato vão para o email do artista ou via Reply-To, não para `noreply@`.

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

- **3.000 emails/mês**, máx. **100/dia** — free tier permanente (suficiente para contato + convites no início)
- **1 domínio** verificado no free tier
- Verificar domínio `onlineportfolio.com.br` (DKIM/SPF) — [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) seção 10.4 · DEV-011
- Templates em `EmailTemplates/` (Razor ou HTML) — versionados no repo

### Implementation (backend)

- NuGet: `Resend` (SDK oficial)
- `IEmailService` abstraction with `ResendEmailService` implementation
- Config: `Resend__ApiKey`, `Resend__FromEmail`, `Resend__FromName`
- Contact endpoint validates: name, email, message, honeypot/rate limit
- Log send failures; do not leak Resend errors to client

### Local development

- API key de dev no password manager; smoke test com `onboarding@resend.dev` ([EXTERNAL_PROVIDERS](./EXTERNAL_PROVIDERS.md) seção 8.5)
- Ou log do payload em `Development` sem enviar
- Domínio `onlineportfolio.com.br` obrigatório em prod (DEV-011)

### ADR-017: Email transacional via Resend

| | |
|---|---|
| **Status** | ✅ Aceito |
| **Data** | 2026-06-21 |
| **Contexto** | SendGrid (Twilio) removeu free tier permanente (mai/2025). Projeto precisa de email transacional .NET com custo zero no MVP. |
| **Decisão** | **Resend** — SDK .NET oficial, templates no repo, 3k emails/mês free. |
| **Alternativas** | SendGrid (pago cedo), Brevo (mais free, SDK verboso), Postmark/SES (custo/complexidade). |

Documento completo: [ADR-017-resend-transactional-email.md](./ADR-017-resend-transactional-email.md).

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
| **Invoices / receipts** | Phase 4 (opcional) | Com billing automatizado (Stripe/PSP), se implementado |

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
| Supabase local stack | Optional | `supabase start` for Storage local (Fase 3); auth fica no Postgres/Identity |

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

| Environment | Frontend | API | Database | Deploy na nuvem? |
|---|---|---|---|---|
| **Local** | Docker Compose (Nuxt) | Docker Compose (API) | Postgres container | ❌ |
| **PR preview** | Vercel preview (por PR) | Local ou mock — **não** API staging | Local / CI | ❌ (efêmero, não é “dev deployado”) |
| **Production** | Vercel (`main`) | Render (Docker) | Supabase | ✅ **único ambiente deployado** |

Local Docker Compose is for day-to-day development. **Only production** is deployed to Vercel + Render + Supabase on merge to `main`.

Ver [ADR-015](#adr-015-deploy-somente-em-production).

### ADR-015: Deploy somente em production

| | |
|---|---|
| **Status** | ✅ Aceito |
| **Data** | 2025-06-21 |
| **Contexto** | Monorepo v1, operador solo/pequeno time, free tiers (Vercel, Render, Supabase). Pergunta: espelhar **dev + prod** deployados (Render dev, Vercel staging, Supabase dev)? |
| **Decisão** | **Somente production deployado** na nuvem. Não criar stack paralela de staging/homologação deployada no v1. |
| **Alternativa rejeitada** | Par completo dev deployado (API dev + front dev + Supabase dev + DNS `dev.*` + workflows duplicados). |

**Modelo em 3 camadas (v1):**

```text
Local (docker compose)     → desenvolvimento diário, Postgres local, API + Nuxt locais
PR preview (Vercel)        → revisão de UI por pull request — efêmero, não substitui staging
Production (main)          → único deploy persistente: Vercel + Render + Supabase prod
```

**O que entra:**

- `deploy-backend.yml` e `deploy-frontend.yml` rodam **só contra production** (push em `main`).
- Um projeto Supabase **`portfolio-prod`** — migrations via CI no prod após merge.
- Render **um** web service (API prod).
- Vercel **um** projeto (front prod); preview de PR nativo; **sem** segundo projeto “staging”.

**O que fica fora do v1:**

- Render service dev / API `api-dev.*`
- Vercel project ou branch fixa de “homologação”
- Supabase project `portfolio-dev` **deployado** (Postgres local cobre dev)
- Workflows `deploy-*-staging.yml` ou matrix prod/dev
- DNS `dev.onlineportfolio.com.br` / `staging.*`

**Consequências:**

| Positivo | Trade-off |
|---|---|
| Menos contas, secrets e DNS para manter | Migrations arriscosas exigem teste local + CI antes do merge |
| Menos custo cognitivo (“qual URL?”) | Sem URL estável de homologação para terceiros |
| Alinha com solo dev + 1–2 artistas iniciais | Reavaliar quando houver 2+ devs ou integrações webhook em homolog |

**Mitigações (sem staging deployado):**

- CI separado (`ci-backend`, `ci-frontend`) em todo PR.
- Docker Compose local = paridade de stack.
- Vercel preview por PR para front.
- Migrations testadas localmente (`dotnet ef database update`) antes do merge; CI migrate só em `main`.

**Quando reavaliar (criar staging deployado):**

- Segundo dev full-time ou cliente precisando URL fixa de homologação.
- Migration destrutiva que quebrou prod uma vez.
- Stripe/webhooks/domínios exigindo ambiente não-prod persistente.
- Tráfego ou dados reais onde “testar só local” deixou de bastar.

Até lá: **local + preview + prod** — ver [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

### Decision: GitHub Actions as pipeline orchestrator

**Preference:** GitHub Actions controls CI/CD. Vercel and Render are **deploy targets**, not the source of truth for the pipeline.

Neither Vercel nor Render **requires** you to rely solely on their dashboard deploy buttons — both support GitHub integration **and** deploy triggered from GitHub Actions. You can (and should) use both together.

| Concern | Owner | Tool |
|---|---|---|
| Tests — **backend** | GitHub Actions | `ci-backend.yml` |
| Tests — **frontend** | GitHub Actions | `ci-frontend.yml` |
| EF migrations (Supabase session pooler) | GitHub Actions | `deploy-backend.yml` |
| Deploy **frontend** (prod) | GitHub Actions | `deploy-frontend.yml` → Vercel CLI |
| Deploy **API** (prod) | Render | Wait for CI em `deploy-backend.yml` |
| Preview deploys (PRs) | Vercel | Native GitHub integration (PR comments) |
| Env vars / secrets | Vercel + Render dashboards + GitHub Secrets | Per platform |

### Decision: **workflows CI separados** (backend + frontend)

**Preferência do projeto:** **dois workflows de CI** no mesmo monorepo — **não** um único `ci.yml` que roda tudo junto.

| Abordagem | Quando usar | Decisão |
|---|---|---|
| **Um `ci.yml` com jobs backend + frontend** | Repo pequeno, todo PR toca os dois lados | ❌ Não — PRs só-frontend esperam `dotnet restore` à toa |
| **Dois workflows CI** (`ci-backend.yml`, `ci-frontend.yml`) | Monorepo com stacks distintas | ✅ **Escolhido** |
| **Dois workflows deploy** (`deploy-backend.yml`, `deploy-frontend.yml`) | Prod simétrico; Actions orquestra os dois | ✅ **Escolhido** |
| **Dois repositórios Git** | Times/release cycles totalmente independentes | ❌ Não — ver seção 20 |

**Por que separado (no monorepo):**

- **Path filters** — PR só em `frontend/` não dispara build .NET (e vice-versa).
- **Status checks claros** no PR: “Backend CI” e “Frontend CI”.
- **Alinha com deploy** — Render (API) e Vercel (Nuxt) já são pipelines distintos.
- **Mesmo PR** pode disparar os dois quando `backend/` **e** `frontend/` mudam (ex.: Epic 1.5 auth).

Isso **não** contradiz o monorepo: continua **um repo**, **um PR**; só os **arquivos de workflow** são separados.

### Git flow — além dos testes, revisar componentes pareados

Em todo PR (humano ou agente), **depois dos testes** e **antes do merge**, conferir se mudou só um lado da stack quando o contrato exige os dois:

| Se alterou… | Conferir no **backend** | Conferir no **frontend** |
|---|---|---|
| Endpoint / DTO / contrato API | Controller, service, validação, `[Authorize]`, OpenAPI | Rota proxy `server/api/**`, composable, tipos TS, página/componente |
| Schema / entidade EF | Migration + seed se necessário | Tipos/consumo da API; formulários se expõe o campo |
| Auth / JWT / roles | Identity, policies, middleware tenant | Proxy (cookies/headers), middleware host, fluxo login em `app.*` |
| Variável de ambiente | `appsettings`, Render env | `runtimeConfig`, Vercel env, `frontend/.env.example` |
| Regra multi-tenant | Filtro EF + checagem membership | Nunca enviar `tenantId` confiável do client; UI admin vs público |
| Feature admin | Rotas protegidas API | Páginas em host `app.*` |
| Feature site público | API pública (só conteúdo publicado) | Páginas `{slug}.*`, middleware tenant |
| Email transacional | Resend service + template | Só se houver UI (ex.: preview) |
| Storage / upload (Fase 3) | Multipart API + service role | Proxy upload; `<img>` CDN URL |

**Checklist rápido antes do merge:**

1. CI relevante verde (`Backend CI` / `Frontend CI` conforme paths do PR).
2. Comentários sticky **Backend coverage** / **Frontend coverage** no PR (se o CI da stack rodou).
3. **Pareamento front↔back** — tabela acima; se API mudou, front **ou** docs do contrato atualizados.
3. `frontend/.env.example` se novos env vars Nuxt; `appsettings.json` (+ `Development`) se novos env vars da API.
4. `docs/DATABASE.md` se schema mudou de forma relevante.
5. Commits [Conventional Commits](./CONVENTIONAL_COMMITS.md); escopo `frontend` / `backend` coerente com paths.

Detalhes para agentes: [docs/AGENT_GUIDE.md](./AGENT_GUIDE.md).

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
  Revisão manual   → componentes pareados front↔back (tabela acima)
  Vercel           → preview deployment (native PR integration)

Push to main:
  deploy-backend.yml  → dotnet test → EF migrate (session pooler :5432) → status check
  Render              → deploy API (Wait for CI — após deploy-backend green)
  deploy-frontend.yml → npm lint/test → vercel pull → npm run build → vercel deploy --prebuilt --prod
```

**Prod simétrico:** backend e frontend têm workflow de deploy próprio. **Preview de PR** continua no Vercel (integração nativa).

**Ordem:** API deploy **deve** rodar após migrations. Frontend **não** depende de migrate, mas **deve** passar pelos CIs de PR antes do merge (branch protection).

### Render: Wait for CI

In Render service settings → enable **Wait for CI** (or equivalent). Render waits for GitHub commit status checks from Actions before deploying. Migrations must complete inside the Actions workflow **before** checks pass.

### Vercel: prod via Actions (preview nativo no PR)

| Modo | Config | Decisão |
|---|---|---|
| **Preview (PR)** | Vercel GitHub App — auto preview por PR | ✅ Mantém |
| **Production (`main`)** | `deploy-frontend.yml` → `vercel deploy --prod` | ✅ **Escolhido** |
| **Production auto-deploy no dashboard Vercel** | Deploy a cada push em `main` | ❌ **Desligar** — evita deploy prod sem passar pelo workflow |

**Recommendation (atualizada):** **CI separado + deploy separado** — espelha Render/backend:

- PR: `ci-backend.yml` + `ci-frontend.yml` + preview Vercel
- `main`: `deploy-backend.yml` (migrate + gate Render) + `deploy-frontend.yml` (Vercel CLI)
- Branch protection: exige Backend CI + Frontend CI antes do merge

### Example workflow structure

```text
.github/workflows/
  ci-backend.yml       # pull_request + push: dotnet test, build (paths backend/**)
  ci-frontend.yml      # pull_request + push: npm lint, test (paths frontend/**)
  deploy-backend.yml   # push main → production: test → EF migrate → gate Render
  deploy-frontend.yml  # push main → production: lint/test → vercel pull → npm run build → vercel deploy --prebuilt --prod
```

**Path filters (exemplo):**

| Workflow | Dispara quando mudam |
|---|---|
| `ci-backend.yml` | `backend/**`, `docs/DATABASE.md`, `.github/workflows/ci-backend.yml`, `deploy-backend.yml` |
| `ci-frontend.yml` | `frontend/**`, `.github/workflows/ci-frontend.yml`, `deploy-frontend.yml` |
| `deploy-backend.yml` | `push` → `main`; paths `backend/**`, migrations |
| `deploy-frontend.yml` | `push` → `main`; paths `frontend/**` |

PRs **só em `docs/`** podem incluir ambos workflows (paths ampliados) ou um `ci-docs.yml` leve — ver [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md).

**Branch protection em `main`:** exigir status checks **Backend CI** e **Frontend CI** (checks skipped por path filter contam como OK no GitHub).

### CI vs Deploy — responsabilidades

Cada workflow de **deploy** roda **só os testes da própria stack** (embutidos no job). **Não** invoca o workflow de CI separado nem a stack oposta:

| Workflow | O que roda antes do deploy |
|---|---|
| `deploy-backend.yml` | `dotnet test` + `ef database update` |
| `deploy-frontend.yml` | `npm run lint` + `npm run test:coverage` + Vercel CLI |
| `ci-backend.yml` | `dotnet test` + coverage (PR comment) — **não deploya** |
| `ci-frontend.yml` | lint + test + coverage (PR comment) — **não deploya** |

**Push `main` (path filters):**

| Mudança | Dispara |
|---|---|
| Só `backend/**` | Backend CI + Backend Deploy (Frontend CI **não**) |
| Só `frontend/**` | Frontend CI + Frontend Deploy (Backend CI **não**) |
| Só `docs/**` | Nada (salvo path do workflow incluir o doc) |
| Ambos | Os 4 workflows (2 CI + 2 deploy) |

**Duplicação intencional:** no mesmo push, CI e deploy da mesma stack rodam testes separados — CI fornece status check nomeado para branch protection; deploy é gate autossuficiente antes de migrate/Vercel (não depende de outro workflow ter terminado).

**Melhorias opcionais (futuro):** reusable workflows `test-backend.yml` / `test-frontend.yml`; `workflow_run` para deploy após CI green; `ci-docs.yml` leve para PRs só em `docs/`. Nenhuma é necessária para o desenho atual.

**GitHub Secrets (Actions):**

| Secret | Purpose |
|---|---|
| `SUPABASE_MIGRATION_CONNECTION_STRING` | Session pooler URI (port 5432, IPv4) |
| `VERCEL_TOKEN` | Deploy prod via `deploy-frontend.yml` |
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
| Email (Resend) | Free tier (3.000 emails/month) |
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
  + colunas Identity (NormalizedEmail, SecurityStamp, EmailConfirmed, …)
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

- ASP.NET Identity **completo** + JWT na API
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

### Phase 4 — Custom domains (+ billing opcional)

- Custom domain onboarding per tenant
- Vercel Domains API automation
- **Billing / subscriptions (opcional — skip no v1):** Stripe preferível se expandir **fora do Brasil**; Asaas/Iugu se só BR — ver [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md)
- Plan limits (DEV-402) — opcional; pode ser manual
- Self-serve signup (optional)
- Google Workspace for operator inbox (optional)

---

## 20. Repository structure

### Decision: **monorepo** (single Git repository)

| | Monorepo (chosen) | Repos separados |
|---|---|---|
| **Local dev** | Um `docker compose up` sobe Postgres + API + Nuxt | Dois clones, duas redes, env duplicado |
| **CI/CD** | Workflows separados: `ci-backend`, `ci-frontend`, `deploy-backend`, `deploy-frontend` | Um `ci.yml` único; ou repos Git separados |
| **Docs / BACKLOG** | Uma fonte de verdade (`docs/`, `DEV-xxx`) | Docs divergem entre repos |
| **Epic 1.5 (auth)** | Front + back no mesmo PR quando API e proxy mudam juntos | PRs coordenados entre repos |
| **Deploy** | Mesmo repo: Vercel `root=frontend`, Render `root=backend` | Funciona, mas mais overhead operacional |
| **Escala do time** | Ideal para 1 dev / time pequeno | Faz sentido com times grandes e release cycles independentes |

**Conclusão:** use **um repositório** `online-portfolio/` com pastas `frontend/` e `backend/`. Vercel e Render apontam para **subpastas do mesmo repo** — não são repos Git separados.

Repos separados só valeria reconsiderar se no futuro frontend e backend tiverem **ciclos de release totalmente independentes** e **times distintos** — não é o caso no v1.

```text
online-portfolio/          ← um repo Git
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
│   ├── pages/                       # rotas compartilhadas; conteúdo varia por surface/tenant
│   ├── components/
│   │   ├── ui/                      # primitivos globais (Ui*)
│   │   ├── app/                     # admin + login (padronizado)
│   │   ├── platform/                # marketing apex
│   │   └── public/
│   │       ├── shared/              # defaults tenant (Public*)
│   │       └── tenants/{slug}/      # overrides por cliente (Ana*, Joao*, …)
│   ├── layouts/                     # app | platform | tenant | default
│   ├── assets/css/themes/           # paletas por tenant (.theme-{slug})
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

Ver [docs/ARCHITECTURE.md](./ARCHITECTURE.md) e [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md).

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
Resend__FromEmail=noreply@onlineportfolio.com.br
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
| Repo layout | Monorepo vs split | **Decidido** — monorepo; Vercel + Render same repo, different roots |
| Subdomain vs path fallback | `ana.onlineportfolio.com.br` vs `onlineportfolio.com.br/ana` | Subdomain recommended |
| Self-serve signup | Manual vs automated | Manual for first customers |
| Contact message history | Email only vs store in DB | Email only for v1 (Resend) |
| Operator inbox | Google Workspace | **Decidido** — fora do v1; v1 = só Resend `noreply@` |
| Global `.com` domain | `onlineportfolio.com` taken | Revisit alternative `.com` later if expanding internationally |
| CI/CD approach | GitHub Actions orchestrator | **Decidido** — see seção 15 |
| Ambientes deployados | Prod only vs prod + staging | **Decidido** — [ADR-015](#adr-015-deploy-somente-em-production); staging deployado **fora do v1** |
| Image processing | On upload in API vs external worker | Phase 3 |
| Thumbnail generation | API (ImageSharp) vs Supabase transform | TBD |
| Postgres RLS | Enable as defense in depth | Optional while API-only access |
| Render cold starts | Accept vs upgrade to paid / Fly.io | Revisit after launch |
| Multi-user per tenant | Owner invites Editors | v1: **PlatformAdmin** adds users; v2: Owner self-service |
| Custom domain admin | `ana-art.com/admin` | Defer; use platform app host |
| Primary keys (PK/FK) | `uuid` vs `serial` / hybrid | **Decidido** — `uuid`/`Guid` domain PKs; slug in public URLs — [docs/DATABASE.md](./DATABASE.md) |
| UI pública por tenant | Monolito vs deploy por artista vs só DB | **Decidido** — mesma app Nuxt; pastas `public/tenants/{slug}/` + temas CSS — [ADR-016 seção 5](#adr-016-três-superfícies-e-ui-por-tenant) |

---

## 23. Testing

### Backend stack (padrão da indústria)

Projeto **`OnlinePortfolio.Api.Tests`** na solution `OnlinePortfolio.Api.slnx`, com pastas `Unit/` e `Integration/`.

| Ferramenta | Pacote NuGet | Uso |
|---|---|---|
| **xUnit** | `xunit` + `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` | Framework de testes |
| **Moq** | `Moq` | Mocks de interfaces (`IEmailService`, `ITenantProvider`, etc.) |
| **FluentAssertions** | `FluentAssertions` | Asserções legíveis (`result.Should().BeOk()`) |
| **Coverlet** | `coverlet.collector` | Cobertura de código (local + CI) |
| **CLI** | — | `dotnet test` — runner oficial |

**Integração** (IT-001+, além da stack acima):

| Ferramenta | Pacote | Uso |
|---|---|---|
| **WebApplicationFactory** | `Microsoft.AspNetCore.Mvc.Testing` | Testes HTTP end-to-end contra a API in-process |
| **Testcontainers** | `Testcontainers.PostgreSql` (ou Postgres service container no GitHub Actions) | Postgres real isolado por fixture |

### Comandos locais

```bash
cd backend

# Todos os testes da solution
dotnet test OnlinePortfolio.Api.slnx

# Só o projeto de testes
dotnet test OnlinePortfolio.Api.Tests/OnlinePortfolio.Api.Tests.csproj

# Com cobertura (Coverlet → arquivo em TestResults/ — gitignored)
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults

# Relatório HTML visual (Windows — pasta: raiz do repo)
powershell -ExecutionPolicy Bypass -File scripts/coverage-backend.ps1
```

Saída do relatório: `TestResults/CoverageReport/index.html`. A pasta `TestResults/` **nunca** entra no Git (`.gitignore`).

### CI

| Workflow | Comando | Quando |
|---|---|---|
| `ci-backend.yml` | `dotnet test` (Release) + Coverlet | PR + push em `main`; comentário **Backend coverage** no PR |
| `ci-frontend.yml` | `npm run lint` + `npm run test:coverage` | PR + push em `main`; comentário **Frontend coverage** no PR |
| `deploy-backend.yml` | `dotnet test` antes de migrate/deploy | Push em `main` (DEV-007) |

Cobertura no CI é **opcional no v1**; quando habilitada, usar o mesmo `--collect:"XPlat Code Coverage"` no workflow.

### Frontend (referência)

Testes de componentes/composables: **Vitest** (UT-009+). Stack separada — ver [docs/BACKLOG.md](./BACKLOG.md).

### Tarefas relacionadas

- Scaffold do projeto: DEV-005b ([docs/BACKLOG.md](./BACKLOG.md))
- Infra integração (WebApplicationFactory, DB, CI): [docs/BACKLOG.md](./BACKLOG.md)
- Casos de teste: [docs/BACKLOG.md](./BACKLOG.md) · [docs/BACKLOG.md](./BACKLOG.md)

---

## Quick reference

```text
Public site:    {slug}.onlineportfolio.com.br ou domínio custom  →  Vercel (por tenant)
Platform:       onlineportfolio.com.br                            →  Vercel (marketing)
Admin:          app.onlineportfolio.com.br (padronizado)          →  Vercel
API:            api.onlineportfolio.com.br (via proxy Nuxt)       →  Render
Database:       Supabase PostgreSQL                               →  EF Core only
Files:          Supabase Storage                                  →  tenants/{tenantId}/...
Auth:           ASP.NET Identity completo + JWT na API           →  Supabase = DB + Storage
Email (app):    Resend + noreply@ (só envio)                    →  contato + convites
Email (ops):    Google Workspace (depois)                         →  caixa postal
PDF:            QuestPDF                                          →  catálogo via API
Domain:         onlineportfolio.com.br (Registro.br)
Isolation:      TenantId + EF filters + Storage paths + API
IDs:            uuid PK/FK (Guid); slug for public tenant URLs — ver `docs/DATABASE.md` (seção Primary keys)
CI/CD:          GitHub Actions → dotnet test + migrations; deploy **prod only** (ADR-015)
Frontend UI:    3 surfaces (app/platform/tenant); per-tenant public components + CSS themes (ADR-016)
Backend tests:  xUnit · Moq · FluentAssertions · Coverlet · dotnet test
Environments:   local + PR preview + production (no staging deploy v1)
Local dev:      docker compose up
```
