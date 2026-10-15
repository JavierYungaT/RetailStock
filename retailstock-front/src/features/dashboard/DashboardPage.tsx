import { useMemo, useState } from 'react'
import { Alert } from '../../components/Alert'
import { PageHeader } from '../../components/PageHeader'
import { Spinner } from '../../components/Spinner'
import { useApiQuery } from '../../hooks/useApiQuery'
import { formatInteger, formatMoney, localIsoDate } from '../../lib/format'
import { dashboardApi, type SalesByDay } from './dashboardApi'
import { SalesByDayChart } from './SalesByDayChart'

const DAYS = 7
const THRESHOLDS = [5, 10, 20, 50]

/** Panel del administrador: KPIs del día, tendencia de 7 días y alertas de stock bajo. */
export function DashboardPage() {
  const [threshold, setThreshold] = useState(10)
  const today = localIsoDate()
  const from = localIsoDate(-(DAYS - 1))

  const sales = useApiQuery((signal) => dashboardApi.salesByDay(from, today, signal), [from, today])
  const lowStock = useApiQuery((signal) => dashboardApi.lowStock(threshold, signal), [threshold])

  // El SP solo devuelve días CON ventas: se completan los días vacíos con 0 para que el eje sea continuo
  const days = useMemo(() => fillMissingDays(sales.data ?? [], DAYS), [sales.data])
  const todayStats = days.find((d) => d.day === today) ?? { day: today, salesCount: 0, total: 0, averageTicket: 0 }
  const weekTotal = days.reduce((sum, d) => sum + d.total, 0)

  return (
    <>
      <PageHeader title="Dashboard" description="Resumen de ventas e inventario." />

      {sales.error && <div className="mb-4"><Alert onRetry={sales.reload}>{sales.error}</Alert></div>}

      {/* KPIs: un número grande vale más que un gráfico cuando la pregunta es "¿cuánto hoy?" */}
      <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Ventas de hoy" value={formatInteger(todayStats.salesCount)} loading={sales.loading && !sales.data} />
        <StatCard label="Total vendido hoy" value={formatMoney(todayStats.total)} loading={sales.loading && !sales.data} />
        <StatCard label="Ticket promedio hoy" value={formatMoney(todayStats.averageTicket)} loading={sales.loading && !sales.data} />
        <StatCard label={`Total últimos ${DAYS} días`} value={formatMoney(weekTotal)} loading={sales.loading && !sales.data} />
      </div>

      <div className="grid gap-6 xl:grid-cols-[3fr_2fr]">
        {sales.loading && !sales.data ? (
          <div className="rounded-xl border border-slate-200 bg-white p-5"><Spinner className="py-20" /></div>
        ) : (
          <SalesByDayChart data={days} today={today} />
        )}

        <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 px-5 py-4">
            <h2 className="text-sm font-semibold text-slate-700">Stock bajo</h2>
            <label className="flex items-center gap-2 text-xs text-slate-500">
              Menos de
              <select value={threshold} onChange={(e) => setThreshold(Number(e.target.value))} className="rounded-md border border-slate-300 px-2 py-1 text-xs text-slate-700">
                {THRESHOLDS.map((t) => <option key={t} value={t}>{t}</option>)}
              </select>
              unidades
            </label>
          </div>

          {lowStock.error ? (
            <div className="p-5"><Alert onRetry={lowStock.reload}>{lowStock.error}</Alert></div>
          ) : !lowStock.data ? (
            <Spinner className="py-16" />
          ) : lowStock.data.length === 0 ? (
            <p className="px-5 py-12 text-center text-sm text-slate-500">✓ Ningún producto bajo {threshold} unidades.</p>
          ) : (
            <ul className={`divide-y divide-slate-100 ${lowStock.loading ? 'opacity-60' : ''}`}>
              {lowStock.data.map((item) => (
                <li key={`${item.productId}-${item.warehouseId}`} className="flex items-center justify-between gap-3 px-5 py-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{item.productName}</p>
                    <p className="text-xs text-slate-500">{item.warehouseName} · <span className="font-mono">{item.productCode}</span></p>
                  </div>
                  {/* Estado con icono + texto, no solo color */}
                  <span className={`tabular inline-flex shrink-0 items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-semibold ${item.quantity <= threshold / 2 ? 'bg-red-50 text-red-700' : 'bg-amber-50 text-amber-800'}`}>
                    <span aria-hidden>{item.quantity <= threshold / 2 ? '▲' : '●'}</span>
                    {formatInteger(item.quantity)} ud.
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </>
  )
}

function StatCard({ label, value, loading }: { label: string; value: string; loading: boolean }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <p className="text-sm text-slate-500">{label}</p>
      {loading ? (
        <div className="mt-2 h-8 w-24 animate-pulse rounded bg-slate-100" aria-hidden />
      ) : (
        <p className="tabular mt-1 text-2xl font-semibold tracking-tight">{value}</p>
      )}
    </div>
  )
}

function fillMissingDays(rows: SalesByDay[], count: number): SalesByDay[] {
  const byDay = new Map(rows.map((r) => [r.day, r]))
  return Array.from({ length: count }, (_, i) => {
    const day = localIsoDate(i - (count - 1))
    return byDay.get(day) ?? { day, salesCount: 0, total: 0, averageTicket: 0 }
  })
}
