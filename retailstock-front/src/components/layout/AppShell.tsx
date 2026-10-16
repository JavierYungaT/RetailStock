import { useEffect, useRef, useState } from 'react'
import { Outlet, useLocation } from 'react-router'
import { cn } from '../../lib/cn'
import { CommandPalette } from './CommandPalette'
import { Sidebar } from './Sidebar'
import { Topbar } from './Topbar'

const COLLAPSED_KEY = 'retailstock.sidebar.collapsed'

/** Estructura de las pantallas autenticadas: barra lateral + barra superior + contenido (<Outlet />). */
export function AppShell() {
  const [collapsed, setCollapsed] = useState(() => {
    try { return localStorage.getItem(COLLAPSED_KEY) === '1' } catch { return false }
  })
  const [mobileOpen, setMobileOpen] = useState(false)
  const [commandOpen, setCommandOpen] = useState(false)
  const mobileRef = useRef<HTMLDialogElement>(null)
  const location = useLocation()

  // Ctrl/⌘ + K abre la paleta de comandos desde cualquier pantalla
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); setCommandOpen((o) => !o) }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  useEffect(() => {
    const dialog = mobileRef.current
    if (!dialog) return
    if (mobileOpen && !dialog.open) dialog.showModal()
    if (!mobileOpen && dialog.open) dialog.close()
  }, [mobileOpen])

  // Al navegar se vuelve al inicio de la página (el menú móvil se cierra en el clic: onNavigate)
  useEffect(() => { window.scrollTo({ top: 0 }) }, [location.pathname])

  function toggleCollapsed() {
    setCollapsed((c) => {
      try { localStorage.setItem(COLLAPSED_KEY, c ? '0' : '1') } catch { /* sin storage: solo esta sesión */ }
      return !c
    })
  }

  return (
    <div className="min-h-dvh">
      {/* Escritorio: barra lateral fija */}
      <aside className={cn('fixed inset-y-0 left-0 z-30 hidden transition-[width] duration-200 lg:block', collapsed ? 'w-[72px]' : 'w-64')}>
        <Sidebar collapsed={collapsed} onToggleCollapsed={toggleCollapsed} />
      </aside>

      {/* Móvil: barra lateral como panel deslizante */}
      <dialog
        ref={mobileRef}
        onClose={() => setMobileOpen(false)}
        onClick={(e) => { if (e.target === mobileRef.current) setMobileOpen(false) }}
        className="fixed inset-y-0 left-0 m-0 h-dvh max-h-dvh w-72 max-w-[85vw] p-0 open:animate-fade-in lg:hidden"
      >
        {mobileOpen && <Sidebar collapsed={false} mobile onNavigate={() => setMobileOpen(false)} />}
      </dialog>

      <div className={cn('transition-[padding] duration-200', collapsed ? 'lg:pl-[72px]' : 'lg:pl-64')}>
        <Topbar onOpenMenu={() => setMobileOpen(true)} onOpenCommand={() => setCommandOpen(true)} />
        <main className="mx-auto max-w-[1400px] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
          <Outlet />
        </main>
      </div>

      <CommandPalette open={commandOpen} onClose={() => setCommandOpen(false)} />
    </div>
  )
}
