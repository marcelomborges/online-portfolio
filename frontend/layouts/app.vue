<template>
  <div class="layout-app">
    <AppShellHeader />
    <UiPageContainer>
      <slot />
    </UiPageContainer>
  </div>
</template>

<style scoped>
.layout-app {
  background: var(--op-color-bg, var(--op-color-surface));
  min-height: 100vh;
}
</style>
