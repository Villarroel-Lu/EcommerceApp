using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApp.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSaludDetalladaMascotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoSalud",
                table: "Mascotas");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Mascotas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CondicionLlegada",
                table: "Mascotas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnTratamiento",
                table: "Mascotas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<string>>(
                 name: "EstadosSalud",
                 table: "Mascotas",
                 type: "text[]",
                 nullable: false,
                 defaultValueSql: "ARRAY[]::text[]"); 

            migrationBuilder.AddColumn<bool>(
                name: "RequiereCuidadosLargoPlazo",
                table: "Mascotas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ResumenCuidadosAdoptante",
                table: "Mascotas",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TieneEnfermedadBase",
                table: "Mascotas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TratamientoAdministrativo",
                table: "Mascotas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Mascotas");

            migrationBuilder.DropColumn(
                name: "CondicionLlegada",
                table: "Mascotas");

            migrationBuilder.DropColumn(
                name: "EnTratamiento",
                table: "Mascotas");

            migrationBuilder.DropColumn(
                name: "EstadosSalud",
                table: "Mascotas");

            migrationBuilder.DropColumn(
                name: "RequiereCuidadosLargoPlazo",
                table: "Mascotas");

            migrationBuilder.DropColumn(
                name: "ResumenCuidadosAdoptante",
                table: "Mascotas");

            migrationBuilder.DropColumn(
                name: "TieneEnfermedadBase",
                table: "Mascotas");

            migrationBuilder.DropColumn(
                name: "TratamientoAdministrativo",
                table: "Mascotas");

            migrationBuilder.AddColumn<string>(
                name: "EstadoSalud",
                table: "Mascotas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }
    }
}
