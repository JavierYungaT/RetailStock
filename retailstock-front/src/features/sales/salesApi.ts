import { apiClient } from '../../api/client'
import type { PagedResult } from '../../api/types'

export interface CreateSaleRequest {
  customerName: string
  customerId: string
  warehouseId: number
  items: { productId: number; quantity: number }[]
}

export interface SaleDetail { productId: number; productCode: string; productName: string; quantity: number; unitPrice: number; lineTotal: number }

/** SaleResponse de la API */
export interface Sale {
  id: number
  number: string
  date: string
  customerName: string
  customerId: string
  warehouseId: number
  warehouseName: string
  userId: number
  userEmail: string
  subtotal: number
  tax: number
  total: number
  syncedToSap: boolean
  sapDocEntry: number | null
  details: SaleDetail[]
}

export interface SaleSummary {
  id: number
  number: string
  date: string
  customerName: string
  warehouseName: string
  itemsCount: number
  total: number
  syncedToSap: boolean
}

export interface SaleQuery { page: number; pageSize: number; name?: string; from?: string; to?: string; warehouseId?: number }

export interface SapSyncResult {
  processed: number
  succeeded: number
  failed: number
  items: { saleId: number; saleNumber: string; success: boolean; sapDocEntry: number | null; alreadyExistedInSap: boolean; error: string | null }[]
}

export const salesApi = {
  create: (request: CreateSaleRequest) => apiClient.post<Sale>('/sales', request).then((r) => r.data),
  get: (id: number, signal?: AbortSignal) => apiClient.get<Sale>(`/sales/${id}`, { signal }).then((r) => r.data),
  list: (query: SaleQuery, signal?: AbortSignal) =>
    apiClient.get<PagedResult<SaleSummary>>('/sales', { params: query, signal }).then((r) => r.data),
  syncToSap: () => apiClient.post<SapSyncResult>('/sap/sync-sales', null, { params: { batchSize: 50 } }).then((r) => r.data),
}
