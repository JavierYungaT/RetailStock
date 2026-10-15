import { useCallback, useEffect, useState } from 'react'
import { getErrorMessage } from '../api/errors'

interface QueryState<T> {
  data: T | null
  loading: boolean
  error: string | null
}

/**
 * Hook genérico para lecturas: ejecuta `fetcher` cuando cambian las dependencias y expone
 * { data, loading, error, reload }.
 *
 * - AbortController: si el usuario sigue escribiendo, la petición anterior se CANCELA; así una
 *   respuesta lenta y vieja nunca pisa a la más reciente (condición de carrera típica en buscadores).
 * - En un proyecto grande se usaría TanStack Query (caché, reintentos, invalidación); aquí se
 *   implementa a mano para entender qué resuelve.
 */
export function useApiQuery<T>(fetcher: (signal: AbortSignal) => Promise<T>, deps: unknown[]) {
  const [state, setState] = useState<QueryState<T>>({ data: null, loading: true, error: null })
  const [reloadToken, setReloadToken] = useState(0)

  // eslint-disable-next-line react-hooks/exhaustive-deps -- las dependencias las define quien llama
  const stableFetcher = useCallback(fetcher, deps)

  useEffect(() => {
    const controller = new AbortController()
    setState((previous) => ({ ...previous, loading: true, error: null })) // conserva los datos previos mientras carga

    stableFetcher(controller.signal)
      .then((data) => setState({ data, loading: false, error: null }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) return // cancelada a propósito: no es un error
        setState((previous) => ({ ...previous, loading: false, error: getErrorMessage(error) }))
      })

    return () => controller.abort()
  }, [stableFetcher, reloadToken])

  const reload = useCallback(() => setReloadToken((n) => n + 1), [])

  return { ...state, reload }
}
