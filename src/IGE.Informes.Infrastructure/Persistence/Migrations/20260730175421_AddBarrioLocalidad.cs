using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBarrioLocalidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Barrios_Nombre",
                table: "Barrios");

            migrationBuilder.AddColumn<Guid>(
                name: "LocalidadId",
                table: "Barrios",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Barrios_LocalidadId",
                table: "Barrios",
                column: "LocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Barrios_Nombre_LocalidadId",
                table: "Barrios",
                columns: new[] { "Nombre", "LocalidadId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Barrios_Localidades_LocalidadId",
                table: "Barrios",
                column: "LocalidadId",
                principalTable: "Localidades",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Barrios_Localidades_LocalidadId",
                table: "Barrios");

            migrationBuilder.DropIndex(
                name: "IX_Barrios_LocalidadId",
                table: "Barrios");

            migrationBuilder.DropIndex(
                name: "IX_Barrios_Nombre_LocalidadId",
                table: "Barrios");

            migrationBuilder.DropColumn(
                name: "LocalidadId",
                table: "Barrios");

            migrationBuilder.CreateIndex(
                name: "IX_Barrios_Nombre",
                table: "Barrios",
                column: "Nombre",
                unique: true);
        }
    }
}
