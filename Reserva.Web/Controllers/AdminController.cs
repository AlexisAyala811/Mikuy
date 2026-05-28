using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entities;
using Reserva.Infrastructure.Persistence;

namespace Reserva.Web.Controllers;

[Authorize]
public sealed class AdminController : Controller
{
    private readonly ReservationDbContext _context;

    public AdminController(ReservationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var calendarUntil = today.AddDays(6);

        ViewBag.UltimasReservas = await _context.Reservas
            .AsNoTracking()
            .Include(reserva => reserva.Cliente)
            .Include(reserva => reserva.Mesa)
            .OrderByDescending(reserva => reserva.Fecha)
            .ThenByDescending(reserva => reserva.Hora)
            .Take(6)
            .ToListAsync(cancellationToken);

        ViewBag.ReservasPendientes = await _context.Reservas
            .AsNoTracking()
            .Include(reserva => reserva.Cliente)
            .Include(reserva => reserva.Mesa)
            .Where(reserva => reserva.Estado == EstadosReserva.Pendiente)
            .OrderBy(reserva => reserva.Fecha)
            .ThenBy(reserva => reserva.Hora)
            .Take(5)
            .ToListAsync(cancellationToken);

        ViewBag.CalendarioReservas = await _context.Reservas
            .AsNoTracking()
            .Include(reserva => reserva.Cliente)
            .Include(reserva => reserva.Mesa)
            .Where(reserva => reserva.Fecha >= today && reserva.Fecha <= calendarUntil)
            .OrderBy(reserva => reserva.Fecha)
            .ThenBy(reserva => reserva.Hora)
            .ToListAsync(cancellationToken);

        return View();
    }
}
