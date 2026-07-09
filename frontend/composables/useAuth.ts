import type { MeResponse, TokenResponse } from '~/types/auth'

export function useAuth() {
  const token = useCookie<string | null>('auth_token', {
    sameSite: 'strict',
    maxAge: 60 * 60,
  })

  const isAuthenticated = computed(() => !!token.value)

  async function login(email: string, password: string): Promise<void> {
    const data = await $fetch<TokenResponse>('/api/v1/auth/login', {
      method: 'POST',
      body: { email, password },
    })
    token.value = data.accessToken
  }

  async function logout(): Promise<void> {
    await $fetch('/api/v1/auth/logout', { method: 'POST' }).catch(() => {})
    token.value = null
  }

  async function fetchMe(): Promise<MeResponse> {
    // Forward Cookie header during SSR so the proxy can read auth_token and
    // inject Authorization: Bearer. On the client side, the browser sends
    // cookies automatically, so useRequestHeaders returns {} and that's fine.
    const headers = useRequestHeaders(['cookie'])
    return await $fetch<MeResponse>('/api/v1/auth/me', { headers })
  }

  return { token, isAuthenticated, login, logout, fetchMe }
}
