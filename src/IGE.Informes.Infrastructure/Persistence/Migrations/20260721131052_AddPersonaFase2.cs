using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonaFase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Personas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Dni = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Rol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Caracteristicas = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Personas_Dni",
                table: "Personas",
                column: "Dni");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Personas");
        }
    }
}
