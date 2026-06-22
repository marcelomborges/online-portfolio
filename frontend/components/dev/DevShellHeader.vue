<script setup lang="ts">
const { platformHost } = usePlatformConfig()
const appConfig = useAppConfig()
</script>

<template>
  <header class="dev-shell-header">
    <NuxtLink to="/" class="dev-shell-header__brand">
      {{ appConfig.site.name }}
    </NuxtLink>
    <span class="dev-shell-header__meta">{{ platformHost }}</span>
  </header>
</template>

<style scoped>
.dev-shell-header {
  align-items: center;
  border-bottom: 1px solid var(--op-color-border);
  display: flex;
  gap: 1rem;
  justify-content: space-between;
  padding: 0.75rem 1.25rem;
}

.dev-shell-header__brand {
  color: var(--op-color-text);
  font-weight: 600;
  text-decoration: none;
}

.dev-shell-header__meta {
  color: var(--op-color-muted);
  font-family: var(--op-font-mono);
  font-size: 0.8125rem;
}
</style>
