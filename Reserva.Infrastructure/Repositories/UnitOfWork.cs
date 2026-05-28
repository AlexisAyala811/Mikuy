using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entities;
using Reserva.Domain.Interfaces;
using Reserva.Infrastructure.Persistence;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ReservationDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(ReservationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IGenericRepository<Cliente> Clientes => Repository<Cliente>();

    public IGenericRepository<ReservaEntity> Reservas => Repository<ReservaEntity>();

    public IGenericRepository<Usuario> Usuarios => Repository<Usuario>();

    public IGenericRepository<Mesa> Mesas => Repository<Mesa>();

    public IGenericRepository<Plato> Platos => Repository<Plato>();

    public IGenericRepository<T> Repository<T>()
        where T : class
    {
        var entityType = typeof(T);

        if (_repositories.TryGetValue(entityType, out var repository))
        {
            return (IGenericRepository<T>)repository;
        }

        var newRepository = new GenericRepository<T>(_context);
        _repositories[entityType] = newRepository;

        return newRepository;
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var affectedRows = await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return affectedRows;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException("No se pudieron guardar los cambios en la base de datos.", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException("Ocurrio un error al confirmar la unidad de trabajo.", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
