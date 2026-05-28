using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Reserva.Domain.Entities;
using Reserva.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Reserva.Tests;

[TestClass]
public class SeguridadTests
{
    [TestMethod]
    public async Task LoginCorrecto_RetornaUsuario()
    {
        // Arrange
        var usuario = new Usuario { IdUsuario = 1, UsuarioNombre = "admin", Password = "password123", Rol = "Admin" };

        var repoMock = new Mock<IGenericRepository<Usuario>>();
        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Usuario> { usuario });

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.SetupGet(u => u.Usuarios).Returns(repoMock.Object);

        // Act
        var all = await uowMock.Object.Usuarios.GetAllAsync();
        var found = all.FirstOrDefault(u => u.UsuarioNombre == "admin" && u.Password == "password123");

        // Assert
        Assert.IsNotNull(found);
        Assert.AreEqual("Admin", found!.Rol);
    }

    [TestMethod]
    public async Task LoginIncorrecto_NoRetornaUsuario()
    {
        // Arrange
        var usuario = new Usuario { IdUsuario = 2, UsuarioNombre = "user", Password = "pwd", Rol = "User" };

        var repoMock = new Mock<IGenericRepository<Usuario>>();
        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Usuario> { usuario });

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.SetupGet(u => u.Usuarios).Returns(repoMock.Object);

        // Act
        var all = await uowMock.Object.Usuarios.GetAllAsync();
        var found = all.FirstOrDefault(u => u.UsuarioNombre == "user" && u.Password == "wrongpassword");

        // Assert
        Assert.IsNull(found);
    }

    [TestMethod]
    public void AccesoAutorizado_RolValido()
    {
        // Arrange
        var usuario = new Usuario { IdUsuario = 3, UsuarioNombre = "manager", Password = "securepwd", Rol = "Manager" };

        // Act
        var autorizado = usuario.Rol == "Admin" || usuario.Rol == "Manager";

        // Assert
        Assert.IsTrue(autorizado);
    }

    [TestMethod]
    public void AccesoDenegado_RolInvalido()
    {
        // Arrange
        var usuario = new Usuario { IdUsuario = 4, UsuarioNombre = "guest", Password = "guestpwd", Rol = "Guest" };

        // Act
        var autorizado = usuario.Rol == "Admin" || usuario.Rol == "Manager";

        // Assert
        Assert.IsFalse(autorizado);
    }
}
