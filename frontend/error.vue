<script setup lang="ts">
import type { NuxtError } from '#app'

useSurfaceHtmlClass({ forceDark: true })

const props = defineProps<{
  error: NuxtError
}>()

const title = computed(() => {
  if (props.error.statusCode === 404) {
    return 'Página não encontrada'
  }
  return 'Algo deu errado'
})

const message = computed(() => {
  return props.error.message || 'Ocorreu um erro inesperado.'
})

function handleClearError() {
  clearError({ redirect: '/' })
}
</script>

<template>
  <NuxtLayout name="structural">
    <ErrorState
      :status-code="error.statusCode"
      :title="title"
      :message="message"
      @action="handleClearError"
    />
  </NuxtLayout>
</template>
