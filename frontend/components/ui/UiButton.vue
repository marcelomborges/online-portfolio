<script setup lang="ts">
withDefaults(
  defineProps<{
    type?: 'button' | 'submit' | 'reset'
    variant?: 'primary' | 'ghost'
  }>(),
  {
    type: 'button',
    variant: 'primary',
  },
)
</script>

<template>
  <button
    :type="type"
    class="ui-button"
    :class="`ui-button--${variant}`"
  >
    <slot />
  </button>
</template>

<style scoped>
.ui-button {
  border: none;
  border-radius: var(--op-radius);
  cursor: pointer;
  font-family: var(--op-font-sans);
  font-size: 0.9375rem;
  padding: 0.625rem 1.25rem;
}

.ui-button--primary {
  background: var(--op-color-primary);
  color: #fff;
}

.ui-button--primary:hover {
  background: var(--op-color-primary-hover);
}

.ui-button--ghost {
  background: transparent;
  border: 1px solid var(--op-color-border);
  color: var(--op-color-text);
}
</style>
