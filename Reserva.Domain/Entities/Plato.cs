using System.ComponentModel.DataAnnotations;

namespace Reserva.Domain.Entities;

public sealed class Plato
{
    public int IdPlato { get; set; }

    [Required(ErrorMessage = "El nombre del plato es obligatorio.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripcion del plato es obligatoria.")]
    [StringLength(320, MinimumLength = 12, ErrorMessage = "La descripcion debe tener entre 12 y 320 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoria del plato es obligatoria.")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "La categoria debe tener entre 3 y 60 caracteres.")]
    public string Categoria { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999.99", ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Precio { get; set; }

    [StringLength(220, ErrorMessage = "La ruta de imagen es demasiado larga.")]
    public string ImagenUrl { get; set; } = "/img/platos/puca-picante.png";

    public bool Activo { get; set; } = true;
}
