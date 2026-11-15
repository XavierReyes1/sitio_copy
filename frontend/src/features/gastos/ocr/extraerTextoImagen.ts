type WorkerOcr = Awaited<ReturnType<typeof import('tesseract.js').createWorker>>

let workerPromise: Promise<WorkerOcr> | undefined

const obtenerWorker = async (): Promise<WorkerOcr> => {
  if (!workerPromise) {
    workerPromise = import('tesseract.js')
      .then(({ createWorker }) => createWorker('spa'))
      .catch((error: unknown) => {
        workerPromise = undefined
        throw error
      })
  }
  return workerPromise
}

export const extraerTextoImagenAsync = async (archivo: File): Promise<string> => {
  const worker = await obtenerWorker()
  const resultado = await worker.recognize(archivo)
  return resultado.data.text.trim()
}
