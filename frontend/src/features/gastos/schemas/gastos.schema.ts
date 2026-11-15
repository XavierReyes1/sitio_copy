import { z } from 'zod'
import { hoyHonduras } from '../../../core/utils/formato'

// Espejo de GastosValidators.cs (FluentValidation) del backend.

export const TAMANO_MAXIMO_BYTES = 10 * 1024 * 1024 // 10 MB
export const EXTENSIONES_PERMITIDAS = ['.pdf', '.jpg', '.jpeg', '.png']

const extensionDe = (nombre: string): string => {
  const punto = nombre.lastIndexOf('.')
  return punto >= 0 ? nombre.slice(punto).toLowerCase() : ''
}

/** Validación del archivo ANTES de gastar red (el backend revisa también la firma real del contenido). */
export const archivoComprobanteSchema = z
  .instanceof(File)
  .refine((f) => EXTENSIONES_PERMITIDAS.includes(extensionDe(f.name)), 'Solo se permiten archivos PDF, JPG o PNG.')
  .refine((f) => f.size > 0, 'El archivo está vacío.')
  .refine((f) => f.size <= TAMANO_MAXIMO_BYTES, 'El archivo no puede pesar más de 10 MB.')

const CAI_REGEX = /^[0-9A-Fa-f]{6}(-[0-9A-Fa-f]{6}){4}-[0-9A-Fa-f]{2}$/
const DECIMALES_REGEX = /^\d+(\.\d{1,2})?$/

/** Datos de cabecera: todos opcionales ('' = vacío) y viajan como texto en el formulario. */
export const datosComprobanteSchema = z
  .object({
    numeroFactura: z.string().trim().max(30, 'El número de factura no puede exceder 30 caracteres.'),
    cai: z
      .string()
      .trim()
      .refine(
        (v) => v === '' || CAI_REGEX.test(v),
        'El CAI debe tener el formato XXXXXX-XXXXXX-XXXXXX-XXXXXX-XXXXXX-XX.',
      ),
    proveedorRTN: z
      .string()
      .trim()
      .refine((v) => v === '' || /^\d{14}$/.test(v), 'El RTN del proveedor debe tener 14 dígitos, sin guiones.'),
    fechaEmision: z
      .string()
      .refine((v) => v === '' || v <= hoyHonduras(), 'La fecha de emisión no puede estar en el futuro.'),
    montoTotal: z
      .string()
      .trim()
      .refine((v) => v === '' || DECIMALES_REGEX.test(v), 'Usa un número válido con máximo 2 decimales.')
      .refine((v) => v === '' || !DECIMALES_REGEX.test(v) || Number(v) > 0, 'El monto total debe ser mayor que cero.'),
    isv: z
      .string()
      .trim()
      .refine((v) => v === '' || DECIMALES_REGEX.test(v), 'Usa un número válido con máximo 2 decimales.'),
  })
  .refine(
    (d) =>
      d.isv === '' ||
      d.montoTotal === '' ||
      !DECIMALES_REGEX.test(d.isv) ||
      !DECIMALES_REGEX.test(d.montoTotal) ||
      Number(d.isv) <= Number(d.montoTotal),
    { path: ['isv'], message: 'El ISV no puede ser mayor que el monto total.' },
  )

export type DatosComprobanteFormValues = z.infer<typeof datosComprobanteSchema>

export const liquidarLoteSchema = z.object({
  descripcion: z.string().trim().max(200, 'La descripción no puede exceder 200 caracteres.'),
})

export type LiquidarLoteFormValues = z.infer<typeof liquidarLoteSchema>
