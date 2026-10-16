import { Check, Package, Search, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { productsApi, type Product } from '../../features/products/productsApi'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { cn } from '../../lib/cn'
import { formatMoney } from '../../lib/format'

/**
 * Combobox de productos con búsqueda remota (nombre o código).
 * Teclado: ↑/↓ para moverse, Enter para elegir, Escape para cerrar.
 */
export function ProductPicker({ id, value, onChange, invalid }: {
  id: string
  value: Pick<Product, 'id' | 'code' | 'name'> | null
  onChange: (product: Product | null) => void
  invalid?: boolean
}) {
  const [query, setQuery] = useState('')
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(0)
  const ref = useRef<HTMLDivElement>(null)
  const debounced = useDebounce(query.trim(), 250)

  const results = useApiQuery(
    (signal) => (open ? productsApi.list({ page: 1, pageSize: 8, name: debounced || undefined }, signal) : Promise.resolve(null)),
    [open, debounced],
  )
  const items = results.data?.items ?? []

  useEffect(() => {
    if (!open) return
    const onPointer = (e: PointerEvent) => { if (!ref.current?.contains(e.target as Node)) setOpen(false) }
    document.addEventListener('pointerdown', onPointer)
    return () => document.removeEventListener('pointerdown', onPointer)
  }, [open])

  function select(product: Product) {
    onChange(product)
    setQuery('')
    setOpen(false)
  }

  if (value && !open) {
    return (
      <div className="flex h-10 items-center gap-3 rounded-lg border border-border bg-surface px-3 shadow-xs">
        <Package className="size-4 shrink-0 text-fg-subtle" />
        <span className="min-w-0 flex-1 truncate text-sm"><span className="font-medium">{value.name}</span> <span className="font-mono text-xs text-fg-muted">{value.code}</span></span>
        <button type="button" onClick={() => { onChange(null); setOpen(true) }} aria-label="Cambiar producto" className="rounded-md p-1 text-fg-subtle hover:bg-surface-hover hover:text-fg"><X className="size-4" /></button>
      </div>
    )
  }

  return (
    <div ref={ref} className="relative">
      <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-fg-subtle" aria-hidden />
      <input
        id={id}
        role="combobox"
        aria-expanded={open}
        aria-controls={`${id}-list`}
        aria-invalid={invalid || undefined}
        autoComplete="off"
        value={query}
        placeholder="Buscar producto por nombre o código…"
        onFocus={() => setOpen(true)}
        onChange={(e) => { setQuery(e.target.value); setActive(0); setOpen(true) }}
        onKeyDown={(e) => {
          if (e.key === 'ArrowDown') { e.preventDefault(); setActive((i) => Math.min(i + 1, items.length - 1)) }
          if (e.key === 'ArrowUp') { e.preventDefault(); setActive((i) => Math.max(i - 1, 0)) }
          if (e.key === 'Enter' && items[active]) { e.preventDefault(); select(items[active]) }
          if (e.key === 'Escape') setOpen(false)
        }}
        className="block h-10 w-full rounded-lg border border-border bg-surface pr-3 pl-9 text-sm shadow-xs placeholder:text-fg-subtle focus:border-primary focus:ring-3 focus:ring-ring focus:outline-none aria-invalid:border-red-400"
      />
      {open && (
        <ul id={`${id}-list`} role="listbox" className="scrollbar-thin absolute z-40 mt-1 max-h-72 w-full animate-scale-in overflow-y-auto rounded-xl border border-border bg-surface p-1 shadow-pop">
          {results.loading && items.length === 0 && <li className="px-3 py-6 text-center text-sm text-fg-muted">Buscando…</li>}
          {!results.loading && items.length === 0 && <li className="px-3 py-6 text-center text-sm text-fg-muted">Sin resultados</li>}
          {items.map((p, i) => (
            <li key={p.id}>
              <button
                type="button"
                role="option"
                aria-selected={i === active}
                onMouseMove={() => setActive(i)}
                onClick={() => select(p)}
                className={cn('flex w-full items-center gap-3 rounded-lg px-3 py-2 text-left', i === active && 'bg-surface-hover')}
              >
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm font-medium">{p.name}</span>
                  <span className="block font-mono text-xs text-fg-muted">{p.code} · {p.categoryName}</span>
                </span>
                <span className="tabular text-sm text-fg-muted">{formatMoney(p.price)}</span>
                {value?.id === p.id && <Check className="size-4 text-primary" />}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
