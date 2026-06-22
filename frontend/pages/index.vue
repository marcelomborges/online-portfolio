<script setup lang="ts">
const { surface, tenantSlug } = useRequestSurface()

const layoutName = computed(() => {
  switch (surface.value) {
    case 'app':
      return 'app'
    case 'platform':
      return 'platform'
    case 'tenant':
      return 'tenant'
    default:
      return 'default'
  }
})
</script>

<template>
  <NuxtLayout :name="layoutName">
    <PlatformLandingHero v-if="surface === 'platform'" />
    <TenantHome v-else-if="surface === 'tenant'" />
    <div v-else>
      <h1>DEV-005 — Nuxt skeleton</h1>
      <p class="index-page__meta">
        Surface: <code>{{ surface }}</code>
        <template v-if="tenantSlug"> · tenant: <code>{{ tenantSlug }}</code></template>
      </p>
      <p class="index-page__lead">
        Em localhost, defina <code>NUXT_PUBLIC_DEV_SURFACE=tenant</code> e
        <code>NUXT_PUBLIC_DEV_TENANT_SLUG=ana</code> para preview do site público.
      </p>
      <DevRuntimeConfigPanel />
    </div>
  </NuxtLayout>
</template>

<style scoped>
.index-page__lead,
.index-page__meta {
  color: var(--op-color-muted);
  line-height: 1.5;
}

.index-page__meta {
  margin: 0 0 1rem;
}
</style>
