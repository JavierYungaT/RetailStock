import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Vite = servidor de desarrollo + bundler (equivalente a webpack/CRA, pero mucho más rápido).
// @tailwindcss/vite: Tailwind v4 se integra como plugin; no hace falta tailwind.config.js ni PostCSS.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: { port: 5173, strictPort: true }, // el puerto que la API permite en CORS
})
