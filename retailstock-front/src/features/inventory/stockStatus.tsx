import { Badge } from '../../components/ui/Badge'

/** Umbral de "stock bajo" usado en existencias, punto de venta y dashboard. */
export const LOW_STOCK_THRESHOLD = 10

export function StockBadge({ quantity }: { quantity: number }) {
  if (quantity === 0) return <Badge tone="danger" dot>Agotado</Badge>
  if (quantity < LOW_STOCK_THRESHOLD) return <Badge tone="warning" dot>Stock bajo</Badge>
  return <Badge tone="success" dot>Disponible</Badge>
}
