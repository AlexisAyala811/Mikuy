using System.ComponentModel.DataAnnotations;

namespace Reserva.Web.Models;

public sealed class ReservationLookupViewModel
{
    [StringLength(24, ErrorMessage = "El codigo no puede superar los 24 caracteres.")]
    [Display(Name = "Codigo de reserva")]
    public string? CodigoReserva { get; set; }

    public string MetodoConsulta { get; set; } = "codigo";

    [EmailAddress(ErrorMessage = "Ingrese un correo valido.")]
    [Display(Name = "Correo")]
    public string? Correo { get; set; }

    [Display(Name = "Correo o telefono")]
    public string? Contacto { get; set; }

    public int? ReservaId { get; set; }

    public ReservationLookupResult? Result { get; set; }

    public IReadOnlyList<ReservationLookupResult> Results { get; set; } = [];
}
