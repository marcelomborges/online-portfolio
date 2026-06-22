<script setup lang="ts">
defineProps<{
  title?: string
  ariaLabel?: string
}>()
</script>

<template>
  <section class="ui-card" :aria-label="ariaLabel">
    <h2 v-if="title || $slots.title" class="ui-card__title">
      <slot name="title">{{ title }}</slot>
    </h2>
    <slot />
  </section>
</template>

<style scoped>
.ui-card {
  background: var(--op-color-surface);
  border: 1px solid var(--op-color-border);
  border-radius: calc(var(--op-radius) * 1.5);
  padding: 1rem 1.25rem;
}

.ui-card__title {
  font-size: 1rem;
  margin: 0 0 0.75rem;
}
</style>
