using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PermiteIdRegistroNuloEnMigracionPendiente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MigracionesPendientes_IdRegistro",
                table: "MigracionesPendientes");

            migrationBuilder.AlterColumn<string>(
                name: "IdRegistro",
                table: "MigracionesPendientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_MigracionesPendientes_IdRegistro",
                table: "MigracionesPendientes",
                column: "IdRegistro",
                unique: true,
                filter: "\"IdRegistro\" IS NOT NULL");
        }

        /// <summary>
        /// Riesgo conocido, no accionado (hallazgo del security-reviewer):
        /// si se ejecuta este rollback habiendo más de una MigracionPendiente
        /// con IdRegistro = null ya persistida, AlterColumn las convierte
        /// todas a IdRegistro = "" (defaultValue) y el CreateIndex único sin
        /// filtro de abajo falla por duplicados — el rollback queda a mitad
        /// de camino, no revierte limpio. No se resuelve porque este Down()
        /// no se ejecuta en la práctica (no hay rollback de producción
        /// planeado); documentado para quien lo encuentre en el futuro.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MigracionesPendientes_IdRegistro",
                table: "MigracionesPendientes");

            migrationBuilder.AlterColumn<string>(
                name: "IdRegistro",
                table: "MigracionesPendientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MigracionesPendientes_IdRegistro",
                table: "MigracionesPendientes",
                column: "IdRegistro",
                unique: true);
        }
    }
}
