# Conventional Commits — project guide

All **AI assistants** (Cursor, GitHub Copilot, Copilot coding agent) and **humans** should follow this format for commits and PR titles.

**Spec:** [conventionalcommits.org](https://www.conventionalcommits.org/)

**Enforcement:**

| Tool | Config file |
|---|---|
| Cursor | `.cursor/rules/conventional-commits.mdc` (`alwaysApply: true`) |
| GitHub Copilot | `.github/copilot-instructions.md` |
| GitHub path instructions | `.github/instructions/conventional-commits.instructions.md` |

---

## Message format

```text
<type>(<optional scope>): <description>

[optional body]

[optional footer(s)]
```

### Subject line rules

- **Imperative mood:** `add login endpoint`, not `added` or `adds`
- **Lowercase** description (proper nouns OK: `SendGrid`, `Identity`)
- **No period** at the end
- **≤ 72 characters** when possible
- One primary intent per commit

---

## Types

| Type | When to use |
|---|---|
| `feat` | New feature or user-visible behavior |
| `fix` | Bug fix |
| `docs` | Documentation only (`docs/`, README, comments in docs) |
| `style` | Formatting, whitespace, semicolons — no logic change |
| `refactor` | Code restructure without changing behavior |
| `perf` | Performance improvement |
| `test` | Adding or updating tests |
| `build` | Build system, NuGet/npm, Docker image |
| `ci` | GitHub Actions, Render/Vercel pipeline config |
| `chore` | Tooling, scripts, misc maintenance |
| `revert` | Reverts a previous commit |

---

## Scopes (this monorepo)

| Scope | Path / area |
|---|---|
| `frontend` | `frontend/` — Nuxt 3 |
| `backend` | `backend/` — ASP.NET Core API |
| `docs` | `docs/`, architecture/backlog |
| `db` | EF migrations, `DATABASE.md` implementation |
| `infra` | Render, Vercel, Supabase, DNS, env templates |
| `docker` | `docker-compose.yml`, Dockerfiles |
| `deps` | Dependency-only bumps |

Omit scope for changes that touch the whole repo equally (e.g. root `README.md` only → `docs: ...` or `chore: ...`).

---

## Examples

### Features and fixes

```text
feat(backend): add ASP.NET Identity login and JWT issuance

feat(frontend): add admin login page with Nuxt API proxy

fix(backend): return 403 when tenant user calls platform routes

fix(frontend): resolve tenant slug from subdomain host header
```

### Documentation and CI

```text
docs: add domain map and Identity completo to architecture

docs(db): document AspNetRoles seed for PlatformAdmin

ci: add GitHub Actions workflow for EF migrations on main

build(docker): add multi-stage Dockerfile for API
```

### Database and infra

```text
feat(db): add initial migration for tenants and Identity tables

chore(infra): document SendGrid noreply sender setup
```

### Breaking changes

Add `!` after type/scope **or** a footer:

```text
feat(backend)!: rename auth routes to /api/v1/auth/*

BREAKING CHANGE: clients must use /api/v1/auth/login instead of /auth/login
```

---

## Pull request titles

Use the **same format** as the commit subject:

```text
feat(epic-1.5): admin login and add user MVP
fix(frontend): proxy Set-Cookie from API on login
docs: conventional commits guide for AI assistants
```

If the PR contains multiple types, pick the **dominant** change or split into smaller PRs.

---

## Task IDs (optional footer)

Link backlog items when useful:

```text
feat(backend): implement POST /platform/tenants/{id}/users/invite

Refs DEV-161
```

---

## Anti-patterns (do not use)

```text
❌ update
❌ fix stuff
❌ wip
❌ changes
❌ commit
❌ asdf
❌ feat: stuff
❌ Fixed bug.
```

---

## Language

- **Commit messages:** English (team/industry convention).
- **PR description body:** Portuguese or English — team preference.

---

## AI checklist before committing

1. Message matches `type(scope): description`
2. Type and scope are accurate
3. No secrets in staged files
4. User explicitly requested the commit (unless automation policy says otherwise)
5. Subject explains **why the change matters**, not only the file name
