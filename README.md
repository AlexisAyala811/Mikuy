# Mikuy

Sistema web de reservas para el restaurante Mikuy, orientado a cocina tradicional de Ayacucho en Huamanga.

## Funcionalidades

- Portada publica, menu de platos, seccion cultural y contacto.
- Registro de clientes y solicitud publica de reservas.
- Consulta dinamica de horarios disponibles con asignacion de mesa por capacidad.
- Panel administrativo con indicadores, ultimas reservas y accesos de gestion.
- CRUD administrativo de clientes, reservas, mesas y platos.
- Confirmacion/cancelacion de reservas mediante estados.
- Acceso administrativo con cookie de autenticacion y contrasenas almacenadas con PBKDF2.
- Enlace de WhatsApp prellenado con detalle completo para avisar confirmaciones o cancelaciones.
- Consulta publica del estado de una reserva por correo y telefono, con opcion de cancelacion por el cliente.
- Envio de correo HTML configurable por SMTP cuando una reserva pasa a confirmada o cancelada.

## Proyectos

- `Reserva.Domain`: entidades, validaciones e interfaces.
- `Reserva.Infrastructure`: EF Core, migraciones, seed, repositorios y seguridad de password.
- `Reserva.Web`: ASP.NET MVC, vistas publicas y administracion.
- `Reserva.Tests`: pruebas de reglas y contratos del dominio.

## Ejecucion

1. Ajustar la cadena `DefaultConnection` si no se usa SQL Server LocalDB.
2. Aplicar migraciones:

   ```powershell
   dotnet ef database update --project Reserva.Infrastructure\Reserva.Infrastructure.csproj --startup-project Reserva.Web\Reserva.Web.csproj
   ```

3. Ejecutar:

   ```powershell
   dotnet run --project Reserva.Web\Reserva.Web.csproj
   ```

El seed crea platos, mesas y usuarios administrativos de demostracion. Cambie esas credenciales antes de usar el sistema fuera de un entorno local.

## Notificaciones

El aviso por WhatsApp usa enlaces `wa.me`, por lo que abre WhatsApp Web o la app con el mensaje listo; el envio final lo realiza manualmente el administrador. El mensaje incluye fecha, hora, personas, mesa, comentario, direccion y recomendaciones para el cliente.

El correo queda desactivado por defecto. Cuando se habilita, el sistema envia una plantilla HTML con tematica Mikuy e imagen del restaurante al confirmar o cancelar una reserva. Para habilitarlo, configure la seccion `Email` en `Reserva.Web/appsettings.json` o con secretos de usuario/variables de entorno:

```json
"Email": {
  "Enabled": true,
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "UserName": "correo@dominio.com",
  "Password": "password-o-app-password",
  "FromEmail": "correo@dominio.com",
  "FromName": "Mikuy Reservas"
}
```
