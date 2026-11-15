using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpendAudit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NombreRazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RTN = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    EstadoSuscripcion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoliticasGasto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiasVigenciaMaxima = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticasGasto", x => x.Id);
                    table.CheckConstraint("CK_PoliticasGasto_Vigencia", "\"DiasVigenciaMaxima\" BETWEEN 1 AND 365");
                    table.ForeignKey(
                        name: "FK_PoliticasGasto_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Puestos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Puestos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Puestos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TerminosRestringidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PoliticaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Termino = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerminosRestringidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerminosRestringidos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TerminosRestringidos_PoliticasGasto_PoliticaId",
                        column: x => x.PoliticaId,
                        principalTable: "PoliticasGasto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopesPuesto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PoliticaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PuestoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopeDiarioHNL = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopesPuesto", x => x.Id);
                    table.CheckConstraint("CK_TopesPuesto_Tope", "\"TopeDiarioHNL\" > 0");
                    table.ForeignKey(
                        name: "FK_TopesPuesto_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TopesPuesto_PoliticasGasto_PoliticaId",
                        column: x => x.PoliticaId,
                        principalTable: "PoliticasGasto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopesPuesto_Puestos_PuestoId",
                        column: x => x.PuestoId,
                        principalTable: "Puestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PuestoId = table.Column<Guid>(type: "uuid", nullable: true),
                    NombreCompleto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.CheckConstraint("CK_Usuarios_Rol", "\"Rol\" IN ('Admin','Empleado','Auditor')");
                    table.ForeignKey(
                        name: "FK_Usuarios_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Usuarios_Puestos_PuestoId",
                        column: x => x.PuestoId,
                        principalTable: "Puestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LotesLiquidacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FechaLiquidacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesLiquidacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotesLiquidacion_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LotesLiquidacion_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comprobantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    TipoArchivo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NombreArchivoOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UrlArchivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    NumeroFactura = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CAI = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ProveedorRTN = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: true),
                    MontoTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ISV = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    TextoExtraido = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ResultadoAuditoria = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FechaCarga = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobantes", x => x.Id);
                    table.CheckConstraint("CK_Comprobantes_Estado", "\"Estado\" IN ('Borrador','Liquidado')");
                    table.CheckConstraint("CK_Comprobantes_Monto", "\"MontoTotal\" IS NULL OR \"MontoTotal\" >= 0");
                    table.CheckConstraint("CK_Comprobantes_Resultado", "\"ResultadoAuditoria\" IS NULL OR \"ResultadoAuditoria\" IN ('Verde','Amarillo','Rojo')");
                    table.ForeignKey(
                        name: "FK_Comprobantes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comprobantes_LotesLiquidacion_LoteId",
                        column: x => x.LoteId,
                        principalTable: "LotesLiquidacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Comprobantes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetalleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComprobanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EsRestringido = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetalleItems_Comprobantes_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetalleItems_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HallazgosAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComprobanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Regla = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Severidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HallazgosAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HallazgosAuditoria_Comprobantes_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HallazgosAuditoria_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_EmpresaId",
                table: "Comprobantes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_EmpresaId_UsuarioId_Estado",
                table: "Comprobantes",
                columns: new[] { "EmpresaId", "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_EmpresaId_UsuarioId_FechaEmision",
                table: "Comprobantes",
                columns: new[] { "EmpresaId", "UsuarioId", "FechaEmision" });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_LoteId",
                table: "Comprobantes",
                column: "LoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_UsuarioId",
                table: "Comprobantes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UX_Comprobantes_FacturaAceptada",
                table: "Comprobantes",
                columns: new[] { "EmpresaId", "ProveedorRTN", "NumeroFactura" },
                unique: true,
                filter: "\"ResultadoAuditoria\" IN ('Verde','Amarillo')");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleItems_ComprobanteId",
                table: "DetalleItems",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleItems_EmpresaId",
                table: "DetalleItems",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_RTN",
                table: "Empresas",
                column: "RTN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HallazgosAuditoria_ComprobanteId",
                table: "HallazgosAuditoria",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_HallazgosAuditoria_EmpresaId",
                table: "HallazgosAuditoria",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesLiquidacion_EmpresaId",
                table: "LotesLiquidacion",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesLiquidacion_EmpresaId_UsuarioId_FechaLiquidacion",
                table: "LotesLiquidacion",
                columns: new[] { "EmpresaId", "UsuarioId", "FechaLiquidacion" });

            migrationBuilder.CreateIndex(
                name: "IX_LotesLiquidacion_UsuarioId",
                table: "LotesLiquidacion",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UX_PoliticasGasto_Activa",
                table: "PoliticasGasto",
                column: "EmpresaId",
                unique: true,
                filter: "\"Activa\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_Puestos_EmpresaId",
                table: "Puestos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Puestos_EmpresaId_Nombre",
                table: "Puestos",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TerminosRestringidos_EmpresaId",
                table: "TerminosRestringidos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_TerminosRestringidos_PoliticaId_Termino",
                table: "TerminosRestringidos",
                columns: new[] { "PoliticaId", "Termino" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopesPuesto_EmpresaId",
                table: "TopesPuesto",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_TopesPuesto_PoliticaId_PuestoId",
                table: "TopesPuesto",
                columns: new[] { "PoliticaId", "PuestoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopesPuesto_PuestoId",
                table: "TopesPuesto",
                column: "PuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmpresaId",
                table: "Usuarios",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmpresaId_Rol",
                table: "Usuarios",
                columns: new[] { "EmpresaId", "Rol" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_PuestoId",
                table: "Usuarios",
                column: "PuestoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetalleItems");

            migrationBuilder.DropTable(
                name: "HallazgosAuditoria");

            migrationBuilder.DropTable(
                name: "TerminosRestringidos");

            migrationBuilder.DropTable(
                name: "TopesPuesto");

            migrationBuilder.DropTable(
                name: "Comprobantes");

            migrationBuilder.DropTable(
                name: "PoliticasGasto");

            migrationBuilder.DropTable(
                name: "LotesLiquidacion");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Puestos");

            migrationBuilder.DropTable(
                name: "Empresas");
        }
    }
}
