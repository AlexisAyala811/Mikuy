namespace Reserva.Web.Models;

public sealed class DashboardStatsViewModel
{
    public int TotalClientes { get; set; }

    public int TotalReservas { get; set; }

    public int ReservasPendientes { get; set; }

    public int ReservasHoy { get; set; }

    public int MesasActivas { get; set; }

    public int PlatosActivos { get; set; }
}
