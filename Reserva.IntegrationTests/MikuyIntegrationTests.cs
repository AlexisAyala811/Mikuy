using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reserva.Domain.Entities;
using Reserva.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.IntegrationTests;

[TestClass]
public sealed partial class MikuyIntegrationTests
{
    private static PostgreSqlContainer _postgres = null!;
    private static MikuyWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("mikuy_integration")
            .WithUsername("mikuy_test")
            .WithPassword("mikuy_test_password")
            .Build();

        await _postgres.StartAsync();
        _factory = new MikuyWebApplicationFactory(_postgres.GetConnectionString());

        using var client = CreateClient();
        using var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [TestMethod]
    public async Task PaginaPrincipal_RespondeCorrectamente()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(html, "Mikuy");
    }

    [TestMethod]
    public async Task Disponibilidad_ConMesaAdecuada_DevuelveDisponible()
    {
        using var client = CreateClient();
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

        using var response = await client.GetAsync(
            $"/Reservas/Disponibilidad?fecha={fecha:yyyy-MM-dd}&hora=18:00&cantidadPersonas=4");
        var result = await response.Content.ReadFromJsonAsync<AvailabilityResponse>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Available);
        StringAssert.Contains(result.Message, "Mesa disponible");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Table));
    }

    [TestMethod]
    public async Task ReservasAdministrativas_SinSesion_RedirigeAlLogin()
    {
        using var client = CreateClient(allowAutoRedirect: false);

        using var response = await client.GetAsync("/Reservas");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/Cuenta/Login", response.Headers.Location?.AbsolutePath);
        StringAssert.Contains(response.Headers.Location?.Query, "ReturnUrl=%2FReservas");
    }

    [TestMethod]
    public async Task Administrador_ConfirmaReserva_YPuedeAvisarAlClientePorWhatsApp()
    {
        int reservaId;
        string clienteNombre;
        string clienteTelefono;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();
            var cliente = await db.Clientes.OrderBy(item => item.IdCliente).FirstAsync();
            var mesa = await db.Mesas.Where(item => item.Activa).OrderByDescending(item => item.IdMesa).FirstAsync();
            var reserva = new ReservaEntity
            {
                CodigoReserva = $"MIK-INT-{Guid.NewGuid():N}"[..24],
                Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(90)),
                Hora = new TimeOnly(20, 0),
                Estado = EstadosReserva.Pendiente,
                IdCliente = cliente.IdCliente,
                IdMesa = mesa.IdMesa,
                CantidadPersonas = 2
            };

            db.Reservas.Add(reserva);
            await db.SaveChangesAsync();
            reservaId = reserva.IdReserva;
            clienteNombre = cliente.Nombre;
            clienteTelefono = cliente.Telefono;
        }

        using var client = CreateClient(allowAutoRedirect: false);
        var loginToken = await GetAntiforgeryTokenAsync(client, "/Cuenta/Login");

        using var loginResponse = await client.PostAsync(
            "/Cuenta/Login",
            Form(
                ("__RequestVerificationToken", loginToken),
                ("Usuario", "admin"),
                ("Password", "Admin123456")));

        Assert.AreEqual(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.AreEqual("/Admin/Dashboard", loginResponse.Headers.Location?.OriginalString);

        var dashboardToken = await GetAntiforgeryTokenAsync(client, "/Admin/Dashboard");
        using var confirmResponse = await client.PostAsync(
            "/Admin/CambiarEstadoReserva",
            Form(
                ("__RequestVerificationToken", dashboardToken),
                ("id", reservaId.ToString()),
                ("estado", EstadosReserva.Confirmada)));

        Assert.AreEqual(HttpStatusCode.Redirect, confirmResponse.StatusCode);
        Assert.AreEqual("/Admin/Dashboard", confirmResponse.Headers.Location?.OriginalString);

        var dashboardHtml = await client.GetStringAsync("/Admin/Dashboard");
        StringAssert.Contains(dashboardHtml, "Avisar al cliente por WhatsApp");
        StringAssert.Contains(dashboardHtml, clienteNombre);

        using var whatsappResponse = await client.GetAsync($"/Admin/NotificarWhatsApp?id={reservaId}");
        Assert.AreEqual(HttpStatusCode.Redirect, whatsappResponse.StatusCode);
        Assert.AreEqual("wa.me", whatsappResponse.Headers.Location?.Host);
        StringAssert.Contains(
            whatsappResponse.Headers.Location?.AbsolutePath,
            NormalizePeruPhone(clienteTelefono));

        using var publicResponse = await client.GetAsync($"/Reservas/Confirmacion/{reservaId}");
        var publicHtml = await publicResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Enviar por WhatsApp", publicHtml);
    }

    [TestMethod]
    public async Task RegistroCliente_GuardaCliente_YCreaCookieDeAcceso()
    {
        using var client = CreateClient(allowAutoRedirect: false);
        var token = await GetAntiforgeryTokenAsync(client, "/Clientes/Registro");
        var email = $"integracion-{Guid.NewGuid():N}@mikuy.test";

        using var response = await client.PostAsync(
            "/Clientes/Registro",
            Form(
                ("__RequestVerificationToken", token),
                ("Nombre", "Cliente Integracion"),
                ("Telefono", "999888777"),
                ("Correo", email)));

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/Reservas/Reservar", response.Headers.Location?.OriginalString);
        Assert.IsTrue(
            response.Headers.TryGetValues("Set-Cookie", out var cookies) &&
            cookies.Any(value => value.StartsWith("Mikuy.ClienteId=", StringComparison.Ordinal)));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();
        Assert.IsTrue(await db.Clientes.AnyAsync(cliente => cliente.Correo == email));
    }

    [TestMethod]
    public async Task PostgreSql_RechazaCorreosDuplicados()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();
        var email = $"duplicado-{Guid.NewGuid():N}@mikuy.test";

        db.Clientes.Add(new Cliente
        {
            Nombre = "Cliente Uno",
            Telefono = "900000001",
            Correo = email
        });
        await db.SaveChangesAsync();

        db.Clientes.Add(new Cliente
        {
            Nombre = "Cliente Dos",
            Telefono = "900000002",
            Correo = email
        });

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [TestMethod]
    public async Task PostgreSql_RechazaDosReservasActivas_EnLaMismaMesaYHorario()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();
        var cliente = await db.Clientes.OrderBy(item => item.IdCliente).FirstAsync();
        var mesa = await db.Mesas.Where(item => item.Activa).OrderBy(item => item.IdMesa).FirstAsync();
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(60));
        var hora = new TimeOnly(19, 0);

        db.Reservas.Add(new ReservaEntity
        {
            CodigoReserva = $"MIK-INT-{Guid.NewGuid():N}"[..24],
            Fecha = fecha,
            Hora = hora,
            Estado = EstadosReserva.Confirmada,
            IdCliente = cliente.IdCliente,
            IdMesa = mesa.IdMesa,
            CantidadPersonas = 2
        });
        await db.SaveChangesAsync();

        db.Reservas.Add(new ReservaEntity
        {
            CodigoReserva = $"MIK-INT-{Guid.NewGuid():N}"[..24],
            Fecha = fecha,
            Hora = hora,
            Estado = EstadosReserva.Pendiente,
            IdCliente = cliente.IdCliente,
            IdMesa = mesa.IdMesa,
            CantidadPersonas = 2
        });

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static HttpClient CreateClient(bool allowAutoRedirect = true)
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = AntiforgeryTokenRegex().Match(html);

        Assert.IsTrue(match.Success, $"No se encontro el token antiforgery en {path}.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] values)
    {
        return new FormUrlEncodedContent(values.Select(value =>
            new KeyValuePair<string, string>(value.Key, value.Value)));
    }

    private static string NormalizePeruPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.StartsWith("51", StringComparison.Ordinal) ? digits : $"51{digits}";
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();

    private sealed record AvailabilityResponse(
        bool Available,
        string Status,
        string Message,
        string? Table);
}
