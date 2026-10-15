import axios from 'axios'
import type { ProblemDetails } from './types'

/** Extrae el ProblemDetails de un error de axios (o null si no hubo respuesta: red caída, CORS, timeout). */
export function getProblem(error: unknown): ProblemDetails | null {
  if (axios.isAxiosError<ProblemDetails>(error) && error.response?.data && typeof error.response.data === 'object') {
    return { status: error.response.status, ...error.response.data }
  }
  return null
}

/** Mensaje legible para mostrar al usuario a partir de cualquier error. */
export function getErrorMessage(error: unknown, fallback = 'Ocurrió un error inesperado.'): string {
  const problem = getProblem(error)
  if (problem) {
    // 400 de validación: se listan los mensajes de los campos
    if (problem.errors) {
      return Object.values(problem.errors).flat().join(' ')
    }
    if (problem.status === 403) return 'No tienes permisos para realizar esta acción.'
    return problem.detail ?? problem.title ?? fallback
  }
  if (axios.isAxiosError(error)) {
    if (error.code === 'ECONNABORTED') return 'La API tardó demasiado en responder.'
    if (!error.response) return 'No se pudo conectar con la API. ¿Está levantada en ' + import.meta.env.VITE_API_URL + '?'
  }
  return fallback
}

/**
 * Errores de validación por campo (claves en camelCase) para pintarlos debajo de cada input.
 * La API devuelve "Price" o "Items[0].Quantity"; se normaliza la primera letra.
 */
export function getFieldErrors(error: unknown): Record<string, string> {
  const errors = getProblem(error)?.errors ?? {}
  return Object.fromEntries(
    Object.entries(errors).map(([key, messages]) => [key.charAt(0).toLowerCase() + key.slice(1), messages[0]]),
  )
}
