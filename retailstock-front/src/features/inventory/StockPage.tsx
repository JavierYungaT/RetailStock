import { ArrowDownToLine, ArrowLeftRight, ArrowUpFromLine, Boxes, Plus } from 'lucide-react'
import { useState } from 'react'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { DropdownMenu } from '../../components/ui/DropdownMenu'
import { EmptyState } from '../../components/ui/EmptyState'
import { Select } from '../../components/ui/Field'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { useCategories, useWarehouses } from '../../hooks/useLookups'
import { formatInteger, formatMoney } from '../../lib/format'
import { useAuth } from '../auth/AuthContext'
import { inventoryApi, type StockItem } from './inventoryApi'
import { MovementDrawer, type MovementDefaults } from './MovementDrawer'
import { StockBadge } from './stockStatus'

const PAGE_SIZE = 15

/** Existencias por producto y bodega. Bodega/Admin pueden registrar movimientos desde cada fila. */
export function StockPage() {
  const { hasRole } = useAuth()
  const canMove = hasRole('Admin', 'Warehouse')
  const [search, setSearch] = useState('')
  const [warehouseId, setWarehouseId] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [page, setPage] = useState(1)
  const [movement, setMovement] = useState<MovementDefaults | null>(null)
  const debounced = useDebounce(search.trim())
  const warehouses = useWarehouses()
  const categories = useCategories()

  const list = useApiQuery(
    (signal) => inventoryApi.stock({ page, pageSize: PAGE_SIZE, name: debounced || undefined, warehouseId: warehouseId ? Number(warehouseId) : undefined, categoryId: categoryId ? Number(categoryId) : undefined }, signal),
    [page, debounced, warehouseId, categoryId],
  )

  const open = (type: MovementDefaults['type'], s: StockItem) =>
    setMovement({ type, product: { id: s.productId, code: s.productCode, name: s.productName }, warehouseId: s.warehouseId })

  const columns: Column<StockItem>[] = [
    {
      key: 'product', header: 'Producto',
      cell: (s) => (
        <span className="min-w-0">
          <span className="flex items-center gap-2 font-medium">{s.productName}{!s.isActive && <Badge>Inactivo</Badge>}</span>
          <span className="block font-mono text-xs text-fg-muted">{s.productCode}</span>
        </span>
      ),
    },
    { key: 'category', header: 'Categoría', cell: (s) => <span className="text-fg-muted">{s.categoryName}</span>, hideBelow: 'lg' },
    { key: 'warehouse', header: 'Bodega', cell: (s) => s.warehouseName, hideBelow: 'sm' },
    { key: 'price', header: 'Precio', align: 'right', cell: (s) => <span className="tabular text-fg-muted">{formatMoney(s.price)}</span>, hideBelow: 'md' },
    { key: 'quantity', header: 'Unidades', align: 'right', cell: (s) => <span className="tabular text-base font-semibold">{formatInteger(s.quantity)}</span> },
    { key: 'status', header: 'Estado', cell: (s) => <StockBadge quantity={s.quantity} />, hideBelow: 'sm' },
  ]
  if (canMove) {
    columns.push({
      key: 'actions', header: <span className="sr-only">Acciones</span>, align: 'right', className: 'w-12',
      cell: (s) => (
        <DropdownMenu items={[
          { label: 'Registrar ingreso', icon: <ArrowDownToLine />, onSelect: () => open('In', s) },
          { label: 'Registrar egreso', icon: <ArrowUpFromLine />, onSelect: () => open('Out', s), disabled: s.quantity === 0 },
          { label: 'Transferir', icon: <ArrowLeftRight />, onSelect: () => open('Transfer', s), disabled: s.quantity === 0 },
        ]} />
      ),
    })
  }

  return (
    <>
      <PageHeader
        title="Existencias"
        description="Unidades disponibles por producto y bodega."
        actions={canMove && <Button icon={<Plus className="size-4" />} onClick={() => setMovement({})}>Nuevo movimiento</Button>}
      />

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <SearchInput value={search} onChange={(v) => { setSearch(v); setPage(1) }} placeholder="Buscar producto…" className="w-full sm:w-72" />
        <div className="w-full sm:w-48">
          <Select value={warehouseId} onChange={(e) => { setWarehouseId(e.target.value); setPage(1) }} aria-label="Filtrar por bodega">
            <option value="">Todas las bodegas</option>
            {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
          </Select>
        </div>
        <div className="w-full sm:w-48">
          <Select value={categoryId} onChange={(e) => { setCategoryId(e.target.value); setPage(1) }} aria-label="Filtrar por categoría">
            <option value="">Todas las categorías</option>
            {categories.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </Select>
        </div>
      </div>

      <DataTable
        columns={columns}
        rows={list.data?.items}
        rowKey={(s) => `${s.productId}-${s.warehouseId}`}
        loading={list.loading}
        error={list.error}
        onRetry={list.reload}
        empty={<EmptyState icon={<Boxes />} title="Sin existencias" description="No hay stock registrado con estos filtros." />}
        footer={list.data && list.data.totalCount > 0 && <Pagination page={list.data.page} pageSize={PAGE_SIZE} totalPages={list.data.totalPages} totalCount={list.data.totalCount} onPageChange={setPage} />}
      />

      {movement && <MovementDrawer defaults={movement} onClose={() => setMovement(null)} onSaved={() => { setMovement(null); list.reload() }} />}
    </>
  )
}
