using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Reserva.Domain.Interfaces;
using Reserva.Domain.Entities;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Tests
{
    [TestClass]
    public class ReservasTests
    {
        [TestMethod]
        public async Task ReservaValida_AgregaYCommit()
        {
            // Arrange
            var nueva = new ReservaEntity { Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), Hora = new TimeOnly(10, 0), IdCliente = 1 };

            var repoMock = new Mock<IGenericRepository<ReservaEntity>>();
            repoMock.Setup(r => r.AddAsync(It.IsAny<ReservaEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

            var uowMock = new Mock<IUnitOfWork>();
            uowMock.SetupGet(u => u.Reservas).Returns(repoMock.Object);
            uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1).Verifiable();

            // Act
            await uowMock.Object.Reservas.AddAsync(nueva);
            var res = await uowMock.Object.CommitAsync();

            // Assert
            Assert.AreEqual(1, res);
            repoMock.Verify(r => r.AddAsync(It.Is<ReservaEntity>(x => x == nueva), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task HorarioOcupado_NoPermiteAgregar()
        {
            // Arrange
            var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            var hora = new TimeOnly(10, 0);

            var existente = new ReservaEntity { IdReserva = 1, Fecha = fecha, Hora = hora, IdCliente = 2 };

            var repoMock = new Mock<IGenericRepository<ReservaEntity>>();
            repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReservaEntity> { existente });

            var uowMock = new Mock<IUnitOfWork>();
            uowMock.SetupGet(u => u.Reservas).Returns(repoMock.Object);

            var nueva = new ReservaEntity { Fecha = fecha, Hora = hora, IdCliente = 1 };

            // Act
            var all = await uowMock.Object.Reservas.GetAllAsync();
            var conflict = all.Any(r => r.Fecha == nueva.Fecha && r.Hora == nueva.Hora);

            // Assert
            Assert.IsTrue(conflict, "El horario está ocupado y debe detectarse el conflicto.");
        }

        [TestMethod]
        public async Task ReservasDuplicadas_NoPermiteMismoClienteMismoHorario()
        {
            // Arrange
            var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
            var hora = new TimeOnly(12, 0);

            var existente = new ReservaEntity { IdReserva = 2, Fecha = fecha, Hora = hora, IdCliente = 5 };

            var repoMock = new Mock<IGenericRepository<ReservaEntity>>();
            repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReservaEntity> { existente });

            var uowMock = new Mock<IUnitOfWork>();
            uowMock.SetupGet(u => u.Reservas).Returns(repoMock.Object);

            var nueva = new ReservaEntity { Fecha = fecha, Hora = hora, IdCliente = 5 };

            // Act
            var all = await uowMock.Object.Reservas.GetAllAsync();
            var duplicate = all.Any(r => r.Fecha == nueva.Fecha && r.Hora == nueva.Hora && r.IdCliente == nueva.IdCliente);

            // Assert
            Assert.IsTrue(duplicate, "Reserva duplicada detectada para el mismo cliente, fecha y hora.");
        }

        [TestMethod]
        public async Task CancelacionCorrecta_ActualizaEstadoYCommit()
        {
            // Arrange
            var reserva = new ReservaEntity { IdReserva = 10, Fecha = DateOnly.FromDateTime(DateTime.Today), Hora = new TimeOnly(9, 0), Estado = EstadosReserva.Confirmada };

            var repoMock = new Mock<IGenericRepository<ReservaEntity>>();
            repoMock.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(reserva);
            repoMock.Setup(r => r.Update(It.IsAny<ReservaEntity>())).Verifiable();

            var uowMock = new Mock<IUnitOfWork>();
            uowMock.SetupGet(u => u.Reservas).Returns(repoMock.Object);
            uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1).Verifiable();

            // Act
            var existing = await uowMock.Object.Reservas.GetByIdAsync(10);
            existing!.Estado = EstadosReserva.Cancelada;
            uowMock.Object.Reservas.Update(existing);
            var r = await uowMock.Object.CommitAsync();

            // Assert
            repoMock.Verify(rp => rp.Update(It.Is<ReservaEntity>(x => x.Estado == EstadosReserva.Cancelada)), Times.Once);
            Assert.AreEqual(1, r);
        }

        [TestMethod]
        public void FechaInvalida_ValidaIValidatableObject()
        {
            // Arrange
            var reserva = new ReservaEntity { Fecha = default, Hora = default, IdCliente = 1 };

            // Act
            var context = new ValidationContext(reserva);
            var results = reserva.Validate(context).ToList();

            // Assert
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(ReservaEntity.Fecha))));
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(ReservaEntity.Hora))));
        }

        [TestMethod]
        public async Task HorariosDisponibles_NoConflictoPermiteReserva()
        {
            // Arrange
            var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
            var hora = new TimeOnly(15, 0);

            var repoMock = new Mock<IGenericRepository<ReservaEntity>>();
            repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReservaEntity>());

            var uowMock = new Mock<IUnitOfWork>();
            uowMock.SetupGet(u => u.Reservas).Returns(repoMock.Object);

            var nueva = new ReservaEntity { Fecha = fecha, Hora = hora, IdCliente = 7 };

            // Act
            var all = await uowMock.Object.Reservas.GetAllAsync();
            var conflict = all.Any(r => r.Fecha == nueva.Fecha && r.Hora == nueva.Hora);

            // Assert
            Assert.IsFalse(conflict, "No debe haber conflicto y el horario debe estar disponible.");
        }
    }
}
