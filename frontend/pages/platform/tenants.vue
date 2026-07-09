<script setup lang="ts">
definePageMeta({ middleware: 'auth', layout: 'app' })
</script>

<template>
  <UiCard title="Tenants" aria-label="Gerenciamento de tenants">
    <p class="platform-tenants__placeholder">Gerenciamento de tenants e usuários em construção.</p>
  </UiCard>
</template>

<style scoped>
.platform-tenants__placeholder {
  color: var(--op-color-muted);
  font-size: 0.9375rem;
  margin: 0;
}
</style>
