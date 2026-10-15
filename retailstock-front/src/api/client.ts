import axios from 'axios'
import { tokenStorage } from './tokenStorage'

/**
 * Instancia única de axios para toda la app.
 * Equivalente en Spring: un RestClient con un ClientHttpRequestInterceptor que agrega el header Authorization.
 */
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL, // http://localhost:5080/api (de .env.development)
  timeout: 15_000,
  headers: { 'Content-Type': 'application/json' },
})

// ---- Interceptor de REQUEST: agrega "Authorization: Bearer <token>" a cada petición ----
apiClient.interceptors.request.use((config) => {
  const session = tokenStorage.get()
  if (session) {
    config.headers.Authorization = `Bearer ${session.token}`
  }
  return config
})

// ---- Interceptor de RESPONSE: ante 401 (token vencido o inválido) se cierra la sesión y se va al login ----
// El AuthContext registra qué hacer (logout + navegar) con setUnauthorizedHandler; así este módulo
// no depende de React ni del router.
let onUnauthorized: (() => void) | null = null

export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler
}

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const isLoginRequest = error.config?.url?.includes('/auth/login')
    // En el login, 401 significa "credenciales incorrectas": lo maneja la pantalla, no se redirige
    if (axios.isAxiosError(error) && error.response?.status === 401 && !isLoginRequest) {
      tokenStorage.clear()
      onUnauthorized?.()
    }
    return Promise.reject(error)
  },
)
