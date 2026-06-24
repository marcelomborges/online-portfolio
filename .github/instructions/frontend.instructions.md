---
applyTo: "frontend/**"
---

# Frontend (Nuxt 3)

- **Components first:** thin pages; separate **app** (`components/app/` + **Nuxt UI**), **platform**, **tenant public** (`public/tenants/{slug}` — **ultra animado**, GSAP/Lenis OK). ADR-016: [docs/ARCHITECTURE.md](../../docs/ARCHITECTURE.md) · ADR-018: [docs/ADR-018-frontend-ui-motion-stack.md](../../docs/ADR-018-frontend-ui-motion-stack.md) · Guia: `docs/FRONTEND_COMPONENTS.md`.
- **Admin:** minimal motion; prefer Nuxt UI. **Tenant:** motion stack per slug — never import motion libs in `components/app/`.
- **BFF:** all API calls via Nuxt server routes (`/api/**` proxy); browser uses relative `/api`, not `api.onlineportfolio.com.br`.
- Forward auth cookies/headers to backend on proxied requests; never log tokens.
- **Security:** no secrets in `NUXT_PUBLIC_*`; no `v-html` on user content without sanitization.
- **No** `@nuxtjs/supabase`, **no** Supabase keys in env.
- Host routing: `app.*` → admin; `{slug}.*` → public tenant site; apex → platform marketing.
- Admin login only on `app.{host}/login`.
- Public gallery may use Supabase Storage CDN URLs in `<img>` only (read-only).
- **Cross-stack:** proxy routes, composables, and types must stay in sync with API contracts — see `docs/AGENT_GUIDE.md`.
- See `docs/ARCHITECTURE.md`.1 and docs/EXTERNAL_PROVIDERS.md
