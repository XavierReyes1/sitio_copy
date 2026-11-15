import { useRef, useState } from 'react'
import type { DragEvent } from 'react'
import { extraerMensajeErrorBackend } from '../../../core/api/extraerMensajeError'
import { subirBorradorAsync } from '../api/gastosApi'
import { extraerTextoImagenAsync } from '../ocr/extraerTextoImagen'
import { archivoComprobanteSchema, EXTENSIONES_PERMITIDAS } from '../schemas/gastos.schema'
import type { Comprobante } from '../types/gastos.types'

type EstadoArchivo = 'subiendo' | 'ok' | 'aviso' | 'error'

interface ArchivoEnCurso {
  clave: string
  nombre: string
  estado: EstadoArchivo
  mensaje?: string
}

interface ZonaCargaProps {
  /** Se llama por cada comprobante creado, para agregarlo al álbum sin otro GET. */
  onSubido: (comprobante: Comprobante) => void
}

export function ZonaCarga({ onSubido }: ZonaCargaProps) {
  const entradaArchivo = useRef<HTMLInputElement>(null)
  const [arrastrando, setArrastrando] = useState<boolean>(false)
  const [archivos, setArchivos] = useState<ArchivoEnCurso[]>([])

  const actualizar = (clave: string, cambios: Partial<ArchivoEnCurso>) =>
    setArchivos((actuales) => actuales.map((a) => (a.clave === clave ? { ...a, ...cambios } : a)))

  const procesar = async (lista: FileList | File[]) => {
    for (const archivo of Array.from(lista)) {
      const clave = `${archivo.name}-${archivo.size}-${crypto.randomUUID()}`
      setArchivos((actuales) => [{ clave, nombre: archivo.name, estado: 'subiendo' }, ...actuales])

      // Filtro sintáctico (Zod): no se gasta red con archivos inválidos
      const validacion = archivoComprobanteSchema.safeParse(archivo)
      if (!validacion.success) {
        actualizar(clave, { estado: 'error', mensaje: validacion.error.issues[0]?.message })
        continue
      }

      try {
        let textoOcr: string | undefined
        let aviso: string | undefined
        const esImagen =
          archivo.type.startsWith('image/') || /\.(jpe?g|png)$/i.test(archivo.name)

        if (esImagen) {
          try {
            textoOcr = await extraerTextoImagenAsync(archivo)
            if (!textoOcr) aviso = 'No se detectó texto; revisa y completa los campos manualmente.'
          } catch (error) {
            const detalle = error instanceof Error ? ` (${error.message})` : ''
            aviso = `No se pudo ejecutar el OCR${detalle}; completa los campos manualmente.`
          }
        }

        const creado = await subirBorradorAsync(archivo, textoOcr)
        actualizar(clave, aviso ? { estado: 'aviso', mensaje: aviso } : { estado: 'ok' })
        onSubido(creado)
      } catch (error) {
        // Filtro semántico (backend): firma real del archivo, límites, sesión…
        actualizar(clave, { estado: 'error', mensaje: extraerMensajeErrorBackend(error) })
      }
    }
  }

  const alSoltar = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    setArrastrando(false)
    if (e.dataTransfer.files.length > 0) void procesar(e.dataTransfer.files)
  }

  return (
    <section aria-label="Subir comprobantes">
      <div
        onDragOver={(e) => {
          e.preventDefault()
          setArrastrando(true)
        }}
        onDragLeave={() => setArrastrando(false)}
        onDrop={alSoltar}
        className={`rounded-xl border-2 border-dashed p-8 text-center transition-colors ${
          arrastrando ? 'border-indigo-400 bg-indigo-500/10' : 'border-slate-700 bg-slate-800/40'
        }`}
      >
        <div className="mb-2 text-4xl">📎</div>
        <p className="font-semibold text-white">Arrastra tus facturas aquí</p>
        <p className="mt-1 text-sm text-slate-400">
          PDF con texto, JPG o PNG · máximo 10 MB · OCR de imágenes en tu navegador
        </p>
        <button
          type="button"
          onClick={() => entradaArchivo.current?.click()}
          className="mt-4 cursor-pointer rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-indigo-500"
        >
          Seleccionar archivos
        </button>
        <input
          ref={entradaArchivo}
          type="file"
          multiple
          accept={EXTENSIONES_PERMITIDAS.join(',')}
          className="hidden"
          aria-label="Seleccionar archivos"
          onChange={(e) => {
            if (e.target.files && e.target.files.length > 0) void procesar(e.target.files)
            e.target.value = '' // permite volver a elegir el mismo archivo
          }}
        />
      </div>

      {archivos.length > 0 && (
        <ul className="mt-3 space-y-2" aria-live="polite">
          {archivos.slice(0, 6).map((a) => (
            <li
              key={a.clave}
              className="flex items-start gap-2.5 rounded-lg border border-slate-700 bg-slate-800/60 px-3 py-2 text-xs"
            >
              <span>
                {a.estado === 'subiendo' ? '⏳' : a.estado === 'ok' ? '✅' : a.estado === 'aviso' ? '⚠️' : '🛡️'}
              </span>
              <div className="min-w-0">
                <p className="truncate font-medium text-white">{a.nombre}</p>
                {a.estado === 'subiendo' && (
                  <p className="text-slate-400">
                    {/\.(jpe?g|png)$/i.test(a.nombre) ? 'Reconociendo texto y subiendo…' : 'Subiendo y leyendo…'}
                  </p>
                )}
                {a.estado === 'ok' && <p className="text-emerald-300">Agregado al álbum</p>}
                {a.estado === 'aviso' && <p className="text-amber-300">{a.mensaje}</p>}
                {a.estado === 'error' && <p className="text-red-300">{a.mensaje}</p>}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
