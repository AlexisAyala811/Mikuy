using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entities;
using Reserva.Infrastructure.Persistence;
using Reserva.Web.DTOs;
using X.PagedList;
using X.PagedList.Extensions;

namespace Reserva.Web.Controllers;

[Authorize]
public sealed class ClientesController : Controller
{
    private const int PageSize = 8;
    private readonly ReservationDbContext _context;
    private readonly IMapper _mapper;

    public ClientesController(ReservationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IActionResult> Index(string? search, string? sort, int? page, CancellationToken cancellationToken)
    {
        var query = _context.Clientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(cliente =>
                cliente.Nombre.Contains(term) ||
                cliente.Telefono.Contains(term) ||
                cliente.Correo.Contains(term));
        }

        query = sort switch
        {
            "nombre_desc" => query.OrderByDescending(cliente => cliente.Nombre),
            "correo" => query.OrderBy(cliente => cliente.Correo),
            "correo_desc" => query.OrderByDescending(cliente => cliente.Correo),
            "telefono" => query.OrderBy(cliente => cliente.Telefono),
            "telefono_desc" => query.OrderByDescending(cliente => cliente.Telefono),
            _ => query.OrderBy(cliente => cliente.Nombre)
        };

        var clientes = await query.ToListAsync(cancellationToken);

        ViewBag.Search = search;
        ViewBag.Sort = sort;

        return View(_mapper.Map<List<ClienteDto>>(clientes).ToPagedList(page ?? 1, PageSize));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdCliente == id, cancellationToken);

        if (cliente is null)
        {
            return NotFound();
        }

        return View(_mapper.Map<ClienteDto>(cliente));
    }

    public IActionResult Create()
    {
        return View(new ClienteDto());
    }

    [AllowAnonymous]
    public IActionResult Registro()
    {
        return View(new ClienteDto());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(ClienteDto dto, CancellationToken cancellationToken)
    {
        await ValidateClienteAsync(dto, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        _context.Clientes.Add(_mapper.Map<Cliente>(dto));
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Cliente registrado. Ya puede reservar en Mikuy.";
        return RedirectToAction("Reservar", "Reservas");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClienteDto dto, CancellationToken cancellationToken)
    {
        await ValidateClienteAsync(dto, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        _context.Clientes.Add(_mapper.Map<Cliente>(dto));

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Cliente creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo guardar el cliente. Verifique que el correo no este duplicado.");
            return View(dto);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes.FindAsync(new object[] { id }, cancellationToken);

        if (cliente is null)
        {
            return NotFound();
        }

        return View(_mapper.Map<ClienteDto>(cliente));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClienteDto dto, CancellationToken cancellationToken)
    {
        if (id != dto.IdCliente)
        {
            return BadRequest();
        }

        await ValidateClienteAsync(dto, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var cliente = await _context.Clientes.FindAsync(new object[] { id }, cancellationToken);

        if (cliente is null)
        {
            return NotFound();
        }

        _mapper.Map(dto, cliente);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Cliente actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo actualizar el cliente. Verifique los datos ingresados.");
            return View(dto);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.IdCliente == id, cancellationToken);

        if (cliente is null)
        {
            return NotFound();
        }

        return View(_mapper.Map<ClienteDto>(cliente));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes
            .Include(item => item.Reservas)
            .FirstOrDefaultAsync(item => item.IdCliente == id, cancellationToken);

        if (cliente is null)
        {
            return NotFound();
        }

        if (cliente.Reservas.Count > 0)
        {
            TempData["Error"] = "No se puede eliminar un cliente con reservas registradas.";
            return RedirectToAction(nameof(Index));
        }

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Cliente eliminado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateClienteAsync(ClienteDto dto, CancellationToken cancellationToken)
    {
        var correoExiste = await _context.Clientes.AnyAsync(cliente =>
            cliente.Correo == dto.Correo &&
            cliente.IdCliente != dto.IdCliente,
            cancellationToken);

        if (correoExiste)
        {
            ModelState.AddModelError(nameof(dto.Correo), "Ya existe un cliente registrado con este correo.");
        }
    }
}
