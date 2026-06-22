# Frontend — componentes e reutilização

Prioridade do projeto: **evitar código repetitivo** usando componentes Vue reutilizáveis e recursos nativos do Nuxt 3.

**Decisão arquitetural:** [ARCHITECTURE.md § ADR-016](./ARCHITECTURE.md#adr-016-três-superfícies-e-ui-por-tenant) — três superfícies (`app` / `platform` / `tenant`), UI pública customizável por slug, login/admin padronizado.

Referência operacional (humanos e agentes). Stack: Nuxt 3.21 + Vue 3 + TypeScript strict.

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

### @nuxt/ui (não adotado ainda)

O ecossistema Nuxt inclui [@nuxt/ui](https://ui.nuxt.com/) (Tailwind + Headless UI). **Não está no projeto hoje.** Antes de adicionar:

- Avaliar alinhamento com design system do produto.
- Registrar decisão em `ARCHITECTURE.md`.
- Preferir `@nuxt/ui` a copiar estilos de botão/card/modal manualmente **depois** da adoção.

Até lá, usar componentes locais em `components/ui/` (`UiButton`, `UiCard`, …).

---

## Estrutura de pastas — três superfícies + tenants

**Uma app Nuxt**, três modos de UI distintos (resolvidos pelo `Host`):

| Superfície | Host | Layout | Componentes |
|---|---|---|---|
| **App (admin/login)** | `app.onlineportfolio.com.br` | `layouts/app.vue` | `components/app/` — **padronizado para todos** |
| **Plataforma** | `onlineportfolio.com.br` | `layouts/platform.vue` | `components/platform/` |
| **Público tenant** | `{slug}.onlineportfolio.com.br` | `layouts/tenant.vue` | `components/public/shared/` + `components/public/tenants/{slug}/` |
| **Dev skeleton** | `localhost` (default) | `layouts/default.vue` | `components/dev/` |

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
│       ├── shared/            # Fallbacks: LandingHero.vue, ContactSection.vue, …
│       └── tenants/
│           ├── ana/           # Exemplo no repo
│           ├── joao/          # Exemplo no repo (landing + contato custom)
│           └── maria/         # Exemplo doc — novo cliente (ver § Onboard maria)
├── layouts/
│   ├── app.vue                # app.*
│   ├── platform.vue           # apex
│   ├── tenant.vue             # {slug}.*
│   └── default.vue            # localhost / dev
├── assets/css/
│   ├── main.css
│   ├── surfaces/
│   │   └── app.css            # dark mode fixo — área privada
│   └── themes/                # por artista (site público)
├── middleware/
│   └── resolve-host.global.ts # Host → surface + tenant slug
└── composables/
    ├── useRequestSurface.ts   # app | platform | tenant | dev
    ├── useSurfaceHtmlClass.ts # surface-app | theme-{slug} no <html>
    ├── useTenantContext.ts    # slug (só em tenant público)
    └── useTenantComponent.ts  # resolve por slug + nome do arquivo (import.meta.glob)
```

**Convenção de nomes (site público)**

| Local | Nome do arquivo | Resolvido por |
|---|---|---|
| `public/shared/` | `LandingHero.vue` | Fallback quando o slug não tem override |
| `public/tenants/ana/` | `LandingHero.vue` | Slug `ana` |
| `public/tenants/joao/` | `ContactSection.vue` | Slug `joao` — só o blocos que existirem na pasta |
| `public/tenants/maria/` | `LandingHero.vue` | Slug `maria` — exemplo de onboarding (§ abaixo) |

**Sem prefixo Ana/Joao/Public no filename** — o slug vem da **pasta** (`tenants/{slug}/`).

**Admin/login (`components/app/`):** UI única em `app.onlineportfolio.com.br`. Tenant ativo vem do **usuário logado** (`/auth/me`), não do hostname. **Dark mode fixo** em toda a área privada — ver § Área privada (dark mode).

---

## Área privada — dark mode fixo

**Decisão:** login, admin (`/admin`), platform admin (`/platform/*`) e qualquer rota em `app.onlineportfolio.com.br` usam **dark mode completo**, padronizado, **sem** customização por tenant.

| Superfície | Tema | Classe `<html>` | CSS |
|---|---|---|---|
| **App (privado)** | Dark fixo | `surface-app` | `assets/css/surfaces/app.css` |
| **Tenant público** | Por artista | `theme-{slug}` | `assets/css/themes/{slug}.css` |
| **Platform / dev** | Light (default) | — | `assets/css/main.css` |

Aplicado globalmente por `useSurfaceHtmlClass()` em `app.vue` conforme `useRequestSurface()`.

**Regras:**

- Componentes em `components/app/` e páginas com `layout: 'app'` herdam tokens `--op-*` do dark theme.
- **Não** criar `themes/` por tenant para admin — não existe `theme-ana` no app.
- **Não** expor toggle claro/escuro no admin no v1.
- Site público (`{slug}.*`) continua 100% sob controle do artista (paletas + componentes por pasta).

Preview local: `NUXT_PUBLIC_DEV_SURFACE=app` → http://localhost:3000/login (fundo escuro).

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

`useTenantComponent('LandingHero')` resolve:

1. `components/public/tenants/{slug}/LandingHero.vue`
2. fallback `components/public/shared/LandingHero.vue`

```vue
<script setup lang="ts">
const LandingHero = useTenantComponent('LandingHero')
</script>

<template>
  <component :is="LandingHero" />
</template>
```

Para adicionar um novo cliente com UI própria:

1. Criar pasta `components/public/tenants/{slug}/`
2. Adicionar blocos nomeados (`LandingHero.vue`, `ContactSection.vue`, …)
3. Criar `assets/css/themes/{slug}.css` (carregado automaticamente)
4. Estender `TenantComponentKey` em `types/frontend.ts` se for um **novo tipo** de bloco (ex.: `GalleryGrid`)

Tenant **sem** pasta custom usa só `public/shared/`. Slugs com pasta no repo: `listTenantUiSlugs()` → `ana`, `joao` (e `maria` após criar os arquivos abaixo).

### Exemplo — onboard do tenant `maria`

Novo artista sem código duplicado de ana/joao — só pasta + tema:

```text
components/public/tenants/maria/
  LandingHero.vue          # opcional — senão usa shared/LandingHero.vue
  ContactSection.vue       # opcional

assets/css/themes/maria.css
```

```css
/* assets/css/themes/maria.css */
.theme-maria {
  --op-color-primary: #7c3aed;
  --op-color-primary-hover: #6d28d9;
  --op-color-surface: #faf5ff;
}
```

Preview local no `.env`:

```text
NUXT_PUBLIC_DEV_SURFACE=tenant
NUXT_PUBLIC_DEV_TENANT_SLUG=maria
```

Em produção: `maria.onlineportfolio.com.br` (após DNS + DEV-104 validar slug na API).

### 3. Páginas compartilhadas, conteúdo variável

Rotas como `/` e `/contact` são **as mesmas URLs** em todos os tenants; o middleware define o slug e a página escolhe o componente certo. Ana, João e Maria podem ter landings e formulários de contato totalmente diferentes sem duplicar rotas.

### 4. Login/admin permanece igual (e sempre dark)

`/login`, `/admin` e `/platform/*` em `app.onlineportfolio.com.br` usam `layouts/app.vue` + `components/app/` + classe `surface-app`. **Não** colocar lógica de tenant no login nem tema claro por artista.

---

## Dev local — simular hosts

Em `localhost` não há subdomínio. Copie `frontend/.env.example` → `.env` e ajuste (comentários completos no `.env.example`).

| `NUXT_PUBLIC_DEV_SURFACE` | Simula em produção | O que abrir |
|---|---|---|
| `dev` | — | http://localhost:3000 — skeleton DEV-005 |
| `tenant` | `{slug}.onlineportfolio.com.br` | `/` e `/contact` do slug (`NUXT_PUBLIC_DEV_TENANT_SLUG`) |
| `platform` | `onlineportfolio.com.br` | Marketing da plataforma |
| `app` | `app.onlineportfolio.com.br` | `/login` — admin padronizado, **dark mode fixo** |

Exemplo site público da Ana:

```text
NUXT_PUBLIC_DEV_SURFACE=tenant
NUXT_PUBLIC_DEV_TENANT_SLUG=ana
```

Exemplo novo tenant Maria (após criar pasta `tenants/maria/` — ver § Onboard maria):

```text
NUXT_PUBLIC_DEV_SURFACE=tenant
NUXT_PUBLIC_DEV_TENANT_SLUG=maria
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

- [ ] Página nova usa layout existente e componentes `ui/` quando aplicável.
- [ ] Nenhum bloco duplicado que já exista como componente.
- [ ] Links internos com `<NuxtLink>`, não `<a href="/">`.
- [ ] Lógica em composable, não inline na página.
- [ ] Sem secrets em `NUXT_PUBLIC_*` ou `app.config.ts`.
- [ ] Componente de tenant novo está em `public/tenants/{slug}/`, não em `app/`.
- [ ] Login/admin não importa componentes de `public/tenants/`.

---

## Referências no repositório

| Artefato | Descrição |
|---|---|
| [ARCHITECTURE.md § ADR-016](./ARCHITECTURE.md#adr-016-três-superfícies-e-ui-por-tenant) | Decisão: três superfícies + UI por tenant |
| `frontend/middleware/resolve-host.global.ts` | Host → surface + slug |
| `frontend/composables/useTenantComponent.ts` | Resolução Ana* → Public* |
| `frontend/components/` | Árvore app / platform / public / ui |
| `frontend/assets/css/themes/` | Paletas por tenant |
| `.cursor/rules/frontend-components.mdc` | Regra para agentes Cursor |
| `.github/instructions/frontend.instructions.md` | Instruções Copilot/CI para `frontend/**` |

Ver também [ARCHITECTURE.md §5](./ARCHITECTURE.md#5-frontend-nuxt--vercel) (frontend / BFF).
