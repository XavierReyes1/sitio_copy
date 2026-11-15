namespace SpendAudit.Tests;

/// <summary>
/// Texto REAL devuelto por Tesseract.js (idioma español) sobre dos facturas de prueba: una limpia y una
/// que simula una foto de celular (giro, sombra, desenfoque, ruido). Sirven para comprobar que el parser
/// tolera los errores típicos del OCR (S/5, O/0, "18V" por "ISV", basura al final de la línea...).
/// Los del navegador llevan saltos \r\n porque FormData normaliza así los saltos de línea al enviarlos.
/// </summary>
public static class OcrFixtures
{
    public const string NodoLimpia = "FERRETERIA EL BUEN PRECIO S. DER.L.\n\nCol. Kennedy, San Pedro Sula, Cortés\n\nRTN: 0801-1999-123456\n\nCAI: A1B2C3-D4E5F6-A7B8C9-D0E1F2-A3B4C5-D6\n\nFactura No. 000-001-01-00000123\n\nFecha de emisión: 15/10/2026\n\nCliente: Constructora Maya\n\nRTN Cliente: 05011988654321\n\nDescripción Cant Importe\n\nCemento gris 42.5kg 5 1,250.00\n\nCerveza Imperial 6 pack 1 180.00\nSubtotal 1,430.00\n1ISV 15% 214.50\n\nTotal a pagar L 1,644.50\nOriginal: Cliente / Copia: Obligado Tributario Emisor\n";

    public const string NodoFoto = "FERRETERIA EL BUEN PRECIO S. DER.L.\nCol. Kennedy, San Pedro Sula, Cortés\nRTN: 0801-1999-123456\nCAI: A1B2C3-D4E5SF6-A7B8C9-DOE1F2-A3B4C5-D6\nFactura No. 000-001-01-00000123\nFecha de emisión: 15/10/2026\nCliente: Constructora Maya\nRTN Cliente: 05011988654321\nLL  »N“€ICNTTT\nDescripción Cant Importe\n2EOeOEZe-\nCemento gris 42.5kg 5 1,250.00\nCerveza Imperial 6 pack = 180.00\nZZ[]]]][U]STT\nSubtotal 1,430.00\n1SV 15% 214.50\nTotal a pagarL — 1,644.50\nOriginal: Cliente / Copia: Obligado Tributario Emisor\n";

    public const string NavegadorLimpia = "FERRETERIA EL BUEN PRECIO S. DER.L.\r\n\r\nCol. Kennedy, San Pedro Sula, Cortés\r\n\r\nRTN: 0801-1999-123456\r\n\r\nCAI: A1B2C3-D4E5F6-A7B8C9-D0E1F2-A3B4C5-D6\r\n\r\nFactura No. 000-001-01-00000123\r\n\r\nFecha de emisión: 15/10/2026\r\n\r\nCliente: Constructora Maya\r\n\r\nRTN Cliente: 05011988654321\r\n\r\nDescripción Cant Importe\r\n\r\nCemento gris 42.5kg 5 1,250.00\r\n\r\nCerveza Imperial 6 pack 1 180.00\r\nSubtotal 1,430.00\r\n1ISV 15% 214.50\r\n\r\nTotal a pagar L 1,644.50\r\nOriginal: Cliente / Copia: Obligado Tributario Emisor";

    public const string NavegadorFoto = "FERRETERIA EL BUEN PRECIO S. DER.L. :\r\nCol. Kennedy, San Pedro Sula, Cortés s\r\nRTN: 0801-1999-123456 :\r\nCAI: A1B2C3-D4E5SF6-A7B8C9-DOE1F2-A3B4C5-D6\r\nFactura No. 000-001-01-00000123\r\nFecha de emisión: 15/10/2026 E\r\nCliente: Constructora Maya\r\nRTN Cliente: 05011988654321\r\nLL \"T_-___—_\"Fn]\r\nDescripción cant Importe\r\nCemento gris 42.5kg 5 1,250.00 b\r\nCerveza Imperial 6 pack 1 180.00\r\nZa\r\nSubtotal 1,430.00 ;\r\n18V 15% 214.50 -\r\nTotal a pagar L 1,644.50\r\noriginal: Cliente / Copia: Obligado Tributario Emisor";
}
