import { resolve } from 'node:path'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '~': resolve(__dirname, '.'),
      '@': resolve(__dirname, '.'),
    },
  },
  test: {
    environment: 'node',
    include: ['**/*.{test,spec}.ts'],
    exclude: ['node_modules', '.nuxt', '.output', '.vercel'],
  },
  coverage: {
    provider: 'istanbul',
    reporter: ['text-summary', 'json-summary'],
    reportsDirectory: './coverage',
    include: ['utils/**/*.ts', 'composables/**/*.ts'],
    exclude: ['**/*.{test,spec}.ts'],
  },
})
