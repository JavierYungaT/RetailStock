import { ArrowRight, DollarSign, PackagePlus, Receipt, ShoppingCart, TriangleAlert, Trophy } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { Alert } from '../../components/ui/Alert'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { SegmentedControl } from '../../components/ui/SegmentedControl'
import { Skeleton } from '../../components/ui/Skeleton'
import { StatCard } from '../../components/ui/StatCard'
import { useApiQuery } from '../../hooks/useApiQuery'
import { formatInteger, formatLongDate, formatMoney, formatRelative, greeting, localIsoDate } from '../../lib/format'
import { useAuth } from '../auth/AuthContext'
import { LOW_STOCK_THRESHOLD } from '../inventory/stockStatus'
import { salesApi } from '../sales/salesApi'
import { dashboardApi, type SalesByDay } from './dashboardApi'
import { SalesChart } from './SalesChart'

type Range = 7 | 14 | 30

/** Panel del administrador: KPIs del día, tendencia, ranking, últimas ventas y alertas de stock. */
export function DashboardPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [range, setRange] = useState<Range>(14)
  const [showTable, setShowTable] = useState(false)
  const [now] = useState(() => new Date()) // fecha fija durante la visita (render puro)
  const today = localIsoDate()
  const from = localIsoDate(-(range - 1))

  const sales = useApiQuery((signal) => dashboardApi.salesByDay(localIsoDate(-30), today, signal), [today])
  const top = useApiQuery((signal) => dashboardApi.topProducts(from, today, 5, signal), [from, today])
  const lowStock = useApiQuery((signal) => dashboardApi.lowStock(LOW_STOCK_THRESHOLD, signal), [])
  const recent = useApiQuery((signal) => salesApi.list({ page: 1, pageSize: 6, from: localIsoDate(-30), to: today }, signal), [today])

  // El procedimiento solo devuelve días CON ventas: se completan los vacíos con 0
  const allDays = useMemo(() => fillDays(sales.data ?? [], 31), [sales.data])
  const days = allDays.slice(-range)
  const todayStats = allDays[allDays.length - 1]
  const yesterday = allDays[allDays.length - 2]
  const rangeTotal = days.reduce((s, d) => s + d.total, 0)
  const loadingKpis = sales.loading && !sales.data
  const userName = user?.email.split('@')[0] ?? ''

  return (
    <>
      <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-sm text-fg-muted first-letter:uppercase">{formatLongDate(now)}</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">{greeting(now)}, <span className="capitalize">{userName}</span></h1>
        </div>
        <div className="flex gap-2">
          <Link to="/sales"><Button variant="secondary" icon={<Receipt className="size-4" />}>Ver ventas</Button></Link>
          <Link to="/sales/new"><Button icon={<ShoppingCart className="size-4" />}>Nueva venta</Button></Link>
        </div>
      </div>

      {sales.error && <Alert className="mb-4">{sales.error}</Alert>}

      <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Ingresos de hoy" icon={<DollarSign />} loading={loadingKpis} value={formatMoney(todayStats.total)} delta={percentChange(todayStats.total, yesterday.total)} deltaLabel="vs. ayer" />
        <StatCard label="Ventas de hoy" icon={<Receipt />} loading={loadingKpis} value={formatInteger(todayStats.salesCount)} delta={percentChange(todayStats.salesCount, yesterday.salesCount)} deltaLabel="vs. ayer" />
        <StatCard label="Ticket promedio" icon={<ShoppingCart />} loading={loadingKpis} value={formatMoney(todayStats.averageTicket)} footer={<span>Hoy · con IVA</span>} />
        <StatCard label="Stock bajo" icon={<TriangleAlert />} tone="warning" loading={lowStock.loading && !lowStock.data} value={formatInteger(lowStock.data?.length ?? 0)} footer={<span>productos con menos de {LOW_STOCK_THRESHOLD} ud.</span>} />
      </div>

      <div className="mb-6 grid gap-6 xl:grid-cols-[2fr_1fr]">
        <Card>
          <CardHeader
            title="Ingresos por día"
            description={<>Últimos {range} días · <span className="tabular font-medium text-fg">{formatMoney(rangeTotal)}</span> con IVA</>}
            actions={
              <>
                <button type="button" onClick={() => setShowTable((v) => !v)} className="text-xs font-medium text-primary hover:underline">{showTable ? 'Ver gráfico' : 'Ver tabla'}</button>
                <SegmentedControl label="Rango" value={range} onChange={setRange} options={[{ value: 7, label: '7d' }, { value: 14, label: '14d' }, { value: 30, label: '30d' }]} />
              </>
            }
          />
          <div className="px-5 pt-4 pb-3">
            {loadingKpis ? <Skeleton className="h-[240px]" /> : <SalesChart data={days} today={today} showTable={showTable} />}
          </div>
        </Card>

        <Card>
          <CardHeader title="Más vendidos" description={`Por unidades · últimos ${range} días`} />
          <div className="p-5">
            {top.loading && !top.data ? (
              <div className="space-y-4">{Array.from({ length: 5 }, (_, i) => <Skeleton key={i} className="h-9" />)}</div>
            ) : top.data && top.data.length > 0 ? (
              <ol className="space-y-4">
                {top.data.map((p, i) => {
                  const pct = (p.quantitySold / top.data![0].quantitySold) * 100
                  return (
                    <li key={p.productId}>
                      <div className="mb-1.5 flex items-baseline justify-between gap-3 text-sm">
                        <span className="flex min-w-0 items-center gap-2">
                          <span className="tabular w-4 text-xs font-semibold text-fg-subtle">{i + 1}</span>
                          <span className="truncate font-medium">{p.productName}</span>
                        </span>
                        <span className="tabular shrink-0 text-xs text-fg-muted">{formatInteger(p.quantitySold)} ud. · {formatMoney(p.revenue)}</span>
                      </div>
                      <div className="ml-6 h-1.5 overflow-hidden rounded-full bg-surface-muted">
                        <div className="h-full rounded-full bg-chart-1" style={{ width: `${pct}%` }} />
                      </div>
                    </li>
                  )
                })}
              </ol>
            ) : (
              <div className="py-10 text-center text-sm text-fg-muted"><Trophy className="mx-auto mb-2 size-6 text-fg-subtle" />Aún no hay ventas en este período.</div>
            )}
          </div>
        </Card>
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader title="Últimas ventas" actions={<Link to="/sales" className="flex items-center gap-1 text-xs font-medium text-primary hover:underline">Ver todas <ArrowRight className="size-3" /></Link>} />
          {recent.loading && !recent.data ? (
            <div className="space-y-3 p-5">{Array.from({ length: 4 }, (_, i) => <Skeleton key={i} className="h-10" />)}</div>
          ) : recent.data && recent.data.items.length > 0 ? (
            <ul className="divide-y divide-border">
              {recent.data.items.map((s) => (
                <li key={s.id} className="flex items-center justify-between gap-3 px-5 py-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{s.customerName}</p>
                    <p className="text-xs text-fg-muted"><span className="font-mono">{s.number}</span> · {formatRelative(s.date)} · {s.warehouseName}</p>
                  </div>
                  <div className="text-right">
                    <p className="tabular text-sm font-semibold">{formatMoney(s.total)}</p>
                    {!s.syncedToSap && <p className="text-[11px] text-amber-600 dark:text-amber-400">Pendiente SAP</p>}
                  </div>
                </li>
              ))}
            </ul>
          ) : (
            <p className="px-5 py-10 text-center text-sm text-fg-muted">Sin ventas recientes.</p>
          )}
        </Card>

        <Card>
          <CardHeader title="Requieren reposición" description={`Productos activos con menos de ${LOW_STOCK_THRESHOLD} unidades`} />
          {lowStock.loading && !lowStock.data ? (
            <div className="space-y-3 p-5">{Array.from({ length: 4 }, (_, i) => <Skeleton key={i} className="h-10" />)}</div>
          ) : lowStock.data && lowStock.data.length > 0 ? (
            <ul className="divide-y divide-border">
              {lowStock.data.map((item) => (
                <li key={`${item.productId}-${item.warehouseId}`} className="flex items-center justify-between gap-3 px-5 py-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{item.productName}</p>
                    <p className="text-xs text-fg-muted">{item.warehouseName} · <span className="font-mono">{item.productCode}</span></p>
                  </div>
                  <div className="flex shrink-0 items-center gap-3">
                    <Badge tone={item.quantity === 0 ? 'danger' : 'warning'} dot>{formatInteger(item.quantity)} ud.</Badge>
                    <Button
                      size="sm"
                      variant="soft"
                      icon={<PackagePlus className="size-3.5" />}
                      onClick={() => navigate('/inventory/movements', { state: { newMovement: { type: 'In', product: { id: item.productId, code: item.productCode, name: item.productName }, warehouseId: item.warehouseId } } })}
                    >
                      Reponer
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          ) : (
            <p className="px-5 py-10 text-center text-sm text-fg-muted">✓ Todo el inventario está sobre el mínimo.</p>
          )}
        </Card>
      </div>
    </>
  )
}

function fillDays(rows: SalesByDay[], count: number): SalesByDay[] {
  const byDay = new Map(rows.map((r) => [r.day, r]))
  return Array.from({ length: count }, (_, i) => {
    const day = localIsoDate(i - (count - 1))
    return byDay.get(day) ?? { day, salesCount: 0, total: 0, averageTicket: 0 }
  })
}

/** Variación porcentual; null si no hay base de comparación (ayer = 0). */
function percentChange(current: number, previous: number) {
  if (previous === 0) return null
  return ((current - previous) / previous) * 100
}
