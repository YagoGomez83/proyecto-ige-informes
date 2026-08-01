using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVehiculoImagen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehiculoImagenes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehiculoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImagenPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubidaPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehiculoImagenes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehiculoImagenes_VehiculoId",
                table: "VehiculoImagenes",
                column: "VehiculoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehiculoImagenes");
        }
    }
}
