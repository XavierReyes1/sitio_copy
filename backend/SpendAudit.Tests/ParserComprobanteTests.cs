using SpendAudit.Application.Features.Gastos.Services;

namespace SpendAudit.Tests;

public class ParserComprobanteTests
{
    private const string FacturaPdf = """
        FERRETERIA EL BUEN PRECIO S. DE R.L.
        RTN: 0801-1999-123456
        CAI: A1B2C3-D4E5F6-A7B8C9-D0E1F2-A3B4C5-D6
        Factura No. 000-001-01-00000123
        Fecha de emisión: 15/10/2026
        Cerveza Imperial 6 pack            180.00
        Cemento gris 42.5kg              1,250.00
        Subtotal                         1,430.00
        ISV 15%                            214.50
        Total a pagar L                  1,644.50
        """;

    private const string OcrFoto = """
        mam FACTURA >
        RESTAURANTE EL CHIRINGUITO
        Colonia Palmira, Tegucigalpa, Honduras
        RTN: 08011980123456
        CAL: AT23E1-458902-8D6E7F-9G0Ht 12-03
        FECHA: 24/05/2024 HORA: 14:38
        FACTURA No: 001-001-02-00004567
        CLIENTE: Consumidor Final
        RTNVID: N/A
        2 PLATOS TIPICOS L 380.00 -
        2 REFRESCOS L 70.00 o
        1 AGUA EMBOTELLADA L 25.00 e —— ;
        1 CAFE L 35.00
        9 SUBTOTAL L.510.00 d
        18 (15%) L 76.50
        A CTOTAL A PAGAR L 586.50
        """;

    [Fact]
    public void Pdf_ExtraeLosCamposDeCabecera()
    {
        var d = ParserComprobante.Parsear(FacturaPdf);

        Assert.Equal("000-001-01-00000123", d.NumeroFactura);
        Assert.Equal("A1B2C3-D4E5F6-A7B8C9-D0E1F2-A3B4C5-D6", d.CAI);
        Assert.Equal("08011999123456", d.ProveedorRTN);
        Assert.Equal(new DateOnly(2026, 10, 15), d.FechaEmision);
        Assert.Equal(1644.50m, d.MontoTotal);
        Assert.Equal(214.50m, d.ISV);
    }

    [Fact]
    public void Pdf_ExtraeSoloLosArticulos_NoSubtotalNiIsvNiTotal()
    {
        var d = ParserComprobante.Parsear(FacturaPdf);

        Assert.Equal(2, d.Items.Count);
        Assert.StartsWith("Cerveza", d.Items[0].Descripcion);
        Assert.Equal(180.00m, d.Items[0].Subtotal);
    }

    [Fact]
    public void OcrFoto_ExtraeLosCamposAunqueElTextoTengaRuido()
    {
        var d = ParserComprobante.Parsear(OcrFoto);

        Assert.Equal("001-001-02-00004567", d.NumeroFactura);
        Assert.Equal("08011980123456", d.ProveedorRTN);
        Assert.Equal(new DateOnly(2024, 5, 24), d.FechaEmision);
        Assert.Equal(586.50m, d.MontoTotal);
        Assert.Equal(76.50m, d.ISV);
        Assert.Equal(4, d.Items.Count);
        Assert.Null(d.CAI);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TextoVacio_DevuelveDatosVacios(string? texto)
    {
        Assert.Same(DatosExtraidos.Vacio, ParserComprobante.Parsear(texto));
    }

    // ---------------------------------------------------------------- texto real de OCR (Tesseract.js)

    [Theory]
    [InlineData(OcrFixtures.NodoLimpia)]
    [InlineData(OcrFixtures.NodoFoto)]
    [InlineData(OcrFixtures.NavegadorLimpia)]
    [InlineData(OcrFixtures.NavegadorFoto)]
    public void Ocr_ExtraeLosDatosClaveAunConRuidoDeFoto(string textoOcr)
    {
        var d = ParserComprobante.Parsear(textoOcr);

        Assert.Equal("000-001-01-00000123", d.NumeroFactura);
        Assert.Equal("08011999123456", d.ProveedorRTN); // el del proveedor, no el "RTN Cliente"
        Assert.Equal(new DateOnly(2026, 10, 15), d.FechaEmision);
        Assert.Equal(1644.50m, d.MontoTotal);
        Assert.Equal(214.50m, d.ISV); // leído como "1ISV", "1SV" o "18V" según la imagen
    }

    [Theory]
    [InlineData(OcrFixtures.NodoLimpia)]
    [InlineData(OcrFixtures.NodoFoto)]
    [InlineData(OcrFixtures.NavegadorLimpia)]
    [InlineData(OcrFixtures.NavegadorFoto)]
    public void Ocr_ExtraeLosDosArticulos_SinBasuraNiLineasDeImpuesto(string textoOcr)
    {
        var d = ParserComprobante.Parsear(textoOcr);

        Assert.Equal(2, d.Items.Count);
        Assert.StartsWith("Cemento", d.Items[0].Descripcion);
        Assert.Equal(1250.00m, d.Items[0].Subtotal);
        Assert.StartsWith("Cerveza", d.Items[1].Descripcion);
        Assert.Equal(180.00m, d.Items[1].Subtotal);
    }

    [Fact]
    public void Ocr_CaiLimpio_SeLee_PeroUnaLecturaConCaracterInsertadoNoSeInventa()
    {
        Assert.Equal("A1B2C3-D4E5F6-A7B8C9-D0E1F2-A3B4C5-D6", ParserComprobante.Parsear(OcrFixtures.NodoLimpia).CAI);
        // "D4E5SF6" tiene 7 caracteres: no hay forma honesta de repararlo; queda vacío para captura manual
        Assert.Null(ParserComprobante.Parsear(OcrFixtures.NodoFoto).CAI);
    }

    [Fact]
    public void Ocr_CorrigeConfusionesDeCaracteres_EnRtnCaiYNumeroDeFactura()
    {
        var d = ParserComprobante.Parsear(
            "RTN: O8O1-1999-12345B\nCAI: A1B2C3-D4E5F6-A7B8C9-DOE1F2-A3B4C5-D6\nFactura 0OO-OO1-O1-OOOOO123\nTotal L 99.00");

        Assert.Equal("08011999123458", d.ProveedorRTN);
        Assert.Equal("A1B2C3-D4E5F6-A7B8C9-D0E1F2-A3B4C5-D6", d.CAI);
        Assert.Equal("000-001-01-00000123", d.NumeroFactura);
    }

    // ---------------------------------------------------------------- fechas

    [Theory]
    [InlineData("Fecha: 5 de octubre de 2026", 2026, 10, 5)]
    [InlineData("Emitida 15-Oct-2026", 2026, 10, 15)]
    [InlineData("Fecha 15/10/26", 2026, 10, 15)]
    [InlineData("2026-11-05", 2026, 11, 5)]
    [InlineData("Fecha de emisión: 31/12/2026", 2026, 12, 31)]
    public void Fecha_AceptaFormatosComunes(string texto, int anio, int mes, int dia)
    {
        Assert.Equal(new DateOnly(anio, mes, dia), ParserComprobante.Parsear(texto).FechaEmision);
    }

    [Fact]
    public void Fecha_PrefiereLaFechaDeEmisionSobreOtrasFechas()
    {
        var d = ParserComprobante.Parsear("Fecha límite de pago: 30/11/2026\nFecha de emisión: 01/11/2026");
        Assert.Equal(new DateOnly(2026, 11, 1), d.FechaEmision);
    }

    [Fact]
    public void Fecha_Imposible_SeDescarta()
    {
        Assert.Null(ParserComprobante.Parsear("Fecha: 31/02/2026").FechaEmision);
    }

    // ---------------------------------------------------------------- otros campos

    [Fact]
    public void Rtn_SiSoloHayUnoDeCliente_ElDelProveedorQuedaVacio()
    {
        Assert.Null(ParserComprobante.Parsear("Cliente RTN 08011999123456").ProveedorRTN);
    }

    [Fact]
    public void Total_TomaElMayor_IgnorandoElSubtotal()
    {
        Assert.Equal(115.00m, ParserComprobante.Parsear("Subtotal 100.00\nTotal 115.00").MontoTotal);
    }

    [Fact]
    public void Isv_AceptaPuntosEnLaSigla()
    {
        Assert.Equal(15.00m, ParserComprobante.Parsear("I.S.V. 15%   15.00").ISV);
    }

    [Fact]
    public void Items_LasLineasConPorcentajeNoSonArticulos()
    {
        var d = ParserComprobante.Parsear("Descuento 10%   25.00\nArroz 5 libras  40.00");
        Assert.Single(d.Items);
    }

    [Fact]
    public void Rtn_UnaPalabraConLetrasParecidasANumerosNoSeConfundeConUnRtn()
    {
        Assert.Null(ParserComprobante.Parsear("BOSSBOSS-BOSS-BOSSBO").ProveedorRTN);
    }
}
