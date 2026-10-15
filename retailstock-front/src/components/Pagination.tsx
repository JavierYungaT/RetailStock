import { formatInteger } from '../lib/format'

/** Controles de paginación a partir del PagedResult de la API. */
export function Pagination({ page, totalPages, totalCount, onPageChange }: {
  page: number
  totalPages: number
  totalCount: number
  onPageChange: (page: number) => void
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-slate-600">
      <span>{formatInteger(totalCount)} resultado{totalCount === 1 ? '' : 's'}</span>
      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
        >
          ← Anterior
        </button>
        <span className="tabular px-1">
          Página {page} de {Math.max(totalPages, 1)}
        </span>
        <button
          type="button"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
        >
          Siguiente →
        </button>
      </div>
    </div>
  )
}
