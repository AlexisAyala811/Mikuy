using System.ComponentModel.DataAnnotations;

namespace Reserva.Domain.Entities;

public sealed class Reserva : IValidatableObject
{
    public int IdReserva { get; set; }

    [StringLength(24, ErrorMessage = "El codigo de reserva no puede superar los 24 caracteres.")]
    public string CodigoReserva { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de la reserva es obligatoria.")]
    public DateOnly Fecha { get; set; }

    [Required(ErrorMessage = "La hora de la reserva es obligatoria.")]
    public TimeOnly Hora { get; set; }

    [Required(ErrorMessage = "El estado de la reserva es obligatorio.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "El estado debe tener entre 3 y 30 caracteres.")]
    public string Estado { get; set; } = EstadosReserva.Pendiente;

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un cliente valido.")]
    public int IdCliente { get; set; }

    public Cliente? Cliente { get; set; }

    [Range(1, 24, ErrorMessage = "La reserva debe indicar entre 1 y 24 personas.")]
    public int CantidadPersonas { get; set; } = 2;

    [StringLength(300, ErrorMessage = "El comentario no puede superar los 300 caracteres.")]
    public string? Comentario { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una mesa valida.")]
    public int IdMesa { get; set; }

    public Mesa? Mesa { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Fecha == default)
        {
            yield return new ValidationResult(
                "La fecha de la reserva debe ser valida.",
                new[] { nameof(Fecha) });
        }

        if (Hora == default)
        {
            yield return new ValidationResult(
                "La hora de la reserva debe ser valida.",
                new[] { nameof(Hora) });
        }

        if (!EstadosReserva.ValoresPermitidos.Contains(Estado))
        {
            yield return new ValidationResult(
                "El estado debe ser Pendiente, Confirmada o Cancelada.",
                new[] { nameof(Estado) });
        }
    }
}
