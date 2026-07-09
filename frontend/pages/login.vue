<script setup lang="ts">
definePageMeta({ layout: 'app' })

const { isApp } = useRequestSurface()

if (!isApp.value) {
  throw createError({
    statusCode: 404,
    statusMessage: 'Login disponível apenas em app.onlineportfolio.com.br',
  })
}

const { isAuthenticated, fetchMe } = useAuth()

function redirectByRole(role: string) {
  return navigateTo(role === 'PlatformAdmin' ? '/platform/tenants' : '/admin')
}

onMounted(async () => {
  if (!isAuthenticated.value) return
  try {
    const me = await fetchMe()
    await redirectByRole(me.role)
  } catch {
    // Token inválido ou expirado — permanece na página de login
  }
})
</script>

<template>
  <UiCard title="Login" aria-label="Login na área administrativa">
    <AppLoginForm @success="redirectByRole" />
  </UiCard>
</template>
