using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAlertaYConcurrenciaInforme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alertas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    VehiculoId = table.Column<Guid>(type: "uuid", nullable: true),
                    PersonaId = table.Column<Guid>(type: "uuid", nullable: true),
                    InformeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InformePrevioId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaGeneracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Atendida = table.Column<bool>(type: "boolean", nullable: false),
                    AtendidaPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaAtencion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_Atendida",
                table: "Alertas",
                column: "Atendida");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_InformeId",
                table: "Alertas",
                column: "InformeId");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_PersonaId",
                table: "Alertas",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_VehiculoId",
                table: "Alertas",
                column: "VehiculoId");

            // El token de concurrencia optimista de InformeConfiguration
            // mapea la columna de sistema xmin ya existente en Postgres — no
            // requiere ningún DDL nuevo (a diferencia de una columna
            // RowVersion tradicional), por eso esta migración no toca
            // "Informes".
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alertas");
        }
    }
}
