import { apiClient } from '../../api/client'
import type { PagedResult } from '../../api/types'

export interface Category { id: number; name: string }

export const categoriesApi = {
  list: (query: { page: number; pageSize: number; name?: string }, signal?: AbortSignal) =>
    apiClient.get<PagedResult<Category>>('/categories', { params: query, signal }).then((r) => r.data),
  /** Todas (para selects): máximo permitido por la API */
  all: (signal?: AbortSignal) =>
    apiClient.get<PagedResult<Category>>('/categories', { params: { page: 1, pageSize: 100 }, signal }).then((r) => r.data.items),
  create: (name: string) => apiClient.post<Category>('/categories', { name }).then((r) => r.data),
  update: (id: number, name: string) => apiClient.put<Category>(`/categories/${id}`, { name }).then((r) => r.data),
  remove: (id: number) => apiClient.delete(`/categories/${id}`),
}
