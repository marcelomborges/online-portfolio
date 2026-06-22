<script setup lang="ts">
defineProps<{
  title?: string
}>()
</script>

<template>
  <section class="ana-contact" aria-label="Contact section">
    <p class="ana-contact__eyebrow">Contato</p>
    <h2>{{ title ?? 'Fale comigo' }}</h2>
    <p class="ana-contact__lead">
      Envie uma mensagem sobre projetos, encomendas ou colaborações.
    </p>
    <p class="ana-contact__email">ola@ana-artista.exemplo</p>
  </section>
</template>

<style scoped>
.ana-contact {
  background: linear-gradient(
    135deg,
    var(--op-color-surface) 0%,
    color-mix(in srgb, var(--op-color-primary) 12%, white) 100%
  );
  border: 1px solid var(--op-color-border);
  border-radius: calc(var(--op-radius) * 2);
  padding: 2rem 1.5rem;
}

.ana-contact__eyebrow {
  color: var(--op-color-muted);
  font-size: 0.75rem;
  letter-spacing: 0.08em;
  margin: 0 0 0.5rem;
  text-transform: uppercase;
}

.ana-contact h2 {
  margin: 0;
}

.ana-contact__lead {
  color: var(--op-color-muted);
  line-height: 1.5;
  margin: 0.75rem 0 0;
}

.ana-contact__email {
  color: var(--op-color-primary);
  font-family: var(--op-font-mono);
  font-size: 0.9375rem;
  margin: 1.25rem 0 0;
}
</style>
