import { useMemo, useState, type FormEvent } from 'react'
import { getErrorMessage, getProblem } from '../../api/errors'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Field, SelectInput, TextInput } from '../../components/Field'
import { PageHeader } from '../../components/PageHeader'
import { Spinner } from '../../components/Spinner'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { formatDateTime, formatMoney } from '../../lib/format'
import { productsApi } from '../products/productsApi'
import { addToCart, calculateTotals, setQuantity, TAX_RATE_PERCENT, type CartItem } from './cart'
import { salesApi, type Sale } from './salesApi'

const FINAL_CONSUMER = { name: 'Consumidor final', id: '9999999999999' }

export function NewSalePage() {
  const [selectedWarehouseId, setWarehouseId] = useState('')
  const [customerName, setCustomerName] = useState('')
  const [customerId, setCustomerId] = useState('')
  const [search, setSearch] = useState('')
  const [cart, setCart] = useState<CartItem[]>([])
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [stockErrorProductId, setStockErrorProductId] = useState<number | null>(null)
  const [completedSale, setCompletedSale] = useState<Sale | null>(null)

  const warehouses = useApiQuery((signal) => salesApi.warehouses(signal), [])
  const debouncedSearch = useDebounce(search.trim())
  const results = useApiQuery(
    (signal) =>
      debouncedSearch.length < 2
        ? Promise.resolve(null) // no buscar con 0-1 letras
        : productsApi.list({ page: 1, pageSize: 8, name: debouncedSearch }, signal),
    [debouncedSearch],
  )

  // Valor DERIVADO (no un useEffect + setState): si hay una sola bodega y no se eligió otra, se usa esa
  const warehouseId = selectedWarehouseId || (warehouses.data?.length === 1 ? String(warehouses.data[0].id) : '')

  // useMemo: recalcula los totales solo cuando cambia el carrito
  const totals = useMemo(() => calculateTotals(cart), [cart])

  function updateCart(next: CartItem[]) {
    setCart(next)
    setError(null)
    setStockErrorProductId(null)
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    setStockErrorProductId(null)
    try {
      const sale = await salesApi.create({
        customerName,
        customerId,
        warehouseId: Number(warehouseId),
        items: cart.map((i) => ({ productId: i.productId, quantity: i.quantity })),
      })
      setCompletedSale(sale)
    } catch (err) {
      const problem = getProblem(err)
      // 409 de stock insuficiente: la API indica producto, disponible y solicitado
      if (problem?.status === 409 && problem.productId !== undefined) {
        const item = cart.find((i) => i.productId === problem.productId)
        setStockErrorProductId(problem.productId)
        setError(
          `Stock insuficiente de “${item?.name ?? `producto ${problem.productId}`}” en esta bodega: ` +
            `disponible ${problem.available}, solicitado ${problem.requested}.`,
        )
      } else {
        setError(getErrorMessage(err, 'No se pudo registrar la venta.'))
      }
    } finally {
      setSubmitting(false)
    }
  }

  function startNewSale() {
    setCompletedSale(null)
    setCart([])
    setCustomerName('')
    setCustomerId('')
    setSearch('')
  }

  if (completedSale) return <SaleReceipt sale={completedSale} onNewSale={startNewSale} />

  const canSubmit = cart.length > 0 && warehouseId !== '' && customerName.trim() !== '' && customerId.trim() !== ''

  return (
    <>
      <PageHeader title="Nueva venta" description={`Los precios no incluyen IVA; se agrega ${TAX_RATE_PERCENT}% al confirmar.`} />

      <form onSubmit={handleSubmit} className="grid gap-6 lg:grid-cols-[1fr_22rem]" noValidate>
        {/* Columna izquierda: datos, buscador y carrito */}
        <div className="space-y-6">
          <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
            <h2 className="mb-4 text-sm font-semibold text-slate-700">Datos de la venta</h2>
            {warehouses.error && <div className="mb-4"><Alert onRetry={warehouses.reload}>{warehouses.error}</Alert></div>}
            <div className="grid gap-4 sm:grid-cols-3">
              <Field label="Bodega" htmlFor="warehouse">
                <SelectInput id="warehouse" value={warehouseId} onChange={(e) => { setWarehouseId(e.target.value); setError(null) }} disabled={warehouses.loading}>
                  <option value="">{warehouses.loading ? 'Cargando…' : 'Seleccione…'}</option>
                  {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
                </SelectInput>
              </Field>
              <Field label="Cliente" htmlFor="customerName">
                <TextInput id="customerName" value={customerName} onChange={(e) => setCustomerName(e.target.value)} placeholder="Nombre" />
              </Field>
              <Field label="Cédula / RUC" htmlFor="customerId">
                <TextInput id="customerId" inputMode="numeric" maxLength={13} value={customerId} onChange={(e) => setCustomerId(e.target.value.replace(/\D/g, ''))} placeholder="10 o 13 dígitos" />
              </Field>
            </div>
            <button
              type="button"
              onClick={() => { setCustomerName(FINAL_CONSUMER.name); setCustomerId(FINAL_CONSUMER.id) }}
              className="mt-3 text-sm font-medium text-brand-600 hover:underline"
            >
              Usar consumidor final
            </button>
          </section>

          <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
            <h2 className="mb-4 text-sm font-semibold text-slate-700">Agregar productos</h2>
            <input
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Buscar por nombre o código (mín. 2 letras)…"
              aria-label="Buscar productos"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-brand-500 focus:outline-2 focus:outline-brand-500/30"
            />
            {results.loading && debouncedSearch.length >= 2 && <Spinner className="mt-3 justify-start" label="Buscando…" />}
            {results.error && <div className="mt-3"><Alert>{results.error}</Alert></div>}
            {results.data && (
              results.data.items.length === 0 ? (
                <p className="mt-3 text-sm text-slate-500">Sin resultados para “{debouncedSearch}”.</p>
              ) : (
                <ul className="mt-3 divide-y divide-slate-100 rounded-lg border border-slate-200">
                  {results.data.items.map((product) => (
                    <li key={product.id} className="flex items-center justify-between gap-3 px-3 py-2">
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium">{product.name}</p>
                        <p className="font-mono text-xs text-slate-500">{product.code}</p>
                      </div>
                      <div className="flex shrink-0 items-center gap-3">
                        <span className="tabular text-sm">{formatMoney(product.price)}</span>
                        <Button type="button" variant="secondary" className="px-3 py-1" onClick={() => updateCart(addToCart(cart, product))}>
                          Agregar
                        </Button>
                      </div>
                    </li>
                  ))}
                </ul>
              )
            )}
          </section>

          <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
            <h2 className="border-b border-slate-200 px-5 py-4 text-sm font-semibold text-slate-700">
              Carrito <span className="font-normal text-slate-500">({totals.itemCount} unidades)</span>
            </h2>
            {cart.length === 0 ? (
              <p className="px-5 py-10 text-center text-sm text-slate-500">Busca un producto y presiona “Agregar”.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="text-left text-xs uppercase tracking-wide text-slate-500">
                    <tr>
                      <th className="px-5 py-2 font-medium">Producto</th>
                      <th className="px-2 py-2 text-center font-medium">Cantidad</th>
                      <th className="px-2 py-2 text-right font-medium">P. unit.</th>
                      <th className="px-5 py-2 text-right font-medium">Total</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {cart.map((item) => (
                      <tr key={item.productId} className={item.productId === stockErrorProductId ? 'bg-red-50' : ''}>
                        <td className="px-5 py-2">
                          <p className="font-medium">{item.name}</p>
                          <p className="font-mono text-xs text-slate-500">{item.code}</p>
                        </td>
                        <td className="px-2 py-2">
                          <div className="flex items-center justify-center gap-1">
                            <QuantityButton label={`Quitar una unidad de ${item.name}`} onClick={() => updateCart(setQuantity(cart, item.productId, item.quantity - 1))}>−</QuantityButton>
                            <QuantityInput
                              label={`Cantidad de ${item.name}`}
                              value={item.quantity}
                              onChange={(quantity) => updateCart(setQuantity(cart, item.productId, quantity))}
                            />
                            <QuantityButton label={`Agregar una unidad de ${item.name}`} onClick={() => updateCart(setQuantity(cart, item.productId, item.quantity + 1))}>+</QuantityButton>
                          </div>
                        </td>
                        <td className="tabular whitespace-nowrap px-2 py-2 text-right">{formatMoney(item.unitPrice)}</td>
                        <td className="tabular whitespace-nowrap px-5 py-2 text-right font-medium">{formatMoney(item.unitPrice * item.quantity)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>

        {/* Columna derecha: totales y confirmación (fija al hacer scroll en escritorio) */}
        <aside className="h-fit space-y-4 rounded-xl border border-slate-200 bg-white p-5 shadow-sm lg:sticky lg:top-6">
          <h2 className="text-sm font-semibold text-slate-700">Resumen</h2>
          <dl className="tabular space-y-2 text-sm">
            <div className="flex justify-between"><dt className="text-slate-500">Subtotal</dt><dd>{formatMoney(totals.subtotal)}</dd></div>
            <div className="flex justify-between"><dt className="text-slate-500">IVA {TAX_RATE_PERCENT}%</dt><dd>{formatMoney(totals.tax)}</dd></div>
            <div className="flex justify-between border-t border-slate-200 pt-2 text-lg font-semibold"><dt>Total</dt><dd>{formatMoney(totals.total)}</dd></div>
          </dl>

          {error && <Alert title={stockErrorProductId ? 'Sin stock suficiente' : undefined}>{error}</Alert>}

          <Button type="submit" className="w-full py-2.5" loading={submitting} disabled={!canSubmit}>
            Confirmar venta
          </Button>
          {!canSubmit && (
            <p className="text-xs text-slate-500">Completa bodega, cliente e identificación, y agrega al menos un producto.</p>
          )}
        </aside>
      </form>
    </>
  )
}

/**
 * Cantidad editable. Guarda un "borrador" de texto mientras se escribe: así se puede borrar el 1 y escribir 6
 * (si el input estuviera atado directo al número, al quedar vacío volvería a 1 y se obtendría "16").
 * Solo se confirma al carrito un entero válido; al salir del campo se descarta el borrador.
 */
function QuantityInput({ label, value, onChange }: { label: string; value: number; onChange: (quantity: number) => void }) {
  const [draft, setDraft] = useState<string | null>(null)
  return (
    <input
      type="text"
      inputMode="numeric"
      aria-label={label}
      value={draft ?? String(value)}
      onFocus={(e) => e.target.select()} // escribir reemplaza el valor
      onChange={(e) => {
        const text = e.target.value.replace(/\D/g, '')
        setDraft(text)
        const quantity = Number.parseInt(text, 10)
        if (quantity > 0) onChange(quantity)
      }}
      onBlur={() => setDraft(null)}
      className="tabular w-14 rounded-md border border-slate-300 px-1 py-1 text-center"
    />
  )
}

function QuantityButton({ label, onClick, children }: { label: string; onClick: () => void; children: string }) {
  return (
    <button type="button" aria-label={label} onClick={onClick} className="size-7 rounded-md border border-slate-300 text-slate-600 hover:bg-slate-100">
      {children}
    </button>
  )
}

/** Comprobante con los montos OFICIALES devueltos por la API. */
function SaleReceipt({ sale, onNewSale }: { sale: Sale; onNewSale: () => void }) {
  return (
    <div className="mx-auto max-w-lg">
      <Alert tone="success" title={`Venta ${sale.number} registrada`}>
        {formatDateTime(sale.date)} · {sale.warehouseName}
      </Alert>
      <div className="mt-4 rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
        <p className="text-sm text-slate-500">Cliente</p>
        <p className="font-medium">{sale.customerName} <span className="text-slate-500">({sale.customerId})</span></p>
        <ul className="mt-4 divide-y divide-slate-100 border-y border-slate-100 text-sm">
          {sale.details.map((d) => (
            <li key={d.productId} className="tabular flex justify-between gap-3 py-2">
              <span>{d.quantity} × {d.productName}</span>
              <span>{formatMoney(d.lineTotal)}</span>
            </li>
          ))}
        </ul>
        <dl className="tabular mt-4 space-y-1 text-sm">
          <div className="flex justify-between"><dt className="text-slate-500">Subtotal</dt><dd>{formatMoney(sale.subtotal)}</dd></div>
          <div className="flex justify-between"><dt className="text-slate-500">IVA {TAX_RATE_PERCENT}%</dt><dd>{formatMoney(sale.tax)}</dd></div>
          <div className="flex justify-between text-lg font-semibold"><dt>Total</dt><dd>{formatMoney(sale.total)}</dd></div>
        </dl>
        <Button className="mt-5 w-full" onClick={onNewSale}>Nueva venta</Button>
      </div>
    </div>
  )
}
