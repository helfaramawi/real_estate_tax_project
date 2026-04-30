import api from './api'
import type { TokenResponse } from './types'

export async function login(username: string, password: string): Promise<TokenResponse> {
  const { data } = await api.post('/auth/login', { username, password })
  const tokens: TokenResponse = data.data
  localStorage.setItem('accessToken', tokens.accessToken)
  localStorage.setItem('refreshToken', tokens.refreshToken)
  return tokens
}

export function logout() {
  localStorage.removeItem('accessToken')
  localStorage.removeItem('refreshToken')
  window.location.href = '/login'
}

export function isAuthenticated(): boolean {
  return !!localStorage.getItem('accessToken')
}

export function getStoredUser(): { username: string; roles: string[] } | null {
  const token = localStorage.getItem('accessToken')
  if (!token) return null
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    const roles = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? []
    return {
      username: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? '',
      roles: Array.isArray(roles) ? roles : [roles],
    }
  } catch {
    return null
  }
}
