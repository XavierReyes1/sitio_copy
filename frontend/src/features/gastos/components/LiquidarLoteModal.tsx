import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { extraerMensajeErrorBackend } from '../../../core/api/extraerMensajeError'
import { Alert, Button, Input, Modal } from '../../../core/ui'
import { formatearMonto } from '../../../core/utils/formato'
import { liquidarLoteAsync } from '../api/gastosApi'
import { liquidarLoteSchema } from '../schemas/gastos.schema'
import type { LiquidarLoteFormValues } from '../schemas/gastos.schema'
import type { Comprobante, ResultadoLote } from '../types/gastos.types'

interface LiquidarLoteModalProps {
  comprobantes: Comprobante[]
  onCerrar: () => void
  onLiquidado: (resultado: ResultadoLote) => void
}

export function LiquidarLoteModal({ comprobantes, onCerrar, onLiquidado }: LiquidarLoteModalProps) {
  const [errorServidor, setErrorServidor] = useState<string | null>(null)

  const total = comprobantes.reduce((suma, c) => suma + (c.montoTotal ?? 0), 0)

  const {
    register,
    handleSubmit,
    formState: { errors, isValid, isSubmitting },
  } = useForm<LiquidarLoteFormValues>({
    resolver: zodResolver(liquidarLoteSchema),
    mode: 'onChange',
    defaultValues: { descripcion: '' },
  })

  const enviar = async (valores: LiquidarLoteFormValues) => {
    try {
      setErrorServidor(null)
      const resultado = await liquidarLoteAsync({
        comprobanteIds: comprobantes.map((c) => c.id),
        descripcion: valores.descripcion === '' ? null : valores.descripcion,
      })
      onLiquidado(resultado)
    } catch (error) {
      // 400 con el detalle de qué comprobante incumple · 404 si alguno ya no existe
      setErrorServidor(extraerMensajeErrorBackend(error))
    }
  }

  return (
    <Modal abierto titulo="Liquidar lote" onCerrar={onCerrar}>
      <p className="mb-4 text-sm text-slate-300">
        Se auditarán <strong className="text-white">{comprobantes.length}</strong>{' '}
        {comprobantes.length === 1 ? 'comprobante' : 'comprobantes'} por un total de{' '}
        <strong className="text-white">{formatearMonto(total)}</strong> contra la política de tu empresa. Una vez
        liquidados ya no podrás editarlos.
      </p>

      <form onSubmit={handleSubmit(enviar)} className="space-y-4" noValidate>
        <Input
          etiqueta="Descripción del lote (opcional)"
          placeholder="Ej. Gira a San Pedro Sula"
          autoFocus
          disabled={isSubmitting}
          error={errors.descripcion?.message}
          {...register('descripcion', { onChange: () => setErrorServidor(null) })}
        />

        {errorServidor && <Alert titulo="Respuesta del servidor:">{errorServidor}</Alert>}

        <div className="flex justify-end gap-2 pt-2">
          <Button variante="fantasma" onClick={onCerrar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" disabled={!isValid} cargando={isSubmitting}>
            🚦 Liquidar y auditar
          </Button>
        </div>
      </form>
    </Modal>
  )
}
