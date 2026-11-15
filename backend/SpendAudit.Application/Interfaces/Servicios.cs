using SpendAudit.Domain.Entities;
using SpendAudit.Domain.Enums;

namespace SpendAudit.Application.Interfaces;

/// <summary>Identidad del usuario actual, leída de los Claims del JWT.</summary>
public interface ICurrentTenantService
{
    Guid? EmpresaId { get; }
    Guid? UsuarioId { get; }
    string? Rol { get; }
}

/// <summary>Confirma en una sola transacción todo lo que los repositorios dejaron pendiente.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string passwordPlano);
    bool Verify(string passwordPlano, string hash);
}

public record TokenGenerado(string Token, DateTime ExpiraEn);

public interface IJwtTokenService
{
    TokenGenerado Generar(Usuario usuario);
}

/// <summary>Devuelve el texto de un comprobante, o null si no se puede extraer (imágenes, PDFs escaneados).</summary>
public interface IExtractorTextoService
{
    Task<string?> ExtraerTextoAsync(Stream contenido, TipoArchivo tipo, CancellationToken ct = default);
}

public interface IFileStorageService
{
    /// <summary>Guarda el archivo y devuelve su clave interna de almacenamiento.</summary>
    Task<string> GuardarAsync(Stream contenido, string extension, Guid empresaId, Guid usuarioId, CancellationToken ct = default);
    Task<Stream?> AbrirAsync(string clave, CancellationToken ct = default);
    Task EliminarAsync(string clave, CancellationToken ct = default);
}
