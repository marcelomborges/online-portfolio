/** Eager-load all tenant theme CSS (`assets/css/themes/{slug}.css`). */
export default defineNuxtPlugin(() => {
  import.meta.glob('~/assets/css/themes/*.css', { eager: true })
})
