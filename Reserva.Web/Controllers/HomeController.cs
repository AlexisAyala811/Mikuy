using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reserva.Infrastructure.Persistence;
using Reserva.Web.Models;

namespace Reserva.Web.Controllers;

public class HomeController : Controller
{
    private readonly ReservationDbContext _context;

    public HomeController(ReservationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.PlatosDestacados = await _context.Platos
            .AsNoTracking()
            .Where(plato => plato.Activo)
            .OrderBy(plato => plato.Nombre)
            .Take(3)
            .ToListAsync(cancellationToken);

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Cultura()
    {
        return View();
    }

    public IActionResult Contacto()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
