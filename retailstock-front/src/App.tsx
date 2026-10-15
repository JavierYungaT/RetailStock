import { Navigate, Route, Routes } from 'react-router'
import { Layout } from './components/Layout'
import { useAuth } from './features/auth/AuthContext'
import { LoginPage } from './features/auth/LoginPage'
import { ProtectedRoute } from './features/auth/ProtectedRoute'
import { homeByRole } from './features/auth/roles'
import { DashboardPage } from './features/dashboard/DashboardPage'
import { ProductsPage } from './features/products/ProductsPage'
import { NewSalePage } from './features/sales/NewSalePage'

/**
 * Tabla de rutas. Las rutas anidadas heredan la protección del padre:
 *   ProtectedRoute (¿hay sesión?) -> Layout (barra + Outlet) -> ProtectedRoute(roles) -> página
 */
export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route index element={<HomeRedirect />} />
          <Route element={<ProtectedRoute roles={['Admin']} />}>
            <Route path="/dashboard" element={<DashboardPage />} />
          </Route>
          <Route element={<ProtectedRoute roles={['Admin', 'Cashier']} />}>
            <Route path="/sales/new" element={<NewSalePage />} />
          </Route>
          <Route path="/products" element={<ProductsPage />} />
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
