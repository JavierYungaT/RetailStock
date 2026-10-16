import { CloudUpload, Printer, Receipt, ShoppingCart } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { toast } from 'sonner'
import { toastError } from '../../api/errors'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { Dialog } from '../../components/ui/Dialog'
import { Drawer } from '../../components/ui/Drawer'
import { EmptyState } from '../../components/ui/EmptyState'
import { Input, Select } from '../../components/ui/Field'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { SegmentedControl } from '../../components/ui/SegmentedControl'
import { Skeleton } from '../../components/ui/Skeleton'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { useWarehouses } from '../../hooks/useLookups'
import { formatDateTime, formatInteger, formatMoney, localIsoDate } from '../../lib/format'
import { useAuth } from '../auth/AuthContext'
import { SaleReceipt } from './ReceiptDialog'
import { salesApi, type SaleSummary, type SapSyncResult } from './salesApi'

const PAGE_SIZE = 15
type Preset = 'today' | '7d' | '30d' | 'custom'
const presetRange: Record<Exclude<Preset, 'custom'>, () => { from: string; to: string }> = {
  today: () => ({ from: localIsoDate(), to: localIsoDate() }),
  '7d': () => ({ from: localIsoDate(-6), to: localIsoDate() }),
  '30d': () => ({ from: localIsoDate(-29), to: localIsoDate() }),
}

export function SalesHistoryPage() {
  const { hasRole } = useAuth()
  const isAdmin = hasRole('Admin')
  const [preset, setPreset] = useState<Preset>('30d')
  const [range, setRange] = useState(presetRange['30d'])
  const [warehouseId, setWarehouseId] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [syncing, setSyncing] = useState(false)
  const [syncResult, setSyncResult] = useState<SapSyncResult | null>(null)
  const debounced = useDebounce(search.trim())
  const warehouses = useWarehouses()

  // Las fechas son días locales del negocio: la API las convierte a UTC (Business:UtcOffsetMinutes)
  const list = useApiQuery(
    (signal) => salesApi.list({ page, pageSize: PAGE_SIZE, from: range.from, to: range.to, name: debounced || undefined, warehouseId: warehouseId ? Number(warehouseId) : undefined }, signal),
    [page, range.from, range.to, debounced, warehouseId],
  )

  function choosePreset(value: Preset) {
    setPreset(value)
    if (value !== 'custom') setRange(presetRange[value]())
    setPage(1)
  }

  async function syncWithSap() {
    setSyncing(true)
    try {
      const result = await salesApi.syncToSap()
      if (result.processed === 0) toast.info('No hay ventas pendientes de sincronizar')
      else if (result.failed === 0) toast.success(`${result.succeeded} ventas enviadas a SAP`)
      else setSyncResult(result)
      list.reload()
    } catch (err) {
      toastError(err, 'No se pudo conectar con SAP.')
    } finally {
      setSyncing(false)
    }
  }

  const columns: Column<SaleSummary>[] = [
    { key: 'number', header: 'Factura', cell: (s) => <span className="font-mono text-[13px] font-medium">{s.number}</span> },
    { key: 'date', header: 'Fecha', cell: (s) => <span className="whitespace-nowrap text-fg-muted">{formatDateTime(s.date)}</span>, hideBelow: 'sm' },
    { key: 'customer', header: 'Cliente', cell: (s) => <span className="font-medium">{s.customerName}</span> },
    { key: 'warehouse', header: 'Bodega', cell: (s) => <span className="text-fg-muted">{s.warehouseName}</span>, hideBelow: 'lg' },
    { key: 'items', header: 'Ítems', align: 'right', cell: (s) => <span className="tabular text-fg-muted">{s.itemsCount}</span>, hideBelow: 'md' },
    { key: 'total', header: 'Total', align: 'right', cell: (s) => <span className="tabular font-semibold">{formatMoney(s.total)}</span> },
    { key: 'sap', header: 'SAP', cell: (s) => s.syncedToSap ? <Badge tone="success" dot>Sincronizada</Badge> : <Badge tone="warning" dot>Pendiente</Badge>, hideBelow: 'sm' },
  ]

  const pageTotal = list.data?.items.reduce((sum, s) => sum + s.total, 0) ?? 0

  return (
    <>
      <PageHeader
        title="Historial de ventas"
        description="Facturas emitidas y su estado en SAP Business One."
        actions={
          <>
            {isAdmin && <Button variant="secondary" icon={<CloudUpload className="size-4" />} loading={syncing} onClick={syncWithSap}>Sincronizar con SAP</Button>}
            <Link to="/sales/new"><Button icon={<ShoppingCart className="size-4" />}>Nueva venta</Button></Link>
          </>
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <SegmentedControl label="Período" value={preset} onChange={choosePreset} options={[{ value: 'today', label: 'Hoy' }, { value: '7d', label: '7 días' }, { value: '30d', label: '30 días' }, { value: 'custom', label: 'Personalizado' }]} />
        {preset === 'custom' && (
          <div className="flex items-center gap-2">
            <Input type="date" aria-label="Desde" value={range.from} max={range.to} onChange={(e) => { setRange({ ...range, from: e.target.value }); setPage(1) }} className="w-40" />
            <span className="text-fg-subtle">–</span>
            <Input type="date" aria-label="Hasta" value={range.to} min={range.from} onChange={(e) => { setRange({ ...range, to: e.target.value }); setPage(1) }} className="w-40" />
          </div>
        )}
        <SearchInput value={search} onChange={(v) => { setSearch(v); setPage(1) }} placeholder="Buscar cliente…" className="w-full sm:w-56" />
        <div className="w-full sm:w-48">
          <Select value={warehouseId} onChange={(e) => { setWarehouseId(e.target.value); setPage(1) }} aria-label="Filtrar por bodega">
            <option value="">Todas las bodegas</option>
            {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
          </Select>
        </div>
      </div>

      <DataTable
        columns={columns}
        rows={list.data?.items}
        rowKey={(s) => s.id}
        loading={list.loading}
        error={list.error}
        onRetry={list.reload}
        onRowClick={(s) => setSelectedId(s.id)}
        empty={<EmptyState icon={<Receipt />} title="Sin ventas en el período" description="Cambia el rango de fechas o registra una venta." action={<Link to="/sales/new"><Button icon={<ShoppingCart className="size-4" />}>Nueva venta</Button></Link>} />}
        footer={list.data && list.data.totalCount > 0 && (
          <>
            <div className="flex justify-end gap-2 border-t border-border bg-surface-muted/40 px-4 py-2.5 text-[13px]">
              <span className="text-fg-muted">Total de esta página:</span><span className="tabular font-semibold">{formatMoney(pageTotal)}</span>
            </div>
            <Pagination page={list.data.page} pageSize={PAGE_SIZE} totalPages={list.data.totalPages} totalCount={list.data.totalCount} onPageChange={setPage} />
          </>
        )}
      />

      {selectedId !== null && <SaleDetailDrawer saleId={selectedId} onClose={() => setSelectedId(null)} />}

      {syncResult && (
        <Dialog
          open
          onClose={() => setSyncResult(null)}
          title="Resultado de la sincronización"
          description={`${syncResult.succeeded} de ${syncResult.processed} ventas enviadas a SAP.`}
          footer={<Button onClick={() => setSyncResult(null)}>Entendido</Button>}
        >
          <ul className="divide-y divide-border rounded-xl border border-border text-sm">
            {syncResult.items.map((item) => (
              <li key={item.saleId} className="flex items-start justify-between gap-3 px-4 py-3">
                <span className="min-w-0">
                  <span className="font-mono font-medium">{item.saleNumber}</span>
                  {item.error && <span className="mt-0.5 block text-xs text-red-600 dark:text-red-400">{item.error}</span>}
                </span>
                {item.success ? <Badge tone="success" dot>DocEntry {item.sapDocEntry}</Badge> : <Badge tone="danger" dot>Falló</Badge>}
              </li>
            ))}
          </ul>
          <p className="mt-3 text-xs text-fg-muted">Las ventas fallidas quedan pendientes y se reintentan en la próxima sincronización.</p>
        </Dialog>
      )}
    </>
  )
}

function SaleDetailDrawer({ saleId, onClose }: { saleId: number; onClose: () => void }) {
  const sale = useApiQuery((signal) => salesApi.get(saleId, signal), [saleId])
  const data = sale.data

  return (
    <Drawer
      open
      onClose={onClose}
      title={data ? <span className="font-mono">{data.number}</span> : 'Venta'}
      description={data ? formatDateTime(data.date) : undefined}
      footer={data && <><Button variant="secondary" icon={<Printer className="size-4" />} onClick={() => window.print()}>Imprimir</Button><Button onClick={onClose}>Cerrar</Button></>}
    >
      {sale.error && <p className="text-sm text-red-600">{sale.error}</p>}
      {!data && sale.loading && <div className="space-y-3"><Skeleton className="h-20" /><Skeleton className="h-40" /><Skeleton className="h-24" /></div>}
      {data && (
        <div className="space-y-5">
          <div className="flex flex-wrap gap-2">
            {data.syncedToSap
              ? <Badge tone="success" dot>Sincronizada con SAP · DocEntry {data.sapDocEntry}</Badge>
              : <Badge tone="warning" dot>Pendiente de enviar a SAP</Badge>}
            <Badge>{formatInteger(data.details.reduce((s, d) => s + d.quantity, 0))} unidades</Badge>
          </div>
          <SaleReceipt sale={data} />
        </div>
      )}
    </Drawer>
  )
}
