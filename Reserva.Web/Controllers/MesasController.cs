using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entities;
using Reserva.Infrastructure.Persistence;

namespace Reserva.Web.Controllers;

[Authorize]
public sealed class MesasController : Controller
{
    private readonly ReservationDbContext _context;

    public MesasController(ReservationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await _context.Mesas.AsNoTracking().OrderBy(mesa => mesa.Numero).ToListAsync(cancellationToken));
    }

    public IActionResult Create()
    {
        return View(new Mesa());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Mesa mesa, CancellationToken cancellationToken)
    {
        if (await _context.Mesas.AnyAsync(item => item.Numero == mesa.Numero, cancellationToken))
        {
            ModelState.AddModelError(nameof(mesa.Numero), "Ya existe una mesa con ese numero.");
        }

        if (!ModelState.IsValid)
        {
            return View(mesa);
        }

        _context.Mesas.Add(mesa);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Mesa registrada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var mesa = await _context.Mesas.FindAsync(new object[] { id }, cancellationToken);
        return mesa is null ? NotFound() : View(mesa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Mesa mesa, CancellationToken cancellationToken)
    {
        if (id != mesa.IdMesa)
        {
            return BadRequest();
        }

        if (await _context.Mesas.AnyAsync(item => item.Numero == mesa.Numero && item.IdMesa != id, cancellationToken))
        {
            ModelState.AddModelError(nameof(mesa.Numero), "Ya existe una mesa con ese numero.");
        }

        if (!ModelState.IsValid)
        {
            return View(mesa);
        }

        _context.Mesas.Update(mesa);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Mesa actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
