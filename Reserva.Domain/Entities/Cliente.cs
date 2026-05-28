using System.ComponentModel.DataAnnotations;

namespace Reserva.Domain.Entities;

public sealed class Cliente
{
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El telefono del cliente es obligatorio.")]
    [Phone(ErrorMessage = "El telefono no tiene un formato valido.")]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "El telefono debe tener entre 7 y 20 caracteres.")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo del cliente es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato valido.")]
    [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
    public string Correo { get; set; } = string.Empty;

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
