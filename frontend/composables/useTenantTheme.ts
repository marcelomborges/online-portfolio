/**
 * @deprecated Use `useSurfaceHtmlClass()` in `app.vue` (aplica `theme-{slug}` no tenant).
 * Mantido para compatibilidade se algum layout chamar diretamente.
 */
export function useTenantTheme() {
  useSurfaceHtmlClass()
}
