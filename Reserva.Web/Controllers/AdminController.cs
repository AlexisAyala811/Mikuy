using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entities;
using Reserva.Infrastructure.Persistence;
using Reserva.Web.Models;
using Reserva.Web.Services;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Web.Controllers;

[Authorize]
public sealed class AdminController : Controller
{
    private readonly ReservationDbContext _context;
    private readonly IReservationNotificationService _notificationService;

    public AdminController(
        ReservationDbContext context,
        IReservationNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Dashboard(string? busquedaGlobal, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var calendarUntil = today.AddDays(6);
        var searchTerm = busquedaGlobal?.Trim();
        var now = DateTime.Now;
        var currentSlot = new TimeOnly(now.Hour, 0);

        ViewBag.BusquedaGlobal = searchTerm;

        var activeTables = await _context.Mesas.CountAsync(mesa => mesa.Activa, cancellationToken);
        var inactiveTables = await _context.Mesas.CountAsync(mesa => !mesa.Activa, cancellationToken);
        var maxActiveCapacity = await _context.Mesas
            .Where(mesa => mesa.Activa)
            .Select(mesa => (int?)mesa.Capacidad)
            .MaxAsync(cancellationToken) ?? 0;
        var pendingReservationsCount = await _context.Reservas
            .CountAsync(reserva => reserva.Estado == EstadosReserva.Pendiente, cancellationToken);
        var currentOccupiedTables = await _context.Reservas
            .Where(reserva =>
                reserva.Fecha == today &&
                reserva.Hora == currentSlot &&
                reserva.Estado != EstadosReserva.Cancelada)
            .Select(reserva => reserva.IdMesa)
            .Distinct()
            .CountAsync(cancellationToken);
        var currentOccupancyPercent = activeTables == 0
            ? 0
            : (int)Math.Round(currentOccupiedTables * 100d / activeTables);
        var insufficientTableReservations = await _context.Reservas
            .CountAsync(reserva =>
                reserva.Fecha >= today &&
                reserva.Estado != EstadosReserva.Cancelada &&
                reserva.CantidadPersonas > maxActiveCapacity,
                cancellationToken);
        var scheduleConflicts = await _context.Reservas
            .Where(reserva => reserva.Fecha >= today && reserva.Estado != EstadosReserva.Cancelada)
            .GroupBy(reserva => new { reserva.Fecha, reserva.Hora, reserva.IdMesa })
            .CountAsync(group => group.Count() > 1, cancellationToken);
        var isCritical = activeTables == 0 ||
            currentOccupancyPercent >= 86 ||
            insufficientTableReservations > 0 ||
            scheduleConflicts > 0;
        var isWarning = !isCritical && (pendingReservationsCount > 0 || inactiveTables > 0 || currentOccupancyPercent >= 61);

        ViewBag.EstadoOperativo = new OperationalStatusViewModel
        {
            State = isCritical ? "red" : isWarning ? "yellow" : "green",
            Title = isCritical
                ? "Atencion critica"
                : isWarning
                    ? "Prioridades por revisar"
                    : "Restaurante operativo",
            Summary = isCritical
                ? "Hay condiciones que pueden afectar la operacion."
                : isWarning
                    ? "El sistema detecto tareas pendientes antes del servicio."
                    : "Todo normal.",
            PendingReservations = pendingReservationsCount,
            TablesRequiringAttention = inactiveTables,
            InsufficientTableReservations = insufficientTableReservations,
            ScheduleConflicts = scheduleConflicts,
            CurrentOccupancyPercent = currentOccupancyPercent
        };

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            ViewBag.ResultadosBusqueda = await _context.Reservas
                .AsNoTracking()
                .Include(reserva => reserva.Cliente)
                .Include(reserva => reserva.Mesa)
                .Where(reserva =>
                    reserva.CodigoReserva.Contains(searchTerm) ||
                    reserva.Estado.Contains(searchTerm) ||
                    (reserva.Cliente != null && (
                        reserva.Cliente.Nombre.Contains(searchTerm) ||
                        reserva.Cliente.Correo.Contains(searchTerm) ||
                        reserva.Cliente.Telefono.Contains(searchTerm))))
                .OrderByDescending(reserva => reserva.Fecha)
                .ThenByDescending(reserva => reserva.Hora)
                .Take(8)
                .ToListAsync(cancellationToken);
        }
        else
        {
            ViewBag.ResultadosBusqueda = Array.Empty<ReservaEntity>();
        }

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
            .ToListAsync(cancellationToken);

        ViewBag.AgendaHoy = await _context.Reservas
            .AsNoTracking()
            .Include(reserva => reserva.Cliente)
            .Include(reserva => reserva.Mesa)
            .Where(reserva => reserva.Fecha == today)
            .OrderBy(reserva => reserva.Hora)
            .ThenBy(reserva => reserva.Mesa!.Numero)
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoReserva(
        int id,
        string estado,
        CancellationToken cancellationToken)
    {
        if (estado is not (EstadosReserva.Confirmada or EstadosReserva.Cancelada))
        {
            return BadRequest();
        }

        var reserva = await _context.Reservas
            .Include(item => item.Cliente)
            .Include(item => item.Mesa)
            .FirstOrDefaultAsync(item => item.IdReserva == id, cancellationToken);

        if (reserva is null)
        {
            return NotFound();
        }

        var previousStatus = reserva.Estado;

        if (previousStatus == estado)
        {
            TempData["Success"] = $"La reserva {reserva.CodigoReserva} ya estaba {estado.ToLowerInvariant()}.";
            return RedirectToAction(nameof(Dashboard));
        }

        reserva.Estado = estado;
        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.NotifyStatusChangedAsync(reserva, previousStatus, cancellationToken);

        TempData["Success"] = estado == EstadosReserva.Confirmada
            ? $"Reserva {reserva.CodigoReserva} confirmada correctamente."
            : $"Reserva {reserva.CodigoReserva} cancelada correctamente.";

        if (estado == EstadosReserva.Confirmada &&
            !string.IsNullOrWhiteSpace(reserva.Cliente?.Telefono))
        {
            TempData["WhatsAppReservationId"] = reserva.IdReserva;
            TempData["WhatsAppClientName"] = reserva.Cliente.Nombre;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> NotificarWhatsApp(int id, CancellationToken cancellationToken)
    {
        var reserva = await _context.Reservas
            .AsNoTracking()
            .Include(item => item.Cliente)
            .Include(item => item.Mesa)
            .FirstOrDefaultAsync(item => item.IdReserva == id, cancellationToken);

        if (reserva is null)
        {
            return NotFound();
        }

        if (reserva.Estado != EstadosReserva.Confirmada)
        {
            TempData["Error"] = "Solo puede avisar por WhatsApp cuando la reserva este confirmada.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (string.IsNullOrWhiteSpace(reserva.Cliente?.Telefono))
        {
            TempData["Error"] = "El cliente no tiene un telefono disponible para WhatsApp.";
            return RedirectToAction(nameof(Dashboard));
        }

        return Redirect(_notificationService.BuildWhatsAppUrl(reserva));
    }
}
