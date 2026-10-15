import { apiClient } from '../../api/client'
import type { PagedResult } from '../../api/types'

export interface Product {
  id: number
  code: string
  name: string
  categoryId: number
  categoryName: string
  price: number
  isActive: boolean
}

export interface Category {
  id: number
  name: string
}

export interface ProductQuery {
  page: number
  pageSize: number
  name?: string
  includeInactive?: boolean
}

export interface CreateProductRequest {
  code: string
  name: string
  categoryId: number
  price: number
}

export interface UpdateProductRequest {
  name: string
  categoryId: number
  price: number
  isActive: boolean
}

export const productsApi = {
  // axios convierte "params" en query string: /products?page=1&pageSize=10&name=arroz
  list: (query: ProductQuery, signal?: AbortSignal) =>
    apiClient.get<PagedResult<Product>>('/products', { params: query, signal }).then((r) => r.data),

  create: (request: CreateProductRequest) => apiClient.post<Product>('/products', request).then((r) => r.data),

  update: (id: number, request: UpdateProductRequest) =>
    apiClient.put<Product>(`/products/${id}`, request).then((r) => r.data),

  categories: (signal?: AbortSignal) =>
    apiClient
      .get<PagedResult<Category>>('/categories', { params: { page: 1, pageSize: 100 }, signal })
      .then((r) => r.data.items),
}
