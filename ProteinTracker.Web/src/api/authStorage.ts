import type { AuthResponse } from '../types/api'

const storageKey = 'protein-tracker-session'
let unauthorizedHandler: (() => void) | undefined

export function getStoredSession(): AuthResponse | null {
  const value = localStorage.getItem(storageKey)
  if (!value) return null

  try {
    const session = JSON.parse(value) as AuthResponse
    if (!session.token || new Date(session.expiresAt).getTime() <= Date.now()) {
      clearStoredSession()
      return null
    }
    return session
  } catch {
    clearStoredSession()
    return null
  }
}

export function storeSession(session: AuthResponse): void {
  localStorage.setItem(storageKey, JSON.stringify(session))
}

export function clearStoredSession(): void {
  localStorage.removeItem(storageKey)
}

export function setUnauthorizedHandler(handler: (() => void) | undefined): void {
  unauthorizedHandler = handler
}

export function handleUnauthorized(): void {
  clearStoredSession()
  unauthorizedHandler?.()
}
