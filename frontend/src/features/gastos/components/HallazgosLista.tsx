import type { Hallazgo } from '../types/gastos.types'

const titulos: Record<Hallazgo['regla'], string> = {
  FacturaDuplicada: 'Factura duplicada',
  TerminoRestringido: 'Término restringido',
  FacturaVencida: 'Factura vencida',
  TopeDiarioExcedido: 'Tope diario excedido',
}

export function HallazgosLista({ hallazgos }: { hallazgos: Hallazgo[] }) {
  if (hallazgos.length === 0) return null
  return (
    <ul className="mt-3 space-y-1.5">
      {hallazgos.map((h, i) => (
        <li
          key={`${h.regla}-${i}`}
          className={`rounded-lg border px-3 py-2 text-xs ${
            h.severidad === 'Rojo'
              ? 'border-red-500/30 bg-red-950/40 text-red-200'
              : 'border-amber-500/30 bg-amber-950/30 text-amber-200'
          }`}
        >
          <strong>{titulos[h.regla]}:</strong> {h.detalle}
        </li>
      ))}
    </ul>
  )
}
