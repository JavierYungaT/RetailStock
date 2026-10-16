import '@fontsource-variable/inter'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { App } from './App'
import { AuthProvider } from './features/auth/AuthContext'
import './index.css'
import { ThemedToaster } from './theme/ThemedToaster'
import { ThemeProvider } from './theme/ThemeProvider'

// StrictMode (solo en desarrollo) ejecuta los efectos dos veces para detectar efectos sin cleanup.
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <ThemeProvider>
        <AuthProvider>
          <App />
          <ThemedToaster />
        </AuthProvider>
      </ThemeProvider>
    </BrowserRouter>
  </StrictMode>,
)
