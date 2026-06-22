<script setup lang="ts">
const { slug } = useTenantContext()
</script>

<template>
  <section class="joao-landing">
    <span class="joao-landing__badge">Artista</span>
    <h1>{{ slug.toUpperCase() }}</h1>
    <p class="joao-landing__tagline">
      Landing minimalista do João — paleta fria, tipografia grande.
    </p>
    <NuxtLink to="/contact" class="joao-landing__link">Contato</NuxtLink>
  </section>
</template>

<style scoped>
.joao-landing {
  border-left: 4px solid var(--op-color-primary);
  padding: 1.5rem 0 1.5rem 1.25rem;
}

.joao-landing__badge {
  color: var(--op-color-muted);
  font-size: 0.75rem;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.joao-landing__tagline {
  color: var(--op-color-muted);
  line-height: 1.6;
  max-width: 28rem;
}

.joao-landing__link {
  color: var(--op-color-primary);
  font-weight: 600;
  text-decoration: none;
}
</style>
