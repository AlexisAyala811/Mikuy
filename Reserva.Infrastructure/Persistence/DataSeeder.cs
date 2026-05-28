using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entities;
using Reserva.Infrastructure.Security;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(ReservationDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await SeedClientesAsync(context, cancellationToken);
        await SeedMesasAsync(context, cancellationToken);
        await SeedPlatosAsync(context, cancellationToken);
        await SeedUsuariosAsync(context, cancellationToken);
        await SeedReservasAsync(context, cancellationToken);
    }

    private static async Task SeedClientesAsync(ReservationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Clientes.AnyAsync(cancellationToken))
        {
            return;
        }

        var clientes = new[]
        {
            new Cliente
            {
                Nombre = "Ana Torres",
                Telefono = "987654321",
                Correo = "ana.torres@correo.com"
            },
            new Cliente
            {
                Nombre = "Carlos Mendoza",
                Telefono = "976543210",
                Correo = "carlos.mendoza@correo.com"
            },
            new Cliente
            {
                Nombre = "Lucia Ramirez",
                Telefono = "965432109",
                Correo = "lucia.ramirez@correo.com"
            }
        };

        await context.Clientes.AddRangeAsync(clientes, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedUsuariosAsync(ReservationDbContext context, CancellationToken cancellationToken)
    {
        var usuariosExistentes = await context.Usuarios.ToListAsync(cancellationToken);

        if (usuariosExistentes.Count > 0)
        {
            foreach (var usuario in usuariosExistentes.Where(usuario => !usuario.Password.StartsWith("PBKDF2$", StringComparison.Ordinal)))
            {
                usuario.Password = PasswordHashing.Hash(usuario.Password);
            }

            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var usuarios = new[]
        {
            new Usuario
            {
                UsuarioNombre = "admin",
                Password = PasswordHashing.Hash("Admin123456"),
                Rol = "Administrador"
            },
            new Usuario
            {
                UsuarioNombre = "recepcion",
                Password = PasswordHashing.Hash("Recepcion123"),
                Rol = "Recepcion"
            }
        };

        await context.Usuarios.AddRangeAsync(usuarios, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedReservasAsync(ReservationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Reservas.AnyAsync(cancellationToken))
        {
            return;
        }

        var clientes = await context.Clientes
            .OrderBy(cliente => cliente.IdCliente)
            .Take(3)
            .ToListAsync(cancellationToken);

        var mesas = await context.Mesas
            .Where(mesa => mesa.Activa)
            .OrderBy(mesa => mesa.Numero)
            .Take(3)
            .ToListAsync(cancellationToken);

        if (clientes.Count < 3 || mesas.Count < 3)
        {
            return;
        }

        var reservas = new[]
        {
            new ReservaEntity
            {
                CodigoReserva = "MIK-20260520-0001",
                Fecha = new DateOnly(2026, 5, 20),
                Hora = new TimeOnly(9, 0),
                Estado = EstadosReserva.Confirmada,
                IdCliente = clientes[0].IdCliente,
                IdMesa = mesas[0].IdMesa,
                CantidadPersonas = 2,
                Comentario = "Mesa junto a la ventana."
            },
            new ReservaEntity
            {
                CodigoReserva = "MIK-20260520-0002",
                Fecha = new DateOnly(2026, 5, 20),
                Hora = new TimeOnly(11, 0),
                Estado = EstadosReserva.Pendiente,
                IdCliente = clientes[1].IdCliente,
                IdMesa = mesas[1].IdMesa,
                CantidadPersonas = 4
            },
            new ReservaEntity
            {
                CodigoReserva = "MIK-20260521-0003",
                Fecha = new DateOnly(2026, 5, 21),
                Hora = new TimeOnly(15, 0),
                Estado = EstadosReserva.Cancelada,
                IdCliente = clientes[2].IdCliente,
                IdMesa = mesas[2].IdMesa,
                CantidadPersonas = 3
            }
        };

        await context.Reservas.AddRangeAsync(reservas, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedMesasAsync(ReservationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Mesas.AnyAsync(cancellationToken))
        {
            return;
        }

        await context.Mesas.AddRangeAsync(
        [
            new Mesa { Numero = 1, Capacidad = 2, Ubicacion = "Patio colonial" },
            new Mesa { Numero = 2, Capacidad = 4, Ubicacion = "Salon principal" },
            new Mesa { Numero = 3, Capacidad = 4, Ubicacion = "Vista a la plaza" },
            new Mesa { Numero = 4, Capacidad = 6, Ubicacion = "Salon familiar" },
            new Mesa { Numero = 5, Capacidad = 8, Ubicacion = "Zona de celebraciones" }
        ], cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPlatosAsync(ReservationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Platos.AnyAsync(cancellationToken))
        {
            await AddMissingPlatosAsync(context, cancellationToken);
            return;
        }

        await context.Platos.AddRangeAsync(
        [
            new Plato
            {
                Nombre = "Puca picante",
                Categoria = "Fondo",
                Descripcion = "Papa, mani, aji panca y carne en una preparacion intensa de Ayacucho.",
                Precio = 18m,
                ImagenUrl = "/img/platos/puca-picante.png"
            },
            new Plato
            {
                Nombre = "Mondongo ayacuchano",
                Categoria = "Tradicional",
                Descripcion = "Caldo festivo de maiz pelado y carnes, servido con hierbas y paciencia.",
                Precio = 22m,
                ImagenUrl = "/img/platos/mondongo.png"
            },
            new Plato
            {
                Nombre = "Qapchi",
                Categoria = "Entrada",
                Descripcion = "Papa nativa con queso fresco, huacatay y aji para abrir el apetito.",
                Precio = 14m,
                ImagenUrl = "/img/platos/qapchi.png"
            },
            new Plato
            {
                Nombre = "Cuy chactado",
                Categoria = "Especial",
                Descripcion = "Cuy dorado con textura crocante, papas y ensalada de la casa.",
                Precio = 36m,
                ImagenUrl = "/img/platos/cuy-chactado.png"
            }
        ], cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task AddMissingPlatosAsync(ReservationDbContext context, CancellationToken cancellationToken)
    {
        var platos = await context.Platos.ToListAsync(cancellationToken);

        foreach (var plato in platos)
        {
            plato.Categoria = plato.Nombre switch
            {
                "Qapchi" => "Entrada",
                "Puca picante" => "Fondo",
                "Mondongo ayacuchano" => "Fondo",
                "Cuy chactado" => "Fondo",
                _ => plato.Categoria
            };
        }

        await context.SaveChangesAsync(cancellationToken);

        var nombres = platos.Select(plato => plato.Nombre).ToList();

        var nuevos = new List<Plato>();

        if (!nombres.Contains("Chicha morada de la casa"))
        {
            nuevos.Add(new Plato
            {
                Nombre = "Chicha morada de la casa",
                Categoria = "Bebida",
                Descripcion = "Bebida tradicional de maiz morado, canela y fruta, servida bien fria.",
                Precio = 8m,
                ImagenUrl = "/img/platos/qapchi.png"
            });
        }

        if (!nombres.Contains("Mazamorra de quinua"))
        {
            nuevos.Add(new Plato
            {
                Nombre = "Mazamorra de quinua",
                Categoria = "Postre",
                Descripcion = "Postre suave de quinua, leche y especias dulces inspirado en la cocina andina.",
                Precio = 10m,
                ImagenUrl = "/img/platos/puca-picante.png"
            });
        }

        if (nuevos.Count == 0)
        {
            return;
        }

        await context.Platos.AddRangeAsync(nuevos, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
