import { Ban, Package, Pencil, Plus, RotateCcw } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { toastError } from '../../api/errors'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { DropdownMenu } from '../../components/ui/DropdownMenu'
import { EmptyState } from '../../components/ui/EmptyState'
import { Select } from '../../components/ui/Field'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { SegmentedControl } from '../../components/ui/SegmentedControl'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { useCategories } from '../../hooks/useLookups'
import { formatMoney } from '../../lib/format'
import { useAuth } from '../auth/AuthContext'
import { ProductFormDrawer } from './ProductFormDrawer'
import { productsApi, type Product } from './productsApi'

const PAGE_SIZE = 10

export function ProductsPage() {
  const { hasRole } = useAuth()
  const isAdmin = hasRole('Admin')

  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [status, setStatus] = useState<'active' | 'all'>('active')
  const [page, setPage] = useState(1)
  const [editing, setEditing] = useState<Product | null | undefined>(undefined)
  const [toggling, setToggling] = useState<Product | null>(null)
  const debounced = useDebounce(search.trim())

  const categories = useCategories()
  const list = useApiQuery(
    (signal) => productsApi.list({ page, pageSize: PAGE_SIZE, name: debounced || undefined, categoryId: categoryId ? Number(categoryId) : undefined, includeInactive: status === 'all' }, signal),
    [page, debounced, categoryId, status],
  )

  const resetPage = <T,>(setter: (v: T) => void) => (value: T) => { setter(value); setPage(1) }

  async function handleToggle() {
    if (!toggling) return
    try {
      if (toggling.isActive) {
        await productsApi.deactivate(toggling.id)
        toast.success(`${toggling.name} desactivado`)
      } else {
        await productsApi.update(toggling.id, { name: toggling.name, categoryId: toggling.categoryId, price: toggling.price, isActive: true })
        toast.success(`${toggling.name} reactivado`)
      }
      list.reload()
    } catch (err) {
      toastError(err)
      throw err
    }
  }

  const columns: Column<Product>[] = [
    {
      key: 'product', header: 'Producto',
      cell: (p) => (
        <span className="flex items-center gap-3">
          <span className="flex size-9 shrink-0 items-center justify-center rounded-lg border border-border bg-surface-muted text-fg-muted"><Package className="size-4" /></span>
          <span className="min-w-0">
            <span className="block truncate font-medium">{p.name}</span>
            <span className="block font-mono text-xs text-fg-muted">{p.code}</span>
          </span>
        </span>
      ),
    },
    { key: 'category', header: 'Categoría', cell: (p) => <Badge>{p.categoryName}</Badge>, hideBelow: 'md' },
    { key: 'price', header: 'Precio', align: 'right', cell: (p) => <span className="tabular font-medium">{formatMoney(p.price)}</span> },
    { key: 'status', header: 'Estado', cell: (p) => <Badge tone={p.isActive ? 'success' : 'neutral'} dot>{p.isActive ? 'Activo' : 'Inactivo'}</Badge>, hideBelow: 'sm' },
  ]
  if (isAdmin) {
    columns.push({
      key: 'actions', header: <span className="sr-only">Acciones</span>, align: 'right', className: 'w-12',
      cell: (p) => (
        <DropdownMenu items={[
          { label: 'Editar', icon: <Pencil />, onSelect: () => setEditing(p) },
          p.isActive
            ? { label: 'Desactivar', icon: <Ban />, tone: 'danger', onSelect: () => setToggling(p) }
            : { label: 'Reactivar', icon: <RotateCcw />, onSelect: () => setToggling(p) },
        ]} />
      ),
    })
  }

  return (
    <>
      <PageHeader
        title="Productos"
        description={isAdmin ? 'Administra el catálogo y los precios (sin IVA).' : 'Catálogo de productos y precios (sin IVA).'}
        actions={isAdmin && <Button icon={<Plus className="size-4" />} onClick={() => setEditing(null)}>Nuevo producto</Button>}
      />

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <SearchInput value={search} onChange={resetPage(setSearch)} placeholder="Buscar por nombre o código…" className="w-full sm:w-72" />
        <div className="w-full sm:w-48">
          <Select value={categoryId} onChange={(e) => resetPage(setCategoryId)(e.target.value)} aria-label="Filtrar por categoría">
            <option value="">Todas las categorías</option>
            {categories.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </Select>
        </div>
        {isAdmin && (
          <SegmentedControl label="Estado" value={status} onChange={resetPage(setStatus)} options={[{ value: 'active', label: 'Activos' }, { value: 'all', label: 'Todos' }]} />
        )}
      </div>

      <DataTable
        columns={columns}
        rows={list.data?.items}
        rowKey={(p) => p.id}
        loading={list.loading}
        error={list.error}
        onRetry={list.reload}
        onRowClick={isAdmin ? (p) => setEditing(p) : undefined}
        empty={
          <EmptyState
            icon={<Package />}
            title={debounced || categoryId ? 'Sin resultados' : 'Aún no hay productos'}
            description={debounced ? `Ningún producto coincide con “${debounced}”.` : categoryId ? 'No hay productos en esta categoría.' : 'Agrega tu primer producto al catálogo.'}
            action={isAdmin && !debounced && !categoryId && <Button icon={<Plus className="size-4" />} onClick={() => setEditing(null)}>Nuevo producto</Button>}
          />
        }
        footer={list.data && list.data.totalCount > 0 && <Pagination page={list.data.page} pageSize={PAGE_SIZE} totalPages={list.data.totalPages} totalCount={list.data.totalCount} onPageChange={setPage} />}
      />

      {editing !== undefined && (
        <ProductFormDrawer product={editing} categories={categories.data ?? []} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); list.reload() }} />
      )}

      <ConfirmDialog
        open={toggling !== null}
        onClose={() => setToggling(null)}
        onConfirm={handleToggle}
        tone={toggling?.isActive ? 'danger' : 'primary'}
        title={toggling?.isActive ? 'Desactivar producto' : 'Reactivar producto'}
        confirmLabel={toggling?.isActive ? 'Desactivar' : 'Reactivar'}
        description={toggling?.isActive
          ? <><strong className="text-fg">{toggling?.name}</strong> dejará de aparecer en el punto de venta. Su historial de ventas se conserva y puedes reactivarlo cuando quieras.</>
          : <><strong className="text-fg">{toggling?.name}</strong> volverá a estar disponible para la venta.</>}
      />
    </>
  )
}
