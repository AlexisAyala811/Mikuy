# Prototipo y tecnologias de interfaz

> El nombre `Desing.md` se conserva para coincidir con el artefacto solicitado.

## Tecnologias utilizadas

- Razor Views para renderizado del servidor.
- HTML5 semantico.
- Bootstrap para grilla, utilidades y componentes base.
- CSS propio en `site.css`.
- Sistema de movimiento reutilizable en `motion-system.css`.
- JavaScript propio en `site.js` y `reservas.js`.
- jQuery Validation y Unobtrusive Validation para formularios.

## Estructura visual

### Sitio publico

```text
+--------------------------------------------------------------+
| Mikuy | Inicio Platos Cultura Reservas Consultar Contacto     |
+--------------------------------------------------------------+
| Hero: cocina ayacuchana + CTA principal                      |
+--------------------------------------------------------------+
| Contenido contextual: platos, cultura, reserva o contacto    |
+--------------------------------------------------------------+
| Footer: horario, contacto y ubicacion                         |
+--------------------------------------------------------------+
```

### Reserva en dos pasos

```text
+-----------------------+  +-----------------------------------+
| Contexto del servicio |  | 1. Reserva | 2. Contacto          |
| y propuesta de valor  |  | Fecha Hora Personas              |
|                       |  | Estado de disponibilidad          |
|                       |  | Datos o comentario + Solicitar    |
+-----------------------+  +-----------------------------------+
```

### Administracion

```text
+--------------------------------------------------------------+
| Mikuy                                             Salir       |
+--------------------------------------------------------------+
| Dashboard | Reservas | Clientes | Mesas | Platos             |
+--------------------------------------------------------------+
| Busqueda global                                              |
| Estado operativo                                             |
| Reservas pendientes                                          |
| Agenda y KPIs                                                |
+--------------------------------------------------------------+
```

## Prototipos existentes

El repositorio conserva prototipos visuales en:

- `docs/prototipos_mikuy/01_prototipo_reserva_fecha_hora.png`
- `docs/prototipos_mikuy/02_prototipo_reserva_datos_cliente.png`
- `docs/prototipos_mikuy/03_prototipo_consulta_comprobante.png`
- `docs/prototipos_mikuy/04_prototipo_dashboard_administrativo.png`

Tambien existen iteraciones anteriores en `docs/prototipos/`.

## Componentes y estados

### Botones

- Principal: terracota, contraste alto y respuesta al hover/clic.
- Secundario: borde o enlace subrayado, sin competir con el CTA.
- Destructivo: rojo y confirmacion previa.
- Deshabilitado: contraste suficiente y cursor coherente.

### Formularios

- Etiquetas visibles sobre cada campo.
- Mensajes de validacion junto al dato incorrecto.
- Foco claro para teclado.
- Datos de cliente reconocido autocompletados.

### Estados

- Cargando: mensaje breve y control bloqueado solo mientras sea necesario.
- Disponible: verde y texto explicito.
- No disponible: alternativa de horario.
- Error: causa comprensible y accion de recuperacion.
- Vacio: explicacion y siguiente accion.
- Exito: codigo y acciones posteriores.

## Movimiento

- Entrada escalonada del hero.
- Parallax y Ken Burns sutiles.
- Aparicion progresiva de secciones al hacer scroll.
- Microinteracciones entre 150 y 300 ms.
- Animaciones desactivadas o reducidas en dispositivos tactiles y con
  `prefers-reduced-motion`.

## Responsive

- La navegacion colapsa en pantallas pequenas.
- Los formularios pasan de dos columnas a una.
- Las tablas deben permitir lectura o desplazamiento horizontal controlado.
- Los CTA conservan un area tactil minima de 44 por 44 px.

