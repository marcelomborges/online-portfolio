<script setup lang="ts">
defineProps<{
  headline?: string
}>()
</script>

<template>
  <section class="platform-landing-hero">
    <p class="platform-landing-hero__eyebrow">Online Portfolio</p>
    <h1>{{ headline ?? 'Portfólios online para artistas' }}</h1>
    <p class="platform-landing-hero__lead">
      Landing da plataforma em <code>onlineportfolio.com.br</code> — separada dos sites dos clientes.
    </p>
  </section>
</template>

<style scoped>
.platform-landing-hero {
  padding: 2rem 0;
}

.platform-landing-hero__eyebrow {
  color: var(--op-color-muted);
  font-size: 0.8125rem;
  margin: 0 0 0.5rem;
}

.platform-landing-hero__lead {
  color: var(--op-color-muted);
  line-height: 1.6;
  max-width: 36rem;
}
</style>
