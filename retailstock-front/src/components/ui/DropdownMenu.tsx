import { Ellipsis } from 'lucide-react'
import { useEffect, useRef, useState, type CSSProperties, type ReactNode } from 'react'
import { cn } from '../../lib/cn'

export interface MenuItem {
  label: string
  icon?: ReactNode
  onSelect: () => void
  tone?: 'default' | 'danger'
  disabled?: boolean
}

const MENU_WIDTH = 192
const ESTIMATED_ITEM_HEIGHT = 38

/**
 * Menú de acciones de fila (⋯).
 * Se posiciona con "position: fixed" calculada desde el botón: así no lo recorta el contenedor con
 * scroll de la tabla (overflow) y se abre hacia arriba si no hay espacio abajo.
 * Cierra con clic afuera, Escape, scroll o al redimensionar.
 */
export function DropdownMenu({ items, label = 'Acciones', trigger }: { items: MenuItem[]; label?: string; trigger?: ReactNode }) {
  const [style, setStyle] = useState<CSSProperties | null>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)
  const menuRef = useRef<HTMLDivElement>(null)
  const open = style !== null

  function toggle() {
    if (open) return setStyle(null)
    const rect = buttonRef.current!.getBoundingClientRect()
    const height = items.length * ESTIMATED_ITEM_HEIGHT + 8
    const openUp = rect.bottom + height > window.innerHeight - 8
    setStyle({
      position: 'fixed',
      top: openUp ? Math.max(8, rect.top - height - 4) : rect.bottom + 4,
      left: Math.min(Math.max(8, rect.right - MENU_WIDTH), window.innerWidth - MENU_WIDTH - 8),
      width: MENU_WIDTH,
    })
  }

  useEffect(() => {
    if (!open) return
    const close = () => setStyle(null)
    const onPointer = (e: PointerEvent) => {
      const target = e.target as Node
      if (!menuRef.current?.contains(target) && !buttonRef.current?.contains(target)) close()
    }
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') { close(); buttonRef.current?.focus() } }
    document.addEventListener('pointerdown', onPointer)
    document.addEventListener('keydown', onKey)
    window.addEventListener('scroll', close, true)
    window.addEventListener('resize', close)
    // Foco al primer elemento para navegar con teclado
    menuRef.current?.querySelector<HTMLButtonElement>('button:not(:disabled)')?.focus()
    return () => {
      document.removeEventListener('pointerdown', onPointer)
      document.removeEventListener('keydown', onKey)
      window.removeEventListener('scroll', close, true)
      window.removeEventListener('resize', close)
    }
  }, [open])

  return (
    <div className="inline-block text-left" onClick={(e) => e.stopPropagation()}>
      <button
        ref={buttonRef}
        type="button"
        aria-label={label}
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={toggle}
        className={cn('rounded-md p-1.5 text-fg-subtle transition-colors hover:bg-surface-hover hover:text-fg', open && 'bg-surface-hover text-fg')}
      >
        {trigger ?? <Ellipsis className="size-4" />}
      </button>
      {open && (
        <div ref={menuRef} role="menu" style={style} className="z-50 animate-scale-in rounded-xl border border-border bg-surface p-1 shadow-pop">
          {items.map((item) => (
            <button
              key={item.label}
              type="button"
              role="menuitem"
              disabled={item.disabled}
              onClick={() => { setStyle(null); item.onSelect() }}
              className={cn(
                'flex w-full items-center gap-2.5 rounded-lg px-2.5 py-2 text-left text-sm transition-colors outline-none disabled:opacity-40 [&_svg]:size-4',
                item.tone === 'danger'
                  ? 'text-red-600 hover:bg-red-50 focus-visible:bg-red-50 dark:text-red-400 dark:hover:bg-red-500/10 dark:focus-visible:bg-red-500/10'
                  : 'text-fg hover:bg-surface-hover focus-visible:bg-surface-hover',
              )}
            >
              {item.icon}
              {item.label}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
