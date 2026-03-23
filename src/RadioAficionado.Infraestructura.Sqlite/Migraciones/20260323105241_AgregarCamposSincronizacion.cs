using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadioAficionado.Infraestructura.Sqlite.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarCamposSincronizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Sincronizado",
                table: "Qsos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Qsos_Sincronizado",
                table: "Qsos",
                column: "Sincronizado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Qsos_Sincronizado",
                table: "Qsos");

            migrationBuilder.DropColumn(
                name: "Sincronizado",
                table: "Qsos");
        }
    }
}
