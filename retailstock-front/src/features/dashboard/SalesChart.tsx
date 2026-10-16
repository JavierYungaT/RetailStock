import { useState } from 'react'
import { useElementWidth } from '../../hooks/useElementWidth'
import { cn } from '../../lib/cn'
import { formatCompactMoney, formatDay, formatDayMonth, formatInteger, formatMoney } from '../../lib/format'
import type { SalesByDay } from './dashboardApi'

const HEIGHT = 240
const MARGIN = { top: 28, right: 8, bottom: 28, left: 52 }
const MAX_BAR_WIDTH = 24
const RADIUS = 4

/**
 * Columnas de ingresos por día (SVG a mano). Una serie -> sin leyenda (el título la nombra).
 * Tooltip por columna con mouse o teclado, etiqueta directa solo en "hoy", etiquetas del eje X
 * espaciadas según el ancho para que no se encimen. La tabla es la vista alternativa accesible.
 */
export function SalesChart({ data, today, showTable }: { data: SalesByDay[]; today: string; showTable: boolean }) {
  const { ref, width } = useElementWidth<HTMLDivElement>()
  const [active, setActive] = useState<number | null>(null)

  if (showTable) {
    return (
      <div className="scrollbar-thin max-h-[260px] overflow-y-auto">
        <table className="tabular w-full text-sm">
          <thead className="sticky top-0 bg-surface text-left text-xs text-fg-muted">
            <tr><th className="py-2 font-medium">Día</th><th className="py-2 text-right font-medium">Ventas</th><th className="py-2 text-right font-medium">Ingresos</th></tr>
          </thead>
          <tbody className="divide-y divide-border">
            {[...data].reverse().map((d) => (
              <tr key={d.day}><td className="py-2">{formatDay(d.day)}</td><td className="py-2 text-right">{formatInteger(d.salesCount)}</td><td className="py-2 text-right font-medium">{formatMoney(d.total)}</td></tr>
            ))}
          </tbody>
        </table>
      </div>
    )
  }

  const plotWidth = Math.max(width - MARGIN.left - MARGIN.right, 0)
  const plotHeight = HEIGHT - MARGIN.top - MARGIN.bottom
  const max = niceMax(Math.max(...data.map((d) => d.total), 0))
  const ticks = [0, max / 2, max]
  const band = data.length > 0 ? plotWidth / data.length : 0
  const barWidth = Math.max(2, Math.min(MAX_BAR_WIDTH, band * 0.62))
  const labelEvery = Math.max(1, Math.ceil(data.length / Math.max(1, Math.floor(plotWidth / 56))))
  const y = (value: number) => MARGIN.top + plotHeight - (max === 0 ? 0 : (value / max) * plotHeight)
  const activeDay = active !== null ? data[active] : null

  return (
    <div ref={ref} className="relative" onMouseLeave={() => setActive(null)}>
      {width > 0 && (
        <svg width={width} height={HEIGHT} role="img" aria-label="Gráfico de columnas: ingresos por día">
          {ticks.map((tick) => (
            <g key={tick}>
              <line x1={MARGIN.left} x2={width - MARGIN.right} y1={y(tick)} y2={y(tick)} className={tick === 0 ? 'stroke-border-strong' : 'stroke-chart-grid'} />
              <text x={MARGIN.left - 10} y={y(tick)} dy="0.32em" textAnchor="end" className="tabular fill-fg-muted text-[11px]">{formatCompactMoney(tick)}</text>
            </g>
          ))}
          {data.map((d, i) => {
            const cx = MARGIN.left + band * i + band / 2
            const top = y(d.total)
            const barHeight = MARGIN.top + plotHeight - top
            const isToday = d.day === today
            const showLabel = isToday || (data.length - 1 - i) % labelEvery === 0
            return (
              <g key={d.day}>
                {barHeight > 0 && (
                  <path d={columnPath(cx - barWidth / 2, top, barWidth, barHeight)} className={cn('fill-chart-1 transition-opacity', active !== null && active !== i && 'opacity-35')} />
                )}
                {isToday && active !== i && d.total > 0 && (
                  <text x={cx} y={top - 8} textAnchor="middle" className="tabular fill-fg text-[11px] font-semibold">{formatMoney(d.total)}</text>
                )}
                {showLabel && (
                  <text x={cx} y={HEIGHT - 8} textAnchor="middle" className={cn('text-[11px]', isToday ? 'fill-fg font-semibold' : 'fill-fg-muted')}>
                    {isToday ? 'Hoy' : formatDayMonth(d.day)}
                  </text>
                )}
                <rect
                  x={cx - band / 2} y={MARGIN.top} width={band} height={plotHeight} fill="transparent" tabIndex={0}
                  aria-label={`${formatDay(d.day)}: ${formatMoney(d.total)} en ${d.salesCount} ventas`}
                  onMouseEnter={() => setActive(i)} onFocus={() => setActive(i)} onBlur={() => setActive(null)}
                  className="outline-none"
                />
              </g>
            )
          })}
        </svg>
      )}
      {activeDay && active !== null && (
        <div
          className="pointer-events-none absolute z-10 w-max -translate-x-1/2 -translate-y-full rounded-lg border border-border bg-surface px-3 py-2 text-xs shadow-pop"
          style={{ left: Math.min(Math.max(MARGIN.left + band * active + band / 2, 80), width - 80), top: y(activeDay.total) - 10 }}
        >
          <p className="font-medium">{formatDay(activeDay.day)}</p>
          <p className="tabular mt-0.5 text-fg-muted">{formatMoney(activeDay.total)} · {formatInteger(activeDay.salesCount)} venta{activeDay.salesCount === 1 ? '' : 's'}</p>
        </div>
      )}
    </div>
  )
}

function columnPath(x: number, y: number, w: number, h: number) {
  const r = Math.min(RADIUS, w / 2, h)
  return `M${x},${y + h} V${y + r} Q${x},${y} ${x + r},${y} H${x + w - r} Q${x + w},${y} ${x + w},${y + r} V${y + h} Z`
}

function niceMax(value: number) {
  if (value <= 0) return 10
  const magnitude = 10 ** Math.floor(Math.log10(value))
  const step = [1, 2, 2.5, 5, 10].find((s) => s * magnitude >= value) ?? 10
  return step * magnitude
}
