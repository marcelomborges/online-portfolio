import type { RequestSurface } from '~/types/frontend'

/**
 * Request surface + tenant slug set by `middleware/resolve-host.global.ts`.
 * Use to branch pages/layouts between app, platform, and tenant public sites.
 */
export function useRequestSurface() {
  const surface = useState<RequestSurface>('request-surface', () => 'dev')
  const tenantSlug = useState<string | null>('tenant-slug', () => null)

  const isApp = computed(() => surface.value === 'app')
  const isPlatform = computed(() => surface.value === 'platform')
  const isTenantPublic = computed(() => surface.value === 'tenant')
  const isDev = computed(() => surface.value === 'dev')

  return {
    surface,
    tenantSlug,
    isApp,
    isPlatform,
    isTenantPublic,
    isDev,
  }
}
