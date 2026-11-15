import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { extraerTextoImagenAsync } from './extraerTextoImagen'

const mocks = vi.hoisted(() => ({
  createWorker: vi.fn(),
  recognize: vi.fn(),
}))

vi.mock('tesseract.js', () => ({
  createWorker: mocks.createWorker,
}))

beforeEach(() => {
  mocks.createWorker.mockResolvedValue({ recognize: mocks.recognize })
  mocks.recognize.mockResolvedValue({ data: { text: '  Factura de prueba  \n' } })
})

afterEach(() => vi.clearAllMocks())

describe('extraerTextoImagenAsync', () => {
  it('inicializa Tesseract en español y devuelve el texto recortado', async () => {
    const archivo = new File(['imagen'], 'factura.png', { type: 'image/png' })

    await expect(extraerTextoImagenAsync(archivo)).resolves.toBe('Factura de prueba')
    expect(mocks.createWorker).toHaveBeenCalledWith('spa')
    expect(mocks.recognize).toHaveBeenCalledWith(archivo)
  })
})
