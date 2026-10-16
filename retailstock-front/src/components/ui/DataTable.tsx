import { RefreshCw } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'
import { Button } from './Button'
import { Skeleton } from './Skeleton'

export interface Column<T> {
  key: string
  header: ReactNode
  cell: (row: T) => ReactNode
  align?: 'left' | 'right' | 'center'
  /** Oculta la columna en pantallas pequeñas */
  hideBelow?: 'sm' | 'md' | 'lg'
  className?: string
}

const hideClass = { sm: 'hidden sm:table-cell', md: 'hidden md:table-cell', lg: 'hidden lg:table-cell' }
const alignClass = { left: 'text-left', right: 'text-right', center: 'text-center' }

/**
 * Tabla genérica de los listados: cabecera fija, filas con hover, esqueletos al cargar,
 * estado vacío y de error. Es genérica (<T>) como un componente con generics en Java.
 */
export function DataTable<T>({ columns, rows, rowKey, loading, error, onRetry, empty, onRowClick, footer, skeletonRows = 6 }: {
  columns: Column<T>[]
  rows: T[] | undefined
  rowKey: (row: T) => string | number
  loading?: boolean
  error?: string | null
  onRetry?: () => void
  empty?: ReactNode
  onRowClick?: (row: T) => void
  footer?: ReactNode
  skeletonRows?: number
}) {
  const initialLoad = loading && !rows

  return (
    <div className="overflow-hidden rounded-xl border border-border bg-surface shadow-card">
      {/* relative: contiene elementos absolutos (sr-only) dentro del scroll horizontal */}
      <div className="scrollbar-thin relative overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="border-b border-border bg-surface-muted/60">
            <tr>
              {columns.map((c) => (
                <th key={c.key} scope="col" className={cn('px-4 py-2.5 text-xs font-medium whitespace-nowrap text-fg-muted', alignClass[c.align ?? 'left'], c.hideBelow && hideClass[c.hideBelow], c.className)}>
                  {c.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className={cn('divide-y divide-border transition-opacity', loading && rows && 'opacity-60')}>
            {initialLoad &&
              Array.from({ length: skeletonRows }, (_, i) => (
                <tr key={i}>
                  {columns.map((c) => (
                    <td key={c.key} className={cn('px-4 py-3.5', c.hideBelow && hideClass[c.hideBelow])}>
                      <Skeleton className={cn('h-4', i % 2 ? 'w-2/3' : 'w-4/5', c.align === 'right' && 'ml-auto w-16')} />
                    </td>
                  ))}
                </tr>
              ))}
            {rows?.map((row) => (
              <tr
                key={rowKey(row)}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                className={cn('transition-colors hover:bg-surface-hover', onRowClick && 'cursor-pointer')}
              >
                {columns.map((c) => (
                  <td key={c.key} className={cn('px-4 py-3', alignClass[c.align ?? 'left'], c.hideBelow && hideClass[c.hideBelow], c.className)}>
                    {c.cell(row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {error && (
        <div role="alert" className="flex flex-col items-center gap-3 border-t border-border px-6 py-10 text-center">
          <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          {onRetry && <Button variant="secondary" size="sm" icon={<RefreshCw className="size-3.5" />} onClick={onRetry}>Reintentar</Button>}
        </div>
      )}
      {!error && rows && rows.length === 0 && <div className="border-t border-border">{empty}</div>}
      {footer}
    </div>
  )
}
