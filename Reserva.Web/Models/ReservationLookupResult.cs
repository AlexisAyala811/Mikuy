namespace Reserva.Web.Models;

public sealed class ReservationLookupResult
{
    public int IdReserva { get; set; }

    public string CodigoReserva { get; set; } = string.Empty;

    public string ClienteNombre { get; set; } = string.Empty;

    public string ClienteCorreo { get; set; } = string.Empty;

    public string ClienteTelefono { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public TimeOnly Hora { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int CantidadPersonas { get; set; }

    public string MesaDescripcion { get; set; } = string.Empty;

    public string? Comentario { get; set; }

    public string WhatsAppUrl { get; set; } = string.Empty;

    public bool CanCancel { get; set; }
}
