using Microsoft.AspNetCore.Mvc.Rendering;

namespace Reserva.Web.Models;

public sealed class TableFilterViewModel
{
    public string Action { get; set; } = "Index";

    public string? Search { get; set; }

    public string SearchPlaceholder { get; set; } = "Buscar";

    public string? Sort { get; set; }

    public string? Estado { get; set; }

    public string? Fecha { get; set; }

    public bool ShowEstado { get; set; }

    public bool ShowFecha { get; set; }

    public IEnumerable<SelectListItem> SortOptions { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> EstadoOptions { get; set; } = Array.Empty<SelectListItem>();
}
