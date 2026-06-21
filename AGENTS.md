# Agent instructions

Hub for AI coding agents (Cursor, GitHub Copilot, Copilot coding agent).

**Full guide:** [docs/AGENT_GUIDE.md](docs/AGENT_GUIDE.md)

## PRIORITY #1 — Security

Multi-tenant SaaS — **security overrides convenience**. Read [AGENT_GUIDE § Security](docs/AGENT_GUIDE.md#security--priority-1-mandatory).

**Always:** tenant isolation · no IDOR · secrets server-only · validate inputs · invite-only auth · rate limits on public writes · safe prod errors.

**Never:** secrets in git/frontend · trust client `TenantId` · skip `[Authorize]` · user enumeration on login · weaken CORS/auth without user approval.

Cursor: `.cursor/rules/security-first.mdc` · GitHub: `.github/instructions/security.instructions.md`

## Quick reference

| Topic | Document |
|---|---|
| Architecture & domains | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| Database & Identity | [docs/DATABASE.md](docs/DATABASE.md) |
| Tasks (DEV-xxx) | [docs/BACKLOG.md](docs/BACKLOG.md) |
| Commits | [docs/CONVENTIONAL_COMMITS.md](docs/CONVENTIONAL_COMMITS.md) |
| Git flow & CI | [docs/AGENT_GUIDE.md § Git flow](docs/AGENT_GUIDE.md#git-flow-ci-and-cross-stack-review) |
| Supabase/Vercel/Render | [docs/EXTERNAL_PROVIDERS.md](docs/EXTERNAL_PROVIDERS.md) |

## Repository status

Documentation phase complete. **No `frontend/` or `backend/` yet** — start with BACKLOG Epic 0 (DEV-001).

## Non-negotiables

0. **SECURITY FIRST** — see [AGENT_GUIDE § Security](docs/AGENT_GUIDE.md#security--priority-1-mandatory); tenant isolation and secrets are never optional.
1. **BFF** — Nuxt proxies all API calls; browser never hits Render directly.
2. **Auth** — ASP.NET Identity completo + JWT; roles in `AspNetRoles`; admin at `app.{domain}` only.
3. **No Supabase Auth** in frontend.
4. **Multi-tenant** — row isolation via `TenantId`; PlatformAdmin only adds users in v1.
5. **Email** — SendGrid `noreply@` transactional only (no inbox v1).
6. **Commits** — [Conventional Commits](docs/CONVENTIONAL_COMMITS.md); commit/push only when user asks.
7. **Scope** — implement BACKLOG acceptance criteria; minimal diff; PT-BR in product docs.
8. **Git/CI** — workflows separados; deploy **prod only** ([ADR-015](docs/ARCHITECTURE.md#adr-015-deploy-somente-em-production)) — [AGENT_GUIDE § Git flow](docs/AGENT_GUIDE.md#git-flow-ci-and-cross-stack-review).

## Implementation order

Epic 0 → Epic 1.5 (login + add user) → Epic 1 (public sites) → …

See [BACKLOG — Suggested implementation order](docs/BACKLOG.md#suggested-implementation-order-first-sprints).

## Tool-specific config

| Tool | File |
|---|---|
| Cursor | `.cursor/rules/security-first.mdc`, `.cursor/rules/project-context.mdc`, `.cursor/rules/conventional-commits.mdc` |
| GitHub Copilot | `.github/copilot-instructions.md`, `.github/instructions/git-workflow.instructions.md` |
| Path instructions | `.github/instructions/*.instructions.md` |
