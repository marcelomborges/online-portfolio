# ADR-018: Stack de UI e motion — admin simples vs tenant ultra animado

| | |
|---|---|
| **Status** | ✅ Aceito |
| **Data** | 2026-06-23 |
| **Decisores** | Operador / time do projeto |
| **Relacionado** | [ADR-016](./ARCHITECTURE.md#adr-016-três-superfícies-e-ui-por-tenant) · [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md) · Epic 1 (DEV-104+) |

---

## Contexto

O produto serve **dois públicos visuais opostos** na mesma app Nuxt:

1. **Operador e artistas (admin)** — login, CRUD, gestão: precisa ser **rápido, previsível, responsivo**, com formulários e tabelas consistentes.
2. **Visitantes do site público (`{slug}.*`)** — portfolio de artista: objetivo é **impacto visual**, motion fluido, identidade forte por cliente (referência: portfolios premium tipo jackiedroujko.com).

Misturar as duas filosofias no mesmo diretório de componentes ou no mesmo bundle global gera:

- Admin lento (Lenis, GSAP, WebGL desnecessários)
- Tenants genéricos demais (só Nuxt UI “corporativo”)
- Regressões de acessibilidade e manutenção

---

## Decisão

### Regra de ouro

| Superfície | Host / layout | Filosofia | Stack |
|---|---|---|---|
| **Admin + login** | `app.*` · `layouts/app.vue` · `components/app/` | **Mínimo motion** — utilitário, denso, WCAG-friendly | **Nuxt UI** + Tailwind |
| **Marketing plataforma** | apex · `layouts/platform.vue` | Simples e rápido; animação **leve** opcional | **Nuxt UI** + motion leve |
| **Site público tenant** | `{slug}.*` · `layouts/tenant.vue` · `public/tenants/{slug}/` | **Ultra animado** — scroll cinematográfico, identidade por slug | Stack motion dedicada (abaixo) |
| **Dev / error** | localhost · `error.vue` | Dark estrutural, sem motion pesado | Tokens `--op-*` / `surface-dark` |

**Login/admin nunca customiza por tenant.** Motion pesado **nunca** entra em `components/app/` ou rotas `/admin`, `/login`, `/platform/*`.

---

## Stack ideal (base Vue + Nuxt)

### Camada comum (toda a app)

| Pacote / módulo | Uso | Onde |
|---|---|---|
| **Nuxt 3.21** + **Vue 3.5** + **TypeScript strict** | Base | Global |
| **@nuxt/ui** v3 | Design system (forms, modals, tables, nav) | **Admin + platform** |
| **Tailwind CSS** (via Nuxt UI) | Layout utilitário | Admin + platform; tenant só se fizer sentido local |
| **@nuxt/image** | Imagens otimizadas (WebP/AVIF, lazy, sizes) | Principalmente **tenant** (galerias) |
| **@vueuse/core** | Helpers (`useMediaQuery`, `useIntersectionObserver`, …) | Global, leve |

### Admin / platform — “fast path”

| Objetivo | Como |
|---|---|
| Formulários e CRUD | Componentes **Nuxt UI** (`UButton`, `UInput`, `UTable`, `UModal`, …) |
| Dados | `useFetch` / `useAsyncData` via BFF `/api` |
| Motion | **Nenhum** Lenis/GSAP/WebGL; no máximo transições CSS curtas ou `prefers-reduced-motion` |
| Performance | Code-split por rota; **não** importar libs de motion no chunk do admin |
| Tema | `surface-dark` fixo — tokens Nuxt UI alinhados ao dark do produto |

### Tenant público — “wow path”

Stack recomendada **por slug** (opt-in em `public/tenants/{slug}/`, não global obrigatório):

| Prioridade | Pacote | Para quê |
|---|---|---|
| **P0** | **GSAP** + **ScrollTrigger** | Reveals no scroll, timelines, stagger, pin — padrão de portfolios premium |
| **P0** | **Lenis** | Smooth scroll fluido |
| **P1** | **@vueuse/motion** ou **Motion for Vue** | Micro-interações (hover, enter/leave) sem boilerplate |
| **P1** | **Variable fonts** (Fontsource ou Google Fonts) | Tipografia como identidade |
| **P2** | **Three.js** / **TresJS** (`@tresjs/core`) | Hero 3D / shaders — **só tenants que pedirem** |
| **P2** | **Rive** ou **Lottie** | Ilustrações animadas vetoriais |

**Composable sugerido (futuro):** `useTenantMotion()` — inicializa Lenis + GSAP apenas quando `surface === 'tenant'` e `prefers-reduced-motion` não está ativo.

---

## O que agrega mais na base Nuxt + Nuxt UI (tenants bonitos)

Ordem de impacto visual vs esforço:

1. **Tipografia forte** + espaço em branco — custo zero de lib
2. **@nuxt/image** — galerias rápidas e nítidas
3. **Lenis + GSAP ScrollTrigger** — 80% do “feel” premium
4. **Temas CSS por slug** (`themes/{slug}.css`) — paleta e ritmo já existentes no repo
5. **Componentes exclusivos por pasta** (`LandingHero.vue`, etc.) — layout único por artista
6. WebGL / Three — só quando o conceito exigir

Nuxt UI **não** é a alma do site tenant — é a **fábrica eficiente do admin**. Tenants usam CSS custom + motion stack acima.

---

## Regras de implementação

### Imports e pastas

```text
components/app/           → Nuxt UI; SEM gsap, lenis, three
components/platform/      → Nuxt UI; motion leve OK
components/public/tenants/{slug}/  → motion pesado OK (isolado por slug)
composables/useTenantMotion.ts      → só chamar em layout tenant ou páginas tenant
plugins/tenant-motion.client.ts     → registrar Lenis/GSAP só se surface=tenant
```

### Proibido

- GSAP / Lenis / Three em `components/app/` ou layouts `app.vue`
- `@nuxt/ui` como **única** identidade visual do site público do artista
- `v-html` com HTML de artista sem sanitização (motion não justifica bypass de segurança)
- Motion global no `nuxt.config` que carrega em admin

### Acessibilidade

- Respeitar **`prefers-reduced-motion: reduce`** — desligar Lenis e simplificar GSAP
- Admin: foco visível, labels, contraste (Nuxt UI + dark tokens)
- Tenant: animar **poucas coisas**, muito bem — não animar tudo

### Performance (tenant)

- Lazy-load GSAP plugins e Three por rota ou `defineAsyncComponent`
- Evitar animar `width`/`height`; preferir `transform` e `opacity`
- Medir LCP no hero — WebGL não pode bloquear first paint

---

## Alternativas rejeitadas

| Alternativa | Motivo |
|---|---|
| Uma lib de motion global para toda a app | Infla admin; artistas não precisam do mesmo motion |
| Só CSS `@keyframes` nos tenants | Insuficiente para scroll-driven premium |
| Deploy Nuxt separado por artista | Custo ops; já rejeitado em ADR-016 |
| Editor drag-and-drop no admin v1 | Escopo Fase 2+; código versionado por slug no v1 |
| Nuxt UI em tenants sem override | Sites ficariam genéricos demais |

---

## Consequências

| Positivo | Negativo / trade-off |
|---|---|
| Admin rápido e homogêneo com Nuxt UI | Cada tenant “premium” exige pasta no repo (PR) |
| Tenants podem rivalizar portfolios Awwwards | Bundle maior **por slug** se abusar de WebGL |
| Separação clara para agentes e reviews | Duas “mentalidades” de front no mesmo monorepo |
| Code-split natural admin vs tenant | Adoção de pacotes ainda pendente (Epic 1+) |

---

## Plano de adoção (backlog)

| Fase | Escopo | Issue sugerida |
|---|---|---|
| 1 | `@nuxt/ui` + Tailwind; admin/login stub com `UButton`/`UInput` | DEV-155+ (Epic 1.5) ou ticket dedicado |
| 2 | `@nuxt/image` em galeria tenant | DEV-105 |
| 3 | `useTenantMotion` + Lenis + GSAP em um tenant piloto (`ana`) | DEV-104 / extensão tenant UI |
| 4 | WebGL / Rive por demanda do cliente | Por tenant, sem ticket global |

---

## Referências

- [FRONTEND_COMPONENTS.md](./FRONTEND_COMPONENTS.md) — pastas, temas, dark mode
- [ARCHITECTURE.md](./ARCHITECTURE.md) — ADR-016 superfícies
- [Nuxt UI](https://ui.nuxt.com/)
- [GSAP ScrollTrigger](https://gsap.com/docs/v3/Plugins/ScrollTrigger/)
- [Lenis](https://github.com/darkroomengineering/lenis)
