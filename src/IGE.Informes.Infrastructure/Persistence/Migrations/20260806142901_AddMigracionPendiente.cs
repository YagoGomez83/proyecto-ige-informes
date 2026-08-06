using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMigracionPendiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MigracionesPendientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdRegistro = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PdfPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DependenciaDestinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioMigradorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CausaCaratula = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PiezaSumarial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Relato = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigracionesPendientes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MigracionesPendientes_DependenciaDestinoId",
                table: "MigracionesPendientes",
                column: "DependenciaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_MigracionesPendientes_IdRegistro",
                table: "MigracionesPendientes",
                column: "IdRegistro",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MigracionesPendientes");
        }
    }
}
