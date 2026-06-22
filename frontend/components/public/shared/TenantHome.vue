<script setup lang="ts">
const LandingHero = useTenantComponent('LandingHero')
</script>

<template>
  <component :is="LandingHero" />
</template>
