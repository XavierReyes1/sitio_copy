import { useCallback } from 'react'
import { useCargaDatos } from '../../../core/hooks/useCargaDatos'
import { obtenerAlbumAsync, obtenerLoteAsync, obtenerLotesAsync } from '../api/gastosApi'
import type { Comprobante, LoteResumen, ResultadoLote } from '../types/gastos.types'

export const useAlbum = () => useCargaDatos<Comprobante[]>(obtenerAlbumAsync, [])

export const useLotes = () => useCargaDatos<LoteResumen[]>(obtenerLotesAsync, [])

export function useLote(id: string) {
  const cargar = useCallback(() => obtenerLoteAsync(id), [id])
  return useCargaDatos<ResultadoLote | null>(cargar, null)
}
