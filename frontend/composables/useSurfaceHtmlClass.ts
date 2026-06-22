/**
 * Sets `<html>` class from request context:
 * - Site público do tenant (`surface=tenant`) → `theme-{slug}` (paleta do artista)
 * - Todo o resto → `surface-dark` (admin, platform, dev, error, páginas estruturais)
 *
 * Erros (`useError()`) sempre usam dark, mesmo se o host for de tenant.
 * `error.vue` deve passar `{ forceDark: true }` — ele substitui `app.vue` no Nuxt.
 */
export function useSurfaceHtmlClass(options?: { forceDark?: boolean }) {
  const { surface, tenantSlug } = useRequestSurface()
  const error = useError()

  const htmlClass = computed(() => {
    if (options?.forceDark || error.value) {
      return 'surface-dark'
    }

    if (surface.value === 'tenant' && tenantSlug.value) {
      return `theme-${tenantSlug.value}`
    }

    return 'surface-dark'
  })

  useHead({
    htmlAttrs: {
      class: htmlClass,
    },
  })
}
