<script setup lang="ts">
definePageMeta({
  layout: 'tenant',
})

const { isTenantPublic } = useRequestSurface()

if (!isTenantPublic.value) {
  throw createError({
    statusCode: 404,
    statusMessage: 'Página de contato disponível apenas em sites públicos de tenant',
  })
}

const ContactSection = useTenantComponent('ContactSection')
</script>

<template>
  <component :is="ContactSection" title="Fale comigo" />
</template>
