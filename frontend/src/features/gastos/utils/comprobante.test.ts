import { describe, expect, it } from 'vitest'
import type { Comprobante } from '../types/gastos.types'
import { aDtoActualizacion, aValoresFormulario, camposFaltantes } from './comprobante'

const base: Comprobante = {
  id: '1',
  tipoArchivo: 'Pdf',
  nombreArchivoOriginal: 'f.pdf',
  urlArchivo: '/api/v1/gastos/1/archivo',
  numeroFactura: '000-001-01-00000123',
  cai: null,
  proveedorRTN: '08019999000011',
  fechaEmision: '2026-11-05',
  montoTotal: 450,
  isv: null,
  estado: 'Borrador',
  resultadoAuditoria: null,
  fechaCarga: '2026-11-08T10:00:00Z',
  items: [],
  hallazgos: [],
}

describe('camposFaltantes', () => {
  it('no reporta nada si están los 4 datos que exige el backend para liquidar', () => {
    expect(camposFaltantes(base)).toEqual([])
  })

  it('reporta cada dato ausente', () => {
    expect(
      camposFaltantes({ ...base, numeroFactura: null, proveedorRTN: null, fechaEmision: null, montoTotal: null }),
    ).toEqual(['número de factura', 'RTN del proveedor', 'fecha de emisión', 'monto total'])
  })

  it('un monto de 0 cuenta como presente (solo null es ausencia)', () => {
    expect(camposFaltantes({ ...base, montoTotal: 0 })).toEqual([])
  })
})

describe('conversión formulario ↔ DTO', () => {
  it('convierte nulos a texto vacío para el formulario', () => {
    expect(aValoresFormulario(base)).toEqual({
      numeroFactura: '000-001-01-00000123',
      cai: '',
      proveedorRTN: '08019999000011',
      fechaEmision: '2026-11-05',
      montoTotal: '450',
      isv: '',
    })
  })

  it('convierte vacíos a null y textos numéricos a number para el PUT', () => {
    expect(
      aDtoActualizacion({
        numeroFactura: ' 000-001-01-00000123 ',
        cai: '',
        proveedorRTN: '',
        fechaEmision: '2026-11-05',
        montoTotal: '450.50',
        isv: '',
      }),
    ).toEqual({
      numeroFactura: '000-001-01-00000123',
      cai: null,
      proveedorRTN: null,
      fechaEmision: '2026-11-05',
      montoTotal: 450.5,
      isv: null,
    })
  })
})
