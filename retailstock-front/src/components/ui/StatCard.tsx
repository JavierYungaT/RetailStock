import { TrendingDown, TrendingUp } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'
import { Skeleton } from './Skeleton'

/** KPI con icono y variación respecto del período anterior (flecha + texto, no solo color). */
export function StatCard({ label, value, icon, delta, deltaLabel, loading, tone = 'primary', footer }: {
  label: string
  value: ReactNode
  icon: ReactNode
  delta?: number | null
  deltaLabel?: string
  loading?: boolean
  tone?: 'primary' | 'warning'
  footer?: ReactNode
}) {
  const positive = (delta ?? 0) >= 0
  return (
    <div className="rounded-xl border border-border bg-surface p-5 shadow-card">
      <div className="flex items-start justify-between gap-3">
        <p className="text-[13px] font-medium text-fg-muted">{label}</p>
        <span className={cn('rounded-lg p-2 [&_svg]:size-4', tone === 'warning' ? 'bg-amber-50 text-amber-600 dark:bg-amber-500/10 dark:text-amber-300' : 'bg-primary-soft text-primary')}>{icon}</span>
      </div>
      {loading ? (
        <Skeleton className="mt-2 h-8 w-28" />
      ) : (
        <p className="tabular mt-1 text-[26px] leading-tight font-semibold tracking-tight">{value}</p>
      )}
      <div className="mt-2 flex min-h-5 items-center gap-1.5 text-xs text-fg-muted">
        {!loading && delta !== undefined && delta !== null && (
          <span className={cn('inline-flex items-center gap-0.5 font-medium', positive ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400')}>
            {positive ? <TrendingUp className="size-3.5" /> : <TrendingDown className="size-3.5" />}
            {positive ? '+' : ''}{delta.toFixed(0)}%
          </span>
        )}
        {!loading && (deltaLabel || footer)}
      </div>
    </div>
  )
}
