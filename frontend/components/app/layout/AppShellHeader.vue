<script setup lang="ts">
const appConfig = useAppConfig()
</script>

<template>
  <header class="app-shell-header">
    <NuxtLink to="/admin" class="app-shell-header__brand">
      {{ appConfig.site.name }} Admin
    </NuxtLink>
  </header>
</template>

<style scoped>
.app-shell-header {
  background: var(--op-color-surface);
  border-bottom: 1px solid var(--op-color-border);
  color: var(--op-color-text);
  padding: 0.75rem 1.25rem;
}

.app-shell-header__brand {
  color: var(--op-color-primary);
  font-weight: 600;
  text-decoration: none;
}

.app-shell-header__brand:hover {
  color: var(--op-color-primary-hover);
}
</style>
