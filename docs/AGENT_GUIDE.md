# Agent guide — online-portfolio

Instructions for **Cursor**, **GitHub Copilot**, **Copilot coding agent**, and other AI assistants.

> **PRIORITY #1: SECURITY** — multi-tenant SaaS handling artist data and auth. When architecture convenience conflicts with security, **security wins**. See [Security (mandatory)](#security--priority-1-mandatory) below.

| Topic | File |
|---|---|
| Commits | [CONVENTIONAL_COMMITS.md](./CONVENTIONAL_COMMITS.md) |
| Architecture | [ARCHITECTURE.md](./ARCHITECTURE.md) |
| Schema | [DATABASE.md](./DATABASE.md) |
| Tasks | [BACKLOG.md](./BACKLOG.md) |
| Git flow & CI | [docs/AGENT_GUIDE.md](./AGENT_GUIDE.md) (seção Git flow) |
| Providers | [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) |
| Security tasks | [docs/BACKLOG.md](./BACKLOG.md) (SEC-001…) |

---

## Repository status

- **Layout:** **monorepo** — `frontend/` (Nuxt 3) + `backend/` (ASP.NET Core); see root [README.md](../README.md).
- **Phase:** Epic 0 in progress — CI PR (DEV-006) ✅, deploy pipelines (DEV-007/007b) ✅; DNS prod (DEV-011) next.
- Do **not** create separate Git repos for front/back unless the user explicitly changes this.
- **Domain:** `onlineportfolio.com.br` (Registro.br).
- Before coding, read BACKLOG **Suggested implementation order** — do not skip Epic 0 / 1.5 foundations.

---

## Security — PRIORITY #1 (mandatory)

Agents must treat security as **the highest priority** — above delivery speed, DX shortcuts, or “temporary” relaxations.

Cross-reference: [docs/ARCHITECTURE.md](./ARCHITECTURE.md) · BACKLOG [docs/BACKLOG.md](./BACKLOG.md)

### Threat model (short)

- **Cross-tenant data leak** (tenant A sees tenant B) — primary risk
- **Privilege escalation** (Editor → PlatformAdmin, cross-tenant admin)
- **Secret exposure** (service role, JWT secret, Resend in git or frontend)
- **IDOR** on artworks, settings, users by guessing IDs
- **Abuse** of public contact/auth endpoints (spam, brute force)
- **Unsafe uploads** (malware, path traversal, oversized files)

### Mandatory controls

| Control | Implementation |
|---|---|
| **Tenant isolation** | `TenantId` on all tenant tables; EF global query filters; API verifies membership on every protected operation |
| **No client tenant trust** | `TenantId` for authorization comes from **authenticated user row**, never from request body/query alone |
| **IDOR prevention** | Load resource → assert `resource.TenantId == currentUser.TenantId` (or PlatformAdmin path) |
| **Secrets** | Frontend: `.env` gitignored, `frontend/.env.example` placeholders only; backend: `appsettings` / Render env (no `.env.example`); Supabase **service role** + Resend + `Jwt__Secret` **only on Render**; no secrets in Nuxt `NUXT_PUBLIC_*` |
| **Auth hardening** | Identity lockout; password policy; invite-only; JWT short TTL + refresh policy; httpOnly cookie if used; same-site where applicable |
| **Login responses** | Same error for bad email vs bad password (no enumeration) |
| **Public API** | Only published content; no internal IDs/flags leaked unnecessarily |
| **BFF** | Nuxt proxy as default — limits CORS exposure and keeps tokens off direct browser→Render calls |
| **Input validation** | All write DTOs validated (FluentValidation/DataAnnotations); max lengths; reject over-posting |
| **Output** | Vue default escaping; no `v-html` on user content without sanitization |
| **Rate limiting** | Contact form, login, invite endpoints (SEC-005) |
| **Uploads (Phase 3)** | API multipart only; MIME/size whitelist; server-generated Storage path under `tenants/{tenantId}/` |
| **Production** | No stack traces to client; Swagger disabled or auth-protected (SEC-002) |
| **Dependencies** | Run/be aware of `npm audit` and `dotnet list package --vulnerable` (SEC-008) |

### Agent checklist (every PR / feature)

Before marking work complete, verify:

1. Could a user of **tenant A** access **tenant B** data with the same request pattern?
2. Could an **Editor** call **PlatformAdmin** routes?
3. Any new secret in code, logs, or client bundle?
4. Are write endpoints validated and rate-limited where public?
5. Do integration tests cover tenant isolation (`IT-012`, `IT-013`, `IT-002`)?

If any answer is uncertain → **stop and fix or ask the user**.

### Never (even for “quick” prototypes)

- Commit `.env`, real connection strings, or API keys
- `AllowAnyOrigin()` CORS in production without explicit user approval
- Skip `[Authorize]` “for now” on admin endpoints
- Return different login errors for unknown email vs wrong password
- Log JWTs, passwords, or Resend payloads with PII
- Raw SQL with string concatenation
- Client-chosen file paths for Storage
- Self-signup endpoint in v1
- Disable HTTPS or cookie flags in production config

### Security-related commits

Use type `fix` or scope when appropriate:

```text
fix(security): prevent IDOR on artwork update by tenant check
fix(backend): generic message on failed login
chore(security): add rate limit to contact endpoint
```

---

## Hard rules (do not violate)

### Architecture

| Rule | Detail |
|---|---|
| **BFF** | Browser → Nuxt server proxy → API. Never call Render from browser (admin or public). |
| **Auth** | ASP.NET Identity **completo** + JWT on API. Roles in `AspNetRoles` / `AspNetUserRoles`. |
| **No Supabase Auth** | No `@nuxtjs/supabase`, no anon key in frontend for login. |
| **Supabase scope** | Postgres + Storage only. Migrations via EF Core only. |
| **Supabase connections** | Local = `localhost`. Prod runtime = Transaction pooler `:6543`. Prod migrate (CI) = Session pooler `:5432`. **No** direct `db.*.supabase.co` in repo/secrets. |
| **Admin host** | Login only at `app.onlineportfolio.com.br` — not on `{slug}.` subdomains. |
| **Public hosts** | `{slug}.onlineportfolio.com.br` = tenant landing + posts/gallery. |
| **Email v1** | Resend `noreply@` — send only, no mailbox. [ADR-017](./ADR-017-resend-transactional-email.md). No Google Workspace in v1. |
| **Uploads v1** | Through API multipart (Phase 3). Exception: public `<img>` CDN URLs. |
| **Tenant isolation** | `TenantId` on tenant data; EF filters + API checks; never trust `TenantId` from request body alone. |

### Roles (v1)

| Role | Scope | Add users? |
|---|---|---|
| `PlatformAdmin` | Platform | Yes — any tenant |
| `Owner` | One tenant | No (v2) |
| `Editor` | One tenant | No |

One email = one account globally. PlatformAdmin has `tenant_id = NULL`.

### Secrets

- Never commit `.env`, API keys, connection strings, JWT secrets, Resend/Supabase service role keys.
- Frontend: no service role key, no Resend key.
- Frontend: use `frontend/.env.example` with placeholders only. Backend: `appsettings.json` templates + Render env.

### Git

- [Conventional Commits](./CONVENTIONAL_COMMITS.md) always.
- Commit only when user explicitly asks.
- Do not push to remote unless user asks.

---

## Git flow, CI, and cross-stack review

### CI: workflows separados (decisão)

| | |
|---|---|
| **Escolhido** | `ci-backend.yml` + `ci-frontend.yml` + `deploy-backend.yml` + `deploy-frontend.yml` |
| **Não usar** | Um único `ci.yml` que sempre roda backend e frontend |
| **Monorepo** | Um repo Git; workflows separados ≠ repos separados |

Detalhes: [docs/ARCHITECTURE.md](./ARCHITECTURE.md) · [docs/ARCHITECTURE.md](./ARCHITECTURE.md) · [docs/EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md)

**Ambientes:** deploy na nuvem **somente production** (`main`). Dev = Docker local; PR = Vercel preview. Sem staging deployado no v1.

### CI em PR (implementado — DEV-006)

Dispara em `pull_request` e `push` → `main`, com **path filters** — só roda o workflow da stack que mudou.

| Workflow | Status check | Comandos | Paths (exemplos) |
|---|---|---|---|
| `ci-backend.yml` | **Backend CI** | `dotnet test` (Release) + Coverlet | `backend/**`, `docs/DATABASE.md`, workflow deploy-backend |
| `deploy-backend.yml` | **Backend Deploy** | `dotnet test` → `dotnet ef database update` (prod) | `push` → `main`; paths `backend/**`, `docs/DATABASE.md` |
| `ci-frontend.yml` | **Frontend CI** | `npm run lint` (typecheck) + `npm run test:coverage` (Vitest) | `frontend/**`, workflow deploy-frontend, action coverage comment |

**Path filters:** PR que muda só `docs/` ou só `scripts/` pode **não** disparar nenhum CI — ok. PR que muda `backend/` **e** `frontend/` dispara **os dois**. Checks **skipped** por path filter contam como OK no GitHub (branch protection).

**Deploy** (`deploy-backend.yml` ✅, `deploy-frontend.yml` ✅). Redeploy manual: **Run workflow** nos deploy pipelines — [docs/DEV_COMMANDS.md](./DEV_COMMANDS.md).

### Comentários de coverage no PR

Somente em **`pull_request`** (não em push direto para `main`). Só publica se o workflow **rodou** — sem CI, sem comentário.

| Comentário sticky | Header | Stack |
|---|---|---|
| **Backend coverage** | `backend-coverage` | Coverlet + `irongut/CodeCoverageSummary` |
| **Frontend coverage** | `frontend-coverage` | Vitest istanbul → script inline no `ci-frontend.yml` |

Dois comentários independentes no mesmo PR quando ambos os CIs rodam. Artefatos (`TestResults/`, `frontend/coverage/`) ficam no `.gitignore`.

### Antes de merge — checklist

1. **CI verde** — `Backend CI` e/ou `Frontend CI` conforme paths alterados no PR.
2. **Coverage** — revisar comentários sticky se existirem (informativo; sem gate de % no v1).
3. **Componentes pareados** — se mudou contrato API, schema, auth, env ou superfície, atualizar **os dois lados** (ou documentar exceção na descrição do PR):

| Mudança | Backend | Frontend |
|---|---|---|
| API / DTO | Controller, service, validation, OpenAPI | `server/api/**` proxy, composables, types, pages |
| EF / schema | Migration (+ seed se necessário) | API consumers, forms |
| Auth / JWT | Identity, `[Authorize]`, tenant middleware | Proxy auth forwarding, `app.*` login |
| Env var | `appsettings`, Render env | Vercel, `frontend/.env.example`, `runtimeConfig` |
| Tenant isolation | EF filters + membership checks | Never trust client `tenantId`; correct host routing |
| Admin feature | Protected routes | UI em `components/app/`, host `app.*`, dark mode (`surface-dark`) |
| Site público tenant | API pública (só conteúdo publicado) | `{slug}.*`, `public/tenants/{slug}/`, `themes/{slug}.css` ([docs/ARCHITECTURE.md](./ARCHITECTURE.md)) |
| Email / Storage (fases futuras) | Resend / multipart API | Proxy ou UI se aplicável |

4. **Docs** — `DATABASE.md` se schema; `frontend/.env.example` ou `appsettings` se novos env vars; `EXTERNAL_PROVIDERS.md` se novo secret de provider.
5. **Commits** — [Conventional Commits](./CONVENTIONAL_COMMITS.md); escopo `backend` / `frontend` coerente com paths tocados.

**Agentes:** em todo PR, verificar a tabela de pareamento mesmo quando o diff toca só um lado — perguntar: “o outro lado precisa de follow-up **neste** PR?”.

**Branch protection (recomendado em `main`):** exigir **Backend CI** + **Frontend CI** (checks skipped = OK).

---

### Code style

- **Minimal scope** — smallest correct diff; match existing patterns.
- **Docs (product/domain):** Brazilian Portuguese.
- **Commits, API names, code identifiers:** English.
- Backend: EF Core, snake_case columns in Postgres, OpenAPI on API.
- Backend tests: xUnit · Moq · FluentAssertions · Coverlet · `dotnet test` ([docs/ARCHITECTURE.md](./ARCHITECTURE.md)).
- Frontend tests: Vitest + istanbul coverage (expand in UT-009+); CI runs `npm run lint` + `npm run test:coverage`.
- **Frontend:** thin pages, three surfaces — **admin = Nuxt UI, minimal motion**; **tenant `{slug}.*` = ultra animado** — [ADR-018](./ADR-018-frontend-ui-motion-stack.md) · [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md).
- Do not over-engineer helpers or tests unless requested or in BACKLOG.

---

## Expected monorepo layout (when scaffolded)

```text
online-portfolio/
├── frontend/          # Nuxt 3 → Vercel
├── backend/
│   ├── OnlinePortfolio.Api.slnx
│   ├── OnlinePortfolio.Api/       # ASP.NET Core → Render
│   └── OnlinePortfolio.Api.Tests/ # xUnit · Moq · FluentAssertions · Coverlet
├── docs/
├── docker-compose.yml
├── AGENTS.md
└── .github/
    └── workflows/       # ci-backend, ci-frontend (+ deploy-* planned)
```

---

## Implementation order (summary)

1. **Epic 0** — monorepo, Docker, CI skeleton (DEV-001…)
2. **Epic 1.5** — DB + Identity + login + add user (DEV-150…162) — **before** public gallery admin features
3. **Epic 1** — public tenant sites (DEV-101…105)
4. **Epic 3** — uploads via API

Full order: [docs/BACKLOG.md](./BACKLOG.md).

When implementing a task, open the matching `DEV-xxx` entry and its acceptance criteria.

---

## Common mistakes to avoid

| Mistake | Why wrong |
|---|---|
| Supabase Auth in Nuxt | Decided: Identity on API |
| `users.role` column | Use `AspNetUserRoles` |
| `ana.domain/login` | Admin is only on `app.` |
| Direct browser → `api.` | CORS + leaks; use Nuxt proxy |
| Auto-run migrations on API startup in prod | CI runs `dotnet ef database update` |
| Store uploads on Render disk | Use Supabase Storage via API |
| Self-signup endpoint | Invite-only v1 |
| Identity “enxuto” without roles table | Use Identity **completo** |
| Skip tenant check “for MVP” | Isolation is MVP — see SEC-003 |
| Verbose 500 errors in prod | Leaks internals — SEC-002 |
| `AllowAnyOrigin()` in prod | Use BFF or strict CORS |

---

## When unsure

1. Check [ARCHITECTURE.md](./ARCHITECTURE.md) seção 2.1 (domains) and seção 9 (auth).
2. Check [DATABASE.md](./DATABASE.md) for schema truth.
3. Check [BACKLOG.md](./BACKLOG.md) for task scope — do not expand beyond acceptance criteria without asking.
