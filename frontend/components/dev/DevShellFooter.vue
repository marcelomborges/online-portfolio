<script setup lang="ts">
defineProps<{
  note?: string
}>()
</script>

<template>
  <footer class="dev-shell-footer">
    <small><slot>{{ note }}</slot></small>
  </footer>
</template>

<style scoped>
.dev-shell-footer {
  border-top: 1px solid var(--op-color-border);
  color: #94a3b8;
  padding: 0.75rem 1.25rem;
  text-align: center;
}
</style>
