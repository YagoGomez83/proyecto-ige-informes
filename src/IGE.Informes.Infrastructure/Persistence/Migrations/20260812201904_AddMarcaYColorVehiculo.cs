using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarcaYColorVehiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ColoresVehiculo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColoresVehiculo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarcasVehiculo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarcasVehiculo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ColoresVehiculo_Nombre",
                table: "ColoresVehiculo",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarcasVehiculo_Nombre",
                table: "MarcasVehiculo",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ColoresVehiculo");

            migrationBuilder.DropTable(
                name: "MarcasVehiculo");
        }
    }
}
