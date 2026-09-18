using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceApp.Migrations
{
    /// <inheritdoc />
    public partial class CrearSolicitudesAdopcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MascotaId = table.Column<int>(type: "integer", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    Nombres = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Edad = table.Column<int>(type: "integer", nullable: false),
                    Telefono = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    DondeVive = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NumeroCarnet = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FotoCarnetAnversoUrl = table.Column<string>(type: "text", nullable: true),
                    FotoCarnetReversoUrl = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    RedSocial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TieneOtrasMascotas = table.Column<bool>(type: "boolean", nullable: false),
                    DetalleOtrasMascotas = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Mascotas_MascotaId",
                        column: x => x.MascotaId,
                        principalTable: "Mascotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_MascotaId",
                table: "Solicitudes",
                column: "MascotaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Solicitudes");
        }
    }
}
