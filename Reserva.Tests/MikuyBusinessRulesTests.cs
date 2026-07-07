using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reserva.Domain.Entities;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

#nullable enable

namespace Reserva.Tests;

[TestClass]
public sealed class MikuyBusinessRulesTests
{
    private static readonly TimeOnly FirstSlot = new(12, 0);
    private static readonly TimeOnly LastSlot = new(21, 0);

    [TestMethod]
    public void Disponibilidad_ReservaCanceladaNoBloqueaMesa()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var hora = new TimeOnly(19, 0);
        var mesas = new[] { Mesa(1, 4, "Interior") };
        var reservas = new[] { Reserva(1, fecha, hora, 1, EstadosReserva.Cancelada) };

        var mesa = FindAvailableMesa(mesas, reservas, fecha, hora, 4);

        Assert.IsNotNull(mesa);
        Assert.AreEqual(1, mesa!.Numero);
    }

    [TestMethod]
    public void Disponibilidad_ReservaConfirmadaBloqueaMesa()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var hora = new TimeOnly(20, 0);
        var mesas = new[] { Mesa(1, 4, "Interior") };
        var reservas = new[] { Reserva(1, fecha, hora, 1, EstadosReserva.Confirmada) };

        var mesa = FindAvailableMesa(mesas, reservas, fecha, hora, 2);

        Assert.IsNull(mesa);
    }

    [TestMethod]
    public void Disponibilidad_UsaOtraMesaSiUnaEstaOcupada()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var hora = new TimeOnly(18, 0);
        var mesas = new[] { Mesa(1, 4, "Interior"), Mesa(2, 4, "Terraza") };
        var reservas = new[] { Reserva(1, fecha, hora, 1, EstadosReserva.Pendiente) };

        var mesa = FindAvailableMesa(mesas, reservas, fecha, hora, 3);

        Assert.IsNotNull(mesa);
        Assert.AreEqual(2, mesa!.Numero);
    }

    [TestMethod]
    public void Disponibilidad_EligeMesaConMenorCapacidadSuficiente()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var hora = new TimeOnly(17, 0);
        var mesas = new[] { Mesa(1, 8, "Salon"), Mesa(2, 4, "Interior"), Mesa(3, 6, "Terraza") };

        var mesa = FindAvailableMesa(mesas, Array.Empty<ReservaEntity>(), fecha, hora, 4);

        Assert.IsNotNull(mesa);
        Assert.AreEqual(2, mesa!.Numero);
    }

    [TestMethod]
    public void Disponibilidad_NoUsaMesaInactiva()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var hora = new TimeOnly(16, 0);
        var mesas = new[] { Mesa(1, 4, "Interior", activa: false), Mesa(2, 6, "Salon") };

        var mesa = FindAvailableMesa(mesas, Array.Empty<ReservaEntity>(), fecha, hora, 4);

        Assert.IsNotNull(mesa);
        Assert.AreEqual(2, mesa!.Numero);
    }

    [TestMethod]
    public void Disponibilidad_RechazaGrupoMayorQueCapacidad()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var hora = new TimeOnly(16, 0);
        var mesas = new[] { Mesa(1, 4, "Interior"), Mesa(2, 6, "Salon") };

        var mesa = FindAvailableMesa(mesas, Array.Empty<ReservaEntity>(), fecha, hora, 8);

        Assert.IsNull(mesa);
    }

    [TestMethod]
    public void Horario_ValidaInicioYUltimoTurno()
    {
        Assert.IsTrue(IsWithinReservationHours(new TimeOnly(12, 0)));
        Assert.IsTrue(IsWithinReservationHours(new TimeOnly(21, 0)));
    }

    [TestMethod]
    public void Horario_RechazaFueraDeAtencion()
    {
        Assert.IsFalse(IsWithinReservationHours(new TimeOnly(11, 0)));
        Assert.IsFalse(IsWithinReservationHours(new TimeOnly(22, 0)));
    }

    [TestMethod]
    public void ReservaPublica_RechazaFechaPasada()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        Assert.IsFalse(IsPublicReservationDateAllowed(fecha));
    }

    [TestMethod]
    public void ReservaPublica_RechazaMasDeNoventaDias()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(91));

        Assert.IsFalse(IsPublicReservationDateAllowed(fecha));
    }

    [TestMethod]
    public void ReservaPublica_AceptaFechaDentroDeNoventaDias()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(45));

        Assert.IsTrue(IsPublicReservationDateAllowed(fecha));
    }

    [TestMethod]
    public void CodigoReserva_UsaFormatoMikuyConFechaEId()
    {
        var fecha = new DateOnly(2026, 7, 7);

        var codigo = BuildReservationCode(fecha, 15);

        Assert.AreEqual("MIK-20260707-0015", codigo);
    }

    [TestMethod]
    public void Cliente_NormalizaTelefonoConSoloDigitos()
    {
        var telefono = NormalizeDigits("+51 954-173-129");

        Assert.AreEqual("51954173129", telefono);
    }

    [TestMethod]
    public void ConsultaContacto_BuscaClientePorCorreoOTelefono()
    {
        var clientes = new[]
        {
            new Cliente { IdCliente = 1, Nombre = "Ana", Correo = "ana@mikuy.pe", Telefono = "999111222" },
            new Cliente { IdCliente = 2, Nombre = "Luis", Correo = "luis@mikuy.pe", Telefono = "988777666" }
        };

        var porCorreo = FindClientByContact(clientes, "ana@mikuy.pe");
        var porTelefono = FindClientByContact(clientes, "988 777 666");

        Assert.AreEqual(1, porCorreo?.IdCliente);
        Assert.AreEqual(2, porTelefono?.IdCliente);
    }

    [TestMethod]
    public void Administracion_OrdenaPendientesAntesQueConfirmadasYCanceladas()
    {
        var reservas = new[]
        {
            Reserva(1, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(20, 0), 1, EstadosReserva.Confirmada),
            Reserva(2, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(17, 0), 2, EstadosReserva.Pendiente),
            Reserva(3, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(18, 0), 3, EstadosReserva.Cancelada)
        };

        var ordenadas = reservas
            .OrderBy(reserva => reserva.Estado == EstadosReserva.Pendiente ? 0 : reserva.Estado == EstadosReserva.Confirmada ? 1 : 2)
            .ThenBy(reserva => reserva.Hora)
            .ToList();

        Assert.AreEqual(2, ordenadas[0].IdReserva);
        Assert.AreEqual(1, ordenadas[1].IdReserva);
        Assert.AreEqual(3, ordenadas[2].IdReserva);
    }

    [TestMethod]
    public void ConsultaCodigo_CoincideSinImportarMayusculas()
    {
        const string codigoGuardado = "MIK-20260707-0015";
        const string codigoIngresado = "mik-20260707-0015";

        Assert.IsTrue(CodesMatch(codigoGuardado, codigoIngresado));
    }

    [TestMethod]
    public void ConsultaCodigo_RechazaCodigoIncorrectoParaComprobante()
    {
        const string codigoGuardado = "MIK-20260707-0015";
        const string codigoIngresado = "MIK-20260707-9999";

        Assert.IsFalse(CodesMatch(codigoGuardado, codigoIngresado));
    }

    [TestMethod]
    public void EstadoReserva_AceptaSoloValoresPermitidos()
    {
        CollectionAssert.Contains(EstadosReserva.ValoresPermitidos, EstadosReserva.Pendiente);
        CollectionAssert.Contains(EstadosReserva.ValoresPermitidos, EstadosReserva.Confirmada);
        CollectionAssert.Contains(EstadosReserva.ValoresPermitidos, EstadosReserva.Cancelada);
        CollectionAssert.DoesNotContain(EstadosReserva.ValoresPermitidos, "Finalizada");
    }

    [TestMethod]
    public void ReservaComentario_TruncaEspaciosAntesDeGuardar()
    {
        var comentario = NormalizeComment("  Cumpleanos familiar  ");

        Assert.AreEqual("Cumpleanos familiar", comentario);
    }

    [TestMethod]
    public void ReservaComentario_VacioSeGuardaComoNull()
    {
        var comentario = NormalizeComment("   ");

        Assert.IsNull(comentario);
    }

    [TestMethod]
    public void HorariosDisponibles_IncluyeTurnosConMesaLibre()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var mesas = new[] { Mesa(1, 4, "Interior"), Mesa(2, 4, "Terraza") };
        var reservas = new[] { Reserva(1, fecha, new TimeOnly(19, 0), 1, EstadosReserva.Confirmada) };

        var horarios = GetAvailableTimes(mesas, reservas, fecha, 4);

        CollectionAssert.Contains(horarios, new TimeOnly(19, 0));
    }

    [TestMethod]
    public void HorariosDisponibles_ExcluyeTurnoCuandoTodasLasMesasEstanOcupadas()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var mesas = new[] { Mesa(1, 4, "Interior"), Mesa(2, 4, "Terraza") };
        var reservas = new[]
        {
            Reserva(1, fecha, new TimeOnly(19, 0), 1, EstadosReserva.Confirmada),
            Reserva(2, fecha, new TimeOnly(19, 0), 2, EstadosReserva.Pendiente)
        };

        var horarios = GetAvailableTimes(mesas, reservas, fecha, 4);

        CollectionAssert.DoesNotContain(horarios, new TimeOnly(19, 0));
    }

    [TestMethod]
    public void HorariosDisponibles_FiltraPorCapacidadSolicitada()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var mesas = new[] { Mesa(1, 2, "Interior"), Mesa(2, 4, "Terraza") };
        var reservas = new[] { Reserva(1, fecha, new TimeOnly(18, 0), 2, EstadosReserva.Confirmada) };

        var horarios = GetAvailableTimes(mesas, reservas, fecha, 4);

        CollectionAssert.DoesNotContain(horarios, new TimeOnly(18, 0));
    }

    [TestMethod]
    public void Mesas_NormalizacionMantieneMaximoVeinte()
    {
        var numeros = Enumerable.Range(1, 55).Select(numero => ((numero - 1) % 20) + 1).ToList();

        var normalizadas = NormalizeTableNumbers(numeros, 20);

        Assert.AreEqual(20, normalizadas.Count);
        Assert.AreEqual(20, normalizadas.Distinct().Count());
    }

    [TestMethod]
    public void Mesas_NormalizacionNoRepiteNumero()
    {
        var numeros = new[] { 1, 1, 2, 2, 3, 4, 4, 5 };

        var normalizadas = NormalizeTableNumbers(numeros, 20);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, normalizadas);
    }

    [TestMethod]
    public void Clientes_SeleccionaExistentePorCorreoNormalizado()
    {
        var clientes = new[] { new Cliente { IdCliente = 7, Correo = "fernando@gmail.com", Telefono = "954173129" } };

        var cliente = FindClientByContact(clientes, "  FERNANDO@gmail.com ");

        Assert.AreEqual(7, cliente?.IdCliente);
    }

    [TestMethod]
    public void Clientes_TelefonoConFormatoEncuentraRegistro()
    {
        var clientes = new[] { new Cliente { IdCliente = 8, Correo = "fer@correo.com", Telefono = "954173129" } };

        var cliente = FindClientByContact(clientes, "954-173-129");

        Assert.AreEqual(8, cliente?.IdCliente);
    }

    [TestMethod]
    public void Dashboard_CuentaClientesEsperadosDeReservasNoCanceladas()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today);
        var reservas = new[]
        {
            Reserva(1, fecha, new TimeOnly(17, 0), 1, EstadosReserva.Pendiente, personas: 4),
            Reserva(2, fecha, new TimeOnly(18, 0), 2, EstadosReserva.Confirmada, personas: 6),
            Reserva(3, fecha, new TimeOnly(19, 0), 3, EstadosReserva.Cancelada, personas: 8)
        };

        var esperados = CountExpectedGuests(reservas, fecha);

        Assert.AreEqual(10, esperados);
    }

    [TestMethod]
    public void Dashboard_CuentaPendientesDelDia()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today);
        var reservas = new[]
        {
            Reserva(1, fecha, new TimeOnly(17, 0), 1, EstadosReserva.Pendiente),
            Reserva(2, fecha, new TimeOnly(18, 0), 2, EstadosReserva.Confirmada),
            Reserva(3, fecha.AddDays(1), new TimeOnly(19, 0), 3, EstadosReserva.Pendiente)
        };

        var pendientes = CountByStatus(reservas, fecha, EstadosReserva.Pendiente);

        Assert.AreEqual(1, pendientes);
    }

    [TestMethod]
    public void Ocupacion_CalculaPorcentajeConMesasOcupadas()
    {
        var porcentaje = CalculateOccupancyPercent(15, 20);

        Assert.AreEqual(75, porcentaje);
    }

    [TestMethod]
    public void Ocupacion_SinMesasRetornaCero()
    {
        var porcentaje = CalculateOccupancyPercent(3, 0);

        Assert.AreEqual(0, porcentaje);
    }

    [TestMethod]
    public void EstadoOperativo_AmarilloCuandoHayPendientes()
    {
        var estado = GetOperationalColor(pendientes: 3, ocupacion: 40, conflictos: 0);

        Assert.AreEqual("amarillo", estado);
    }

    [TestMethod]
    public void EstadoOperativo_RojoCuandoHayConflictosOCapacidadCritica()
    {
        var porConflicto = GetOperationalColor(pendientes: 0, ocupacion: 50, conflictos: 1);
        var porOcupacion = GetOperationalColor(pendientes: 0, ocupacion: 90, conflictos: 0);

        Assert.AreEqual("rojo", porConflicto);
        Assert.AreEqual("rojo", porOcupacion);
    }

    private static Mesa? FindAvailableMesa(
        IEnumerable<Mesa> mesas,
        IEnumerable<ReservaEntity> reservas,
        DateOnly fecha,
        TimeOnly hora,
        int personas)
    {
        if (!IsWithinReservationHours(hora) || personas <= 0)
        {
            return null;
        }

        var mesasOcupadas = reservas
            .Where(reserva =>
                reserva.Fecha == fecha &&
                reserva.Hora == hora &&
                reserva.Estado != EstadosReserva.Cancelada)
            .Select(reserva => reserva.IdMesa)
            .ToHashSet();

        return mesas
            .Where(mesa => mesa.Activa && mesa.Capacidad >= personas && !mesasOcupadas.Contains(mesa.IdMesa))
            .OrderBy(mesa => mesa.Capacidad)
            .ThenBy(mesa => mesa.Numero)
            .FirstOrDefault();
    }

    private static bool IsWithinReservationHours(TimeOnly hora) => hora >= FirstSlot && hora <= LastSlot;

    private static bool IsPublicReservationDateAllowed(DateOnly fecha)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return fecha >= today && fecha <= today.AddDays(90);
    }

    private static string BuildReservationCode(DateOnly fecha, int idReserva) => $"MIK-{fecha:yyyyMMdd}-{idReserva:0000}";

    private static string NormalizeDigits(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());

    private static Cliente? FindClientByContact(IEnumerable<Cliente> clientes, string contacto)
    {
        var term = contacto.Trim().ToLowerInvariant();
        var digits = NormalizeDigits(term);

        return term.Contains('@', StringComparison.Ordinal)
            ? clientes.FirstOrDefault(cliente => cliente.Correo == term)
            : clientes.FirstOrDefault(cliente => cliente.Telefono.Contains(digits));
    }

    private static Mesa Mesa(int numero, int capacidad, string ubicacion, bool activa = true) =>
        new()
        {
            IdMesa = numero,
            Numero = numero,
            Capacidad = capacidad,
            Ubicacion = ubicacion,
            Activa = activa
        };

    private static bool CodesMatch(string codigoGuardado, string? codigoIngresado) =>
        string.Equals(codigoGuardado, codigoIngresado?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeComment(string? comentario) =>
        string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();

    private static List<TimeOnly> GetAvailableTimes(
        IEnumerable<Mesa> mesas,
        IEnumerable<ReservaEntity> reservas,
        DateOnly fecha,
        int personas)
    {
        var disponibles = new List<TimeOnly>();

        for (var hora = FirstSlot; hora <= LastSlot; hora = hora.AddHours(1))
        {
            if (FindAvailableMesa(mesas, reservas, fecha, hora, personas) is not null)
            {
                disponibles.Add(hora);
            }
        }

        return disponibles;
    }

    private static List<int> NormalizeTableNumbers(IEnumerable<int> numeros, int maximo) =>
        numeros
            .Where(numero => numero >= 1 && numero <= maximo)
            .Distinct()
            .OrderBy(numero => numero)
            .Take(maximo)
            .ToList();

    private static int CountExpectedGuests(IEnumerable<ReservaEntity> reservas, DateOnly fecha) =>
        reservas
            .Where(reserva => reserva.Fecha == fecha && reserva.Estado != EstadosReserva.Cancelada)
            .Sum(reserva => reserva.CantidadPersonas);

    private static int CountByStatus(IEnumerable<ReservaEntity> reservas, DateOnly fecha, string estado) =>
        reservas.Count(reserva => reserva.Fecha == fecha && reserva.Estado == estado);

    private static int CalculateOccupancyPercent(int ocupadas, int total) =>
        total <= 0 ? 0 : (int)Math.Round(ocupadas * 100m / total);

    private static string GetOperationalColor(int pendientes, int ocupacion, int conflictos)
    {
        if (conflictos > 0 || ocupacion >= 86)
        {
            return "rojo";
        }

        return pendientes > 0 ? "amarillo" : "verde";
    }

    private static ReservaEntity Reserva(int id, DateOnly fecha, TimeOnly hora, int idMesa, string estado, int personas = 2) =>
        new()
        {
            IdReserva = id,
            Fecha = fecha,
            Hora = hora,
            IdCliente = id,
            IdMesa = idMesa,
            CantidadPersonas = personas,
            Estado = estado
        };
}
