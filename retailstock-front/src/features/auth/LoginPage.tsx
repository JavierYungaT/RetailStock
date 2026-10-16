import { ArrowRight, BarChart3, Boxes, Eye, EyeOff, Lock, Mail, Moon, ShieldCheck, ShoppingCart, Sun, Warehouse } from 'lucide-react'
import { useState, type FormEvent, type KeyboardEvent, type ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { getErrorMessage } from '../../api/errors'
import type { Role } from '../../api/types'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { Field, Input } from '../../components/ui/Field'
import { Logo } from '../../components/ui/Logo'
import { cn } from '../../lib/cn'
import { useTheme } from '../../theme/ThemeProvider'
import { useAuth } from './AuthContext'
import { homeByRole } from './roles'

const CURRENT_YEAR = new Date().getFullYear()

interface LocationState {
  from?: string
}

const demoUsers: { email: string; password: string; role: Role; label: string; description: string; icon: ReactNode }[] = [
  { email: 'admin@retailstock.com', password: 'Admin123!', role: 'Admin', label: 'Administrador', description: 'Dashboard, catálogo y usuarios', icon: <ShieldCheck /> },
  { email: 'caja@retailstock.com', password: 'Cashier123!', role: 'Cashier', label: 'Cajero', description: 'Punto de venta', icon: <ShoppingCart /> },
  { email: 'bodega@retailstock.com', password: 'Warehouse123!', role: 'Warehouse', label: 'Bodega', description: 'Existencias y movimientos', icon: <Warehouse /> },
]

export function LoginPage() {
  const { user, login, logoutReason } = useAuth()
  const { resolved, toggle } = useTheme()
  const state = (useLocation().state ?? {}) as LocationState

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [capsLock, setCapsLock] = useState(false)
  const [submitting, setSubmitting] = useState<string | null>(null) // email que se está autenticando
  const [error, setError] = useState<string | null>(null)
  const [shakeKey, setShakeKey] = useState(0)

  // Con sesión: ir a donde quería entrar o a su pantalla de inicio
  if (user) return <Navigate to={state.from ?? homeByRole[user.role]} replace />

  async function authenticate(credentials: { email: string; password: string }) {
    setSubmitting(credentials.email)
    setError(null)
    try {
      await login(credentials) // al guardar el usuario, el render siguiente redirige
    } catch (err) {
      setError(getErrorMessage(err, 'No se pudo iniciar sesión.'))
      setShakeKey((k) => k + 1) // reinicia la animación de "sacudida"
      setSubmitting(null)
    }
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    void authenticate({ email, password })
  }

  const detectCaps = (e: KeyboardEvent<HTMLInputElement>) => setCapsLock(e.getModifierState('CapsLock'))

  return (
    <div className="grid min-h-dvh lg:grid-cols-[1.05fr_1fr]">
      {/* ---------- Panel de marca (solo escritorio) ---------- */}
      <aside className="relative hidden overflow-hidden bg-[#0f1220] p-12 text-white lg:flex lg:flex-col">
        <div className="absolute inset-0 bg-[radial-gradient(60rem_40rem_at_-10%_-10%,rgba(99,102,241,0.45),transparent_60%),radial-gradient(40rem_30rem_at_110%_110%,rgba(139,92,246,0.35),transparent_60%)]" aria-hidden />
        <div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,0.04)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.04)_1px,transparent_1px)] bg-[size:48px_48px] [mask-image:radial-gradient(ellipse_at_center,black,transparent_75%)]" aria-hidden />

        <div className="relative">
          <Logo className="size-9" textClassName="text-lg text-white" />
        </div>

        <div className="relative my-auto max-w-lg">
          <p className="mb-4 inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/5 px-3 py-1 text-xs font-medium text-indigo-200">
            <span className="size-1.5 rounded-full bg-emerald-400" aria-hidden /> Inventario y ventas en tiempo real
          </p>
          <h1 className="text-4xl leading-tight font-semibold tracking-tight">
            Controla tu inventario y tus ventas <span className="bg-gradient-to-r from-indigo-300 to-violet-300 bg-clip-text text-transparent">desde un solo lugar.</span>
          </h1>
          <ul className="mt-8 space-y-4 text-[15px] text-slate-300">
            {[
              { icon: <ShoppingCart />, text: 'Punto de venta con IVA y control de stock al instante' },
              { icon: <Boxes />, text: 'Existencias por bodega, transferencias y kardex' },
              { icon: <BarChart3 />, text: 'Reportes de ventas e integración con SAP Business One' },
            ].map((feature) => (
              <li key={feature.text} className="flex items-center gap-3">
                <span className="rounded-lg border border-white/10 bg-white/5 p-2 text-indigo-200 [&_svg]:size-4">{feature.icon}</span>
                {feature.text}
              </li>
            ))}
          </ul>

          {/* Vista previa decorativa de métricas */}
          <div className="mt-10 grid max-w-md grid-cols-2 gap-3" aria-hidden>
            <div className="rounded-xl border border-white/10 bg-white/[0.06] p-4 backdrop-blur">
              <p className="text-xs text-slate-400">Ventas de hoy</p>
              <p className="mt-1 text-2xl font-semibold">$1.248,30</p>
              <p className="mt-1 text-xs font-medium text-emerald-300">↑ 12% vs. ayer</p>
            </div>
            <div className="rounded-xl border border-white/10 bg-white/[0.06] p-4 backdrop-blur">
              <p className="text-xs text-slate-400">Últimos 7 días</p>
              <div className="mt-3 flex h-10 items-end gap-1.5">
                {[40, 65, 45, 80, 60, 90, 72].map((h, i) => (
                  <span key={i} className="flex-1 rounded-t-[3px] bg-indigo-400/80" style={{ height: `${h}%` }} />
                ))}
              </div>
            </div>
          </div>
        </div>

        <p className="relative text-xs text-slate-500">© {CURRENT_YEAR} RetailStock · Comisariato y retail</p>
      </aside>

      {/* ---------- Formulario ---------- */}
      <main className="relative flex items-center justify-center px-5 py-12 sm:px-10">
        <button
          type="button"
          onClick={toggle}
          aria-label={resolved === 'dark' ? 'Activar tema claro' : 'Activar tema oscuro'}
          className="absolute top-5 right-5 rounded-lg p-2 text-fg-muted transition-colors hover:bg-surface-hover hover:text-fg"
        >
          {resolved === 'dark' ? <Sun className="size-[18px]" /> : <Moon className="size-[18px]" />}
        </button>

        <div className="w-full max-w-[400px]">
          <div className="mb-8 lg:hidden"><Logo className="size-9" textClassName="text-lg" /></div>

          <h2 className="text-2xl font-semibold tracking-tight">Bienvenido de nuevo</h2>
          <p className="mt-1.5 text-sm text-fg-muted">Ingresa con tu cuenta para continuar.</p>

          <form key={shakeKey} onSubmit={handleSubmit} className={cn('mt-8 space-y-5', shakeKey > 0 && 'animate-shake')} noValidate>
            {logoutReason === 'expired' && <Alert tone="info">Tu sesión expiró. Vuelve a iniciar sesión.</Alert>}
            {error && <Alert>{error}</Alert>}

            <Field label="Correo electrónico" htmlFor="email">
              <Input id="email" type="email" autoComplete="username" autoFocus required placeholder="nombre@empresa.com" value={email} onChange={(e) => setEmail(e.target.value)} leftIcon={<Mail />} className="h-11" />
            </Field>

            <Field label="Contraseña" htmlFor="password" hint={capsLock ? '⇪ Bloq Mayús está activado' : undefined}>
              <Input
                id="password"
                type={showPassword ? 'text' : 'password'}
                autoComplete="current-password"
                required
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                onKeyUp={detectCaps}
                onKeyDown={detectCaps}
                leftIcon={<Lock />}
                className="h-11"
                rightSlot={
                  <button type="button" onClick={() => setShowPassword((s) => !s)} aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'} className="rounded-md p-2 text-fg-subtle hover:text-fg">
                    {showPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                  </button>
                }
              />
            </Field>

            <Button type="submit" size="lg" className="w-full" loading={submitting === email && !!email} disabled={!email || !password || submitting !== null}>
              Iniciar sesión <ArrowRight className="size-4" />
            </Button>
          </form>

          <div className="mt-10">
            <div className="relative flex items-center">
              <span className="flex-1 border-t border-border" />
              <span className="px-3 text-xs font-medium text-fg-subtle">Acceso rápido de demostración</span>
              <span className="flex-1 border-t border-border" />
            </div>
            <div className="mt-4 grid grid-cols-1 gap-2">
              {demoUsers.map((demo) => (
                <button
                  key={demo.email}
                  type="button"
                  disabled={submitting !== null}
                  onClick={() => void authenticate(demo)}
                  className="group flex w-full min-w-0 items-center gap-3 rounded-xl border border-border bg-surface p-3 text-left transition-all hover:border-primary/40 hover:shadow-card disabled:opacity-60"
                >
                  <span className="rounded-lg bg-primary-soft p-2 text-primary [&_svg]:size-4">{demo.icon}</span>
                  <span className="min-w-0 flex-1">
                    <span className="block text-sm font-medium">{demo.label}</span>
                    <span className="block truncate text-xs text-fg-muted">{demo.description} · {demo.email}</span>
                  </span>
                  {submitting === demo.email ? (
                    <span className="size-4 animate-spin rounded-full border-2 border-primary border-t-transparent" aria-label="Ingresando" />
                  ) : (
                    <ArrowRight className="size-4 text-fg-subtle transition-transform group-hover:translate-x-0.5 group-hover:text-primary" aria-hidden />
                  )}
                </button>
              ))}
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}
