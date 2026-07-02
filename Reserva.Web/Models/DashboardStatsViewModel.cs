namespace Reserva.Web.Models;

public sealed class DashboardStatsViewModel
{
    public int ReservasPendientes { get; set; }

    public int ReservasHoy { get; set; }

    public int ReservasConfirmadas { get; set; }

    public int ReservasCanceladas { get; set; }

    public int ClientesEsperados { get; set; }

    public int MesasActivas { get; set; }

    public int MesasOcupadasActuales { get; set; }

    public int OcupacionPorcentaje { get; set; }

    public string OcupacionEstado { get; set; } = "healthy";

    public string OcupacionMensaje { get; set; } = "Disponibilidad saludable.";
}
