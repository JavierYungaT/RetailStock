import { cn } from '../../lib/cn'

/** Selector de opciones excluyentes (7d / 14d / 30d, Ingreso / Egreso...). */
export function SegmentedControl<T extends string | number>({ options, value, onChange, label }: {
  options: { value: T; label: string }[]
  value: T
  onChange: (value: T) => void
  label: string
}) {
  return (
    <div role="radiogroup" aria-label={label} className="inline-flex rounded-lg border border-border bg-surface-muted p-0.5">
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          role="radio"
          aria-checked={option.value === value}
          onClick={() => onChange(option.value)}
          className={cn(
            'rounded-md px-3 py-1 text-[13px] font-medium transition-all',
            option.value === value ? 'bg-surface text-fg shadow-card' : 'text-fg-muted hover:text-fg',
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}
