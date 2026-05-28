using Reserva.Domain.Entities;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IGenericRepository<Cliente> Clientes { get; }

    IGenericRepository<ReservaEntity> Reservas { get; }

    IGenericRepository<Usuario> Usuarios { get; }

    IGenericRepository<Mesa> Mesas { get; }

    IGenericRepository<Plato> Platos { get; }

    IGenericRepository<T> Repository<T>()
        where T : class;

    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
