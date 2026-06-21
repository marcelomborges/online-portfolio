---
applyTo: "frontend/**"
---

# Frontend (Nuxt 3)

- **BFF:** all API calls via Nuxt server routes (`/api/**` proxy); browser uses relative `/api`, not `api.onlineportfolio.com.br`.
- Forward auth cookies/headers to backend on proxied requests; never log tokens.
- **Security:** no secrets in `NUXT_PUBLIC_*`; no `v-html` on user content without sanitization.
- **No** `@nuxtjs/supabase`, **no** Supabase keys in env.
- Host routing: `app.*` → admin; `{slug}.*` → public tenant site; apex → platform marketing.
- Admin login only on `app.{host}/login`.
- Public gallery may use Supabase Storage CDN URLs in `<img>` only (read-only).
- **Cross-stack:** proxy routes, composables, and types must stay in sync with API contracts — see `docs/AGENT_GUIDE.md` § Git flow.
- See `docs/ARCHITECTURE.md` §2.1 and §5.
