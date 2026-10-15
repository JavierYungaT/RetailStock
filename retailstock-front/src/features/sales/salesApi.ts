import { apiClient } from '../../api/client'
import type { PagedResult } from '../../api/types'

export interface Warehouse {
  id: number
  name: string
  location: string
}

export interface CreateSaleRequest {
  customerName: string
  customerId: string
  warehouseId: number
  items: { productId: number; quantity: number }[]
}

export interface SaleDetail {
  productId: number
  productCode: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

/** SaleResponse de la API */
export interface Sale {
  id: number
  number: string
  date: string
  customerName: string
  customerId: string
  warehouseId: number
  warehouseName: string
  userEmail: string
  subtotal: number
  tax: number
  total: number
  syncedToSap: boolean
  details: SaleDetail[]
}

export const salesApi = {
  warehouses: (signal?: AbortSignal) =>
    apiClient
      .get<PagedResult<Warehouse>>('/warehouses', { params: { page: 1, pageSize: 100 }, signal })
      .then((r) => r.data.items),

  create: (request: CreateSaleRequest) => apiClient.post<Sale>('/sales', request).then((r) => r.data),
}
