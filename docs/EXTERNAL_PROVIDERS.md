# External Provider Configuration Guide

Step-by-step configuration for every third-party service required to run the artist portfolio platform in production.

**Related:** [ARCHITECTURE.md](./ARCHITECTURE.md) · [BACKLOG.md](./BACKLOG.md)  
**Platform domain:** `onlineportfolio.com.br` — **registrado** no [Registro.br](https://registro.br)  
**Email v1:** SendGrid `noreply@onlineportfolio.com.br` — só envio transacional, sem caixa postal  
**Project management:** [Linear](https://linear.app) — issues a partir do BACKLOG (`DEV-xxx`)  
**Operator inbox (futuro):** Google Workspace — depois do lançamento

---

## Table of contents

1. [Overview](#1-overview)
   - [1.1 Master checklist — criar contas](#11-master-checklist--criar-contas)
2. [Recommended setup order](#2-recommended-setup-order)
3. [Domain & DNS](#3-domain--dns)
4. [Supabase](#4-supabase)
5. [Render (API)](#5-render-api)
6. [Vercel (Frontend)](#6-vercel-frontend)
7. [SendGrid (Email)](#7-sendgrid-email)
8. [Google Workspace (caixa postal operador — futuro)](#8-google-workspace-caixa-postal-operador--futuro)
9. [GitHub (CI/CD)](#9-github-cicd)
10. [Linear (project management)](#10-linear-project-management)
11. [QuestPDF (no external config)](#11-questpdf-no-external-config)
12. [Per-tenant setup (custom domains)](#12-per-tenant-setup-custom-domains)
13. [Future: Stripe (Phase 4)](#13-future-stripe-phase-4)
14. [Secrets & environment variable map](#14-secrets--environment-variable-map)
15. [Phase checklists](#15-phase-checklists)
16. [Troubleshooting](#16-troubleshooting)

---

## 1. Overview

### Services involved

| Provider | Purpose | Contas | Guia |
|---|---|---|---|
| **Registro.br** | Domínio `onlineportfolio.com.br` | ✅ 1 (feito) | [§3.1](#31-domínio-onlineportfoliocombr-registrobr) |
| **GitHub** | Repo monorepo + CI | 1 repo | [§9](#9-github-cicd) |
| **Linear** | Issues, sprints, backlog (`DEV-xxx`) | 1 workspace | [§10](#10-linear-project-management) |
| **Supabase** | PostgreSQL, Storage | 1 prod (+ dev opcional) | [§4](#4-supabase) |
| **Render** | ASP.NET Core API (Docker) | 1 web service | [§5](#5-render-api) |
| **Vercel** | Nuxt frontend | 1 project | [§6](#6-vercel-frontend) |
| **SendGrid** | Email transacional (`noreply@`) | 1 conta; sem inbox | [§7](#7-sendgrid-email) |
| **Google Workspace** | Caixa postal operador (futuro) | Fora do v1 | [§8](#8-google-workspace-caixa-postal-operador--futuro) |
| **QuestPDF** | PDF generation | Sem conta (NuGet) | [§11](#11-questpdf-no-external-config) |

### 1.1 Master checklist — criar contas

Use esta tabela para abrir cada serviço na ordem. Marque conforme for concluindo.

| # | Provider | Criar conta | Painel | Doc deste repo | Status |
|---|---|---|---|---|---|
| 1 | **Registro.br** | [registro.br](https://registro.br) | [Painel NIC](https://registro.br/login/) | [§3.1](#31-domínio-onlineportfoliocombr-registrobr) | ✅ **Feito** — domínio comprado |
| 2 | **GitHub** | [github.com/signup](https://github.com/signup) | [github.com](https://github.com) | [§9](#9-github-cicd) | ⬜ Criar repo `online-portfolio` |
| 3 | **Linear** | [linear.app/signup](https://linear.app/signup) | [linear.app](https://linear.app) | [§10](#10-linear-project-management) | ⬜ Workspace + import BACKLOG |
| 4 | **Supabase** | [supabase.com/dashboard](https://supabase.com/dashboard) | [Dashboard](https://supabase.com/dashboard) | [§4](#4-supabase) | ⬜ Projeto `portfolio-prod` |
| 5 | **Render** | [dashboard.render.com/register](https://dashboard.render.com/register) | [Render](https://dashboard.render.com) | [§5](#5-render-api) | ⬜ Web service API |
| 6 | **Vercel** | [vercel.com/signup](https://vercel.com/signup) | [Vercel](https://vercel.com/dashboard) | [§6](#6-vercel-frontend) | ⬜ Projeto Nuxt |
| 7 | **SendGrid** | [signup.sendgrid.com](https://signup.sendgrid.com/) | [SendGrid](https://app.sendgrid.com) | [§7](#7-sendgrid-email) | ⬜ API key + domínio |
| 8 | **Google Workspace** | [workspace.google.com](https://workspace.google.com/) | [Admin](https://admin.google.com) | [§8](#8-google-workspace-caixa-postal-operador--futuro) | ⏸️ Depois do lançamento |
| 9 | **Stripe** | [dashboard.stripe.com/register](https://dashboard.stripe.com/register) | [Stripe](https://dashboard.stripe.com) | [§13](#13-future-stripe-phase-4) | ⏸️ Fase 4 |

**O que guardar em password manager / GitHub Secrets:** ver [§14](#14-secrets--environment-variable-map).

**Ordem recomendada de configuração:** [§2](#2-recommended-setup-order) (não precisa criar todas as contas no mesmo dia — domínio já está ok).

### Domain layout (production)

Mapa completo: [ARCHITECTURE.md §2.1](./ARCHITECTURE.md#21-mapa-de-domínios-e-superfícies-do-produto)

| Host | Aponta para | Finalidade |
|---|---|---|
| `onlineportfolio.com.br` | Vercel | Landing da **plataforma** (SaaS) |
| `www.onlineportfolio.com.br` | Vercel | Redirect ou alias para apex |
| `{slug}.onlineportfolio.com.br` | Vercel | Site **público** do tenant (landing + posts) — ex.: `ana.`, `mark.` |
| `*.onlineportfolio.com.br` | Vercel | Wildcard para subdomínios de tenant |
| `app.onlineportfolio.com.br` | Vercel | **Admin padronizado** — login único; proxy → API |
| `api.onlineportfolio.com.br` | Render | API .NET (acesso via proxy Nuxt, não direto do browser) |
| `{domínio-do-artista}.com` | Vercel | Domínio custom do tenant — site público (Fase 4) |

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
                     │ SendGrid │
                     └──────────┘
```

---

## 2. Recommended setup order

Configure in this order to avoid circular dependencies:

```text
1. Registro.br          → ✅ onlineportfolio.com.br registrado (DNS no deploy — §3)
2. GitHub               → repo monorepo + push inicial
3. Linear               → workspace + importar BACKLOG (§10) — pode ser cedo, ajuda no tracking
4. Supabase             → projeto, connection strings, keys (Postgres + Storage)
5. Render               → conta + conectar repo (deploy após DEV-001)
6. SendGrid             → conta + API key (domínio auth no deploy — §7)
7. Vercel               → conta + conectar repo (deploy após DEV-001)
8. DNS (no deploy)      → nameservers Vercel + api. → Render (§3.2)
9. GitHub Actions       → secrets migration + workflows (§9)
10. SendGrid (prod)     → autenticar onlineportfolio.com.br na Vercel DNS
11. Por tenant          → domínios custom Vercel (Fase 4)
12. Google Workspace    → inbox operador (futuro)
```

---

## 3. Domain & DNS

### 3.1 Domínio `onlineportfolio.com.br` (Registro.br)

**Status:** ✅ **Registrado** — titular ativo no Registro.br. Próximo passo no deploy: DNS ([§3.2](#32-dns-strategy-at-deploy-time)).

| Item | Valor |
|---|---|
| **Domínio** | `onlineportfolio.com.br` |
| **Registrar** | [Registro.br](https://registro.br) — único registrador oficial `.br` |
| **Painel** | [registro.br/login](https://registro.br/login/) |
| **Renovação** | ~R$ 40/ano (Pix, boleto ou cartão) |
| **Titular** | Seu CPF/CNPJ — não transfere para Vercel |
| **DNS agora** | Pode manter DNS padrão Registro.br até deploy |
| **DNS no deploy** | Nameservers Vercel (recomendado) — [§3.2](#32-dns-strategy-at-deploy-time) |

| Check | Result |
|---|---|
| `onlineportfolio.com` | **Indisponível** — registrado por terceiros |
| `onlineportfolio.com.br` | ✅ **Seu** — registrado |

#### Verificar no painel Registro.br

- [x] Domínio `onlineportfolio.com.br` com status **Ativo** / **Publicado**
- [ ] Anotar data de expiração / renovação automática
- [ ] Guardar login Registro.br no password manager
- [ ] DNS: deixar padrão **ou** já apontar NS Vercel se for deploy em breve ([§3.2](#32-dns-strategy-at-deploy-time))

#### Referências Registro.br

| Recurso | URL |
|---|---|
| Painel / login | https://registro.br/login/ |
| Ajuda DNS | https://registro.br/ajuda |
| WHOIS | https://registro.br/tecnologia/ferramentas/whois |
| Alterar nameservers | Painel → domínio → **Alterar servidores DNS** |

#### Por que `.com.br` continua ok

- Your first two artists are in Brazil — `.com.br` is trusted locally
- Vercel, Render, Supabase, SendGrid, and Google Workspace all support `.com.br` custom domains
- Same architecture: `app.`, `api.`, `{slug}.` subdomains work identically
- Fixed price: **R$ 40/year** with no renewal surprises
- Payment: Pix, boleto, or card — no international card required

**Trade-off:** Menos “SaaS global” que `.com` — ok para v1 Brasil.

#### Registro concluído — o que fazer agora vs no deploy

| Quando | Ação |
|---|---|
| **Agora (pós-compra)** | Confirmar **Ativo** no painel; guardar credenciais; opcional: criar contas GitHub, Linear, Supabase |
| **Epic 0 (DEV-001)** | Push monorepo no GitHub |
| **Deploy** | Nameservers Vercel + domínios Vercel/Render ([§3.2](#32-dns-strategy-at-deploy-time)) |
| **Pós-deploy** | SendGrid DKIM na Vercel DNS ([§3.5](#35-dns-de-email--sendgrid-v1-só-envio)) |

#### Registro.br + Vercel nameservers (titular continua no Registro.br)

Changing nameservers to Vercel **does not transfer** the domain. You still own it, renew at Registro.br (R$ 40/year), and manage titular (CPF/CNPJ) there. Vercel only hosts DNS records.

```text
Registro.br  →  titular, renovação, NS → ns1/ns2.vercel-dns.com
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
| **SendGrid** | — | N/A | SPF/DKIM for email, not web |

All providers accept `.com.br` and subdomains.

#### Future: global `.com` (optional)

`onlineportfolio.com` is taken. If needed later, consider monitoring it for expiry or registering an alternative (e.g. `portfolioonline.com`, `getonlineportfolio.com`) and pointing it to the same Vercel app as an alias.

### 3.2 DNS strategy (at deploy time)

#### Option A — Vercel nameservers (recommended)

1. Registro.br panel → **Alterar servidores DNS**:
   ```text
   ns1.vercel-dns.com
   ns2.vercel-dns.com
   ```
2. Vercel project → **Settings → Domains** → add:
   ```text
   onlineportfolio.com.br
   app.onlineportfolio.com.br
   *.onlineportfolio.com.br
   ```
3. Vercel DNS → add CNAME for API:
   ```text
   api  →  CNAME  →  your-service.onrender.com
   ```
4. Wait for SSL (automatic, usually minutes after DNS propagates)

#### Option B — DNS at Registro.br (manual records)

Add records in Registro.br panel after Vercel shows required values (`vercel domains inspect` or dashboard):

| Type | Name | Value | Purpose |
|---|---|---|---|
| `A` | `@` | Vercel IP (from dashboard) | Apex |
| `CNAME` | `www` | `cname.vercel-dns.com` | www |
| `CNAME` | `app` | `cname.vercel-dns.com` | Admin |
| `CNAME` | `ana` | `cname.vercel-dns.com` | Per-tenant (repeat per slug) |
| `CNAME` | `api` | Render hostname | API |

Wildcard `*.onlineportfolio.com.br` SSL on Vercel **requires** Option A (Vercel nameservers).

### 3.3 DNS records reference (Vercel)

Add these in your DNS provider **after** connecting the domain in Vercel (Vercel shows exact values — use theirs if different):

| Type | Name | Value | Purpose |
|---|---|---|---|
| `A` | `@` | Vercel IP (shown in dashboard) | Apex → marketing site |
| `CNAME` | `www` | `cname.vercel-dns.com` | www alias |
| `CNAME` | `app` | `cname.vercel-dns.com` | Admin app |
| `CNAME` | `*` | `cname.vercel-dns.com` | Wildcard tenant subdomains |

**Wildcard note:** Some registrars require DNS on Cloudflare or similar to support `*.onlineportfolio.com.br`. Verify wildcard SSL is issued in Vercel after DNS propagates.

### 3.4 Records for Render (API)

In Vercel **or** DNS provider (Render dashboard gives instructions):

| Type | Name | Value | Purpose |
|---|---|---|---|
| `CNAME` | `api` | Render-provided hostname (e.g. `xxx.onrender.com`) | API custom domain |

Enable **HTTPS** on Render after DNS verification.

### 3.5 DNS de email — SendGrid (v1, só envio)

Para autenticar `noreply@onlineportfolio.com.br` no SendGrid, adicione CNAME/TXT na **Vercel DNS** (não precisa de MX — **sem caixa postal** no v1):

| Type | Name | Value |
|---|---|---|
| `CNAME` | `emXXXX` (SendGrid provides) | SendGrid target |
| `CNAME` | `s1._domainkey` | SendGrid DKIM |
| `CNAME` | `s2._domainkey` | SendGrid DKIM |

Add **SPF** TXT record if SendGrid instructs (include `sendgrid.net`).

---

## 4. Supabase

| | |
|---|---|
| **Criar conta** | [supabase.com/dashboard](https://supabase.com/dashboard) (GitHub ou email) |
| **Painel** | [supabase.com/dashboard](https://supabase.com/dashboard) |
| **Docs** | [supabase.com/docs](https://supabase.com/docs) |
| **Pricing** | [supabase.com/pricing](https://supabase.com/pricing) — free tier ok para v1 |

### 4.1 Create project

| Setting | Recommendation |
|---|---|
| **Organization** | Your org |
| **Project name** | `portfolio-prod` — **único projeto Supabase no v1** ([ADR-015](./ARCHITECTURE.md#adr-015-deploy-somente-em-production)) |
| **Region** | Closest to most users (e.g. EU West if artists in Europe) |
| **Database password** | Strong password — store in password manager |

Wait for project provisioning (~2 minutes).

### 4.2 Database — connection strings for EF Core

Go to **Project Settings → Database → Connection string**.

You need **two** connection strings:

| Use | Mode in Supabase UI | Port | Used by |
|---|---|---|---|
| **Runtime (API)** | Connection pooling → Transaction | `6543` | Render `ConnectionStrings__Default` |
| **Migrations (CI/local)** | Direct connection | `5432` | GitHub Actions, `dotnet ef database update` |

**Format (URI):**

```text
postgresql://postgres.[project-ref]:[password]@aws-0-[region].pooler.supabase.com:6543/postgres
postgresql://postgres.[project-ref]:[password]@db.[project-ref].supabase.co:5432/postgres
```

**Settings to verify:**

- [ ] Use **Transaction** pool mode for EF Core on port 6543 (not Session, unless you configure accordingly)
- [ ] Enable **SSL** — Supabase requires SSL; Npgsql connection string should include `SSL Mode=Require`
- [ ] Do **not** expose these strings in frontend or git

**Dev / staging Supabase — fora do v1:**

Não criar segundo projeto Supabase deployado para homologação. Desenvolvimento usa **Postgres local** (Docker Compose). Decisão: [ARCHITECTURE ADR-015](./ARCHITECTURE.md#adr-015-deploy-somente-em-production).

Reavaliar projeto Supabase staging apenas quando ADR-015 indicar (ex.: segundo dev, webhooks em homolog).

### 4.3 API keys

Go to **Project Settings → API**.

| Key | Where it goes | Never put in |
|---|---|---|
| **Project URL** | `https://[ref].supabase.co` → Render `Supabase__Url` (Storage, Phase 3+) | — |
| **service_role** | Render `Supabase__ServiceRoleKey` only | Frontend, git, browser |

The **service role** bypasses Storage policies — treat like a root password. **Anon key is not used in v1** (no client-side Supabase SDK).

### 4.4 Autenticação — **não usa Supabase Auth**

Login admin usa **ASP.NET Identity + JWT** na API .NET. Supabase fornece **Postgres + Storage** apenas.

| Concern | Onde |
|---|---|
| Senhas | Postgres via EF Identity |
| Assinatura JWT | Render: `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` |
| UI de login | `app.onlineportfolio.com.br/login` → proxy Nuxt → API |
| Emails de convite | SendGrid (API envia no convite) |

Nenhuma configuração do dashboard Supabase Auth no v1.

### 4.5 Storage (Phase 3+)

Go to **Storage → New bucket**.

| Bucket | Public | Purpose |
|---|---|---|
| `artworks-public` | Yes | Web images, thumbnails |
| `artworks-originals` | No | High-res originals (signed URLs) |

**Access model (v1):**

- **Public read:** gallery pages use CDN URLs in `<img>` (no auth)
- **Admin write:** API only, using service role key — no browser → Storage uploads

Path prefix: `tenants/{tenantId}/...`

### 4.6 Network / security (optional hardening)

| Setting | Location | Note |
|---|---|---|
| **Network restrictions** | Database settings | Optional IP allowlist for prod DB (Render egress IPs change on free tier — often skip on free) |
| **RLS on Postgres tables** | SQL | Optional defense-in-depth; API uses service role or direct connection — primary isolation remains in EF |

### 4.8 Supabase checklist summary

**Phase 1 (database only):**

- [ ] Project created
- [ ] Direct + pooler connection strings saved
- [ ] Service role key saved (Storage, Phase 3+)
- [ ] Migrations applied via CI or local EF

**Phase 1.5 (auth — API, not Supabase):**

- [ ] Render: `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`
- [ ] SendGrid: invite + transactional email
- [ ] Test login via Nuxt proxy at `app.onlineportfolio.com.br/login`

**Phase 3 (storage):**

- [ ] Buckets created (public read)
- [ ] API upload path tested (multipart via proxy)
- [ ] Gallery `<img>` CDN URLs working

---

## 5. Render (API)

| | |
|---|---|
| **Criar conta** | [dashboard.render.com/register](https://dashboard.render.com/register) |
| **Painel** | [dashboard.render.com](https://dashboard.render.com) |
| **Docs** | [render.com/docs](https://render.com/docs) |
| **GitHub App** | [Render → Account Settings → GitHub](https://dashboard.render.com) (conectar após login) |
| **Pricing** | [render.com/pricing](https://render.com/pricing) — free tier com cold start |

### 5.1 Create web service

| Setting | Value |
|---|---|
| **Type** | Web Service |
| **Source** | Connect GitHub repo |
| **Root directory** | `backend` (or repo root if Dockerfile path set) |
| **Runtime** | Docker |
| **Dockerfile path** | `backend/Dockerfile` |
| **Branch** | `main` |
| **Region** | Same as Supabase when possible |
| **Instance type** | Free |

### 5.2 Service configuration

| Setting | Value |
|---|---|
| **Health check path** | `/health` |
| **Auto-deploy** | Yes (on push to main) |
| **Build command** | (Docker handles build) |
| **Start command** | (from Dockerfile `ENTRYPOINT`) |

**Free tier behavior:**

- Spins down after ~15 min idle
- Cold starts 5–30+ seconds
- No persistent disk — do not store uploads locally

### 5.3 Custom domain

1. **Settings → Custom Domains → Add** `api.onlineportfolio.com.br`
2. Add CNAME at DNS provider (see [§3.4](#34-records-for-render-api))
3. Wait for SSL certificate provisioning

### 5.4 Environment variables

Set in **Environment → Environment Variables** (or `render.yaml`):

| Variable | Value | Secret |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | No |
| `ASPNETCORE_URLS` | `http://+:8080` | No |
| `ConnectionStrings__Default` | Supabase pooler URI (`6543`) | Yes |
| `Jwt__Secret` | Chave de assinatura JWT da API | Yes |
| `Jwt__Issuer` | `OnlinePortfolio.Api` | No |
| `Jwt__Audience` | `OnlinePortfolio.Admin` | No |
| `Supabase__Url` | `https://[ref].supabase.co` (Storage, Fase 3+) | No |
| `Supabase__ServiceRoleKey` | Supabase service role | Yes |
| `SendGrid__ApiKey` | SendGrid | Yes |
| `SendGrid__FromEmail` | `noreply@onlineportfolio.com.br` | No |
| `SendGrid__FromName` | Your platform name | No |

**Do not set** `ConnectionStrings__Migration` on Render unless you intentionally run migrations from the container (not recommended — use CI instead).

### 5.5 Render checklist

- [ ] Web service created (Docker)
- [ ] Health check returns 200 at `/health`
- [ ] All env vars set
- [ ] Custom domain `api.onlineportfolio.com.br` verified + HTTPS
- [ ] Logs visible in Render dashboard
- [ ] Test: `GET https://api.onlineportfolio.com.br/health`
- [ ] Test: public API endpoint returns data

---

## 6. Vercel (Frontend)

| | |
|---|---|
| **Criar conta** | [vercel.com/signup](https://vercel.com/signup) |
| **Painel** | [vercel.com/dashboard](https://vercel.com/dashboard) |
| **Docs** | [vercel.com/docs](https://vercel.com/docs) |
| **GitHub App** | Import project → conectar repositório GitHub |
| **Domínios** | [vercel.com/docs/projects/domains](https://vercel.com/docs/projects/domains) |

### 6.1 Create project

| Setting | Value |
|---|---|
| **Import** | GitHub repository |
| **Root directory** | `frontend` |
| **Framework preset** | Nuxt.js (auto-detected) |
| **Build command** | `npm run build` (default) |
| **Output** | Nuxt preset (`.output` — Vercel handles) |
| **Install command** | `npm install` |

### 6.2 Environment variables

Set for **Production**, **Preview**, and **Development** as appropriate:

| Variable | Production example | Secret |
|---|---|---|
| `NUXT_PUBLIC_API_BASE` | `/api` (Nuxt proxy) | No |
| `NUXT_PUBLIC_PLATFORM_HOST` | `onlineportfolio.com.br` | No |
| `NUXT_PUBLIC_APP_HOST` | `app.onlineportfolio.com.br` | No |

**Server-only variables** (not prefixed `NUXT_PUBLIC_`) for Nuxt server routes:

| Variable | Purpose |
|---|---|
| `NUXT_API_INTERNAL_BASE` | `https://api.onlineportfolio.com.br` — proxy target to Render |

No Supabase keys on Vercel in v1.

### 6.3 Domains

Go to **Project → Settings → Domains**.

Add:

| Domain | Purpose |
|---|---|
| `onlineportfolio.com.br` | Marketing |
| `www.onlineportfolio.com.br` | Redirect to apex (configure in Vercel) |
| `app.onlineportfolio.com.br` | Admin |
| `*.onlineportfolio.com.br` | Tenant subdomains |

**Per-tenant custom domains** — add manually (Phase 4) or via API later. See [§12](#12-per-tenant-setup-custom-domains).

### 6.4 Vercel project settings

| Setting | Recommendation |
|---|---|
| **Node.js version** | Match `frontend/package.json` engines (e.g. 20.x) |
| **Deployment protection** | Optional password for preview envs |
| **Serverless function region** | Close to Render/Supabase region |

**Nuxt on Vercel:**

- Ensure `nitro` preset is `vercel` (Nuxt 3 default when deployed to Vercel)
- Server routes (`server/api/*`) run as serverless functions — used for API proxy

### 6.5 Vercel checklist

- [ ] Project connected to GitHub
- [ ] Root directory = `frontend`
- [ ] Environment variables set
- [ ] Production deploy succeeds
- [ ] Domains added and SSL active
- [ ] Wildcard subdomain works (`test.onlineportfolio.com.br`)
- [ ] `app.onlineportfolio.com.br` loads admin routes
- [ ] Server proxy reaches Render API (check network tab / logs)

---

## 7. SendGrid (Email)

**Decisão v1:** `noreply@onlineportfolio.com.br` — só envio (contato, convites). Sem caixa postal.

| | |
|---|---|
| **Criar conta** | [signup.sendgrid.com](https://signup.sendgrid.com/) |
| **Painel** | [app.sendgrid.com](https://app.sendgrid.com) |
| **Docs** | [docs.sendgrid.com](https://docs.sendgrid.com/) |
| **API Keys** | Painel → **Settings → API Keys** |
| **Domínio (prod)** | **Settings → Sender Authentication → Authenticate Your Domain** |
| **Pricing** | [sendgrid.com/pricing](https://sendgrid.com/pricing) — 100 emails/dia free |

### 7.0 Escopo v1 (o que entra / o que não entra)

| Incluído | Fora do v1 |
|---|---|
| Envio: formulário de contato | Caixa `noreply@` (não recebe mail) |
| Envio: convite de usuário (accept-invite) | Google Workspace / Zoho |
| Remetente fixo `noreply@onlineportfolio.com.br` | MX no Registro.br para inbox |
| API key só no Render | SendGrid no frontend |

**Reply-To no contato:** email do visitante (artista responde direto) ou conforme regra da API — não depende de inbox `@onlineportfolio.com.br`.

### 7.1 Account setup

- [ ] Create SendGrid account (free tier: **100 emails/day**)
- [ ] Complete account verification

### 7.2 API key

Go to **Settings → API Keys → Create API Key**.

| Setting | Value |
|---|---|
| **Name** | `portfolio-api-prod` |
| **Permissions** | Restricted → **Mail Send** only |

Store in Render as `SendGrid__ApiKey`. **Never** expose in Vercel or frontend.

### 7.3 Identidade do remetente (`noreply@`)

**Produção:** autenticar domínio `onlineportfolio.com.br` (SPF/DKIM na **Vercel DNS**).

**Início rápido (dev/teste):** verificação de remetente único — link no email de confirmação (não cria caixa postal).

**Opção A — Single sender (início rápido):**

- Verificar `noreply@onlineportfolio.com.br` no SendGrid (link por email — use um email pessoal seu para clicar, **não** cria inbox no domínio)
- Bom para testes iniciais

**Opção B — Autenticação de domínio (recomendado para produção):**

Go to **Settings → Sender Authentication → Authenticate Your Domain**.

- [ ] Informar `onlineportfolio.com.br`
- [ ] Adicionar CNAME/TXT na **Vercel DNS** (registros que o SendGrid passar)
- [ ] Verificar domínio no painel SendGrid

| Setting | Valor |
|---|---|
| **From email** | `noreply@onlineportfolio.com.br` |
| **From name** | Online Portfolio |
| **Reply-To** | Dinâmico — email do visitante no contato; convites sem reply esperado |

### 7.4 SendGrid checklist

- [ ] API key created (Mail Send only)
- [ ] Domain authenticated (or single sender verified)
- [ ] DNS records propagated
- [ ] Test email from API contact endpoint
- [ ] Check spam folder / SendGrid activity feed if not received

### 7.5 Local development

- Use separate API key with restricted access, or
- Log emails to console in Development, or
- SendGrid sandbox / only send to verified addresses

---

## 8. Google Workspace (caixa postal operador — futuro)

**Status:** **Fora do v1.** No início só SendGrid `noreply@` (envio). Documentado para quando precisar **ler/responder** em `@onlineportfolio.com.br`.

| | |
|---|---|
| **Criar conta** | [workspace.google.com](https://workspace.google.com/) |
| **Painel admin** | [admin.google.com](https://admin.google.com) |
| **Pricing** | [workspace.google.com/pricing](https://workspace.google.com/pricing) — ~US$ 6–7/usuário/mês |
| **Quando** | Após lançamento, quando precisar inbox `@onlineportfolio.com.br` |

### What it is (vs SendGrid)

| | SendGrid (v1) | Google Workspace (futuro) |
|---|---|---|
| **Função** | App **envia** (`noreply@`, contato, convites) | Você **lê/responde** (`hello@`, `marcelo@`, etc.) |
| **Caixa postal** | **Não** | Sim |
| **Quando** | v1 | Após lançamento (quando precisar inbox) |
| **Custo** | Free tier (100/dia) | ~US$ 6–7/usuário/mês |

Registrar `onlineportfolio.com.br` **agora** já reserva os endereços `@onlineportfolio.com.br`. Google Workspace pode esperar — v1 usa só SendGrid.

### When to configure

- You want daily business email on your domain
- You outgrow forwarding or personal Gmail for support/sales

### Setup steps (when ready)

1. Go to [Google Workspace](https://workspace.google.com/)
2. Choose **Business Starter** (~US$ 6/user/month)
3. Enter domain: `onlineportfolio.com.br`
4. Verify domain ownership (TXT record in Cloudflare DNS)
5. Add **MX records** Google provides (may coexist with SendGrid SPF/DKIM for `noreply@`)
6. Create users: e.g. `hello@onlineportfolio.com.br`, `marcelo@onlineportfolio.com.br`

### DNS coexistence (SendGrid + Google)

Both can work on the same domain:

| Record type | SendGrid | Google Workspace |
|---|---|---|
| **MX** | Not used (SendGrid sends via API, not your MX) | Required for receiving mail |
| **SPF (TXT)** | Include `sendgrid.net` | Include `google.com` — merge into one SPF record |
| **DKIM** | SendGrid CNAMEs | Google CNAMEs |

When adding Google later, **merge SPF** rather than creating duplicate TXT records. Example intent:

```text
v=spf1 include:sendgrid.net include:_spf.google.com ~all
```

### Google Workspace checklist (future)

- [ ] Domínio `onlineportfolio.com.br` já registrado (você)
- [ ] Workspace subscription active
- [ ] Domain verified in Google Admin
- [ ] MX records configured
- [ ] SPF/DKIM updated alongside SendGrid
- [ ] Test send/receive from `hello@onlineportfolio.com.br`

---

## 9. GitHub (CI/CD)

| | |
|---|---|
| **Criar conta** | [github.com/signup](https://github.com/signup) |
| **Novo repositório** | [github.com/new](https://github.com/new) — nome sugerido: `online-portfolio` |
| **Actions secrets** | Repo → **Settings → Secrets and variables → Actions** |
| **Docs Actions** | [docs.github.com/actions](https://docs.github.com/en/actions) |

**Decision:** GitHub Actions is the **pipeline orchestrator** (project preference). Vercel and Render are deploy targets connected to the repo — they do not replace Actions for tests and migrations.

### Do Vercel / Render require repo connection?

| Platform | Must connect GitHub? | Default behavior | Actions alternative |
|---|---|---|---|
| **Vercel** | Recommended, not mandatory | Auto-deploy on push to connected branch | `vercel deploy --prod` with `VERCEL_TOKEN` |
| **Render** | Recommended for Docker builds | Auto-deploy on push | `RENDER_DEPLOY_HOOK_URL` curl from Actions |

**Neither platform forces you to deploy only from their dashboard.** Connect the repo for builds, env vars, and PR previews; use Actions to control **when** deploy is safe (after tests + migrations).

### Recommended setup

```text
GitHub repo
  ├── Vercel GitHub App     → PR previews (prod via Actions)
  ├── Render GitHub App     → API build + deploy (Wait for CI)
  └── GitHub Actions        → CI + deploy separados (backend e frontend)

Pull request:
  ci-backend.yml    → dotnet test (paths backend/**)
  ci-frontend.yml   → npm lint/test (paths frontend/**)
  Revisão           → componentes pareados front↔back (ver §9.7)
  Vercel            → preview URL on PR (native — mantém)

Push to main:
  deploy-backend.yml  → test → migrate (direct :5432) → status check
  Render              → deploy API (Wait for CI — after deploy-backend green)
  deploy-frontend.yml → lint/test → vercel deploy --prod
```

**Decisão:** CI **e deploy separados** — quatro workflows, **somente production** ([ADR-015](./ARCHITECTURE.md#adr-015-deploy-somente-em-production)). Sem Render/Vercel/Supabase “dev” deployados no v1.

**Critical order:** migrations in `deploy-backend.yml` **before** Render deploys. Enable **Wait for CI** on Render.

### 9.1 Repository

- [ ] Code hosted on GitHub
- [ ] Branch protection on `main` (optional but recommended)
- [ ] Vercel connected — PR previews on; **production auto-deploy off**
- [ ] Render connected — **Wait for CI** on `deploy-backend.yml`

### 9.2 GitHub Actions secrets

Go to **Repository → Settings → Secrets and variables → Actions**.

| Secret | Purpose |
|---|---|
| `SUPABASE_MIGRATION_CONNECTION_STRING` | Direct Postgres URI (port 5432) for EF migrations |
| `VERCEL_TOKEN` | `deploy-frontend.yml` — `vercel deploy --prod` |
| `VERCEL_ORG_ID` | Vercel CLI org/team ID |
| `VERCEL_PROJECT_ID` | Vercel project ID (root `frontend`) |
| `RENDER_DEPLOY_HOOK_URL` | Optional — if Render auto-deploy disabled |

**Do not** store service role key in Actions unless a workflow explicitly needs it.

### 9.3 Workflow files (planned)

**Decisão:** CI **e deploy separados** — quatro workflows.

```text
.github/workflows/
  ci-backend.yml       # pull_request + push: dotnet test/build
  ci-frontend.yml      # pull_request + push: npm lint/test
  deploy-backend.yml   # push main: test → EF migrate → Render Wait for CI
  deploy-frontend.yml  # push main: lint/test → vercel deploy --prod
```

| Workflow | Trigger | Paths (filtro) | Jobs |
|---|---|---|---|
| `ci-backend.yml` | `pull_request`, `push` | `backend/**`, `docs/DATABASE.md`, workflows backend | `dotnet test`, build |
| `ci-frontend.yml` | `pull_request`, `push` | `frontend/**`, workflows frontend | `npm run lint`, test |
| `deploy-backend.yml` | `push` → `main` | `backend/**`, migrations | test → `dotnet ef database update` |
| `deploy-frontend.yml` | `push` → `main` | `frontend/**` | lint/test → `vercel deploy --prod` |

**Vercel dashboard:**

| Setting | Valor |
|---|---|
| GitHub App conectado | Sim — **PR previews** |
| Production Branch | `main` |
| **Auto-deploy production** | **Off** (prod via `deploy-frontend.yml`) |
| Root Directory | `frontend` |
| Env vars | Production + Preview no painel Vercel |

### 9.4 Platform dashboard settings (still required)

| Platform | Configure in dashboard |
|---|---|
| **Vercel** | Env vars, domains, root dir `frontend`, PR previews |
| **Render** | Env vars, Dockerfile path, health check `/health`, **Wait for CI** |
| **GitHub** | Secrets, branch protection requiring Actions on `main` |

### 9.5 Example workflow responsibilities

```text
on pull_request:
  ci-backend.yml  → dotnet test (if backend paths changed)
  ci-frontend.yml → npm lint/test (if frontend paths changed)
  reviewer/agent  → cross-stack component checklist (§9.7)

on push to main:
  deploy-backend.yml:
    1. dotnet test
    2. dotnet ef database update (SUPABASE_MIGRATION_CONNECTION_STRING, port 5432)
    3. GitHub commit status → success
  Render → deploy API (Wait for CI)
  deploy-frontend.yml:
    1. npm run lint / test
    2. vercel deploy --prod (VERCEL_TOKEN, VERCEL_ORG_ID, VERCEL_PROJECT_ID)
```

Migrations must use **direct** connection (5432), not pooler.

### 9.6 GitHub checklist

- [ ] Repo created and pushed
- [ ] `ci-backend.yml` and `ci-frontend.yml` with path filters
- [ ] `deploy-backend.yml` and `deploy-frontend.yml` on `main`
- [ ] Migration connection string + Vercel secrets in Actions
- [ ] Vercel **production auto-deploy disabled** (prod via Actions only)
- [ ] Both CI status checks visible on PR
- [ ] Render + Vercel GitHub apps installed with repo access
- [ ] Render **Wait for CI** enabled (gate on `deploy-backend.yml`)
- [ ] Branch protection on `main` requires Backend CI + Frontend CI (recommended)

### 9.7 Git flow — revisão de componentes (front + back)

Além dos testes automatizados, **sempre** conferir atualizações pareadas antes do merge:

| Se alterou… | Backend | Frontend |
|---|---|---|
| Endpoint / DTO / contrato | Controller, service, validação, auth | Proxy `server/api/**`, composable, tipos, UI |
| Schema EF | Migration | Consumo API / formulários |
| Auth / JWT / roles | Identity, policies | Proxy headers, login `app.*` |
| Env var | Render + `backend/.env.example` | Vercel + `frontend/.env.example` |
| Multi-tenant | EF filter + membership | Sem `tenantId` confiável do client |
| Admin vs público | Rotas `[Authorize]` vs public API | Host `app.*` vs `{slug}.*` |

Checklist: CI verde → pareamento front↔back → `.env.example` → `DATABASE.md` se schema → commits convencionais.

Referência completa: [ARCHITECTURE §15](./ARCHITECTURE.md#git-flow--além-dos-testes-revisar-componentes-pareados) · [AGENT_GUIDE](./AGENT_GUIDE.md#git-flow-ci-and-cross-stack-review).

---

## 10. Linear (project management)

Gestão de issues e sprints. Cada `DEV-xxx` do [BACKLOG.md](./BACKLOG.md) vira uma issue no Linear.

| | |
|---|---|
| **Criar conta** | [linear.app/signup](https://linear.app/signup) |
| **Painel** | [linear.app](https://linear.app) |
| **Docs** | [linear.app/docs](https://linear.app/docs) |
| **Import CSV** | Linear → **Settings → Import** (opcional) |
| **Backlog fonte** | [docs/BACKLOG.md](./BACKLOG.md) |
| **Template issue** | [BACKLOG.md § Linear import template](./BACKLOG.md#linear-import-template-copy-per-issue) |

**Custo:** plano **Free** cobre workspace pequeno (issues ilimitadas no free tier atual — confirmar em [linear.app/pricing](https://linear.app/pricing)).

### 10.1 Criar workspace

1. Acesse [linear.app/signup](https://linear.app/signup) — login com GitHub (recomendado) ou Google
2. **Create a workspace** — nome sugerido: `Online Portfolio` ou seu nome pessoal
3. Escolha template **Software development** (ou blank)
4. Convide só você no v1 — sem custo extra

### 10.2 Estrutura sugerida (espelha BACKLOG.md)

| BACKLOG.md | Linear |
|---|---|
| `## Epic 0 — Foundation` etc. | **Project** ou **Initiative** |
| `### DEV-xxx` | **Issue** (título: `DEV-xxx — …`) |
| Campo `Area` | **Label** (`backend`, `frontend`, `infra`, `security`, …) |
| Campo `Priority` (P0–P3) | **Priority** (Urgent / High / Medium / Low) |
| Campo `Phase` | **Cycle** ou **Milestone** |
| `Depends on` | Relação **Blocked by** |
| Acceptance criteria | Checklist na descrição da issue |

**Labels a criar:** `infra` · `backend` · `frontend` · `database` · `devops` · `docs` · `unit-test` · `integration-test` · `security`

### 10.3 Importar backlog (primeira sprint)

**Opção A — Manual (recomendado para Epic 0):**

1. Abra [BACKLOG.md](./BACKLOG.md) → Epic 0
2. Marque **DEV-000** como **Done** (domínio já registrado)
3. Crie issue **DEV-001 — Monorepo scaffold** como próxima (P0)
4. Copie descrição + acceptance criteria do markdown
5. Repita conforme avança — não precisa importar os 50+ issues de uma vez

**Opção B — CSV:**

Colunas: `ID`, `Title`, `Description`, `Priority`, `Labels`, `Epic` — export manual a partir do BACKLOG.

**Opção C — Linear API:** script futuro; ver [developers.linear.app](https://developers.linear.app/docs/graphql/working-with-the-graphql-api).

### 10.4 Integração com GitHub (opcional)

1. Linear → **Settings → Integrations → GitHub**
2. Conecte o repo `online-portfolio`
3. Issues `DEV-xxx` linkam PRs automaticamente se o título/commit mencionar `DEV-001` etc.

### 10.5 Ordem das issues (referência rápida)

Ver [BACKLOG.md § Sprint order](./BACKLOG.md):

```text
Epic 0:  DEV-000 ✅ → DEV-001 → DEV-002 → …
Epic 1.5: login + add user (após scaffold)
Epic 1:  sites públicos por tenant
```

### 10.6 Linear checklist

- [ ] Conta criada em [linear.app](https://linear.app)
- [ ] Workspace criado
- [ ] Labels de `Area` configuradas
- [ ] DEV-000 marcado Done (domínio Registro.br)
- [ ] DEV-001 criado como issue ativa (próximo passo)
- [ ] (Opcional) GitHub integration ligada ao repo

---

## 11. QuestPDF (no external config)

QuestPDF is a **NuGet package** in the .NET API — no external account or API key.

| Item | Action |
|---|---|
| **License** | Set `LicenseType.Community` in code at startup |
| **Eligibility** | Free if org revenue < $1M USD/year |
| **Configuration** | None — optional layout/assets in repo |

See [QuestPDF license](https://www.questpdf.com/license/).

---

## 12. Per-tenant setup (custom domains)

When onboarding an artist with their own domain (Phase 4):

### 12.1 Platform (your side)

1. Add domain in **Vercel → Project → Domains** (e.g. `ana-art.com`)
2. Vercel shows DNS records for artist to configure
3. After verification, store in database:
   - `Tenant.CustomDomain = "ana-art.com"`
   - `Tenant.CustomDomainVerifiedAt = now()`
4. Nuxt middleware resolves tenant from `Host` header

### 12.2 Artist (their side)

At their domain registrar:

| Record | Value |
|---|---|
| `CNAME` `www` | `cname.vercel-dns.com` |
| `A` `@` or ALIAS | Vercel apex records (as shown in dashboard) |

**No configuration needed** on Render or Supabase for tenant custom domains (public site only).

Admin remains at `app.onlineportfolio.com.br` — artist domain is gallery-only.

### 12.3 Subdomain tenants (default)

For `ana.onlineportfolio.com.br`:

- Wildcard DNS `*.onlineportfolio.com.br` → Vercel handles SSL automatically
- Create tenant with `Slug = "ana"` in database — no extra Vercel config per tenant

---

## 13. Future: Stripe (Phase 4)

Not required for initial launch. When adding billing:

| Item | Configuration |
|---|---|
| **Stripe account** | [dashboard.stripe.com](https://dashboard.stripe.com) |
| **Products / prices** | One price per plan (Basic, Pro, etc.) |
| **Webhook** | `https://api.onlineportfolio.com.br/api/v1/webhooks/stripe` on Render |
| **Webhook secret** | Render env `Stripe__WebhookSecret` |
| **API keys** | `Stripe__SecretKey` on Render only |
| **Customer portal** | Enable in Stripe for self-service billing |

---

## 14. Secrets & environment variable map

Where each value is configured and consumed:

| Secret / config | Configured in | Used by |
|---|---|---|
| Supabase pooler connection string | Render | API runtime (EF) |
| Supabase direct connection string | GitHub Actions secret, local `.env` | Migrations only |
| Supabase service role key | Render env | API Storage writes (Phase 3+) |
| `Jwt__Secret` | Render env | API-issued JWT signing |
| SendGrid API key | Render env | API email (invites + contact) |
| SendGrid from email/name | Render env | API email headers |
| `NUXT_PUBLIC_*` vars | Vercel env | Nuxt client + build |
| Database password | Supabase (set at create) | Embedded in connection strings |

### Never commit to git

- All connection strings
- `service_role` key
- `Jwt__Secret`
- SendGrid API key
- Stripe keys (future)

Use `.env.example` with placeholder values in `frontend/` and `backend/`.

---

## 15. Phase checklists

### Phase 0 — Foundation (contas + scaffold)

| Provider | Configure |
|---|---|
| **Registro.br** | ✅ Domínio ativo — DNS no deploy ([§3.2](#32-dns-strategy-at-deploy-time)) |
| **GitHub** | Repo monorepo + push ([§9](#9-github-cicd)) |
| **Linear** | Workspace + DEV-001 issue ([§10](#10-linear-project-management)) |
| **Supabase** | **Só `portfolio-prod`** — dev local via Docker ([ADR-015](./ARCHITECTURE.md#adr-015-deploy-somente-em-production)) |

### Phase 1 — Public portfolio + contact form

| Provider | Configure |
|---|---|
| **Domain DNS** | Nameservers Vercel + `api.` → Render ([§3](#3-domain--dns)) |
| Supabase | Project, DB strings, apply EF migrations |
| Render | API + health check + DB connection + SendGrid |
| SendGrid | API key + sender/domain verification |
| Vercel | Nuxt deploy + API URL env var + platform domains |
| DNS | `api.`, apex, `app.`, wildcard → correct targets |
| GitHub | Repo, CI migrations |

### Phase 1.5 — Admin login + add user

| Provider | Configure |
|---|---|
| Render | Identity + JWT env vars; auth endpoints |
| SendGrid | Invite email templates |
| Vercel | BFF proxy to API; `app.` domain |

### Phase 2 — Admin features (post-login)

| Provider | Configure |
|---|---|
| Render | Artwork CRUD endpoints |
| Vercel | Admin UI pages via proxy |

### Phase 3 — Image uploads

| Provider | Configure |
|---|---|
| Supabase | Storage buckets; public read |
| Vercel | Proxy upload routes |
| Render | Service role for Storage writes |

### Phase 4 — Custom domains + billing

| Provider | Configure |
|---|---|
| Vercel | Per-tenant domains (manual or Domains API) |
| Stripe | Products, webhooks, API keys on Render |
| DNS | Artist-owned domains → Vercel |
| Google Workspace | Operator inbox at `@onlineportfolio.com.br` (optional) |

---

## 16. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| API 500 on DB connect | Wrong connection string or pooler mode | Use transaction pooler on 6543; check password and SSL |
| EF migrations fail | Using pooler for migrations | Use direct connection on port 5432 |
| Login 401 | Wrong password or inactive user | Check Identity seed; verify proxy forwards body |
| CORS error browser → API | Direct browser call to Render | Use Nuxt `/api` proxy only |
| SendGrid emails not arriving | Domain not authenticated | Complete DKIM/SPF; check SendGrid activity |
| Cold API first request slow | Render free tier sleep | Expected; upgrade or accept for admin use |
| Wildcard subdomain SSL fail | DNS wildcard missing | Add `*.onlineportfolio.com.br` CNAME; wait for propagation |
| Storage upload fails | API path/tenant validation | Check multipart proxy + service role key |
| JWT invalid in API | Wrong `Jwt__Secret` or expired token | Verify Render env matches API config |

---

## Quick reference — URLs to bookmark

| Service | Criar conta | Painel | Docs |
|---|---|---|---|
| **Registro.br** | [registro.br](https://registro.br) | [Login](https://registro.br/login/) | [Ajuda](https://registro.br/ajuda) |
| **GitHub** | [Signup](https://github.com/signup) | [github.com](https://github.com) | [Actions docs](https://docs.github.com/en/actions) |
| **Linear** | [Signup](https://linear.app/signup) | [linear.app](https://linear.app) | [Docs](https://linear.app/docs) |
| **Supabase** | [Dashboard](https://supabase.com/dashboard) | [Dashboard](https://supabase.com/dashboard) | [Docs](https://supabase.com/docs) |
| **Render** | [Register](https://dashboard.render.com/register) | [Dashboard](https://dashboard.render.com) | [Custom domains](https://render.com/docs/custom-domains) |
| **Vercel** | [Signup](https://vercel.com/signup) | [Dashboard](https://vercel.com/dashboard) | [Domains](https://vercel.com/docs/projects/domains) |
| **SendGrid** | [Signup](https://signup.sendgrid.com/) | [App](https://app.sendgrid.com) | [Sender auth](https://docs.sendgrid.com/ui/account-and-settings/how-to-set-up-domain-authentication) |
| **Google Workspace** | [workspace.google.com](https://workspace.google.com/) | [Admin](https://admin.google.com) | — |
| **Stripe** (Fase 4) | [Register](https://dashboard.stripe.com/register) | [Dashboard](https://dashboard.stripe.com) | [Webhooks](https://docs.stripe.com/webhooks) |
| **QuestPDF** | — | — | [License](https://www.questpdf.com/license/) |
| **BACKLOG (issues)** | — | — | [BACKLOG.md](./BACKLOG.md) |
