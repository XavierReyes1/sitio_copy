using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendAudit.API.Contracts;
using SpendAudit.Application.Common.Models;
using SpendAudit.Application.Features.Gastos.DTOs;
using SpendAudit.Application.Features.Gastos.Services;

namespace SpendAudit.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/gastos")]
public class GastosController : ControllerBase
{
    private const string RolesQueGastan = $"{Roles.Empleado},{Roles.Admin}";
    private const long LimiteFormulario = LimitesArchivo.TamanoMaximoBytes + 1024 * 1024; // archivo + campos

    private readonly IGastosService _gastos;
    public GastosController(IGastosService gastos) => _gastos = gastos;

    /// <summary>Sube un comprobante (PDF/JPG/PNG) al álbum de borradores del usuario.</summary>
    [HttpPost("borrador")]
    [Authorize(Roles = RolesQueGastan)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteFormulario)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteFormulario)]
    [ProducesResponseType(typeof(ComprobanteResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearBorrador([FromForm] CreateBorradorForm form, CancellationToken ct)
    {
        var dto = new CreateBorradorRequestDto
        {
            ArchivoNombre = form.Archivo?.FileName ?? string.Empty,
            ArchivoTamano = form.Archivo?.Length ?? 0,
            TextoOcr = form.TextoOcr,
            NumeroFactura = form.NumeroFactura,
            CAI = form.CAI,
            ProveedorRTN = form.ProveedorRTN,
            FechaEmision = form.FechaEmision,
            MontoTotal = form.MontoTotal,
            ISV = form.ISV
        };

        await using var contenido = form.Archivo?.OpenReadStream() ?? Stream.Null;
        var creado = await _gastos.CrearBorradorAsync(dto, contenido, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    [HttpGet("album")]
    [Authorize(Roles = RolesQueGastan)]
    public async Task<ActionResult<List<ComprobanteResponseDto>>> Album(CancellationToken ct) =>
        Ok(await _gastos.ObtenerAlbumAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ComprobanteResponseDto>> Obtener(Guid id, CancellationToken ct) =>
        Ok(await _gastos.ObtenerAsync(id, ct));

    /// <summary>Descarga el archivo original. Pasa por la API (no es estático) para respetar el aislamiento.</summary>
    [HttpGet("{id:guid}/archivo")]
    public async Task<IActionResult> Archivo(Guid id, CancellationToken ct)
    {
        var archivo = await _gastos.ObtenerArchivoAsync(id, ct);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(archivo.Contenido, archivo.ContentType);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RolesQueGastan)]
    public async Task<ActionResult<ComprobanteResponseDto>> Actualizar(
        Guid id, UpdateBorradorRequestDto dto, CancellationToken ct) =>
        Ok(await _gastos.ActualizarBorradorAsync(id, dto, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RolesQueGastan)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await _gastos.EliminarBorradorAsync(id, ct);
        return NoContent();
    }

    /// <summary>Audita los borradores seleccionados contra la política de la empresa y devuelve el semáforo.</summary>
    [HttpPost("liquidar-lote")]
    [Authorize(Roles = RolesQueGastan)]
    public async Task<ActionResult<ResultadoLoteDto>> LiquidarLote(LiquidarLoteRequestDto dto, CancellationToken ct) =>
        Ok(await _gastos.LiquidarLoteAsync(dto, ct));

    [HttpGet("lotes")]
    public async Task<ActionResult<List<LoteResumenDto>>> Lotes(CancellationToken ct) =>
        Ok(await _gastos.ObtenerLotesAsync(ct));

    [HttpGet("lotes/{id:guid}")]
    public async Task<ActionResult<ResultadoLoteDto>> Lote(Guid id, CancellationToken ct) =>
        Ok(await _gastos.ObtenerLoteAsync(id, ct));
}
