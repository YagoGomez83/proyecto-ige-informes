using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehiculosFase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasAlerta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasAlerta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehiculos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Modelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Dominio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DominioCerteza = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccionARealizar = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AvisarA = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FechaBaja = table.Column<DateOnly>(type: "date", nullable: true),
                    Caracteristicas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CategoriasAlertaIds = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasAlerta_Nombre",
                table: "CategoriasAlerta",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Dominio",
                table: "Vehiculos",
                column: "Dominio");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Estado",
                table: "Vehiculos",
                column: "Estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoriasAlerta");

            migrationBuilder.DropTable(
                name: "Vehiculos");
        }
    }
}
