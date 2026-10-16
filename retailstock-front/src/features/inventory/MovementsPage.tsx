import { ArrowDownToLine, ArrowLeftRight, ArrowRight, ArrowUpFromLine, History, Plus } from 'lucide-react'
import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { Avatar } from '../../components/ui/Avatar'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { EmptyState } from '../../components/ui/EmptyState'
import { Select } from '../../components/ui/Field'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { SegmentedControl } from '../../components/ui/SegmentedControl'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { useWarehouses } from '../../hooks/useLookups'
import { cn } from '../../lib/cn'
import { formatDateTime, formatInteger, formatRelative } from '../../lib/format'
import { inventoryApi, type Movement, type MovementType } from './inventoryApi'
import { MovementDrawer, type MovementDefaults } from './MovementDrawer'

const PAGE_SIZE = 15

const typeMeta: Record<MovementType, { label: string; tone: 'success' | 'danger' | 'info'; icon: typeof ArrowDownToLine; sign: string }> = {
  In: { label: 'Ingreso', tone: 'success', icon: ArrowDownToLine, sign: '+' },
  Out: { label: 'Egreso', tone: 'danger', icon: ArrowUpFromLine, sign: '−' },
  Transfer: { label: 'Transferencia', tone: 'info', icon: ArrowLeftRight, sign: '' },
}

/** Kardex: historial inmutable de movimientos (no se editan ni borran: se corrigen con otro movimiento). */
export function MovementsPage() {
  const location = useLocation()
  const navigate = useNavigate()
  // Otra pantalla (dashboard, existencias) puede pedir abrir el formulario prellenado vía location.state
  const requested = (location.state as { newMovement?: MovementDefaults } | null)?.newMovement
  const [movement, setMovement] = useState<MovementDefaults | null>(requested ?? null)
  const [type, setType] = useState<MovementType | 'all'>('all')
  const [warehouseId, setWarehouseId] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const debounced = useDebounce(search.trim())
  const warehouses = useWarehouses()

  const list = useApiQuery(
    (signal) => inventoryApi.movements({ page, pageSize: PAGE_SIZE, name: debounced || undefined, type: type === 'all' ? undefined : type, warehouseId: warehouseId ? Number(warehouseId) : undefined }, signal),
    [page, debounced, type, warehouseId],
  )

  function closeDrawer() {
    setMovement(null)
    if (requested) navigate(location.pathname, { replace: true, state: null }) // no reabrir al recargar
  }

  const columns: Column<Movement>[] = [
    {
      key: 'date', header: 'Fecha',
      cell: (m) => (
        <span title={formatDateTime(m.date)}>
          <span className="block whitespace-nowrap">{formatDateTime(m.date)}</span>
          <span className="block text-xs text-fg-muted">{formatRelative(m.date)}</span>
        </span>
      ),
    },
    {
      key: 'type', header: 'Tipo',
      cell: (m) => { const t = typeMeta[m.type]; return <Badge tone={t.tone} icon={<t.icon className="size-3" />}>{t.label}</Badge> },
    },
    {
      key: 'product', header: 'Producto',
      cell: (m) => <span><span className="block font-medium">{m.productName}</span><span className="block font-mono text-xs text-fg-muted">{m.productCode}</span></span>,
    },
    {
      key: 'route', header: 'Bodega', hideBelow: 'md',
      cell: (m) => m.type === 'Transfer'
        ? <span className="flex items-center gap-1.5 whitespace-nowrap">{m.fromWarehouseName}<ArrowRight className="size-3.5 text-fg-subtle" />{m.toWarehouseName}</span>
        : <span>{m.toWarehouseName ?? m.fromWarehouseName}</span>,
    },
    {
      key: 'quantity', header: 'Cantidad', align: 'right',
      cell: (m) => (
        <span className={cn('tabular font-semibold', m.type === 'In' && 'text-emerald-600 dark:text-emerald-400', m.type === 'Out' && 'text-red-600 dark:text-red-400')}>
          {typeMeta[m.type].sign}{formatInteger(m.quantity)}
        </span>
      ),
    },
    { key: 'user', header: 'Usuario', hideBelow: 'lg', cell: (m) => <span className="flex items-center gap-2"><Avatar name={m.userEmail} className="size-6 text-[10px]" /><span className="text-fg-muted">{m.userEmail}</span></span> },
  ]

  return (
    <>
      <PageHeader
        title="Movimientos"
        description="Kardex de ingresos, egresos y transferencias."
        actions={<Button icon={<Plus className="size-4" />} onClick={() => setMovement({})}>Nuevo movimiento</Button>}
      />

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <SegmentedControl
          label="Tipo"
          value={type}
          onChange={(v) => { setType(v); setPage(1) }}
          options={[{ value: 'all', label: 'Todos' }, { value: 'In', label: 'Ingresos' }, { value: 'Out', label: 'Egresos' }, { value: 'Transfer', label: 'Transferencias' }]}
        />
        <SearchInput value={search} onChange={(v) => { setSearch(v); setPage(1) }} placeholder="Buscar producto…" className="w-full sm:w-64" />
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
        rowKey={(m) => m.id}
        loading={list.loading}
        error={list.error}
        onRetry={list.reload}
        empty={<EmptyState icon={<History />} title="Sin movimientos" description="Los ingresos, egresos, transferencias y ventas aparecerán aquí." action={<Button icon={<Plus className="size-4" />} onClick={() => setMovement({})}>Registrar el primero</Button>} />}
        footer={list.data && list.data.totalCount > 0 && <Pagination page={list.data.page} pageSize={PAGE_SIZE} totalPages={list.data.totalPages} totalCount={list.data.totalCount} onPageChange={setPage} />}
      />

      {movement && <MovementDrawer defaults={movement} onClose={closeDrawer} onSaved={() => { closeDrawer(); list.reload() }} />}
    </>
  )
}
