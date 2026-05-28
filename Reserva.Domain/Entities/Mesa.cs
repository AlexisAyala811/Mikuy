using System.ComponentModel.DataAnnotations;

namespace Reserva.Domain.Entities;

public sealed class Mesa
{
    public int IdMesa { get; set; }

    [Range(1, 999, ErrorMessage = "El numero de mesa debe ser valido.")]
    public int Numero { get; set; }

    [Range(1, 24, ErrorMessage = "La capacidad debe estar entre 1 y 24 personas.")]
    public int Capacidad { get; set; } = 2;

    [Required(ErrorMessage = "La ubicacion de la mesa es obligatoria.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "La ubicacion debe tener entre 3 y 80 caracteres.")]
    public string Ubicacion { get; set; } = string.Empty;

    public bool Activa { get; set; } = true;

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
