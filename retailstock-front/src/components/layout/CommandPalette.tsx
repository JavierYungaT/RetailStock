import { CornerDownLeft, LogOut, Moon, Search, Sun } from 'lucide-react'
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router'
import { useAuth } from '../../features/auth/AuthContext'
import { cn } from '../../lib/cn'
import { useTheme } from '../../theme/ThemeProvider'
import { navigationFor } from './navigation'

interface Command {
  id: string
  label: string
  group: string
  icon: ReactNode
  keywords?: string
  run: () => void
}

/** Paleta de comandos (Ctrl/⌘ + K): navegar y ejecutar acciones sin el mouse. */
export function CommandPalette({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { user, logout } = useAuth()
  const { resolved, toggle } = useTheme()
  const navigate = useNavigate()
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [query, setQuery] = useState('')
  const [active, setActive] = useState(0)

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    if (open && !dialog.open) { setQuery(''); setActive(0); dialog.showModal() }
    if (!open && dialog.open) dialog.close()
  }, [open])

  const commands = useMemo<Command[]>(() => {
    if (!user) return []
    const pages = navigationFor(user.role).flatMap((group) =>
      group.items.map((item) => ({ id: item.to, label: item.label, group: 'Ir a', icon: item.icon, keywords: item.keywords, run: () => navigate(item.to) })),
    )
    return [
      ...pages,
      { id: 'theme', label: resolved === 'dark' ? 'Cambiar a tema claro' : 'Cambiar a tema oscuro', group: 'Acciones', icon: resolved === 'dark' ? <Sun /> : <Moon />, keywords: 'tema modo oscuro claro', run: toggle },
      { id: 'logout', label: 'Cerrar sesión', group: 'Acciones', icon: <LogOut />, keywords: 'salir', run: () => logout() },
    ]
  }, [user, resolved, toggle, logout, navigate])

  const normalize = (text: string) => text.toLowerCase().normalize('NFD').replace(/\p{Diacritic}/gu, '')
  const filtered = commands.filter((c) => normalize(`${c.label} ${c.keywords ?? ''}`).includes(normalize(query.trim())))

  function execute(command: Command | undefined) {
    if (!command) return
    onClose()
    command.run()
  }

  function onKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'ArrowDown') { e.preventDefault(); setActive((i) => Math.min(i + 1, filtered.length - 1)) }
    if (e.key === 'ArrowUp') { e.preventDefault(); setActive((i) => Math.max(i - 1, 0)) }
    if (e.key === 'Enter') { e.preventDefault(); execute(filtered[active]) }
  }

  let lastGroup = ''
  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      onClick={(e) => { if (e.target === dialogRef.current) onClose() }}
      className="mx-auto mt-[12vh] w-[calc(100%-2rem)] max-w-xl rounded-2xl border border-border bg-surface p-0 shadow-pop open:animate-scale-in"
    >
      {open && (
        <div onKeyDown={onKeyDown}>
          <div className="flex items-center gap-3 border-b border-border px-4">
            <Search className="size-4 text-fg-subtle" aria-hidden />
            <input
              autoFocus
              value={query}
              onChange={(e) => { setQuery(e.target.value); setActive(0) }}
              placeholder="Buscar páginas y acciones…"
              aria-label="Buscar comandos"
              className="h-13 flex-1 bg-transparent text-sm outline-none placeholder:text-fg-subtle"
            />
            <kbd className="rounded border border-border px-1.5 py-0.5 text-[11px] text-fg-muted">Esc</kbd>
          </div>
          <ul role="listbox" className="scrollbar-thin max-h-80 overflow-y-auto p-2">
            {filtered.length === 0 && <li className="px-3 py-8 text-center text-sm text-fg-muted">Sin resultados para “{query}”.</li>}
            {filtered.map((command, index) => {
              const header = command.group !== lastGroup ? command.group : null
              lastGroup = command.group
              return (
                <li key={command.id}>
                  {header && <p className="px-3 pt-2 pb-1 text-[11px] font-semibold tracking-wider text-fg-subtle uppercase">{header}</p>}
                  <button
                    type="button"
                    role="option"
                    aria-selected={index === active}
                    onMouseMove={() => setActive(index)}
                    onClick={() => execute(command)}
                    className={cn('flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left text-sm [&_svg]:size-4', index === active ? 'bg-primary-soft text-primary-soft-fg' : 'text-fg')}
                  >
                    <span className={index === active ? '' : 'text-fg-subtle'}>{command.icon}</span>
                    <span className="flex-1">{command.label}</span>
                    {index === active && <CornerDownLeft className="size-3.5 opacity-60" aria-hidden />}
                  </button>
                </li>
              )
            })}
          </ul>
        </div>
      )}
    </dialog>
  )
}
