using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Reserva.Domain.Entities;
using Reserva.Domain.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Reserva.Tests;

[TestClass]
public class ClientesTests
{
    [TestMethod]
    public async Task RegistroValido_AgregaClienteYCommit()
    {
        // Arrange
        var cliente = new Cliente { Nombre = "Juan Perez", Telefono = "5551234567", Correo = "juan@example.com" };

        var repoMock = new Mock<IGenericRepository<Cliente>>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.SetupGet(u => u.Clientes).Returns(repoMock.Object);
        uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1).Verifiable();

        // Act
        await uowMock.Object.Clientes.AddAsync(cliente);
        var result = await uowMock.Object.CommitAsync();

        // Assert
        Assert.AreEqual(1, result);
        repoMock.Verify(r => r.AddAsync(It.Is<Cliente>(c => c == cliente), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public void CorreoInvalido_ValidaAtributos_DataAnnotations()
    {
        // Arrange
        var cliente = new Cliente { Nombre = "A", Telefono = "123", Correo = "no-valido" };

        // Act
        var context = new ValidationContext(cliente);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(cliente, context, results, validateAllProperties: true);

        // Assert
        Assert.IsFalse(valid);
        Assert.IsTrue(results.Any(r => r.ErrorMessage?.Contains("correo") == true || r.MemberNames.Contains(nameof(Cliente.Correo))));
    }

    [TestMethod]
    public async Task EliminacionCorrecta_RemueveClienteYCommit()
    {
        // Arrange
        var cliente = new Cliente { IdCliente = 1, Nombre = "Eliminar", Telefono = "5550000000", Correo = "elim@example.com" };

        var repoMock = new Mock<IGenericRepository<Cliente>>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        repoMock.Setup(r => r.Remove(It.IsAny<Cliente>())).Verifiable();

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.SetupGet(u => u.Clientes).Returns(repoMock.Object);
        uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1).Verifiable();

        // Act
        var existing = await uowMock.Object.Clientes.GetByIdAsync(1);
        uowMock.Object.Clientes.Remove(existing!);
        var result = await uowMock.Object.CommitAsync();

        // Assert
        Assert.IsNotNull(existing);
        repoMock.Verify(r => r.Remove(It.Is<Cliente>(c => c.IdCliente == 1)), Times.Once);
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task Busqueda_RetornaListaDeClientes()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            new Cliente { IdCliente = 1, Nombre = "A", Telefono = "1", Correo = "a@example.com" },
            new Cliente { IdCliente = 2, Nombre = "B", Telefono = "2", Correo = "b@example.com" }
        };

        var repoMock = new Mock<IGenericRepository<Cliente>>();
        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(clientes);

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.SetupGet(u => u.Clientes).Returns(repoMock.Object);

        // Act
        var result = await uowMock.Object.Clientes.GetAllAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task Edicion_ActualizaClienteYCommit()
    {
        // Arrange
        var cliente = new Cliente { IdCliente = 3, Nombre = "Original", Telefono = "5553334444", Correo = "orig@example.com" };

        var repoMock = new Mock<IGenericRepository<Cliente>>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        repoMock.Setup(r => r.Update(It.IsAny<Cliente>())).Verifiable();

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.SetupGet(u => u.Clientes).Returns(repoMock.Object);
        uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1).Verifiable();

        // Act
        var existing = await uowMock.Object.Clientes.GetByIdAsync(3);
        existing!.Nombre = "Modificado";
        uowMock.Object.Clientes.Update(existing);
        var result = await uowMock.Object.CommitAsync();

        // Assert
        repoMock.Verify(r => r.Update(It.Is<Cliente>(c => c.Nombre == "Modificado")), Times.Once);
        Assert.AreEqual(1, result);
    }
}
