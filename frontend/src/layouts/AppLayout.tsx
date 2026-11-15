import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import type { Rol } from '../core/auth/roles'
import { Badge } from '../core/ui'
import { useAuth } from '../features/auth/hooks/useAuth'

interface ItemMenu {
  ruta: string
  etiqueta: string
  icono: string
  roles: Rol[]
}

const MENU: ItemMenu[] = [
  { ruta: '/album', etiqueta: 'Mi álbum', icono: '🗂️', roles: ['Empleado', 'Admin'] },
  { ruta: '/lotes', etiqueta: 'Lotes liquidados', icono: '🚦', roles: ['Empleado', 'Admin', 'Auditor'] },
  { ruta: '/politicas', etiqueta: 'Política de gastos', icono: '📜', roles: ['Admin', 'Auditor'] },
  { ruta: '/administracion', etiqueta: 'Equipo y puestos', icono: '👥', roles: ['Admin'] },
]

const claseEnlace = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-3 whitespace-nowrap rounded-lg border px-3 py-2.5 text-sm font-medium transition-colors ${
    isActive
      ? 'border-indigo-500/20 bg-indigo-600/15 text-indigo-400'
      : 'border-transparent text-slate-400 hover:bg-slate-900 hover:text-white'
  }`

const colorRol: Record<Rol, string> = {
  Admin: 'border-indigo-500/30 bg-indigo-500/10 text-indigo-300',
  Empleado: 'border-emerald-500/30 bg-emerald-500/10 text-emerald-300',
  Auditor: 'border-amber-500/30 bg-amber-500/10 text-amber-300',
}

export function AppLayout() {
  const { usuario, tieneRol, cerrarSesion } = useAuth()
  const navigate = useNavigate()

  const items = MENU.filter((item) => tieneRol(...item.roles))

  const salir = () => {
    cerrarSesion()
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex min-h-screen flex-col bg-slate-900 text-slate-100 md:flex-row">
      {/* Menú lateral (escritorio) */}
      <aside className="hidden shrink-0 flex-col justify-between border-r border-slate-800 bg-slate-950 p-5 md:flex md:w-64">
        <div>
          <div className="mb-8 flex items-center gap-3 px-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 font-bold text-white shadow">
              SA
            </div>
            <span className="text-xl font-bold tracking-tight text-white">SpendAudit</span>
          </div>
          <nav className="space-y-2" aria-label="Navegación principal">
            {items.map((item) => (
              <NavLink key={item.ruta} to={item.ruta} className={claseEnlace}>
                <span>{item.icono}</span> {item.etiqueta}
              </NavLink>
            ))}
          </nav>
        </div>

        {usuario && (
          <div className="space-y-3 rounded-xl border border-slate-800 bg-slate-900 p-3.5">
            <div>
              <p className="truncate text-sm font-semibold text-white">{usuario.nombreCompleto}</p>
              <p className="truncate text-xs text-slate-400">{usuario.email}</p>
              {usuario.puesto && <p className="truncate text-xs text-slate-500">{usuario.puesto}</p>}
            </div>
            <div className="flex items-center justify-between">
              <Badge className={colorRol[usuario.rol]}>{usuario.rol}</Badge>
              <button
                type="button"
                onClick={salir}
                className="cursor-pointer text-xs font-medium text-slate-400 hover:text-red-400"
              >
                Cerrar sesión
              </button>
            </div>
          </div>
        )}
      </aside>

      {/* Cabecera móvil */}
      <header className="border-b border-slate-800 bg-slate-950 p-3 md:hidden">
        <div className="mb-3 flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="flex h-7 w-7 items-center justify-center rounded-md bg-indigo-600 text-sm font-bold text-white">
              SA
            </div>
            <span className="text-lg font-bold text-white">SpendAudit</span>
          </div>
          <button type="button" onClick={salir} className="cursor-pointer text-xs font-medium text-slate-400">
            Salir
          </button>
        </div>
        <nav className="flex gap-2 overflow-x-auto" aria-label="Navegación principal">
          {items.map((item) => (
            <NavLink key={item.ruta} to={item.ruta} className={claseEnlace}>
              <span>{item.icono}</span> {item.etiqueta}
            </NavLink>
          ))}
        </nav>
      </header>

      {/* Área central: aquí se inyecta la vista de cada feature */}
      <main className="min-w-0 flex-1 overflow-y-auto p-5 md:p-10">
        <Outlet />
      </main>
    </div>
  )
}
