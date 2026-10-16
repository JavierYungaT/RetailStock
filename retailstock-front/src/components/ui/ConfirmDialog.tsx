import { TriangleAlert } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Button } from './Button'
import { Dialog } from './Dialog'

/**
 * Confirmación para acciones sensibles (eliminar, desactivar). onConfirm puede ser async:
 * el botón muestra carga y el diálogo se cierra solo si termina bien.
 */
export function ConfirmDialog({ open, onClose, onConfirm, title, description, confirmLabel = 'Confirmar', tone = 'danger' }: {
  open: boolean
  onClose: () => void
  onConfirm: () => Promise<void> | void
  title: string
  description: ReactNode
  confirmLabel?: string
  tone?: 'danger' | 'primary'
}) {
  const [busy, setBusy] = useState(false)

  async function handleConfirm() {
    setBusy(true)
    try {
      await onConfirm()
      onClose()
    } catch {
      // El llamador muestra el error (toast); el diálogo queda abierto para reintentar o cancelar
    } finally {
      setBusy(false)
    }
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      size="sm"
      title={
        <span className="flex items-center gap-3">
          <span className={tone === 'danger' ? 'rounded-full bg-red-50 p-2 text-red-600 dark:bg-red-500/10 dark:text-red-400' : 'rounded-full bg-primary-soft p-2 text-primary'}>
            <TriangleAlert className="size-4" />
          </span>
          {title}
        </span>
      }
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={busy}>Cancelar</Button>
          <Button variant={tone === 'danger' ? 'danger' : 'primary'} onClick={handleConfirm} loading={busy}>{confirmLabel}</Button>
        </>
      }
    >
      <div className="text-sm text-fg-muted">{description}</div>
    </Dialog>
  )
}
