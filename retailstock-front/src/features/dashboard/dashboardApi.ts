import { apiClient } from '../../api/client'

/** Respuestas de /api/reports (Dapper + procedimientos almacenados, solo Admin). */
export interface SalesByDay {
  day: string // "2026-10-15" (día local del negocio)
  salesCount: number
  total: number
  averageTicket: number
}

export interface LowStockItem {
  productId: number
  productCode: string
  productName: string
  categoryName: string
  warehouseId: number
  warehouseName: string
  quantity: number
}

export const dashboardApi = {
  salesByDay: (from: string, to: string, signal?: AbortSignal) =>
    apiClient.get<SalesByDay[]>('/reports/sales-by-day', { params: { from, to }, signal }).then((r) => r.data),

  lowStock: (threshold: number, signal?: AbortSignal) =>
    apiClient.get<LowStockItem[]>('/reports/low-stock', { params: { threshold }, signal }).then((r) => r.data),
}
