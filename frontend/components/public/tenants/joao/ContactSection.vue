<script setup lang="ts">
defineProps<{
  title?: string
}>()
</script>

<template>
  <UiCard :title="title ?? 'Contato'" aria-label="Contact section">
    <p class="joao-contact__lead">
      Formulário minimalista do João — paleta fria, borda lateral como na landing.
    </p>
  </UiCard>
</template>

<style scoped>
.joao-contact__lead {
  border-left: 3px solid var(--op-color-primary);
  color: var(--op-color-muted);
  line-height: 1.5;
  margin: 0;
  padding-left: 0.75rem;
}
</style>
