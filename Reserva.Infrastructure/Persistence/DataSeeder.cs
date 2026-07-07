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
        await NormalizeMesasAsync(context, cancellationToken);

        var existingNumbers = await context.Mesas
            .Select(mesa => mesa.Numero)
            .ToListAsync(cancellationToken);

        var ubicaciones = new[] { "Patio colonial", "Salon principal", "Vista a la plaza", "Salon familiar", "Terraza" };
        var capacidades = new[] { 2, 4, 4, 6, 6, 8 };
        var mesasFaltantes = Enumerable.Range(1, 20)
            .Where(numero => !existingNumbers.Contains(numero))
            .Select(numero => new Mesa
            {
                Numero = numero,
                Capacidad = capacidades[(numero - 1) % capacidades.Length],
                Ubicacion = ubicaciones[(numero - 1) % ubicaciones.Length],
                Activa = true
            })
            .ToList();

        if (mesasFaltantes.Count == 0)
        {
            return;
        }

        await context.Mesas.AddRangeAsync(mesasFaltantes, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task NormalizeMesasAsync(ReservationDbContext context, CancellationToken cancellationToken)
    {
        var mesas = await context.Mesas
            .Include(mesa => mesa.Reservas)
            .OrderBy(mesa => mesa.IdMesa)
            .ToListAsync(cancellationToken);

        if (mesas.Count <= 20 && mesas.Select(mesa => mesa.Numero).Distinct().Count() == mesas.Count)
        {
            return;
        }

        var selected = new List<Mesa>(20);
        var remaining = new List<Mesa>(mesas);

        // Keep one physical table for every visible number, preferring tables that already have reservations.
        for (var number = 1; number <= 20; number++)
        {
            var candidate = remaining
                .Where(mesa => mesa.Numero == number)
                .OrderByDescending(mesa => mesa.Reservas.Count)
                .ThenBy(mesa => mesa.IdMesa)
                .FirstOrDefault()
                ?? remaining
                    .OrderByDescending(mesa => mesa.Reservas.Count)
                    .ThenBy(mesa => mesa.IdMesa)
                    .FirstOrDefault();

            if (candidate is null)
            {
                break;
            }

            candidate.Numero = number;
            selected.Add(candidate);
            remaining.Remove(candidate);
        }

        if (selected.Count != 20)
        {
            return;
        }

        var occupiedSlots = selected
            .SelectMany(mesa => mesa.Reservas
                .Where(reserva => reserva.Estado != EstadosReserva.Cancelada)
                .Select(reserva => (mesa.IdMesa, reserva.Fecha, reserva.Hora)))
            .ToHashSet();

        foreach (var duplicate in remaining)
        {
            foreach (var reserva in duplicate.Reservas.ToList())
            {
                var target = selected
                    .Where(mesa => mesa.Capacidad >= reserva.CantidadPersonas)
                    .OrderByDescending(mesa => mesa.Numero == duplicate.Numero)
                    .ThenBy(mesa => mesa.Reservas.Count)
                    .ThenBy(mesa => mesa.IdMesa)
                    .FirstOrDefault(mesa => reserva.Estado == EstadosReserva.Cancelada ||
                        !occupiedSlots.Contains((mesa.IdMesa, reserva.Fecha, reserva.Hora)));

                // Keep the original table if a safe reassignment is not possible.
                if (target is null)
                {
                    continue;
                }

                reserva.IdMesa = target.IdMesa;
                reserva.Mesa = target;
                target.Reservas.Add(reserva);

                if (reserva.Estado != EstadosReserva.Cancelada)
                {
                    occupiedSlots.Add((target.IdMesa, reserva.Fecha, reserva.Hora));
                }
            }
        }

        var deletableDuplicates = remaining.Where(mesa => mesa.Reservas.Count == 0).ToList();
        if (deletableDuplicates.Count == 0)
        {
            return;
        }

        context.Mesas.RemoveRange(deletableDuplicates);
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
            },
            ..PlatosAdicionales()
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

        nuevos.AddRange(PlatosAdicionales().Where(plato => !nombres.Contains(plato.Nombre)));

        if (nuevos.Count == 0)
        {
            return;
        }

        await context.Platos.AddRangeAsync(nuevos, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<Plato> PlatosAdicionales() =>
    [
        new Plato
        {
            Nombre = "Chicha morada de la casa",
            Categoria = "Bebida",
            Descripcion = "Bebida tradicional de maiz morado, canela y fruta, servida bien fria.",
            Precio = 8m,
            ImagenUrl = "/img/platos/chicha-morada.png"
        },
        new Plato
        {
            Nombre = "Mazamorra de quinua",
            Categoria = "Postre",
            Descripcion = "Postre suave de quinua, leche y especias dulces inspirado en la cocina andina.",
            Precio = 10m,
            ImagenUrl = "/img/platos/qapchi.png"
        },
        new Plato
        {
            Nombre = "Adobo ayacuchano",
            Categoria = "Fondo",
            Descripcion = "Cerdo marinado con aji, chicha de jora y especias, servido con pan artesanal.",
            Precio = 24m,
            ImagenUrl = "/img/platos/puca-picante.png"
        },
        new Plato
        {
            Nombre = "Caldo de cabeza",
            Categoria = "Tradicional",
            Descripcion = "Caldo intenso con hierbabuena, mote y papa para empezar el dia con energia.",
            Precio = 20m,
            ImagenUrl = "/img/platos/mondongo.png"
        },
        new Plato
        {
            Nombre = "Teqte ayacuchano",
            Categoria = "Entrada",
            Descripcion = "Guiso de arvejas, queso fresco, papa y hierbas de la region.",
            Precio = 16m,
            ImagenUrl = "/img/platos/qapchi.png"
        },
        new Plato
        {
            Nombre = "Chicharron con mote",
            Categoria = "Fondo",
            Descripcion = "Trozos crocantes de cerdo acompanados con mote, papa dorada y salsa criolla.",
            Precio = 26m,
            ImagenUrl = "/img/platos/cuy-chactado.png"
        },
        new Plato
        {
            Nombre = "Sopa de quinua",
            Categoria = "Tradicional",
            Descripcion = "Sopa nutritiva con quinua, verduras andinas y hierbas aromaticas.",
            Precio = 15m,
            ImagenUrl = "/img/platos/mondongo.png"
        },
        new Plato
        {
            Nombre = "Trucha andina",
            Categoria = "Especial",
            Descripcion = "Trucha dorada con papas nativas, ensalada fresca y salsa de huacatay.",
            Precio = 30m,
            ImagenUrl = "/img/platos/cuy-chactado.png"
        },
        new Plato
        {
            Nombre = "Humitas ayacuchanas",
            Categoria = "Entrada",
            Descripcion = "Maiz tierno molido, envuelto en panca y servido con queso fresco.",
            Precio = 12m,
            ImagenUrl = "/img/platos/qapchi.png"
        },
        new Plato
        {
            Nombre = "Dulce de calabaza",
            Categoria = "Postre",
            Descripcion = "Postre tradicional de calabaza confitada con canela y clavo de olor.",
            Precio = 11m,
            ImagenUrl = "/img/platos/puca-picante.png"
        }
    ];
}
