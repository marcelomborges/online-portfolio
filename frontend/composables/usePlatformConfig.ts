/**
 * Typed access to public platform env (NUXT_PUBLIC_*).
 * Server-only API base: useRuntimeConfig().apiInternalBase in server routes.
 */
export function usePlatformConfig() {
  const config = useRuntimeConfig()

  return {
    apiBase: config.public.apiBase,
    platformHost: config.public.platformHost,
    appHost: config.public.appHost,
  }
}
