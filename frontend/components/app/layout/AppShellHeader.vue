<script setup lang="ts">
const appConfig = useAppConfig()
const { isAuthenticated, logout } = useAuth()

async function handleLogout() {
  await logout()
  await navigateTo('/login')
}
</script>

<template>
  <header class="app-shell-header">
    <NuxtLink to="/admin" class="app-shell-header__brand">
      {{ appConfig.site.name }} Admin
    </NuxtLink>
    <button
      v-if="isAuthenticated"
      class="app-shell-header__logout"
      @click="handleLogout"
    >
      Sair
    </button>
  </header>
</template>

<style scoped>
.app-shell-header {
  align-items: center;
  background: var(--op-color-surface);
  border-bottom: 1px solid var(--op-color-border);
  color: var(--op-color-text);
  display: flex;
  justify-content: space-between;
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

.app-shell-header__logout {
  background: transparent;
  border: none;
  color: var(--op-color-muted);
  cursor: pointer;
  font-family: var(--op-font-sans);
  font-size: 0.875rem;
  padding: 0.25rem 0;
}

.app-shell-header__logout:hover {
  color: var(--op-color-text);
}
</style>
