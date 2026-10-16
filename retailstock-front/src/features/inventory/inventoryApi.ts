import { apiClient } from '../../api/client'
import type { PagedResult } from '../../api/types'

export type MovementType = 'In' | 'Out' | 'Transfer'

export interface StockItem {
  productId: number
  productCode: string
  productName: string
  categoryId: number
  categoryName: string
  price: number
  isActive: boolean
  warehouseId: number
  warehouseName: string
  quantity: number
}

export interface Movement {
  id: number
  type: MovementType
  productId: number
  productCode: string
  productName: string
  fromWarehouseId: number | null
  fromWarehouseName: string | null
  toWarehouseId: number | null
  toWarehouseName: string | null
  quantity: number
  date: string
  userId: number
  userEmail: string
}

export interface StockQuery {
  page: number
  pageSize: number
  name?: string
  productId?: number
  warehouseId?: number
  categoryId?: number
  onlyActive?: boolean
}

export interface MovementQuery {
  page: number
  pageSize: number
  name?: string
  productId?: number
  warehouseId?: number
  type?: MovementType
}

export const inventoryApi = {
  stock: (query: StockQuery, signal?: AbortSignal) =>
    apiClient.get<PagedResult<StockItem>>('/inventory/stock', { params: query, signal }).then((r) => r.data),
  movements: (query: MovementQuery, signal?: AbortSignal) =>
    apiClient.get<PagedResult<Movement>>('/inventory/movements', { params: query, signal }).then((r) => r.data),
  entry: (request: { productId: number; warehouseId: number; quantity: number }) =>
    apiClient.post<Movement>('/inventory/entries', request).then((r) => r.data),
  exit: (request: { productId: number; warehouseId: number; quantity: number }) =>
    apiClient.post<Movement>('/inventory/exits', request).then((r) => r.data),
  transfer: (request: { productId: number; fromWarehouseId: number; toWarehouseId: number; quantity: number }) =>
    apiClient.post<Movement>('/inventory/transfers', request).then((r) => r.data),
}
