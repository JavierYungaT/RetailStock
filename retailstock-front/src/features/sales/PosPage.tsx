import { Minus, Package, Plus, ScanBarcode, ShoppingCart, Trash2, UserRound, Warehouse } from 'lucide-react'
import { omit } from '../../lib/omit'
import { useEffect, useMemo, useRef, useState } from 'react'
import { toast } from 'sonner'
import { getErrorMessage, getFieldErrors, getProblem } from '../../api/errors'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { Field, Input, Select } from '../../components/ui/Field'
import { SearchInput } from '../../components/ui/SearchInput'
import { Skeleton } from '../../components/ui/Skeleton'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useDebounce } from '../../hooks/useDebounce'
import { useCategories, useWarehouses } from '../../hooks/useLookups'
import { cn } from '../../lib/cn'
import { formatInteger, formatMoney } from '../../lib/format'
import { inventoryApi, type StockItem } from '../inventory/inventoryApi'
import { LOW_STOCK_THRESHOLD } from '../inventory/stockStatus'
import { addToCart, calculateTotals, lineTotal, setQuantity, TAX_RATE_PERCENT, type CartItem } from './cart'
import { ReceiptDialog } from './ReceiptDialog'
import { salesApi, type Sale } from './salesApi'

const FINAL_CONSUMER = { name: 'Consumidor final', id: '9999999999999' }
const PAGE_SIZE = 24

/** Punto de venta: catálogo con stock de la bodega + carrito + cobro. */
export function PosPage() {
  const warehouses = useWarehouses()
  const categories = useCategories()
  const [selectedWarehouseId, setWarehouseId] = useState('')
  const [categoryId, setCategoryId] = useState<number | null>(null)
  const [search, setSearch] = useState('')
  const [cart, setCart] = useState<CartItem[]>([])
  const [customer, setCustomer] = useState({ name: '', id: '' })
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [stockErrorProductId, setStockErrorProductId] = useState<number | null>(null)
  const [completedSale, setCompletedSale] = useState<Sale | null>(null)
  const searchRef = useRef<HTMLDivElement>(null)
  const cartRef = useRef<HTMLElement>(null)
  const [cartVisible, setCartVisible] = useState(false)
  const debounced = useDebounce(search.trim(), 250)

  // Una sola bodega -> se usa esa (valor derivado, sin efecto)
  const warehouseId = selectedWarehouseId || (warehouses.data?.length === 1 ? String(warehouses.data[0].id) : '')
  const warehouse = warehouses.data?.find((w) => String(w.id) === warehouseId)

  const catalog = useApiQuery(
    (signal) => warehouseId
      ? inventoryApi.stock({ page: 1, pageSize: PAGE_SIZE, warehouseId: Number(warehouseId), name: debounced || undefined, categoryId: categoryId ?? undefined, onlyActive: true }, signal)
      : Promise.resolve(null),
    [warehouseId, debounced, categoryId],
  )

  const totals = useMemo(() => calculateTotals(cart), [cart])
  const inCart = (productId: number) => cart.find((i) => i.productId === productId)?.quantity ?? 0

  // Atajo "/" para enfocar el buscador (como en los POS y en GitHub)
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement
      if (e.key === '/' && !['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName)) {
        e.preventDefault()
        searchRef.current?.querySelector('input')?.focus()
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  // Observa si el carrito está en pantalla (móvil): si lo está, la barra flotante sobra
  useEffect(() => {
    const element = cartRef.current
    if (!element) return
    const observer = new IntersectionObserver(([entry]) => setCartVisible(entry.isIntersecting), { threshold: 0.15 })
    observer.observe(element)
    return () => observer.disconnect()
  }, [])

  function updateCart(next: CartItem[]) {
    setCart(next)
    setError(null)
    setStockErrorProductId(null)
  }

  function changeWarehouse(id: string) {
    if (cart.length > 0) toast.info('Carrito vaciado', { description: 'El stock cambia según la bodega.' })
    setWarehouseId(id)
    setCart([])
  }

  function add(item: StockItem) {
    if (inCart(item.productId) >= item.quantity) {
      toast.warning(`Solo hay ${item.quantity} unidades de ${item.productName}`)
      return
    }
    updateCart(addToCart(cart, { productId: item.productId, code: item.productCode, name: item.productName, unitPrice: item.price, available: item.quantity }))
  }

  async function checkout() {
    setSubmitting(true); setError(null); setFieldErrors({}); setStockErrorProductId(null)
    try {
      const sale = await salesApi.create({
        customerName: customer.name,
        customerId: customer.id,
        warehouseId: Number(warehouseId),
        items: cart.map((i) => ({ productId: i.productId, quantity: i.quantity })),
      })
      setCompletedSale(sale)
      catalog.reload() // el stock cambió
    } catch (err) {
      const problem = getProblem(err)
      if (problem?.status === 409 && problem.productId !== undefined) {
        const item = cart.find((i) => i.productId === problem.productId)
        setStockErrorProductId(problem.productId)
        setError(`Otro cajero vendió unidades de “${item?.name}”: ahora hay ${problem.available} disponibles.`)
        catalog.reload()
      } else {
        const fields = getFieldErrors(err)
        setFieldErrors(fields)
        if (Object.keys(fields).length === 0) setError(getErrorMessage(err, 'No se pudo registrar la venta.'))
      }
    } finally {
      setSubmitting(false)
    }
  }

  function newSale() {
    setCompletedSale(null)
    setCart([])
    setCustomer({ name: '', id: '' })
    setSearch('')
  }

  const canCheckout = cart.length > 0 && !!warehouseId && customer.name.trim() !== '' && customer.id.trim() !== ''

  return (
    <div className={cn('grid gap-6 xl:grid-cols-[1fr_400px]', cart.length > 0 && 'pb-20 xl:pb-0')}>
      {/* ================= Catálogo ================= */}
      <section className="min-w-0">
        <div className="mb-5 flex flex-wrap items-end justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Punto de venta</h1>
            <p className="mt-1 text-sm text-fg-muted">Precios sin IVA · se agrega {TAX_RATE_PERCENT}% al cobrar.</p>
          </div>
          <div className="w-full sm:w-60">
            <Select value={warehouseId} onChange={(e) => changeWarehouse(e.target.value)} aria-label="Bodega" disabled={warehouses.loading}>
              <option value="">{warehouses.loading ? 'Cargando bodegas…' : 'Selecciona la bodega…'}</option>
              {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
            </Select>
          </div>
        </div>

        {!warehouseId ? (
          <div className="rounded-xl border border-dashed border-border-strong bg-surface">
            <EmptyState icon={<Warehouse />} title="Selecciona la bodega" description="El catálogo muestra el stock disponible de la bodega donde se despacha la venta." />
          </div>
        ) : (
          <>
            <div className="mb-4 space-y-3">
              <div ref={searchRef} className="relative">
                <SearchInput value={search} onChange={setSearch} placeholder="Buscar por nombre o código…" />
                <kbd className="pointer-events-none absolute top-1/2 right-10 hidden -translate-y-1/2 rounded border border-border px-1.5 text-[11px] text-fg-subtle sm:block">/</kbd>
              </div>
              <div className="scrollbar-thin -mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
                {[{ id: null, name: 'Todo' }, ...(categories.data ?? [])].map((c) => (
                  <button
                    key={c.id ?? 'all'}
                    type="button"
                    onClick={() => setCategoryId(c.id)}
                    aria-pressed={categoryId === c.id}
                    className={cn(
                      'shrink-0 rounded-full border px-3.5 py-1.5 text-[13px] font-medium transition-colors',
                      categoryId === c.id ? 'border-primary bg-primary text-white' : 'border-border bg-surface text-fg-muted hover:border-border-strong hover:text-fg',
                    )}
                  >
                    {c.name}
                  </button>
                ))}
              </div>
            </div>

            {catalog.error && <Alert className="mb-4">{catalog.error}</Alert>}

            <div className={cn('grid grid-cols-2 gap-3 sm:grid-cols-3 2xl:grid-cols-4', catalog.loading && catalog.data && 'opacity-60')}>
              {!catalog.data && catalog.loading &&
                Array.from({ length: 8 }, (_, i) => (
                  <div key={i} className="rounded-xl border border-border bg-surface p-4"><Skeleton className="mb-3 size-9" /><Skeleton className="mb-2 h-4 w-4/5" /><Skeleton className="h-3 w-1/2" /><Skeleton className="mt-4 h-5 w-16" /></div>
                ))}
              {catalog.data?.items.map((item) => {
                const reserved = inCart(item.productId)
                const remaining = item.quantity - reserved
                const soldOut = remaining <= 0
                return (
                  <button
                    key={item.productId}
                    type="button"
                    onClick={() => add(item)}
                    disabled={item.quantity === 0}
                    className={cn(
                      'group relative flex flex-col rounded-xl border bg-surface p-4 text-left shadow-card transition-all',
                      'hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-pop disabled:pointer-events-none disabled:opacity-50',
                      reserved > 0 ? 'border-primary/50 ring-2 ring-primary/15' : 'border-border',
                    )}
                  >
                    {reserved > 0 && <span className="tabular absolute top-3 right-3 rounded-full bg-primary px-2 py-0.5 text-xs font-semibold text-white">{reserved}</span>}
                    <span className="mb-3 flex size-9 items-center justify-center rounded-lg bg-surface-muted text-fg-muted transition-colors group-hover:bg-primary-soft group-hover:text-primary"><Package className="size-4" /></span>
                    <span className="line-clamp-2 min-h-10 text-sm leading-5 font-medium">{item.productName}</span>
                    <span className="mt-0.5 font-mono text-[11px] text-fg-muted">{item.productCode}</span>
                    <span className="mt-3 flex items-end justify-between gap-2">
                      <span className="tabular text-base font-semibold">{formatMoney(item.price)}</span>
                      <span className={cn('tabular text-xs font-medium', item.quantity === 0 || soldOut ? 'text-red-600 dark:text-red-400' : remaining < LOW_STOCK_THRESHOLD ? 'text-amber-600 dark:text-amber-400' : 'text-fg-muted')}>
                        {item.quantity === 0 ? 'Agotado' : `${formatInteger(Math.max(remaining, 0))} disp.`}
                      </span>
                    </span>
                  </button>
                )
              })}
            </div>
            {catalog.data && catalog.data.items.length === 0 && (
              <div className="rounded-xl border border-border bg-surface">
                <EmptyState icon={<ScanBarcode />} title="Sin productos" description={debounced ? `Nada coincide con “${debounced}” en ${warehouse?.name}.` : 'Esta bodega no tiene productos con stock en esta categoría.'} />
              </div>
            )}
            {catalog.data && catalog.data.totalCount > PAGE_SIZE && (
              <p className="mt-3 text-center text-xs text-fg-muted">Mostrando {PAGE_SIZE} de {catalog.data.totalCount}. Usa el buscador para encontrar más productos.</p>
            )}
          </>
        )}
      </section>

      {/* ================= Carrito ================= */}
      <aside id="cart" ref={cartRef} className="h-fit rounded-2xl border border-border bg-surface shadow-card xl:sticky xl:top-24">
        <div className="flex items-center justify-between border-b border-border px-5 py-4">
          <h2 className="flex items-center gap-2 text-sm font-semibold"><ShoppingCart className="size-4 text-fg-muted" /> Venta actual</h2>
          {cart.length > 0 && <button type="button" onClick={() => updateCart([])} className="text-xs font-medium text-fg-muted hover:text-red-600">Vaciar</button>}
        </div>

        <div className="space-y-3 border-b border-border px-5 py-4">
          <div className="flex items-center justify-between">
            <p className="flex items-center gap-1.5 text-[13px] font-medium"><UserRound className="size-3.5 text-fg-muted" /> Cliente</p>
            <button type="button" onClick={() => setCustomer({ name: FINAL_CONSUMER.name, id: FINAL_CONSUMER.id })} className="text-xs font-medium text-primary hover:underline">Consumidor final</button>
          </div>
          <Field label="Nombre" htmlFor="customerName" error={fieldErrors.customerName} className="[&_label]:sr-only">
            <Input id="customerName" placeholder="Nombre del cliente" value={customer.name} onChange={(e) => { setCustomer({ ...customer, name: e.target.value }); setFieldErrors((errors) => omit(errors, 'customerName')) }} invalid={!!fieldErrors.customerName} />
          </Field>
          <Field label="Cédula o RUC" htmlFor="customerId" error={fieldErrors.customerId} className="[&_label]:sr-only">
            <Input id="customerId" placeholder="Cédula (10) o RUC (13 dígitos)" inputMode="numeric" maxLength={13} value={customer.id} onChange={(e) => { setCustomer({ ...customer, id: e.target.value.replace(/\D/g, '') }); setFieldErrors((errors) => omit(errors, 'customerId')) }} invalid={!!fieldErrors.customerId} className="tabular" />
          </Field>
        </div>

        <div className="scrollbar-thin max-h-[42vh] overflow-y-auto">
          {cart.length === 0 ? (
            <div className="px-5 py-10 text-center">
              <ShoppingCart className="mx-auto mb-3 size-8 text-fg-subtle" />
              <p className="text-sm font-medium">El carrito está vacío</p>
              <p className="mt-1 text-xs text-fg-muted">Toca un producto del catálogo para agregarlo.</p>
            </div>
          ) : (
            <ul className="divide-y divide-border">
              {cart.map((item) => (
                <li key={item.productId} className={cn('px-5 py-3', item.productId === stockErrorProductId && 'bg-red-50 dark:bg-red-500/10')}>
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium">{item.name}</p>
                      <p className="tabular text-xs text-fg-muted">{formatMoney(item.unitPrice)} c/u</p>
                    </div>
                    <p className="tabular text-sm font-semibold">{formatMoney(lineTotal(item))}</p>
                  </div>
                  <div className="mt-2 flex items-center justify-between">
                    <div className="flex items-center rounded-lg border border-border">
                      <button type="button" aria-label={`Quitar una unidad de ${item.name}`} onClick={() => updateCart(setQuantity(cart, item.productId, item.quantity - 1))} className="p-1.5 text-fg-muted hover:text-fg"><Minus className="size-3.5" /></button>
                      <QuantityInput label={`Cantidad de ${item.name}`} value={item.quantity} onChange={(q) => updateCart(setQuantity(cart, item.productId, q))} />
                      <button type="button" aria-label={`Agregar una unidad de ${item.name}`} disabled={item.quantity >= item.available} onClick={() => updateCart(setQuantity(cart, item.productId, item.quantity + 1))} className="p-1.5 text-fg-muted hover:text-fg disabled:opacity-30"><Plus className="size-3.5" /></button>
                    </div>
                    <span className="flex items-center gap-2">
                      {item.quantity >= item.available && <span className="text-[11px] font-medium text-amber-600 dark:text-amber-400">Máx. disponible</span>}
                      <button type="button" aria-label={`Quitar ${item.name}`} onClick={() => updateCart(setQuantity(cart, item.productId, 0))} className="rounded-md p-1.5 text-fg-subtle hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-500/10"><Trash2 className="size-3.5" /></button>
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="space-y-4 border-t border-border bg-surface-muted/40 px-5 py-4">
          <dl className="tabular space-y-1.5 text-sm">
            <div className="flex justify-between text-fg-muted"><dt>Subtotal ({totals.itemCount} {totals.itemCount === 1 ? 'unidad' : 'unidades'})</dt><dd>{formatMoney(totals.subtotal)}</dd></div>
            <div className="flex justify-between text-fg-muted"><dt>IVA {TAX_RATE_PERCENT}%</dt><dd>{formatMoney(totals.tax)}</dd></div>
            <div className="flex items-baseline justify-between pt-1"><dt className="font-medium">Total</dt><dd className="text-2xl font-semibold tracking-tight">{formatMoney(totals.total)}</dd></div>
          </dl>
          {error && <Alert title={stockErrorProductId ? 'Stock insuficiente' : undefined}>{error}</Alert>}
          <Button size="lg" className="w-full" loading={submitting} disabled={!canCheckout} onClick={checkout}>
            Cobrar {cart.length > 0 && formatMoney(totals.total)}
          </Button>
          {!canCheckout && cart.length > 0 && <p className="text-center text-xs text-fg-muted">Completa el nombre y la identificación del cliente.</p>}
        </div>
      </aside>

      {/* Móvil: barra fija con el total que lleva al carrito */}
      {cart.length > 0 && !cartVisible && (
        <a href="#cart" className="fixed inset-x-4 bottom-4 z-20 flex items-center justify-between rounded-xl bg-primary px-4 py-3 text-white shadow-pop xl:hidden">
          <span className="flex items-center gap-2 text-sm font-medium"><ShoppingCart className="size-4" /> {totals.itemCount} en el carrito</span>
          <span className="tabular font-semibold">{formatMoney(totals.total)}</span>
        </a>
      )}

      {completedSale && (
        <ReceiptDialog sale={completedSale} onClose={newSale} onNewSale={() => { newSale(); toast.success(`Venta ${completedSale.number} registrada`) }} />
      )}
    </div>
  )
}

/** Cantidad editable con borrador: se puede borrar el 1 y escribir 6 sin que quede "16". */
function QuantityInput({ label, value, onChange }: { label: string; value: number; onChange: (quantity: number) => void }) {
  const [draft, setDraft] = useState<string | null>(null)
  return (
    <input
      type="text"
      inputMode="numeric"
      aria-label={label}
      value={draft ?? String(value)}
      onFocus={(e) => e.target.select()}
      onChange={(e) => {
        const text = e.target.value.replace(/\D/g, '')
        setDraft(text)
        const quantity = Number.parseInt(text, 10)
        if (quantity > 0) onChange(quantity)
      }}
      onBlur={() => setDraft(null)}
      className="tabular w-10 border-x border-border bg-transparent py-1 text-center text-sm outline-none"
    />
  )
}
