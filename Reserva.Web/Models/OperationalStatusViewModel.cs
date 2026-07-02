namespace Reserva.Web.Models;

public sealed class OperationalStatusViewModel
{
    public string State { get; set; } = "green";

    public string Title { get; set; } = "Restaurante operativo";

    public string Summary { get; set; } = "Todo normal.";

    public int PendingReservations { get; set; }

    public int TablesRequiringAttention { get; set; }

    public int InsufficientTableReservations { get; set; }

    public int ScheduleConflicts { get; set; }

    public int CurrentOccupancyPercent { get; set; }

    public bool HasCriticalCapacity => CurrentOccupancyPercent >= 86;
}
