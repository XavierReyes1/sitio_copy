import type { Semaforo } from '../types/gastos.types'

const estilos: Record<Semaforo, { punto: string; caja: string; texto: string }> = {
  Verde: { punto: 'bg-emerald-400', caja: 'border-emerald-500/30 bg-emerald-500/10 text-emerald-300', texto: 'Aprobada' },
  Amarillo: { punto: 'bg-amber-400', caja: 'border-amber-500/30 bg-amber-500/10 text-amber-300', texto: 'Requiere revisión' },
  Rojo: { punto: 'bg-red-500', caja: 'border-red-500/30 bg-red-500/10 text-red-300', texto: 'Bloqueada' },
}

export function SemaforoBadge({ resultado }: { resultado: Semaforo | null }) {
  if (!resultado) {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-md border border-slate-600 bg-slate-700/40 px-2.5 py-1 text-xs font-semibold text-slate-300">
        Borrador
      </span>
    )
  }
  const { punto, caja, texto } = estilos[resultado]
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1 text-xs font-semibold ${caja}`}>
      <span className={`h-2 w-2 rounded-full ${punto}`} aria-hidden="true" />
      {resultado} · {texto}
    </span>
  )
}

/** Contadores compactos para las listas de lotes. */
export function ResumenSemaforo({
  verdes,
  amarillos,
  rojos,
}: {
  verdes: number
  amarillos: number
  rojos: number
}) {
  return (
    <div className="flex items-center gap-2 text-xs font-semibold" aria-label="Resumen del semáforo">
      <span className="flex items-center gap-1 text-emerald-300" title="Verdes">
        <span className="h-2 w-2 rounded-full bg-emerald-400" /> {verdes}
      </span>
      <span className="flex items-center gap-1 text-amber-300" title="Amarillos">
        <span className="h-2 w-2 rounded-full bg-amber-400" /> {amarillos}
      </span>
      <span className="flex items-center gap-1 text-red-300" title="Rojos">
        <span className="h-2 w-2 rounded-full bg-red-500" /> {rojos}
      </span>
    </div>
  )
}
