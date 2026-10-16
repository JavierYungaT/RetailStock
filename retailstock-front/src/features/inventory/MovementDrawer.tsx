import { ArrowDownToLine, ArrowLeftRight, ArrowRight, ArrowUpFromLine } from 'lucide-react'
import { omit } from '../../lib/omit'
import { useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { getErrorMessage, getFieldErrors, getProblem } from '../../api/errors'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { Field, Input, Select } from '../../components/ui/Field'
import { ProductPicker } from '../../components/ui/ProductPicker'
import { useApiQuery } from '../../hooks/useApiQuery'
import { useWarehouses } from '../../hooks/useLookups'
import { cn } from '../../lib/cn'
import { formatInteger } from '../../lib/format'
import { inventoryApi, type MovementType } from './inventoryApi'

export interface MovementDefaults {
  type?: MovementType
  product?: { id: number; code: string; name: string } | null
  warehouseId?: number
}

const typeOptions: { value: MovementType; label: string; description: string; icon: typeof ArrowDownToLine }[] = [
  { value: 'In', label: 'Ingreso', description: 'Compra o recepción', icon: ArrowDownToLine },
  { value: 'Out', label: 'Egreso', description: 'Merma o ajuste', icon: ArrowUpFromLine },
  { value: 'Transfer', label: 'Transferencia', description: 'Entre bodegas', icon: ArrowLeftRight },
]

/** Registrar ingreso, egreso o transferencia. Muestra el stock disponible en la bodega de origen. */
export function MovementDrawer({ defaults, onClose, onSaved }: { defaults?: MovementDefaults; onClose: () => void; onSaved: () => void }) {
  const [type, setType] = useState<MovementType>(defaults?.type ?? 'In')
  const [product, setProduct] = useState(defaults?.product ?? null)
  const [warehouseId, setWarehouseId] = useState(defaults?.warehouseId ? String(defaults.warehouseId) : '')
  const [toWarehouseId, setToWarehouseId] = useState('')
  const [quantity, setQuantity] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const warehouses = useWarehouses()

  // Stock actual del producto en la bodega elegida (origen en egresos/transferencias)
  const current = useApiQuery(
    (signal) => product && warehouseId
      ? inventoryApi.stock({ page: 1, pageSize: 1, productId: product.id, warehouseId: Number(warehouseId) }, signal).then((r) => r.items[0]?.quantity ?? 0)
      : Promise.resolve(null),
    [product?.id, warehouseId],
  )
  const available = current.data
  const qty = Number(quantity)
  const exceeds = type !== 'In' && available !== null && qty > available

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!product) { setFieldErrors({ productId: 'Selecciona un producto.' }); return }
    setSaving(true); setError(null); setFieldErrors({})
    try {
      const base = { productId: product.id, quantity: qty }
      const movement = type === 'In'
        ? await inventoryApi.entry({ ...base, warehouseId: Number(warehouseId) })
        : type === 'Out'
          ? await inventoryApi.exit({ ...base, warehouseId: Number(warehouseId) })
          : await inventoryApi.transfer({ ...base, fromWarehouseId: Number(warehouseId), toWarehouseId: Number(toWarehouseId) })
      const verb = { In: 'Ingreso', Out: 'Egreso', Transfer: 'Transferencia' }[type]
      toast.success(`${verb} registrado`, { description: `${formatInteger(movement.quantity)} × ${movement.productName}` })
      onSaved()
    } catch (err) {
      const fields = getFieldErrors(err)
      setFieldErrors(fields)
      const problem = getProblem(err)
      if (problem?.status === 409 && problem.available !== undefined) setError(`Stock insuficiente: disponible ${problem.available}, solicitado ${problem.requested}.`)
      else if (Object.keys(fields).length === 0) setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Drawer
      open
      onClose={onClose}
      title="Nuevo movimiento"
      description="Queda registrado en el kardex con tu usuario."
      footer={<><Button variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" form="movement-form" loading={saving} disabled={exceeds}>Registrar movimiento</Button></>}
    >
      <form id="movement-form" onSubmit={handleSubmit} className="space-y-5" noValidate>
        <div role="radiogroup" aria-label="Tipo de movimiento" className="grid grid-cols-3 gap-2">
          {typeOptions.map(({ value, label, description, icon: Icon }) => (
            <button
              key={value}
              type="button"
              role="radio"
              aria-checked={type === value}
              onClick={() => { setType(value); setError(null) }}
              className={cn('rounded-xl border p-3 text-left transition-all', type === value ? 'border-primary bg-primary-soft/60 ring-3 ring-ring' : 'border-border hover:bg-surface-hover')}
            >
              <Icon className={cn('mb-2 size-4', type === value ? 'text-primary' : 'text-fg-subtle')} />
              <span className="block text-sm font-medium">{label}</span>
              <span className="block text-xs text-fg-muted">{description}</span>
            </button>
          ))}
        </div>

        {error && <Alert>{error}</Alert>}

        <Field label="Producto" htmlFor="movement-product" error={fieldErrors.productId} required>
          <ProductPicker id="movement-product" value={product} onChange={(p) => { setProduct(p); setFieldErrors((errors) => omit(errors, 'productId')) }} invalid={!!fieldErrors.productId} />
        </Field>

        <div className={cn('grid gap-3', type === 'Transfer' && 'sm:grid-cols-[1fr_auto_1fr] sm:items-end')}>
          <Field label={type === 'In' ? 'Bodega de destino' : 'Bodega de origen'} htmlFor="movement-warehouse" error={fieldErrors.warehouseId ?? fieldErrors.fromWarehouseId} required>
            <Select id="movement-warehouse" value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)}>
              <option value="">Seleccione…</option>
              {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
            </Select>
          </Field>
          {type === 'Transfer' && (
            <>
              <ArrowRight className="mx-auto mb-2.5 hidden size-4 text-fg-subtle sm:block" aria-hidden />
              <Field label="Bodega de destino" htmlFor="movement-to" error={fieldErrors.toWarehouseId} required>
                <Select id="movement-to" value={toWarehouseId} onChange={(e) => setToWarehouseId(e.target.value)}>
                  <option value="">Seleccione…</option>
                  {warehouses.data?.filter((w) => String(w.id) !== warehouseId).map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
                </Select>
              </Field>
            </>
          )}
        </div>

        {product && warehouseId && (
          <div className="flex items-center justify-between rounded-xl border border-border bg-surface-muted/60 px-4 py-3 text-sm">
            <span className="text-fg-muted">Stock actual en la bodega</span>
            <span className="tabular font-semibold">{current.loading ? '…' : `${formatInteger(available ?? 0)} ud.`}</span>
          </div>
        )}

        <Field
          label="Cantidad"
          htmlFor="movement-quantity"
          error={fieldErrors.quantity ?? (exceeds ? `No puede superar las ${available} unidades disponibles.` : undefined)}
          hint={type === 'In' && available !== null && qty > 0 ? `Quedará en ${formatInteger(available + qty)} ud.` : type !== 'In' && available !== null && qty > 0 && !exceeds ? `Quedará en ${formatInteger(available - qty)} ud.` : undefined}
          required
        >
          <Input id="movement-quantity" type="number" min={1} inputMode="numeric" value={quantity} onChange={(e) => { setQuantity(e.target.value); setFieldErrors((errors) => omit(errors, 'quantity')) }} invalid={exceeds || !!fieldErrors.quantity} className="tabular" placeholder="0" />
        </Field>
      </form>
    </Drawer>
  )
}
