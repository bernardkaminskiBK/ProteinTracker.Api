import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi } from '../api/client'
import {
  clearStoredSession,
  getStoredSession,
  setUnauthorizedHandler,
  storeSession,
} from '../api/authStorage'
import type { AuthRequest, AuthResponse } from '../types/api'

interface AuthContextValue {
  session: AuthResponse | null
  login: (request: AuthRequest) => Promise<void>
  register: (request: AuthRequest) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthResponse | null>(() => getStoredSession())

  const logout = () => {
    clearStoredSession()
    setSession(null)
  }

  useEffect(() => {
    setUnauthorizedHandler(logout)
    return () => setUnauthorizedHandler(undefined)
  })

  const authenticate = async (request: AuthRequest, mode: 'login' | 'register') => {
    const response = mode === 'login'
      ? await authApi.login(request)
      : await authApi.register(request)
    storeSession(response)
    setSession(response)
  }

  const value = useMemo<AuthContextValue>(() => ({
    session,
    login: (request) => authenticate(request, 'login'),
    register: (request) => authenticate(request, 'register'),
    logout,
  }), [session])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// oxlint-disable-next-line react/only-export-components -- the hook and provider share one private context.
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider.')
  return context
}
