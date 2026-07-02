/**
 * Sets `<html>` class from request context:
 * - Public tenant site (`surface=tenant`) → `theme-{slug}` (artist palette)
 * - Everything else → `surface-dark` (admin, platform, dev, error, structural pages)
 *
 * Errors (`useError()`) always use dark, even on tenant hosts.
 * `error.vue` should pass `{ forceDark: true }` — it replaces `app.vue` in Nuxt.
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
