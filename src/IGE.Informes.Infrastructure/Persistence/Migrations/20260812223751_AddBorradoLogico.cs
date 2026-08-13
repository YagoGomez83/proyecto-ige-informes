using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBorradoLogico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Vehiculos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "Vehiculos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Personas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "Personas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Informes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "Informes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "CasosAnalisis",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "CasosAnalisis",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Eliminado",
                table: "Vehiculos",
                column: "Eliminado");

            migrationBuilder.CreateIndex(
                name: "IX_Personas_Eliminado",
                table: "Personas",
                column: "Eliminado");

            migrationBuilder.CreateIndex(
                name: "IX_Informes_Eliminado",
                table: "Informes",
                column: "Eliminado");

            migrationBuilder.CreateIndex(
                name: "IX_CasosAnalisis_Eliminado",
                table: "CasosAnalisis",
                column: "Eliminado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehiculos_Eliminado",
                table: "Vehiculos");

            migrationBuilder.DropIndex(
                name: "IX_Personas_Eliminado",
                table: "Personas");

            migrationBuilder.DropIndex(
                name: "IX_Informes_Eliminado",
                table: "Informes");

            migrationBuilder.DropIndex(
                name: "IX_CasosAnalisis_Eliminado",
                table: "CasosAnalisis");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "Personas");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Informes");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "Informes");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "CasosAnalisis");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "CasosAnalisis");
        }
    }
}
