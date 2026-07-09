export interface TokenResponse {
  accessToken: string
  tokenType: string
  expiresIn: number
  expiresAt: string
}

export interface TenantInfo {
  id: string
  slug: string
  displayName: string
}

export interface MeResponse {
  id: string
  email: string
  role: string
  tenant: TenantInfo | null
}
