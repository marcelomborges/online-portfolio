import type { Component } from 'vue'
import type { TenantComponentKey } from '~/types/frontend'
import { resolveTenantComponent } from '~/utils/tenantComponentResolver'

/**
 * Public tenant site only (`{slug}.*`). Admin/login uses `components/app/` — not this composable.
 *
 * Component file name must match `key` (e.g. `LandingHero.vue`).
 * Lookup: `components/public/tenants/{slug}/{key}.vue` (page blocks are tenant-exclusive).
 */
export function useTenantComponent(key: TenantComponentKey): Component {
  const { slug } = useTenantContext()
  return resolveTenantComponent(slug, key)
}
