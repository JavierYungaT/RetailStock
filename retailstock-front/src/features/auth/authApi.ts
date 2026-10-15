import { apiClient } from '../../api/client'
import type { Role } from '../../api/types'

export interface LoginRequest {
  email: string
  password: string
}

/** LoginResponse de la API */
export interface LoginResponse {
  token: string
  tokenType: string
  expiresAtUtc: string
  email: string
  role: Role
}

export const authApi = {
  login: (request: LoginRequest) => apiClient.post<LoginResponse>('/auth/login', request).then((r) => r.data),
}
