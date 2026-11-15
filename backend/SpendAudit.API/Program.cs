using System.Text;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SpendAudit.API.Middlewares;
using SpendAudit.API.Services;
using SpendAudit.Application;
using SpendAudit.Application.Common.Models;
using SpendAudit.Application.Interfaces;
using SpendAudit.Infrastructure;
using SpendAudit.Infrastructure.Persistence;
using SpendAudit.Infrastructure.Services;

// 12-Factor: la configuración sale del entorno. En local se lee de .env (si existe) sin pisar
// las variables reales (en Docker/Render llegan por el entorno y .env no existe).
if (File.Exists(".env")) Env.NoClobber().Load();

var builder = WebApplication.CreateBuilder(args);

// ---------- Capas ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantService, HttpCurrentTenantService>();

// ---------- Controladores y formato de errores ----------
builder.Services
    .AddControllers(o =>
    {
        // La validación semántica la hace FluentValidation; no queremos el [Required] implícito en inglés.
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o =>
    {
        // Errores de binding (JSON mal formado, tipos incorrectos) con la MISMA forma que FluentValidation.
        o.InvalidModelStateResponseFactory = ctx => new BadRequestObjectResult(
            ctx.ModelState
                .Where(kv => kv.Value is { Errors.Count: > 0 })
                .SelectMany(kv => kv.Value!.Errors.Select(e => new
                {
                    propertyName = kv.Key,
                    errorMessage = string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? "El valor enviado no es válido."
                        : e.ErrorMessage
                }))
                .ToList());
    });

// ---------- Autenticación JWT ----------
var jwt = builder.Configuration.GetSection(JwtSettings.Seccion).Get<JwtSettings>()!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // conserva los nombres "sub", "empresaId", "role" tal cual
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimsNombres.UsuarioId,
            RoleClaimType = ClaimsNombres.Rol
        };
        o.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { mensaje = "Debes iniciar sesión para acceder a este recurso." });
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsJsonAsync(new { mensaje = "No tienes permisos para realizar esta acción." });
            }
        };
    });
builder.Services.AddAuthorization();

// ---------- CORS (el frontend de la Fase 3) ----------
var origenes = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
    p.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod()));

// ---------- Documentación ----------
builder.Services.AddOpenApi();

var app = builder.Build();

// Opcional: aplica las migraciones al arrancar (útil en Docker y en Render).
if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<SpendAuditDbContext>().Database.Migrate();
}

// El middleware de excepciones va PRIMERO para envolver a todo lo demás.
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Docs:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // UI en /scalar/v1
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { estado = "ok" })).AllowAnonymous();
app.MapControllers();

app.Run();
