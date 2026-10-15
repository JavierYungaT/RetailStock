# retailstock-front

Frontend de RetailStock: React 19 + TypeScript + Vite + Tailwind CSS v4 + axios + React Router.

## Arranque

```bash
npm install
npm run dev        # http://localhost:5173 (la API debe estar en http://localhost:5080)
```

Variables (`.env.development`): `VITE_API_URL=http://localhost:5080/api`

## Scripts

| Comando | Qué hace |
|---|---|
| `npm run dev` | Servidor de desarrollo con recarga en caliente |
| `npm run build` | Chequeo de tipos (`tsc -b`) + build de producción en `dist/` |
| `npm run lint` | Linter (oxlint) |
| `npm run preview` | Sirve el build de `dist/` |

## Estructura

```
src/
  api/          cliente axios (interceptores JWT y 401), tipos y errores ProblemDetails
  components/   UI reutilizable (Layout, Button, Modal, Alert, Pagination...)
  features/
    auth/       AuthContext, login, rutas protegidas por rol
    products/   tabla paginada con búsqueda; crear/editar (Admin)
    sales/      nueva venta: carrito, totales con IVA 15%, confirmación
    dashboard/  KPIs del día, gráfico de ventas por día, stock bajo (Admin)
  hooks/        useApiQuery, useDebounce, useElementWidth
  lib/          formato de montos y fechas
```

Usuarios de prueba: `admin@retailstock.com / Admin123!`, `caja@retailstock.com / Cashier123!`, `bodega@retailstock.com / Warehouse123!`.
