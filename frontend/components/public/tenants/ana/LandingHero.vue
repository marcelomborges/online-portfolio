<script setup lang="ts">
const { slug } = useTenantContext()
const displayName = computed(
  () => slug.charAt(0).toUpperCase() + slug.slice(1),
)
</script>

<template>
  <section class="ana-landing">
    <p class="ana-landing__eyebrow">Portfolio</p>
    <h1>Olá, sou {{ displayName }}</h1>
    <p class="ana-landing__subtitle">
      Landing customizada da Ana — paleta quente, layout editorial.
    </p>
    <NuxtLink to="/contact" class="ana-landing__cta">Ver galeria</NuxtLink>
  </section>
</template>

<style scoped>
.ana-landing {
  background: linear-gradient(
    135deg,
    var(--op-color-surface) 0%,
    color-mix(in srgb, var(--op-color-primary) 12%, white) 100%
  );
  border: 1px solid var(--op-color-border);
  border-radius: calc(var(--op-radius) * 2);
  padding: 2rem 1.5rem;
}

.ana-landing__eyebrow {
  color: var(--op-color-muted);
  font-size: 0.75rem;
  letter-spacing: 0.08em;
  margin: 0 0 0.5rem;
  text-transform: uppercase;
}

.ana-landing__subtitle {
  color: var(--op-color-muted);
  line-height: 1.5;
  margin: 0.75rem 0 0;
}

.ana-landing__cta {
  background: var(--op-color-primary);
  border-radius: var(--op-radius);
  color: #fff;
  display: inline-block;
  margin-top: 1.25rem;
  padding: 0.625rem 1rem;
  text-decoration: none;
}

.ana-landing__cta:hover {
  background: var(--op-color-primary-hover);
}
</style>
