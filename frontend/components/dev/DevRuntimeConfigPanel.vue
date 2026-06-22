<script setup lang="ts">
const { apiBase, platformHost, appHost } = usePlatformConfig()

const items = computed(() => [
  { label: 'NUXT_PUBLIC_API_BASE', value: apiBase },
  { label: 'NUXT_PUBLIC_PLATFORM_HOST', value: platformHost },
  { label: 'NUXT_PUBLIC_APP_HOST', value: appHost },
])
</script>

<template>
  <UiCard title="Public runtime config" aria-label="Runtime config (public)">
    <UiKeyValueList :items="items" />
  </UiCard>
</template>
