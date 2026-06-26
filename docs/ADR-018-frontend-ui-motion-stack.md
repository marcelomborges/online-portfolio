# ADR-018: UI and motion stack — simple admin vs ultra animated tenant

| | |
|---|---|
| **Status** | ✅ Accepted |
| **Date** | 2026-06-23 |
| **Deciders** | Operator / project team |
| **Related** | [ADR-016](./ARCHITECTURE.md#adr-016-three-surfaces-and-per-tenant-ui) · [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md) · Epic 1 (DEV-104+) |

---

## Context

The product serves **two opposite visual audiences** in the same Nuxt app:

1. **Operator and artists (admin)** — login, CRUD, management: must be **fast, predictable, responsive**, with consistent forms and tables.
2. **Public site visitors (`{slug}.*`)** — artist portfolio: goal is **visual impact**, fluid motion, strong identity per client (reference: premium portfolios like jackiedroujko.com).

Mixing both philosophies in the same component directory or global bundle causes:

- Slow admin (unnecessary Lenis, GSAP, WebGL)
- Tenants that are too generic (Nuxt UI only, "corporate" look)
- Accessibility and maintenance regressions

---

## Decision

### Golden rule

| Surface | Host / layout | Philosophy | Stack |
|---|---|---|---|
| **Admin + login** | `app.*` · `layouts/app.vue` · `components/app/` | **Minimal motion** — utility, dense, WCAG-friendly | **Nuxt UI** + Tailwind |
| **Platform marketing** | apex · `layouts/platform.vue` | Simple and fast; **light** animation optional | **Nuxt UI** + light motion |
| **Public tenant site** | `{slug}.*` · `layouts/tenant.vue` · `public/tenants/{slug}/` | **Ultra animated** — cinematic scroll, identity per slug | Dedicated motion stack (below) |
| **Dev / error** | localhost · `error.vue` | Structural dark, no heavy motion | `--op-*` tokens / `surface-dark` |

**Login/admin never customizes per tenant.** Heavy motion **never** enters `components/app/` or `/admin`, `/login`, `/platform/*` routes.

---

## Ideal stack (Vue + Nuxt base)

### Common layer (entire app)

| Package / module | Usage | Where |
|---|---|---|
| **Nuxt 3.21** + **Vue 3.5** + **TypeScript strict** | Base | Global |
| **@nuxt/ui** v3 | Design system (forms, modals, tables, nav) | **Admin + platform** |
| **Tailwind CSS** (via Nuxt UI) | Utility layout | Admin + platform; tenant only if locally useful |
| **@nuxt/image** | Optimized images (WebP/AVIF, lazy, sizes) | Mainly **tenant** (galleries) |
| **@vueuse/core** | Helpers (`useMediaQuery`, `useIntersectionObserver`, …) | Global, lightweight |

### Admin / platform — "fast path"

| Goal | How |
|---|---|
| Forms and CRUD | **Nuxt UI** components (`UButton`, `UInput`, `UTable`, `UModal`, …) |
| Data | `useFetch` / `useAsyncData` via BFF `/api` |
| Motion | **None** Lenis/GSAP/WebGL; at most short CSS transitions or `prefers-reduced-motion` |
| Performance | Code-split by route; **do not** import motion libs in admin chunk |
| Theme | Fixed `surface-dark` — Nuxt UI tokens aligned with product dark |

### Public tenant — "wow path"

Recommended stack **per slug** (opt-in in `public/tenants/{slug}/`, not globally mandatory):

| Priority | Package | Purpose |
|---|---|---|
| **P0** | **GSAP** + **ScrollTrigger** | Scroll reveals, timelines, stagger, pin — premium portfolio standard |
| **P0** | **Lenis** | Fluid smooth scroll |
| **P1** | **[@vueuse/motion](https://motion.vueuse.org/)** or **Motion for Vue** | Micro-interactions (hover, enter/leave) without boilerplate |
| **P1** | **[Vue Bits](https://vue-bits.dev/)** | Animated component/background catalog (Vue 3 + Tailwind) — copy via `jsrepo`/`shadcn` to `public/tenants/{slug}/` |
| **P1** | **Variable fonts** (Fontsource or Google Fonts) | Typography as identity |
| **P2** | **Three.js** / **TresJS** (`@tresjs/core`) | 3D hero / shaders — **only tenants that request it** |
| **P2** | **Rive** or **Lottie** | Vector animated illustrations |

**Suggested composable (future):** `useTenantMotion()` — initializes Lenis + GSAP only when `surface === 'tenant'` and `prefers-reduced-motion` is not active.

---

## What adds most on Nuxt + Nuxt UI base (beautiful tenants)

Visual impact vs effort order:

1. **Strong typography** + whitespace — zero lib cost
2. **@nuxt/image** — fast, sharp galleries
3. **Lenis + GSAP ScrollTrigger** — 80% of premium "feel"
4. **CSS themes per slug** (`themes/{slug}.css`) — palette and rhythm already in repo
5. **Exclusive components per folder** (`LandingHero.vue`, etc.) — unique layout per artist
6. **[Vue Bits](https://vue-bits.dev/)** — backgrounds, text effects, animated UI (130+); install per component with `jsrepo`/`shadcn`, not globally in admin
7. WebGL / Three — only when the concept requires it

Nuxt UI is **not** the soul of the tenant site — it is the **efficient admin factory**. Tenants use custom CSS + motion stack above.

---

## Implementation rules

### Imports and folders

```text
components/app/           → Nuxt UI; NO gsap, lenis, three
components/platform/      → Nuxt UI; light motion OK
components/public/tenants/{slug}/  → heavy motion OK (isolated per slug); Vue Bits OK here
composables/useTenantMotion.ts      → call only in tenant layout or tenant pages
plugins/tenant-motion.client.ts     → register Lenis/GSAP only if surface=tenant
```

### Vue Bits (catalog — opt-in per tenant)

[Vue Bits](https://vue-bits.dev/) is an **open source** library of animated Vue components (backgrounds, text effects, animations, UI patterns). It does **not** replace Nuxt UI in admin.

| Where to use | How |
|---|---|
| **`public/tenants/{slug}/`** | ✅ Primary — hero, backgrounds, text shine, etc. |
| **`components/platform/`** | ⚠️ Optional, light motion (1–2 blocks) |
| **`components/app/`** | ❌ Forbidden |

**Adoption:** install **per component** in repo (versioned in Git), not as mandatory global dependency:

```bash
# Example (jsrepo) — run in frontend/; adjust destination path to tenants/{slug}/
npx jsrepo add https://vue-bits.dev/r/aurora.json
```

Alternative: shadcn-compatible CLI (see site docs). After copying, adapt `--op-*` / `theme-{slug}` tokens and respect `prefers-reduced-motion`.

**Review:** treat copied code as a normal PR — no unsafe `v-html`; do not expose secrets; lazy-load on tenant routes when heavy.

### Forbidden

- GSAP / Lenis / Three in `components/app/` or `app.vue` layouts
- `@nuxt/ui` as the **only** visual identity of the artist public site
- `v-html` with artist HTML without sanitization (motion does not justify security bypass)
- Global motion in `nuxt.config` that loads in admin

### Accessibility

- Respect **`prefers-reduced-motion: reduce`** — disable Lenis and simplify GSAP
- Admin: visible focus, labels, contrast (Nuxt UI + dark tokens)
- Tenant: animate **few things**, very well — do not animate everything

### Performance (tenant)

- Lazy-load GSAP plugins and Three per route or `defineAsyncComponent`
- Avoid animating `width`/`height`; prefer `transform` and `opacity`
- Measure LCP on hero — WebGL must not block first paint

### Analytics (Vercel Web Analytics — DEV-110)

| Surface | `@vercel/analytics` |
|---|---|
| Admin / login (`app.*`) | ❌ Do not load |
| Platform (apex) | ✅ Yes |
| Public tenant (`{slug}.*`) | ✅ Yes |

Runbook: [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) section 9.5 · [BACKLOG DEV-110](./BACKLOG.md#dev-110--vercel-web-analytics).

---

## Rejected alternatives

| Alternative | Reason |
|---|---|
| One global motion lib for entire app | Inflates admin; artists do not need the same motion |
| CSS `@keyframes` only on tenants | Insufficient for premium scroll-driven effects |
| Separate Nuxt deploy per artist | Ops cost; already rejected in ADR-016 |
| Drag-and-drop editor in admin v1 | Phase 2+ scope; versioned code per slug in v1 |
| Nuxt UI on tenants without override | Sites would be too generic |

---

## Consequences

| Positive | Negative / trade-off |
|---|---|
| Fast, homogeneous admin with Nuxt UI | Each "premium" tenant requires a repo folder (PR) |
| Tenants can rival Awwwards portfolios | Larger bundle **per slug** if WebGL is overused |
| Clear separation for agents and reviews | Two front-end "mindsets" in same monorepo |
| Natural code-split admin vs tenant | Package adoption still pending (Epic 1+) |

---

## Adoption plan (backlog)

| Phase | Scope | Suggested issue |
|---|---|---|
| 1 | `@nuxt/ui` + Tailwind; admin/login stub with `UButton`/`UInput` | DEV-155+ (Epic 1.5) or dedicated ticket |
| 2 | `@nuxt/image` in tenant gallery | DEV-105 |
| 3 | `useTenantMotion` + Lenis + GSAP in one pilot tenant (`ana`) | DEV-104 / tenant UI extension |
| 3b | Vue Bits (1–2 pilot components in `public/tenants/ana/`) | Per tenant, ref. [vue-bits.dev](https://vue-bits.dev/) |
| 4 | WebGL / Rive on client demand | Per tenant, no global ticket |

---

## References

- [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md) — folders, themes, dark mode
- [ARCHITECTURE.md](./ARCHITECTURE.md) — ADR-016 surfaces
- [Nuxt UI](https://ui.nuxt.com/)
- [Vue Bits](https://vue-bits.dev/) — animated Vue 3 + Tailwind components/backgrounds (public tenant)
- [GSAP ScrollTrigger](https://gsap.com/docs/v3/Plugins/ScrollTrigger/)
- [Lenis](https://github.com/darkroomengineering/lenis)
