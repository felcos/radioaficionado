using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadioAficionado.Infraestructura.Sqlite.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TipoActivacion = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IndicativoActivador = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FechaInicio = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Localizador = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    EstadoActivacion = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Qsos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IndicativoPropio = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    IndicativoContacto = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FechaHoraInicio = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FechaHoraFin = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Frecuencia = table.Column<long>(type: "INTEGER", nullable: false),
                    Modo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SenalEnviada = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SenalRecibida = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Potencia = table.Column<double>(type: "REAL", nullable: true),
                    LocalizadorContacto = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    Notas = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FechaModificacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ActivacionId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qsos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Qsos_Activaciones_ActivacionId",
                        column: x => x.ActivacionId,
                        principalTable: "Activaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activaciones_EstadoActivacion",
                table: "Activaciones",
                column: "EstadoActivacion");

            migrationBuilder.CreateIndex(
                name: "IX_Activaciones_FechaInicio",
                table: "Activaciones",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_Activaciones_IndicativoActivador",
                table: "Activaciones",
                column: "IndicativoActivador");

            migrationBuilder.CreateIndex(
                name: "IX_Activaciones_Referencia",
                table: "Activaciones",
                column: "Referencia");

            migrationBuilder.CreateIndex(
                name: "IX_Activaciones_TipoActivacion",
                table: "Activaciones",
                column: "TipoActivacion");

            migrationBuilder.CreateIndex(
                name: "IX_Qsos_ActivacionId",
                table: "Qsos",
                column: "ActivacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Qsos_FechaHoraInicio",
                table: "Qsos",
                column: "FechaHoraInicio");

            migrationBuilder.CreateIndex(
                name: "IX_Qsos_Frecuencia",
                table: "Qsos",
                column: "Frecuencia");

            migrationBuilder.CreateIndex(
                name: "IX_Qsos_IndicativoContacto",
                table: "Qsos",
                column: "IndicativoContacto");

            migrationBuilder.CreateIndex(
                name: "IX_Qsos_IndicativoPropio",
                table: "Qsos",
                column: "IndicativoPropio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Qsos");

            migrationBuilder.DropTable(
                name: "Activaciones");
        }
    }
}
