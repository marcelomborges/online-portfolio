<script setup lang="ts">
const { surface, tenantSlug, isDev } = useRequestSurface()
const appConfig = useAppConfig()

const layoutName = computed(() => {
  switch (surface.value) {
    case 'app':
      return 'app'
    case 'platform':
      return 'platform'
    case 'tenant':
      return 'tenant'
    default:
      return 'default'
  }
})
</script>

<template>
  <NuxtLayout :name="layoutName">
    <PlatformLandingHero v-if="surface === 'platform'" />
    <TenantHome v-else-if="surface === 'tenant'" />
    <div v-else class="index-page">
      <section class="index-page__hero" aria-label="Online Portfolio">
        <p class="index-page__eyebrow">{{ appConfig.site.name }}</p>
        <h1>Portfólios online para artistas</h1>
        <p class="index-page__lead">
          Publique landing, posts e galeria no seu domínio — com identidade visual própria e área
          administrativa padronizada para cada tenant.
        </p>
        <p v-if="surface === 'app'" class="index-page__hint">
          Painel administrativo em construção.
          <NuxtLink to="/login" class="index-page__link">Ir para login</NuxtLink>
        </p>
        <p v-else-if="isDev" class="index-page__hint">
          Preview de deploy — em produção, os domínios oficiais entram no
          <span class="index-page__hint-em">DEV-011</span>.
        </p>
      </section>
      <DevRuntimeConfigPanel v-if="isDev" class="index-page__panel" />
      <p v-if="isDev" class="index-page__meta">
        Surface: <code>{{ surface }}</code>
        <template v-if="tenantSlug"> · tenant: <code>{{ tenantSlug }}</code></template>
      </p>
    </div>
  </NuxtLayout>
</template>

<style scoped>
.index-page__hero {
  padding: 2rem 0 1.5rem;
}

.index-page__eyebrow {
  color: var(--op-color-muted);
  font-size: 0.8125rem;
  letter-spacing: 0.04em;
  margin: 0 0 0.5rem;
  text-transform: uppercase;
}

.index-page__hero h1 {
  font-size: clamp(1.75rem, 4vw, 2.25rem);
  line-height: 1.2;
  margin: 0 0 1rem;
}

.index-page__lead,
.index-page__hint,
.index-page__meta {
  color: var(--op-color-muted);
  line-height: 1.6;
  max-width: 36rem;
}

.index-page__lead {
  margin: 0 0 1rem;
}

.index-page__hint {
  margin: 0;
}

.index-page__hint-em {
  color: var(--op-color-text);
  font-weight: 500;
}

.index-page__link {
  color: var(--op-color-primary);
  font-weight: 500;
  margin-left: 0.25rem;
  text-decoration: none;
}

.index-page__link:hover {
  color: var(--op-color-primary-hover);
  text-decoration: underline;
}

.index-page__panel {
  margin-top: 1.5rem;
}

.index-page__meta {
  font-size: 0.8125rem;
  margin: 1.5rem 0 0;
}
</style>
