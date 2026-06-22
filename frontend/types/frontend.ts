/** Host surface resolved from the request (see middleware/resolve-host.global.ts). */
export type RequestSurface = 'dev' | 'app' | 'platform' | 'tenant'

/**
 * Public page block names — must match `{Name}.vue` under `tenants/{slug}/` or `shared/`.
 * Add keys here when introducing new resolvable public blocks (e.g. GalleryGrid).
 */
export type TenantComponentKey = 'LandingHero' | 'ContactSection'
