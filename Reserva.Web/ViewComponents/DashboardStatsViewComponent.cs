using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entities;
using Reserva.Infrastructure.Persistence;
using Reserva.Web.Models;

namespace Reserva.Web.ViewComponents;

public sealed class DashboardStatsViewComponent : ViewComponent
{
    private readonly ReservationDbContext _context;

    public DashboardStatsViewComponent(ReservationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.Now;
        var currentSlot = new TimeOnly(now.Hour, 0);
        var activeTables = await _context.Mesas.CountAsync(mesa => mesa.Activa);
        var occupiedTables = await _context.Reservas
            .Where(reserva =>
                reserva.Fecha == today &&
                reserva.Hora == currentSlot &&
                reserva.Estado != EstadosReserva.Cancelada)
            .Select(reserva => reserva.IdMesa)
            .Distinct()
            .CountAsync();
        var occupancyPercent = activeTables == 0
            ? 0
            : (int)Math.Round(occupiedTables * 100d / activeTables);
        var occupancyState = occupancyPercent switch
        {
            <= 60 => "healthy",
            <= 85 => "high",
            _ => "critical"
        };
        var occupancyMessage = occupancyState switch
        {
            "healthy" => "Disponibilidad saludable.",
            "high" => "Alta ocupacion.",
            _ => "Capacidad critica."
        };

        var model = new DashboardStatsViewModel
        {
            ReservasHoy = await _context.Reservas.CountAsync(reserva => reserva.Fecha == today),
            ReservasPendientes = await _context.Reservas.CountAsync(reserva =>
                reserva.Fecha == today &&
                reserva.Estado == EstadosReserva.Pendiente),
            ReservasConfirmadas = await _context.Reservas.CountAsync(reserva =>
                reserva.Fecha == today &&
                reserva.Estado == EstadosReserva.Confirmada),
            ReservasCanceladas = await _context.Reservas.CountAsync(reserva =>
                reserva.Fecha == today &&
                reserva.Estado == EstadosReserva.Cancelada),
            ClientesEsperados = await _context.Reservas
                .Where(reserva => reserva.Fecha == today && reserva.Estado != EstadosReserva.Cancelada)
                .SumAsync(reserva => reserva.CantidadPersonas),
            MesasActivas = activeTables,
            MesasOcupadasActuales = occupiedTables,
            OcupacionPorcentaje = occupancyPercent,
            OcupacionEstado = occupancyState,
            OcupacionMensaje = occupancyMessage
        };

        return View(model);
    }
}
