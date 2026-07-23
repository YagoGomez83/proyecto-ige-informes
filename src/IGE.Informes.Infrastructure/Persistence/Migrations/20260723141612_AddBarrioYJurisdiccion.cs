using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBarrioYJurisdiccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid[]>(
                name: "BarrioIds",
                table: "Dependencias",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "DependenciaId",
                table: "Camaras",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Barrios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barrios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dependencias_Nombre",
                table: "Dependencias",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Camaras_DependenciaId",
                table: "Camaras",
                column: "DependenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Barrios_Nombre",
                table: "Barrios",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Camaras_Dependencias_DependenciaId",
                table: "Camaras",
                column: "DependenciaId",
                principalTable: "Dependencias",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Camaras_Dependencias_DependenciaId",
                table: "Camaras");

            migrationBuilder.DropTable(
                name: "Barrios");

            migrationBuilder.DropIndex(
                name: "IX_Dependencias_Nombre",
                table: "Dependencias");

            migrationBuilder.DropIndex(
                name: "IX_Camaras_DependenciaId",
                table: "Camaras");

            migrationBuilder.DropColumn(
                name: "BarrioIds",
                table: "Dependencias");

            migrationBuilder.DropColumn(
                name: "DependenciaId",
                table: "Camaras");
        }
    }
}
