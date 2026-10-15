import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { setUnauthorizedHandler } from '../../api/client'
import { tokenStorage, type StoredSession } from '../../api/tokenStorage'
import type { Role } from '../../api/types'
import { authApi, type LoginRequest } from './authApi'

/**
 * Estado global de autenticación con Context API (sin Redux).
 * Equivalente conceptual en Spring: el SecurityContext, pero en el navegador.
 */
export type LogoutReason = 'manual' | 'expired'

interface AuthContextValue {
  user: StoredSession | null
  isAuthenticated: boolean
  /** Por qué se cerró la última sesión: el login muestra "tu sesión expiró" y ProtectedRoute decide si recordar la ruta. */
  logoutReason: LogoutReason | null
  login: (request: LoginRequest) => Promise<StoredSession>
  logout: (reason?: LogoutReason) => void
  hasRole: (...roles: Role[]) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  // Inicialización perezosa: se lee localStorage UNA vez al montar (sesión que sobrevive a recargas)
  const [user, setUser] = useState<StoredSession | null>(() => tokenStorage.get())
  const [logoutReason, setLogoutReason] = useState<LogoutReason | null>(null)

  // logout NO navega: solo limpia la sesión. Al quedar user = null, ProtectedRoute redirige al login.
  // Un único responsable de la redirección evita que dos navegaciones compitan (y se pierda el motivo).
  const logout = useCallback((reason: LogoutReason = 'manual') => {
    tokenStorage.clear()
    setLogoutReason(reason)
    setUser(null)
  }, [])

  const login = useCallback(async (request: LoginRequest) => {
    const response = await authApi.login(request)
    const session: StoredSession = {
      token: response.token,
      email: response.email,
      role: response.role,
      expiresAtUtc: response.expiresAtUtc,
    }
    tokenStorage.set(session)
    setLogoutReason(null)
    setUser(session)
    return session
  }, [])

  // El interceptor de axios avisa cuando la API responde 401 -> sesión vencida
  useEffect(() => {
    setUnauthorizedHandler(() => logout('expired'))
    return () => setUnauthorizedHandler(null)
  }, [logout])

  // Cierre automático cuando vence el token, aunque el usuario no haga peticiones
  useEffect(() => {
    if (!user) return
    const msLeft = new Date(user.expiresAtUtc).getTime() - Date.now()
    const timer = setTimeout(() => logout('expired'), Math.max(msLeft, 0))
    return () => clearTimeout(timer)
  }, [user, logout])

  // useMemo: el objeto del contexto solo cambia cuando cambia el usuario (evita re-renders en cascada)
  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      logoutReason,
      login,
      logout,
      hasRole: (...roles) => user !== null && roles.includes(user.role),
    }),
    [user, logoutReason, login, logout],
  )

  // React 19: el propio Context se usa como provider (<AuthContext value={...}>)
  return <AuthContext value={value}>{children}</AuthContext>
}

/** Hook de acceso al contexto; falla claro si se usa fuera del AuthProvider. */
// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth debe usarse dentro de <AuthProvider>.')
  return context
}
