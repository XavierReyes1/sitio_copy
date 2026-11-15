using System.Text.RegularExpressions;
using SpendAudit.Domain.Entities;
using SpendAudit.Domain.Enums;

namespace SpendAudit.Application.Features.Gastos.Services;

public record HallazgoCalculado(ReglaAuditoria Regla, SeveridadHallazgo Severidad, string Detalle);

public record ResultadoComprobante(
    Guid ComprobanteId,
    ResultadoAuditoria Resultado,
    IReadOnlyList<HallazgoCalculado> Hallazgos,
    IReadOnlyList<Guid> ItemsRestringidos);

/// <summary>Todo lo que el motor necesita saber; se arma desde la política y la base de datos.</summary>
public class ContextoAuditoria
{
    public int DiasVigenciaMaxima { get; init; }
    public IReadOnlyList<string> TerminosRestringidos { get; init; } = Array.Empty<string>();
    /// <summary>Tope diario del puesto del usuario; null si su puesto no tiene tope configurado.</summary>
    public decimal? TopeDiarioHNL { get; init; }
    public DateOnly Hoy { get; init; }
    /// <summary>Facturas ya liquidadas como Verde/Amarillo en la empresa: (RTN proveedor, número).</summary>
    public IReadOnlySet<(string Rtn, string Numero)> FacturasAceptadas { get; init; } =
        new HashSet<(string, string)>();
    /// <summary>Lo que el usuario ya liquidó (no Rojo) por fecha de emisión, antes de este lote.</summary>
    public IReadOnlyDictionary<DateOnly, decimal> TotalesPreviosPorDia { get; init; } =
        new Dictionary<DateOnly, decimal>();
}

/// <summary>
/// Reglas deterministas de auditoría. No toca base de datos: recibe datos, devuelve resultados.
/// Rojo  → factura duplicada o término restringido.
/// Amarillo → factura vencida o tope diario excedido.
/// Verde → sin hallazgos.
/// </summary>
public class MotorAuditoria
{
    public IReadOnlyList<ResultadoComprobante> Auditar(
        IEnumerable<Comprobante> comprobantes, ContextoAuditoria ctx)
    {
        var aceptadas = new HashSet<(string, string)>(ctx.FacturasAceptadas);
        var acumuladoPorDia = new Dictionary<DateOnly, decimal>(ctx.TotalesPreviosPorDia);
        var patrones = ConstruirPatrones(ctx.TerminosRestringidos);
        var resultados = new List<ResultadoComprobante>();

        // Se procesan en el orden en que se subieron: ante un duplicado dentro del mismo lote,
        // el primero queda aceptado y el segundo se bloquea.
        foreach (var c in comprobantes.OrderBy(x => x.FechaCarga).ThenBy(x => x.Id))
        {
            var hallazgos = new List<HallazgoCalculado>();

            // 1) Factura duplicada → Rojo
            var clave = ClaveFactura(c);
            if (clave is not null && aceptadas.Contains(clave.Value))
            {
                hallazgos.Add(new HallazgoCalculado(
                    ReglaAuditoria.FacturaDuplicada, SeveridadHallazgo.Rojo,
                    $"La factura {c.NumeroFactura} del proveedor {c.ProveedorRTN} ya fue registrada anteriormente."));
            }

            // 2) Términos restringidos → Rojo
            var (terminosHallados, itemsRestringidos) = BuscarTerminos(c, patrones);
            if (terminosHallados.Count > 0)
            {
                hallazgos.Add(new HallazgoCalculado(
                    ReglaAuditoria.TerminoRestringido, SeveridadHallazgo.Rojo,
                    $"El comprobante contiene términos no permitidos por la política: {string.Join(", ", terminosHallados)}."));
            }

            // 3) Vigencia → Amarillo
            if (c.FechaEmision is { } emision)
            {
                var antiguedad = ctx.Hoy.DayNumber - emision.DayNumber;
                if (antiguedad > ctx.DiasVigenciaMaxima)
                {
                    hallazgos.Add(new HallazgoCalculado(
                        ReglaAuditoria.FacturaVencida, SeveridadHallazgo.Amarillo,
                        $"La factura tiene {antiguedad} días de antigüedad y la vigencia máxima es de {ctx.DiasVigenciaMaxima} días."));
                }
            }

            // 4) Tope diario → Amarillo (las facturas Rojas no se reembolsan, no consumen tope)
            var esRojo = hallazgos.Any(h => h.Severidad == SeveridadHallazgo.Rojo);
            if (!esRojo && c.FechaEmision is { } dia && c.MontoTotal is { } monto)
            {
                acumuladoPorDia.TryGetValue(dia, out var previo);
                var nuevoTotal = previo + monto;
                acumuladoPorDia[dia] = nuevoTotal;

                if (ctx.TopeDiarioHNL is { } tope && nuevoTotal > tope)
                {
                    hallazgos.Add(new HallazgoCalculado(
                        ReglaAuditoria.TopeDiarioExcedido, SeveridadHallazgo.Amarillo,
                        $"El total del {dia:dd/MM/yyyy} acumula L {nuevoTotal:N2} y el tope diario del puesto es L {tope:N2}."));
                }
            }

            if (!esRojo && clave is not null) aceptadas.Add(clave.Value);

            var resultado = hallazgos.Count == 0
                ? ResultadoAuditoria.Verde
                : (ResultadoAuditoria)hallazgos.Max(h => (int)h.Severidad);

            resultados.Add(new ResultadoComprobante(c.Id, resultado, hallazgos, itemsRestringidos));
        }

        return resultados;
    }

    private static (string Rtn, string Numero)? ClaveFactura(Comprobante c) =>
        string.IsNullOrWhiteSpace(c.NumeroFactura) || string.IsNullOrWhiteSpace(c.ProveedorRTN)
            ? null
            : (c.ProveedorRTN.Trim(), c.NumeroFactura.Trim());

    /// <summary>Cada término coincide si aparece al inicio de una palabra (así "cerveza" detecta "cervezas").</summary>
    private static List<(string Termino, Regex Patron)> ConstruirPatrones(IEnumerable<string> terminos) =>
        terminos
            .Select(t => (Original: t.Trim(), Norm: TextoUtil.Normalizar(t)))
            .Where(t => t.Norm.Length > 0)
            .DistinctBy(t => t.Norm)
            .Select(t => (t.Original, new Regex(
                $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(t.Norm)}",
                RegexOptions.CultureInvariant | RegexOptions.Compiled)))
            .ToList();

    private static (List<string> Terminos, IReadOnlyList<Guid> Items) BuscarTerminos(
        Comprobante c, List<(string Termino, Regex Patron)> patrones)
    {
        var hallados = new List<string>();
        var itemsRestringidos = new List<Guid>();
        if (patrones.Count == 0) return (hallados, itemsRestringidos);

        var textoNorm = TextoUtil.Normalizar(c.TextoExtraido);
        var descripciones = c.Items.Select(i => (i.Id, Norm: TextoUtil.Normalizar(i.Descripcion))).ToList();

        foreach (var (termino, patron) in patrones)
        {
            var enTexto = textoNorm.Length > 0 && patron.IsMatch(textoNorm);
            var itemsConTermino = descripciones.Where(d => patron.IsMatch(d.Norm)).Select(d => d.Id).ToList();

            if (enTexto || itemsConTermino.Count > 0) hallados.Add(termino);
            itemsRestringidos.AddRange(itemsConTermino);
        }

        return (hallados, itemsRestringidos.Distinct().ToList());
    }
}
