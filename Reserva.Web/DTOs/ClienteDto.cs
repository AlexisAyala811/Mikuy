using System.ComponentModel.DataAnnotations;

namespace Reserva.Web.DTOs;

public sealed class ClienteDto
{
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El telefono del cliente es obligatorio.")]
    [Phone(ErrorMessage = "Ingrese un telefono valido.")]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "El telefono debe tener entre 7 y 20 caracteres.")]
    [Display(Name = "Telefono")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo del cliente es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo valido.")]
    [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;
}
