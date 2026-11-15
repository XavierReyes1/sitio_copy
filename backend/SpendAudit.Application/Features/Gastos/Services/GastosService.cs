using FluentValidation;
using FluentValidation.Results;
using SpendAudit.Application.Common.Exceptions;
using SpendAudit.Application.Common.Models;
using SpendAudit.Application.Features.Gastos.DTOs;
using SpendAudit.Application.Features.Gastos.Mappings;
using SpendAudit.Application.Interfaces;
using SpendAudit.Domain.Entities;
using SpendAudit.Domain.Enums;

namespace SpendAudit.Application.Features.Gastos.Services;

public interface IGastosService
{
    Task<ComprobanteResponseDto> CrearBorradorAsync(CreateBorradorRequestDto dto, Stream archivo, CancellationToken ct = default);
    Task<List<ComprobanteResponseDto>> ObtenerAlbumAsync(CancellationToken ct = default);
    Task<ComprobanteResponseDto> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<ArchivoDescarga> ObtenerArchivoAsync(Guid id, CancellationToken ct = default);
    Task<ComprobanteResponseDto> ActualizarBorradorAsync(Guid id, UpdateBorradorRequestDto dto, CancellationToken ct = default);
    Task EliminarBorradorAsync(Guid id, CancellationToken ct = default);
    Task<ResultadoLoteDto> LiquidarLoteAsync(LiquidarLoteRequestDto dto, CancellationToken ct = default);
    Task<List<LoteResumenDto>> ObtenerLotesAsync(CancellationToken ct = default);
    Task<ResultadoLoteDto> ObtenerLoteAsync(Guid id, CancellationToken ct = default);
}

public class GastosService : IGastosService
{
    private readonly IComprobanteRepository _comprobantes;
    private readonly ILoteRepository _lotes;
    private readonly IPoliticaRepository _politicas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentTenantService _tenant;
    private readonly IFileStorageService _storage;
    private readonly IExtractorTextoService _extractor;
    private readonly MotorAuditoria _motor;
    private readonly IValidator<CreateBorradorRequestDto> _createValidator;
    private readonly IValidator<UpdateBorradorRequestDto> _updateValidator;
    private readonly IValidator<LiquidarLoteRequestDto> _liquidarValidator;

    public GastosService(
        IComprobanteRepository comprobantes, ILoteRepository lotes, IPoliticaRepository politicas,
        IUsuarioRepository usuarios, IUnitOfWork uow, ICurrentTenantService tenant,
        IFileStorageService storage, IExtractorTextoService extractor, MotorAuditoria motor,
        IValidator<CreateBorradorRequestDto> createValidator,
        IValidator<UpdateBorradorRequestDto> updateValidator,
        IValidator<LiquidarLoteRequestDto> liquidarValidator)
    {
        _comprobantes = comprobantes;
        _lotes = lotes;
        _politicas = politicas;
        _usuarios = usuarios;
        _uow = uow;
        _tenant = tenant;
        _storage = storage;
        _extractor = extractor;
        _motor = motor;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _liquidarValidator = liquidarValidator;
    }

    // ---------------------------------------------------------------- HU-04 / HU-07
    public async Task<ComprobanteResponseDto> CrearBorradorAsync(
        CreateBorradorRequestDto dto, Stream archivo, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(dto, ct);
        var (empresaId, usuarioId) = RequerirIdentidad();

        // Copiamos a memoria (máx. 10 MB, ya validado) para poder leer la firma y luego extraer el texto.
        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);
        if (memoria.Length > LimitesArchivo.TamanoMaximoBytes)
            throw new ValidationException(new[] { new ValidationFailure("Archivo", "El archivo no puede pesar más de 10 MB.") });

        var extension = Path.GetExtension(dto.ArchivoNombre).ToLowerInvariant();
        var tipo = FirmaArchivo.Detectar(memoria.GetBuffer().AsSpan(0, (int)Math.Min(memoria.Length, 16)), extension)
            ?? throw new ValidationException(new[]
            {
                new ValidationFailure("Archivo", "El contenido del archivo no corresponde a su extensión.")
            });

        memoria.Position = 0;
        var clave = await _storage.GuardarAsync(memoria, extension, empresaId, usuarioId, ct);

        try
        {
            // Extracción automática (solo PDFs con texto; en imágenes no hay OCR en el MVP).
            memoria.Position = 0;
            var texto = await _extractor.ExtraerTextoAsync(memoria, tipo, ct);

            // Fotos: el OCR lo hace el navegador y manda el texto; pasa por el MISMO parser que los PDFs.
            // Es información declarada por el cliente: sirve para ahorrar digitación, pero el empleado igual
            // puede editar los campos, así que no se trata como una fuente de confianza distinta.
            if (tipo == TipoArchivo.Imagen && !string.IsNullOrWhiteSpace(dto.TextoOcr))
                texto = dto.TextoOcr;

            var extraidos = ParserComprobante.Parsear(texto);

            // Lo que el empleado capturó a mano tiene prioridad sobre lo extraído.
            var comprobante = new Comprobante
            {
                EmpresaId = empresaId,
                UsuarioId = usuarioId,
                TipoArchivo = tipo,
                NombreArchivoOriginal = Path.GetFileName(dto.ArchivoNombre),
                UrlArchivo = clave,
                NumeroFactura = Limpiar(dto.NumeroFactura) ?? extraidos.NumeroFactura,
                CAI = Limpiar(dto.CAI)?.ToUpperInvariant() ?? extraidos.CAI,
                ProveedorRTN = Limpiar(dto.ProveedorRTN) ?? extraidos.ProveedorRTN,
                FechaEmision = dto.FechaEmision ?? extraidos.FechaEmision,
                MontoTotal = dto.MontoTotal ?? extraidos.MontoTotal,
                ISV = dto.ISV ?? extraidos.ISV,
                TextoExtraido = Truncar(texto, 50_000),
                Estado = EstadoComprobante.Borrador
            };
            foreach (var item in extraidos.Items)
            {
                comprobante.Items.Add(new DetalleItem
                {
                    EmpresaId = empresaId,
                    Descripcion = item.Descripcion,
                    Subtotal = item.Subtotal
                });
            }

            _comprobantes.Add(comprobante);
            await _uow.SaveChangesAsync(ct);
            return ComprobanteMapper.ToDto(comprobante);
        }
        catch
        {
            // Si no se pudo registrar, no dejamos el archivo huérfano.
            await _storage.EliminarAsync(clave, CancellationToken.None);
            throw;
        }
    }

    // ---------------------------------------------------------------- HU-05
    public async Task<List<ComprobanteResponseDto>> ObtenerAlbumAsync(CancellationToken ct = default)
    {
        var (_, usuarioId) = RequerirIdentidad();
        var borradores = await _comprobantes.GetAlbumAsync(usuarioId, ct);
        return borradores.Select(ComprobanteMapper.ToDto).ToList();
    }

    public async Task<ComprobanteResponseDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var comprobante = await CargarVisibleAsync(id, ct);
        return ComprobanteMapper.ToDto(comprobante);
    }

    public async Task<ArchivoDescarga> ObtenerArchivoAsync(Guid id, CancellationToken ct = default)
    {
        var comprobante = await CargarVisibleAsync(id, ct);
        var stream = await _storage.AbrirAsync(comprobante.UrlArchivo, ct)
            ?? throw new NotFoundException("El archivo del comprobante ya no está disponible.");
        return new ArchivoDescarga(stream, FirmaArchivo.ContentType(comprobante.NombreArchivoOriginal),
            comprobante.NombreArchivoOriginal);
    }

    // ---------------------------------------------------------------- HU-06
    public async Task<ComprobanteResponseDto> ActualizarBorradorAsync(
        Guid id, UpdateBorradorRequestDto dto, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(dto, ct);
        var comprobante = await CargarBorradorPropioAsync(id, ct);

        comprobante.NumeroFactura = Limpiar(dto.NumeroFactura);
        comprobante.CAI = Limpiar(dto.CAI)?.ToUpperInvariant();
        comprobante.ProveedorRTN = Limpiar(dto.ProveedorRTN);
        comprobante.FechaEmision = dto.FechaEmision;
        comprobante.MontoTotal = dto.MontoTotal;
        comprobante.ISV = dto.ISV;

        await _uow.SaveChangesAsync(ct);
        return ComprobanteMapper.ToDto(comprobante);
    }

    public async Task EliminarBorradorAsync(Guid id, CancellationToken ct = default)
    {
        var comprobante = await CargarBorradorPropioAsync(id, ct);
        var clave = comprobante.UrlArchivo;

        _comprobantes.Remove(comprobante);
        await _uow.SaveChangesAsync(ct);
        await _storage.EliminarAsync(clave, CancellationToken.None);
    }

    // ---------------------------------------------------------------- HU-09
    public async Task<ResultadoLoteDto> LiquidarLoteAsync(LiquidarLoteRequestDto dto, CancellationToken ct = default)
    {
        await _liquidarValidator.ValidateAndThrowAsync(dto, ct);
        var (empresaId, usuarioId) = RequerirIdentidad();

        // 1) Los comprobantes deben ser del usuario (el filtro global ya descarta los de otras empresas).
        var seleccionados = await _comprobantes.GetByIdsAsync(dto.ComprobanteIds, usuarioId, ct);
        var noEncontrados = dto.ComprobanteIds.Except(seleccionados.Select(c => c.Id)).ToList();
        if (noEncontrados.Count > 0)
            throw new NotFoundException($"No se encontraron {noEncontrados.Count} de los comprobantes indicados.");

        // 2) Solo borradores y con los datos mínimos para poder auditar.
        var problemas = new List<ValidationFailure>();
        foreach (var c in seleccionados)
        {
            var etiqueta = c.NumeroFactura ?? c.NombreArchivoOriginal;
            if (c.Estado != EstadoComprobante.Borrador)
                problemas.Add(new ValidationFailure("ComprobanteIds", $"El comprobante {etiqueta} ya fue liquidado."));
            if (c.FechaEmision is null)
                problemas.Add(new ValidationFailure("ComprobanteIds", $"Al comprobante {etiqueta} le falta la fecha de emisión."));
            if (c.MontoTotal is null)
                problemas.Add(new ValidationFailure("ComprobanteIds", $"Al comprobante {etiqueta} le falta el monto total."));
            if (string.IsNullOrWhiteSpace(c.NumeroFactura) || string.IsNullOrWhiteSpace(c.ProveedorRTN))
                problemas.Add(new ValidationFailure("ComprobanteIds",
                    $"Al comprobante {etiqueta} le falta el número de factura o el RTN del proveedor."));
        }
        if (problemas.Count > 0) throw new ValidationException(problemas);

        // 3) Contexto de auditoría: política de la empresa, puesto del usuario, historial.
        var politica = await _politicas.GetActivaAsync(paraEditar: false, ct)
            ?? throw new ConflictException("Tu empresa no tiene una política de gastos activa.");
        var usuario = await _usuarios.GetByIdAsync(usuarioId, ct)
            ?? throw new UnauthorizedException("Tu sesión ya no es válida.");

        var numeros = seleccionados.Select(c => c.NumeroFactura!.Trim()).Distinct().ToList();
        var fechas = seleccionados.Select(c => c.FechaEmision!.Value).Distinct().ToList();

        var contexto = new ContextoAuditoria
        {
            DiasVigenciaMaxima = politica.DiasVigenciaMaxima,
            TerminosRestringidos = politica.Terminos.Select(t => t.Termino).ToList(),
            TopeDiarioHNL = usuario.PuestoId is { } puestoId
                ? politica.Topes.FirstOrDefault(t => t.PuestoId == puestoId)?.TopeDiarioHNL
                : null,
            Hoy = HoraLocal.Hoy(),
            FacturasAceptadas = await _comprobantes.GetFacturasAceptadasAsync(numeros, ct),
            TotalesPreviosPorDia = await _comprobantes.GetTotalesPorDiaAsync(usuarioId, fechas, ct)
        };

        // 4) Auditoría y persistencia: todo en un solo SaveChanges (transacción).
        var resultados = _motor.Auditar(seleccionados, contexto).ToDictionary(r => r.ComprobanteId);

        var lote = new LoteLiquidacion
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion)
                ? $"Lote {DateTime.UtcNow.AddHours(-6):yyyy-MM-dd HH:mm}"
                : dto.Descripcion.Trim(),
            FechaLiquidacion = DateTime.UtcNow
        };
        _lotes.Add(lote);

        foreach (var c in seleccionados)
        {
            var r = resultados[c.Id];
            c.LoteId = lote.Id;
            c.Estado = EstadoComprobante.Liquidado;
            c.ResultadoAuditoria = r.Resultado;

            foreach (var h in r.Hallazgos)
            {
                c.Hallazgos.Add(new HallazgoAuditoria
                {
                    EmpresaId = empresaId,
                    Regla = h.Regla,
                    Severidad = h.Severidad,
                    Detalle = h.Detalle
                });
            }
            foreach (var item in c.Items.Where(i => r.ItemsRestringidos.Contains(i.Id)))
                item.EsRestringido = true;

            lote.Comprobantes.Add(c);
        }

        await _uow.SaveChangesAsync(ct);
        return ComprobanteMapper.ToDto(lote);
    }

    // ---------------------------------------------------------------- HU-10
    public async Task<List<LoteResumenDto>> ObtenerLotesAsync(CancellationToken ct = default)
    {
        var (_, usuarioId) = RequerirIdentidad();
        var lotes = await _lotes.GetAllAsync(VeTodaLaEmpresa() ? null : usuarioId, ct);
        return lotes.Select(ComprobanteMapper.ToResumenDto).ToList();
    }

    public async Task<ResultadoLoteDto> ObtenerLoteAsync(Guid id, CancellationToken ct = default)
    {
        var (_, usuarioId) = RequerirIdentidad();
        var lote = await _lotes.GetByIdAsync(id, ct);

        // 404 (no 403): para quien no puede verlo, el lote simplemente no existe.
        if (lote is null || (!VeTodaLaEmpresa() && lote.UsuarioId != usuarioId))
            throw new NotFoundException("El lote no existe.");

        return ComprobanteMapper.ToDto(lote);
    }

    // ---------------------------------------------------------------- helpers
    private (Guid EmpresaId, Guid UsuarioId) RequerirIdentidad()
    {
        if (_tenant.EmpresaId is { } e && _tenant.UsuarioId is { } u) return (e, u);
        throw new UnauthorizedException("Sesión inválida.");
    }

    private bool VeTodaLaEmpresa() => _tenant.Rol is Roles.Admin or Roles.Auditor;

    /// <summary>Empleado: solo los suyos. Admin/Auditor: todos los de su empresa.</summary>
    private async Task<Comprobante> CargarVisibleAsync(Guid id, CancellationToken ct)
    {
        var (_, usuarioId) = RequerirIdentidad();
        var c = await _comprobantes.GetByIdAsync(id, paraEditar: false, ct);
        if (c is null || (!VeTodaLaEmpresa() && c.UsuarioId != usuarioId))
            throw new NotFoundException("El comprobante no existe.");
        return c;
    }

    private async Task<Comprobante> CargarBorradorPropioAsync(Guid id, CancellationToken ct)
    {
        var (_, usuarioId) = RequerirIdentidad();
        var c = await _comprobantes.GetByIdAsync(id, paraEditar: true, ct);
        if (c is null || c.UsuarioId != usuarioId)
            throw new NotFoundException("El comprobante no existe.");
        if (c.Estado != EstadoComprobante.Borrador)
            throw new ConflictException("Solo se pueden modificar comprobantes en estado Borrador.");
        return c;
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? Truncar(string? valor, int max) =>
        valor is null ? null : valor.Length <= max ? valor : valor[..max];
}
