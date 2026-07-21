using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInformesFase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Causas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Caratula = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NroPiezaSumarial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CircunscripcionJudicial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Causas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Informes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdRegistro = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaAnalisis = table.Column<DateOnly>(type: "date", nullable: false),
                    Relato = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    CasoAnalisisId = table.Column<Guid>(type: "uuid", nullable: false),
                    CausaId = table.Column<Guid>(type: "uuid", nullable: true),
                    DependenciaDestinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PdfPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Informes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InformeAnalistas",
                columns: table => new
                {
                    InformeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InformeAnalistas", x => new { x.InformeId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_InformeAnalistas_Informes_InformeId",
                        column: x => x.InformeId,
                        principalTable: "Informes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Informes_CasoAnalisisId",
                table: "Informes",
                column: "CasoAnalisisId");

            migrationBuilder.CreateIndex(
                name: "IX_Informes_CausaId",
                table: "Informes",
                column: "CausaId");

            migrationBuilder.CreateIndex(
                name: "IX_Informes_DependenciaDestinoId",
                table: "Informes",
                column: "DependenciaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Informes_IdRegistro",
                table: "Informes",
                column: "IdRegistro",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Causas");

            migrationBuilder.DropTable(
                name: "InformeAnalistas");

            migrationBuilder.DropTable(
                name: "Informes");
        }
    }
}
