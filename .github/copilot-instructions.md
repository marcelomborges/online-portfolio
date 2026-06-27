# GitHub Copilot — repository instructions

Multi-tenant artist portfolio SaaS. **Full agent guide:** [docs/AGENT_GUIDE.md](../docs/AGENT_GUIDE.md)

## AI instruction files — keep in sync (MANDATORY)

Any time you change architecture, patterns, conventions, or project rules, update **all** AI instruction files in the same operation:

- `CLAUDE.md` — Claude Code
- `.github/copilot-instructions.md` — GitHub Copilot (this file)
- `docs/ARCHITECTURE.md` — source of truth, update first
- `docs/AGENT_GUIDE.md` — general agent guidance

Never leave these files out of sync.

## PRIORITY #1 — Security

**Security overrides speed.** Multi-tenant isolation is mandatory.

- **Tenant isolation:** `TenantId` + EF filters + API checks on every protected operation; no IDOR.
- **Never trust client tenant:** authorize from JWT user row, not body/query `tenantId`.
- **Secrets:** service role, JWT secret, Resend — **Render only**; never in git or Nuxt public env.
- **Auth:** invite-only; lockout; generic login errors; short-lived JWT.
- **Public API:** published content only; rate-limit contact/login.
- **Prod:** no stack traces; lock down Swagger.

Details: [docs/AGENT_GUIDE.md](./AGENT_GUIDE.md) · [docs/ARCHITECTURE.md](./ARCHITECTURE.md)

Path instructions: `.github/instructions/security.instructions.md`

## Before coding

- Monorepo scaffold ready (`frontend/` Nuxt 3, `backend/` ASP.NET Core). CI PR (DEV-006) ✅; deploy pipelines (DEV-007/007b) ✅; DNS prod (DEV-011) ✅. Next: Epic 1.5 (login MVP).
- Follow task order in [docs/BACKLOG.md](../docs/BACKLOG.md) (Epic 0 → 1.5 login MVP → Epic 1 public sites).
- Architecture truth: [docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md), schema: [docs/DATABASE.md](../docs/DATABASE.md).

## Architecture (required)

- **BFF:** Nuxt server proxy → ASP.NET API; browser never calls Render directly.
- **Auth:** ASP.NET Identity (full) + JWT; `AspNetRoles` / `AspNetUserRoles`; no Supabase Auth in frontend.
- **Hosts:** admin + login = `app.onlineportfolio.com.br`; public = `{slug}.onlineportfolio.com.br`.
- **Email v1:** Resend `mail@onlineportfolio.com.br` send-only; Reply-To for replies; no Google Workspace mailbox yet.
- **Tenants:** PlatformAdmin invites users in v1 only.
- **Supabase:** Postgres + Storage only; EF migrations; no direct browser → DB.

## Git commits — Conventional Commits (required)

```text
<type>(<scope>): <description>
```

Types: `feat` · `fix` · `docs` · `style` · `refactor` · `perf` · `test` · `build` · `ci` · `chore` · `revert`

Scopes: `frontend` · `backend` · `docs` · `db` · `infra` · `docker` · `deps`

Security fixes: `fix(backend): …` or mention security in body. Full guide: [docs/CONVENTIONAL_COMMITS.md](../docs/CONVENTIONAL_COMMITS.md)

## Git flow & CI

- **CI + deploy separate:** `ci-backend.yml` + `ci-frontend.yml` (✅) + `deploy-backend.yml` + `deploy-frontend.yml` — not one combined `ci.yml`.
- **PR coverage comments:** sticky **Backend coverage** / **Frontend coverage** — only when that stack's CI runs on the PR.
- **Before merge:** green CI + **paired components** — API ↔ proxy/types/UI; schema ↔ migration; auth/env both sides. See [docs/AGENT_GUIDE.md](./AGENT_GUIDE.md).

Path instructions: `.github/instructions/git-workflow.instructions.md`

## Code conventions

- Minimal scope; match BACKLOG acceptance criteria.
- **Language (inline completions + chat):** see next section and [docs/LANGUAGE.md](../docs/LANGUAGE.md). **Do not** suggest Portuguese because nearby Vue templates are pt-BR.
- Backend: EF Core, snake_case Postgres columns, `/api/v1/` versioning.
- Backend tests: xUnit · Moq · FluentAssertions · Coverlet · `dotnet test` ([docs/ARCHITECTURE.md](./ARCHITECTURE.md)).
- Frontend: Nuxt 3, host-based routing; **3 surfaces** (app/platform/tenant), per-tenant public UI — [docs/ARCHITECTURE.md](./ARCHITECTURE.md) · [FRONTEND_COMPONENTS.md](../docs/FRONTEND_COMPONENTS.md).

## Backend design patterns (mandatory)

**Layer rule:** Controller → Service → Repository. Never skip. No EF Core outside repositories. No business rules in controllers or repositories.

**Options Pattern:** every config block has a typed class in `Options/` (`JwtOptions`, `DevSeedOptions`, …). Register via `services.Configure<T>(config.GetSection(T.Section))`. No raw `configuration["Key"]` in services/controllers.

**Result Pattern:** services return `Result<T>` from `Common/Result.cs`. Use `Error.NotFound()`, `.Conflict()`, `.Unauthorized()`, `.Forbidden()`, `.Validation()`. Controllers map with `.Match(onSuccess, onFailure)`. Exceptions only for unexpected failures.

**Unit of Work:** `IUnitOfWork.CommitAsync()` called only in services. Repositories only manipulate the EF change tracker — never `SaveChanges`. `ApplicationDbContext` implements `IUnitOfWork`.

Path instructions: `.github/instructions/language.instructions.md` · `.github/instructions/vue.instructions.md` · `.github/instructions/frontend.instructions.md`

## Language — Copilot completions (required)

This repo is **English-first**. Copilot must **not** default to Portuguese for the user's locale or because a file contains pt-BR UI copy.

| Suggest **English** | Suggest **pt-BR** only here |
|---|---|
| Code comments, XML/JSDoc, log messages | Vue `<template>` user-visible text |
| C# / TypeScript identifiers and string literals in non-UI code | API `message` fields returned to the UI |
| Commit messages, docs, tests | Transactional email bodies to artists |
| `<script>` blocks in `.vue` (including `statusMessage` only when dev/debug — prefer English keys + i18n later) | Labels, buttons, validation, empty states in admin/public UI |

**Vue SFC rule:** pt-BR in `<template>` does **not** extend to `<script>` completions. When completing `<script>`, use English comments and English developer-facing strings unless explicitly writing a user-facing `message` for the API/UI.

Full guide: [docs/LANGUAGE.md](../docs/LANGUAGE.md)

## Avoid

Supabase Auth in Nuxt · `users.role` column (use Identity roles) · login on tenant subdomains · uploads direct to Storage from browser · migrations on API startup in prod · **any security shortcut without user approval**
