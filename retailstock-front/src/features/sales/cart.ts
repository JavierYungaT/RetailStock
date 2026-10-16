export interface CartItem {
  productId: number
  code: string
  name: string
  unitPrice: number
  quantity: number
  /** Unidades disponibles en la bodega al momento de agregarlo (tope del carrito) */
  available: number
}

export interface CartTotals { subtotal: number; tax: number; total: number; itemCount: number }

/** Mismo valor que Sale.TaxRate en el dominio (IVA Ecuador). */
export const TAX_RATE_PERCENT = 15

/**
 * Totales calculados IGUAL que el backend (Sale.AddDetail): en CENTAVOS ENTEROS (en JavaScript
 * 0.1 + 0.2 !== 0.3) y con el IVA redondeado "hacia arriba en la mitad" (0.045 -> 0.05),
 * como MidpointRounding.AwayFromZero. El total oficial es el que devuelve la API.
 */
export function calculateTotals(items: CartItem[]): CartTotals {
  const subtotalCents = items.reduce((sum, item) => sum + toCents(item.unitPrice) * item.quantity, 0)
  const taxCents = Math.round((subtotalCents * TAX_RATE_PERCENT) / 100)
  return {
    subtotal: subtotalCents / 100,
    tax: taxCents / 100,
    total: (subtotalCents + taxCents) / 100,
    itemCount: items.reduce((sum, item) => sum + item.quantity, 0),
  }
}

export const lineTotal = (item: CartItem) => (toCents(item.unitPrice) * item.quantity) / 100

const toCents = (amount: number) => Math.round(amount * 100)

/** Funciones puras: devuelven un arreglo NUEVO (React detecta el cambio por referencia). */
export function addToCart(items: CartItem[], product: Omit<CartItem, 'quantity'>): CartItem[] {
  const existing = items.find((i) => i.productId === product.productId)
  if (existing) return setQuantity(items, product.productId, existing.quantity + 1)
  return product.available > 0 ? [...items, { ...product, quantity: 1 }] : items
}

/** Ajusta la cantidad entre 1 y lo disponible; 0 o menos quita la línea. */
export function setQuantity(items: CartItem[], productId: number, quantity: number): CartItem[] {
  if (quantity <= 0) return items.filter((i) => i.productId !== productId)
  return items.map((i) => (i.productId === productId ? { ...i, quantity: Math.min(quantity, i.available) } : i))
}
