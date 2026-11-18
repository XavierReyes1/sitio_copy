import { lazy, Suspense } from 'react'
import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { AppLayout } from '../layouts/AppLayout'
import { RedireccionInicial, RutaPorRol, RutaProtegida, RutaPublica } from './guards'

const LoginView = lazy(() =>
  import('../features/auth/components/LoginView').then((modulo) => ({ default: modulo.LoginView })),
)
const RegistroEmpresaView = lazy(() =>
  import('../features/auth/components/RegistroEmpresaView').then((modulo) => ({
    default: modulo.RegistroEmpresaView,
  })),
)
const AlbumView = lazy(() =>
  import('../features/gastos/components/AlbumView').then((modulo) => ({ default: modulo.AlbumView })),
)
const LotesView = lazy(() =>
  import('../features/gastos/components/LotesView').then((modulo) => ({ default: modulo.LotesView })),
)
const LoteDetalleView = lazy(() =>
  import('../features/gastos/components/LoteDetalleView').then((modulo) => ({
    default: modulo.LoteDetalleView,
  })),
)
const PoliticaView = lazy(() =>
  import('../features/politicas/components/PoliticaView').then((modulo) => ({
    default: modulo.PoliticaView,
  })),
)
const AdministracionView = lazy(() =>
  import('../features/usuarios/components/AdministracionView').then((modulo) => ({
    default: modulo.AdministracionView,
  })),
)

function NoEncontrado() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-slate-900 p-6 text-center text-slate-100">
      <p className="text-5xl">🧭</p>
      <h1 className="mt-3 text-2xl font-bold">Página no encontrada</h1>
      <Link to="/" className="mt-4 text-indigo-400 hover:text-indigo-300">
        Volver al inicio
      </Link>
    </div>
  )
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <Suspense
        fallback={
          <div
            role="status"
            className="flex min-h-screen items-center justify-center bg-slate-900 text-lg font-semibold text-indigo-400"
          >
            Cargando página...
          </div>
        }
      >
        <Routes>
          {/* Públicas */}
          <Route element={<RutaPublica />}>
            <Route path="/login" element={<LoginView />} />
            <Route path="/registro" element={<RegistroEmpresaView />} />
          </Route>

          {/* Protegidas por JWT */}
          <Route element={<RutaProtegida />}>
            <Route element={<AppLayout />}>
              <Route index element={<RedireccionInicial />} />

              <Route element={<RutaPorRol roles={['Empleado', 'Admin']} />}>
                <Route path="/album" element={<AlbumView />} />
              </Route>

              <Route path="/lotes" element={<LotesView />} />
              <Route path="/lotes/:id" element={<LoteDetalleView />} />

              <Route element={<RutaPorRol roles={['Admin', 'Auditor']} />}>
                <Route path="/politicas" element={<PoliticaView />} />
              </Route>

              <Route element={<RutaPorRol roles={['Admin']} />}>
                <Route path="/administracion" element={<AdministracionView />} />
              </Route>
            </Route>
          </Route>

          <Route path="*" element={<NoEncontrado />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
