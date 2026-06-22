<template>
  <div class="layout-structural">
    <UiPageContainer>
      <slot />
    </UiPageContainer>
  </div>
</template>

<style scoped>
.layout-structural {
  background: var(--op-color-bg, var(--op-color-surface));
  min-height: 100vh;
}
</style>
