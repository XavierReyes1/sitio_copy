using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpendAudit.Application.Features.Gastos.Services;

public record ItemExtraido(string Descripcion, decimal Subtotal);

public record DatosExtraidos(
    string? NumeroFactura,
    string? CAI,
    string? ProveedorRTN,
    DateOnly? FechaEmision,
    decimal? MontoTotal,
    decimal? ISV,
    IReadOnlyList<ItemExtraido> Items)
{
    public static readonly DatosExtraidos Vacio =
        new(null, null, null, null, null, null, Array.Empty<ItemExtraido>());
}

/// <summary>
/// Convierte el texto plano de una factura hondureña en campos. El texto puede venir de un PDF
/// (PdfPig) o de un OCR de fotografías, que confunde caracteres parecidos (O/0, I/1, S/5, B/8), por lo
/// que el parser tolera esas confusiones en los campos numéricos. Si un campo no se reconoce queda en
/// null y el empleado lo completa a mano (HU-06).
/// </summary>
public static class ParserComprobante
{
    private const string Monto = @"(\d{1,3}(?:,\d{3})+\.\d{2}|\d+\.\d{2})";

    /// <summary>Un dígito, o un carácter que el OCR suele confundir con uno.</summary>
    private const string D = "[0-9OoIlSB]";

    private const string Limite = @"(?<![0-9A-Za-z])";
    private const string LimiteFin = @"(?![0-9A-Za-z])";

    // Número de factura SAR: 000-001-01-00000123
    private static readonly Regex FacturaRegex = new(
        $@"{Limite}{D}{{3}}-{D}{{3}}-{D}{{2}}-{D}{{8}}{LimiteFin}", RegexOptions.Compiled);

    // RTN: 14 dígitos, con o sin guiones (0801-1999-123456)
    private static readonly Regex RtnRegex = new(
        $@"{Limite}{D}{{4}}-?{D}{{4}}-?{D}{{6}}{LimiteFin}", RegexOptions.Compiled);

    // CAI: 32 hexadecimales en grupos 6-6-6-6-6-2 (se captura con holgura y luego se corrige y valida)
    private static readonly Regex CaiCandidatoRegex = new(
        @"(?<![0-9A-Za-z])[0-9A-Za-z]{6}(?:-[0-9A-Za-z]{6}){4}-[0-9A-Za-z]{2}(?![0-9A-Za-z])", RegexOptions.Compiled);
    private static readonly Regex CaiValidoRegex = new(
        @"^[0-9A-F]{6}(?:-[0-9A-F]{6}){4}-[0-9A-F]{2}$", RegexOptions.Compiled);

    private const string FechaNumerica = @"(?<d>\d{1,2})[/\-.](?<m>\d{1,2})[/\-.](?<a>\d{4}|\d{2})(?!\d)";
    private const string FechaTexto = @"(?<d2>\d{1,2})[\s\-/]*(?:de\s+)?(?<mes>[A-Za-zñÑ]{3,10})\.?,?[\s\-/]*(?:de\s+|del\s+)?(?<a2>\d{4})(?!\d)";
    private const string FechaIso = @"(?<a3>\d{4})-(?<m3>\d{2})-(?<d3>\d{2})(?!\d)";

    private static readonly Regex FechaRegex = new(
        $@"(?<![\d])(?:{FechaIso}|{FechaNumerica}|{FechaTexto})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EtiquetaEmisionRegex = new(
        @"fecha\s+(?:de\s+)?emisi\w*[^\d\n]{0,15}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EtiquetaFechaRegex = new(
        @"fecha[^\d\n]{0,25}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TotalRegex = new(
        @"(?<![A-Za-z])(?:[A-Za-z]{1,2})?total\b[^\d\n]{0,60}" + Monto,
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // "ISV", "I.S.V.", "Impuesto sobre ventas" y sus lecturas típicas de OCR: 1SV, lSV, 18V, I5V...
    private static readonly Regex IsvRegex = new(
        @"(?:(?<![A-Za-z0-9])[I1l!|]{1,2}\.?[S5$8]\.?V\b|impuesto\s+sobre\s+ventas)[^\d\n]{0,60}(?:\d{1,2}(?:\.\d+)?\s*%[^\d\n]{0,60})?" + Monto,
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex IsvPorcentajeRegex = new(
        @"(?<!\d)(?:15|18)\s*%[^\d\n]{0,60}" + Monto, RegexOptions.Compiled);

    private static readonly Regex LineaItemRegex = new(
        @"^(?<desc>.*\p{L}.*?)\s+(?:L\.?\s*)?(?<monto>" + Monto + @")\s*[^\d\n]*$",
        RegexOptions.Compiled);

    // Líneas con monto que NO son artículos comprados.
    private static readonly string[] PalabrasNoItem =
    {
        "total", "subtotal", "sub total", "isv", "i.s.v", "impuesto", "descuento", "rebaja",
        "importe", "exonerado", "exento", "gravado", "efectivo", "cambio", "tarjeta", "propina",
        "pago", "saldo", "recibido"
    };

    // Un RTN en una línea con estas palabras es del cliente, no del proveedor.
    private static readonly string[] PalabrasCliente = { "cliente", "comprador", "adquirente", "receptor", "adquiriente" };

    private static readonly Dictionary<string, int> Meses = new()
    {
        ["ene"] = 1, ["feb"] = 2, ["mar"] = 3, ["abr"] = 4, ["may"] = 5, ["jun"] = 6,
        ["jul"] = 7, ["ago"] = 8, ["sep"] = 9, ["set"] = 9, ["oct"] = 10, ["nov"] = 11, ["dic"] = 12
    };

    private const int MaxItems = 50;

    public static DatosExtraidos Parsear(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return DatosExtraidos.Vacio;

        return new DatosExtraidos(
            NumeroFactura: ExtraerNumeroFactura(texto),
            CAI: ExtraerCai(texto),
            ProveedorRTN: ExtraerRtnProveedor(texto),
            FechaEmision: ExtraerFecha(texto),
            MontoTotal: MayorMonto(TotalRegex, texto),
            ISV: ExtraerIsv(texto),
            Items: ExtraerItems(texto));
    }

    // ------------------------------------------------------------ identificadores
    private static string? ExtraerNumeroFactura(string texto)
    {
        // La estructura 3-3-2-8 con guiones es tan específica que no hace falta más filtro.
        var m = FacturaRegex.Match(texto);
        return m.Success ? CorregirDigitos(m.Value) : null;
    }

    private static string? ExtraerRtnProveedor(string texto)
    {
        foreach (var linea in texto.Split('\n'))
        {
            var norm = TextoUtil.Normalizar(linea);
            if (PalabrasCliente.Any(p => norm.Contains(p))) continue; // RTN del cliente

            foreach (Match m in RtnRegex.Matches(linea))
            {
                if (MayoriaDigitos(m.Value))
                    return CorregirDigitos(m.Value).Replace("-", "");
            }
        }
        return null;
    }

    private static string? ExtraerCai(string texto)
    {
        foreach (Match m in CaiCandidatoRegex.Matches(texto))
        {
            var corregido = CorregirHex(m.Value);
            if (CaiValidoRegex.IsMatch(corregido)) return corregido;
        }
        return null;
    }

    /// <summary>Evita falsos positivos: al menos la mitad de los caracteres deben ser dígitos reales.</summary>
    private static bool MayoriaDigitos(string valor)
    {
        var sinGuiones = valor.Replace("-", "");
        return sinGuiones.Count(char.IsDigit) * 2 >= sinGuiones.Length;
    }

    private static string CorregirDigitos(string valor)
    {
        var sb = new StringBuilder(valor.Length);
        foreach (var c in valor)
        {
            sb.Append(c switch
            {
                'O' or 'o' => '0',
                'I' or 'l' => '1',
                'S' => '5',
                'B' => '8',
                _ => c
            });
        }
        return sb.ToString();
    }

    private static string CorregirHex(string valor)
    {
        var sb = new StringBuilder(valor.Length);
        foreach (var c in valor)
        {
            sb.Append(char.ToUpperInvariant(c) switch
            {
                'O' or 'Q' => '0',
                'I' or 'L' => '1',
                'S' => '5',
                'Z' => '2',
                'G' => '6',
                var x => x
            });
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------ fecha
    private static DateOnly? ExtraerFecha(string texto)
    {
        // 1) La fecha etiquetada como "Fecha de emisión" tiene prioridad; 2) cualquier "Fecha";
        // 3) la primera fecha válida del documento.
        foreach (var etiqueta in new[] { EtiquetaEmisionRegex, EtiquetaFechaRegex })
        {
            foreach (Match e in etiqueta.Matches(texto))
            {
                var resto = texto[(e.Index + e.Length)..];
                var m = FechaRegex.Match(resto[..Math.Min(resto.Length, 40)]);
                if (m.Success && m.Index <= 3 && TryFecha(m, out var f)) return f;
            }
        }

        foreach (Match m in FechaRegex.Matches(texto))
            if (TryFecha(m, out var f)) return f;

        return null;
    }

    private static bool TryFecha(Match m, out DateOnly fecha)
    {
        fecha = default;
        int anio, mes, dia;

        if (m.Groups["a3"].Success)
        {
            anio = int.Parse(m.Groups["a3"].Value);
            mes = int.Parse(m.Groups["m3"].Value);
            dia = int.Parse(m.Groups["d3"].Value);
        }
        else if (m.Groups["a"].Success)
        {
            dia = int.Parse(m.Groups["d"].Value);
            mes = int.Parse(m.Groups["m"].Value);
            anio = int.Parse(m.Groups["a"].Value);
            if (anio < 100) anio += 2000; // 15/10/26
        }
        else if (m.Groups["a2"].Success)
        {
            var clave = TextoUtil.Normalizar(m.Groups["mes"].Value);
            if (clave.Length < 3 || !Meses.TryGetValue(clave[..3], out mes)) return false;
            dia = int.Parse(m.Groups["d2"].Value);
            anio = int.Parse(m.Groups["a2"].Value);
        }
        else return false;

        if (anio < 2000 || anio > 2100 || mes < 1 || mes > 12 || dia < 1 || dia > DateTime.DaysInMonth(anio, mes))
            return false;

        fecha = new DateOnly(anio, mes, dia);
        return true;
    }

    // ------------------------------------------------------------ montos e ítems
    private static decimal? MayorMonto(Regex regex, string texto)
    {
        decimal? mayor = null;
        foreach (Match m in regex.Matches(texto))
        {
            var valor = ParseMonto(m.Groups[m.Groups.Count - 1].Value);
            if (valor is not null && (mayor is null || valor > mayor)) mayor = valor;
        }
        return mayor;
    }

    private static decimal? ExtraerIsv(string texto)
    {
        var etiquetado = MayorMonto(IsvRegex, texto);
        if (etiquetado is not null) return etiquetado;

        decimal? mayor = null;
        foreach (var linea in texto.Split('\n'))
        {
            var norm = TextoUtil.Normalizar(linea);
            if (norm.Contains("descuento") || norm.Contains("rebaja")) continue;

            var monto = MayorMonto(IsvPorcentajeRegex, linea);
            if (monto is not null && (mayor is null || monto > mayor)) mayor = monto;
        }
        return mayor;
    }

    private static IReadOnlyList<ItemExtraido> ExtraerItems(string texto)
    {
        var items = new List<ItemExtraido>();
        foreach (var lineaCruda in texto.Split('\n'))
        {
            if (items.Count >= MaxItems) break;

            var linea = lineaCruda.Trim();
            if (linea.Length < 5) continue;

            var m = LineaItemRegex.Match(linea);
            if (!m.Success) continue;

            var descripcion = m.Groups["desc"].Value.Trim();
            if (descripcion.Length < 3) continue;
            if (descripcion.Contains('%')) continue; // líneas de impuesto o descuento ("ISV 15%", "Desc. 10%")

            var norm = TextoUtil.Normalizar(descripcion);
            if (PalabrasNoItem.Any(p => norm.Contains(p))) continue;

            var monto = ParseMonto(m.Groups["monto"].Value);
            if (monto is null) continue;

            items.Add(new ItemExtraido(
                descripcion.Length > 200 ? descripcion[..200] : descripcion, monto.Value));
        }
        return items;
    }

    private static decimal? ParseMonto(string texto) =>
        decimal.TryParse(texto.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
            ? v : null;
}
