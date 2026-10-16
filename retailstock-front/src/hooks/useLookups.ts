import { categoriesApi } from '../features/categories/categoriesApi'
import { warehousesApi } from '../features/warehouses/warehousesApi'
import { useApiQuery } from './useApiQuery'

/** Catálogos pequeños para los selects (bodegas, categorías). */
export const useWarehouses = () => useApiQuery((signal) => warehousesApi.all(signal), [])
export const useCategories = () => useApiQuery((signal) => categoriesApi.all(signal), [])
