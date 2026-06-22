/**
 * Active tenant on public sites (`{slug}.onlineportfolio.com.br`).
 * Not available on `app.*` — use `useRequestSurface().isApp` there instead.
 */
export function useTenantContext() {
  const { tenantSlug, isTenantPublic } = useRequestSurface()

  if (!isTenantPublic.value || !tenantSlug.value) {
    throw createError({
      statusCode: 500,
      statusMessage: 'useTenantContext() is only valid on tenant public hosts',
    })
  }

  return {
    slug: tenantSlug.value,
  }
}
