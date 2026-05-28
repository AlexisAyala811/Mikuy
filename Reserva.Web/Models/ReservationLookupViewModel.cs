using System.ComponentModel.DataAnnotations;

namespace Reserva.Web.Models;

public sealed class ReservationLookupViewModel
{
    [Display(Name = "Codigo de reserva")]
    public string? CodigoReserva { get; set; }

    [Required(ErrorMessage = "Ingrese el correo usado en la reserva.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo valido.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;

    [Display(Name = "Telefono")]
    public string? Telefono { get; set; }

    public int? ReservaId { get; set; }

    public ReservationLookupResult? Result { get; set; }
}
