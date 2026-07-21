using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCasoAnalisisFase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CasosAnalisis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Resultado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NroLlamado911 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DependenciaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoIncidenteId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehiculoInvolucradoTexto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ElementoSustraido = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CamarasAnalizadasTexto = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasosAnalisis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dependencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependencias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposIncidente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposIncidente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CasoAnalistas",
                columns: table => new
                {
                    CasoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasoAnalistas", x => new { x.CasoId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_CasoAnalistas_CasosAnalisis_CasoId",
                        column: x => x.CasoId,
                        principalTable: "CasosAnalisis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CasosAnalisis_DependenciaId",
                table: "CasosAnalisis",
                column: "DependenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_CasosAnalisis_TipoIncidenteId",
                table: "CasosAnalisis",
                column: "TipoIncidenteId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposIncidente_Codigo",
                table: "TiposIncidente",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CasoAnalistas");

            migrationBuilder.DropTable(
                name: "Dependencias");

            migrationBuilder.DropTable(
                name: "TiposIncidente");

            migrationBuilder.DropTable(
                name: "CasosAnalisis");
        }
    }
}
