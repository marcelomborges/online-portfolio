<script setup lang="ts">
const props = defineProps<{
  title?: string
  subtitle?: string
}>()

const { slug } = useTenantContext()
const displayTitle = computed(
  () => props.title ?? slug.charAt(0).toUpperCase() + slug.slice(1),
)
</script>

<template>
  <section class="landing-hero">
    <p class="landing-hero__eyebrow">Portfolio</p>
    <h1>{{ displayTitle }}</h1>
    <p v-if="subtitle" class="landing-hero__subtitle">{{ subtitle }}</p>
    <slot />
  </section>
</template>

<style scoped>
.landing-hero {
  background: linear-gradient(
    135deg,
    var(--op-color-surface) 0%,
    color-mix(in srgb, var(--op-color-primary) 12%, white) 100%
  );
  border: 1px solid var(--op-color-border);
  border-radius: calc(var(--op-radius) * 2);
  padding: 2rem 1.5rem;
}

.landing-hero__eyebrow {
  color: var(--op-color-muted);
  font-size: 0.75rem;
  letter-spacing: 0.08em;
  margin: 0 0 0.5rem;
  text-transform: uppercase;
}

.landing-hero__subtitle {
  color: var(--op-color-muted);
  line-height: 1.5;
  margin: 0.75rem 0 0;
}
</style>
