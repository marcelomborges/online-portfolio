# Frontend — componentes e reutilização

Prioridade do projeto: **evitar código repetitivo** usando componentes Vue reutilizáveis e recursos nativos do Nuxt 3.

**Decisão arquitetural:** [docs/ARCHITECTURE.md](./ARCHITECTURE.md) — três superfícies (`app` / `platform` / `tenant`), UI pública customizável por slug, login/admin padronizado.

**UI e motion (admin vs tenant):** [ADR-018](./ADR-018-frontend-ui-motion-stack.md) — admin **simples + Nuxt UI**; site `{slug}.*` **ultra animado**.

Referência operacional (humanos e agentes). Stack base: Nuxt 3.21 + Vue 3 + TypeScript strict.

---

## Princípios

1. **Páginas finas** — `pages/` só orquestram layout, dados e composables; markup repetível vai para `components/`.
2. **Composables para lógica** — estado, fetch, helpers em `composables/` (ex.: `usePlatformConfig`).
3. **Componentes para UI** — qualquer bloco usado 2+ vezes ou com responsabilidade clara vira componente.
4. **Nuxt built-ins primeiro** — preferir `<NuxtLink>`, `<NuxtLayout>`, `<NuxtPage>`, `<NuxtLoadingIndicator>`, `<NuxtRouteAnnouncer>`, `<NuxtImg>`, `<NuxtTime>`, etc., em vez de reinventar.
5. **Auto-import** — Nuxt registra `components/`, `composables/` e `utils/` automaticamente; não importar manualmente salvo exceção (ex.: tipos em `error.vue`).

---

## O que é “componente pronto do Nuxt”

| Recurso | Uso |
|---|---|
| **Componentes Vue built-in** | `NuxtLink`, `NuxtLayout`, `NuxtPage`, `NuxtLoadingIndicator`, `NuxtRouteAnnouncer` |
| **Auto-import de `components/`** | Prefixo por pasta: `components/ui/UiButton.vue` → `<UiButton />` |
| **Composables** | `useRuntimeConfig`, `useAppConfig`, `useFetch`, `useAsyncData`, `useRoute`, `useRouter` |
| **Layouts** | `layouts/default.vue`, `layouts/admin.vue` — shell compartilhado |
| **`app.config.ts`** | Metadados estáticos (nome do site, links) — não secrets |
| **`#components` / `resolveComponent`** | Só quando precisar de componente dinâmico |

### @nuxt/ui — admin e platform (decisão ADR-018)

**Adotado como design system** para `app.*`, `/admin`, `/login` e marketing da plataforma (apex). Objetivo: **rápido, responsivo, mínimo motion**.

| Superfície | Nuxt UI | Motion |
|---|---|---|
| **Admin / login** (`components/app/`) | ✅ Sim — forms, tables, modals | ❌ Não (sem Lenis/GSAP/WebGL) |
| **Platform** (`components/platform/`) | ✅ Sim | ⚠️ Leve opcional (CSS / micro) |
| **Tenant público** (`public/tenants/{slug}/`) | ❌ Não como identidade principal | ✅ **Ultra animado** — GSAP, Lenis, etc. |

Detalhes da stack tenant: [ADR-018](./ADR-018-frontend-ui-motion-stack.md).

Até a instalação dos pacotes (Epic 1+), primitivos locais em `components/ui/` (`UiButton`, `UiCard`, …) com tokens `--op-*`.

**Regra:** não importar GSAP/Lenis/Three em `components/app/`. Não usar Nuxt UI como “cara” do site do artista — cada slug tem componentes e tema próprios.

---

## Estrutura de pastas — três superfícies + tenants

**Uma app Nuxt**, três modos de UI distintos (resolvidos pelo `Host`):

| Superfície | Host | Layout | Componentes | UI / motion |
|---|---|---|---|---|
| **App (admin/login)** | `app.onlineportfolio.com.br` | `layouts/app.vue` | `components/app/` — **padronizado** | Nuxt UI · **sem motion pesado** |
| **Plataforma** | `onlineportfolio.com.br` | `layouts/platform.vue` | `components/platform/` | Nuxt UI · motion leve |
| **Público tenant** | `{slug}.onlineportfolio.com.br` | `layouts/tenant.vue` | `public/shared/` + `public/tenants/{slug}/` | Custom por slug · **ultra animado** |
| **Dev skeleton** | `localhost` (default) | `layouts/default.vue` | `components/dev/` | Dark estrutural |

```text
frontend/
├── components/
│   ├── ui/                    # Primitivos globais (UiButton, UiCard) — usam --op-* tokens
│   ├── error/
│   ├── dev/                   # Shell do skeleton local
│   ├── app/                   # Login + admin — NUNCA customizar por tenant
│   │   └── layout/
│   ├── platform/              # Marketing da plataforma (apex)
│   └── public/
│       ├── shared/            # Orquestração (TenantHome.vue) — não blocos de página
│       └── tenants/
│           ├── ana/           # LandingHero + ContactSection
│           └── joao/          # LandingHero + ContactSection
├── layouts/
│   ├── app.vue                # app.* — admin/login
│   ├── platform.vue           # apex — marketing
│   ├── tenant.vue             # {slug}.* — site público do artista
│   ├── structural.vue         # error + shells sem header tenant
│   └── default.vue            # localhost / dev skeleton
├── assets/css/
│   ├── main.css
│   ├── surfaces/
│   │   └── dark.css           # dark mode — tudo exceto site público tenant
│   └── themes/                # ana.css, joao.css — só site público
├── middleware/
│   └── resolve-host.global.ts # Host → surface + tenant slug
└── composables/
    ├── useRequestSurface.ts   # app | platform | tenant | dev
    ├── useSurfaceHtmlClass.ts # surface-dark | theme-{slug} no <html>
    ├── useTenantContext.ts    # slug (só em tenant público)
    └── useTenantComponent.ts  # resolve por slug + nome do arquivo (import.meta.glob)
```

**Convenção de nomes (site público)**

| Local | Nome do arquivo | Resolvido por |
|---|---|---|
| `public/tenants/ana/` | `LandingHero.vue`, `ContactSection.vue` | Slug `ana` |
| `public/tenants/joao/` | `LandingHero.vue`, `ContactSection.vue` | Slug `joao` |
| `public/shared/` | `TenantHome.vue` | Orquestração da landing (`/` tenant) |

**Sem prefixo no filename** — o slug vem da **pasta** (`tenants/{slug}/`). Landing e contato são **exclusivos por tenant** (sem fallback genérico). Tenants no repo: **ana** e **joao** (seed DEV-002).

**Admin/login (`components/app/`):** UI única em `app.onlineportfolio.com.br`. Tenant ativo vem do **usuário logado** (`/auth/me`), não do hostname. Ver docs/FRONTEND_COMPONENTS.md

---

## Dark mode estrutural (tudo exceto site público do tenant)

**Decisão:** somente as **páginas exclusivas do site público do artista** (`surface=tenant`, layout `tenant`, rotas como `/` e `/contact` em `{slug}.*`) usam tema claro/customizável (`theme-{slug}`). **Todo o resto** usa **dark mode completo** (`surface-dark`).

| Contexto | Tema | Classe `<html>` |
|---|---|---|
| **Site público tenant** (`ana.*`, `joao.*`) | Por artista | `theme-{slug}` |
| **Admin, login, platform, dev skeleton** | Dark fixo | `surface-dark` |
| **Páginas de erro** (`error.vue`) | Dark fixo | `surface-dark` (mesmo em host de tenant) |
| **Layouts estruturais** (`app`, `platform`, `default`, `structural`) | Dark fixo | `surface-dark` |
| **Componentes `app/*`** e `ui/*` em telas admin | Dark fixo | herdam `surface-dark` |

CSS: `assets/css/surfaces/dark.css`. Aplicado por `useSurfaceHtmlClass()` em `app.vue`.

**O que entra no dark:**

- `app.onlineportfolio.com.br` — login, `/admin`, `/platform/*`
- `onlineportfolio.com.br` — marketing da plataforma
- Modo `dev` no localhost — skeleton
- `error.vue` + `components/error/`
- Futuras telas admin compartilhadas em `components/app/`

**O que NÃO entra (só tenant público):**

- `components/public/tenants/{slug}/` — blocos de página por artista
- `assets/css/themes/{slug}.css` — paleta do artista

**Regras:**

- **Não** customizar dark por tenant no admin.
- **Não** toggle claro/escuro no v1.
- Novos tenants no repo: pasta `tenants/{slug}/` + `themes/{slug}.css` (hoje: `ana`, `joao`).

Preview: `NUXT_PUBLIC_DEV_SURFACE=app` → `/login` escuro · `tenant` + `ana` → landing clara/custom animada.

---

## Motion e performance (ADR-018)

| Onde | O que pode | O que evitar |
|---|---|---|
| `components/app/` | Transições CSS curtas; Nuxt UI defaults | Lenis, GSAP, ScrollTrigger, Three, Rive |
| `components/platform/` | Fade/slide leve | Scroll hijacking, WebGL |
| `public/tenants/{slug}/` | GSAP + ScrollTrigger, Lenis, @vueuse/motion, WebGL opt-in | Importar de `components/app/` para “reaproveitar botão” |
| `layouts/tenant.vue` | Inicializar smooth scroll / motion composable | Carregar motion no layout `app.vue` |

**Acessibilidade:** `prefers-reduced-motion: reduce` desliga Lenis e simplifica animações tenant.

**Bundle:** motion libs carregadas só na superfície tenant (plugin `.client.ts` + `surface === 'tenant'`).

### Analytics (Vercel Web Analytics — DEV-110)

| Superfície | `@vercel/analytics` |
|---|---|
| Admin / login (`app.*`) | ❌ Não carregar |
| Platform (apex) | ✅ Sim |
| Tenant público (`{slug}.*`) | ✅ Sim |

Detalhes: [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) seção 9.5 · [BACKLOG DEV-110](./BACKLOG.md#dev-110--vercel-web-analytics).

---

## Customização por tenant — site público (paleta, landing, contato)

Sim — **mesma aplicação**, identidade visual diferente por cliente no site público.

### 1. Paleta de cores (CSS variables)

Tokens em `main.css` (`--op-color-primary`, etc.). Cada tenant sobrescreve via classe no `<html>`:

```css
/* assets/css/themes/ana.css */
.theme-ana {
  --op-color-primary: #a0522d;
}
```

`useSurfaceHtmlClass()` em `app.vue` aplica `theme-{slug}` no tenant. Temas em `assets/css/themes/{slug}.css` carregados automaticamente (`plugins/tenant-themes.ts`).

### 2. Componentes diferentes por tenant (automático por pasta)

`utils/tenantComponentResolver.ts` usa `import.meta.glob` — **sem registry manual**.

`useTenantComponent('LandingHero')` resolve `components/public/tenants/{slug}/LandingHero.vue` — **obrigatório por tenant** (sem fallback genérico).

```vue
<script setup lang="ts">
const LandingHero = useTenantComponent('LandingHero')
</script>

<template>
  <component :is="LandingHero" />
</template>
```

Para adicionar um novo tenant com UI própria (além de `ana` / `joao`):

1. Criar pasta `components/public/tenants/{slug}/`
2. Adicionar `LandingHero.vue` e `ContactSection.vue` (e futuros blocos por página)
3. Criar `assets/css/themes/{slug}.css` (carregado automaticamente)
4. Estender `TenantComponentKey` em `types/frontend.ts` se for um **novo tipo** de bloco (ex.: `GalleryGrid`)

Cada tenant ativo no repo precisa da pasta completa. Slugs: `listTenantUiSlugs()` → `ana`, `joao`.

### 3. Páginas compartilhadas, conteúdo variável

Rotas como `/` e `/contact` são **as mesmas URLs** em todos os tenants; o middleware define o slug e a página escolhe o componente certo. Ana e João podem ter landings e formulários de contato totalmente diferentes sem duplicar rotas.

### 4. Login/admin (sempre dark, nunca por tenant)

`/login`, `/admin` e `/platform/*` usam `layouts/app.vue` + `components/app/` + `surface-dark`. **Não** colocar lógica de tenant no login.

---

## Dev local — simular hosts

Em `localhost` não há subdomínio. Copie `frontend/.env.example` → `.env` e ajuste (comentários completos no `.env.example`).

| `NUXT_PUBLIC_DEV_SURFACE` | Simula em produção | O que abrir |
|---|---|---|
| `dev` | — | http://localhost:3000 — skeleton DEV-005 (**dark**) |
| `tenant` | `{slug}.onlineportfolio.com.br` | `/` e `/contact` — tema do artista (`ana`, `joao`) |
| `platform` | `onlineportfolio.com.br` | Marketing (**dark**) |
| `app` | `app.onlineportfolio.com.br` | `/login` — admin (**dark**) |

Exemplo site público da Ana:

```text
NUXT_PUBLIC_DEV_SURFACE=tenant
NUXT_PUBLIC_DEV_TENANT_SLUG=ana
```

Reinicie o dev server após mudar o `.env`.

---

## Quando extrair componente

Extrair quando **qualquer** condição for verdadeira:

- Mesmo markup/CSS aparece em 2+ lugares.
- Bloco tem props/slots claros (título, ações, lista).
- Facilita teste ou story isolada.
- Página passa de ~80 linhas de template.

**Não** extrair helpers de uma linha ou wrappers sem ganho real.

---

## Padrões por camada

### `app.vue`

Shell global mínimo:

```vue
<NuxtLoadingIndicator />
<NuxtRouteAnnouncer />
<NuxtLayout>
  <NuxtPage />
</NuxtLayout>
```

### `layouts/`

| Layout | Uso |
|---|---|
| `app.vue` | Admin/login em `app.*` |
| `platform.vue` | Marketing em `onlineportfolio.com.br` |
| `tenant.vue` | Sites públicos `{slug}.*` (+ tema por tenant) |
| `default.vue` | Skeleton localhost |

Não duplicar header/shell entre páginas — cada layout compõe os componentes da sua superfície.

### `pages/`

- `definePageMeta` para layout/auth quando necessário.
- Dados via composables ou `useFetch`/`useAsyncData`.
- Sem estilos globais duplicados — usar tokens em `assets/css/main.css` ou scoped nos componentes `ui/`.

### `components/ui/`

- Props tipadas com `defineProps`.
- Variantes via prop (`variant="primary"`) ou classes BEM (`ui-button--ghost`).
- Slots para conteúdo flexível (`default`, `title`, `actions`).
- CSS variables compartilhadas (`--op-color-*` em `assets/css/main.css`).

### Composables

- Um composable por domínio (`usePlatformConfig`, `useRequestSurface`, `useTenantContext`, `useTenantComponent`).
- Retornar refs/computed; não misturar markup.

---

## Tokens de design (CSS)

Variáveis globais em `frontend/assets/css/main.css`, registradas em `nuxt.config.ts` → `css: ['~/assets/css/main.css']`.

Componentes `ui/` referenciam `var(--op-*)` — evita repetir cores/fontes.

---

## Checklist para PRs frontend

- [ ] Página nova usa layout existente e componentes corretos da superfície (Nuxt UI no admin; custom no tenant).
- [ ] Nenhum bloco duplicado que já exista como componente.
- [ ] Links internos com `<NuxtLink>`, não `<a href="/">`.
- [ ] Lógica em composable, não inline na página.
- [ ] Sem secrets em `NUXT_PUBLIC_*` ou `app.config.ts`.
- [ ] Componente de tenant novo está em `public/tenants/{slug}/`, não em `app/`.
- [ ] Login/admin não importa componentes de `public/tenants/`.
- [ ] Admin não importa GSAP/Lenis/Three (ADR-018).
- [ ] Motion tenant respeita `prefers-reduced-motion`.

---

## Referências no repositório

| Artefato | Descrição |
|---|---|
| [ADR-018](./ADR-018-frontend-ui-motion-stack.md) | Admin simples (Nuxt UI) vs tenant ultra animado |
| [EXTERNAL_PROVIDERS.md](./EXTERNAL_PROVIDERS.md) seção 9.5 | Vercel Web Analytics (DEV-110) |
| [docs/ARCHITECTURE.md](./ARCHITECTURE.md) | Decisão: três superfícies + UI por tenant |
| `frontend/middleware/resolve-host.global.ts` | Host → surface + slug |
| `frontend/composables/useTenantComponent.ts` | Resolução Ana* → Public* |
| `frontend/components/` | Árvore app / platform / public / ui |
| `frontend/assets/css/themes/` | Paletas por tenant |
| `.cursor/rules/frontend-components.mdc` | Regra para agentes Cursor |
| `.github/instructions/frontend.instructions.md` | Instruções Copilot/CI para `frontend/**` |

Ver também [docs/ARCHITECTURE.md](./ARCHITECTURE.md) (frontend / BFF).
