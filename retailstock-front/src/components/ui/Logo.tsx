import { useId } from 'react'
import { cn } from '../../lib/cn'

/** Marca de RetailStock: caja isométrica sobre degradado índigo-violeta (igual que el favicon). */
export function Logo({ className, withText = true, textClassName }: { className?: string; withText?: boolean; textClassName?: string }) {
  // id ÚNICO por instancia: con un id fijo, dos logos en la página comparten <linearGradient> y si el
  // primero está oculto (display: none) el segundo pierde el degradado.
  const gradientId = useId()
  return (
    <span className="inline-flex items-center gap-2.5">
      <svg viewBox="0 0 32 32" className={cn('size-8 shrink-0', className)} aria-hidden>
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#6366f1" />
            <stop offset="1" stopColor="#8b5cf6" />
          </linearGradient>
        </defs>
        <rect width="32" height="32" rx="8" fill={`url(#${gradientId})`} />
        <path d="M9 11.5 16 8l7 3.5v9L16 24l-7-3.5z M9 11.5 16 15l7-3.5M16 15v9" fill="none" stroke="#fff" strokeWidth="2" strokeLinejoin="round" />
      </svg>
      {withText && <span className={cn('text-[15px] font-semibold tracking-tight', textClassName)}>RetailStock</span>}
    </span>
  )
}
