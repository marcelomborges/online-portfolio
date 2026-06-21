# External Provider Configuration Guide

Step-by-step configuration for every third-party service required to run the artist portfolio platform in production.

**Related:** [ARCHITECTURE.md](./ARCHITECTURE.md) — system design and decisions  
**Platform domain:** `onlineportfolio.com.br` (`onlineportfolio.com` unavailable)  
**Email v1:** SendGrid com `noreply@onlineportfolio.com.br` — **só envio transacional**, sem caixa postal. Google Workspace (inbox) — **depois**.

**Operator email:** Google Workspace (pago) — **fora do v1**; registro do domínio reserva `@onlineportfolio.com.br` para o futuro

---

## Table of contents

1. [Overview](#1-overview)
2. [Recommended setup order](#2-recommended-setup-order)
3. [Domain & DNS](#3-domain--dns)
4. [Supabase](#4-supabase)
5. [Render (API)](#5-render-api)
6. [Vercel (Frontend)](#6-vercel-frontend)
7. [SendGrid (Email)](#7-sendgrid-email)
8. [Google Workspace (caixa postal operador — futuro)](#8-google-workspace-caixa-postal-operador--futuro)
9. [GitHub (CI/CD)](#9-github-cicd)
10. [QuestPDF (no external config)](#10-questpdf-no-external-config)
11. [Per-tenant setup (custom domains)](#11-per-tenant-setup-custom-domains)
12. [Future: Stripe (Phase 4)](#12-future-stripe-phase-4)
13. [Secrets & environment variable map](#13-secrets--environment-variable-map)
14. [Phase checklists](#14-phase-checklists)
15. [Troubleshooting](#15-troubleshooting)

---

## 1. Overview

### Services involved

| Provider | Purpose | Accounts needed |
|---|---|---|
| **Domain registrar** | Own `onlineportfolio.com.br` | 1 — [Registro.br](https://registro.br) |
| **Supabase** | PostgreSQL, Storage | 1 prod project (+ optional dev project) |
| **Render** | ASP.NET Core API (Docker) | 1 web service |
| **Vercel** | Nuxt frontend | 1 project |
| **SendGrid** | Email transacional (`noreply@`) — contato + convites | 1 conta; **sem inbox** |
| **Google Workspace** | Caixa postal operador (futuro) | Fora do v1 — ver [§8](#8-google-workspace-caixa-postal-operador--futuro) |
| **GitHub** | Source repo + CI | 1 repo |
| **QuestPDF** | PDF generation | No account (NuGet license only) |

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
1. Registro.br          → registrar onlineportfolio.com.br (R$ 40/ano)
2. Supabase             → projeto, connection strings, keys (Postgres + Storage)
3. GitHub               → repo conectado
4. Render               → API deployada, health check ok
5. DNS                  → api.onlineportfolio.com.br → Render
6. SendGrid             → API key + verificação de domínio em onlineportfolio.com.br
7. Vercel               → Nuxt deploy, proxy BFF, domínios platform + app + wildcard
8. DNS                  → apex, app., *. → Vercel; api. → Render
9. SendGrid             → emails de convite + formulário de contato (API)
10. GitHub Actions     → secrets de migration, workflow de deploy
11. Por tenant          → domínios custom no Vercel (Fase 4)
12. Google Workspace    → inbox operador (futuro — não v1)
```

---

## 3. Domain & DNS

### 3.1 Register `onlineportfolio.com.br` (Registro.br)

**Decision:** Primary platform domain is **`onlineportfolio.com.br`**.

| Check | Result |
|---|---|
| `onlineportfolio.com` | **Unavailable** — already registered by someone else |
| `onlineportfolio.com.br` | **Available** — register at Registro.br |

#### Why `.com.br` is fine for this project

- Your first two artists are in Brazil — `.com.br` is trusted locally
- Vercel, Render, Supabase, SendGrid, and Google Workspace all support `.com.br` custom domains
- Same architecture: `app.`, `api.`, `{slug}.` subdomains work identically
- Fixed price: **R$ 40/year** with no renewal surprises
- Payment: Pix, boleto, or card — no international card required

**Trade-off:** Slightly less “global SaaS” feel than `.com` for international customers. Revisit a `.com` alternative later if you expand abroad (e.g. buy a variant on Cloudflare when available).

#### Steps (Registro.br)

1. Go to [registro.br](https://registro.br)
2. Search `onlineportfolio.com.br` → confirm availability
3. Register with **CPF** (person) or **CNPJ** (company)
4. Pay **R$ 40/year**
5. In the Registro.br panel, configure DNS — see [§3.2](#32-dns-records-for-vercel) and [§3.3](#33-records-for-render-api)

**DNS options:**

| Option | When to use |
|---|---|
| **DNS at Registro.br** | Simplest — edit records in the Registro.br panel |
| **Cloudflare DNS (free)** | Add site to Cloudflare, change nameservers at Registro.br to Cloudflare — better tooling for wildcards and DNS management |

Registro.br does **not** use Cloudflare Registrar for `.com.br` — registration is **only** via Registro.br.

#### What registration gives you

- `onlineportfolio.com.br` for Vercel / Render
- **Reservation** of all `@onlineportfolio.com.br` email addresses
- No Google Workspace payment needed until you want a real inbox

#### Purchase now, configure DNS later

You can register at Registro.br and leave default DNS until the app is ready to deploy. No hosting or nameservers required at purchase time.

| When | Action |
|---|---|
| **Now** | Register + pay → confirm **Ativo** |
| **At deploy** | Nameservers Vercel or DNS records → Vercel + Render |

#### Registro.br + Vercel nameservers (ownership stays at Registro.br)

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

Dashboard: [https://supabase.com/dashboard](https://supabase.com/dashboard)

### 4.1 Create project

| Setting | Recommendation |
|---|---|
| **Organization** | Your org |
| **Project name** | `portfolio-prod` (and `portfolio-dev` optional) |
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

**Optional — separate dev project:**

Create a second Supabase project for staging/dev with the same configuration pattern.

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

Dashboard: [https://dashboard.render.com](https://dashboard.render.com)

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
2. Add CNAME at DNS provider (see [§3.3](#33-records-for-render-api))
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

Dashboard: [https://vercel.com/dashboard](https://vercel.com/dashboard)

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

**Per-tenant custom domains** — add manually (Phase 4) or via API later. See [§10](#10-per-tenant-setup-custom-domains).

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

**Decisão v1:** usar **`noreply@onlineportfolio.com.br`** via SendGrid para **enviar** emails necessários (contato, convites). **Sem caixa postal** — não configurar MX para receber, Registro.br só registra o domínio.

Dashboard: [https://app.sendgrid.com](https://app.sendgrid.com)

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
  ├── Vercel GitHub App     → builds frontend, PR previews, prod deploy
  ├── Render GitHub App     → builds Docker API, prod deploy
  └── GitHub Actions        → tests + EF migrations (orchestrator)

Pull request:
  Actions  → test backend + frontend
  Vercel   → preview URL on PR (native)

Push to main:
  Actions  → test → migrate Supabase (direct :5432) → pass status check
  Render   → deploy API (enable "Wait for CI" — deploys after Actions green)
  Vercel   → deploy frontend (auto on push, or after required checks)
```

**Critical order:** migrations run in Actions **before** Render deploys. Enable **Wait for CI** on the Render service.

### 9.1 Repository

- [ ] Code hosted on GitHub
- [ ] Branch protection on `main` (optional but recommended)
- [ ] Render and Vercel connected for auto-deploy on push

### 9.2 GitHub Actions secrets

Go to **Repository → Settings → Secrets and variables → Actions**.

| Secret | Purpose |
|---|---|
| `SUPABASE_MIGRATION_CONNECTION_STRING` | Direct Postgres URI (port 5432) for EF migrations |
| `RENDER_DEPLOY_HOOK_URL` | Optional — only if Render auto-deploy is disabled |
| `VERCEL_TOKEN` | Optional — only if deploying frontend from Actions (Option C) |

**Do not** store service role key in Actions unless a workflow explicitly needs it.

### 9.3 Workflow files (planned)

```text
.github/workflows/
  ci.yml           # on pull_request: dotnet test, npm test/lint
  deploy-prod.yml  # on push to main: test → migrate → status check
```

### 9.4 Platform dashboard settings (still required)

| Platform | Configure in dashboard |
|---|---|
| **Vercel** | Env vars, domains, root dir `frontend`, PR previews |
| **Render** | Env vars, Dockerfile path, health check `/health`, **Wait for CI** |
| **GitHub** | Secrets, branch protection requiring Actions on `main` |

### 9.5 Example workflow responsibilities

```text
on pull_request:
  1. dotnet test (backend)
  2. npm test / lint (frontend)

on push to main:
  1. dotnet test
  2. npm test / lint
  3. dotnet ef database update (SUPABASE_MIGRATION_CONNECTION_STRING, port 5432)
  4. GitHub commit status → success
  5. Render deploys (Wait for CI) + Vercel deploys frontend
```

Migrations must use **direct** connection (5432), not pooler.

### 9.6 GitHub checklist

- [ ] Repo created and pushed
- [ ] Migration connection string in Actions secrets
- [ ] CI workflow runs on PR/push
- [ ] Render + Vercel GitHub apps installed with repo access
- [ ] Render **Wait for CI** enabled
- [ ] Branch protection on `main` requires Actions checks (recommended)

---

## 10. QuestPDF (no external config)

QuestPDF is a **NuGet package** in the .NET API — no external account or API key.

| Item | Action |
|---|---|
| **License** | Set `LicenseType.Community` in code at startup |
| **Eligibility** | Free if org revenue < $1M USD/year |
| **Configuration** | None — optional layout/assets in repo |

See [QuestPDF license](https://www.questpdf.com/license/).

---

## 11. Per-tenant setup (custom domains)

When onboarding an artist with their own domain (Phase 4):

### 11.1 Platform (your side)

1. Add domain in **Vercel → Project → Domains** (e.g. `ana-art.com`)
2. Vercel shows DNS records for artist to configure
3. After verification, store in database:
   - `Tenant.CustomDomain = "ana-art.com"`
   - `Tenant.CustomDomainVerifiedAt = now()`
4. Nuxt middleware resolves tenant from `Host` header

### 11.2 Artist (their side)

At their domain registrar:

| Record | Value |
|---|---|
| `CNAME` `www` | `cname.vercel-dns.com` |
| `A` `@` or ALIAS | Vercel apex records (as shown in dashboard) |

**No configuration needed** on Render or Supabase for tenant custom domains (public site only).

Admin remains at `app.onlineportfolio.com.br` — artist domain is gallery-only.

### 11.3 Subdomain tenants (default)

For `ana.onlineportfolio.com.br`:

- Wildcard DNS `*.onlineportfolio.com.br` → Vercel handles SSL automatically
- Create tenant with `Slug = "ana"` in database — no extra Vercel config per tenant

---

## 12. Future: Stripe (Phase 4)

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

## 13. Secrets & environment variable map

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

## 14. Phase checklists

### Phase 1 — Public portfolio + contact form

| Provider | Configure |
|---|---|
| **Domain** | Register `onlineportfolio.com.br` at Registro.br (R$ 40/year) |
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

## 15. Troubleshooting

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

| Service | URL |
|---|---|
| Supabase dashboard | https://supabase.com/dashboard |
| Render dashboard | https://dashboard.render.com |
| Vercel dashboard | https://vercel.com/dashboard |
| SendGrid dashboard | https://app.sendgrid.com |
| Supabase docs — Auth | https://supabase.com/docs/guides/auth |
| Supabase docs — Storage | https://supabase.com/docs/guides/storage |
| Vercel docs — domains | https://vercel.com/docs/projects/domains |
| Render docs — custom domains | https://render.com/docs/custom-domains |
| Google Admin | https://admin.google.com |
| Cloudflare Registrar | https://www.cloudflare.com/products/registrar/ |
| Registro.br | https://registro.br |
