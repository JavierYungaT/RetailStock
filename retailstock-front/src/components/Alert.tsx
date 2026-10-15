import type { ReactNode } from 'react'

type Tone = 'error' | 'success' | 'info'

const styles: Record<Tone, string> = {
  error: 'border-red-200 bg-red-50 text-red-800',
  success: 'border-emerald-200 bg-emerald-50 text-emerald-800',
  info: 'border-brand-100 bg-brand-50 text-brand-700',
}

const icons: Record<Tone, string> = { error: '⚠', success: '✓', info: 'ℹ' }

/** Mensaje de estado con icono + texto (nunca solo color). */
export function Alert({ tone = 'error', title, children, onRetry }: {
  tone?: Tone
  title?: string
  children?: ReactNode
  onRetry?: () => void
}) {
  return (
    <div role={tone === 'error' ? 'alert' : 'status'} className={`flex items-start gap-3 rounded-lg border px-4 py-3 text-sm ${styles[tone]}`}>
      <span aria-hidden className="mt-0.5 font-bold">{icons[tone]}</span>
      <div className="flex-1">
        {title && <p className="font-semibold">{title}</p>}
        {children && <div className={title ? 'mt-0.5' : ''}>{children}</div>}
      </div>
      {onRetry && (
        <button type="button" onClick={onRetry} className="shrink-0 font-medium underline underline-offset-2">
          Reintentar
        </button>
      )}
    </div>
  )
}
