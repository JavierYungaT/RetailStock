import { DollarSign, Hash } from 'lucide-react'
import { omit } from '../../lib/omit'
import { useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { Alert } from '../../components/ui/Alert'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { Field, Input, Select } from '../../components/ui/Field'
import { formatMoney } from '../../lib/format'
import type { Category } from '../categories/categoriesApi'
import { productsApi, type Product } from './productsApi'

const TAX_RATE = 0.15

/** Crear (product = null) o editar un producto en un panel lateral. */
export function ProductFormDrawer({ product, categories, onClose, onSaved }: {
  product: Product | null
  categories: Category[]
  onClose: () => void
  onSaved: () => void
}) {
  const isEdit = product !== null
  const [form, setForm] = useState({
    code: product?.code ?? '',
    name: product?.name ?? '',
    categoryId: product ? String(product.categoryId) : '',
    price: product ? String(product.price) : '',
    isActive: product?.isActive ?? true,
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) => {
    setForm((f) => ({ ...f, [key]: value }))
    setFieldErrors((errors) => omit(errors, key)) // al corregir el campo desaparece su error
  }

  const price = Number(form.price)
  const priceWithTax = Number.isFinite(price) && price > 0 ? Math.round(price * (1 + TAX_RATE) * 100) / 100 : null

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true); setError(null); setFieldErrors({})
    try {
      const common = { name: form.name, categoryId: Number(form.categoryId), price }
      const saved = isEdit
        ? await productsApi.update(product.id, { ...common, isActive: form.isActive })
        : await productsApi.create({ ...common, code: form.code })
      toast.success(isEdit ? `Producto ${saved.code} actualizado` : `Producto ${saved.code} creado`)
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
    <Drawer
      open
      onClose={onClose}
      title={isEdit ? product.name : 'Nuevo producto'}
      description={isEdit ? <span className="font-mono">{product.code}</span> : 'Registra un artículo en el catálogo.'}
      footer={<><Button variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" form="product-form" loading={saving}>{isEdit ? 'Guardar cambios' : 'Crear producto'}</Button></>}
    >
      <form id="product-form" onSubmit={handleSubmit} className="space-y-5" noValidate>
        {error && <Alert>{error}</Alert>}

        <Field label="Código" htmlFor="code" error={fieldErrors.code} hint={isEdit ? 'El código no se modifica: es el ItemCode en SAP.' : 'Letras, números y guiones. Ej. ABR-011'} required>
          <Input id="code" value={form.code} onChange={(e) => set('code', e.target.value.toUpperCase())} disabled={isEdit} invalid={!!fieldErrors.code} leftIcon={<Hash />} autoFocus={!isEdit} className="font-mono" />
        </Field>

        <Field label="Nombre" htmlFor="name" error={fieldErrors.name} required>
          <Input id="name" value={form.name} onChange={(e) => set('name', e.target.value)} invalid={!!fieldErrors.name} placeholder="Ej. Fideo Don Vittorio 400 g" autoFocus={isEdit} />
        </Field>

        <div className="grid gap-5 sm:grid-cols-2">
          <Field label="Categoría" htmlFor="categoryId" error={fieldErrors.categoryId} required>
            <Select id="categoryId" value={form.categoryId} onChange={(e) => set('categoryId', e.target.value)} invalid={!!fieldErrors.categoryId}>
              <option value="">Seleccione…</option>
              {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </Select>
          </Field>
          <Field label="Precio sin IVA" htmlFor="price" error={fieldErrors.price} hint={priceWithTax ? `Con IVA 15%: ${formatMoney(priceWithTax)}` : undefined} required>
            <Input id="price" type="number" min="0.01" step="0.01" inputMode="decimal" value={form.price} onChange={(e) => set('price', e.target.value)} invalid={!!fieldErrors.price} leftIcon={<DollarSign />} placeholder="0.00" className="tabular" />
          </Field>
        </div>

        {isEdit && (
          <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-border p-4 transition-colors hover:bg-surface-hover">
            <input type="checkbox" checked={form.isActive} onChange={(e) => set('isActive', e.target.checked)} className="mt-0.5 size-4 accent-[var(--primary)]" />
            <span>
              <span className="block text-sm font-medium">Producto activo</span>
              <span className="block text-xs text-fg-muted">Los productos inactivos no se pueden vender ni ingresar a bodega, pero conservan su historial.</span>
            </span>
          </label>
        )}
      </form>
    </Drawer>
  )
}
