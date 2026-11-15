import type { ActualizarBorradorRequest, Comprobante } from '../types/gastos.types'
import type { DatosComprobanteFormValues } from '../schemas/gastos.schema'

/** Datos mínimos que el backend exige para poder auditar un comprobante al liquidar. */
export const camposFaltantes = (c: Comprobante): string[] => {
  const faltantes: string[] = []
  if (!c.numeroFactura) faltantes.push('número de factura')
  if (!c.proveedorRTN) faltantes.push('RTN del proveedor')
  if (!c.fechaEmision) faltantes.push('fecha de emisión')
  if (c.montoTotal === null) faltantes.push('monto total')
  return faltantes
}

export const aValoresFormulario = (c: Comprobante): DatosComprobanteFormValues => ({
  numeroFactura: c.numeroFactura ?? '',
  cai: c.cai ?? '',
  proveedorRTN: c.proveedorRTN ?? '',
  fechaEmision: c.fechaEmision ?? '',
  montoTotal: c.montoTotal === null ? '' : String(c.montoTotal),
  isv: c.isv === null ? '' : String(c.isv),
})

const textoONulo = (v: string): string | null => (v.trim() === '' ? null : v.trim())
const numeroONulo = (v: string): number | null => (v.trim() === '' ? null : Number(v))

export const aDtoActualizacion = (v: DatosComprobanteFormValues): ActualizarBorradorRequest => ({
  numeroFactura: textoONulo(v.numeroFactura),
  cai: textoONulo(v.cai),
  proveedorRTN: textoONulo(v.proveedorRTN),
  fechaEmision: textoONulo(v.fechaEmision),
  montoTotal: numeroONulo(v.montoTotal),
  isv: numeroONulo(v.isv),
})
