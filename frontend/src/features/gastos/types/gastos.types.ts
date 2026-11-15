// Espejo de Application/Features/Gastos/DTOs (backend .NET)

export type EstadoComprobante = 'Borrador' | 'Liquidado'
export type Semaforo = 'Verde' | 'Amarillo' | 'Rojo'
export type TipoArchivo = 'Pdf' | 'Imagen'

export interface Hallazgo {
  regla: 'FacturaDuplicada' | 'TerminoRestringido' | 'FacturaVencida' | 'TopeDiarioExcedido'
  severidad: 'Amarillo' | 'Rojo'
  detalle: string
}

export interface DetalleItem {
  descripcion: string
  subtotal: number
  esRestringido: boolean
}

export interface Comprobante {
  id: string
  tipoArchivo: TipoArchivo
  nombreArchivoOriginal: string
  urlArchivo: string
  numeroFactura: string | null
  cai: string | null
  proveedorRTN: string | null
  fechaEmision: string | null // "yyyy-MM-dd"
  montoTotal: number | null
  isv: number | null
  estado: EstadoComprobante
  resultadoAuditoria: Semaforo | null
  fechaCarga: string
  items: DetalleItem[]
  hallazgos: Hallazgo[]
}

/** PUT /gastos/{id}: reemplaza los datos editables (null limpia el campo). */
export interface ActualizarBorradorRequest {
  numeroFactura: string | null
  cai: string | null
  proveedorRTN: string | null
  fechaEmision: string | null
  montoTotal: number | null
  isv: number | null
}

export interface LiquidarLoteRequest {
  comprobanteIds: string[]
  descripcion: string | null
}

export interface ResultadoLote {
  loteId: string
  descripcion: string
  fechaLiquidacion: string
  totalVerdes: number
  totalAmarillos: number
  totalRojos: number
  comprobantes: Comprobante[]
}

export interface LoteResumen {
  id: string
  descripcion: string
  fechaLiquidacion: string
  usuario: string
  totalComprobantes: number
  totalVerdes: number
  totalAmarillos: number
  totalRojos: number
}
