using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SpendAudit.Application.Features.Auth.Services;
using SpendAudit.Application.Features.Gastos.Services;
using SpendAudit.Application.Features.Politicas.Services;
using SpendAudit.Application.Features.Usuarios.Services;

namespace SpendAudit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Al primer error de una regla se detiene: evita ejecutar Must() sobre valores nulos.
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

        // Registra todos los AbstractValidator<T> del ensamblado.
        services.AddValidatorsFromAssemblyContaining<IAuthService>();

        services.AddSingleton<MotorAuditoria>(); // sin estado: es seguro compartirlo

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuariosService, UsuariosService>();
        services.AddScoped<IPoliticasService, PoliticasService>();
        services.AddScoped<IGastosService, GastosService>();

        return services;
    }
}
