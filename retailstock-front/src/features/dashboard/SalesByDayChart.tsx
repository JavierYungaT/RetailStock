import { useState } from 'react'
import { useElementWidth } from '../../hooks/useElementWidth'
import { formatDay, formatInteger, formatMoney } from '../../lib/format'
import type { SalesByDay } from './dashboardApi'

const HEIGHT = 220
const MARGIN = { top: 24, right: 8, bottom: 28, left: 56 }
const MAX_BAR_WIDTH = 24 // columnas delgadas: el resto de la banda queda como aire
const RADIUS = 4 // punta redondeada; la base (sobre el eje) queda recta

/**
 * Columnas de "total vendido por día" en SVG hecho a mano (sin librería de gráficos).
 * Una sola serie -> el título la nombra y no lleva leyenda. Tooltip por columna (hover o foco con teclado)
 * y una vista de tabla alternativa con los mismos datos.
 */
export function SalesByDayChart({ data, today }: { data: SalesByDay[]; today: string }) {
  const { ref, width } = useElementWidth<HTMLDivElement>()
  const [active, setActive] = useState<number | null>(null)
  const [showTable, setShowTable] = useState(false)

  const plotWidth = Math.max(width - MARGIN.left - MARGIN.right, 0)
  const plotHeight = HEIGHT - MARGIN.top - MARGIN.bottom
  const max = niceMax(Math.max(...data.map((d) => d.total), 0))
  const ticks = [0, max / 2, max]
  const band = data.length > 0 ? plotWidth / data.length : 0
  const barWidth = Math.min(MAX_BAR_WIDTH, band * 0.6)
  const y = (value: number) => MARGIN.top + plotHeight - (max === 0 ? 0 : (value / max) * plotHeight)

  const activeDay = active !== null ? data[active] : null

  return (
    <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="mb-4 flex items-start justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-slate-700">Total vendido por día</h2>
          <p className="text-xs text-slate-500">Últimos {data.length} días · USD con IVA</p>
        </div>
        <button type="button" onClick={() => setShowTable((v) => !v)} className="text-xs font-medium text-brand-600 hover:underline">
          {showTable ? 'Ver gráfico' : 'Ver tabla'}
        </button>
      </div>

      {showTable ? (
        <table className="tabular w-full text-sm">
          <thead className="text-left text-xs uppercase tracking-wide text-slate-500">
            <tr><th className="py-1 font-medium">Día</th><th className="py-1 text-right font-medium">Ventas</th><th className="py-1 text-right font-medium">Total</th></tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {data.map((d) => (
              <tr key={d.day}><td className="py-1.5">{formatDay(d.day)}</td><td className="py-1.5 text-right">{formatInteger(d.salesCount)}</td><td className="py-1.5 text-right">{formatMoney(d.total)}</td></tr>
            ))}
          </tbody>
        </table>
      ) : (
        <div ref={ref} className="relative" onMouseLeave={() => setActive(null)}>
          {width > 0 && (
            <svg width={width} height={HEIGHT} role="img" aria-label="Gráfico de columnas: total vendido por día">
              {/* Grilla y eje Y: tenues, para no competir con los datos */}
              {ticks.map((tick) => (
                <g key={tick}>
                  <line x1={MARGIN.left} x2={width - MARGIN.right} y1={y(tick)} y2={y(tick)} className={tick === 0 ? 'stroke-slate-300' : 'stroke-slate-100'} />
                  <text x={MARGIN.left - 8} y={y(tick)} dy="0.32em" textAnchor="end" className="fill-slate-500 text-[11px] tabular">
                    {formatCompactMoney(tick)}
                  </text>
                </g>
              ))}

              {data.map((d, i) => {
                const cx = MARGIN.left + band * i + band / 2
                const top = y(d.total)
                const barHeight = MARGIN.top + plotHeight - top
                const isToday = d.day === today
                return (
                  <g key={d.day}>
                    {barHeight > 0 && (
                      <path
                        d={columnPath(cx - barWidth / 2, top, barWidth, barHeight)}
                        className={active === null || active === i ? 'fill-brand-500' : 'fill-brand-500/40'}
                      />
                    )}
                    {/* Etiqueta directa selectiva: solo el valor de HOY (se oculta si su tooltip está abierto) */}
                    {isToday && active !== i && (
                      <text x={cx} y={top - 6} textAnchor="middle" className="fill-slate-900 text-[11px] font-semibold tabular">
                        {formatMoney(d.total)}
                      </text>
                    )}
                    <text x={cx} y={HEIGHT - 8} textAnchor="middle" className={`text-[11px] ${isToday ? 'fill-slate-900 font-semibold' : 'fill-slate-500'}`}>
                      {isToday ? 'Hoy' : formatDay(d.day).split(',')[0]}
                    </text>
                    {/* Zona de hover/foco: toda la banda, más grande que la columna */}
                    <rect
                      x={cx - band / 2}
                      y={MARGIN.top}
                      width={band}
                      height={plotHeight}
                      fill="transparent"
                      tabIndex={0}
                      aria-label={`${formatDay(d.day)}: ${formatMoney(d.total)} en ${d.salesCount} ventas`}
                      onMouseEnter={() => setActive(i)}
                      onFocus={() => setActive(i)}
                      onBlur={() => setActive(null)}
                      className="cursor-default outline-none"
                    />
                  </g>
                )
              })}
            </svg>
          )}

          {activeDay && active !== null && (
            <div
              className="pointer-events-none absolute z-10 w-max -translate-x-1/2 -translate-y-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs shadow-lg"
              style={{
                left: Math.min(Math.max(MARGIN.left + band * active + band / 2, 70), width - 70),
                top: y(activeDay.total) - 8,
              }}
            >
              <p className="font-medium text-slate-900">{formatDay(activeDay.day)}</p>
              <p className="tabular text-slate-600">{formatMoney(activeDay.total)} · {formatInteger(activeDay.salesCount)} venta{activeDay.salesCount === 1 ? '' : 's'}</p>
            </div>
          )}
        </div>
      )}
    </section>
  )
}

/** Columna con las esquinas superiores redondeadas y la base recta sobre el eje. */
function columnPath(x: number, y: number, w: number, h: number) {
  const r = Math.min(RADIUS, w / 2, h)
  return `M${x},${y + h} V${y + r} Q${x},${y} ${x + r},${y} H${x + w - r} Q${x + w},${y} ${x + w},${y + r} V${y + h} Z`
}

/** Máximo "redondo" del eje (1, 2, 2.5, 5 × 10^n) para que las marcas sean legibles. */
function niceMax(value: number) {
  if (value <= 0) return 10
  const magnitude = 10 ** Math.floor(Math.log10(value))
  const step = [1, 2, 2.5, 5, 10].find((s) => s * magnitude >= value) ?? 10
  return step * magnitude
}

const compact = new Intl.NumberFormat('es-EC', { style: 'currency', currency: 'USD', notation: 'compact', maximumFractionDigits: 1 })
const formatCompactMoney = (value: number) => compact.format(value)
