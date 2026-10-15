import { useState } from 'react'
import { NavLink, Outlet } from 'react-router'
import type { Role } from '../api/types'
import { useAuth } from '../features/auth/AuthContext'
import { roleLabels } from '../features/auth/roles'

interface NavItem {
  to: string
  label: string
  roles: Role[]
}

// El menú muestra solo lo que el rol puede usar (la API igual lo valida con [Authorize])
const navItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', roles: ['Admin'] },
  { to: '/sales/new', label: 'Nueva venta', roles: ['Admin', 'Cashier'] },
  { to: '/products', label: 'Productos', roles: ['Admin', 'Cashier', 'Warehouse'] },
]

/** Estructura común de las pantallas autenticadas: barra superior + contenido (<Outlet />). */
export function Layout() {
  const { user, logout } = useAuth()
  const [menuOpen, setMenuOpen] = useState(false)
  if (!user) return null

  const items = navItems.filter((item) => item.roles.includes(user.role))
  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `rounded-lg px-3 py-2 text-sm font-medium transition-colors ${isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'}`

  return (
    <div className="min-h-dvh">
      <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/90 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-7xl items-center gap-6 px-4 sm:px-6">
          <div className="flex items-center gap-2">
            <span className="flex size-8 items-center justify-center rounded-lg bg-brand-500 text-sm font-bold text-white">R</span>
            <span className="font-semibold tracking-tight">RetailStock</span>
          </div>

          <nav className="hidden gap-1 md:flex" aria-label="Principal">
            {items.map((item) => <NavLink key={item.to} to={item.to} className={linkClass}>{item.label}</NavLink>)}
          </nav>

          <div className="ml-auto hidden items-center gap-3 md:flex">
            <div className="text-right leading-tight">
              <p className="text-sm font-medium">{user.email}</p>
              <p className="text-xs text-slate-500">{roleLabels[user.role]}</p>
            </div>
            <button type="button" onClick={() => logout()} className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50">
              Salir
            </button>
          </div>

          <button
            type="button"
            className="ml-auto rounded-lg p-2 text-slate-600 hover:bg-slate-100 md:hidden"
            aria-label="Abrir menú"
            aria-expanded={menuOpen}
            onClick={() => setMenuOpen((open) => !open)}
          >
            ☰
          </button>
        </div>

        {/* Menú móvil */}
        {menuOpen && (
          <div className="border-t border-slate-200 px-4 py-3 md:hidden">
            <nav className="flex flex-col gap-1" aria-label="Principal móvil">
              {items.map((item) => (
                <NavLink key={item.to} to={item.to} className={linkClass} onClick={() => setMenuOpen(false)}>{item.label}</NavLink>
              ))}
            </nav>
            <div className="mt-3 flex items-center justify-between border-t border-slate-100 pt-3">
              <p className="text-sm">{user.email} <span className="text-slate-500">· {roleLabels[user.role]}</span></p>
              <button type="button" onClick={() => logout()} className="text-sm font-medium text-red-600">Salir</button>
            </div>
          </div>
        )}
      </header>

      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <Outlet />
      </main>
    </div>
  )
}
