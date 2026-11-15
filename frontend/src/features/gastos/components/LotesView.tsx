import { Link } from 'react-router-dom'
import { Alert, Cargando, EstadoVacio, PageHeader } from '../../../core/ui'
import { formatearFechaHora } from '../../../core/utils/formato'
import { useAuth } from '../../auth/hooks/useAuth'
import { useLotes } from '../hooks/useGastos'
import { ResumenSemaforo } from './SemaforoBadge'

export function LotesView() {
  const { tieneRol } = useAuth()
  const { datos: lotes, cargando, error } = useLotes()
  const veTodos = tieneRol('Admin', 'Auditor')

  return (
    <div>
      <PageHeader
        titulo="Lotes liquidados"
        descripcion={
          veTodos
            ? 'Todos los lotes de tu empresa, con el semáforo de cada comprobante.'
            : 'Tus lotes liquidados, con el semáforo de cada comprobante.'
        }
      />

      {cargando && <Cargando mensaje="Consultando lotes…" />}
      {error && !cargando && <Alert titulo="Error de comunicación:">{error}</Alert>}

      {!cargando && !error && lotes.length === 0 && (
        <EstadoVacio icono="🚦" titulo="Aún no hay lotes liquidados">
          Cuando liquides comprobantes desde tu álbum aparecerán aquí.
        </EstadoVacio>
      )}

      {lotes.length > 0 && (
        <div className="overflow-x-auto rounded-xl border border-slate-700 bg-slate-800/60">
          <table className="w-full min-w-[40rem] text-left text-sm">
            <thead className="border-b border-slate-700 text-xs uppercase tracking-wide text-slate-400">
              <tr>
                <th className="px-4 py-3">Lote</th>
                {veTodos && <th className="px-4 py-3">Empleado</th>}
                <th className="px-4 py-3">Fecha</th>
                <th className="px-4 py-3">Comprobantes</th>
                <th className="px-4 py-3">Semáforo</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800">
              {lotes.map((l) => (
                <tr key={l.id} className="hover:bg-slate-800">
                  <td className="px-4 py-3 font-medium text-white">{l.descripcion}</td>
                  {veTodos && <td className="px-4 py-3 text-slate-300">{l.usuario}</td>}
                  <td className="px-4 py-3 text-slate-400">{formatearFechaHora(l.fechaLiquidacion)}</td>
                  <td className="px-4 py-3 text-slate-300">{l.totalComprobantes}</td>
                  <td className="px-4 py-3">
                    <ResumenSemaforo verdes={l.totalVerdes} amarillos={l.totalAmarillos} rojos={l.totalRojos} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link to={`/lotes/${l.id}`} className="font-medium text-indigo-400 hover:text-indigo-300">
                      Ver detalle →
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
