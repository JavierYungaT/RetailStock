import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import { App } from './App'
import { AuthProvider } from './features/auth/AuthContext'
import './index.css'

// BrowserRouter envuelve todo: las rutas (y cualquier componente) pueden usar navegación.
// StrictMode (solo en desarrollo) ejecuta los efectos dos veces para detectar efectos sin cleanup.
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
)
