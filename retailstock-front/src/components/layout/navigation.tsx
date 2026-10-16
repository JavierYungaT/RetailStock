import { ArrowLeftRight, Boxes, LayoutDashboard, Package, Receipt, ShoppingCart, Tags, Users, Warehouse } from 'lucide-react'
import type { ReactNode } from 'react'
import type { Role } from '../../api/types'

export interface NavItem {
  to: string
  label: string
  icon: ReactNode
  roles: Role[]
  /** Palabras extra para la paleta de comandos */
  keywords?: string
}

export interface NavGroup {
  label: string
  items: NavItem[]
}

/** Menú lateral. Cada rol ve solo lo que puede usar (la API igual lo valida con [Authorize]). */
export const navigation: NavGroup[] = [
  {
    label: 'General',
    items: [{ to: '/dashboard', label: 'Dashboard', icon: <LayoutDashboard />, roles: ['Admin'], keywords: 'inicio resumen kpi reportes' }],
  },
  {
    label: 'Ventas',
    items: [
      { to: '/sales/new', label: 'Punto de venta', icon: <ShoppingCart />, roles: ['Admin', 'Cashier'], keywords: 'nueva venta caja pos facturar' },
      { to: '/sales', label: 'Historial de ventas', icon: <Receipt />, roles: ['Admin', 'Cashier'], keywords: 'facturas sap' },
    ],
  },
  {
    label: 'Inventario',
    items: [
      { to: '/products', label: 'Productos', icon: <Package />, roles: ['Admin', 'Warehouse', 'Cashier'], keywords: 'catálogo precios' },
      { to: '/inventory/stock', label: 'Existencias', icon: <Boxes />, roles: ['Admin', 'Warehouse', 'Cashier'], keywords: 'stock bodega' },
      { to: '/inventory/movements', label: 'Movimientos', icon: <ArrowLeftRight />, roles: ['Admin', 'Warehouse'], keywords: 'kardex ingreso egreso transferencia' },
    ],
  },
  {
    label: 'Configuración',
    items: [
      { to: '/categories', label: 'Categorías', icon: <Tags />, roles: ['Admin'] },
      { to: '/warehouses', label: 'Bodegas', icon: <Warehouse />, roles: ['Admin'], keywords: 'sucursales' },
      { to: '/users', label: 'Usuarios', icon: <Users />, roles: ['Admin'], keywords: 'roles permisos' },
    ],
  },
]

export function navigationFor(role: Role): NavGroup[] {
  return navigation
    .map((group) => ({ ...group, items: group.items.filter((item) => item.roles.includes(role)) }))
    .filter((group) => group.items.length > 0)
}
