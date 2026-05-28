using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entities;
using Reserva.Infrastructure.Persistence;
using Reserva.Web.DTOs;
using Reserva.Web.Models;
using Reserva.Web.Services;
using X.PagedList;
using X.PagedList.Extensions;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Web.Controllers;

[Authorize]
public sealed class ReservasController : Controller
{
    private const int PageSize = 8;
    private static readonly TimeOnly FirstSlot = new(9, 0);
    private static readonly TimeOnly LastSlot = new(21, 0);
    private readonly ReservationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IReservationNotificationService _notificationService;
    private readonly IReservationReceiptService _receiptService;

    public ReservasController(
        ReservationDbContext context,
        IMapper mapper,
        IReservationNotificationService notificationService,
        IReservationReceiptService receiptService)
    {
        _context = context;
        _mapper = mapper;
        _notificationService = notificationService;
        _receiptService = receiptService;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? estado,
        DateOnly? fecha,
        string? sort,
        int? page,
        CancellationToken cancellationToken)
    {
        var query = _context.Reservas
            .AsNoTracking()
            .Include(reserva => reserva.Cliente)
            .Include(reserva => reserva.Mesa)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(reserva =>
                reserva.Estado.Contains(term) ||
                (reserva.Cliente != null && reserva.Cliente.Nombre.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(reserva => reserva.Estado == estado);
        }

        if (fecha.HasValue)
        {
            query = query.Where(reserva => reserva.Fecha == fecha.Value);
        }

        query = sort switch
        {
            "fecha" => query.OrderBy(reserva => reserva.Fecha).ThenBy(reserva => reserva.Hora),
            "hora" => query.OrderBy(reserva => reserva.Hora),
            "cliente" => query.OrderBy(reserva => reserva.Cliente!.Nombre),
            "estado" => query.OrderBy(reserva => reserva.Estado),
            _ => query.OrderByDescending(reserva => reserva.Fecha).ThenBy(reserva => reserva.Hora)
        };

        var reservas = await query.ToListAsync(cancellationToken);

        ViewBag.Search = search;
        ViewBag.Estado = estado;
        ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd");
        ViewBag.Sort = sort;

        var reservaDtos = _mapper.Map<List<ReservaDto>>(reservas);

        foreach (var dto in reservaDtos)
        {
            var reserva = reservas.First(item => item.IdReserva == dto.IdReserva);
            dto.WhatsAppUrl = _notificationService.BuildWhatsAppUrl(reserva);
        }

        return View(reservaDtos.ToPagedList(page ?? 1, PageSize));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
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

        var dto = _mapper.Map<ReservaDto>(reserva);
        dto.WhatsAppUrl = _notificationService.BuildWhatsAppUrl(reserva);

        return View(dto);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var dto = new ReservaDto();
        await LoadFormDataAsync(dto.Fecha, null, cancellationToken);

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservaDto dto, CancellationToken cancellationToken)
    {
        await ValidateReservaAsync(dto, null, cancellationToken);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(dto.Fecha, null, cancellationToken);
            return View(dto);
        }

        var reserva = _mapper.Map<ReservaEntity>(dto);
        _context.Reservas.Add(reserva);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await EnsureReservationCodeAsync(reserva.IdReserva, cancellationToken);
            TempData["Success"] = "Reserva creada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo guardar la reserva. El horario seleccionado ya esta ocupado.");
            await LoadFormDataAsync(dto.Fecha, null, cancellationToken);
            return View(dto);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var reserva = await _context.Reservas.FindAsync(new object[] { id }, cancellationToken);

        if (reserva is null)
        {
            return NotFound();
        }

        var dto = _mapper.Map<ReservaDto>(reserva);
        await LoadFormDataAsync(dto.Fecha, dto.IdReserva, cancellationToken);

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReservaDto dto, CancellationToken cancellationToken)
    {
        if (id != dto.IdReserva)
        {
            return BadRequest();
        }

        await ValidateReservaAsync(dto, id, cancellationToken);

        if (!ModelState.IsValid)
        {
            await LoadFormDataAsync(dto.Fecha, id, cancellationToken);
            return View(dto);
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
        _mapper.Map(dto, reserva);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _notificationService.NotifyStatusChangedAsync(reserva, previousStatus, cancellationToken);
            TempData["Success"] = "Reserva actualizada correctamente.";

            if (previousStatus != reserva.Estado && reserva.Estado is EstadosReserva.Confirmada or EstadosReserva.Cancelada)
            {
                TempData["WhatsAppUrl"] = _notificationService.BuildWhatsAppUrl(reserva);
                TempData["WhatsAppText"] = "Enviar WhatsApp al cliente";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo actualizar la reserva. El horario seleccionado ya esta ocupado.");
            await LoadFormDataAsync(dto.Fecha, id, cancellationToken);
            return View(dto);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
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

        return View(_mapper.Map<ReservaDto>(reserva));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var reserva = await _context.Reservas.FindAsync(new object[] { id }, cancellationToken);

        if (reserva is null)
        {
            return NotFound();
        }

        _context.Reservas.Remove(reserva);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Reserva eliminada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> HorariosDisponibles(DateOnly fecha, int? idReserva, CancellationToken cancellationToken)
    {
        var horarios = await GetAvailableTimesAsync(fecha, idReserva, cancellationToken);

        return Json(horarios.Select(hora => new
        {
            value = hora.ToString("HH:mm"),
            text = hora.ToString("HH:mm")
        }));
    }

    private async Task LoadFormDataAsync(DateOnly fecha, int? idReserva, CancellationToken cancellationToken)
    {
        var clientes = await _context.Clientes
            .AsNoTracking()
            .OrderBy(cliente => cliente.Nombre)
            .ToListAsync(cancellationToken);

        var horarios = await GetAvailableTimesAsync(fecha, idReserva, cancellationToken);
        var mesas = await _context.Mesas
            .AsNoTracking()
            .Where(mesa => mesa.Activa)
            .OrderBy(mesa => mesa.Numero)
            .ToListAsync(cancellationToken);

        ViewBag.Clientes = new SelectList(clientes, nameof(Cliente.IdCliente), nameof(Cliente.Nombre));
        ViewBag.Estados = EstadosReserva.ValoresPermitidos
            .Select(estado => new SelectListItem(estado, estado));
        ViewBag.Mesas = new SelectList(
            mesas.Select(mesa => new { Value = mesa.IdMesa, Text = $"Mesa {mesa.Numero} - {mesa.Capacidad} personas - {mesa.Ubicacion}" }),
            "Value",
            "Text");
        ViewBag.HorariosDisponibles = new SelectList(
            horarios.Select(hora => new { Value = hora.ToString("HH:mm"), Text = hora.ToString("HH:mm") }),
            "Value",
            "Text");
    }

    private async Task ValidateReservaAsync(ReservaDto dto, int? idReserva, CancellationToken cancellationToken)
    {
        if (!EstadosReserva.ValoresPermitidos.Contains(dto.Estado))
        {
            ModelState.AddModelError(nameof(dto.Estado), "Seleccione un estado valido.");
        }

        var clienteExiste = await _context.Clientes.AnyAsync(
            cliente => cliente.IdCliente == dto.IdCliente,
            cancellationToken);

        if (!clienteExiste)
        {
            ModelState.AddModelError(nameof(dto.IdCliente), "Seleccione un cliente valido.");
        }

        if (dto.Hora == default)
        {
            ModelState.AddModelError(nameof(dto.Hora), "Seleccione un horario disponible.");
            return;
        }

        var mesa = await _context.Mesas.FirstOrDefaultAsync(item => item.IdMesa == dto.IdMesa && item.Activa, cancellationToken);

        if (mesa is null)
        {
            ModelState.AddModelError(nameof(dto.IdMesa), "Seleccione una mesa activa.");
            return;
        }

        if (mesa.Capacidad < dto.CantidadPersonas)
        {
            ModelState.AddModelError(nameof(dto.IdMesa), "La mesa seleccionada no cubre la cantidad de personas.");
        }

        var horarioOcupado = await _context.Reservas.AnyAsync(reserva =>
            reserva.Fecha == dto.Fecha &&
            reserva.Hora == dto.Hora &&
            reserva.IdMesa == dto.IdMesa &&
            (!idReserva.HasValue || reserva.IdReserva != idReserva.Value),
            cancellationToken);

        if (horarioOcupado)
        {
            ModelState.AddModelError(nameof(dto.Hora), "El horario seleccionado ya esta ocupado.");
        }
    }

    private async Task<IReadOnlyList<TimeOnly>> GetAvailableTimesAsync(
        DateOnly fecha,
        int? idReserva,
        CancellationToken cancellationToken)
    {
        var horariosOcupados = await _context.Reservas
            .AsNoTracking()
            .Where(reserva =>
                reserva.Fecha == fecha &&
                (!idReserva.HasValue || reserva.IdReserva != idReserva.Value))
            .Select(reserva => reserva.Hora)
            .ToListAsync(cancellationToken);

        var mesasActivas = await _context.Mesas.CountAsync(mesa => mesa.Activa, cancellationToken);
        var disponibles = new List<TimeOnly>();

        for (var hora = FirstSlot; hora <= LastSlot; hora = hora.AddHours(1))
        {
            if (mesasActivas > horariosOcupados.Count(horario => horario == hora))
            {
                disponibles.Add(hora);
            }
        }

        return disponibles;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Reservar(CancellationToken cancellationToken)
    {
        var model = new ReservaPublicaViewModel();
        await LoadPublicTimesAsync(model.Fecha, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reservar(ReservaPublicaViewModel model, CancellationToken cancellationToken)
    {
        if (model.Fecha < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(model.Fecha), "Seleccione una fecha vigente.");
        }

        var mesa = await FindAvailableMesaAsync(model.Fecha, model.Hora, model.CantidadPersonas, cancellationToken);

        if (model.Hora == default || mesa is null)
        {
            ModelState.AddModelError(nameof(model.Hora), "No hay una mesa disponible para ese horario y cantidad de personas.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPublicTimesAsync(model.Fecha, cancellationToken);
            return View(model);
        }

        var cliente = await _context.Clientes.FirstOrDefaultAsync(item => item.Correo == model.Correo, cancellationToken);

        if (cliente is null)
        {
            cliente = new Cliente
            {
                Nombre = model.NombreCliente.Trim(),
                Telefono = model.Telefono.Trim(),
                Correo = model.Correo.Trim()
            };
            _context.Clientes.Add(cliente);
        }
        else
        {
            cliente.Nombre = model.NombreCliente.Trim();
            cliente.Telefono = model.Telefono.Trim();
        }

        var reserva = new ReservaEntity
        {
            Cliente = cliente,
            Fecha = model.Fecha,
            Hora = model.Hora,
            CantidadPersonas = model.CantidadPersonas,
            Comentario = model.Comentario?.Trim(),
            IdMesa = mesa!.IdMesa,
            Estado = EstadosReserva.Pendiente
        };

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync(cancellationToken);
        await EnsureReservationCodeAsync(reserva.IdReserva, cancellationToken);
        TempData["Success"] = "Recibimos su reserva. Mikuy confirmara el horario desde el panel administrativo.";
        return RedirectToAction(nameof(Confirmacion), new { id = reserva.IdReserva });
    }

    [AllowAnonymous]
    public async Task<IActionResult> Confirmacion(int id, CancellationToken cancellationToken)
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

        return View(new ReservationConfirmationViewModel
        {
            IdReserva = reserva.IdReserva,
            CodigoReserva = reserva.CodigoReserva,
            ClienteNombre = reserva.Cliente?.Nombre ?? string.Empty,
            ClienteCorreo = reserva.Cliente?.Correo ?? string.Empty,
            ClienteTelefono = reserva.Cliente?.Telefono ?? string.Empty,
            Fecha = reserva.Fecha,
            Hora = reserva.Hora,
            Estado = reserva.Estado,
            CantidadPersonas = reserva.CantidadPersonas,
            MesaDescripcion = reserva.Mesa is null ? string.Empty : $"Mesa {reserva.Mesa.Numero} - {reserva.Mesa.Ubicacion}",
            Comentario = reserva.Comentario
        });
    }

    [AllowAnonymous]
    public async Task<IActionResult> Comprobante(int id, CancellationToken cancellationToken)
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

        var pdf = _receiptService.BuildReceiptPdf(reserva);
        var fileName = $"comprobante-{reserva.CodigoReserva}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    [AllowAnonymous]
    public IActionResult Consultar()
    {
        return View(new ReservationLookupViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Consultar(ReservationLookupViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var correo = model.Correo.Trim();
        var telefono = NormalizeDigits(model.Telefono);
        var codigo = model.CodigoReserva?.Trim();

        var query = _context.Reservas
            .AsNoTracking()
            .Include(reserva => reserva.Cliente)
            .Include(reserva => reserva.Mesa)
            .Where(reserva => reserva.Cliente != null && reserva.Cliente.Correo == correo);

        if (!string.IsNullOrWhiteSpace(telefono))
        {
            query = query.Where(reserva => reserva.Cliente != null && reserva.Cliente.Telefono.Contains(telefono));
        }

        if (!string.IsNullOrWhiteSpace(codigo))
        {
            query = query.Where(reserva => reserva.CodigoReserva == codigo);
        }

        var reserva = await query
            .OrderByDescending(item => item.Fecha)
            .ThenByDescending(item => item.Hora)
            .FirstOrDefaultAsync(cancellationToken);

        if (reserva is null)
        {
            ModelState.AddModelError(string.Empty, "No encontramos una reserva con esos datos.");
            return View(model);
        }

        model.Result = _notificationService.BuildLookupResult(reserva);
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarConsulta(ReservationLookupViewModel model, CancellationToken cancellationToken)
    {
        if (!model.ReservaId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Seleccione una reserva valida para cancelar.");
            return View(nameof(Consultar), model);
        }

        if (string.IsNullOrWhiteSpace(model.Correo))
        {
            ModelState.AddModelError(nameof(model.Correo), "Ingrese el correo usado en la reserva.");
            return View(nameof(Consultar), model);
        }

        var correo = model.Correo.Trim();
        var telefono = NormalizeDigits(model.Telefono);
        var codigo = model.CodigoReserva?.Trim();

        var reserva = await _context.Reservas
            .Include(item => item.Cliente)
            .Include(item => item.Mesa)
            .FirstOrDefaultAsync(item =>
                item.IdReserva == model.ReservaId.Value &&
                item.Cliente != null &&
                item.Cliente.Correo == correo,
                cancellationToken);

        if (reserva is null ||
            (!string.IsNullOrWhiteSpace(codigo) && reserva.CodigoReserva != codigo) ||
            (!string.IsNullOrWhiteSpace(telefono) && !NormalizeDigits(reserva.Cliente?.Telefono).Contains(telefono)))
        {
            ModelState.AddModelError(string.Empty, "No pudimos validar la reserva con esos datos.");
            return View(nameof(Consultar), model);
        }

        if (reserva.Estado == EstadosReserva.Cancelada)
        {
            TempData["Success"] = "La reserva ya estaba cancelada.";
            model.Result = _notificationService.BuildLookupResult(reserva);
            return View(nameof(Consultar), model);
        }

        if (reserva.Fecha < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(string.Empty, "No se puede cancelar una reserva de una fecha pasada desde esta pantalla.");
            model.Result = _notificationService.BuildLookupResult(reserva);
            return View(nameof(Consultar), model);
        }

        var previousStatus = reserva.Estado;
        reserva.Estado = EstadosReserva.Cancelada;
        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.NotifyStatusChangedAsync(reserva, previousStatus, cancellationToken);

        TempData["Success"] = "Su reserva fue cancelada correctamente. Tambien le enviaremos el aviso por correo si el correo esta configurado.";
        model.Result = _notificationService.BuildLookupResult(reserva);
        return View(nameof(Consultar), model);
    }

    private async Task LoadPublicTimesAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        ViewBag.HorariosDisponibles = new SelectList(
            (await GetAvailableTimesAsync(fecha, null, cancellationToken))
                .Select(hora => new { Value = hora.ToString("HH:mm"), Text = hora.ToString("HH:mm") }),
            "Value",
            "Text");
    }

    private async Task<Mesa?> FindAvailableMesaAsync(
        DateOnly fecha,
        TimeOnly hora,
        int cantidadPersonas,
        CancellationToken cancellationToken)
    {
        var mesasOcupadas = _context.Reservas
            .Where(reserva => reserva.Fecha == fecha && reserva.Hora == hora)
            .Select(reserva => reserva.IdMesa);

        return await _context.Mesas
            .AsNoTracking()
            .Where(mesa =>
                mesa.Activa &&
                mesa.Capacidad >= cantidadPersonas &&
                !mesasOcupadas.Contains(mesa.IdMesa))
            .OrderBy(mesa => mesa.Capacidad)
            .ThenBy(mesa => mesa.Numero)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string NormalizeDigits(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }

    private async Task EnsureReservationCodeAsync(int idReserva, CancellationToken cancellationToken)
    {
        var reserva = await _context.Reservas.FindAsync(new object[] { idReserva }, cancellationToken);

        if (reserva is null || !string.IsNullOrWhiteSpace(reserva.CodigoReserva))
        {
            return;
        }

        reserva.CodigoReserva = $"MIK-{reserva.Fecha:yyyyMMdd}-{idReserva:0000}";
        await _context.SaveChangesAsync(cancellationToken);
    }
}
