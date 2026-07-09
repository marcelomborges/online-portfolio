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
    // useRequestFetch forwards the browser's Cookie header during SSR so the
    // proxy can inject Authorization: Bearer from auth_token on the server side.
    const fetch = useRequestFetch()
    return await fetch<MeResponse>('/api/v1/auth/me')
  }

  return { token, isAuthenticated, login, logout, fetchMe }
}
