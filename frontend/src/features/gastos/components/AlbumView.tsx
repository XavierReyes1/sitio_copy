import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { extraerMensajeErrorBackend } from '../../../core/api/extraerMensajeError'
import { Alert, Button, Cargando, EstadoVacio, Modal, PageHeader } from '../../../core/ui'
import {
  abrirArchivoEnNuevaPestanaAsync,
  eliminarBorradorAsync,
} from '../api/gastosApi'
import { useAlbum } from '../hooks/useGastos'
import type { Comprobante } from '../types/gastos.types'
import { ComprobanteCard } from './ComprobanteCard'
import { EditarComprobanteModal } from './EditarComprobanteModal'
import { LiquidarLoteModal } from './LiquidarLoteModal'
import { ZonaCarga } from './ZonaCarga'

export function AlbumView() {
  const navigate = useNavigate()
  const { datos: album, setDatos: setAlbum, cargando, error } = useAlbum()

  const [seleccion, setSeleccion] = useState<Set<string>>(new Set())
  const [editando, setEditando] = useState<Comprobante | null>(null)
  const [eliminando, setEliminando] = useState<Comprobante | null>(null)
  const [errorEliminar, setErrorEliminar] = useState<string | null>(null)
  const [borrando, setBorrando] = useState<boolean>(false)
  const [liquidando, setLiquidando] = useState<boolean>(false)
  const [errorAccion, setErrorAccion] = useState<string | null>(null)

  const seleccionados = album.filter((c) => seleccion.has(c.id))

  // Actualizaciones inmutables (spread / map / filter): React detecta el nuevo puntero y re-renderiza
  const alSubir = (nuevo: Comprobante) => setAlbum((actual) => [nuevo, ...actual])

  const alSeleccionar = (id: string, marcado: boolean) =>
    setSeleccion((actual) => {
      const siguiente = new Set(actual)
      if (marcado) siguiente.add(id)
      else siguiente.delete(id)
      return siguiente
    })

  const alGuardarEdicion = (actualizado: Comprobante) => {
    setAlbum((actual) => actual.map((c) => (c.id === actualizado.id ? actualizado : c)))
    setEditando(null)
  }

  const confirmarEliminacion = async () => {
    if (!eliminando) return
    try {
      setBorrando(true)
      setErrorEliminar(null)
      await eliminarBorradorAsync(eliminando.id)
      setAlbum((actual) => actual.filter((c) => c.id !== eliminando.id))
      setSeleccion((actual) => {
        const siguiente = new Set(actual)
        siguiente.delete(eliminando.id)
        return siguiente
      })
      setEliminando(null)
    } catch (e) {
      setErrorEliminar(extraerMensajeErrorBackend(e))
    } finally {
      setBorrando(false)
    }
  }

  const verArchivo = async (c: Comprobante) => {
    try {
      setErrorAccion(null)
      await abrirArchivoEnNuevaPestanaAsync(c.id)
    } catch (e) {
      setErrorAccion(extraerMensajeErrorBackend(e))
    }
  }

  return (
    <div>
      <PageHeader
        titulo="Mi álbum de comprobantes"
        descripcion="Acumula tus facturas durante el viaje y, al terminar, liquídalas en un lote para que se auditen."
        acciones={
          <Button disabled={seleccionados.length === 0} onClick={() => setLiquidando(true)}>
            🚦 Liquidar lote{seleccionados.length > 0 ? ` (${seleccionados.length})` : ''}
          </Button>
        }
      />

      <div className="mb-8">
        <ZonaCarga onSubido={alSubir} />
      </div>

      {errorAccion && (
        <div className="mb-4">
          <Alert>{errorAccion}</Alert>
        </div>
      )}

      {cargando && <Cargando mensaje="Consultando tu álbum…" />}
      {error && !cargando && <Alert titulo="Error de comunicación:">{error}</Alert>}

      {!cargando && !error && album.length === 0 && (
        <EstadoVacio icono="🗂️" titulo="Tu álbum está vacío">
          Sube tu primera factura con la zona de arriba.
        </EstadoVacio>
      )}

      {!cargando && !error && album.length > 0 && (
        <>
          <p className="mb-4 text-xs text-slate-400">
            <strong className="text-white">{album.length}</strong> borradores ·{' '}
            <strong className="text-white">{seleccionados.length}</strong> seleccionados
          </p>
          <section className="grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-3">
            {album.map((c) => (
              <ComprobanteCard
                key={c.id}
                comprobante={c}
                seleccionado={seleccion.has(c.id)}
                onSeleccionar={alSeleccionar}
                onEditar={setEditando}
                onEliminar={(x) => {
                  setErrorEliminar(null)
                  setEliminando(x)
                }}
                onVerArchivo={verArchivo}
              />
            ))}
          </section>
        </>
      )}

      {editando && (
        <EditarComprobanteModal
          key={editando.id}
          comprobante={editando}
          onCerrar={() => setEditando(null)}
          onGuardado={alGuardarEdicion}
        />
      )}

      {liquidando && (
        <LiquidarLoteModal
          comprobantes={seleccionados}
          onCerrar={() => setLiquidando(false)}
          onLiquidado={(resultado) => navigate(`/lotes/${resultado.loteId}`)}
        />
      )}

      <Modal abierto={eliminando !== null} titulo="Eliminar comprobante" onCerrar={() => setEliminando(null)}>
        <p className="mb-4 text-sm text-slate-300">
          ¿Eliminar{' '}
          <strong className="text-white">{eliminando?.numeroFactura ?? eliminando?.nombreArchivoOriginal}</strong>? Se
          borrará también el archivo y no se puede deshacer.
        </p>
        {errorEliminar && (
          <div className="mb-4">
            <Alert titulo="Respuesta del servidor:">{errorEliminar}</Alert>
          </div>
        )}
        <div className="flex justify-end gap-2">
          <Button variante="fantasma" onClick={() => setEliminando(null)} disabled={borrando}>
            Cancelar
          </Button>
          <Button variante="peligro" onClick={confirmarEliminacion} cargando={borrando}>
            Eliminar
          </Button>
        </div>
      </Modal>
    </div>
  )
}
