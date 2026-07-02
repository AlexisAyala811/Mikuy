using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reserva.Infrastructure.Persistence.Migrations;

/// <summary>
/// A cancelled reservation remains in the history but must not hold a table.
/// </summary>
public partial class AllowRebookingCancelledTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "UX_Reservas_Mesa_Fecha_Hora", table: "Reservas");
        migrationBuilder.CreateIndex(
            name: "UX_Reservas_Mesa_Fecha_Hora",
            table: "Reservas",
            columns: new[] { "IdMesa", "Fecha", "Hora" },
            unique: true,
            filter: "[Estado] <> 'Cancelada'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "UX_Reservas_Mesa_Fecha_Hora", table: "Reservas");
        migrationBuilder.CreateIndex(
            name: "UX_Reservas_Mesa_Fecha_Hora",
            table: "Reservas",
            columns: new[] { "IdMesa", "Fecha", "Hora" },
            unique: true);
    }
}
