import type { Component } from 'vue'

type VueModule = { default: Component }

/** Not resolved via slug — orchestration / layout helpers in `shared/`. */
const SHARED_ORCHESTRATION = new Set(['TenantHome'])

const tenantModules = import.meta.glob<VueModule>(
  '~/components/public/tenants/*/*.vue',
  { eager: true },
)

const sharedModules = import.meta.glob<VueModule>(
  '~/components/public/shared/*.vue',
  { eager: true },
)

function normalizePath(path: string): string {
  return path.replace(/\\/g, '/')
}

function parseTenantEntry(path: string): { slug: string; name: string } | null {
  const normalized = normalizePath(path)
  const match = normalized.match(/\/tenants\/([^/]+)\/([^/]+)\.vue$/)
  if (!match) {
    return null
  }
  return { slug: match[1], name: match[2] }
}

function parseSharedEntry(path: string): string | null {
  const normalized = normalizePath(path)
  const match = normalized.match(/\/shared\/([^/]+)\.vue$/)
  if (!match) {
    return null
  }
  return match[1]
}

function buildTenantComponentMaps() {
  const bySlug = new Map<string, Map<string, Component>>()
  const shared = new Map<string, Component>()

  for (const [path, module] of Object.entries(tenantModules)) {
    const parsed = parseTenantEntry(path)
    if (!parsed) {
      continue
    }
    if (!bySlug.has(parsed.slug)) {
      bySlug.set(parsed.slug, new Map())
    }
    bySlug.get(parsed.slug)!.set(parsed.name, module.default)
  }

  for (const [path, module] of Object.entries(sharedModules)) {
    const name = parseSharedEntry(path)
    if (!name || SHARED_ORCHESTRATION.has(name)) {
      continue
    }
    shared.set(name, module.default)
  }

  return { bySlug, shared }
}

const registry = buildTenantComponentMaps()

/** Slugs discovered from `components/public/tenants/{slug}/` folders. */
export function listTenantUiSlugs(): string[] {
  return [...registry.bySlug.keys()].sort()
}

/**
 * Resolve public tenant UI by folder slug + component file name (without `.vue`).
 * Order: `tenants/{slug}/{name}.vue` → fallback `shared/{name}.vue`.
 */
export function resolveTenantComponent(slug: string, name: string): Component {
  const tenantComponent = registry.bySlug.get(slug)?.get(name)
  if (tenantComponent) {
    return tenantComponent
  }

  const sharedComponent = registry.shared.get(name)
  if (sharedComponent) {
    return sharedComponent
  }

  throw createError({
    statusCode: 500,
    statusMessage:
      `Tenant UI not found for slug="${slug}" component="${name}". `
      + `Expected tenants/${slug}/${name}.vue or shared/${name}.vue`,
  })
}
