import type { RequestSurface } from '~/types/frontend'

/**
 * Resolves host → surface + tenant slug (DEV-104 will extend with API validation).
 *
 * | Host                         | Surface   |
 * |------------------------------|-----------|
 * | app.{platform}               | app       |
 * | {platform} / www.{platform}  | platform  |
 * | {slug}.{platform}            | tenant    |
 * | localhost (dev)              | from env  |
 */
export default defineNuxtRouteMiddleware(() => {
  const config = useRuntimeConfig()
  const platformHost = config.public.platformHost as string
  const appHost = config.public.appHost as string
  const devSurface = (config.public.devSurface as RequestSurface) || 'dev'
  const devTenantSlug = (config.public.devTenantSlug as string) || 'ana'

  const surface = useState<RequestSurface>('request-surface', () => 'dev')
  const tenantSlug = useState<string | null>('tenant-slug', () => null)

  if (import.meta.server) {
    const host = useRequestHeaders(['host']).host?.split(':')[0] ?? ''

    if (host === appHost || host.startsWith('app.')) {
      surface.value = 'app'
      tenantSlug.value = null
      return
    }

    if (host === platformHost || host === `www.${platformHost}`) {
      surface.value = 'platform'
      tenantSlug.value = null
      return
    }

    const suffix = `.${platformHost}`
    if (host.endsWith(suffix) && host.length > suffix.length) {
      surface.value = 'tenant'
      tenantSlug.value = host.slice(0, -suffix.length)
      return
    }

    // Local dev / unknown host — configurable via NUXT_PUBLIC_DEV_*
    surface.value = devSurface
    tenantSlug.value = devSurface === 'tenant' ? devTenantSlug : null
    return
  }

  // Client: state already hydrated from server payload
})
