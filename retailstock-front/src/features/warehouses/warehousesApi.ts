import { apiClient } from '../../api/client'
import type { PagedResult } from '../../api/types'

export interface Warehouse { id: number; name: string; location: string }
export interface WarehouseRequest { name: string; location: string }

export const warehousesApi = {
  list: (query: { page: number; pageSize: number; name?: string }, signal?: AbortSignal) =>
    apiClient.get<PagedResult<Warehouse>>('/warehouses', { params: query, signal }).then((r) => r.data),
  all: (signal?: AbortSignal) =>
    apiClient.get<PagedResult<Warehouse>>('/warehouses', { params: { page: 1, pageSize: 100 }, signal }).then((r) => r.data.items),
  create: (request: WarehouseRequest) => apiClient.post<Warehouse>('/warehouses', request).then((r) => r.data),
  update: (id: number, request: WarehouseRequest) => apiClient.put<Warehouse>(`/warehouses/${id}`, request).then((r) => r.data),
  remove: (id: number) => apiClient.delete(`/warehouses/${id}`),
}
