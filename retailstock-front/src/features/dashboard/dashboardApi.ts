import { apiClient } from '../../api/client'

/** Respuestas de /api/reports (Dapper + procedimientos almacenados, solo Admin). */
export interface SalesByDay { day: string; salesCount: number; total: number; averageTicket: number }
export interface LowStockItem { productId: number; productCode: string; productName: string; categoryName: string; warehouseId: number; warehouseName: string; quantity: number }
export interface TopProduct { productId: number; productCode: string; productName: string; quantitySold: number; revenue: number; salesCount: number }

export const dashboardApi = {
  salesByDay: (from: string, to: string, signal?: AbortSignal) =>
    apiClient.get<SalesByDay[]>('/reports/sales-by-day', { params: { from, to }, signal }).then((r) => r.data),
  lowStock: (threshold: number, signal?: AbortSignal) =>
    apiClient.get<LowStockItem[]>('/reports/low-stock', { params: { threshold }, signal }).then((r) => r.data),
  topProducts: (from: string, to: string, top: number, signal?: AbortSignal) =>
    apiClient.get<TopProduct[]>('/reports/top-products', { params: { from, to, top }, signal }).then((r) => r.data),
}
