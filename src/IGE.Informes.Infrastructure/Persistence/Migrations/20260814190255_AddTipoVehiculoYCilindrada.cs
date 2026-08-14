using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoVehiculoYCilindrada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cilindrada",
                table: "Vehiculos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoVehiculo",
                table: "Vehiculos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Auto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cilindrada",
                table: "Vehiculos");

            migrationBuilder.DropColumn(
                name: "TipoVehiculo",
                table: "Vehiculos");
        }
    }
}
