using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCamaraFase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Camaras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Camaras", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Camaras_Codigo",
                table: "Camaras",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Camaras");
        }
    }
}
