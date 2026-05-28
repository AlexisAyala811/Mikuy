using System.ComponentModel.DataAnnotations;
using Reserva.Domain.Entities;

namespace Reserva.Web.DTOs;

public sealed class ReservaDto
{
    public int IdReserva { get; set; }

    [Display(Name = "Codigo")]
    public string CodigoReserva { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de la reserva es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "La hora de la reserva es obligatoria.")]
    [DataType(DataType.Time)]
    [Display(Name = "Hora")]
    public TimeOnly Hora { get; set; }

    [Required(ErrorMessage = "El estado de la reserva es obligatorio.")]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = EstadosReserva.Pendiente;

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un cliente.")]
    [Display(Name = "Cliente")]
    public int IdCliente { get; set; }

    [Display(Name = "Cliente")]
    public string? ClienteNombre { get; set; }

    [Range(1, 24, ErrorMessage = "Indique entre 1 y 24 personas.")]
    [Display(Name = "Personas")]
    public int CantidadPersonas { get; set; } = 2;

    [StringLength(300, ErrorMessage = "El comentario no puede superar los 300 caracteres.")]
    [Display(Name = "Comentario")]
    public string? Comentario { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una mesa.")]
    [Display(Name = "Mesa")]
    public int IdMesa { get; set; }

    [Display(Name = "Mesa")]
    public string? MesaDescripcion { get; set; }

    public string? ClienteTelefono { get; set; }

    public string? WhatsAppUrl { get; set; }
}
