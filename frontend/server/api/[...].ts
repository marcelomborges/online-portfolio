export default defineEventHandler((event) => {
  const config = useRuntimeConfig(event)
  const url = getRequestURL(event)
  const target = `${config.apiInternalBase}${url.pathname}${url.search}`

  // Inject JWT stored in auth_token cookie as Authorization header so the
  // API's Bearer middleware receives it on every proxied request.
  const token = getCookie(event, 'auth_token')
  if (token)
    event.node.req.headers['authorization'] = `Bearer ${token}`

  return proxyRequest(event, target)
})
