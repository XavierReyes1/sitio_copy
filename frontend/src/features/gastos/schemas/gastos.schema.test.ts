import { describe, expect, it } from 'vitest'
import { hoyHonduras } from '../../../core/utils/formato'
import { archivoComprobanteSchema, datosComprobanteSchema, liquidarLoteSchema } from './gastos.schema'

const vacio = { numeroFactura: '', cai: '', proveedorRTN: '', fechaEmision: '', montoTotal: '', isv: '' }
const mensajes = (datos: object) => {
  const r = datosComprobanteSchema.safeParse({ ...vacio, ...datos })
  return r.success ? [] : r.error.issues.map((i) => i.message)
}

describe('archivoComprobanteSchema', () => {
  const archivo = (nombre: string, bytes: number) => new File([new Uint8Array(bytes)], nombre)

  it.each(['factura.pdf', 'foto.JPG', 'foto.jpeg', 'scan.png'])('acepta %s', (nombre) => {
    expect(archivoComprobanteSchema.safeParse(archivo(nombre, 100)).success).toBe(true)
  })

  it('rechaza extensiones no permitidas', () => {
    const r = archivoComprobanteSchema.safeParse(archivo('virus.exe', 100))
    expect(r.success).toBe(false)
    expect(r.error?.issues[0]?.message).toBe('Solo se permiten archivos PDF, JPG o PNG.')
  })

  it('rechaza archivos vacíos', () => {
    expect(archivoComprobanteSchema.safeParse(archivo('a.pdf', 0)).success).toBe(false)
  })

  it('acepta exactamente 10 MB y rechaza 1 byte más', () => {
    expect(archivoComprobanteSchema.safeParse(archivo('a.pdf', 10 * 1024 * 1024)).success).toBe(true)
    const r = archivoComprobanteSchema.safeParse(archivo('a.pdf', 10 * 1024 * 1024 + 1))
    expect(r.error?.issues[0]?.message).toBe('El archivo no puede pesar más de 10 MB.')
  })
})

describe('datosComprobanteSchema (espejo de DatosComprobanteValidatorBase)', () => {
  it('acepta todo vacío (los datos son opcionales hasta liquidar)', () => {
    expect(mensajes({})).toEqual([])
  })

  it('acepta una factura completa y válida', () => {
    expect(
      mensajes({
        numeroFactura: '000-001-01-00000123',
        cai: 'a1b2c3-d4e5f6-a7b8c9-d0e1f2-a3b4c5-d6',
        proveedorRTN: '08019999000011',
        fechaEmision: '2026-01-15',
        montoTotal: '450.50',
        isv: '58.76',
      }),
    ).toEqual([])
  })

  it('valida el CAI y el RTN', () => {
    expect(mensajes({ cai: 'ABC' })).toContain(
      'El CAI debe tener el formato XXXXXX-XXXXXX-XXXXXX-XXXXXX-XXXXXX-XX.',
    )
    expect(mensajes({ proveedorRTN: '0801-1999-000011' })).toContain(
      'El RTN del proveedor debe tener 14 dígitos, sin guiones.',
    )
  })

  it('rechaza fechas futuras y acepta hoy', () => {
    expect(mensajes({ fechaEmision: '2999-01-01' })).toContain('La fecha de emisión no puede estar en el futuro.')
    expect(mensajes({ fechaEmision: hoyHonduras() })).toEqual([])
  })

  it('valida el monto: positivo y máximo 2 decimales', () => {
    expect(mensajes({ montoTotal: '0' })).toContain('El monto total debe ser mayor que cero.')
    expect(mensajes({ montoTotal: '10.999' })).toContain('Usa un número válido con máximo 2 decimales.')
    expect(mensajes({ montoTotal: 'abc' })).toContain('Usa un número válido con máximo 2 decimales.')
    expect(mensajes({ montoTotal: '-5' })).toContain('Usa un número válido con máximo 2 decimales.')
  })

  it('el ISV no puede superar el monto total, pero puede ser 0', () => {
    expect(mensajes({ montoTotal: '100', isv: '150' })).toContain('El ISV no puede ser mayor que el monto total.')
    expect(mensajes({ montoTotal: '100', isv: '0' })).toEqual([])
    expect(mensajes({ montoTotal: '100', isv: '100' })).toEqual([])
  })

  it('limita el número de factura a 30 caracteres', () => {
    expect(mensajes({ numeroFactura: 'x'.repeat(31) })).toContain(
      'El número de factura no puede exceder 30 caracteres.',
    )
  })
})

describe('liquidarLoteSchema', () => {
  it('la descripción es opcional y se limita a 200 caracteres', () => {
    expect(liquidarLoteSchema.safeParse({ descripcion: '' }).success).toBe(true)
    expect(liquidarLoteSchema.safeParse({ descripcion: 'x'.repeat(200) }).success).toBe(true)
    expect(liquidarLoteSchema.safeParse({ descripcion: 'x'.repeat(201) }).success).toBe(false)
  })
})
