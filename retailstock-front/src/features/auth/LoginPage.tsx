import { useState, type FormEvent } from 'react'
import { Navigate, useLocation } from 'react-router'
import { getErrorMessage } from '../../api/errors'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Field, TextInput } from '../../components/Field'
import { useAuth } from './AuthContext'
import { homeByRole } from './roles'

interface LocationState {
  from?: string
}

const demoUsers = [
  { email: 'admin@retailstock.com', password: 'Admin123!', label: 'Admin' },
  { email: 'caja@retailstock.com', password: 'Cashier123!', label: 'Cajero' },
  { email: 'bodega@retailstock.com', password: 'Warehouse123!', label: 'Bodega' },
]

export function LoginPage() {
  const { user, login, logoutReason } = useAuth()
  const state = (useLocation().state ?? {}) as LocationState

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Con sesión (ya estaba logueado o acaba de iniciar): ir a donde quería entrar o a su inicio.
  // Si "from" no le corresponde a su rol, ProtectedRoute lo redirige a su pantalla.
  if (user) return <Navigate to={state.from ?? homeByRole[user.role]} replace />

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await login({ email, password }) // al guardar el usuario, el render siguiente redirige
    } catch (err) {
      setError(getErrorMessage(err, 'No se pudo iniciar sesión.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="flex min-h-dvh items-center justify-center px-4 py-10">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-3 flex size-12 items-center justify-center rounded-xl bg-brand-500 text-xl font-bold text-white">R</div>
          <h1 className="text-2xl font-semibold tracking-tight">RetailStock</h1>
          <p className="mt-1 text-sm text-slate-500">Inventario y ventas</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 rounded-xl border border-slate-200 bg-white p-6 shadow-sm" noValidate>
          {logoutReason === 'expired' && <Alert tone="info">Tu sesión expiró. Vuelve a iniciar sesión.</Alert>}
          {error && <Alert>{error}</Alert>}

          <Field label="Email" htmlFor="email">
            <TextInput id="email" type="email" autoComplete="username" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </Field>
          <Field label="Contraseña" htmlFor="password">
            <TextInput id="password" type="password" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} />
          </Field>

          <Button type="submit" loading={submitting} disabled={!email || !password} className="w-full">
            Iniciar sesión
          </Button>
        </form>

        <div className="mt-6 rounded-xl border border-dashed border-slate-300 p-4 text-xs text-slate-500">
          <p className="mb-2 font-medium text-slate-600">Usuarios de prueba</p>
          <div className="flex flex-wrap gap-2">
            {demoUsers.map((demo) => (
              <button
                key={demo.email}
                type="button"
                onClick={() => { setEmail(demo.email); setPassword(demo.password) }}
                className="rounded-md bg-slate-100 px-2.5 py-1 font-medium text-slate-700 hover:bg-slate-200"
              >
                {demo.label}
              </button>
            ))}
          </div>
        </div>
      </div>
    </main>
  )
}
