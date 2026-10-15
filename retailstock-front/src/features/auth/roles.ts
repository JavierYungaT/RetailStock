import type { Role } from '../../api/types'

/** Pantalla de inicio de cada rol (a dónde ir después del login o al entrar a "/"). */
export const homeByRole: Record<Role, string> = {
  Admin: '/dashboard',
  Cashier: '/sales/new',
  Warehouse: '/products',
}

export const roleLabels: Record<Role, string> = {
  Admin: 'Administrador',
  Cashier: 'Cajero',
  Warehouse: 'Bodega',
}
