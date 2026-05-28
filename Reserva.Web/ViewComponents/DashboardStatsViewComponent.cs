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

        var model = new DashboardStatsViewModel
        {
            TotalClientes = await _context.Clientes.CountAsync(),
            TotalReservas = await _context.Reservas.CountAsync(),
            ReservasPendientes = await _context.Reservas.CountAsync(reserva => reserva.Estado == EstadosReserva.Pendiente),
            ReservasHoy = await _context.Reservas.CountAsync(reserva => reserva.Fecha == today),
            MesasActivas = await _context.Mesas.CountAsync(mesa => mesa.Activa),
            PlatosActivos = await _context.Platos.CountAsync(plato => plato.Activo)
        };

        return View(model);
    }
}
