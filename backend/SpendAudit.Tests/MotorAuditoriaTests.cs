using SpendAudit.Application.Features.Gastos.Services;
using SpendAudit.Domain.Entities;
using SpendAudit.Domain.Enums;

namespace SpendAudit.Tests;

public class MotorAuditoriaTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 8);
    private const string Rtn = "08011999123456";
    private readonly MotorAuditoria _motor = new();

    private static Comprobante Crear(string numero, decimal monto, DateOnly fecha, string? texto = null, int minutos = 0) => new()
    {
        NumeroFactura = numero,
        ProveedorRTN = Rtn,
        MontoTotal = monto,
        FechaEmision = fecha,
        TextoExtraido = texto,
        FechaCarga = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc).AddMinutes(minutos)
    };

    private static ContextoAuditoria Contexto(
        decimal? tope = 1000, int dias = 15, string[]? terminos = null,
        HashSet<(string, string)>? aceptadas = null, Dictionary<DateOnly, decimal>? previos = null) => new()
    {
        DiasVigenciaMaxima = dias,
        TerminosRestringidos = terminos ?? new[] { "alcohol", "Cerveza" },
        TopeDiarioHNL = tope,
        Hoy = Hoy,
        FacturasAceptadas = aceptadas ?? new HashSet<(string, string)>(),
        TotalesPreviosPorDia = previos ?? new Dictionary<DateOnly, decimal>()
    };

    [Fact]
    public void FacturaLimpia_EsVerde()
    {
        var r = _motor.Auditar(new[] { Crear("A", 200, Hoy.AddDays(-1)) }, Contexto());
        Assert.Equal(ResultadoAuditoria.Verde, r[0].Resultado);
        Assert.Empty(r[0].Hallazgos);
    }

    [Fact]
    public void FacturaVencida_EsAmarilla()
    {
        var r = _motor.Auditar(new[] { Crear("B", 200, Hoy.AddDays(-20)) }, Contexto(dias: 15));
        Assert.Equal(ResultadoAuditoria.Amarillo, r[0].Resultado);
        Assert.Equal(ReglaAuditoria.FacturaVencida, r[0].Hallazgos[0].Regla);
    }

    [Fact]
    public void TopeDiarioExcedido_MarcaAmarilloLaFacturaQueLoCruza()
    {
        var r = _motor.Auditar(new[] { Crear("C", 600, Hoy), Crear("D", 600, Hoy, minutos: 1) }, Contexto(tope: 1000));
        Assert.Equal(ResultadoAuditoria.Verde, r[0].Resultado);
        Assert.Equal(ResultadoAuditoria.Amarillo, r[1].Resultado);
        Assert.Equal(ReglaAuditoria.TopeDiarioExcedido, r[1].Hallazgos[0].Regla);
    }

    [Fact]
    public void TopeDiario_SumaLoYaLiquidadoAntes()
    {
        var r = _motor.Auditar(new[] { Crear("E", 400, Hoy) },
            Contexto(tope: 1000, previos: new Dictionary<DateOnly, decimal> { [Hoy] = 700m }));
        Assert.Equal(ResultadoAuditoria.Amarillo, r[0].Resultado);
    }

    [Fact]
    public void SinTopeConfigurado_NoHayReglaDeTope()
    {
        var r = _motor.Auditar(new[] { Crear("F", 5000, Hoy) }, Contexto(tope: null));
        Assert.Equal(ResultadoAuditoria.Verde, r[0].Resultado);
    }

    [Fact]
    public void TerminoRestringido_EsRojo_ConPluralYOtroCaso()
    {
        var r = _motor.Auditar(new[] { Crear("G", 100, Hoy, "Cervezas Imperial x6") }, Contexto());
        Assert.Equal(ResultadoAuditoria.Rojo, r[0].Resultado);
        Assert.Equal(ReglaAuditoria.TerminoRestringido, r[0].Hallazgos[0].Regla);
    }

    [Fact]
    public void TerminoRestringido_IgnoraTildesYMayusculas()
    {
        var r = _motor.Auditar(new[] { Crear("H", 100, Hoy, "Compra de CERVEZA") }, Contexto(terminos: new[] { "cervéza" }));
        Assert.Equal(ResultadoAuditoria.Rojo, r[0].Resultado);
    }

    [Fact]
    public void TerminoRestringido_NoCoincideEnMedioDeUnaPalabra()
    {
        var r = _motor.Auditar(new[] { Crear("I", 100, Hoy, "Personal de limpieza") }, Contexto(terminos: new[] { "sonal" }));
        Assert.Equal(ResultadoAuditoria.Verde, r[0].Resultado);
    }

    [Fact]
    public void TerminoRestringido_MarcaElItemInfractor()
    {
        var c = Crear("J", 100, Hoy, "texto sin nada");
        var malo = new DetalleItem { Descripcion = "Whisky alcohol 750ml", Subtotal = 80 };
        c.Items.Add(malo);
        c.Items.Add(new DetalleItem { Descripcion = "Agua pura", Subtotal = 20 });

        var r = _motor.Auditar(new[] { c }, Contexto());

        Assert.Equal(ResultadoAuditoria.Rojo, r[0].Resultado);
        Assert.Single(r[0].ItemsRestringidos);
        Assert.Equal(malo.Id, r[0].ItemsRestringidos[0]);
    }

    [Fact]
    public void DuplicadaContraLaBase_EsRoja()
    {
        var aceptadas = new HashSet<(string, string)> { (Rtn, "K") };
        var r = _motor.Auditar(new[] { Crear("K", 100, Hoy) }, Contexto(aceptadas: aceptadas));
        Assert.Equal(ResultadoAuditoria.Rojo, r[0].Resultado);
        Assert.Equal(ReglaAuditoria.FacturaDuplicada, r[0].Hallazgos[0].Regla);
    }

    [Fact]
    public void DuplicadaDentroDelLote_LaPrimeraPasa_LaSegundaEsRoja()
    {
        var r = _motor.Auditar(new[] { Crear("L", 100, Hoy), Crear("L", 100, Hoy, minutos: 1) }, Contexto());
        Assert.Equal(ResultadoAuditoria.Verde, r[0].Resultado);
        Assert.Equal(ResultadoAuditoria.Rojo, r[1].Resultado);
    }

    [Fact]
    public void UnaFacturaRoja_NoConsumeElTopeDiario()
    {
        var r = _motor.Auditar(new[] { Crear("M", 900, Hoy, "alcohol"), Crear("N", 900, Hoy, minutos: 1) }, Contexto(tope: 1000));
        Assert.Equal(ResultadoAuditoria.Rojo, r[0].Resultado);
        Assert.Equal(ResultadoAuditoria.Verde, r[1].Resultado);
    }

    [Fact]
    public void Rojo_GanaSobreAmarillo_PeroConservaAmbosHallazgos()
    {
        var r = _motor.Auditar(new[] { Crear("O", 5000, Hoy.AddDays(-30), "alcohol") }, Contexto());
        Assert.Equal(ResultadoAuditoria.Rojo, r[0].Resultado);
        Assert.Contains(r[0].Hallazgos, h => h.Regla == ReglaAuditoria.FacturaVencida);
        Assert.Contains(r[0].Hallazgos, h => h.Regla == ReglaAuditoria.TerminoRestringido);
    }
}
