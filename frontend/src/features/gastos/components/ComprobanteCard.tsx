import { Badge, Button } from '../../../core/ui'
import { formatearFecha, formatearFechaHora, formatearMonto } from '../../../core/utils/formato'
import type { Comprobante } from '../types/gastos.types'
import { camposFaltantes } from '../utils/comprobante'

interface ComprobanteCardProps {
  comprobante: Comprobante
  seleccionado: boolean
  onSeleccionar: (id: string, seleccionado: boolean) => void
  onEditar: (comprobante: Comprobante) => void
  onEliminar: (comprobante: Comprobante) => void
  onVerArchivo: (comprobante: Comprobante) => void
}

export function ComprobanteCard({
  comprobante: c,
  seleccionado,
  onSeleccionar,
  onEditar,
  onEliminar,
  onVerArchivo,
}: ComprobanteCardProps) {
  const faltantes = camposFaltantes(c)
  const incompleto = faltantes.length > 0

  return (
    <article
      className={`flex flex-col justify-between rounded-xl border bg-slate-800 p-5 shadow-sm transition-colors ${
        seleccionado ? 'border-indigo-500' : 'border-slate-700 hover:border-slate-500'
      }`}
    >
      <div>
        <div className="mb-3 flex items-start justify-between gap-3">
          <label className={`flex items-center gap-2 text-xs ${incompleto ? 'cursor-not-allowed opacity-60' : 'cursor-pointer'}`}>
            <input
              type="checkbox"
              checked={seleccionado}
              disabled={incompleto}
              onChange={(e) => onSeleccionar(c.id, e.target.checked)}
              className="h-4 w-4 accent-indigo-500"
            />
            <span className="text-slate-300">Incluir en el lote</span>
          </label>
          <Badge className="border-slate-600 bg-slate-700/40 text-slate-300">
            {c.tipoArchivo === 'Pdf' ? '📄 PDF' : '🖼️ Imagen'}
          </Badge>
        </div>

        <h3 className="truncate text-base font-bold text-white" title={c.nombreArchivoOriginal}>
          {c.numeroFactura ?? c.nombreArchivoOriginal}
        </h3>
        <p className="mb-4 text-xs text-slate-500">Cargado el {formatearFechaHora(c.fechaCarga)}</p>

        <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
          <div>
            <dt className="text-xs text-slate-500">Fecha de emisión</dt>
            <dd className="text-slate-200">{formatearFecha(c.fechaEmision)}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">Monto total</dt>
            <dd className="font-semibold text-white">{formatearMonto(c.montoTotal)}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">RTN proveedor</dt>
            <dd className="truncate text-slate-200">{c.proveedorRTN ?? '—'}</dd>
          </div>
          <div>
            <dt className="text-xs text-slate-500">ISV</dt>
            <dd className="text-slate-200">{formatearMonto(c.isv)}</dd>
          </div>
        </dl>

        {incompleto && (
          <p className="mt-4 rounded-lg border border-amber-500/30 bg-amber-950/30 px-3 py-2 text-xs text-amber-300">
            ⚠️ Completa para poder liquidar: {faltantes.join(', ')}.
          </p>
        )}
      </div>

      <div className="mt-5 flex flex-wrap gap-2">
        <Button variante="fantasma" className="px-3 py-2 text-xs" onClick={() => onVerArchivo(c)}>
          Ver archivo
        </Button>
        <Button variante="secundario" className="px-3 py-2 text-xs" onClick={() => onEditar(c)}>
          Editar datos
        </Button>
        <Button variante="fantasma" className="px-3 py-2 text-xs text-red-300" onClick={() => onEliminar(c)}>
          Eliminar
        </Button>
      </div>
    </article>
  )
}
