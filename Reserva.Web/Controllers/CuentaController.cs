using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reserva.Infrastructure.Persistence;
using Reserva.Infrastructure.Security;
using Reserva.Web.Models;

namespace Reserva.Web.Controllers;

public sealed class CuentaController : Controller
{
    private readonly ReservationDbContext _context;

    public CuentaController(ReservationDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        return User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Dashboard", "Admin")
            : View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UsuarioNombre == model.Usuario, cancellationToken);

        if (usuario is null || !PasswordHashing.Verify(model.Password, usuario.Password))
        {
            ModelState.AddModelError(string.Empty, "Usuario o contrasena incorrectos.");
            return View(model);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, usuario.UsuarioNombre),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Dashboard", "Admin");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
