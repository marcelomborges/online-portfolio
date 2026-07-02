# Frontend — components and reuse

Project priority: **avoid repetitive code** using reusable Vue components and native Nuxt 3 features.

**Architectural decision:** [docs/ARCHITECTURE.md](./ARCHITECTURE.md) — three surfaces (`app` / `platform` / `tenant`), customizable public UI per slug, standardized login/admin.

**UI and motion (admin vs tenant):** [ADR-018](./ADR-018-frontend-ui-motion-stack.md) — admin **simple + Nuxt UI**; `{slug}.*` site **ultra animated**.

**Operational reference (humans and agents).** Base stack: Nuxt 3.21 + Vue 3 + TypeScript strict.

---

## Language

- **English:** code, comments, docs, commits, API identifiers, logs.
- **pt-BR only:** product UI (Vue templates, user-facing API/email copy).
- Full guide: [LANGUAGE.md](./LANGUAGE.md).

---

## Principles

1. **Thin pages** — `pages/` only orchestrate layout, data, and composables; repeatable markup goes in `components/`.
2. **Composables for logic** — state, fetch, helpers in `composables/` (e.g. `usePlatformConfig`).
3. **Components for UI** — any block used 2+ times or with a clear responsibility becomes a component.
4. **Nuxt built-ins first** — prefer `<NuxtLink>`, `<NuxtLayout>`, `<NuxtPage>`, `<NuxtLoadingIndicator>`, `<NuxtRouteAnnouncer>`, `<NuxtImg>`, `<NuxtTime>`, etc., instead of reinventing.
5. **Auto-import** — Nuxt registers `components/`, `composables/`, and `utils/` automatically; avoid manual imports except edge cases (e.g. types in `error.vue`).

---

## Nuxt built-in components

| Resource | Usage |
|---|---|
| **Vue built-in components** | `NuxtLink`, `NuxtLayout`, `NuxtPage`, `NuxtLoadingIndicator`, `NuxtRouteAnnouncer` |
| **Auto-import from `components/`** | Folder prefix: `components/ui/UiButton.vue` → `<UiButton />` |
| **Composables** | `useRuntimeConfig`, `useAppConfig`, `useFetch`, `useAsyncData`, `useRoute`, `useRouter` |
| **Layouts** | `layouts/default.vue`, `layouts/admin.vue` — shared shell |
| **`app.config.ts`** | Static metadata (site name, links) — not secrets |
| **`#components` / `resolveComponent`** | Only when dynamic component is needed |

### @nuxt/ui — admin and platform (ADR-018 decision)

**Adopted as design system** for `app.*`, `/admin`, `/login`, and platform marketing (apex). Goal: **fast, responsive, minimal motion**.

| Surface | Nuxt UI | Motion |
|---|---|---|
| **Admin / login** (`components/app/`) | ✅ Yes — forms, tables, modals | ❌ No (no Lenis/GSAP/WebGL) |
| **Platform** (`components/platform/`) | ✅ Yes | ⚠️ Light optional (CSS / micro) |
| **Public tenant** (`public/tenants/{slug}/`) | ❌ Not as primary identity | ✅ **Ultra animated** — GSAP, Lenis, etc. |

Tenant stack details: [ADR-018](./ADR-018-frontend-ui-motion-stack.md).

### Vue Bits — animated catalog (tenant)

[Vue Bits](https://vue-bits.dev/) — reference for **backgrounds, text effects, and animated UI** (Vue 3 + TypeScript + Tailwind). **Opt-in** per slug in `public/tenants/{slug}/`; install per component via `jsrepo` or shadcn (code enters the repo). **Do not** use in admin (`components/app/`) — **Nuxt UI** stays there. See [ADR-018](./ADR-018-frontend-ui-motion-stack.md).

Until packages are installed (Epic 1+), local primitives in `components/ui/` (`UiButton`, `UiCard`, …) with `--op-*` tokens.

**Rule:** do not import GSAP/Lenis/Three in `components/app/`. Do not use Nuxt UI as the artist site's "face" — each slug has its own components and theme.

---

## Folder structure — three surfaces + tenants

**One Nuxt app**, three distinct UI modes (resolved by `Host`):

| Surface | Host | Layout | Components | UI / motion |
|---|---|---|---|---|
| **App (admin/login)** | `app.onlineportfolio.com.br` | `layouts/app.vue` | `components/app/` — **standardized** | Nuxt UI · **no heavy motion** |
| **Platform** | `onlineportfolio.com.br` | `layouts/platform.vue` | `components/platform/` | Nuxt UI · light motion |
| **Public tenant** | `{slug}.onlineportfolio.com.br` | `layouts/tenant.vue` | `public/shared/` + `public/tenants/{slug}/` | Custom per slug · **ultra animated** |
| **Dev skeleton** | `localhost` (default) | `layouts/default.vue` | `components/dev/` | Structural dark |

```text
frontend/
├── components/
│   ├── ui/                    # Global primitives (UiButton, UiCard) — use --op-* tokens
│   ├── error/
│   ├── dev/                   # Local skeleton shell
│   ├── app/                   # Login + admin — NEVER customize per tenant
│   │   └── layout/
│   ├── platform/              # Platform marketing (apex)
│   └── public/
│       ├── shared/            # Orchestration (TenantHome.vue) — not page blocks
│       └── tenants/
│           ├── ana/           # LandingHero + ContactSection
│           └── joao/          # LandingHero + ContactSection
├── layouts/
│   ├── app.vue                # app.* — admin/login
│   ├── platform.vue           # apex — marketing
│   ├── tenant.vue             # {slug}.* — artist public site
│   ├── structural.vue         # error + shells without tenant header
│   └── default.vue            # localhost / dev skeleton
├── assets/css/
│   ├── main.css
│   ├── surfaces/
│   │   └── dark.css           # dark mode — everything except public tenant site
│   └── themes/                # ana.css, joao.css — public site only
├── middleware/
│   └── resolve-host.global.ts # Host → surface + tenant slug
└── composables/
    ├── useRequestSurface.ts   # app | platform | tenant | dev
    ├── useSurfaceHtmlClass.ts # surface-dark | theme-{slug} on <html>
    ├── useTenantContext.ts    # slug (public tenant only)
    └── useTenantComponent.ts  # resolve by slug + filename (import.meta.glob)
```

**Naming convention (public site)**

| Location | Filename | Resolved by |
|---|---|---|
| `public/tenants/ana/` | `LandingHero.vue`, `ContactSection.vue` | Slug `ana` |
| `public/tenants/joao/` | `LandingHero.vue`, `ContactSection.vue` | Slug `joao` |
| `public/shared/` | `TenantHome.vue` | Landing orchestration (`/` tenant) |

**No prefix in filename** — slug comes from the **folder** (`tenants/{slug}/`). Landing and contact are **exclusive per tenant** (no generic fallback). Tenants in repo: **ana** and **joao** (DEV-002 seed).

**Admin/login (`components/app/`):** single UI at `app.onlineportfolio.com.br`. Active tenant comes from **logged-in user** (`/auth/me`), not hostname. See docs/FRONTEND_COMPONENTS.md

---

## Structural dark mode (everything except public tenant site)

**Decision:** only **artist public site pages** (`surface=tenant`, layout `tenant`, routes like `/` and `/contact` on `{slug}.*`) use light/customizable theme (`theme-{slug}`). **Everything else** uses **full dark mode** (`surface-dark`).

| Context | Theme | `<html>` class |
|---|---|---|
| **Public tenant site** (`ana.*`, `joao.*`) | Per artist | `theme-{slug}` |
| **Admin, login, platform, dev skeleton** | Fixed dark | `surface-dark` |
| **Error pages** (`error.vue`) | Fixed dark | `surface-dark` (even on tenant host) |
| **Structural layouts** (`app`, `platform`, `default`, `structural`) | Fixed dark | `surface-dark` |
| **`app/*` components** and `ui/*` on admin screens | Fixed dark | inherit `surface-dark` |

CSS: `assets/css/surfaces/dark.css`. Applied by `useSurfaceHtmlClass()` in `app.vue`.

**What uses dark mode:**

- `app.onlineportfolio.com.br` — login, `/admin`, `/platform/*`
- `onlineportfolio.com.br` — platform marketing
- `dev` mode on localhost — skeleton
- `error.vue` + `components/error/`
- Future shared admin screens in `components/app/`

**What does NOT (public tenant only):**

- `components/public/tenants/{slug}/` — page blocks per artist
- `assets/css/themes/{slug}.css` — artist palette

**Rules:**

- **Do not** customize dark per tenant in admin.
- **Do not** light/dark toggle in v1.
- New tenants in repo: folder `tenants/{slug}/` + `themes/{slug}.css` (today: `ana`, `joao`).

Preview: `NUXT_PUBLIC_DEV_SURFACE=app` → dark `/login` · `tenant` + `ana` → light/custom animated landing.

---

## Motion and performance (ADR-018)

| Where | Allowed | Avoid |
|---|---|---|
| `components/app/` | Short CSS transitions; Nuxt UI defaults | Lenis, GSAP, ScrollTrigger, Three, Rive |
| `components/platform/` | Light fade/slide | Scroll hijacking, WebGL |
| `public/tenants/{slug}/` | GSAP + ScrollTrigger, Lenis, @vueuse/motion, WebGL opt-in | Import from `components/app/` to "reuse button" |
| `layouts/tenant.vue` | Initialize smooth scroll / motion composable | Load motion in `app.vue` layout |

**Accessibility:** `prefers-reduced-motion: reduce` disables Lenis and simplifies tenant animations.

**Bundle:** motion libs loaded only on tenant surface (`.client.ts` plugin + `surface === 'tenant'`).

### Analytics (Vercel Web Analytics — DEV-110)

| Surface | `@vercel/analytics` |
|---|---|
| Admin / login (`app.*`) | ❌ Do not load |
| Platform (apex) | ✅ Yes |
| Public tenant (`{slug}.*`) | ✅ Yes |

Details: [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) section 9.5 · [BACKLOG DEV-110](./BACKLOG.md#dev-110--vercel-web-analytics).

---

## Per-tenant customization — public site (palette, landing, contact)

Yes — **same application**, different visual identity per client on the public site.

### 1. Color palette (CSS variables)

Tokens in `main.css` (`--op-color-primary`, etc.). Each tenant overrides via class on `<html>`:

```css
/* assets/css/themes/ana.css */
.theme-ana {
  --op-color-primary: #a0522d;
}
```

`useSurfaceHtmlClass()` in `app.vue` applies `theme-{slug}` on tenant. Themes in `assets/css/themes/{slug}.css` loaded automatically (`plugins/tenant-themes.ts`).

### 2. Different components per tenant (automatic by folder)

`utils/tenantComponentResolver.ts` uses `import.meta.glob` — **no manual registry**.

`useTenantComponent('LandingHero')` resolves `components/public/tenants/{slug}/LandingHero.vue` — **required per tenant** (no generic fallback).

```vue
<script setup lang="ts">
const LandingHero = useTenantComponent('LandingHero')
</script>

<template>
  <component :is="LandingHero" />
</template>
```

To add a new tenant with its own UI (beyond `ana` / `joao`):

1. Create folder `components/public/tenants/{slug}/`
2. Add `LandingHero.vue` and `ContactSection.vue` (and future page blocks)
3. Create `assets/css/themes/{slug}.css` (loaded automatically)
4. Extend `TenantComponentKey` in `types/frontend.ts` if it is a **new block type** (e.g. `GalleryGrid`)

Each active tenant in the repo needs the full folder. Slugs: `listTenantUiSlugs()` → `ana`, `joao`.

### 3. Shared pages, variable content

Routes like `/` and `/contact` are **the same URLs** on all tenants; middleware sets the slug and the page picks the right component. Ana and João can have completely different landings and contact forms without duplicating routes.

### 4. Login/admin (always dark, never per tenant)

`/login`, `/admin`, and `/platform/*` use `layouts/app.vue` + `components/app/` + `surface-dark`. **Do not** put tenant logic in login.

---

## Local dev — simulate hosts

On `localhost` there is no subdomain. Copy `frontend/.env.example` → `.env` and adjust (full comments in `.env.example`).

| `NUXT_PUBLIC_DEV_SURFACE` | Simulates in production | What to open |
|---|---|---|
| `dev` | — | http://localhost:3000 — DEV-005 skeleton (**dark**) |
| `tenant` | `{slug}.onlineportfolio.com.br` | `/` and `/contact` — artist theme (`ana`, `joao`) |
| `platform` | `onlineportfolio.com.br` | Marketing (**dark**) |
| `app` | `app.onlineportfolio.com.br` | `/login` — admin (**dark**) |

Ana public site example:

```text
NUXT_PUBLIC_DEV_SURFACE=tenant
NUXT_PUBLIC_DEV_TENANT_SLUG=ana
```

Restart dev server after changing `.env`.

---

## When to extract a component

Extract when **any** condition is true:

- Same markup/CSS appears in 2+ places.
- Block has clear props/slots (title, actions, list).
- Enables isolated test or story.
- Page exceeds ~80 lines of template.

**Do not** extract one-line helpers or wrappers with no real benefit.

---

## Patterns by layer

### `app.vue`

Minimal global shell:

```vue
<NuxtLoadingIndicator />
<NuxtRouteAnnouncer />
<NuxtLayout>
  <NuxtPage />
</NuxtLayout>
```

### `layouts/`

| Layout | Usage |
|---|---|
| `app.vue` | Admin/login on `app.*` |
| `platform.vue` | Marketing on `onlineportfolio.com.br` |
| `tenant.vue` | Public sites `{slug}.*` (+ per-tenant theme) |
| `default.vue` | Localhost skeleton |

Do not duplicate header/shell between pages — each layout composes its surface's components.

### `pages/`

- `definePageMeta` for layout/auth when needed.
- Data via composables or `useFetch`/`useAsyncData`.
- No duplicated global styles — use tokens in `assets/css/main.css` or scoped in `ui/` components.

### `components/ui/`

- Typed props with `defineProps`.
- Variants via prop (`variant="primary"`) or BEM classes (`ui-button--ghost`).
- Slots for flexible content (`default`, `title`, `actions`).
- Shared CSS variables (`--op-color-*` in `assets/css/main.css`).

### Composables

- One composable per domain (`usePlatformConfig`, `useRequestSurface`, `useTenantContext`, `useTenantComponent`).
- Return refs/computed; do not mix markup.

---

## Design tokens (CSS)

Global variables in `frontend/assets/css/main.css`, registered in `nuxt.config.ts` → `css: ['~/assets/css/main.css']`.

`ui/` components reference `var(--op-*)` — avoids repeating colors/fonts.

---

## Frontend PR checklist

- [ ] New page uses existing layout and correct surface components (Nuxt UI in admin; custom in tenant).
- [ ] No duplicated block that already exists as a component.
- [ ] Internal links with `<NuxtLink>`, not `<a href="/">`.
- [ ] Logic in composable, not inline in page.
- [ ] No secrets in `NUXT_PUBLIC_*` or `app.config.ts`.
- [ ] New tenant component is in `public/tenants/{slug}/`, not in `app/`.
- [ ] Login/admin does not import components from `public/tenants/`.
- [ ] Admin does not import GSAP/Lenis/Three/Vue Bits (ADR-018).
- [ ] Tenant motion respects `prefers-reduced-motion`.

---

## Repository references

| Artifact | Description |
|---|---|
| [ADR-018](./ADR-018-frontend-ui-motion-stack.md) | Simple admin (Nuxt UI) vs ultra animated tenant |
| [Vue Bits](https://vue-bits.dev/) | Animated component catalog — public tenant (jsrepo/shadcn) |
| [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) section 9.5 | Vercel Web Analytics (DEV-110) |
| [docs/ARCHITECTURE.md](./ARCHITECTURE.md) | Decision: three surfaces + UI per tenant |
| `frontend/middleware/resolve-host.global.ts` | Host → surface + slug |
| `frontend/composables/useTenantComponent.ts` | Ana* → Public* resolution |
| `frontend/components/` | app / platform / public / ui tree |
| `frontend/assets/css/themes/` | Palettes per tenant |
| `.cursor/rules/frontend-components.mdc` | Cursor agent rule |
| `.github/instructions/frontend.instructions.md` | Copilot/CI instructions for `frontend/**` |

See also [docs/ARCHITECTURE.md](./ARCHITECTURE.md) (frontend / BFF).
