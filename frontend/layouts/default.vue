<template>
  <div class="layout-default">
    <DevShellHeader />
    <UiPageContainer>
      <slot />
    </UiPageContainer>
    <DevShellFooter note="Online Portfolio — ambiente de desenvolvimento" />
  </div>
</template>

<style scoped>
.layout-default {
  background: var(--op-color-bg, var(--op-color-surface));
  min-height: 100vh;
}
</style>
