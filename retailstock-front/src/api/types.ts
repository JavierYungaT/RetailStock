// Tipos que reflejan los DTOs de la API (RetailStock.Application). Si cambia un record en C#, se cambia aquí.

export type Role = 'Admin' | 'Warehouse' | 'Cashier'

/** PagedResult<T> de la API */
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

/**
 * ProblemDetails (RFC 7807) que devuelve el middleware global.
 * "errors" solo viene en los 400 de validación; available/requested en el 409 de stock insuficiente.
 */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  traceId?: string
  errors?: Record<string, string[]>
  productId?: number
  warehouseId?: number
  available?: number
  requested?: number
}
