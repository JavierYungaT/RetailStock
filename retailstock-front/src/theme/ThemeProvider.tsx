import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'

export type ThemePreference = 'light' | 'dark' | 'system'

interface ThemeContextValue {
  preference: ThemePreference
  resolved: 'light' | 'dark'
  setPreference: (preference: ThemePreference) => void
  toggle: () => void
}

const STORAGE_KEY = 'retailstock.theme'
const ThemeContext = createContext<ThemeContextValue | null>(null)
const media = () => window.matchMedia('(prefers-color-scheme: dark)')

function readPreference(): ThemePreference {
  try {
    const value = localStorage.getItem(STORAGE_KEY)
    return value === 'light' || value === 'dark' ? value : 'system'
  } catch {
    return 'system'
  }
}

/** Tema claro/oscuro/sistema. La clase .dark en <html> activa los tokens oscuros de index.css. */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [preference, setPreferenceState] = useState<ThemePreference>(readPreference)
  const [systemDark, setSystemDark] = useState(() => media().matches)

  // Si la preferencia es "sistema", sigue los cambios del sistema operativo en vivo
  useEffect(() => {
    const query = media()
    const onChange = (e: MediaQueryListEvent) => setSystemDark(e.matches)
    query.addEventListener('change', onChange)
    return () => query.removeEventListener('change', onChange)
  }, [])

  const resolved = preference === 'system' ? (systemDark ? 'dark' : 'light') : preference

  useEffect(() => {
    document.documentElement.classList.toggle('dark', resolved === 'dark')
  }, [resolved])

  const setPreference = useCallback((value: ThemePreference) => {
    setPreferenceState(value)
    try { localStorage.setItem(STORAGE_KEY, value) } catch { /* storage bloqueado: solo vale para esta sesión */ }
  }, [])

  const value = useMemo<ThemeContextValue>(
    () => ({ preference, resolved, setPreference, toggle: () => setPreference(resolved === 'dark' ? 'light' : 'dark') }),
    [preference, resolved, setPreference],
  )

  return <ThemeContext value={value}>{children}</ThemeContext>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useTheme() {
  const context = useContext(ThemeContext)
  if (!context) throw new Error('useTheme debe usarse dentro de <ThemeProvider>.')
  return context
}
