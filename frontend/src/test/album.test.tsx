// @vitest-environment jsdom
import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AlbumView } from '../features/gastos/components/AlbumView'
import { eliminarBorradorAsync, liquidarLoteAsync, obtenerAlbumAsync } from '../features/gastos/api/gastosApi'
import type { Comprobante } from '../features/gastos/types/gastos.types'

vi.mock('../features/gastos/api/gastosApi', () => ({
  obtenerAlbumAsync: vi.fn(),
  liquidarLoteAsync: vi.fn(),
  eliminarBorradorAsync: vi.fn(),
  subirBorradorAsync: vi.fn(),
  actualizarBorradorAsync: vi.fn(),
  abrirArchivoEnNuevaPestanaAsync: vi.fn(),
}))

const comprobante = (sobrescribe: Partial<Comprobante>): Comprobante => ({
  id: 'x',
  tipoArchivo: 'Pdf',
  nombreArchivoOriginal: 'f.pdf',
  urlArchivo: '',
  numeroFactura: null,
  cai: null,
  proveedorRTN: null,
  fechaEmision: null,
  montoTotal: null,
  isv: null,
  estado: 'Borrador',
  resultadoAuditoria: null,
  fechaCarga: '2026-11-08T10:00:00Z',
  items: [],
  hallazgos: [],
  ...sobrescribe,
})

const completo = comprobante({
  id: 'c1',
  numeroFactura: '000-001-01-00000123',
  proveedorRTN: '08019999000011',
  fechaEmision: '2026-11-05',
  montoTotal: 450,
})
const incompleto = comprobante({ id: 'c2', nombreArchivoOriginal: 'borrosa.jpg', tipoArchivo: 'Imagen' })

function Pantalla() {
  return (
    <MemoryRouter initialEntries={['/album']}>
      <Routes>
        <Route path="/album" element={<AlbumView />} />
        <Route path="/lotes/:id" element={<p>DETALLE DEL LOTE</p>} />
      </Routes>
    </MemoryRouter>
  )
}

beforeEach(() => {
  vi.mocked(obtenerAlbumAsync).mockResolvedValue([completo, incompleto])
})
afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('AlbumView', () => {
  it('lista los borradores y bloquea los incompletos explicando qué falta', async () => {
    render(<Pantalla />)

    expect(await screen.findByText('000-001-01-00000123')).toBeInTheDocument()
    const tarjetaIncompleta = screen.getByText('borrosa.jpg').closest('article')!
    expect(within(tarjetaIncompleta).getByRole('checkbox')).toBeDisabled()
    expect(within(tarjetaIncompleta).getByText(/número de factura, RTN del proveedor, fecha de emisión, monto total/)).toBeInTheDocument()
  })

  it('el botón de liquidar solo se activa al seleccionar y cuenta la selección', async () => {
    const usuario = userEvent.setup()
    render(<Pantalla />)
    await screen.findByText('000-001-01-00000123')

    expect(screen.getByRole('button', { name: /Liquidar lote/ })).toBeDisabled()

    const tarjeta = screen.getByText('000-001-01-00000123').closest('article')!
    await usuario.click(within(tarjeta).getByRole('checkbox'))
    expect(screen.getByRole('button', { name: /Liquidar lote \(1\)/ })).toBeEnabled()
  })

  it('liquida el lote con los ids seleccionados y navega al detalle', async () => {
    const usuario = userEvent.setup()
    vi.mocked(liquidarLoteAsync).mockResolvedValue({
      loteId: 'L9',
      descripcion: 'Gira',
      fechaLiquidacion: '2026-11-08T12:00:00Z',
      totalVerdes: 1,
      totalAmarillos: 0,
      totalRojos: 0,
      comprobantes: [],
    })
    render(<Pantalla />)
    await screen.findByText('000-001-01-00000123')

    const tarjeta = screen.getByText('000-001-01-00000123').closest('article')!
    await usuario.click(within(tarjeta).getByRole('checkbox'))
    await usuario.click(screen.getByRole('button', { name: /Liquidar lote \(1\)/ }))

    const dialogo = await screen.findByRole('dialog', { name: 'Liquidar lote' })
    await usuario.type(within(dialogo).getByLabelText(/Descripción/), 'Gira')
    await usuario.click(within(dialogo).getByRole('button', { name: /Liquidar y auditar/ }))

    expect(await screen.findByText('DETALLE DEL LOTE')).toBeInTheDocument()
    expect(liquidarLoteAsync).toHaveBeenCalledWith({ comprobanteIds: ['c1'], descripcion: 'Gira' })
  })

  it('elimina un borrador tras confirmar y lo quita de la lista sin recargar', async () => {
    const usuario = userEvent.setup()
    vi.mocked(eliminarBorradorAsync).mockResolvedValue(undefined)
    render(<Pantalla />)
    await screen.findByText('000-001-01-00000123')

    const tarjeta = screen.getByText('000-001-01-00000123').closest('article')!
    await usuario.click(within(tarjeta).getByRole('button', { name: 'Eliminar' }))

    const dialogo = await screen.findByRole('dialog', { name: 'Eliminar comprobante' })
    await usuario.click(within(dialogo).getByRole('button', { name: 'Eliminar' }))

    await waitFor(() => expect(screen.queryByText('000-001-01-00000123')).not.toBeInTheDocument())
    expect(eliminarBorradorAsync).toHaveBeenCalledWith('c1')
    expect(obtenerAlbumAsync).toHaveBeenCalledTimes(1) // actualización local, sin un segundo GET
  })

  it('muestra el estado vacío cuando no hay borradores', async () => {
    vi.mocked(obtenerAlbumAsync).mockResolvedValue([])
    render(<Pantalla />)
    expect(await screen.findByText('Tu álbum está vacío')).toBeInTheDocument()
  })
})
