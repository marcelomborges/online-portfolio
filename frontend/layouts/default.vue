<template>
  <div class="layout-default">
    <DevShellHeader />
    <UiPageContainer>
      <slot />
    </UiPageContainer>
    <DevShellFooter note="DEV-005 — Nuxt 3 skeleton (modo dev)" />
  </div>
</template>

<style scoped>
.layout-default {
  background: var(--op-color-bg, var(--op-color-surface));
  min-height: 100vh;
}
</style>
