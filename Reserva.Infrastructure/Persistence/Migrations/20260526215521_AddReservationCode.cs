using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reserva.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoReserva",
                table: "Reservas",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Reservas
                SET CodigoReserva = CONCAT(
                    'MIK-',
                    FORMAT(Fecha, 'yyyyMMdd'),
                    '-',
                    RIGHT(CONCAT('0000', IdReserva), 4))
                WHERE CodigoReserva = ''
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_CodigoReserva",
                table: "Reservas",
                column: "CodigoReserva",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservas_CodigoReserva",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "CodigoReserva",
                table: "Reservas");
        }
    }
}
