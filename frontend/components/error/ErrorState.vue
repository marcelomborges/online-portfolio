<script setup lang="ts">
const props = defineProps<{
  statusCode?: number
  title: string
  message: string
}>()

const emit = defineEmits<{
  action: []
}>()

const displayCode = computed(() => props.statusCode ?? 500)
</script>

<template>
  <main class="error-state">
    <p class="error-state__code">{{ displayCode }}</p>
    <h1>{{ title }}</h1>
    <p class="error-state__message">{{ message }}</p>
    <UiButton @click="emit('action')">
      <slot name="action">Voltar ao início</slot>
    </UiButton>
  </main>
</template>

<style scoped>
.error-state {
  margin: 4rem auto;
  max-width: 28rem;
  padding: 0 1rem;
  text-align: center;
}

.error-state__code {
  color: var(--op-color-muted);
  font-size: 0.875rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  margin: 0 0 0.5rem;
}

.error-state__message {
  color: #475569;
  line-height: 1.5;
  margin: 0.75rem 0 1.5rem;
}
</style>
