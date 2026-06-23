---
applyTo: "**/*"
---

# Security — PRIORITY #1

This project is **multi-tenant SaaS**. Security overrides speed and convenience.

Reference: `docs/AGENT_GUIDE.md` (Security section), `docs/ARCHITECTURE.md`, BACKLOG `SEC-001`…`SEC-011`.

## Always

- Treat **tenant isolation** as mandatory: `TenantId` filters, membership checks, no IDOR.
- Keep **secrets off** frontend and git; service role and SendGrid keys server-only.
- **Validate** all inputs on write endpoints; limit string lengths.
- Use **invite-only** auth; generic login failure messages.
- **Rate-limit** public write endpoints (contact, auth attempts).
- Return **safe errors** in production (no stack traces).
- Prefer **Nuxt BFF proxy** over exposing API directly to browsers.

## Never

- Commit secrets or put service role / JWT secret in Vercel public env.
- Trust `TenantId` or tenant slug from request body for authorization.
- Expose unpublished/draft data on public routes.
- Add self-signup, bypass auth, or weaken CORS "for testing" without user approval.

When unsure, choose the safer design and document the threat in the PR/commit body.
