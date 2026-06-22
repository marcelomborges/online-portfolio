# GitHub Copilot — repository instructions

Multi-tenant artist portfolio SaaS. **Full agent guide:** [docs/AGENT_GUIDE.md](../docs/AGENT_GUIDE.md)

## PRIORITY #1 — Security

**Security overrides speed.** Multi-tenant isolation is mandatory.

- **Tenant isolation:** `TenantId` + EF filters + API checks on every protected operation; no IDOR.
- **Never trust client tenant:** authorize from JWT user row, not body/query `tenantId`.
- **Secrets:** service role, JWT secret, SendGrid — **Render only**; never in git or Nuxt public env.
- **Auth:** invite-only; lockout; generic login errors; short-lived JWT.
- **Public API:** published content only; rate-limit contact/login.
- **Prod:** no stack traces; lock down Swagger.

Details: [AGENT_GUIDE § Security](../docs/AGENT_GUIDE.md#security--priority-1-mandatory) · [ARCHITECTURE §18](../docs/ARCHITECTURE.md#18-security-requirements)

Path instructions: `.github/instructions/security.instructions.md`

## Before coding

- Monorepo scaffold ready (`frontend/` Nuxt 3, `backend/` ASP.NET Core). CI PR: `ci-backend.yml` + `ci-frontend.yml` (DEV-006). Next: deploy pipelines (DEV-007+).
- Follow task order in [docs/BACKLOG.md](../docs/BACKLOG.md) (Epic 0 → 1.5 login MVP → Epic 1 public sites).
- Architecture truth: [docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md), schema: [docs/DATABASE.md](../docs/DATABASE.md).

## Architecture (required)

- **BFF:** Nuxt server proxy → ASP.NET API; browser never calls Render directly.
- **Auth:** ASP.NET Identity **completo** + JWT; `AspNetRoles` / `AspNetUserRoles`; no Supabase Auth in frontend.
- **Hosts:** admin + login = `app.onlineportfolio.com.br`; public = `{slug}.onlineportfolio.com.br`.
- **Email v1:** SendGrid `noreply@` send-only; no Google Workspace mailbox yet.
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

- **CI + deploy separados:** `ci-backend.yml` + `ci-frontend.yml` (✅) + `deploy-backend.yml` + `deploy-frontend.yml` (planned) — not one combined `ci.yml`.
- **PR coverage comments:** sticky **Backend coverage** / **Frontend coverage** — only when that stack's CI runs on the PR.
- **Before merge:** green CI + **paired components** — API ↔ proxy/types/UI; schema ↔ migration; auth/env both sides. See [AGENT_GUIDE § Git flow](../docs/AGENT_GUIDE.md#git-flow-ci-and-cross-stack-review).

Path instructions: `.github/instructions/git-workflow.instructions.md`

## Code conventions

- Minimal scope; match BACKLOG acceptance criteria.
- Backend: EF Core, snake_case Postgres columns, `/api/v1/` versioning.
- Backend tests: xUnit · Moq · FluentAssertions · Coverlet · `dotnet test` ([ARCHITECTURE §23](../docs/ARCHITECTURE.md#23-testing)).
- Frontend: Nuxt 3, host-based routing; **3 surfaces** (app/platform/tenant), per-tenant public UI — [ADR-016](../docs/ARCHITECTURE.md#adr-016-três-superfícies-e-ui-por-tenant) · [FRONTEND_COMPONENTS.md](../docs/FRONTEND_COMPONENTS.md).
- Product/docs text: Brazilian Portuguese. Commits and code identifiers: English.

## Avoid

Supabase Auth in Nuxt · `users.role` column (use Identity roles) · login on tenant subdomains · uploads direct to Storage from browser · migrations on API startup in prod · **any security shortcut without user approval**
