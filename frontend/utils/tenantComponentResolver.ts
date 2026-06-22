import type { Component } from 'vue'

type VueModule = { default: Component }

const tenantModules = import.meta.glob<VueModule>(
  '~/components/public/tenants/*/*.vue',
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

function buildTenantRegistry() {
  const bySlug = new Map<string, Map<string, VueModule>>()

  for (const [path, module] of Object.entries(tenantModules)) {
    const parsed = parseTenantEntry(path)
    if (!parsed) {
      continue
    }
    if (!bySlug.has(parsed.slug)) {
      bySlug.set(parsed.slug, new Map())
    }
    bySlug.get(parsed.slug)!.set(parsed.name, module)
  }

  return bySlug
}

const registry = buildTenantRegistry()

/** Slugs discovered from `components/public/tenants/{slug}/` folders. */
export function listTenantUiSlugs(): string[] {
  return [...registry.keys()].sort()
}

/**
 * Resolve public tenant UI by folder slug + component file name (without `.vue`).
 * Only `tenants/{slug}/` — `shared/` (e.g. TenantHome) is a normal Nuxt component.
 */
export function resolveTenantComponent(slug: string, name: string): Component {
  const module = registry.get(slug)?.get(name)
  if (!module) {
    throw createError({
      statusCode: 500,
      statusMessage:
        `Tenant UI not found for slug="${slug}" component="${name}". `
        + `Expected tenants/${slug}/${name}.vue`,
    })
  }

  return module.default
}
