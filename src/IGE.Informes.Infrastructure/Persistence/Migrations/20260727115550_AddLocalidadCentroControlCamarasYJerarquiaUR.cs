using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IGE.Informes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalidadCentroControlCamarasYJerarquiaUR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Camaras_Codigo",
                table: "Camaras");

            migrationBuilder.AddColumn<Guid>(
                name: "UnidadRegionalId",
                table: "Dependencias",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CentroControlCamarasId",
                table: "Camaras",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalidadId",
                table: "Camaras",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CentrosControlCamaras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sigla = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentrosControlCamaras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Localidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localidades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dependencias_UnidadRegionalId",
                table: "Dependencias",
                column: "UnidadRegionalId");

            migrationBuilder.CreateIndex(
                name: "IX_Camaras_CentroControlCamarasId",
                table: "Camaras",
                column: "CentroControlCamarasId");

            migrationBuilder.CreateIndex(
                name: "IX_Camaras_Codigo",
                table: "Camaras",
                column: "Codigo");

            migrationBuilder.CreateIndex(
                name: "IX_Camaras_LocalidadId",
                table: "Camaras",
                column: "LocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_CentrosControlCamaras_Sigla",
                table: "CentrosControlCamaras",
                column: "Sigla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_Nombre",
                table: "Localidades",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Camaras_CentrosControlCamaras_CentroControlCamarasId",
                table: "Camaras",
                column: "CentroControlCamarasId",
                principalTable: "CentrosControlCamaras",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Camaras_Localidades_LocalidadId",
                table: "Camaras",
                column: "LocalidadId",
                principalTable: "Localidades",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Dependencias_Dependencias_UnidadRegionalId",
                table: "Dependencias",
                column: "UnidadRegionalId",
                principalTable: "Dependencias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Camaras_CentrosControlCamaras_CentroControlCamarasId",
                table: "Camaras");

            migrationBuilder.DropForeignKey(
                name: "FK_Camaras_Localidades_LocalidadId",
                table: "Camaras");

            migrationBuilder.DropForeignKey(
                name: "FK_Dependencias_Dependencias_UnidadRegionalId",
                table: "Dependencias");

            migrationBuilder.DropTable(
                name: "CentrosControlCamaras");

            migrationBuilder.DropTable(
                name: "Localidades");

            migrationBuilder.DropIndex(
                name: "IX_Dependencias_UnidadRegionalId",
                table: "Dependencias");

            migrationBuilder.DropIndex(
                name: "IX_Camaras_CentroControlCamarasId",
                table: "Camaras");

            migrationBuilder.DropIndex(
                name: "IX_Camaras_Codigo",
                table: "Camaras");

            migrationBuilder.DropIndex(
                name: "IX_Camaras_LocalidadId",
                table: "Camaras");

            migrationBuilder.DropColumn(
                name: "UnidadRegionalId",
                table: "Dependencias");

            migrationBuilder.DropColumn(
                name: "CentroControlCamarasId",
                table: "Camaras");

            migrationBuilder.DropColumn(
                name: "LocalidadId",
                table: "Camaras");

            migrationBuilder.CreateIndex(
                name: "IX_Camaras_Codigo",
                table: "Camaras",
                column: "Codigo",
                unique: true);
        }
    }
}
