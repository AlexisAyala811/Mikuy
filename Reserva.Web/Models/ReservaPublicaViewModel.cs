using System.ComponentModel.DataAnnotations;

namespace Reserva.Web.Models;

public sealed class ReservaPublicaViewModel
{
    [Required(ErrorMessage = "Ingrese su nombre.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCliente { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su telefono.")]
    [Phone(ErrorMessage = "Ingrese un telefono valido.")]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "El telefono debe tener entre 7 y 20 caracteres.")]
    [Display(Name = "Telefono")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su correo.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo valido.")]
    [StringLength(150)]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione una fecha.")]
    [Display(Name = "Fecha")]
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required(ErrorMessage = "Seleccione una hora.")]
    [Display(Name = "Hora")]
    public TimeOnly Hora { get; set; }

    [Range(1, 24, ErrorMessage = "Indique entre 1 y 24 personas.")]
    [Display(Name = "Personas")]
    public int CantidadPersonas { get; set; } = 2;

    [StringLength(300, ErrorMessage = "El comentario no puede superar los 300 caracteres.")]
    [Display(Name = "Comentario")]
    public string? Comentario { get; set; }
}
