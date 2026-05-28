namespace Reserva.Domain.Entities;

public static class EstadosReserva
{
    public const string Pendiente = "Pendiente";
    public const string Confirmada = "Confirmada";
    public const string Cancelada = "Cancelada";

    public static readonly string[] ValoresPermitidos =
    {
        Pendiente,
        Confirmada,
        Cancelada
    };
}
