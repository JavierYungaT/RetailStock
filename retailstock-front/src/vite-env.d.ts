/// <reference types="vite/client" />

// Tipado de las variables de entorno (import.meta.env.VITE_API_URL deja de ser "any")
interface ImportMetaEnv {
  readonly VITE_API_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
