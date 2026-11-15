import { afterEach, describe, expect, it, vi } from 'vitest'
import { subirBorradorAsync } from './gastosApi'

const mocks = vi.hoisted(() => ({
  post: vi.fn(),
}))

vi.mock('../../../core/api/apiClient', () => ({
  apiClient: { post: mocks.post },
}))

afterEach(() => vi.clearAllMocks())

describe('subirBorradorAsync', () => {
  it('envía el texto OCR en el formulario multipart', async () => {
    mocks.post.mockResolvedValue({ data: { id: 'comprobante-1' } })
    const archivo = new File(['imagen'], 'factura.jpg', { type: 'image/jpeg' })

    await subirBorradorAsync(archivo, 'Factura No: 001-001-02-00004567')

    expect(mocks.post).toHaveBeenCalledWith('/gastos/borrador', expect.any(FormData))
    const formulario = mocks.post.mock.calls[0][1] as FormData
    expect(formulario.get('archivo')).toBe(archivo)
    expect(formulario.get('textoOcr')).toBe('Factura No: 001-001-02-00004567')
  })
})
