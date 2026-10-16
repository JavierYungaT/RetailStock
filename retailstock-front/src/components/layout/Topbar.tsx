import { ChevronDown, LogOut, Menu, Monitor, Moon, Search, Sun } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../../features/auth/AuthContext'
import { roleLabels } from '../../features/auth/roles'
import { cn } from '../../lib/cn'
import { useTheme, type ThemePreference } from '../../theme/ThemeProvider'
import { Avatar } from '../ui/Avatar'

const themeOptions: { value: ThemePreference; label: string; icon: typeof Sun }[] = [
  { value: 'light', label: 'Claro', icon: Sun },
  { value: 'dark', label: 'Oscuro', icon: Moon },
  { value: 'system', label: 'Sistema', icon: Monitor },
]

export function Topbar({ onOpenMenu, onOpenCommand }: { onOpenMenu: () => void; onOpenCommand: () => void }) {
  const { user, logout } = useAuth()
  const { preference, setPreference, resolved, toggle } = useTheme()
  const [menuOpen, setMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const isMac = typeof navigator !== 'undefined' && /Mac|iPhone|iPad/.test(navigator.platform)

  useEffect(() => {
    if (!menuOpen) return
    const onPointer = (e: PointerEvent) => { if (!menuRef.current?.contains(e.target as Node)) setMenuOpen(false) }
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') setMenuOpen(false) }
    document.addEventListener('pointerdown', onPointer)
    document.addEventListener('keydown', onKey)
    return () => { document.removeEventListener('pointerdown', onPointer); document.removeEventListener('keydown', onKey) }
  }, [menuOpen])

  if (!user) return null

  return (
    <header className="sticky top-0 z-20 flex h-16 items-center gap-3 border-b border-border bg-surface/80 px-4 backdrop-blur-md sm:px-6">
      <button type="button" onClick={onOpenMenu} aria-label="Abrir menú" className="rounded-lg p-2 text-fg-muted hover:bg-surface-hover lg:hidden">
        <Menu className="size-5" />
      </button>

      {/* Buscador global = abre la paleta de comandos */}
      <button
        type="button"
        onClick={onOpenCommand}
        className="flex h-9 w-full max-w-md items-center gap-2.5 rounded-lg border border-border bg-surface-muted/60 px-3 text-sm text-fg-subtle transition-colors hover:border-border-strong"
      >
        <Search className="size-4" aria-hidden />
        <span className="flex-1 truncate text-left">Buscar o ir a…</span>
        <kbd className="hidden rounded border border-border bg-surface px-1.5 py-0.5 text-[11px] font-medium text-fg-muted sm:inline">{isMac ? '⌘' : 'Ctrl'} K</kbd>
      </button>

      <div className="ml-auto flex items-center gap-1">
        <button type="button" onClick={toggle} aria-label={resolved === 'dark' ? 'Activar tema claro' : 'Activar tema oscuro'} className="rounded-lg p-2 text-fg-muted transition-colors hover:bg-surface-hover hover:text-fg">
          {resolved === 'dark' ? <Sun className="size-[18px]" /> : <Moon className="size-[18px]" />}
        </button>

        <div ref={menuRef} className="relative">
          <button
            type="button"
            onClick={() => setMenuOpen((o) => !o)}
            aria-haspopup="menu"
            aria-expanded={menuOpen}
            className="flex items-center gap-2.5 rounded-lg py-1.5 pr-2 pl-1.5 transition-colors hover:bg-surface-hover"
          >
            <Avatar name={user.email} />
            <span className="hidden text-left leading-tight md:block">
              <span className="block max-w-44 truncate text-[13px] font-medium">{user.email}</span>
              <span className="block text-xs text-fg-muted">{roleLabels[user.role]}</span>
            </span>
            <ChevronDown className="hidden size-4 text-fg-subtle md:block" aria-hidden />
          </button>

          {menuOpen && (
            <div role="menu" className="absolute right-0 z-30 mt-2 w-64 animate-scale-in rounded-xl border border-border bg-surface p-1.5 shadow-pop">
              <div className="flex items-center gap-3 px-2.5 py-2.5">
                <Avatar name={user.email} className="size-9" />
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium">{user.email}</p>
                  <p className="text-xs text-fg-muted">{roleLabels[user.role]}</p>
                </div>
              </div>
              <div className="my-1 border-t border-border" />
              <p className="px-2.5 pt-1.5 pb-1 text-[11px] font-semibold tracking-wider text-fg-subtle uppercase">Tema</p>
              <div className="grid grid-cols-3 gap-1 px-1 pb-1.5">
                {themeOptions.map(({ value, label, icon: Icon }) => (
                  <button
                    key={value}
                    type="button"
                    onClick={() => setPreference(value)}
                    aria-pressed={preference === value}
                    className={cn('flex flex-col items-center gap-1 rounded-lg py-2 text-xs transition-colors', preference === value ? 'bg-primary-soft font-medium text-primary-soft-fg' : 'text-fg-muted hover:bg-surface-hover')}
                  >
                    <Icon className="size-4" /> {label}
                  </button>
                ))}
              </div>
              <div className="my-1 border-t border-border" />
              <button type="button" role="menuitem" onClick={() => logout()} className="flex w-full items-center gap-2.5 rounded-lg px-2.5 py-2 text-sm text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-500/10">
                <LogOut className="size-4" /> Cerrar sesión
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
