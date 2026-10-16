import { MapPin, Pencil, Plus, Trash2, Warehouse as WarehouseIcon } from 'lucide-react'
import { omit } from '../../lib/omit'
import { useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { getErrorMessage, getFieldErrors, toastError } from '../../api/errors'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { ConfirmDialog } from '../../components/ui/ConfirmDialog'
import { DataTable, type Column } from '../../components/ui/DataTable'
import { Dialog } from '../../components/ui/Dialog'
import { DropdownMenu } from '../../components/ui/DropdownMenu'
import { EmptyState } from '../../components/ui/EmptyState'
import { Field, Input } from '../../components/ui/Field'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { warehousesApi, type Warehouse } from './warehousesApi'

const PAGE_SIZE = 10

/** CRUD de bodegas: eliminar solo si no tiene stock, movimientos ni ventas (si no, 409). */
export function WarehousesPage() {
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [editing, setEditing] = useState<Warehouse | null | undefined>(undefined)
  const [deleting, setDeleting] = useState<Warehouse | null>(null)
  const debounced = useDebounce(search.trim())

  const list = useApiQuery((signal) => warehousesApi.list({ page, pageSize: PAGE_SIZE, name: debounced || undefined }, signal), [page, debounced])

  const columns: Column<Warehouse>[] = [
    {
      key: 'name', header: 'Bodega',
      cell: (w) => (
        <span className="flex items-center gap-3">
          <span className="rounded-lg bg-primary-soft p-2 text-primary"><WarehouseIcon className="size-4" /></span>
          <span className="font-medium">{w.name}</span>
        </span>
      ),
    },
    { key: 'location', header: 'Ubicación', cell: (w) => <span className="flex items-center gap-1.5 text-fg-muted"><MapPin className="size-3.5 shrink-0" />{w.location}</span>, hideBelow: 'sm' },
    { key: 'code', header: 'Código SAP', cell: (w) => <span className="font-mono text-xs text-fg-muted">{String(w.id).padStart(2, '0')}</span>, hideBelow: 'md' },
    {
      key: 'actions', header: <span className="sr-only">Acciones</span>, align: 'right', className: 'w-12',
      cell: (w) => (
        <DropdownMenu items={[
          { label: 'Editar', icon: <Pencil />, onSelect: () => setEditing(w) },
          { label: 'Eliminar', icon: <Trash2 />, tone: 'danger', onSelect: () => setDeleting(w) },
        ]} />
      ),
    },
  ]

  async function handleDelete() {
    if (!deleting) return
    try {
      await warehousesApi.remove(deleting.id)
      toast.success(`Bodega “${deleting.name}” eliminada`)
      list.reload()
    } catch (err) {
      toastError(err)
      throw err
    }
  }

  return (
    <>
      <PageHeader
        title="Bodegas"
        description="Sucursales y centros de almacenamiento."
        actions={<Button icon={<Plus className="size-4" />} onClick={() => setEditing(null)}>Nueva bodega</Button>}
      />

      <div className="mb-4"><SearchInput value={search} onChange={(v) => { setSearch(v); setPage(1) }} placeholder="Buscar bodega…" className="sm:max-w-xs" /></div>

      <DataTable
        columns={columns}
        rows={list.data?.items}
        rowKey={(w) => w.id}
        loading={list.loading}
        error={list.error}
        onRetry={list.reload}
        onRowClick={(w) => setEditing(w)}
        empty={<EmptyState icon={<WarehouseIcon />} title={debounced ? 'Sin resultados' : 'Aún no hay bodegas'} description={debounced ? `Ninguna bodega coincide con “${debounced}”.` : 'Registra tu primera bodega o sucursal.'} />}
        footer={list.data && list.data.totalCount > 0 && <Pagination page={list.data.page} pageSize={PAGE_SIZE} totalPages={list.data.totalPages} totalCount={list.data.totalCount} onPageChange={setPage} />}
      />

      {editing !== undefined && <WarehouseDialog warehouse={editing} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); list.reload() }} />}

      <ConfirmDialog
        open={deleting !== null}
        onClose={() => setDeleting(null)}
        onConfirm={handleDelete}
        title="Eliminar bodega"
        confirmLabel="Eliminar"
        description={<>¿Eliminar <strong className="text-fg">{deleting?.name}</strong>? Solo es posible si no tiene stock, movimientos ni ventas.</>}
      />
    </>
  )
}

function WarehouseDialog({ warehouse, onClose, onSaved }: { warehouse: Warehouse | null; onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState({ name: warehouse?.name ?? '', location: warehouse?.location ?? '' })
  const [saving, setSaving] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true); setError(null); setFieldErrors({})
    try {
      const saved = warehouse ? await warehousesApi.update(warehouse.id, form) : await warehousesApi.create(form)
      toast.success(warehouse ? 'Bodega actualizada' : `Bodega “${saved.name}” creada`)
      onSaved()
    } catch (err) {
      const fields = getFieldErrors(err)
      setFieldErrors(fields)
      if (Object.keys(fields).length === 0) setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog
      open
      onClose={onClose}
      title={warehouse ? 'Editar bodega' : 'Nueva bodega'}
      footer={<><Button variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" form="warehouse-form" loading={saving}>{warehouse ? 'Guardar cambios' : 'Crear bodega'}</Button></>}
    >
      <form id="warehouse-form" onSubmit={handleSubmit} className="space-y-4" noValidate>
        {error && <Alert>{error}</Alert>}
        <Field label="Nombre" htmlFor="warehouse-name" error={fieldErrors.name} required>
          <Input id="warehouse-name" autoFocus value={form.name} onChange={(e) => { setForm({ ...form, name: e.target.value }); setFieldErrors((errors) => omit(errors, 'name')) }} invalid={!!fieldErrors.name} placeholder="Ej. Sucursal Sur" />
        </Field>
        <Field label="Ubicación" htmlFor="warehouse-location" error={fieldErrors.location} required>
          <Input id="warehouse-location" value={form.location} onChange={(e) => { setForm({ ...form, location: e.target.value }); setFieldErrors((errors) => omit(errors, 'location')) }} invalid={!!fieldErrors.location} leftIcon={<MapPin />} placeholder="Ciudad - dirección" />
        </Field>
      </form>
    </Dialog>
  )
}
