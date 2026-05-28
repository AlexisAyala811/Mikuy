using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entities;
using Reserva.Infrastructure.Persistence;

namespace Reserva.Web.Controllers;

public sealed class PlatosController : Controller
{
    private readonly ReservationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public PlatosController(ReservationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var platos = await _context.Platos
            .AsNoTracking()
            .Where(plato => plato.Activo)
            .OrderBy(plato => plato.Categoria)
            .ThenBy(plato => plato.Nombre)
            .ToListAsync(cancellationToken);

        return View(platos);
    }

    [Authorize]
    public async Task<IActionResult> Admin(CancellationToken cancellationToken)
    {
        return View(await _context.Platos.AsNoTracking().OrderBy(plato => plato.Nombre).ToListAsync(cancellationToken));
    }

    [Authorize]
    public IActionResult Create()
    {
        return View(new Plato());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Plato plato, IFormFile? imagenArchivo, CancellationToken cancellationToken)
    {
        await TrySetUploadedImageAsync(plato, imagenArchivo, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(plato);
        }

        _context.Platos.Add(plato);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Plato registrado correctamente.";
        return RedirectToAction(nameof(Admin));
    }

    [Authorize]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var plato = await _context.Platos.FindAsync(new object[] { id }, cancellationToken);
        return plato is null ? NotFound() : View(plato);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Plato plato, IFormFile? imagenArchivo, CancellationToken cancellationToken)
    {
        if (id != plato.IdPlato)
        {
            return BadRequest();
        }

        await TrySetUploadedImageAsync(plato, imagenArchivo, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(plato);
        }

        _context.Platos.Update(plato);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Plato actualizado correctamente.";
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var plato = await _context.Platos.FindAsync(new object[] { id }, cancellationToken);

        if (plato is null)
        {
            return NotFound();
        }

        plato.Activo = false;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Plato retirado del menu publico.";
        return RedirectToAction(nameof(Admin));
    }

    private async Task TrySetUploadedImageAsync(Plato plato, IFormFile? imagenArchivo, CancellationToken cancellationToken)
    {
        if (imagenArchivo is null || imagenArchivo.Length == 0)
        {
            return;
        }

        var extension = Path.GetExtension(imagenArchivo.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(plato.ImagenUrl), "Suba una imagen JPG, PNG o WEBP.");
            return;
        }

        const long maxBytes = 5 * 1024 * 1024;
        if (imagenArchivo.Length > maxBytes)
        {
            ModelState.AddModelError(nameof(plato.ImagenUrl), "La imagen no debe superar 5 MB.");
            return;
        }

        var fileName = $"{Slugify(plato.Nombre)}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        var relativePath = $"/img/platos/{fileName}";
        var directory = Path.Combine(_environment.WebRootPath, "img", "platos");
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, fileName);
        await using var stream = System.IO.File.Create(fullPath);
        await imagenArchivo.CopyToAsync(stream, cancellationToken);

        plato.ImagenUrl = relativePath;
    }

    private static string Slugify(string value)
    {
        var cleaned = new string(value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        cleaned = string.Join('-', cleaned.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(cleaned) ? "plato" : cleaned;
    }
}
