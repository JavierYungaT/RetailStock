import { X } from 'lucide-react'
import { useEffect, useRef, type ReactNode } from 'react'
import { cn } from '../../lib/cn'

/**
 * Panel lateral (slide-over) para formularios y detalles: mantiene visible el listado de fondo,
 * típico de los CRM. También es un <dialog> nativo (foco, Escape, fondo inerte).
 */
export function Drawer({ open, onClose, title, description, children, footer, width = 'md' }: {
  open: boolean
  onClose: () => void
  title: ReactNode
  description?: ReactNode
  children: ReactNode
  footer?: ReactNode
  width?: 'md' | 'lg'
}) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={ref}
      onClose={onClose}
      onClick={(e) => { if (e.target === ref.current) onClose() }}
      className={cn(
        'fixed inset-y-0 right-0 left-auto m-0 h-dvh max-h-dvh w-full border-l border-border bg-surface p-0 shadow-pop open:animate-slide-in-right',
        width === 'lg' ? 'max-w-2xl' : 'max-w-md',
      )}
    >
      {open && (
        <div className="flex h-full flex-col">
          <div className="flex items-start justify-between gap-4 border-b border-border px-6 py-5">
            <div className="min-w-0">
              <h2 className="truncate text-base font-semibold">{title}</h2>
              {description && <p className="mt-1 text-sm text-fg-muted">{description}</p>}
            </div>
            <button type="button" onClick={onClose} aria-label="Cerrar" className="-mt-1 -mr-2 rounded-lg p-1.5 text-fg-subtle hover:bg-surface-hover hover:text-fg">
              <X className="size-4" />
            </button>
          </div>
          <div className="scrollbar-thin flex-1 overflow-y-auto px-6 py-5">{children}</div>
          {footer && <div className="flex justify-end gap-2 border-t border-border bg-surface-muted/40 px-6 py-4">{footer}</div>}
        </div>
      )}
    </dialog>
  )
}
