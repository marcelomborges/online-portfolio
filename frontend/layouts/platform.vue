<template>
  <div class="layout-platform">
    <UiPageContainer>
      <slot />
    </UiPageContainer>
  </div>
</template>

<style scoped>
.layout-platform {
  background: var(--op-color-bg, var(--op-color-surface));
  min-height: 100vh;
}
</style>
