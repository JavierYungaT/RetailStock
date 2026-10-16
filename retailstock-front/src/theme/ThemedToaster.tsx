import { Toaster } from 'sonner'
import { useTheme } from './ThemeProvider'

/** Notificaciones (toasts) que siguen el tema activo */
export function ThemedToaster() {
  const { resolved } = useTheme()
  return <Toaster theme={resolved} position="bottom-right" richColors closeButton toastOptions={{ className: 'font-sans' }} />
}
