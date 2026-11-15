# Cambios del backend: lectura automática de facturas (fotos y PDFs)

Reemplaza estos archivos por los de este paquete (cada uno conserva su ruta y su dueño del reparto original).

| Archivo | Cambio | Dueño |
|---|---|---|
| `backend/SpendAudit.Application/Features/Gastos/Services/ParserComprobante.cs` | Parser reescrito: tolera errores de OCR (O/0, I/1, S/5, B/8, `18V` por `ISV`), fechas en español y de 2 dígitos, RTN del proveedor vs. del cliente, CAI hexadecimal corregido, ruido al final de las líneas | P6 |
| `backend/SpendAudit.Application/Features/Gastos/Services/GastosService.cs` | En fotos usa el texto del OCR del navegador (`TextoOcr`) como `TextoExtraido` y lo pasa por el mismo parser | P6 |
| `backend/SpendAudit.API/Controllers/GastosController.cs` | Pasa `TextoOcr` del formulario al DTO | P6 |
| `backend/SpendAudit.Application/Features/Gastos/DTOs/GastosDtos.cs` | `CreateBorradorRequestDto.TextoOcr` | P5 |
| `backend/SpendAudit.Application/Features/Gastos/Validators/GastosValidators.cs` | `TextoOcr` máximo 50 000 caracteres | P5 |
| `backend/SpendAudit.API/Contracts/CreateBorradorForm.cs` | Campo `TextoOcr` del multipart | P5 |
| `backend/SpendAudit.Tests/SpendAudit.Tests.csproj` | Agrega `<Using Include="Xunit" />` (sin esto las pruebas no compilaban) | P6 |
| `backend/SpendAudit.Tests/ParserComprobanteTests.cs` | 29 casos, incluidos 4 textos reales de OCR | P6 |
| `backend/SpendAudit.Tests/OcrFixtures.cs` (nuevo) | Textos reales de Tesseract.js usados como datos de prueba | P6 |

## Contrato
`POST /api/v1/gastos/borrador` (multipart) acepta un campo opcional `textoOcr` (texto). Se usa **solo si el archivo es una imagen**;
en PDFs lo ignora porque el servidor extrae el texto.

## Nota de confianza
`textoOcr` lo declara el cliente. Sirve para ahorrar digitación, pero un empleado podría enviar un texto distinto al de la imagen
(por ejemplo, sin la palabra «cerveza») y evitar la regla de términos restringidos en esa factura. Las facturas en PDF no tienen este
problema porque el texto lo extrae el servidor. Para un entorno de producción estricto, el OCR debería hacerse en el servidor.
