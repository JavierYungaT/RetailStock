import { CircleAlert, CircleCheck, Info } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'

type Tone = 'error' | 'success' | 'info'

const styles: Record<Tone, string> = {
  error: 'border-red-200 bg-red-50 text-red-800 dark:border-red-500/30 dark:bg-red-500/10 dark:text-red-200',
  success: 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-200',
  info: 'border-primary/20 bg-primary-soft text-primary-soft-fg',
}
const icons = { error: CircleAlert, success: CircleCheck, info: Info }

export function Alert({ tone = 'error', title, children, className }: { tone?: Tone; title?: string; children?: ReactNode; className?: string }) {
  const Icon = icons[tone]
  return (
    <div role={tone === 'error' ? 'alert' : 'status'} className={cn('flex gap-3 rounded-lg border px-3.5 py-3 text-sm', styles[tone], className)}>
      <Icon className="mt-0.5 size-4 shrink-0" aria-hidden />
      <div className="min-w-0">
        {title && <p className="font-medium">{title}</p>}
        {children && <div className={title ? 'mt-0.5 opacity-90' : ''}>{children}</div>}
      </div>
    </div>
  )
}
