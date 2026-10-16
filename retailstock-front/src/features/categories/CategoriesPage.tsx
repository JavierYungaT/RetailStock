import { Pencil, Plus, Tags, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { getErrorMessage, getFieldErrors, toastError } from '../../api/errors'
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
import { Alert } from '../../components/ui/Alert'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { categoriesApi, type Category } from './categoriesApi'

const PAGE_SIZE = 10

/** CRUD de categorías: crear y editar en diálogo, eliminar con confirmación (409 si tiene productos). */
export function CategoriesPage() {
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [editing, setEditing] = useState<Category | null | undefined>(undefined) // undefined = cerrado, null = nueva
  const [deleting, setDeleting] = useState<Category | null>(null)
  const debounced = useDebounce(search.trim())

  const list = useApiQuery((signal) => categoriesApi.list({ page, pageSize: PAGE_SIZE, name: debounced || undefined }, signal), [page, debounced])

  const columns: Column<Category>[] = [
    {
      key: 'name',
      header: 'Categoría',
      cell: (c) => (
        <span className="flex items-center gap-3">
          <span className="rounded-lg bg-primary-soft p-2 text-primary"><Tags className="size-4" /></span>
          <span className="font-medium">{c.name}</span>
        </span>
      ),
    },
    { key: 'id', header: 'ID', cell: (c) => <span className="font-mono text-xs text-fg-muted">#{c.id}</span>, hideBelow: 'sm' },
    {
      key: 'actions', header: <span className="sr-only">Acciones</span>, align: 'right', className: 'w-12',
      cell: (c) => (
        <DropdownMenu items={[
          { label: 'Editar', icon: <Pencil />, onSelect: () => setEditing(c) },
          { label: 'Eliminar', icon: <Trash2 />, tone: 'danger', onSelect: () => setDeleting(c) },
        ]} />
      ),
    },
  ]

  async function handleDelete() {
    if (!deleting) return
    try {
      await categoriesApi.remove(deleting.id)
      toast.success(`Categoría “${deleting.name}” eliminada`)
      list.reload()
    } catch (err) {
      toastError(err)
      throw err
    }
  }

  return (
    <>
      <PageHeader
        title="Categorías"
        description="Agrupa los productos del catálogo."
        actions={<Button icon={<Plus className="size-4" />} onClick={() => setEditing(null)}>Nueva categoría</Button>}
      />

      <div className="mb-4"><SearchInput value={search} onChange={(v) => { setSearch(v); setPage(1) }} placeholder="Buscar categoría…" className="sm:max-w-xs" /></div>

      <DataTable
        columns={columns}
        rows={list.data?.items}
        rowKey={(c) => c.id}
        loading={list.loading}
        error={list.error}
        onRetry={list.reload}
        onRowClick={(c) => setEditing(c)}
        empty={<EmptyState icon={<Tags />} title={debounced ? 'Sin resultados' : 'Aún no hay categorías'} description={debounced ? `Ninguna categoría coincide con “${debounced}”.` : 'Crea la primera para organizar tus productos.'} action={!debounced && <Button icon={<Plus className="size-4" />} onClick={() => setEditing(null)}>Nueva categoría</Button>} />}
        footer={list.data && list.data.totalCount > 0 && <Pagination page={list.data.page} pageSize={PAGE_SIZE} totalPages={list.data.totalPages} totalCount={list.data.totalCount} onPageChange={setPage} />}
      />

      {editing !== undefined && (
        <CategoryDialog category={editing} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); list.reload() }} />
      )}

      <ConfirmDialog
        open={deleting !== null}
        onClose={() => setDeleting(null)}
        onConfirm={handleDelete}
        title="Eliminar categoría"
        confirmLabel="Eliminar"
        description={<>¿Eliminar <strong className="text-fg">{deleting?.name}</strong>? Solo es posible si no tiene productos asociados.</>}
      />
    </>
  )
}

function CategoryDialog({ category, onClose, onSaved }: { category: Category | null; onClose: () => void; onSaved: () => void }) {
  const [name, setName] = useState(category?.name ?? '')
  const [saving, setSaving] = useState(false)
  const [fieldError, setFieldError] = useState<string>()
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true); setError(null); setFieldError(undefined)
    try {
      const saved = category ? await categoriesApi.update(category.id, name) : await categoriesApi.create(name)
      toast.success(category ? 'Categoría actualizada' : `Categoría “${saved.name}” creada`)
      onSaved()
    } catch (err) {
      const fields = getFieldErrors(err)
      if (fields.name) setFieldError(fields.name); else setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog
      open
      onClose={onClose}
      title={category ? 'Editar categoría' : 'Nueva categoría'}
      description={category ? undefined : 'El nombre debe ser único.'}
      footer={<><Button variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" form="category-form" loading={saving}>{category ? 'Guardar cambios' : 'Crear categoría'}</Button></>}
    >
      <form id="category-form" onSubmit={handleSubmit} className="space-y-4" noValidate>
        {error && <Alert>{error}</Alert>}
        <Field label="Nombre" htmlFor="category-name" error={fieldError} required>
          <Input id="category-name" autoFocus value={name} onChange={(e) => { setName(e.target.value); setFieldError(undefined) }} invalid={!!fieldError} placeholder="Ej. Lácteos" />
        </Field>
      </form>
    </Dialog>
  )
}
