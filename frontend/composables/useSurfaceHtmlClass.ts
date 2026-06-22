/**
 * Sets `<html>` class from request surface:
 * - `app` → `surface-app` (dark mode fixo — área privada)
 * - `tenant` → `theme-{slug}` (paleta por artista)
 * - `platform` / `dev` → sem classe extra (light default)
 */
export function useSurfaceHtmlClass() {
  const { surface, tenantSlug } = useRequestSurface()

  const htmlClass = computed(() => {
    switch (surface.value) {
      case 'app':
        return 'surface-app'
      case 'tenant':
        return tenantSlug.value ? `theme-${tenantSlug.value}` : ''
      default:
        return ''
    }
  })

  useHead({
    htmlAttrs: {
      class: htmlClass,
    },
  })
}
