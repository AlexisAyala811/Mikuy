using System.ComponentModel.DataAnnotations;

namespace Reserva.Domain.Entities;

public sealed class Usuario
{
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres.")]
    public string UsuarioNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [StringLength(200, MinimumLength = 8, ErrorMessage = "La contrasena debe tener entre 8 y 200 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "El rol debe tener entre 3 y 30 caracteres.")]
    public string Rol { get; set; } = string.Empty;
}
