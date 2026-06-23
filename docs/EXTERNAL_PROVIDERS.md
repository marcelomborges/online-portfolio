# External Provider Configuration Guide

Step-by-step configuration for every third-party service required to run the artist portfolio platform in production.

**Related:** [ARCHITECTURE.md](./ARCHITECTURE.md) · [BACKLOG.md](./BACKLOG.md)  
**Platform domain:** `onlineportfolio.com.br` — **registrado** no [Registro.br](https://registro.br)  
**Email v1:** Resend `noreply@onlineportfolio.com.br` — só envio transacional, sem caixa postal ([ADR-017](./ADR-017-resend-transactional-email.md))  
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
8. [Resend (Email)](#8-resend-email)
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
| **Registro.br** | Domínio `onlineportfolio.com.br` | ✅ 1 (feito) | seção 3 |
| **GitHub** | Repo monorepo + CI/CD | 1 repo | seção 4 |
| **Linear** | Issues, sprints (`DEV-xxx`) | 1 workspace | seção 5 |
| **Supabase** | PostgreSQL, Storage | 1 prod | seção 6 |
| **Render** | ASP.NET Core API (Docker) | 1 web service | seção 7 |
| **Resend** | Email transacional (`noreply@`) | 1 conta | seção 8 |
| **Vercel** | Nuxt frontend | 1 project | seção 9 |
| **Stripe** | Cobrança SaaS (**opcional**, futuro) | ⏸️ não usar agora | seção 13 |
| **Google Workspace** | Inbox operador (futuro) | ⏸️ pós-lançamento | seção 14 |
| **QuestPDF** | PDF (NuGet) | Sem conta | seção 11 |

### 1.1 Master checklist — criar contas

Use esta tabela para abrir cada serviço na ordem. Marque conforme for concluindo.

| # | Provider | Criar conta | Painel | Doc deste repo | Status |
|---|---|---|---|---|---|
| 1 | **Registro.br** | [registro.br](https://registro.br) | [Painel NIC](https://registro.br/login/) | seção 3.1 | ✅ **Feito** — domínio comprado |
| 2 | **GitHub** | [github.com/signup](https://github.com/signup) | [github.com](https://github.com) | seção 4 | ⬜ Criar repo `online-portfolio` |
| 3 | **Linear** | [linear.app/signup](https://linear.app/signup) | [linear.app](https://linear.app) | seção 5 | ✅ Backlog importado |
| 4 | **Supabase** | [supabase.com/dashboard](https://supabase.com/dashboard) | [Dashboard](https://supabase.com/dashboard) | seção 6 | ✅ `online-portfolio-db-prod` + opcional `online-portfolio-db-dev` |
| 5 | **Render** | [dashboard.render.com/register](https://dashboard.render.com/register) | [Render](https://dashboard.render.com) | seção 7 | ✅ API prod (`*.onrender.com`; domínio custom → DEV-011) |
| 6 | **Resend** | [resend.com/signup](https://resend.com/signup) | [Resend](https://resend.com/domains) | seção 8 | ✅ conta + API key + Render env (DEV-014) |
| 7 | **Vercel** | [vercel.com/signup](https://vercel.com/signup) | [Vercel](https://vercel.com/dashboard) | seção 9 | ⬜ Projeto Nuxt |
| 8 | **Google Workspace** | [workspace.google.com](https://workspace.google.com/) | [Admin](https://admin.google.com) | seção 14 | ⏸️ Depois do lançamento |
| 9 | **Stripe** | [dashboard.stripe.com/register](https://dashboard.stripe.com/register) | [Stripe](https://dashboard.stripe.com) | seção 13 | ⏸️ **Opcional** — só se/quando cobrar |

**O que guardar em password manager / GitHub Secrets:** ver seção 15.

**Ordem recomendada:** seção 2.

### Domain layout (production)

Mapa completo: [docs/ARCHITECTURE.md](./ARCHITECTURE.md)

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
                     │ Resend │
                     └──────────┘
```

---

## 2. Ordem de setup

Siga esta ordem para evitar dependências circulares. As seções **3–9** deste arquivo seguem a mesma sequência.

| Passo | Provider | Ação | Seção |
|---|---|---|---|
| 1 | **Registro.br** | ✅ Domínio registrado | seção 3 |
| 2 | **GitHub** | Repo monorepo + push | seção 4 |
| 3 | **Linear** | Workspace + BACKLOG (pode ser cedo) | seção 5 |
| 4 | **Supabase** | `online-portfolio-db-prod` (+ opcional `online-portfolio-db-dev` para local) | seção 6 |
| 5 | **Render** | Conta + web service (após DEV-001) | seção 7 |
| 6 | **Resend** | Conta + API key | seção 8 |
| 7 | **Vercel** | Conta + projeto Nuxt (após DEV-001) | seção 9 |
| 8 | **DNS** | NS Vercel, `api.`, DKIM Resend | seção 10 |
| 9 | **GitHub Actions** | Workflows + secrets | seção 4.3 |
| — | **Por tenant** | Domínio custom do artista | seção 12 |
| — | **Stripe** | Cobrança — **opcional, não agora** | seção 13 |
| — | **Google Workspace** | Inbox operador — **futuro** | seção 14 |

**Deploy na nuvem:** somente **production** ([docs/ARCHITECTURE.md](./ARCHITECTURE.md)).

**Issues no backlog:** cada passo acima tem issue `DEV-xxx` — ver [docs/BACKLOG.md](./BACKLOG.md).

---

## 3. Registro.br — domínio

### 3.1 Domínio `onlineportfolio.com.br`

**Status:** ✅ **Registrado** — titular ativo no Registro.br. DNS de produção → seção 10 (após Render + Vercel).

| Item | Valor |
|---|---|
| **Domínio** | `onlineportfolio.com.br` |
| **Registrar** | [Registro.br](https://registro.br) — único registrador oficial `.br` |
| **Painel** | [registro.br/login](https://registro.br/login/) |
| **Renovação** | ~R$ 40/ano (Pix, boleto ou cartão) |
| **Titular** | Seu CPF/CNPJ — não transfere para Vercel |
| **DNS agora** | Pode manter DNS padrão Registro.br até deploy |
| **DNS no deploy** | Nameservers Vercel — seção 10 |

| Check | Result |
|---|---|
| `onlineportfolio.com` | **Indisponível** — registrado por terceiros |
| `onlineportfolio.com.br` | ✅ **Seu** — registrado |

#### Verificar no painel Registro.br

- [x] Domínio `onlineportfolio.com.br` com status **Ativo** / **Publicado**
- [ ] Anotar data de expiração / renovação automática
- [ ] Guardar login Registro.br no password manager
- [ ] DNS: deixar padrão **ou** apontar NS Vercel no deploy (seção 10)

#### Referências Registro.br

| Recurso | URL |
|---|---|
| Painel / login | https://registro.br/login/ |
| Ajuda DNS | https://registro.br/ajuda |
| WHOIS | https://registro.br/tecnologia/ferramentas/whois |
| Alterar nameservers | Painel → domínio → **Alterar servidores DNS** |

#### Por que `.com.br` continua ok

- Your first two artists are in Brazil — `.com.br` is trusted locally
- Vercel, Render, Supabase, Resend, and Google Workspace all support `.com.br` custom domains
- Same architecture: `app.`, `api.`, `{slug}.` subdomains work identically
- Fixed price: **R$ 40/year** with no renewal surprises
- Payment: Pix, boleto, or card — no international card required

**Trade-off:** Menos “SaaS global” que `.com` — ok para v1 Brasil.

#### Registro concluído — o que fazer agora vs no deploy

| Quando | Ação |
|---|---|
| **Agora (pós-compra)** | Confirmar **Ativo** no painel; guardar credenciais; opcional: criar contas GitHub, Linear, Supabase |
| **Epic 0 (DEV-001)** | Push monorepo no GitHub |
| **Deploy** | Nameservers Vercel + domínios (seção 10) |
| **Pós-deploy** | Resend DKIM na Vercel DNS (seção 10.4) |

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
| **Resend** | — | N/A | SPF/DKIM for email, not web |

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
- [ ] Render connected — **After CI Checks Pass** + root `backend` (seção 4.6) — DEV-007 ✅ After CI; confirmar root `backend`

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
  deploy-frontend.yml  # push main: lint/test → vercel deploy --prod (DEV-007b)
```

**Decisão:** CI **e deploy separados**; **somente production** ([docs/ARCHITECTURE.md](./ARCHITECTURE.md)).

### 4.4 Git flow — revisão de componentes (front + back)

Além dos testes, conferir pareamento front↔back antes do merge. Referência: [docs/ARCHITECTURE.md](./ARCHITECTURE.md) · [docs/AGENT_GUIDE.md](./AGENT_GUIDE.md).

### 4.5 GitHub checklist

- [ ] Repo created and pushed
- [x] `ci-backend.yml`, `ci-frontend.yml` (PR CI — DEV-006)
- [x] `deploy-backend.yml` (DEV-007) — migrate prod + gate Render
- [ ] `deploy-frontend.yml` (DEV-007b)
- [x] `SUPABASE_MIGRATION_CONNECTION_STRING` configured; Render **After CI Checks Pass** enabled

### 4.6 Monorepo — deploy isolado por stack

Push em `main` no monorepo **não** deve redeployar a stack que não mudou. Três camadas trabalham juntas:

| Camada | Só `frontend/**` | Só `backend/**` |
|--------|------------------|-----------------|
| **GitHub Actions** | `ci-frontend.yml` + `deploy-frontend.yml` (quando existir) | `ci-backend.yml` + `deploy-backend.yml` |
| **Render (API)** | Sem autodeploy | Autodeploy após **Backend Deploy** green |
| **Vercel (frontend)** | Prod via `deploy-frontend.yml` (quando existir) | Sem deploy de produção |

**GitHub:** `paths` nos workflows de deploy (`deploy-backend.yml`, `deploy-frontend.yml`) e nos CIs em **push**; em **PR**, os workflows sempre reportam status (skip interno com `paths-filter` — ver `ci-backend.yml` / `ci-frontend.yml`).

**Render** ([monorepo support](https://render.com/docs/monorepo-support)):

| Setting | Valor |
|---------|--------|
| **Root directory** | **`backend`** (obrigatório) — mudanças fora de `backend/` **não** disparam autodeploy |
| **Build Filters → Included paths** (opcional, reforço) | `backend/**`, `.github/workflows/deploy-backend.yml`, `docs/DATABASE.md` |
| **After CI Checks Pass** | On — aguarda checks do commit (incl. **Backend Deploy**) |

**Vercel** ([monorepo](https://vercel.com/docs/monorepos)):

| Setting | Valor |
|---------|--------|
| **Root directory** | **`frontend`** — mudanças fora de `frontend/` **não** disparam build/deploy deste projeto |
| **Production auto-deploy** | **Off** — produção só via `deploy-frontend.yml` (paths `frontend/**`, …) |
| **PR previews** | On — previews só quando o PR altera arquivos sob `frontend/` |

```
push main só frontend/
  → Frontend CI (push) · deploy-frontend (futuro)
  → Render API: não
  → Vercel prod: sim (via Actions)

push main só backend/
  → Backend CI · Backend Deploy · Render API (após checks)
  → Vercel prod: não
```

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
| **Project name** | `online-portfolio-db-prod` |
| **Region** | Closest to most users (e.g. EU West if artists in Europe) |
| **Database password** | Strong password — store in password manager |

Wait for project provisioning (~2 minutes).

### 6.2 Database — connection strings for EF Core

Go to **Project Settings → Database → Connection string** (ou **Connect** no dashboard).

O projeto usa **duas** strings Supabase em produção — **não** direct:

| Use | Mode in Supabase UI | Port | Used by |
|---|---|---|---|
| **Runtime (API)** | Connection pooling → **Transaction** | `6543` | Render `ConnectionStrings__Default` |
| **Migrations (CI)** | Connection pooling → **Session** | `5432` | GitHub secret `SUPABASE_MIGRATION_CONNECTION_STRING` |

**Dev local:** Postgres no Docker Compose (`localhost:5432`) — ver [docs/DEV_COMMANDS.md](./DEV_COMMANDS.md). Não apontar o dia a dia para Supabase prod.

**Direct (`db.[ref].supabase.co:5432`):** não configurar no repo nem nos secrets. Host IPv6; GitHub Actions / Render / Vercel são IPv4-only → `Network is unreachable`. Opcional só para ferramentas manuais (pg_dump, DBeaver) se a tua rede tiver IPv6 ou add-on IPv4 Supabase.

**Por que Session pooler no CI?** Mesmo host pooler que o runtime, porta **5432** (Session), user `postgres.[project-ref]` — compatível com IPv4 e com `dotnet ef`.

**Format (Npgsql / URI):**

```text
# Runtime — Render (Transaction)
Host=aws-1-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.[project-ref];Password=...;SSL Mode=Require;Trust Server Certificate=true

# Migrations — GitHub Actions (Session)
Host=aws-1-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.[project-ref];Password=...;SSL Mode=Require;Trust Server Certificate=true
```

**Settings to verify:**

- [ ] Runtime API: **Transaction** na porta **6543** (nunca usar para `dotnet ef`)
- [ ] CI migrate: **Session** na porta **5432** (copiar de **Connect → Session pooler**)
- [ ] **Não** gravar `db.[ref].supabase.co` no secret de migrate
- [ ] Enable **SSL** — `SSL Mode=Require` na connection string Npgsql
- [ ] Do **not** expose these strings in frontend or git

**Dev / staging Supabase — fora do v1:**

Não criar segundo projeto Supabase deployado para homologação. Desenvolvimento usa **Postgres local** (Docker Compose). Decisão: [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

**DEV-008b (futuro):** projeto Supabase dev remoto — strings e fluxo serão rediscutidos; até lá, local = `localhost`.

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
| Emails de convite | Resend (API envia no convite) |

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
| **Root directory** | **`backend`** — obrigatório no monorepo; push só em `frontend/` não dispara API (seção 4.6) |
| **Runtime** | Docker |
| **Dockerfile path** | `OnlinePortfolio.Api/Dockerfile` (relativo ao root `backend`) |
| **Branch** | `main` |
| **Region** | Same as Supabase when possible |
| **Instance type** | Free |

### 7.2 Service configuration

| Setting | Value |
|---|---|
| **Health check path** | `/health` |
| **Auto-deploy** | Yes (on push to `main` que altere `backend/`) |
| **After CI Checks Pass** | **On** — aguardar checks do commit (incl. **Backend Deploy** de `deploy-backend.yml`) |
| **Build Filters** (opcional) | Included: `backend/**`, `.github/workflows/deploy-backend.yml`, `docs/DATABASE.md` |
| **Build command** | (Docker handles build) |
| **Start command** | (from Dockerfile `ENTRYPOINT`) |

**Monorepo:** com **Root directory** = `backend`, alterações só em `frontend/` **não** redeployam a API. Ver seção 4.6.

**Free tier behavior:**

- Spins down after ~15 min idle
- Cold starts 5–30+ seconds
- No persistent disk — do not store uploads locally

### 7.3 Custom domain

1. **Settings → Custom Domains → Add** `api.onlineportfolio.com.br`
2. Add CNAME at DNS provider (see seção 10.3)
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
| `Resend__ApiKey` | Resend API token (`re_...`) | Yes |
| `Resend__FromEmail` | `noreply@onlineportfolio.com.br` | No |
| `Resend__FromName` | Your platform name | No |

**Render UI:** após salvar, valores ficam mascarados (ícone de **olho** para revelar). Não há toggle separado de “secret” no painel atual — mesmo assim, trate `Resend__ApiKey`, `Jwt__Secret`, connection strings e `service_role` como credenciais: password manager + nunca no git.

**Do not set** `ConnectionStrings__Migration` on Render unless you intentionally run migrations from the container (not recommended — use CI instead).

### 7.5 Render checklist

- [x] Web service created (Docker)
- [x] **Root directory** = `backend` (monorepo — seção 4.6)
- [x] **After CI Checks Pass** enabled
- [x] Health check returns 200 at `/health` (`*.onrender.com`)
- [x] All env vars set (DB pooler, `Jwt__*`, `Resend__*`, `ASPNETCORE_*`)
- [ ] Custom domain `api.onlineportfolio.com.br` verified + HTTPS (DEV-011)
- [x] Logs visible in Render dashboard
- [ ] Test: `GET https://api.onlineportfolio.com.br/health` (após DEV-011)
- [ ] Test: public API endpoint returns data

---

## 8. Resend (Email)

**Decisão v1:** `noreply@onlineportfolio.com.br` — só envio (contato, convites). Sem caixa postal. Ver [ADR-017](./ADR-017-resend-transactional-email.md) (substitui SendGrid — fim do free tier permanente em mai/2025).

| | |
|---|---|
| **Criar conta** | [resend.com/signup](https://resend.com/signup) |
| **Painel** | [resend.com/domains](https://resend.com/domains) |
| **Docs** | [resend.com/docs](https://resend.com/docs) |
| **API Keys** | Painel → **API Keys** |
| **Domínio (prod)** | **Domains** → adicionar `onlineportfolio.com.br` — DNS em seção 10.4 |
| **SDK .NET** | [resend.com/docs/send-with-dotnet](https://resend.com/docs/send-with-dotnet) · NuGet `Resend` |
| **Pricing** | [resend.com/pricing](https://resend.com/pricing) — **3.000 emails/mês** free (máx. 100/dia) |

### 8.1 Escopo v1

| Incluído | Fora do v1 |
|---|---|
| Formulário de contato, convites | Caixa postal / MX |
| `noreply@onlineportfolio.com.br` | Resend no frontend |
| Templates em `EmailTemplates/` (repo) | Editor visual no painel |

### 8.2 API key (DEV-014)

1. Criar conta Resend + verificar email — ✅
2. **API Keys** → Create → nome `portfolio-api-prod` — ✅
3. Copiar key `re_...` (só aparece uma vez) → **password manager** (nunca no git)
4. Render → **Environment** → variáveis abaixo (`Resend__ApiKey` mascarada no painel após salvar)
5. Smoke test local (seção 8.5) — opcional antes do Render

| Render env | Valor |
|---|---|
| `Resend__ApiKey` | `re_...` (sensível — mascarada no Render) |
| `Resend__FromEmail` | `noreply@onlineportfolio.com.br` |
| `Resend__FromName` | Online Portfolio |

Equivalente local: secção `Resend` em `appsettings.Development.json` ou `Resend__*` via env (ver `backend/OnlinePortfolio.Api/appsettings.json`).

### 8.5 Smoke test — primeiro email (antes de DEV-011)

Valida conta + API key **sem** domínio próprio: o Resend permite enviar de `onboarding@resend.dev` até verificares `onlineportfolio.com.br` (DEV-011).

1. Instalar SDK (projeto de teste ou futuro DEV-107):

```bash
dotnet add package Resend
```

2. Substituir `re_xxxxxxxxx` pela API key real (**só local** — nunca commitar).

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

3. **PowerShell** (env var, sem hardcode no código):

```powershell
$env:Resend__ApiKey = "re_xxxxxxxxx"   # colar do password manager
```

4. Após **DEV-011:** trocar `From` para `Online Portfolio <noreply@onlineportfolio.com.br>`.

Referência produção (DI no `Program.cs` — implementação em DEV-107):

```csharp
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
    o.ApiToken = builder.Configuration["Resend:ApiKey"]!);
builder.Services.AddTransient<IResend, ResendClient>();
```

### 8.3 Domínio e remetente

- **DEV-011 (prod):** adicionar domínio no Resend → copiar registros DNS → Vercel DNS (seção 10.4)
- Até DKIM verificado: testes com domínio de onboarding Resend ou key de dev (não usar em prod)
- `noreply@` não tem inbox — não usar Single Sender com endereço sem caixa

### 8.4 Resend checklist

- [x] Conta Resend criada
- [x] API key `portfolio-api-prod` criada
- [x] API key no password manager
- [x] `Resend__*` no Render
- [ ] Smoke test enviado (seção 8.5)
- [ ] Domínio `onlineportfolio.com.br` verificado (DEV-011)
- [ ] `IEmailService` + `ResendEmailService` (DEV-107)

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
| **Root directory** | **`frontend`** — obrigatório no monorepo; push só em `backend/` não dispara este projeto (seção 4.6) |
| **Framework preset** | Nuxt.js (auto-detected) |
| **Production auto-deploy** | **Off** — prod só via `deploy-frontend.yml` (paths `frontend/**`; seção 4.3) |

**Monorepo:** com **Root directory** = `frontend` + auto-deploy de produção desligado, alterações só em `backend/` **não** publicam o Nuxt em produção. PR previews continuam limitados a mudanças em `frontend/`.

### 9.2 Environment variables

| Variable | Production example | Secret |
|---|---|---|
| `NUXT_PUBLIC_API_BASE` | `/api` (Nuxt proxy) | No |
| `NUXT_PUBLIC_PLATFORM_HOST` | `onlineportfolio.com.br` | No |
| `NUXT_PUBLIC_APP_HOST` | `app.onlineportfolio.com.br` | No |
| `NUXT_API_INTERNAL_BASE` | `https://api.onlineportfolio.com.br` | No |

### 9.3 Domains

Add in **Project → Settings → Domains** (detalhes DNS em seção 10):

| Domain | Purpose |
|---|---|
| `onlineportfolio.com.br` | Marketing |
| `app.onlineportfolio.com.br` | Admin |
| `*.onlineportfolio.com.br` | Tenant subdomains |

Domínios custom por artista → seção 12.

### 9.4 Vercel checklist

- [ ] Project connected; **Root directory** = `frontend` (monorepo — seção 4.6)
- [ ] Env vars set; PR previews on
- [ ] Production auto-deploy **off** (prod via `deploy-frontend.yml`)
- [ ] Proxy reaches Render API

---

## 10. DNS no deploy

Configurar **depois** de Render (seção 7) e Vercel (seção 9) existirem.

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

### 10.4 DNS de email — Resend (v1, só envio)

Na **Vercel DNS** (sem MX para caixa postal — só envio):

1. Resend → **Domains** → Add `onlineportfolio.com.br`
2. Copiar registros exatos do painel (DKIM, SPF — valores gerados por conta)
3. Adicionar na Vercel DNS conforme indicado (ex.: `resend._domainkey`, subdomínio `send`, etc.)
4. **Verify DNS Records** no Resend

| Tipo típico | Notas |
|---|---|
| `TXT` / `CNAME` DKIM | Ex.: `resend._domainkey` — valor exato do painel |
| `TXT` SPF | Subdomínio `send` (ou conforme Resend) |
| `MX` (bounce) | Se Resend indicar para o subdomínio de envio |

Não reutilizar registros DNS de outros provedores (ex. `sendgrid.net`) — usar só os gerados pelo Resend. Ver [ADR-017](./ADR-017-resend-transactional-email.md).

Ao adicionar Google Workspace depois, **mesclar SPF** conforme docs Resend + Google.

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

**Não substitui:** Resend, Vercel/Render, Identity (login).

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

**Status:** ⏸️ Fora do v1. Resend cobre **envio**; Google Workspace cobre **inbox** (`hello@`, `marcelo@`, …).

| | Resend (v1) | Google Workspace (futuro) |
|---|---|---|
| **Função** | App **envia** (`noreply@`, contato, convites) | Você **lê/responde** |
| **Caixa postal** | Não | Sim |
| **Custo** | Free tier (100/dia) | ~US$ 6–7/usuário/mês |

| | |
|---|---|
| **Criar conta** | [workspace.google.com](https://workspace.google.com/) |
| **Painel** | [admin.google.com](https://admin.google.com) |

Ao adicionar Google, **mesclar SPF** conforme documentação Resend + Google (não copiar `include:sendgrid.net`).

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
| Stripe keys (opcional) | Render env | Billing webhooks — só se DEV-403 |
| Database password | Supabase (set at create) | Embedded in connection strings |

**Never commit:** connection strings, `service_role`, `Jwt__Secret`, Resend/Stripe keys. Frontend: `frontend/.env.example` placeholders only; backend: `appsettings` templates + Render env.

---

## 16. Phase checklists

### Phase 0 — Foundation

| Provider | Configure |
|---|---|
| **Registro.br** | ✅ Domínio — DNS no deploy seção 10 |
| **GitHub** | Repo + workflows seção 4 |
| **Linear** | Workspace seção 5 |
| **Supabase** | `online-portfolio-db-prod` (Render/CI) + opcional `online-portfolio-db-dev` (local `appsettings` only) |

### Phase 1 — Public portfolio + contact

| Provider | Configure |
|---|---|
| Render, Resend, Vercel | seção 7 · seção 8 · seção 9 |
| DNS | seção 10 |

### Phase 4 — Custom domains (+ billing opcional)

| Provider | Configure |
|---|---|
| Vercel | Domínios por tenant seção 12 |
| Stripe / PSP | **Opcional** — só se cobrar seção 13 |
| Google Workspace | Inbox operador (opcional) seção 14 |

---

## 17. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| API 500 on DB connect | Wrong connection string or pooler mode | Transaction pooler on 6543; check SSL |
| EF migrations fail in CI (`Network is unreachable`, IPv6) | Secret usa `db.*.supabase.co` (direct, IPv6) | Trocar secret para **Session pooler** `:5432` (`aws-*-*.pooler.supabase.com`, user `postgres.[ref]`) |
| EF migrations fail | Transaction pooler (`6543`) no migrate | Usar **Session** pooler `:5432` |
| Login 401 | Wrong password or inactive user | Identity seed; proxy forwards body |
| CORS error | Direct browser → Render | Nuxt `/api` proxy only |
| Resend emails not arriving | Domain not authenticated | DKIM/SPF seção 10.4 |
| Wildcard SSL fail | DNS wildcard missing | seção 10.2 |
| JWT invalid | Wrong `Jwt__Secret` | Verify Render env |

---

## Quick reference — URLs to bookmark

**Ordem de setup:** Registro.br → GitHub → Linear → Supabase → Render → Resend → Vercel → DNS.

| Service | Criar conta | Painel | Docs |
|---|---|---|---|
| **Registro.br** | [registro.br](https://registro.br) | [Login](https://registro.br/login/) | [Ajuda](https://registro.br/ajuda) |
| **GitHub** | [Signup](https://github.com/signup) | [github.com](https://github.com) | [Actions](https://docs.github.com/en/actions) |
| **Linear** | [Signup](https://linear.app/signup) | [linear.app](https://linear.app) | [Docs](https://linear.app/docs) |
| **Supabase** | [Dashboard](https://supabase.com/dashboard) | [Dashboard](https://supabase.com/dashboard) | [Docs](https://supabase.com/docs) |
| **Render** | [Register](https://dashboard.render.com/register) | [Dashboard](https://dashboard.render.com) | [Domains](https://render.com/docs/custom-domains) |
| **Resend** | [Signup](https://resend.com/signup) | [Dashboard](https://resend.com/domains) | [Domain setup](https://resend.com/docs/dashboard/domains/introduction) |
| **Vercel** | [Signup](https://vercel.com/signup) | [Dashboard](https://vercel.com/dashboard) | [Domains](https://vercel.com/docs/projects/domains) |
| **Stripe** (opcional) | [Register](https://dashboard.stripe.com/register) | [Dashboard](https://dashboard.stripe.com) | [Webhooks](https://docs.stripe.com/webhooks) |
| **Google Workspace** | [workspace.google.com](https://workspace.google.com/) | [Admin](https://admin.google.com) | — |
| **QuestPDF** | — | — | [License](https://www.questpdf.com/license/) |
| **BACKLOG** | — | — | [BACKLOG.md](./BACKLOG.md) |
