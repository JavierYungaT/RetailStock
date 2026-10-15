import { useState } from 'react'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { PageHeader } from '../../components/PageHeader'
import { Pagination } from '../../components/Pagination'
import { Spinner } from '../../components/Spinner'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { formatMoney } from '../../lib/format'
import { useAuth } from '../auth/AuthContext'
import { ProductFormModal } from './ProductFormModal'
import { productsApi, type Product } from './productsApi'

const PAGE_SIZE = 10

export function ProductsPage() {
  const { hasRole } = useAuth()
  const isAdmin = hasRole('Admin')

  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [includeInactive, setIncludeInactive] = useState(false)
  // undefined = modal cerrado · null = crear · Product = editar
  const [editing, setEditing] = useState<Product | null | undefined>(undefined)
  const [notice, setNotice] = useState<string | null>(null)

  const debouncedSearch = useDebounce(search.trim())

  const products = useApiQuery(
    (signal) => productsApi.list({ page, pageSize: PAGE_SIZE, name: debouncedSearch || undefined, includeInactive }, signal),
    [page, debouncedSearch, includeInactive],
  )
  // Las categorías solo se necesitan para el formulario (Admin)
  const categories = useApiQuery((signal) => (isAdmin ? productsApi.categories(signal) : Promise.resolve([])), [isAdmin])

  function handleSearchChange(value: string) {
    setSearch(value)
    setPage(1) // una búsqueda nueva siempre empieza en la página 1
  }

  function handleSaved(saved: Product) {
    setNotice(editing ? `Producto ${saved.code} actualizado.` : `Producto ${saved.code} creado.`)
    setEditing(undefined)
    products.reload()
  }

  const data = products.data

  return (
    <>
      <PageHeader
        title="Productos"
        description="Catálogo de productos y precios (sin IVA)."
        actions={isAdmin && <Button onClick={() => { setNotice(null); setEditing(null) }}>+ Nuevo producto</Button>}
      />

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <input
          type="search"
          value={search}
          onChange={(e) => handleSearchChange(e.target.value)}
          placeholder="Buscar por nombre o código…"
          aria-label="Buscar productos"
          className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm sm:max-w-xs focus:border-brand-500 focus:outline-2 focus:outline-brand-500/30"
        />
        {isAdmin && (
          <label className="flex items-center gap-2 text-sm text-slate-600">
            <input type="checkbox" checked={includeInactive} onChange={(e) => { setIncludeInactive(e.target.checked); setPage(1) }} className="size-4 accent-brand-500" />
            Incluir inactivos
          </label>
        )}
        {products.loading && data && <Spinner label="Actualizando…" />}
      </div>

      {notice && <div className="mb-4"><Alert tone="success">{notice}</Alert></div>}
      {products.error && <div className="mb-4"><Alert onRetry={products.reload}>{products.error}</Alert></div>}

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
        {!data && products.loading ? (
          <Spinner className="py-16" />
        ) : data && data.items.length === 0 ? (
          <p className="py-16 text-center text-sm text-slate-500">
            {debouncedSearch ? `No hay productos que coincidan con “${debouncedSearch}”.` : 'No hay productos.'}
          </p>
        ) : (
          // overflow-x-auto: en móvil la tabla se desplaza dentro de su caja, no la página.
          // relative: el texto "sr-only" (position: absolute) queda contenido; sin esto escapa y ensancha la página.
          <div className="relative overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-slate-200 bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                <tr>
                  <th className="px-4 py-3 font-medium">Código</th>
                  <th className="px-4 py-3 font-medium">Nombre</th>
                  <th className="hidden px-4 py-3 font-medium md:table-cell">Categoría</th>
                  <th className="px-4 py-3 text-right font-medium">Precio</th>
                  <th className="px-4 py-3 font-medium">Estado</th>
                  {isAdmin && <th className="px-4 py-3"><span className="sr-only">Acciones</span></th>}
                </tr>
              </thead>
              <tbody className={`divide-y divide-slate-100 ${products.loading ? 'opacity-60' : ''}`}>
                {data?.items.map((product) => (
                  <tr key={product.id} className="hover:bg-slate-50">
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-xs text-slate-600">{product.code}</td>
                    <td className="px-4 py-3 font-medium">{product.name}</td>
                    <td className="hidden px-4 py-3 text-slate-600 md:table-cell">{product.categoryName}</td>
                    <td className="tabular whitespace-nowrap px-4 py-3 text-right">{formatMoney(product.price)}</td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${product.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                        <span aria-hidden>{product.isActive ? '●' : '○'}</span>
                        {product.isActive ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    {isAdmin && (
                      <td className="px-4 py-3 text-right">
                        <Button variant="ghost" className="px-2 py-1" onClick={() => { setNotice(null); setEditing(product) }}>Editar</Button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {data && data.totalCount > 0 && (
        <div className="mt-4">
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </div>
      )}

      {editing !== undefined && (
        <ProductFormModal
          product={editing}
          categories={categories.data ?? []}
          onClose={() => setEditing(undefined)}
          onSaved={handleSaved}
        />
      )}
    </>
  )
}
