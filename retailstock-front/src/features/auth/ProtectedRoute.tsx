import { Navigate, Outlet, useLocation } from 'react-router'
import type { Role } from '../../api/types'
import { useAuth } from './AuthContext'
import { homeByRole } from './roles'

/**
 * Ruta protegida (como .authenticated() / .hasAnyRole() del SecurityFilterChain, pero en el front).
 *  - Sin sesión -> /login (recordando a dónde quería ir, salvo tras un "Salir" manual).
 *  - Sin el rol -> a su pantalla de inicio.
 * IMPORTANTE: esto solo es UX. La seguridad real la aplica la API ([Authorize]); el front se puede manipular.
 */
export function ProtectedRoute({ roles }: { roles?: Role[] }) {
  const { user, logoutReason } = useAuth()
  const location = useLocation()

  if (!user) {
    // Tras un "Salir" manual no se recuerda la ruta: quien entre después (quizá otro usuario) va a SU inicio.
    // Si la sesión expiró o se entró por URL directa, se vuelve a la misma pantalla después del login.
    const state = logoutReason === 'manual' ? undefined : { from: location.pathname }
    return <Navigate to="/login" replace state={state} />
  }

  if (roles && !roles.includes(user.role)) {
    return <Navigate to={homeByRole[user.role]} replace />
  }

  return <Outlet /> // renderiza la ruta hija
}
