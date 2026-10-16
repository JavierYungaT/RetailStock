import { Navigate, Route, Routes } from 'react-router'
import { AppShell } from './components/layout/AppShell'
import { useAuth } from './features/auth/AuthContext'
import { LoginPage } from './features/auth/LoginPage'
import { ProtectedRoute } from './features/auth/ProtectedRoute'
import { homeByRole } from './features/auth/roles'
import { CategoriesPage } from './features/categories/CategoriesPage'
import { DashboardPage } from './features/dashboard/DashboardPage'
import { MovementsPage } from './features/inventory/MovementsPage'
import { StockPage } from './features/inventory/StockPage'
import { ProductsPage } from './features/products/ProductsPage'
import { PosPage } from './features/sales/PosPage'
import { SalesHistoryPage } from './features/sales/SalesHistoryPage'
import { UsersPage } from './features/users/UsersPage'
import { WarehousesPage } from './features/warehouses/WarehousesPage'

/**
 * Rutas. Las anidadas heredan la protección del padre:
 *   ProtectedRoute (¿hay sesión?) -> AppShell (barra lateral + Outlet) -> ProtectedRoute(roles) -> página
 * Los roles son solo UX: la API valida todo con [Authorize].
 */
export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route index element={<HomeRedirect />} />

          <Route element={<ProtectedRoute roles={['Admin']} />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/categories" element={<CategoriesPage />} />
            <Route path="/warehouses" element={<WarehousesPage />} />
            <Route path="/users" element={<UsersPage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['Admin', 'Cashier']} />}>
            <Route path="/sales/new" element={<PosPage />} />
            <Route path="/sales" element={<SalesHistoryPage />} />
          </Route>

          <Route element={<ProtectedRoute roles={['Admin', 'Warehouse']} />}>
            <Route path="/inventory/movements" element={<MovementsPage />} />
          </Route>

          <Route path="/products" element={<ProductsPage />} />
          <Route path="/inventory/stock" element={<StockPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

/** "/" lleva a la pantalla de inicio del rol */
function HomeRedirect() {
  const { user } = useAuth()
  return <Navigate to={user ? homeByRole[user.role] : '/login'} replace />
}
