import { useState, type FormEvent } from 'react'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { Alert } from '../../components/Alert'
import { Button } from '../../components/Button'
import { Field, SelectInput, TextInput } from '../../components/Field'
import { Modal } from '../../components/Modal'
import { productsApi, type Category, type Product } from './productsApi'

interface FormState {
  code: string
  name: string
  categoryId: string
  price: string
  isActive: boolean
}

const emptyForm: FormState = { code: '', name: '', categoryId: '', price: '', isActive: true }

/**
 * Crear (product = null) o editar un producto. Los errores de validación (400) se muestran bajo cada
 * campo; el código duplicado (409) arriba del formulario.
 */
export function ProductFormModal({ product, categories, onClose, onSaved }: {
  product: Product | null
  categories: Category[]
  onClose: () => void
  onSaved: (product: Product) => void
}) {
  const isEdit = product !== null
  const [form, setForm] = useState<FormState>(() =>
    product
      ? { code: product.code, name: product.name, categoryId: String(product.categoryId), price: String(product.price), isActive: product.isActive }
      : emptyForm,
  )
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})

  const update = <K extends keyof FormState>(key: K, value: FormState[K]) => setForm((f) => ({ ...f, [key]: value }))

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    try {
      const common = { name: form.name, categoryId: Number(form.categoryId), price: Number(form.price) }
      const saved = isEdit
        ? await productsApi.update(product.id, { ...common, isActive: form.isActive })
        : await productsApi.create({ ...common, code: form.code })
      onSaved(saved)
    } catch (err) {
      const errors = getFieldErrors(err)
      setFieldErrors(errors)
      // Si no hay errores por campo (409, 403, red...), se muestra el mensaje general
      if (Object.keys(errors).length === 0) setError(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal open title={isEdit ? `Editar ${product.code}` : 'Nuevo producto'} onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        {error && <Alert>{error}</Alert>}

        <Field label="Código" htmlFor="code" error={fieldErrors.code}>
          <TextInput
            id="code"
            value={form.code}
            onChange={(e) => update('code', e.target.value)}
            disabled={isEdit}
            invalid={!!fieldErrors.code}
            placeholder="ABR-011"
            autoFocus={!isEdit}
          />
        </Field>
        <Field label="Nombre" htmlFor="name" error={fieldErrors.name}>
          <TextInput id="name" value={form.name} onChange={(e) => update('name', e.target.value)} invalid={!!fieldErrors.name} />
        </Field>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Categoría" htmlFor="categoryId" error={fieldErrors.categoryId}>
            <SelectInput id="categoryId" value={form.categoryId} onChange={(e) => update('categoryId', e.target.value)} invalid={!!fieldErrors.categoryId}>
              <option value="">Seleccione…</option>
              {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </SelectInput>
          </Field>
          <Field label="Precio (sin IVA)" htmlFor="price" error={fieldErrors.price}>
            <TextInput id="price" type="number" min="0.01" step="0.01" inputMode="decimal" value={form.price} onChange={(e) => update('price', e.target.value)} invalid={!!fieldErrors.price} />
          </Field>
        </div>
        {isEdit && (
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={form.isActive} onChange={(e) => update('isActive', e.target.checked)} className="size-4 accent-brand-500" />
            Producto activo
          </label>
        )}

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>{isEdit ? 'Guardar cambios' : 'Crear producto'}</Button>
        </div>
      </form>
    </Modal>
  )
}
