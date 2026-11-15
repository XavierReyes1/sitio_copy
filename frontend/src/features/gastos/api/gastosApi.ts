import { apiClient } from '../../../core/api/apiClient'
import type {
  ActualizarBorradorRequest,
  Comprobante,
  LiquidarLoteRequest,
  LoteResumen,
  ResultadoLote,
} from '../types/gastos.types'

export const subirBorradorAsync = async (archivo: File, textoOcr?: string): Promise<Comprobante> => {
  const formulario = new FormData()
  formulario.append('archivo', archivo)
  if (textoOcr?.trim()) formulario.append('textoOcr', textoOcr)
  const respuesta = await apiClient.post<Comprobante>('/gastos/borrador', formulario)
  return respuesta.data
}

export const obtenerAlbumAsync = async (): Promise<Comprobante[]> => {
  const respuesta = await apiClient.get<Comprobante[]>('/gastos/album')
  return respuesta.data
}

export const actualizarBorradorAsync = async (
  id: string,
  dto: ActualizarBorradorRequest,
): Promise<Comprobante> => {
  const respuesta = await apiClient.put<Comprobante>(`/gastos/${id}`, dto)
  return respuesta.data
}

export const eliminarBorradorAsync = async (id: string): Promise<void> => {
  await apiClient.delete(`/gastos/${id}`)
}

export const liquidarLoteAsync = async (dto: LiquidarLoteRequest): Promise<ResultadoLote> => {
  const respuesta = await apiClient.post<ResultadoLote>('/gastos/liquidar-lote', dto)
  return respuesta.data
}

export const obtenerLotesAsync = async (): Promise<LoteResumen[]> => {
  const respuesta = await apiClient.get<LoteResumen[]>('/gastos/lotes')
  return respuesta.data
}

export const obtenerLoteAsync = async (id: string): Promise<ResultadoLote> => {
  const respuesta = await apiClient.get<ResultadoLote>(`/gastos/lotes/${id}`)
  return respuesta.data
}

/**
 * El archivo solo se sirve con autenticación (no hay URLs públicas), así que no basta un <a href>:
 * se descarga con el JWT, se convierte en una URL temporal y se abre en una pestaña nueva.
 */
export const abrirArchivoEnNuevaPestanaAsync = async (id: string): Promise<void> => {
  // La pestaña se abre ANTES del await para que el navegador no la bloquee como pop-up.
  const pestana = window.open('', '_blank')
  try {
    const respuesta = await apiClient.get<Blob>(`/gastos/${id}/archivo`, { responseType: 'blob' })
    const url = URL.createObjectURL(respuesta.data)
    if (pestana) {
      pestana.location.href = url
    } else {
      window.location.href = url
    }
    // Se libera la memoria un rato después, cuando la pestaña ya cargó el archivo.
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (error) {
    pestana?.close()
    throw error
  }
}
