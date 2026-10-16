import { cn } from '../../lib/cn'

/** Bloque gris animado que reserva el espacio mientras cargan los datos (evita saltos de layout). */
export function Skeleton({ className }: { className?: string }) {
  return <span aria-hidden className={cn('block animate-pulse rounded-md bg-surface-muted', className)} />
}
