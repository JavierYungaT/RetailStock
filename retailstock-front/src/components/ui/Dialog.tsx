import { X } from 'lucide-react'
import { useEffect, useRef, type ReactNode } from 'react'
import { cn } from '../../lib/cn'

/**
 * Diálogo modal con <dialog> nativo: el navegador gestiona foco atrapado, Escape y fondo inerte.
 * size: ancho máximo. footer: acciones fijas abajo.
 */
export function Dialog({ open, onClose, title, description, children, footer, size = 'md', className }: {
  open: boolean
  onClose: () => void
  title: ReactNode
  description?: ReactNode
  children?: ReactNode
  footer?: ReactNode
  size?: 'sm' | 'md' | 'lg'
  className?: string
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
      onClick={(e) => { if (e.target === ref.current) onClose() }} // clic en el fondo cierra
      className={cn(
        'm-auto w-[calc(100%-2rem)] rounded-2xl border border-border bg-surface p-0 shadow-pop open:animate-scale-in',
        size === 'sm' ? 'max-w-sm' : size === 'lg' ? 'max-w-2xl' : 'max-w-lg',
        className,
      )}
    >
      {open && (
        <div className="flex max-h-[85dvh] flex-col">
          <div className="flex items-start justify-between gap-4 px-6 pt-5 pb-4">
            <div>
              <h2 className="text-base font-semibold">{title}</h2>
              {description && <p className="mt-1 text-sm text-fg-muted">{description}</p>}
            </div>
            <button type="button" onClick={onClose} aria-label="Cerrar" className="-mt-1 -mr-2 rounded-lg p-1.5 text-fg-subtle hover:bg-surface-hover hover:text-fg">
              <X className="size-4" />
            </button>
          </div>
          {children && <div className="scrollbar-thin overflow-y-auto px-6 pb-5">{children}</div>}
          {footer && <div className="flex justify-end gap-2 border-t border-border bg-surface-muted/40 px-6 py-3.5">{footer}</div>}
        </div>
      )}
    </dialog>
  )
}
