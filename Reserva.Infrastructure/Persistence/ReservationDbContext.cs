using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entities;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Infrastructure.Persistence;

public sealed class ReservationDbContext : DbContext
{
    public ReservationDbContext(DbContextOptions<ReservationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<ReservaEntity> Reservas => Set<ReservaEntity>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Mesa> Mesas => Set<Mesa>();

    public DbSet<Plato> Platos => Set<Plato>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCliente(modelBuilder);
        ConfigureMesa(modelBuilder);
        ConfigurePlato(modelBuilder);
        ConfigureReserva(modelBuilder);
        ConfigureUsuario(modelBuilder);
    }

    private static void ConfigureCliente(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");

            entity.HasKey(cliente => cliente.IdCliente);

            entity.Property(cliente => cliente.IdCliente)
                .ValueGeneratedOnAdd();

            entity.Property(cliente => cliente.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(cliente => cliente.Telefono)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(cliente => cliente.Correo)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(cliente => cliente.Correo)
                .IsUnique();

            entity.HasIndex(cliente => cliente.Telefono);
        });
    }

    private static void ConfigureReserva(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReservaEntity>(entity =>
        {
            entity.ToTable("Reservas", table =>
            {
                table.HasCheckConstraint("CK_Reservas_Estado", "\"Estado\" IN ('Pendiente', 'Confirmada', 'Cancelada')");
            });

            entity.HasKey(reserva => reserva.IdReserva);

            entity.Property(reserva => reserva.IdReserva)
                .ValueGeneratedOnAdd();

            entity.Property(reserva => reserva.CodigoReserva)
                .IsRequired()
                .HasMaxLength(24);

            entity.Property(reserva => reserva.Fecha)
                .IsRequired()
                .HasColumnType("date");

            entity.Property(reserva => reserva.Hora)
                .IsRequired()
                .HasColumnType("time(0) without time zone");

            entity.Property(reserva => reserva.Estado)
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue(EstadosReserva.Pendiente);

            entity.Property(reserva => reserva.IdCliente)
                .IsRequired()
                .HasColumnName("ClienteIdCliente");

            entity.Property(reserva => reserva.CantidadPersonas)
                .IsRequired();

            entity.Property(reserva => reserva.Comentario)
                .HasMaxLength(300);

            entity.Property(reserva => reserva.IdMesa)
                .IsRequired();

            entity.HasOne(reserva => reserva.Cliente)
                .WithMany(cliente => cliente.Reservas)
                .HasForeignKey(reserva => reserva.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(reserva => reserva.Mesa)
                .WithMany(mesa => mesa.Reservas)
                .HasForeignKey(reserva => reserva.IdMesa)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(reserva => new { reserva.IdMesa, reserva.Fecha, reserva.Hora })
                .IsUnique()
                .HasFilter("\"Estado\" <> 'Cancelada'")
                .HasDatabaseName("UX_Reservas_Mesa_Fecha_Hora");

            entity.HasIndex(reserva => reserva.Estado);

            entity.HasIndex(reserva => reserva.CodigoReserva)
                .IsUnique();
        });
    }

    private static void ConfigureMesa(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mesa>(entity =>
        {
            entity.ToTable("Mesas");
            entity.HasKey(mesa => mesa.IdMesa);
            entity.Property(mesa => mesa.IdMesa).ValueGeneratedOnAdd();
            entity.Property(mesa => mesa.Ubicacion).IsRequired().HasMaxLength(80);
            entity.Property(mesa => mesa.Activa).HasDefaultValue(true);
            entity.HasIndex(mesa => mesa.Numero);
        });
    }

    private static void ConfigurePlato(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plato>(entity =>
        {
            entity.ToTable("Platos");
            entity.HasKey(plato => plato.IdPlato);
            entity.Property(plato => plato.IdPlato).ValueGeneratedOnAdd();
            entity.Property(plato => plato.Nombre).IsRequired().HasMaxLength(120);
            entity.Property(plato => plato.Descripcion).IsRequired().HasMaxLength(320);
            entity.Property(plato => plato.Categoria).IsRequired().HasMaxLength(60);
            entity.Property(plato => plato.Precio).HasColumnType("decimal(8,2)");
            entity.Property(plato => plato.ImagenUrl).IsRequired().HasMaxLength(220);
            entity.Property(plato => plato.Activo).HasDefaultValue(true);
        });
    }

    private static void ConfigureUsuario(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");

            entity.HasKey(usuario => usuario.IdUsuario);

            entity.Property(usuario => usuario.IdUsuario)
                .ValueGeneratedOnAdd();

            entity.Property(usuario => usuario.UsuarioNombre)
                .IsRequired()
                .HasColumnName("Usuario")
                .HasMaxLength(50);

            entity.Property(usuario => usuario.Password)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(usuario => usuario.Rol)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(usuario => usuario.UsuarioNombre)
                .IsUnique();
        });
    }
}
