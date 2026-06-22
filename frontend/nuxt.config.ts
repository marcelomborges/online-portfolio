// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  devtools: { enabled: true },

  css: ['~/assets/css/main.css', '~/assets/css/surfaces/app.css'],

  components: [
    { path: '~/components/ui', pathPrefix: false },
    { path: '~/components/error', pathPrefix: false },
    { path: '~/components/dev', pathPrefix: false },
    { path: '~/components/app', pathPrefix: false },
    { path: '~/components/platform', pathPrefix: false },
    {
      path: '~/components/public/shared',
      pathPrefix: false,
      ignore: ['LandingHero.vue', 'ContactSection.vue'],
    },
  ],

  compatibilityDate: '2025-06-21',

  typescript: {
    strict: true,
    typeCheck: false,
  },

  runtimeConfig: {
    apiInternalBase: process.env.NUXT_API_INTERNAL_BASE || 'http://localhost:8080',
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || '/api',
      platformHost: process.env.NUXT_PUBLIC_PLATFORM_HOST || 'onlineportfolio.com.br',
      appHost: process.env.NUXT_PUBLIC_APP_HOST || 'app.onlineportfolio.com.br',
      /** Local dev: app | platform | tenant | dev */
      devSurface: process.env.NUXT_PUBLIC_DEV_SURFACE || 'dev',
      /** Local dev tenant slug when devSurface=tenant */
      devTenantSlug: process.env.NUXT_PUBLIC_DEV_TENANT_SLUG || 'ana',
    },
  },

  nitro: {
    preset: 'vercel',
  },
})
