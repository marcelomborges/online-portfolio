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
  min-height: 100vh;
}
</style>
