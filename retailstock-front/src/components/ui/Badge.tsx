import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'

export type BadgeTone = 'neutral' | 'primary' | 'success' | 'warning' | 'danger' | 'info'

const tones: Record<BadgeTone, string> = {
  neutral: 'bg-surface-muted text-fg-muted ring-border',
  primary: 'bg-primary-soft text-primary-soft-fg ring-primary/20',
  success: 'bg-emerald-50 text-emerald-700 ring-emerald-600/15 dark:bg-emerald-500/10 dark:text-emerald-300 dark:ring-emerald-400/20',
  warning: 'bg-amber-50 text-amber-800 ring-amber-600/20 dark:bg-amber-500/10 dark:text-amber-300 dark:ring-amber-400/20',
  danger: 'bg-red-50 text-red-700 ring-red-600/15 dark:bg-red-500/10 dark:text-red-300 dark:ring-red-400/20',
  info: 'bg-sky-50 text-sky-700 ring-sky-600/15 dark:bg-sky-500/10 dark:text-sky-300 dark:ring-sky-400/20',
}

const dots: Record<BadgeTone, string> = {
  neutral: 'bg-fg-subtle', primary: 'bg-primary', success: 'bg-emerald-500', warning: 'bg-amber-500', danger: 'bg-red-500', info: 'bg-sky-500',
}

/** Etiqueta de estado. El punto + texto evita depender solo del color. */
export function Badge({ tone = 'neutral', dot = false, icon, children, className }: {
  tone?: BadgeTone
  dot?: boolean
  icon?: ReactNode
  children: ReactNode
  className?: string
}) {
  return (
    <span className={cn('inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium whitespace-nowrap ring-1 ring-inset', tones[tone], className)}>
      {dot && <span className={cn('size-1.5 rounded-full', dots[tone])} aria-hidden />}
      {icon}
      {children}
    </span>
  )
}
