import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { extraerMensajeErrorBackend } from '../../../core/api/extraerMensajeError'
import { Alert, Button, Input, Modal } from '../../../core/ui'
import { actualizarBorradorAsync } from '../api/gastosApi'
import { datosComprobanteSchema } from '../schemas/gastos.schema'
import type { DatosComprobanteFormValues } from '../schemas/gastos.schema'
import type { Comprobante } from '../types/gastos.types'
import { aDtoActualizacion, aValoresFormulario } from '../utils/comprobante'

interface EditarComprobanteModalProps {
  comprobante: Comprobante
  onCerrar: () => void
  onGuardado: (actualizado: Comprobante) => void
}

/** Se monta solo mientras se edita, con `key={comprobante.id}`: cada edición arranca con un formulario limpio. */
export function EditarComprobanteModal({ comprobante, onCerrar, onGuardado }: EditarComprobanteModalProps) {
  const [errorServidor, setErrorServidor] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isValid, isSubmitting },
  } = useForm<DatosComprobanteFormValues>({
    resolver: zodResolver(datosComprobanteSchema),
    mode: 'onChange',
    defaultValues: aValoresFormulario(comprobante),
  })

  const enviar = async (valores: DatosComprobanteFormValues) => {
    try {
      setErrorServidor(null)
      const actualizado = await actualizarBorradorAsync(comprobante.id, aDtoActualizacion(valores))
      onGuardado(actualizado)
    } catch (error) {
      setErrorServidor(extraerMensajeErrorBackend(error))
    }
  }

  const limpiar = { onChange: () => setErrorServidor(null) }

  return (
    <Modal abierto titulo="Corregir datos del comprobante" onCerrar={onCerrar} ancho="lg">
      <p className="mb-4 text-sm text-slate-400">
        Archivo: <span className="font-medium text-slate-200">{comprobante.nombreArchivoOriginal}</span>. Revisa lo que
        el sistema leyó de la factura y corrige lo que haga falta.
      </p>

      <form onSubmit={handleSubmit(enviar)} className="space-y-4" noValidate>
        <div className="grid gap-4 sm:grid-cols-2">
          <Input
            etiqueta="Número de factura"
            placeholder="000-001-01-00000123"
            disabled={isSubmitting}
            error={errors.numeroFactura?.message}
            {...register('numeroFactura', limpiar)}
          />
          <Input
            etiqueta="RTN del proveedor"
            inputMode="numeric"
            maxLength={14}
            placeholder="14 dígitos, sin guiones"
            disabled={isSubmitting}
            error={errors.proveedorRTN?.message}
            {...register('proveedorRTN', limpiar)}
          />
          <Input
            etiqueta="Fecha de emisión"
            type="date"
            disabled={isSubmitting}
            error={errors.fechaEmision?.message}
            {...register('fechaEmision', limpiar)}
          />
          <Input
            etiqueta="CAI"
            placeholder="XXXXXX-XXXXXX-XXXXXX-XXXXXX-XXXXXX-XX"
            disabled={isSubmitting}
            error={errors.cai?.message}
            {...register('cai', limpiar)}
          />
          <Input
            etiqueta="Monto total (L)"
            inputMode="decimal"
            disabled={isSubmitting}
            error={errors.montoTotal?.message}
            {...register('montoTotal', limpiar)}
          />
          <Input
            etiqueta="ISV (L)"
            inputMode="decimal"
            disabled={isSubmitting}
            error={errors.isv?.message}
            {...register('isv', limpiar)}
          />
        </div>

        {errorServidor && <Alert titulo="Respuesta del servidor:">{errorServidor}</Alert>}

        <div className="flex justify-end gap-2 pt-2">
          <Button variante="fantasma" onClick={onCerrar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" disabled={!isValid} cargando={isSubmitting}>
            💾 Guardar cambios
          </Button>
        </div>
      </form>
    </Modal>
  )
}
