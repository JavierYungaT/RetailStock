import { Search, X } from 'lucide-react'
import { Input } from './Field'

export function SearchInput({ value, onChange, placeholder = 'Buscar…', className, autoFocus }: {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  className?: string
  autoFocus?: boolean
}) {
  return (
    <div className={className}>
      <Input
        type="search"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        aria-label={placeholder}
        autoFocus={autoFocus}
        leftIcon={<Search />}
        className="[&::-webkit-search-cancel-button]:hidden"
        rightSlot={value ? (
          <button type="button" onClick={() => onChange('')} aria-label="Limpiar búsqueda" className="rounded-md p-1 text-fg-subtle hover:bg-surface-hover hover:text-fg">
            <X className="size-3.5" />
          </button>
        ) : undefined}
      />
    </div>
  )
}
