<script setup lang="ts">
export type UiKeyValueItem = {
  label: string
  value: string
}

defineProps<{
  items: UiKeyValueItem[]
}>()
</script>

<template>
  <dl class="ui-kv">
    <div v-for="item in items" :key="item.label" class="ui-kv__row">
      <dt class="ui-kv__label">{{ item.label }}</dt>
      <dd class="ui-kv__value">{{ item.value }}</dd>
    </div>
  </dl>
</template>

<style scoped>
.ui-kv {
  display: grid;
  gap: 0.75rem;
  margin: 0;
}

.ui-kv__label {
  color: var(--op-color-muted);
  font-family: var(--op-font-mono);
  font-size: 0.75rem;
}

.ui-kv__value {
  font-family: var(--op-font-mono);
  margin: 0.15rem 0 0;
}
</style>
