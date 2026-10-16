import { ChevronLeft, ChevronRight } from 'lucide-react'
import { formatInteger } from '../../lib/format'

/** "Mostrando 1–10 de 42" + anterior/siguiente. */
export function Pagination({ page, pageSize, totalPages, totalCount, onPageChange }: {
  page: number
  pageSize: number
  totalPages: number
  totalCount: number
  onPageChange: (page: number) => void
}) {
  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)
  const button = 'inline-flex size-8 items-center justify-center rounded-lg border border-border bg-surface text-fg-muted hover:bg-surface-hover hover:text-fg disabled:pointer-events-none disabled:opacity-40'
  return (
    <div className="flex items-center justify-between gap-3 border-t border-border px-4 py-3 text-[13px] text-fg-muted">
      <span className="tabular">
        Mostrando <span className="font-medium text-fg">{from}–{to}</span> de <span className="font-medium text-fg">{formatInteger(totalCount)}</span>
      </span>
      <div className="flex items-center gap-2">
        <span className="tabular hidden sm:inline">Página {page} de {Math.max(totalPages, 1)}</span>
        <button type="button" className={button} onClick={() => onPageChange(page - 1)} disabled={page <= 1} aria-label="Página anterior"><ChevronLeft className="size-4" /></button>
        <button type="button" className={button} onClick={() => onPageChange(page + 1)} disabled={page >= totalPages} aria-label="Página siguiente"><ChevronRight className="size-4" /></button>
      </div>
    </div>
  )
}
