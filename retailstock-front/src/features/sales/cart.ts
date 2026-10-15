import type { Product } from '../products/productsApi'

export interface CartItem {
  productId: number
  code: string
  name: string
  unitPrice: number
  quantity: number
}

export interface CartTotals {
  subtotal: number
  tax: number
  total: number
  itemCount: number
}

/** Mismo valor que Sale.TaxRate en el dominio (IVA Ecuador). */
export const TAX_RATE_PERCENT = 15

/**
 * Totales de la vista previa calculados IGUAL que el backend (Sale.AddDetail):
 *  - Se trabaja en CENTAVOS ENTEROS: en JavaScript 0.1 + 0.2 !== 0.3 (números de punto flotante);
 *    el backend usa decimal, aquí enteros para no arrastrar errores.
 *  - IVA redondeado "hacia arriba en la mitad" (0.045 -> 0.05), como MidpointRounding.AwayFromZero.
 * El total oficial es el que devuelve la API; este es solo la vista previa.
 */
export function calculateTotals(items: CartItem[]): CartTotals {
  const subtotalCents = items.reduce((sum, item) => sum + toCents(item.unitPrice) * item.quantity, 0)
  const taxCents = Math.round((subtotalCents * TAX_RATE_PERCENT) / 100) // Math.round(x.5) sube: igual que AwayFromZero para positivos
  return {
    subtotal: subtotalCents / 100,
    tax: taxCents / 100,
    total: (subtotalCents + taxCents) / 100,
    itemCount: items.reduce((sum, item) => sum + item.quantity, 0),
  }
}

const toCents = (amount: number) => Math.round(amount * 100)

/** Agrega un producto o suma 1 si ya está (funciones puras: devuelven un arreglo NUEVO, no mutan el estado). */
export function addToCart(items: CartItem[], product: Product): CartItem[] {
  const existing = items.find((i) => i.productId === product.id)
  if (existing) {
    return items.map((i) => (i.productId === product.id ? { ...i, quantity: i.quantity + 1 } : i))
  }
  return [...items, { productId: product.id, code: product.code, name: product.name, unitPrice: product.price, quantity: 1 }]
}

export function setQuantity(items: CartItem[], productId: number, quantity: number): CartItem[] {
  if (quantity <= 0) return items.filter((i) => i.productId !== productId)
  return items.map((i) => (i.productId === productId ? { ...i, quantity } : i))
}
