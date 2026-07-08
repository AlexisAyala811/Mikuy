# Estrategia de pruebas de Mikuy

## Objetivo

Comprobar que el sistema conserva las reglas de reserva, la seguridad y los
flujos esenciales mientras evoluciona la interfaz o la infraestructura.

## Estado actual

- Framework: MSTest.
- Dobles de prueba: Moq.
- Pruebas unitarias: `Reserva.Tests`.
- Pruebas de integracion: `Reserva.IntegrationTests`.
- Base de integracion: PostgreSQL 17 temporal mediante Testcontainers.
- Total actual: 58 pruebas automatizadas (51 unitarias y 7 de integracion).

## Distribucion

| Archivo | Enfoque |
|---|---|
| `ReservasTests.cs` | Acciones y validaciones del flujo de reservas |
| `ClientesTests.cs` | Registro, consulta y mantenimiento de clientes |
| `SeguridadTests.cs` | Autorizacion, cookies y hash de contrasenas |
| `MikuyModelTests.cs` | Validaciones de entidades y modelos |
| `MikuyBusinessRulesTests.cs` | Disponibilidad, capacidad, estados y KPIs |
| `MikuyIntegrationTests.cs` | HTTP MVC, autenticacion, cookies y restricciones PostgreSQL |

## Integracion implementada

Las pruebas de `Reserva.IntegrationTests` arrancan la aplicacion mediante
`WebApplicationFactory`, aplican las migraciones y ejecutan el seeder sobre un
contenedor PostgreSQL desechable. La base se elimina al finalizar la suite.

Actualmente verifican:

- Respuesta HTTP de la pagina principal.
- Consulta publica de disponibilidad con una mesa adecuada.
- Redireccion al login desde una ruta administrativa protegida.
- Login administrativo, confirmacion de reserva y aviso protegido por WhatsApp.
- Registro persistente de un cliente y creacion de `Mikuy.ClienteId`.
- Restriccion unica del correo del cliente.
- Restriccion de doble reserva activa para una misma mesa, fecha y hora.

## Piramide recomendada

```text
               / E2E \
              /-------\
             /Integracion\
            /-------------\
           /   Unitarias   \
          /-----------------\
```

- Muchas pruebas unitarias para reglas y transformaciones.
- Pruebas de integracion para EF Core y PostgreSQL.
- Pocas pruebas end-to-end para recorridos de alto valor.

## Casos criticos

### Disponibilidad

- Encuentra mesa activa con capacidad suficiente.
- Rechaza mesas inactivas o pequenas.
- No ofrece una mesa ocupada en fecha y hora.
- Una reserva cancelada vuelve a liberar la mesa.
- Dos solicitudes concurrentes no ocupan la misma mesa.

### Reserva publica

- Requiere fecha, hora, personas y contacto valido.
- Rechaza fechas u horas fuera de reglas.
- Reconoce al cliente registrado.
- Genera estado Pendiente y codigo unico.
- Muestra confirmacion y comprobante.

### Consulta

- Encuentra por codigo.
- Encuentra por correo o telefono.
- No expone reservas ajenas con datos insuficientes.
- Sincroniza estado entre ultima reserva y resultado.
- Cancela solo una reserva permitida.

### Administracion

- Requiere autenticacion.
- Confirma y cancela estados validos.
- Calcula pendientes, confirmadas, canceladas y ocupacion.
- Filtra y ordena reservas, clientes y mesas.
- Impide eliminar datos relacionados de forma insegura.

### Seguridad

- Las contrasenas se verifican mediante hash.
- Los POST incluyen antiforgery.
- Las cookies sensibles tienen configuracion segura.
- Los DTOs invalidos no llegan a persistencia.
- Los secretos no forman parte del repositorio.

## Pruebas pendientes recomendadas

1. Ejecucion idempotente del seeder sobre una base ya poblada.
2. Concurrencia simultanea de dos solicitudes HTTP para la misma mesa.
3. Flujo E2E: disponibilidad, reserva y confirmacion.
4. Flujo E2E: acceso de cliente y autocompletado.
5. Flujo E2E: administrador confirma una reserva.
6. Accesibilidad automatizada de paginas publicas y administrativas.
7. Responsive visual en movil, tableta y escritorio.
8. Arranque del contenedor Docker con variables equivalentes a Railway.
9. Fallo controlado cuando PostgreSQL no esta disponible.

## Ejecucion

```powershell
dotnet test Reserva.Tests\Reserva.Tests.csproj --no-restore --verbosity minimal
```

Con Docker Desktop en ejecucion:

```powershell
dotnet test Reserva.IntegrationTests\Reserva.IntegrationTests.csproj --no-restore --verbosity minimal
```

La primera ejecucion descarga la imagen `postgres:17-alpine`. No utiliza ni
modifica la base local del desarrollador ni la base desplegada en Railway.

## Criterio para commits

- Todas las pruebas existentes deben pasar.
- Una regla nueva debe incluir al menos una prueba positiva y una negativa.
- Una correccion debe incluir una prueba que falle antes del arreglo.
- Los cambios de base de datos deben probar migracion y restricciones.
- Los cambios visuales deben verificarse en viewport movil y escritorio.
