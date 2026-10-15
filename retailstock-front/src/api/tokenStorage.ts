import type { Role } from './types'

// Sesión guardada en localStorage (sobrevive a recargas y pestañas nuevas).
// Ojo: localStorage es accesible por cualquier JavaScript de la página; ante un XSS el token se puede robar.
// La alternativa más segura es una cookie HttpOnly emitida por la API (ver explicación de la Fase 9).

const STORAGE_KEY = 'retailstock.session'

export interface StoredSession {
  token: string
  email: string
  role: Role
  expiresAtUtc: string
}

export const tokenStorage = {
  get(): StoredSession | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      if (!raw) return null
      const session = JSON.parse(raw) as StoredSession
      // Token vencido = sin sesión (evita una petición segura de fallar con 401)
      if (new Date(session.expiresAtUtc).getTime() <= Date.now()) {
        localStorage.removeItem(STORAGE_KEY)
        return null
      }
      return session
    } catch {
      return null // JSON corrupto o storage bloqueado (modo privado)
    }
  },

  set(session: StoredSession) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  },

  clear() {
    localStorage.removeItem(STORAGE_KEY)
  },
}
