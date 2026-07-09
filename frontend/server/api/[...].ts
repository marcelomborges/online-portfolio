export default defineEventHandler((event) => {
  const config = useRuntimeConfig(event)
  const url = getRequestURL(event)
  const target = `${config.apiInternalBase}${url.pathname}${url.search}`
  return proxyRequest(event, target)
})
