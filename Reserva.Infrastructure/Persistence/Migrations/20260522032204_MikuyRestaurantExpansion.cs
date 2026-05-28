using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reserva.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MikuyRestaurantExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'UX_Reservas_Fecha_Hora'
                      AND object_id = OBJECT_ID(N'Reservas')
                )
                DROP INDEX [UX_Reservas_Fecha_Hora] ON [Reservas];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'Reservas', N'ClienteIdCliente') IS NULL
                   AND COL_LENGTH(N'Reservas', N'IdCliente') IS NOT NULL
                    EXEC sp_rename N'Reservas.IdCliente', N'ClienteIdCliente', N'COLUMN';

                IF COL_LENGTH(N'Reservas', N'CantidadPersonas') IS NULL
                    ALTER TABLE [Reservas] ADD [CantidadPersonas] int NOT NULL CONSTRAINT [DF_Reservas_CantidadPersonas] DEFAULT 0;

                IF COL_LENGTH(N'Reservas', N'Comentario') IS NULL
                    ALTER TABLE [Reservas] ADD [Comentario] nvarchar(300) NULL;

                IF COL_LENGTH(N'Reservas', N'IdMesa') IS NULL
                    ALTER TABLE [Reservas] ADD [IdMesa] int NOT NULL CONSTRAINT [DF_Reservas_IdMesa] DEFAULT 0;
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'Mesas', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Mesas] (
                        [IdMesa] int NOT NULL IDENTITY,
                        [Numero] int NOT NULL,
                        [Capacidad] int NOT NULL,
                        [Ubicacion] nvarchar(80) NOT NULL,
                        [Activa] bit NOT NULL CONSTRAINT [DF_Mesas_Activa] DEFAULT CAST(1 AS bit),
                        CONSTRAINT [PK_Mesas] PRIMARY KEY ([IdMesa])
                    );
                END;

                IF OBJECT_ID(N'Platos', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Platos] (
                        [IdPlato] int NOT NULL IDENTITY,
                        [Nombre] nvarchar(120) NOT NULL,
                        [Descripcion] nvarchar(320) NOT NULL,
                        [Categoria] nvarchar(60) NOT NULL,
                        [Precio] decimal(8,2) NOT NULL,
                        [ImagenUrl] nvarchar(220) NOT NULL,
                        [Activo] bit NOT NULL CONSTRAINT [DF_Platos_Activo] DEFAULT CAST(1 AS bit),
                        CONSTRAINT [PK_Platos] PRIMARY KEY ([IdPlato])
                    );
                END;

                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'Mesas', N'Numero') IS NULL
                    ALTER TABLE [Mesas] ADD [Numero] int NOT NULL CONSTRAINT [DF_Mesas_Numero] DEFAULT 0;
                IF COL_LENGTH(N'Mesas', N'Capacidad') IS NULL
                    ALTER TABLE [Mesas] ADD [Capacidad] int NOT NULL CONSTRAINT [DF_Mesas_Capacidad] DEFAULT 2;
                IF COL_LENGTH(N'Mesas', N'Ubicacion') IS NULL
                    ALTER TABLE [Mesas] ADD [Ubicacion] nvarchar(80) NOT NULL CONSTRAINT [DF_Mesas_Ubicacion] DEFAULT N'Salon';
                IF COL_LENGTH(N'Mesas', N'Activa') IS NULL
                    ALTER TABLE [Mesas] ADD [Activa] bit NOT NULL CONSTRAINT [DF_Mesas_Activa_Upgrade] DEFAULT CAST(1 AS bit);

                IF COL_LENGTH(N'Platos', N'Descripcion') IS NULL
                    ALTER TABLE [Platos] ADD [Descripcion] nvarchar(320) NOT NULL CONSTRAINT [DF_Platos_Descripcion] DEFAULT N'Plato de Mikuy';
                IF COL_LENGTH(N'Platos', N'Categoria') IS NULL
                    ALTER TABLE [Platos] ADD [Categoria] nvarchar(60) NOT NULL CONSTRAINT [DF_Platos_Categoria] DEFAULT N'Tradicional';
                IF COL_LENGTH(N'Platos', N'Precio') IS NULL
                    ALTER TABLE [Platos] ADD [Precio] decimal(8,2) NOT NULL CONSTRAINT [DF_Platos_Precio] DEFAULT 0;
                IF COL_LENGTH(N'Platos', N'ImagenUrl') IS NULL
                    ALTER TABLE [Platos] ADD [ImagenUrl] nvarchar(220) NOT NULL CONSTRAINT [DF_Platos_ImagenUrl] DEFAULT N'/img/platos/puca-picante.png';
                IF COL_LENGTH(N'Platos', N'Activo') IS NULL
                    ALTER TABLE [Platos] ADD [Activo] bit NOT NULL CONSTRAINT [DF_Platos_Activo_Upgrade] DEFAULT CAST(1 AS bit);
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Mesas WHERE Numero = 1)
                    INSERT INTO Mesas (Numero, Capacidad, Ubicacion, Activa) VALUES (1, 2, N'Patio colonial', 1);
                IF NOT EXISTS (SELECT 1 FROM Mesas WHERE Numero = 2)
                    INSERT INTO Mesas (Numero, Capacidad, Ubicacion, Activa) VALUES (2, 4, N'Salon principal', 1);
                IF NOT EXISTS (SELECT 1 FROM Mesas WHERE Numero = 3)
                    INSERT INTO Mesas (Numero, Capacidad, Ubicacion, Activa) VALUES (3, 4, N'Vista a la plaza', 1);
                IF NOT EXISTS (SELECT 1 FROM Mesas WHERE Numero = 4)
                    INSERT INTO Mesas (Numero, Capacidad, Ubicacion, Activa) VALUES (4, 6, N'Salon familiar', 1);
                IF NOT EXISTS (SELECT 1 FROM Mesas WHERE Numero = 5)
                    INSERT INTO Mesas (Numero, Capacidad, Ubicacion, Activa) VALUES (5, 8, N'Zona de celebraciones', 1);
                """);

            migrationBuilder.Sql("""
                UPDATE Reservas
                SET IdMesa = (SELECT IdMesa FROM Mesas WHERE Numero = 1),
                    CantidadPersonas = CASE WHEN CantidadPersonas = 0 THEN 2 ELSE CantidadPersonas END
                WHERE IdMesa = 0;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'UX_Reservas_Mesa_Fecha_Hora'
                      AND object_id = OBJECT_ID(N'Reservas')
                )
                    CREATE UNIQUE INDEX [UX_Reservas_Mesa_Fecha_Hora] ON [Reservas] ([IdMesa], [Fecha], [Hora]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_Mesas_Numero'
                      AND object_id = OBJECT_ID(N'Mesas')
                )
                    CREATE INDEX [IX_Mesas_Numero] ON [Mesas] ([Numero]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_Reservas_Mesas_IdMesa'
                      AND parent_object_id = OBJECT_ID(N'Reservas')
                )
                    ALTER TABLE [Reservas] ADD CONSTRAINT [FK_Reservas_Mesas_IdMesa]
                    FOREIGN KEY ([IdMesa]) REFERENCES [Mesas] ([IdMesa]) ON DELETE NO ACTION;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_Mesas_IdMesa",
                table: "Reservas");

            migrationBuilder.DropTable(
                name: "Mesas");

            migrationBuilder.DropTable(
                name: "Platos");

            migrationBuilder.DropIndex(
                name: "UX_Reservas_Mesa_Fecha_Hora",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "CantidadPersonas",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "Comentario",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "IdMesa",
                table: "Reservas");

            migrationBuilder.CreateIndex(
                name: "UX_Reservas_Fecha_Hora",
                table: "Reservas",
                columns: new[] { "Fecha", "Hora" },
                unique: true);
        }
    }
}
