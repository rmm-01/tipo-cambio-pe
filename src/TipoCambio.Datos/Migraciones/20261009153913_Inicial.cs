using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TipoCambio.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TiposCambio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Compra = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false),
                    Venta = table.Column<decimal>(type: "TEXT", precision: 10, scale: 4, nullable: false),
                    Fuente = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RegistradoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposCambio", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TiposCambio_Fecha",
                table: "TiposCambio",
                column: "Fecha",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TiposCambio");
        }
    }
}
