# Analisis funcional de Mikuy

## 1. Contexto

Mikuy es un sistema web para un restaurante de cocina ayacuchana. Atiende dos
grupos principales:

- Clientes que desean conocer el restaurante, revisar platos, comprobar
  disponibilidad, reservar una mesa y consultar el estado de su solicitud.
- Personal administrativo que necesita confirmar reservas, organizar mesas,
  consultar clientes y mantener el catalogo de platos.

## 2. Problema

La gestion manual por telefono o mensajes genera incertidumbre para el cliente
y trabajo repetitivo para el restaurante. Los problemas principales son:

- El cliente no sabe si existe una mesa adecuada antes de enviar la solicitud.
- Los datos de contacto se vuelven a escribir aunque el cliente ya se registro.
- Una reserva puede perderse si el usuario no conserva su codigo.
- El administrador necesita detectar rapidamente solicitudes pendientes,
  conflictos y nivel de ocupacion.
- La asignacion manual puede duplicar una mesa para la misma fecha y hora.
- La informacion de clientes, mesas, platos y reservas puede quedar dispersa.

## 3. Necesidades

### Cliente

- Consultar disponibilidad en tiempo real por fecha, hora y cantidad de personas.
- Completar la reserva en dos pasos simples.
- Identificarse por correo o telefono si ya esta registrado.
- Recibir un codigo unico y visible.
- Consultar o cancelar su reserva sin crear una cuenta obligatoria.
- Descargar un comprobante y recibir notificaciones del cambio de estado.
- Usar el sitio desde telefono, tableta o computadora.

### Administrador

- Ver primero las reservas que requieren accion.
- Confirmar o cancelar sin abandonar el dashboard.
- Buscar por cliente, codigo, fecha o estado.
- Evitar cruces de horario y mesas repetidas.
- Conocer reservas, clientes esperados, ocupacion y conflictos.
- Gestionar clientes, mesas y platos desde un modulo protegido.

## 4. Requisitos funcionales

| ID | Requisito | Prioridad |
|---|---|---|
| RF-01 | Mostrar platos activos e informacion institucional | Media |
| RF-02 | Consultar disponibilidad por fecha, hora y personas | Critica |
| RF-03 | Sugerir una mesa activa con capacidad suficiente | Critica |
| RF-04 | Registrar una reserva inicialmente como Pendiente | Critica |
| RF-05 | Generar un codigo con formato `MIK-AAAAMMDD-NNNN` | Alta |
| RF-06 | Registrar o reconocer clientes por correo o telefono | Alta |
| RF-07 | Consultar reservas por codigo o dato de contacto | Alta |
| RF-08 | Permitir al cliente cancelar una reserva valida | Alta |
| RF-09 | Generar comprobante PDF de la reserva | Media |
| RF-10 | Autenticar al administrador | Critica |
| RF-11 | Confirmar o cancelar reservas desde administracion | Critica |
| RF-12 | Administrar clientes, mesas y platos | Alta |
| RF-13 | Mostrar agenda, pendientes y estado operativo | Alta |
| RF-14 | Enviar notificaciones de cambios relevantes | Media |

## 5. Requisitos no funcionales

- **Usabilidad:** lenguaje claro, formularios breves y estados visibles.
- **Accesibilidad:** contraste legible, foco de teclado y movimiento reducible.
- **Rendimiento:** assets optimizados y animaciones basadas en transformaciones.
- **Seguridad:** validacion del servidor, cookies `HttpOnly`, antiforgery y
  contrasenas almacenadas como hash.
- **Integridad:** una mesa no puede tener dos reservas no canceladas en la misma
  fecha y hora.
- **Disponibilidad:** la aplicacion debe poder ejecutarse en Docker y Railway.
- **Mantenibilidad:** separacion entre dominio, infraestructura, web y pruebas.
- **Compatibilidad:** interfaz adaptable a navegadores modernos y moviles.

## 6. Reglas de negocio

1. La reserva admite entre 1 y 24 personas.
2. Solo se consideran mesas activas y con capacidad suficiente.
3. Una reserva puede estar `Pendiente`, `Confirmada` o `Cancelada`.
4. Las reservas canceladas liberan la mesa para el mismo horario.
5. El codigo de reserva es unico.
6. El correo del cliente es unico.
7. No se elimina un cliente que tenga reservas asociadas sin resolver su
   historial.
8. Las rutas administrativas requieren autenticacion.
9. El cliente registrado conserva su identificacion mediante la cookie
   `Mikuy.ClienteId`.
10. PostgreSQL aplica una restriccion unica parcial para evitar cruces de mesa.

## 7. Tareas de usuario

### TU-01: Comprobar disponibilidad

**Como** cliente, **quiero** seleccionar fecha, hora y personas, **para** saber
si el restaurante puede recibirme antes de compartir mis datos.

**Criterios de aceptacion:**

- Se muestra un estado de carga durante la consulta.
- Si existe capacidad, se informa la mesa sugerida.
- Si no existe capacidad, se ofrece cambiar horario o cantidad.
- Un error de red se diferencia de la falta de disponibilidad.

### TU-02: Solicitar una reserva

**Como** cliente, **quiero** completar mis datos y un comentario opcional,
**para** enviar una solicitud al restaurante.

**Criterios de aceptacion:**

- La disponibilidad se valida otra vez en el servidor.
- Un cliente reconocido no vuelve a escribir nombre, telefono y correo.
- La reserva queda Pendiente y recibe un codigo unico.
- La confirmacion permite copiar el codigo y consultar el estado.

### TU-03: Recuperar una reserva

**Como** cliente, **quiero** buscar por codigo, correo o telefono, **para**
recuperar mi reserva aunque no recuerde todos sus datos.

### TU-04: Operar reservas pendientes

**Como** administrador, **quiero** confirmar o cancelar solicitudes desde el
dashboard, **para** responder con pocos pasos y reducir errores.

### TU-05: Administrar capacidad

**Como** administrador, **quiero** activar, editar y ordenar mesas unicas,
**para** representar la capacidad fisica real del restaurante.

## 8. Alcance actual

El producto implementa reserva publica, disponibilidad, consulta y cancelacion,
acceso de clientes, comprobante PDF, notificaciones, dashboard y CRUD
administrativo. La aplicacion usa PostgreSQL y esta preparada para Docker y
Railway.

## 9. Fuera de alcance inmediato

- Pago en linea.
- Seleccion visual de una mesa por parte del cliente.
- Programa de fidelidad.
- Aplicacion movil nativa.
- Integracion bidireccional con WhatsApp Business.
- Multiples locales o zonas horarias.

## 10. Riesgos y mejoras futuras

- Reemplazar la cookie de cliente por autenticacion persistente si se requiere
  historial privado.
- Incorporar concurrencia optimista y reintentos claros ante reservas simultaneas.
- Configurar un proveedor real de correo en produccion.
- Medir conversion, abandono del formulario y tiempos de respuesta.
- Agregar pruebas de integracion con PostgreSQL y pruebas end-to-end.

