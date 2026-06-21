# Agent guide — online-portfolio

Instructions for **Cursor**, **GitHub Copilot**, **Copilot coding agent**, and other AI assistants.

> **PRIORITY #1: SECURITY** — multi-tenant SaaS handling artist data and auth. When architecture convenience conflicts with security, **security wins**. See [Security (mandatory)](#security--priority-1-mandatory) below.

| Topic | File |
|---|---|
| Commits | [CONVENTIONAL_COMMITS.md](./CONVENTIONAL_COMMITS.md) |
| Architecture | [ARCHITECTURE.md](./ARCHITECTURE.md) |
| Schema | [DATABASE.md](./DATABASE.md) |
| Tasks | [BACKLOG.md](./BACKLOG.md) |
| Git flow & CI | [§ Git flow](#git-flow-ci-and-cross-stack-review) |
| Providers | [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) |
| Security tasks | [BACKLOG.md § Security](./BACKLOG.md#security) (SEC-001…) |

---

## Repository status

- **Layout:** **monorepo** — one Git repo, `frontend/` + `backend/`; Vercel and Render deploy from subfolders (not separate repos).
- **Phase:** docs complete; code not scaffolded yet (Epic 0 — DEV-001).
- Do **not** create separate Git repos for front/back unless the user explicitly changes this.
- **Domain:** `onlineportfolio.com.br` (Registro.br).
- Before coding, read BACKLOG **Suggested implementation order** — do not skip Epic 0 / 1.5 foundations.

---

## Security — PRIORITY #1 (mandatory)

Agents must treat security as **the highest priority** — above delivery speed, DX shortcuts, or “temporary” relaxations.

Cross-reference: [ARCHITECTURE.md §18](./ARCHITECTURE.md#18-security-requirements) · BACKLOG [SEC-001…SEC-011](./BACKLOG.md#security)

### Threat model (short)

- **Cross-tenant data leak** (tenant A sees tenant B) — primary risk
- **Privilege escalation** (Editor → PlatformAdmin, cross-tenant admin)
- **Secret exposure** (service role, JWT secret, SendGrid in git or frontend)
- **IDOR** on artworks, settings, users by guessing IDs
- **Abuse** of public contact/auth endpoints (spam, brute force)
- **Unsafe uploads** (malware, path traversal, oversized files)

### Mandatory controls

| Control | Implementation |
|---|---|
| **Tenant isolation** | `TenantId` on all tenant tables; EF global query filters; API verifies membership on every protected operation |
| **No client tenant trust** | `TenantId` for authorization comes from **authenticated user row**, never from request body/query alone |
| **IDOR prevention** | Load resource → assert `resource.TenantId == currentUser.TenantId` (or PlatformAdmin path) |
| **Secrets** | `.env` gitignored; `.env.example` placeholders only; Supabase **service role** + SendGrid + `Jwt__Secret` **only on Render**; no secrets in Nuxt `NUXT_PUBLIC_*` |
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
- Log JWTs, passwords, or SendGrid payloads with PII
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
| **Admin host** | Login only at `app.onlineportfolio.com.br` — not on `{slug}.` subdomains. |
| **Public hosts** | `{slug}.onlineportfolio.com.br` = tenant landing + posts/gallery. |
| **Email v1** | SendGrid `noreply@` — send only, no mailbox. No Google Workspace in v1. |
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

- Never commit `.env`, API keys, connection strings, JWT secrets, SendGrid/Supabase service role keys.
- Frontend: no service role key, no SendGrid key.
- Use `.env.example` with placeholders only.

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

Detalhes: [ARCHITECTURE §15](./ARCHITECTURE.md#15-deployment--cicd) · [ADR-015](./ARCHITECTURE.md#adr-015-deploy-somente-em-production) · [EXTERNAL_PROVIDERS §9](./EXTERNAL_PROVIDERS.md#93-workflow-files-planned)

**Ambientes:** deploy na nuvem **somente production** (`main`). Dev = Docker local; PR = Vercel preview. Sem staging deployado no v1.

### Antes de merge — além dos testes

1. **CI verde** — Backend CI e/ou Frontend CI conforme paths alterados no PR.
2. **Componentes pareados** — se mudou contrato API, schema, auth ou env, atualizar **os dois lados** (ou documentar exceção):

| Mudança | Backend | Frontend |
|---|---|---|
| API / DTO | Controller, service, validation, OpenAPI | `server/api/**` proxy, composables, types, pages |
| EF / schema | Migration | API consumers, forms |
| Auth / JWT | Identity, `[Authorize]`, tenant middleware | Proxy auth forwarding, `app.*` login |
| Env var | Render, `backend/.env.example` | Vercel, `frontend/.env.example`, `runtimeConfig` |
| Tenant isolation | EF filters + membership checks | Never trust client `tenantId`; correct host routing |
| Admin feature | Protected routes | UI under `app.{host}` |
| Public site | Published-only public API | `{slug}.{host}` pages + middleware |

3. **Docs** — `DATABASE.md` if schema; provider env map if new secrets.
4. **Scopes** — commits `feat(backend):` / `feat(frontend):` matching touched paths.

Agents: on every PR, explicitly verify the pairing table even when only one side changed in the diff (ask: “does the other side need a follow-up in this PR?”).

---

### Code style

- **Minimal scope** — smallest correct diff; match existing patterns.
- **Docs (product/domain):** Brazilian Portuguese.
- **Commits, API names, code identifiers:** English.
- Backend: EF Core, snake_case columns in Postgres, OpenAPI on API.
- Do not over-engineer helpers or tests unless requested or in BACKLOG.

---

## Expected monorepo layout (when scaffolded)

```text
online-portfolio/
├── frontend/          # Nuxt 3 → Vercel
├── backend/           # ASP.NET Core Web API → Render
├── docs/
├── docker-compose.yml
├── AGENTS.md
└── .github/
```

---

## Implementation order (summary)

1. **Epic 0** — monorepo, Docker, CI skeleton (DEV-001…)
2. **Epic 1.5** — DB + Identity + login + add user (DEV-150…162) — **before** public gallery admin features
3. **Epic 1** — public tenant sites (DEV-101…105)
4. **Epic 3** — uploads via API

Full order: [BACKLOG.md § Suggested implementation order](./BACKLOG.md#suggested-implementation-order-first-sprints).

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

1. Check [ARCHITECTURE.md](./ARCHITECTURE.md) §2.1 (domains) and §9 (auth).
2. Check [DATABASE.md](./DATABASE.md) for schema truth.
3. Check [BACKLOG.md](./BACKLOG.md) for task scope — do not expand beyond acceptance criteria without asking.
