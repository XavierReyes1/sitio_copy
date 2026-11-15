import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { extraerMensajeErrorBackend } from '../../../core/api/extraerMensajeError'
import { Alert, Button, Cargando, PageHeader } from '../../../core/ui'
import { formatearFecha, formatearFechaHora, formatearMonto } from '../../../core/utils/formato'
import { abrirArchivoEnNuevaPestanaAsync } from '../api/gastosApi'
import { useLote } from '../hooks/useGastos'
import { HallazgosLista } from './HallazgosLista'
import { ResumenSemaforo, SemaforoBadge } from './SemaforoBadge'

export function LoteDetalleView() {
  const { id = '' } = useParams<{ id: string }>()
  const { datos: lote, cargando, error } = useLote(id)
  const [errorArchivo, setErrorArchivo] = useState<string | null>(null)

  const verArchivo = async (comprobanteId: string) => {
    try {
      setErrorArchivo(null)
      await abrirArchivoEnNuevaPestanaAsync(comprobanteId)
    } catch (e) {
      setErrorArchivo(extraerMensajeErrorBackend(e))
    }
  }

  return (
    <div>
      <Link to="/lotes" className="mb-4 inline-block text-sm text-indigo-400 hover:text-indigo-300">
        ← Volver a los lotes
      </Link>

      {cargando && <Cargando mensaje="Consultando el lote…" />}
      {error && !cargando && <Alert titulo="No se pudo cargar el lote:">{error}</Alert>}

      {lote && (
        <>
          <PageHeader
            titulo={lote.descripcion}
            descripcion={`Liquidado el ${formatearFechaHora(lote.fechaLiquidacion)}`}
            acciones={
              <ResumenSemaforo verdes={lote.totalVerdes} amarillos={lote.totalAmarillos} rojos={lote.totalRojos} />
            }
          />

          {errorArchivo && (
            <div className="mb-4">
              <Alert>{errorArchivo}</Alert>
            </div>
          )}

          <ul className="space-y-4">
            {lote.comprobantes.map((c) => (
              <li key={c.id} className="rounded-xl border border-slate-700 bg-slate-800 p-5">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                  <div className="min-w-0">
                    <h3 className="truncate text-base font-bold text-white">
                      {c.numeroFactura ?? c.nombreArchivoOriginal}
                    </h3>
                    <p className="mt-1 text-sm text-slate-400">
                      {formatearFecha(c.fechaEmision)} · RTN {c.proveedorRTN ?? '—'} ·{' '}
                      <strong className="text-white">{formatearMonto(c.montoTotal)}</strong>
                    </p>
                  </div>
                  <div className="flex items-center gap-3">
                    <SemaforoBadge resultado={c.resultadoAuditoria} />
                    <Button variante="fantasma" className="px-3 py-2 text-xs" onClick={() => verArchivo(c.id)}>
                      Ver archivo
                    </Button>
                  </div>
                </div>

                <HallazgosLista hallazgos={c.hallazgos} />

                {c.items.some((i) => i.esRestringido) && (
                  <div className="mt-3 text-xs text-red-300">
                    Artículos restringidos:{' '}
                    {c.items
                      .filter((i) => i.esRestringido)
                      .map((i) => i.descripcion)
                      .join(', ')}
                  </div>
                )}
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  )
}
