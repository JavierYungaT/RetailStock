import { apiClient } from '../../api/client'
import type { PagedResult, Role } from '../../api/types'

export interface User { id: number; email: string; role: Role; isActive: boolean }

export interface UserQuery { page: number; pageSize: number; name?: string; role?: Role; isActive?: boolean }

export const usersApi = {
  list: (query: UserQuery, signal?: AbortSignal) =>
    apiClient.get<PagedResult<User>>('/users', { params: query, signal }).then((r) => r.data),
  create: (request: { email: string; password: string; role: Role }) => apiClient.post<User>('/users', request).then((r) => r.data),
  update: (id: number, request: { role: Role; isActive: boolean }) => apiClient.put<User>(`/users/${id}`, request).then((r) => r.data),
  resetPassword: (id: number, newPassword: string) => apiClient.put(`/users/${id}/password`, { newPassword }),
}
