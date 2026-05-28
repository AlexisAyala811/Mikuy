using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reserva.Domain.Entities;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Tests;

[TestClass]
public sealed class MikuyModelTests
{
    [TestMethod]
    public void MesaConCapacidadInvalida_FallaValidacion()
    {
        var mesa = new Mesa { Numero = 8, Capacidad = 0, Ubicacion = "Salon principal" };
        var resultados = Validate(mesa);

        Assert.IsTrue(resultados.Any(resultado => resultado.MemberNames.Contains(nameof(Mesa.Capacidad))));
    }

    [TestMethod]
    public void PlatoActivoConPrecioValido_PasaValidacion()
    {
        var plato = new Plato
        {
            Nombre = "Puca picante",
            Categoria = "Fondo",
            Descripcion = "Preparacion tradicional ayacuchana para la carta de Mikuy.",
            Precio = 18m
        };

        Assert.AreEqual(0, Validate(plato).Count);
    }

    [TestMethod]
    public void ReservaConGrupoGrande_FallaValidacion()
    {
        var reserva = new ReservaEntity
        {
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Hora = new TimeOnly(13, 0),
            IdCliente = 1,
            IdMesa = 1,
            CantidadPersonas = 25
        };

        var resultados = Validate(reserva);

        Assert.IsTrue(resultados.Any(resultado => resultado.MemberNames.Contains(nameof(ReservaEntity.CantidadPersonas))));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), resultados, validateAllProperties: true);
        return resultados;
    }
}
