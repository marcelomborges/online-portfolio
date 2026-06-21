---
applyTo: "**"
---

# Git flow, CI, and cross-stack review

## CI structure (decided)

| Workflow | Purpose |
|---|---|
| `ci-backend.yml` | PR/push — `dotnet test` (paths: `backend/**`, …) |
| `ci-frontend.yml` | PR/push — `npm lint`/test (paths: `frontend/**`) |
| `deploy-backend.yml` | Push `main` — test → EF migrate → gate Render |
| `deploy-frontend.yml` | Push `main` — lint/test → `vercel deploy --prod` |

**Do not** collapse into a single `ci.yml` that always runs both stacks.

Monorepo = **one Git repo**; separate workflows ≠ separate repositories.

**Environments:** cloud deploy **production only** on `main`. Local = Docker Compose; PR = Vercel preview. No deployed staging in v1 — [ADR-015](../docs/ARCHITECTURE.md#adr-015-deploy-somente-em-production).

## Before every merge (human or agent)

1. Relevant CI checks green (`Backend CI`, `Frontend CI`).
2. **Paired components** — if the change affects the API contract, auth, schema, or env, update **both** sides in the same PR when possible:

| Change | Backend | Frontend |
|---|---|---|
| Endpoint / DTO | Controller, service, validation, `[Authorize]` | `server/api/**` proxy, composables, types, UI |
| EF schema | Migration | API consumers, forms |
| Auth / JWT | Identity, policies, tenant checks | Proxy headers/cookies, `app.*` login |
| Env var | Render, `backend/.env.example` | Vercel, `frontend/.env.example` |
| Multi-tenant | EF filters + membership | No trusted client `tenantId`; correct host routing |

3. Update `docs/DATABASE.md` if schema changed; `.env.example` on both sides if new env vars.
4. [Conventional Commits](../docs/CONVENTIONAL_COMMITS.md) with scope `backend` / `frontend`.

Full guide: [docs/AGENT_GUIDE.md § Git flow](../docs/AGENT_GUIDE.md#git-flow-ci-and-cross-stack-review)
