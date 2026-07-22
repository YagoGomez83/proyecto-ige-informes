using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenciaFase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evidencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroImagen = table.Column<int>(type: "integer", nullable: false),
                    InformeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CamaraId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaHoraCaptura = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImagenPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VehiculoIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    PersonaIds = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidencias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evidencias_CamaraId",
                table: "Evidencias",
                column: "CamaraId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidencias_InformeId",
                table: "Evidencias",
                column: "InformeId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidencias_InformeId_NumeroImagen",
                table: "Evidencias",
                columns: new[] { "InformeId", "NumeroImagen" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Evidencias");
        }
    }
}
