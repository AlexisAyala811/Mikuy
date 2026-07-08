# Mikuy
Sistema web de reservas para el restaurante Mikuy, orientado a clientes que desean reservar mesa y a administradores que gestionan reservas, clientes, mesas y platos. El proyecto prioriza una experiencia calida, clara y funcional para un restaurante de cocina ayacuchana.

## Stack
- Lenguaje: C# con .NET 10
- Framework / runtime: ASP.NET Core MVC
- Base de datos: PostgreSQL con Entity Framework Core y Npgsql
- Tests: MSTest + Moq
- Frontend: Razor Views, Bootstrap, CSS personalizado y JavaScript propio
- Despliegue: Docker + Railway

## Comandos
- `dotnet run --project Reserva.Web\Reserva.Web.csproj --urls http://127.0.0.1:5089` — arranca el servidor en local
- `dotnet test Reserva.Tests\Reserva.Tests.csproj --no-restore --verbosity minimal` — ejecuta los tests; deben pasar antes de cada commit
- `dotnet build Reserva.Web\Reserva.Web.csproj --no-restore` — compila la aplicacion web
- `dotnet publish Reserva.Web\Reserva.Web.csproj --configuration Release --output artifacts\publish` — compila para produccion
- `docker compose up -d` — levanta PostgreSQL local con la configuracion de `docker-compose.yml`
- `docker build -t mikuy .` — construye la imagen Docker de la aplicacion
- `dotnet ef database update --project Reserva.Infrastructure\Reserva.Infrastructure.csproj --startup-project Reserva.Web\Reserva.Web.csproj` — aplica migraciones a la base de datos configurada

## Estructura del proyecto
- `Reserva.Domain/` — entidades del dominio, validaciones e interfaces principales
- `Reserva.Infrastructure/` — Entity Framework Core, migraciones, repositorios, seeders y seguridad de contrasenas
- `Reserva.Web/` — aplicacion ASP.NET Core MVC, controladores, vistas, servicios, CSS y JavaScript
- `Reserva.Tests/` — pruebas unitarias y reglas de negocio del sistema
- `Reserva.Web/wwwroot/` — assets publicos: CSS, JS, imagenes y librerias del frontend
- `Reserva.Infrastructure/Persistence/Migrations/` — migraciones PostgreSQL de Entity Framework Core
- `docs/` — documentacion complementaria del proyecto
- `Dockerfile` — configuracion para despliegue en Railway
- `docker-compose.yml` — PostgreSQL local para desarrollo

## Convenciones
- Usar PascalCase para clases, propiedades publicas, metodos publicos y entidades C#.
- Usar camelCase para variables locales y parametros.
- Mantener controladores MVC delgados; la logica compleja debe vivir en servicios, repositorios o reglas bien aisladas.
- Validar toda entrada del usuario antes de guardarla o usarla para consultar datos.
- Mantener las vistas Razor claras, con HTML semantico y clases CSS reutilizables.
- Los estilos globales viven principalmente en `Reserva.Web/wwwroot/css/site.css` y las animaciones reutilizables en `motion-system.css`.
- Las pruebas nuevas deben ir en `Reserva.Tests/`, agrupadas por tema o regla de negocio.
- Las migraciones deben generarse para PostgreSQL, no para SQL Server.
- Antes de cambiar flujo de reservas, comprobar disponibilidad, estados y consulta publica.
- Antes de desplegar, ejecutar `dotnet build` y `dotnet test`.

## No hagas
- No volver a agregar `Microsoft.EntityFrameworkCore.SqlServer`; la base oficial del proyecto es PostgreSQL.
- No cambiar la cadena de conexion de produccion a `localhost` para Railway.
- No subir archivos `.env`, secretos, claves SMTP, credenciales reales ni configuraciones privadas.
- No eliminar migraciones sin revisar el impacto en Railway o en la base PostgreSQL.
- No tocar cambios ajenos ni revertir archivos sin confirmacion.
- No romper el flujo de cliente registrado: si existe cookie `Mikuy.ClienteId`, la reserva debe autocompletar datos y mostrar solo comentario.
- No agregar dependencias nuevas sin justificar su necesidad.
- No dejar errores visuales de contraste en textos importantes como codigos de reserva, estados o disponibilidad.
- No subir carpetas generadas como `bin/`, `obj/`, `.vs/`, `TestResults/` o `artifacts/`.

## Flujo de trabajo
- Antes de una tarea no trivial, propón un plan breve y espera mi OK si el cambio puede afectar arquitectura, base de datos o despliegue.
- Una tarea a la vez; al terminar, dime que cambiaste y que verificaste.
- Si no estas seguro al 80%, pregunta. No inventes.
- Para cambios de codigo, compila antes de cerrar la tarea.
- Para cambios de reglas de negocio, ejecuta las pruebas.
- Para cambios de Railway, confirma que `DATABASE_URL` o variables `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD` esten configuradas en el servicio web.
- Para cambios en GitHub, crear commits claros y subirlos a `origin/main` solo cuando el usuario lo solicite.

## Documentacion
- `README.md` — resumen general del sistema, funcionalidades y ejecucion local.
- `Reserva.Web/appsettings.json` — configuracion local de PostgreSQL y correo.
- `docker-compose.yml` — servicio PostgreSQL local.
- `Dockerfile` — despliegue Docker usado por Railway.
- `Reserva.Tests/` — ejemplos de reglas esperadas del negocio.
- Railway: el servicio web debe recibir `DATABASE_URL=${{Postgres.DATABASE_URL}}` o variables PostgreSQL equivalentes.
