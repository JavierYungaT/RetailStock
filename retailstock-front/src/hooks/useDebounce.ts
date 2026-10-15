import { useEffect, useState } from 'react'

/**
 * Devuelve el valor solo después de que deja de cambiar durante `delayMs`.
 * Se usa en los buscadores: escribir "arroz" hace 1 petición, no 5 (una por letra).
 */
export function useDebounce<T>(value: T, delayMs = 350): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs)
    // Cleanup: si el valor cambia antes de tiempo, se cancela el timer anterior
    return () => clearTimeout(timer)
  }, [value, delayMs])

  return debounced
}
