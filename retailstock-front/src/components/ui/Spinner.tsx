import { LoaderCircle } from 'lucide-react'
import { cn } from '../../lib/cn'

export function Spinner({ label = 'Cargando…', className }: { label?: string; className?: string }) {
  return (
    <div role="status" className={cn('flex items-center justify-center gap-2 text-sm text-fg-muted', className)}>
      <LoaderCircle className="size-4 animate-spin" aria-hidden />
      <span>{label}</span>
    </div>
  )
}
