<script setup lang="ts">
defineProps<{
  title?: string
  emailLabel?: string
}>()
</script>

<template>
  <UiCard :title="title ?? 'Contato'" aria-label="Contact section">
    <p class="contact-section__lead">
      Formulário compartilhado — crie
      <code>components/public/tenants/{slug}/ContactSection.vue</code>
      para customizar por cliente.
    </p>
    <p v-if="emailLabel" class="contact-section__email">{{ emailLabel }}</p>
  </UiCard>
</template>

<style scoped>
.contact-section__lead {
  color: var(--op-color-muted);
  line-height: 1.5;
  margin: 0;
}

.contact-section__email {
  font-family: var(--op-font-mono);
  margin: 1rem 0 0;
}
</style>
