import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { AppLayout } from '../layouts/AppLayout'
import { LoginView } from '../features/auth/components/LoginView'
import { RegistroEmpresaView } from '../features/auth/components/RegistroEmpresaView'
import { AlbumView } from '../features/gastos/components/AlbumView'
import { LotesView } from '../features/gastos/components/LotesView'
import { LoteDetalleView } from '../features/gastos/components/LoteDetalleView'
import { PoliticaView } from '../features/politicas/components/PoliticaView'
import { AdministracionView } from '../features/usuarios/components/AdministracionView'
import { RedireccionInicial, RutaPorRol, RutaProtegida, RutaPublica } from './guards'

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
    </BrowserRouter>
  )
}
