using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpendAudit.Application.Features.Auth.DTOs;
using SpendAudit.Application.Features.Auth.Services;

namespace SpendAudit.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Crea una empresa nueva (tenant) y su primer usuario Administrador.</summary>
    [HttpPost("registro-empresa")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegistrarEmpresa(RegistrarEmpresaRequestDto dto, CancellationToken ct)
    {
        var resultado = await _auth.RegistrarEmpresaAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>Devuelve un JWT con los Claims sub, empresaId y role.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto, CancellationToken ct) =>
        Ok(await _auth.LoginAsync(dto, ct));
}
