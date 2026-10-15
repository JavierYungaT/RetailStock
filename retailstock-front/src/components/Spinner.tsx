/** Indicador de carga accesible (role="status" lo anuncia a lectores de pantalla). */
export function Spinner({ label = 'Cargando…', className = '' }: { label?: string; className?: string }) {
  return (
    <div role="status" className={`flex items-center justify-center gap-2 text-sm text-slate-500 ${className}`}>
      <span className="size-4 animate-spin rounded-full border-2 border-slate-300 border-t-brand-500" aria-hidden />
      <span>{label}</span>
    </div>
  )
}
