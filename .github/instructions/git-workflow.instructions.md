---
applyTo: "**"
---

# Git flow, CI, and cross-stack review

## Agent / AI restrictions

Do **not** run `git commit`, `git push`, `git checkout`, `git merge`, or `git rebase` unless the user explicitly requests it in the same message. See `.cursor/rules/git-workflow.mdc`.

## CI structure (decided)

| Workflow | Status | Purpose |
|---|---|---|
| `ci-backend.yml` | ✅ | PR/push → `dotnet test` + Coverlet; comment **Backend coverage** on PR |
| `ci-frontend.yml` | ✅ | PR/push → `npm run lint` + `npm run test:coverage`; comment **Frontend coverage** on PR |
| `deploy-backend.yml` | 🔜 DEV-007 | Push `main` — test → EF migrate → gate Render |
| `deploy-frontend.yml` | 🔜 DEV-007b | Push `main` — lint/test → `vercel deploy --prod` |

**Do not** collapse into a single `ci.yml` that always runs both stacks.

Monorepo = **one Git repo**; separate workflows ≠ separate repositories.

**Path filters:** each CI runs only when its paths change. Skipped checks count as OK on branch protection. No workflow run → no coverage comment on the PR.

**Environments:** cloud **deploy** production only on `main`. Local = Docker Compose **or** Supabase `online-portfolio-db-dev` (`.env`); PR = Vercel preview. No deployed API/front staging in v1 — [docs/ARCHITECTURE.md](./ARCHITECTURE.md).

## Before every merge (human or agent)

1. Relevant CI checks green (`Backend CI`, `Frontend CI`).
2. Review sticky **Backend coverage** / **Frontend coverage** comments if present (informational in v1).
3. **Paired components** — if the change affects API contract, auth, schema, env, or surface (admin vs tenant public), update **both** sides in the same PR when possible:

| Change | Backend | Frontend |
|---|---|---|
| Endpoint / DTO | Controller, service, validation, `[Authorize]` | `server/api/**` proxy, composables, types, UI |
| EF schema | Migration (+ seed if needed) | API consumers, forms |
| Auth / JWT | Identity, policies, tenant checks | Proxy headers/cookies, `app.*` login |
| Env var | Render, `appsettings` | Vercel, `frontend/.env.example`, `runtimeConfig` |
| Multi-tenant | EF filters + membership | No trusted client `tenantId`; correct host routing |
| Admin UI | Protected API routes | `components/app/`, `app.*`, `surface-dark` |
| Tenant public site | Published-only public API | `{slug}.*`, `public/tenants/{slug}/`, themes |

4. Update `docs/DATABASE.md` if schema changed; `frontend/.env.example` if new Nuxt env vars.
5. [Conventional Commits](../docs/CONVENTIONAL_COMMITS.md) with scope `backend` / `frontend`.

Full guide: [docs/AGENT_GUIDE.md](./AGENT_GUIDE.md)
