// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  devtools: { enabled: true },

  runtimeConfig: {
    apiInternalBase: process.env.NUXT_API_INTERNAL_BASE || 'http://localhost:8080',
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || '/api',
      platformHost: process.env.NUXT_PUBLIC_PLATFORM_HOST || 'onlineportfolio.com.br',
      appHost: process.env.NUXT_PUBLIC_APP_HOST || 'app.onlineportfolio.com.br',
    },
  },

  nitro: {
    preset: 'vercel',
  },
})
