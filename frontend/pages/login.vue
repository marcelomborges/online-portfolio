<script setup lang="ts">
definePageMeta({
  layout: 'app',
})

// Guard: login only on app host (DEV-104 will harden)
const { isApp } = useRequestSurface()

if (!isApp.value) {
  throw createError({
    statusCode: 404,
    statusMessage: 'Login disponível apenas em app.onlineportfolio.com.br',
  })
}
</script>

<template>
  <UiCard title="Login" aria-label="Admin login">
    <p class="login-page__lead">
      Área padronizada — mesma UI de login para todos os tenants em
      <code>app.onlineportfolio.com.br</code>.
    </p>
    <p class="login-page__note">Área de login em breve.</p>
  </UiCard>
</template>

<style scoped>
.login-page__lead {
  color: var(--op-color-muted);
  line-height: 1.5;
  margin: 0 0 1rem;
}

.login-page__note {
  font-size: 0.875rem;
  margin: 0;
}
</style>
