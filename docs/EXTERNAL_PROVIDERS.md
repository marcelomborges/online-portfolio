# External Provider Configuration Guide

Step-by-step configuration for every third-party service required to run the artist portfolio platform in production.

**Related:** [ARCHITECTURE.md](./ARCHITECTURE.md) · [BACKLOG.md](./BACKLOG.md)  
**Platform domain:** `onlineportfolio.com.br` — **registrado** no [Registro.br](https://registro.br)  
**Email v1:** SendGrid `noreply@onlineportfolio.com.br` — só envio transacional, sem caixa postal  
**Project management:** [Linear](https://linear.app) — issues a partir do BACKLOG (`DEV-xxx`)  
**Operator inbox (futuro):** Google Workspace — depois do lançamento  
**Cobrança / billing:** **opcional** — v1 **sem cobrança**; tenants provisionados manualmente pelo operador

---

## Table of contents

**Ordem de leitura = ordem de setup.** Fase 4 e serviços futuros no final.

1. [Overview](#1-overview)
2. [Ordem de setup](#2-ordem-de-setup)
3. [Registro.br — domínio](#3-registrobr--domínio)
4. [GitHub & CI/CD](#4-github-cicd--actions)
5. [Linear](#5-linear-project-management)
6. [Supabase](#6-supabase)
7. [Render (API)](#7-render-api)
8. [SendGrid (Email)](#8-sendgrid-email)
9. [Vercel (Frontend)](#9-vercel-frontend)
10. [DNS no deploy](#10-dns-no-deploy)
11. [QuestPDF (sem conta)](#11-questpdf-sem-conta-externa)
12. [Domínios custom por tenant (Fase 4)](#12-domínios-custom-por-tenant-fase-4)
13. [Stripe — cobrança SaaS (opcional)](#13-stripe--cobrança-saas-fase-4-opcional)
14. [Google Workspace (futuro)](#14-google-workspace-caixa-postal-operador--futuro)
15. [Secrets & environment map](#15-secrets--environment-variable-map)
16. [Phase checklists](#16-phase-checklists)
17. [Troubleshooting](#17-troubleshooting)
18. [Quick reference — URLs](#quick-reference--urls-to-bookmark)

---

## 1. Overview

### Services involved

| Provider | Purpose | Contas | Guia |
|---|---|---|---|
| **Registro.br** | Domínio `onlineportfolio.com.br` | ✅ 1 (feito) | [§3](#3-registrobr--domínio) |
| **GitHub** | Repo monorepo + CI/CD | 1 repo | [§4](#4-github-cicd--actions) |
| **Linear** | Issues, sprints (`DEV-xxx`) | 1 workspace | [§5](#5-linear-project-management) |
| **Supabase** | PostgreSQL, Storage | 1 prod | [§6](#6-supabase) |
| **Render** | ASP.NET Core API (Docker) | 1 web service | [§7](#7-render-api) |
| **SendGrid** | Email transacional (`noreply@`) | 1 conta | [§8](#8-sendgrid-email) |
| **Vercel** | Nuxt frontend | 1 project | [§9](#9-vercel-frontend) |
| **Stripe** | Cobrança SaaS (**opcional**, futuro) | ⏸️ não usar agora | [§13](#13-stripe--cobrança-saas-fase-4-opcional) |
| **Google Workspace** | Inbox operador (futuro) | ⏸️ pós-lançamento | [§14](#14-google-workspace-caixa-postal-operador--futuro) |
| **QuestPDF** | PDF (NuGet) | Sem conta | [§11](#11-questpdf-sem-conta-externa) |

### 1.1 Master checklist — criar contas

Use esta tabela para abrir cada serviço na ordem. Marque conforme for concluindo.

| # | Provider | Criar conta | Painel | Doc deste repo | Status |
|---|---|---|---|---|---|
| 1 | **Registro.br** | [registro.br](https://registro.br) | [Painel NIC](https://registro.br/login/) | [§3.1](#31-domínio-onlineportfoliocombr) | ✅ **Feito** — domínio comprado |
| 2 | **GitHub** | [github.com/signup](https://github.com/signup) | [github.com](https://github.com) | [§4](#4-github-cicd--actions) | ⬜ Criar repo `online-portfolio` |
| 3 | **Linear** | [linear.app/signup](https://linear.app/signup) | [linear.app](https://linear.app) | [§5](#5-linear-project-management) | ✅ Backlog importado |
| 4 | **Supabase** | [supabase.com/dashboard](https://supabase.com/dashboard) | [Dashboard](https://supabase.com/dashboard) | [§6](#6-supabase) | ⬜ Projeto `portfolio-prod` |
| 5 | **Render** | [dashboard.render.com/register](https://dashboard.render.com/register) | [Render](https://dashboard.render.com) | [§7](#7-render-api) | ⬜ Web service API |
| 6 | **SendGrid** | [signup.sendgrid.com](https://signup.sendgrid.com/) | [SendGrid](https://app.sendgrid.com) | [§8](#8-sendgrid-email) | ⬜ API key (domínio no deploy §10) |
| 7 | **Vercel** | [vercel.com/signup](https://vercel.com/signup) | [Vercel](https://vercel.com/dashboard) | [§9](#9-vercel-frontend) | ⬜ Projeto Nuxt |
| 8 | **Google Workspace** | [workspace.google.com](https://workspace.google.com/) | [Admin](https://admin.google.com) | [§14](#14-google-workspace-caixa-postal-operador--futuro) | ⏸️ Depois do lançamento |
| 9 | **Stripe** | [dashboard.stripe.com/register](https://dashboard.stripe.com/register) | [Stripe](https://dashboard.stripe.com) | [§13](#13-stripe--cobrança-saas-fase-4-opcional) | ⏸️ **Opcional** — só se/quando cobrar |

**O que guardar em password manager / GitHub Secrets:** ver [§15](#15-secrets--environment-variable-map).

**Ordem recomendada:** [§2](#2-ordem-de-setup).

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

## 2. Ordem de setup

Siga esta ordem para evitar dependências circulares. As seções **§3–§9** seguem a mesma sequência.

| Passo | Provider | Ação | Seção |
|---|---|---|---|
| 1 | **Registro.br** | ✅ Domínio registrado | [§3](#3-registrobr--domínio) |
| 2 | **GitHub** | Repo monorepo + push | [§4](#4-github-cicd--actions) |
| 3 | **Linear** | Workspace + BACKLOG (pode ser cedo) | [§5](#5-linear-project-management) |
| 4 | **Supabase** | `portfolio-prod`, connection strings | [§6](#6-supabase) |
| 5 | **Render** | Conta + web service (após DEV-001) | [§7](#7-render-api) |
| 6 | **SendGrid** | Conta + API key | [§8](#8-sendgrid-email) |
| 7 | **Vercel** | Conta + projeto Nuxt (após DEV-001) | [§9](#9-vercel-frontend) |
| 8 | **DNS** | NS Vercel, `api.`, DKIM SendGrid | [§10](#10-dns-no-deploy) |
| 9 | **GitHub Actions** | Workflows + secrets | [§4.3](#43-workflow-files-planned) |
| — | **Por tenant** | Domínio custom do artista | [§12](#12-domínios-custom-por-tenant-fase-4) |
| — | **Stripe** | Cobrança — **opcional, não agora** | [§13](#13-stripe--cobrança-saas-fase-4-opcional) |
| — | **Google Workspace** | Inbox operador — **futuro** | [§14](#14-google-workspace-caixa-postal-operador--futuro) |

**Deploy na nuvem:** somente **production** ([ADR-015](./ARCHITECTURE.md#adr-015-deploy-somente-em-production)).

**Issues no backlog:** cada passo acima tem issue `DEV-xxx` — ver [BACKLOG § Provider setup index](./BACKLOG.md#provider-setup-index).

---

## 3. Registro.br — domínio

### 3.1 Domínio `onlineportfolio.com.br`

**Status:** ✅ **Registrado** — titular ativo no Registro.br. DNS de produção → [§10](#10-dns-no-deploy) (após Render + Vercel).

| Item | Valor |
|---|---|
| **Domínio** | `onlineportfolio.com.br` |
| **Registrar** | [Registro.br](https://registro.br) — único registrador oficial `.br` |
| **Painel** | [registro.br/login](https://registro.br/login/) |
| **Renovação** | ~R$ 40/ano (Pix, boleto ou cartão) |
| **Titular** | Seu CPF/CNPJ — não transfere para Vercel |
| **DNS agora** | Pode manter DNS padrão Registro.br até deploy |
| **DNS no deploy** | Nameservers Vercel — [§10](#10-dns-no-deploy) |

| Check | Result |
|---|---|
| `onlineportfolio.com` | **Indisponível** — registrado por terceiros |
| `onlineportfolio.com.br` | ✅ **Seu** — registrado |

#### Verificar no painel Registro.br

- [x] Domínio `onlineportfolio.com.br` com status **Ativo** / **Publicado**
- [ ] Anotar data de expiração / renovação automática
- [ ] Guardar login Registro.br no password manager
- [ ] DNS: deixar padrão **ou** apontar NS Vercel no deploy ([§10](#10-dns-no-deploy))

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
| **Deploy** | Nameservers Vercel + domínios ([§10](#10-dns-no-deploy)) |
| **Pós-deploy** | SendGrid DKIM na Vercel DNS ([§10.4](#104-dns-de-email--sendgrid-v1-só-envio)) |

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

---

## 4. GitHub (CI/CD) & Actions

| | |
|---|---|
| **Criar conta** | [github.com/signup](https://github.com/signup) |
| **Novo repositório** | [github.com/new](https://github.com/new) — nome sugerido: `online-portfolio` |
| **Actions secrets** | Repo → **Settings → Secrets and variables → Actions** |
| **Docs Actions** | [docs.github.com/actions](https://docs.github.com/en/actions) |

**Decision:** GitHub Actions is the **pipeline orchestrator**. Vercel and Render are deploy targets — not a substitute for tests and migrations.

### 4.1 Repository

- [ ] Code hosted on GitHub
- [ ] Branch protection on `main` (recommended)
- [ ] Vercel connected — PR previews on; **production auto-deploy off**
- [ ] Render connected — **Wait for CI** on `deploy-backend.yml`

### 4.2 GitHub Actions secrets

| Secret | Purpose |
|---|---|
| `SUPABASE_MIGRATION_CONNECTION_STRING` | Direct Postgres URI (port 5432) for EF migrations |
| `VERCEL_TOKEN` | `deploy-frontend.yml` — `vercel deploy --prod` |
| `VERCEL_ORG_ID` | Vercel CLI org/team ID |
| `VERCEL_PROJECT_ID` | Vercel project ID (root `frontend`) |
| `RENDER_DEPLOY_HOOK_URL` | Optional — if Render auto-deploy disabled |

### 4.3 Workflow files (planned)

```text
.github/workflows/
  ci-backend.yml       # pull_request + push: dotnet test (xUnit/Moq/FluentAssertions/Coverlet) + build
  ci-frontend.yml      # pull_request + push: npm lint/test
  deploy-backend.yml   # push main: test → EF migrate → Render Wait for CI
  deploy-frontend.yml  # push main: lint/test → vercel deploy --prod
```

**Decisão:** CI **e deploy separados**; **somente production** ([ADR-015](./ARCHITECTURE.md#adr-015-deploy-somente-em-production)).

### 4.4 Git flow — revisão de componentes (front + back)

Além dos testes, conferir pareamento front↔back antes do merge. Referência: [ARCHITECTURE §15](./ARCHITECTURE.md#git-flow--além-dos-testes-revisar-componentes-pareados) · [AGENT_GUIDE](./AGENT_GUIDE.md#git-flow-ci-and-cross-stack-review).

### 4.5 GitHub checklist

- [ ] Repo created and pushed
- [ ] `ci-backend.yml`, `ci-frontend.yml`, `deploy-backend.yml`, `deploy-frontend.yml`
- [ ] Secrets configured; Render **Wait for CI** enabled

---

## 5. Linear (project management)

Gestão de issues e sprints. Cada `DEV-xxx` do [BACKLOG.md](./BACKLOG.md) vira uma issue no Linear.

| | |
|---|---|
| **Criar conta** | [linear.app/signup](https://linear.app/signup) |
| **Painel** | [linear.app](https://linear.app) |
| **Docs** | [linear.app/docs](https://linear.app/docs) |
| **Import CSV** | Linear → **Settings → Import** (opcional) |
| **Backlog fonte** | [docs/BACKLOG.md](./BACKLOG.md) |

**Custo:** plano **Free** cobre workspace pequeno — confirmar em [linear.app/pricing](https://linear.app/pricing).

### 5.1 Criar workspace

1. Acesse [linear.app/signup](https://linear.app/signup) — login com GitHub (recomendado) ou Google
2. **Create a workspace** — nome sugerido: `Online Portfolio`
3. Escolha template **Software development** (ou blank)

### 5.2 Estrutura sugerida (espelha BACKLOG.md)

| BACKLOG.md | Linear |
|---|---|
| `## Epic 0 — Foundation` etc. | **Project** ou **Initiative** |
| `### DEV-xxx` | **Issue** (título: `DEV-xxx — …`) |
| Campo `Area` | **Label** (`backend`, `frontend`, `infra`, …) |
| Campo `Priority` (P0–P3) | **Priority** (Urgent / High / Medium / Low) |
| `Depends on` | Relação **Blocked by** |
| Acceptance criteria | Checklist na descrição da issue |

**Labels a criar:** `infra` · `backend` · `frontend` · `database` · `devops` · `docs` · `security`

### 5.3 Importar backlog (Epic 0)

**Manual (recomendado):** DEV-000 ✅ Done → criar **DEV-001** como próxima issue; copiar acceptance criteria do BACKLOG.

**CSV:** colunas `ID`, `Title`, `Description`, `Priority`, `Labels`, `Epic`.

### 5.4 Integração GitHub (opcional)

Linear → **Settings → Integrations → GitHub** → repo `online-portfolio`. PRs linkam se título/commit mencionar `DEV-001`.

### 5.5 Linear checklist

- [ ] Workspace criado
- [ ] Labels de `Area` configuradas
- [ ] DEV-000 Done; DEV-001 ativo
- [ ] (Opcional) GitHub integration

---

## 6. Supabase

| | |
|---|---|
| **Criar conta** | [supabase.com/dashboard](https://supabase.com/dashboard) (GitHub ou email) |
| **Painel** | [supabase.com/dashboard](https://supabase.com/dashboard) |
| **Docs** | [supabase.com/docs](https://supabase.com/docs) |
| **Pricing** | [supabase.com/pricing](https://supabase.com/pricing) — free tier ok para v1 |

### 6.1 Create project

| Setting | Recommendation |
|---|---|
| **Organization** | Your org |
| **Project name** | `portfolio-prod` — **único projeto Supabase no v1** ([ADR-015](./ARCHITECTURE.md#adr-015-deploy-somente-em-production)) |
| **Region** | Closest to most users (e.g. EU West if artists in Europe) |
| **Database password** | Strong password — store in password manager |

Wait for project provisioning (~2 minutes).

### 6.2 Database — connection strings for EF Core

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

### 6.3 API keys

Go to **Project Settings → API**.

| Key | Where it goes | Never put in |
|---|---|---|
| **Project URL** | `https://[ref].supabase.co` → Render `Supabase__Url` (Storage, Phase 3+) | — |
| **service_role** | Render `Supabase__ServiceRoleKey` only | Frontend, git, browser |

The **service role** bypasses Storage policies — treat like a root password. **Anon key is not used in v1** (no client-side Supabase SDK).

### 6.4 Autenticação — **não usa Supabase Auth**

Login admin usa **ASP.NET Identity + JWT** na API .NET. Supabase fornece **Postgres + Storage** apenas.

| Concern | Onde |
|---|---|
| Senhas | Postgres via EF Identity |
| Assinatura JWT | Render: `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` |
| UI de login | `app.onlineportfolio.com.br/login` → proxy Nuxt → API |
| Emails de convite | SendGrid (API envia no convite) |

Nenhuma configuração do dashboard Supabase Auth no v1.

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

## 7. Render (API)

| | |
|---|---|
| **Criar conta** | [dashboard.render.com/register](https://dashboard.render.com/register) |
| **Painel** | [dashboard.render.com](https://dashboard.render.com) |
| **Docs** | [render.com/docs](https://render.com/docs) |
| **GitHub App** | [Render → Account Settings → GitHub](https://dashboard.render.com) (conectar após login) |
| **Pricing** | [render.com/pricing](https://render.com/pricing) — free tier com cold start |

### 7.1 Create web service

| Setting | Value |
|---|---|
| **Type** | Web Service |
| **Source** | Connect GitHub repo |
| **Root directory** | `backend` (or repo root if Dockerfile path set) |
| **Runtime** | Docker |
| **Dockerfile path** | `backend/OnlinePortfolio.Api/Dockerfile` |
| **Branch** | `main` |
| **Region** | Same as Supabase when possible |
| **Instance type** | Free |

### 7.2 Service configuration

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

### 7.3 Custom domain

1. **Settings → Custom Domains → Add** `api.onlineportfolio.com.br`
2. Add CNAME at DNS provider (see [§10.3](#103-records-for-render-api))
3. Wait for SSL certificate provisioning

### 7.4 Environment variables

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

### 7.5 Render checklist

- [ ] Web service created (Docker)
- [ ] Health check returns 200 at `/health`
- [ ] All env vars set
- [ ] Custom domain `api.onlineportfolio.com.br` verified + HTTPS
- [ ] Logs visible in Render dashboard
- [ ] Test: `GET https://api.onlineportfolio.com.br/health`
- [ ] Test: public API endpoint returns data

---

## 8. SendGrid (Email)

**Decisão v1:** `noreply@onlineportfolio.com.br` — só envio (contato, convites). Sem caixa postal.

| | |
|---|---|
| **Criar conta** | [signup.sendgrid.com](https://signup.sendgrid.com/) |
| **Painel** | [app.sendgrid.com](https://app.sendgrid.com) |
| **Docs** | [docs.sendgrid.com](https://docs.sendgrid.com/) |
| **API Keys** | Painel → **Settings → API Keys** |
| **Domínio (prod)** | **Settings → Sender Authentication** — registros DNS em [§10.4](#104-dns-de-email--sendgrid-v1-só-envio) |
| **Pricing** | [sendgrid.com/pricing](https://sendgrid.com/pricing) — 100 emails/dia free |

### 8.1 Escopo v1

| Incluído | Fora do v1 |
|---|---|
| Formulário de contato, convites | Caixa postal / MX |
| `noreply@onlineportfolio.com.br` | SendGrid no frontend |

### 8.2 API key

- [ ] Conta SendGrid + verificação
- [ ] API key `portfolio-api-prod` — permissão **Mail Send** only
- [ ] Guardar no Render como `SendGrid__ApiKey`

### 8.3 Remetente (`noreply@`)

- **Dev:** single sender verification (rápido)
- **Prod:** autenticar domínio `onlineportfolio.com.br` — DNS em [§10.4](#104-dns-de-email--sendgrid-v1-só-envio)

### 8.4 SendGrid checklist

- [ ] API key no Render
- [ ] Domínio autenticado (prod)
- [ ] Teste de envio via API

---

## 9. Vercel (Frontend)

| | |
|---|---|
| **Criar conta** | [vercel.com/signup](https://vercel.com/signup) |
| **Painel** | [vercel.com/dashboard](https://vercel.com/dashboard) |
| **Docs** | [vercel.com/docs](https://vercel.com/docs) |
| **GitHub App** | Import project → conectar repositório GitHub |
| **Domínios** | [vercel.com/docs/projects/domains](https://vercel.com/docs/projects/domains) |

### 9.1 Create project

| Setting | Value |
|---|---|
| **Import** | GitHub repository |
| **Root directory** | `frontend` |
| **Framework preset** | Nuxt.js (auto-detected) |
| **Production auto-deploy** | **Off** — prod via `deploy-frontend.yml` ([§4.3](#43-workflow-files-planned)) |

### 9.2 Environment variables

| Variable | Production example | Secret |
|---|---|---|
| `NUXT_PUBLIC_API_BASE` | `/api` (Nuxt proxy) | No |
| `NUXT_PUBLIC_PLATFORM_HOST` | `onlineportfolio.com.br` | No |
| `NUXT_PUBLIC_APP_HOST` | `app.onlineportfolio.com.br` | No |
| `NUXT_API_INTERNAL_BASE` | `https://api.onlineportfolio.com.br` | No |

### 9.3 Domains

Add in **Project → Settings → Domains** (detalhes DNS em [§10](#10-dns-no-deploy)):

| Domain | Purpose |
|---|---|
| `onlineportfolio.com.br` | Marketing |
| `app.onlineportfolio.com.br` | Admin |
| `*.onlineportfolio.com.br` | Tenant subdomains |

Domínios custom por artista → [§12](#12-domínios-custom-por-tenant-fase-4).

### 9.4 Vercel checklist

- [ ] Project connected; root `frontend`
- [ ] Env vars set; PR previews on
- [ ] Production auto-deploy **off**
- [ ] Proxy reaches Render API

---

## 10. DNS no deploy

Configurar **depois** de Render (§7) e Vercel (§9) existirem.

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
4. Aguardar SSL (minutos após propagação)

**Alternativa:** DNS manual no Registro.br — sem wildcard SSL fácil; ver registros abaixo.

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

### 10.4 DNS de email — SendGrid (v1, só envio)

Na **Vercel DNS** (sem MX — sem caixa postal):

| Type | Name | Value |
|---|---|---|
| `CNAME` | `emXXXX` (SendGrid) | SendGrid target |
| `CNAME` | `s1._domainkey` | SendGrid DKIM |
| `CNAME` | `s2._domainkey` | SendGrid DKIM |

Add **SPF** TXT if SendGrid instructs (`include:sendgrid.net`).

---

## 11. QuestPDF (sem conta externa)

QuestPDF is a **NuGet package** in the .NET API — no external account or API key.

| Item | Action |
|---|---|
| **License** | Set `LicenseType.Community` in code at startup |
| **Eligibility** | Free if org revenue < $1M USD/year |
| **Configuration** | None — optional layout/assets in repo |

See [QuestPDF license](https://www.questpdf.com/license/).

---

## 12. Domínios custom por tenant (Fase 4)

Quando um artista usar domínio próprio (ex.: `ana-art.com`) em vez de `{slug}.onlineportfolio.com.br`.

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

Admin permanece em `app.onlineportfolio.com.br` — domínio do artista é só galeria pública.

### 12.3 Subdomain tenants (default)

`ana.onlineportfolio.com.br` — wildcard `*.onlineportfolio.com.br` no Vercel; `Slug = "ana"` no banco.

---

## 13. Stripe — cobrança SaaS (Fase 4, **opcional**)

**Status:** ⏸️ **Fora do escopo inicial** — **não cobrar no v1**; **não criar conta Stripe** até decidir cobrar ([DEV-403](./BACKLOG.md), P3 / skip).

### Decisão v1

| Item | v1 (agora) | Futuro (se/quando cobrar) |
|---|---|---|
| Cobrança automática | **Não** | Checkout + assinatura |
| Conta Stripe / PSP | **Não criar** | Avaliar na hora |
| Planos / limites | Operador define manualmente (ou sem limite rígido) | `subscriptions` + webhooks |
| Onboarding artista | Manual pelo operador | Pode continuar manual |

### Para que serve (quando implementar)

Processador de **pagamentos** para **assinatura mensal** dos artistas (tenants). Só entra se você passar a cobrar pela plataforma.

| Função | O que faz |
|---|---|
| **Assinatura recorrente** | Artista paga mensalmente (planos Basic, Pro, …) |
| **Checkout** | Link/página de pagamento |
| **Customer Portal** | Artista atualiza pagamento, vê faturas, cancela |
| **Webhooks** | API recebe eventos e atualiza `subscriptions` no Postgres |
| **Limites por plano** | Ex.: nº de obras, domínio custom só no Pro |

**Não substitui:** SendGrid, Vercel/Render, Identity (login).

```text
Artista → Checkout → webhook → API .NET → Postgres (subscription) → limites do tenant
```

### Brasil vs. expansão internacional

| Cenário | Preferência | Motivo |
|---|---|---|
| **Só Brasil** | Asaas, Iugu ou similar | PIX recorrente, boleto, rotina fiscal BR |
| **Exportar para fora do BR** | **Stripe** (ou Adyen) | Multi-moeda, cartões globais, mesma API em vários países |
| **Brasil agora + global depois** | Stripe ou híbrido (avaliar na ADR) | Stripe cobre BR (PIX/cartão) e escala internacionalmente |

**Resumo:** se a meta incluir **artistas fora do Brasil**, Stripe (ou gateway global equivalente) tende a ser **melhor** que PSP só-BR. Para **100% Brasil**, players locais podem ser mais práticos. Decisão **aberta** — implementar só quando cobrar.

| | |
|---|---|
| **Criar conta** | [dashboard.stripe.com/register](https://dashboard.stripe.com/register) |
| **Painel** | [dashboard.stripe.com](https://dashboard.stripe.com) |
| **Docs** | [docs.stripe.com](https://docs.stripe.com/) |

### Configuração prevista (se DEV-403 for feito)

| Item | Onde |
|---|---|
| Products / Prices | Dashboard do PSP escolhido |
| Webhook URL | `https://api.onlineportfolio.com.br/api/v1/webhooks/stripe` (ou equivalente) |
| `Stripe__WebhookSecret` | Render env |
| `Stripe__SecretKey` | Render env — **nunca** no frontend |

### Stripe checklist (só quando cobrar)

- [ ] Decisão PSP documentada (Stripe vs. local vs. híbrido)
- [ ] Conta verificada; products/prices criados
- [ ] Webhook + handler na API
- [ ] Customer Portal habilitado (se Stripe)
- [ ] Teste com cartões/métodos de teste

---

## 14. Google Workspace (caixa postal operador — futuro)

**Status:** ⏸️ Fora do v1. SendGrid cobre **envio**; Google Workspace cobre **inbox** (`hello@`, `marcelo@`, …).

| | SendGrid (v1) | Google Workspace (futuro) |
|---|---|---|
| **Função** | App **envia** (`noreply@`, contato, convites) | Você **lê/responde** |
| **Caixa postal** | Não | Sim |
| **Custo** | Free tier (100/dia) | ~US$ 6–7/usuário/mês |

| | |
|---|---|
| **Criar conta** | [workspace.google.com](https://workspace.google.com/) |
| **Painel** | [admin.google.com](https://admin.google.com) |

Ao adicionar Google, **mesclar SPF** com SendGrid: `v=spf1 include:sendgrid.net include:_spf.google.com ~all`

---

## 15. Secrets & environment variable map

| Secret / config | Configured in | Used by |
|---|---|---|
| Supabase pooler connection string | Render | API runtime (EF) |
| Supabase direct connection string | GitHub Actions secret, local `.env` | Migrations only |
| Supabase service role key | Render env | API Storage writes (Phase 3+) |
| `Jwt__Secret` | Render env | API-issued JWT signing |
| SendGrid API key | Render env | API email (invites + contact) |
| SendGrid from email/name | Render env | API email headers |
| `NUXT_PUBLIC_*` vars | Vercel env | Nuxt client + build |
| Stripe keys (opcional) | Render env | Billing webhooks — só se DEV-403 |
| Database password | Supabase (set at create) | Embedded in connection strings |

**Never commit:** connection strings, `service_role`, `Jwt__Secret`, SendGrid/Stripe keys. Use `.env.example` in `frontend/` and `backend/`.

---

## 16. Phase checklists

### Phase 0 — Foundation

| Provider | Configure |
|---|---|
| **Registro.br** | ✅ Domínio — DNS no deploy [§10](#10-dns-no-deploy) |
| **GitHub** | Repo + workflows [§4](#4-github-cicd--actions) |
| **Linear** | Workspace [§5](#5-linear-project-management) |
| **Supabase** | `portfolio-prod` only [ADR-015](./ARCHITECTURE.md#adr-015-deploy-somente-em-production) |

### Phase 1 — Public portfolio + contact

| Provider | Configure |
|---|---|
| Render, SendGrid, Vercel | [§7](#7-render-api) · [§8](#8-sendgrid-email) · [§9](#9-vercel-frontend) |
| DNS | [§10](#10-dns-no-deploy) |

### Phase 4 — Custom domains (+ billing opcional)

| Provider | Configure |
|---|---|
| Vercel | Domínios por tenant [§12](#12-domínios-custom-por-tenant-fase-4) |
| Stripe / PSP | **Opcional** — só se cobrar [§13](#13-stripe--cobrança-saas-fase-4-opcional) |
| Google Workspace | Inbox operador (opcional) [§14](#14-google-workspace-caixa-postal-operador--futuro) |

---

## 17. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| API 500 on DB connect | Wrong connection string or pooler mode | Transaction pooler on 6543; check SSL |
| EF migrations fail | Using pooler for migrations | Direct connection port 5432 |
| Login 401 | Wrong password or inactive user | Identity seed; proxy forwards body |
| CORS error | Direct browser → Render | Nuxt `/api` proxy only |
| SendGrid emails not arriving | Domain not authenticated | DKIM/SPF [§10.4](#104-dns-de-email--sendgrid-v1-só-envio) |
| Wildcard SSL fail | DNS wildcard missing | [§10.2](#102-dns-records-reference-vercel) |
| JWT invalid | Wrong `Jwt__Secret` | Verify Render env |

---

## Quick reference — URLs to bookmark

**Ordem de setup:** Registro.br → GitHub → Linear → Supabase → Render → SendGrid → Vercel → DNS.

| Service | Criar conta | Painel | Docs |
|---|---|---|---|
| **Registro.br** | [registro.br](https://registro.br) | [Login](https://registro.br/login/) | [Ajuda](https://registro.br/ajuda) |
| **GitHub** | [Signup](https://github.com/signup) | [github.com](https://github.com) | [Actions](https://docs.github.com/en/actions) |
| **Linear** | [Signup](https://linear.app/signup) | [linear.app](https://linear.app) | [Docs](https://linear.app/docs) |
| **Supabase** | [Dashboard](https://supabase.com/dashboard) | [Dashboard](https://supabase.com/dashboard) | [Docs](https://supabase.com/docs) |
| **Render** | [Register](https://dashboard.render.com/register) | [Dashboard](https://dashboard.render.com) | [Domains](https://render.com/docs/custom-domains) |
| **SendGrid** | [Signup](https://signup.sendgrid.com/) | [App](https://app.sendgrid.com) | [Sender auth](https://docs.sendgrid.com/ui/account-and-settings/how-to-set-up-domain-authentication) |
| **Vercel** | [Signup](https://vercel.com/signup) | [Dashboard](https://vercel.com/dashboard) | [Domains](https://vercel.com/docs/projects/domains) |
| **Stripe** (opcional) | [Register](https://dashboard.stripe.com/register) | [Dashboard](https://dashboard.stripe.com) | [Webhooks](https://docs.stripe.com/webhooks) |
| **Google Workspace** | [workspace.google.com](https://workspace.google.com/) | [Admin](https://admin.google.com) | — |
| **QuestPDF** | — | — | [License](https://www.questpdf.com/license/) |
| **BACKLOG** | — | — | [BACKLOG.md](./BACKLOG.md) |
