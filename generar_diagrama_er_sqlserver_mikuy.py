from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path("docs/diagrama_er_sqlserver_mikuy.png")
W, H = 2400, 1650

COLORS = {
    "bg": "#f5f7fb",
    "grid": "#edf1f6",
    "ink": "#182235",
    "muted": "#5c6878",
    "line": "#475569",
    "table": "#ffffff",
    "pk": "#dc2626",
    "fk": "#2563eb",
    "unique": "#7c3aed",
    "clientes": "#2563eb",
    "reservas": "#d97706",
    "mesas": "#0f766e",
    "platos": "#16a34a",
    "usuarios": "#6d5bd0",
}


def font(size, bold=False):
    base = Path(r"C:\Windows\Fonts")
    for name in [
        "arialbd.ttf" if bold else "arial.ttf",
        "segoeuib.ttf" if bold else "segoeui.ttf",
        "calibrib.ttf" if bold else "calibri.ttf",
    ]:
        candidate = base / name
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


F_TITLE = font(54, True)
F_SUBTITLE = font(28)
F_TABLE = font(31, True)
F_FIELD = font(24)
F_FIELD_BOLD = font(24, True)
F_NOTE = font(21)
F_LEGEND = font(23, True)
F_SMALL = font(19)


def centered(draw, box, text, fnt, fill=COLORS["ink"]):
    x1, y1, x2, y2 = box
    lines = text.split("\n")
    heights = []
    widths = []
    for line in lines:
        b = draw.textbbox((0, 0), line, font=fnt)
        widths.append(b[2] - b[0])
        heights.append(b[3] - b[1])
    total_h = sum(heights) + 8 * (len(lines) - 1)
    y = y1 + (y2 - y1 - total_h) / 2
    for line, w, h in zip(lines, widths, heights):
        draw.text((x1 + (x2 - x1 - w) / 2, y), line, font=fnt, fill=fill)
        y += h + 8


def rounded(draw, box, fill, outline="#cbd5e1", width=2, radius=18):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def label(draw, xy, text, fill, text_fill="#ffffff"):
    x, y = xy
    b = draw.textbbox((0, 0), text, font=F_SMALL)
    w = b[2] - b[0] + 22
    draw.rounded_rectangle((x, y, x + w, y + 30), radius=12, fill=fill)
    draw.text((x + 11, y + 6), text, font=F_SMALL, fill=text_fill)
    return w


def draw_table(draw, x, y, w, name, accent, rows, notes=None):
    row_h = 42
    header_h = 78
    note_h = 0 if not notes else 42 + 32 * len(notes)
    h = header_h + row_h * len(rows) + note_h + 22
    rounded(draw, (x, y, x + w, y + h), COLORS["table"], COLORS["line"], 3, 18)
    draw.rounded_rectangle((x, y, x + w, y + header_h), radius=18, fill="#e9eef7", outline=COLORS["line"], width=3)
    draw.rectangle((x + 3, y + header_h - 20, x + w - 3, y + header_h), fill="#e9eef7")
    draw.rectangle((x, y + header_h - 7, x + w, y + header_h), fill=accent)
    centered(draw, (x + 16, y + 11, x + w - 16, y + header_h - 10), name, F_TABLE)

    cy = y + header_h + 14
    for row in rows:
        marker, field, typ = row
        mx = x + 22
        if marker:
            if marker == "PK":
                color = COLORS["pk"]
            elif marker == "FK":
                color = COLORS["fk"]
            elif marker == "UQ":
                color = COLORS["unique"]
            else:
                color = COLORS["muted"]
            label(draw, (mx, cy + 4), marker, color)
            text_x = x + 82
        else:
            text_x = x + 34
        draw.text((text_x, cy + 6), field, font=F_FIELD_BOLD if marker in {"PK", "FK"} else F_FIELD, fill=COLORS["ink"])
        type_box = draw.textbbox((0, 0), typ, font=F_FIELD)
        draw.text((x + w - 26 - (type_box[2] - type_box[0]), cy + 6), typ, font=F_FIELD, fill=COLORS["muted"])
        cy += row_h

    if notes:
        draw.line((x + 22, cy + 8, x + w - 22, cy + 8), fill="#d7dee8", width=2)
        cy += 26
        for note in notes:
            draw.text((x + 28, cy), note, font=F_SMALL, fill=COLORS["muted"])
            cy += 32
    return (x, y, x + w, y + h)


def table_anchor(box, side, offset=0):
    x1, y1, x2, y2 = box
    cy = (y1 + y2) / 2 + offset
    if side == "left":
        return (x1, cy)
    if side == "right":
        return (x2, cy)
    if side == "top":
        return ((x1 + x2) / 2 + offset, y1)
    return ((x1 + x2) / 2 + offset, y2)


def crow_foot(draw, point, direction, color=COLORS["line"]):
    x, y = point
    spread = 22
    length = 36
    if direction == "left":
        draw.line((x, y, x - length, y - spread), fill=color, width=4)
        draw.line((x, y, x - length, y), fill=color, width=4)
        draw.line((x, y, x - length, y + spread), fill=color, width=4)
    elif direction == "right":
        draw.line((x, y, x + length, y - spread), fill=color, width=4)
        draw.line((x, y, x + length, y), fill=color, width=4)
        draw.line((x, y, x + length, y + spread), fill=color, width=4)
    elif direction == "down":
        draw.line((x, y, x - spread, y + length), fill=color, width=4)
        draw.line((x, y, x, y + length), fill=color, width=4)
        draw.line((x, y, x + spread, y + length), fill=color, width=4)
    else:
        draw.line((x, y, x - spread, y - length), fill=color, width=4)
        draw.line((x, y, x, y - length), fill=color, width=4)
        draw.line((x, y, x + spread, y - length), fill=color, width=4)


def one_marker(draw, point, direction, color=COLORS["line"]):
    x, y = point
    if direction in {"left", "right"}:
        draw.line((x, y - 24, x, y + 24), fill=color, width=5)
    else:
        draw.line((x - 24, y, x + 24, y), fill=color, width=5)


def relation(draw, start, end, label_text, many_at="end", label_shift=(0, 0), route_y=None):
    sx, sy = start
    ex, ey = end
    mid_x = (sx + ex) / 2
    color = COLORS["line"]
    if route_y is None:
        draw.line((sx, sy, mid_x, sy), fill=color, width=4)
        draw.line((mid_x, sy, mid_x, ey), fill=color, width=4)
        draw.line((mid_x, ey, ex, ey), fill=color, width=4)
        label_base_x = mid_x
        label_base_y = min(sy, ey) + abs(sy - ey) / 2
    else:
        draw.line((sx, sy, sx + 48, sy), fill=color, width=4)
        draw.line((sx + 48, sy, sx + 48, route_y), fill=color, width=4)
        draw.line((sx + 48, route_y, ex - 48, route_y), fill=color, width=4)
        draw.line((ex - 48, route_y, ex - 48, ey), fill=color, width=4)
        draw.line((ex - 48, ey, ex, ey), fill=color, width=4)
        label_base_x = (sx + ex) / 2
        label_base_y = route_y
    one_marker(draw, start, "right" if ex > sx else "left", color)
    if many_at == "end":
        crow_foot(draw, end, "left" if ex > sx else "right", color)
    else:
        crow_foot(draw, start, "right" if ex > sx else "left", color)
    b = draw.textbbox((0, 0), label_text, font=F_NOTE)
    tw, th = b[2] - b[0], b[3] - b[1]
    lx = label_base_x - tw / 2 + label_shift[0]
    ly = label_base_y - 32 + label_shift[1]
    draw.rounded_rectangle((lx - 14, ly - 8, lx + tw + 14, ly + th + 10), radius=12, fill="#ffffff", outline="#cbd5e1", width=2)
    draw.text((lx, ly), label_text, font=F_NOTE, fill=COLORS["ink"])


def draw_legend(draw):
    x, y = 1560, 1350
    rounded(draw, (x, y, x + 610, y + 144), "#ffffff", "#cbd5e1", 2, 16)
    draw.text((x + 24, y + 22), "Leyenda", font=F_LEGEND, fill=COLORS["ink"])
    items = [("PK", COLORS["pk"], "Clave primaria"), ("FK", COLORS["fk"], "Clave foránea"), ("UQ", COLORS["unique"], "Índice único")]
    cy = y + 66
    for marker, color, text in items:
        label(draw, (x + 24, cy - 4), marker, color)
        draw.text((x + 90, cy), text, font=F_NOTE, fill=COLORS["muted"])
        cy += 32
    draw.line((x + 305, y + 86, x + 385, y + 86), fill=COLORS["line"], width=4)
    one_marker(draw, (x + 305, y + 86), "right")
    crow_foot(draw, (x + 385, y + 86), "left")
    draw.text((x + 405, y + 72), "Relación 1 a muchos", font=F_NOTE, fill=COLORS["muted"])


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    img = Image.new("RGB", (W, H), COLORS["bg"])
    draw = ImageDraw.Draw(img)

    for gx in range(0, W, 80):
        draw.line((gx, 0, gx, H), fill=COLORS["grid"], width=1)
    for gy in range(0, H, 80):
        draw.line((0, gy, W, gy), fill=COLORS["grid"], width=1)

    centered(draw, (0, 45, W, 112), "Diagrama Entidad-Relación de SQL Server - Mikuy", F_TITLE)
    centered(draw, (0, 116, W, 155), "Modelo de datos para reservas, mesas, clientes, catálogo gastronómico y acceso administrativo", F_SUBTITLE, COLORS["muted"])

    clientes = draw_table(
        draw,
        90,
        285,
        545,
        "CLIENTES",
        COLORS["clientes"],
        [
            ("PK", "IdCliente", "int"),
            ("", "Nombre", "nvarchar(100)"),
            ("", "Telefono", "nvarchar(20)"),
            ("UQ", "Correo", "nvarchar(150)"),
        ],
        ["IX: Telefono", "UQ: Correo"],
    )

    reservas = draw_table(
        draw,
        900,
        250,
        610,
        "RESERVAS",
        COLORS["reservas"],
        [
            ("PK", "IdReserva", "int"),
            ("UQ", "CodigoReserva", "nvarchar(24)"),
            ("", "Fecha", "date"),
            ("", "Hora", "time(0)"),
            ("", "Estado", "nvarchar(30)"),
            ("FK", "ClienteIdCliente", "int"),
            ("FK", "IdMesa", "int"),
            ("", "CantidadPersonas", "int"),
            ("", "Comentario", "nvarchar(300)"),
        ],
        ["CK: Estado IN Pendiente, Confirmada, Cancelada", "UX: IdMesa + Fecha + Hora si Estado <> Cancelada"],
    )

    mesas = draw_table(
        draw,
        1770,
        285,
        465,
        "MESAS",
        COLORS["mesas"],
        [
            ("PK", "IdMesa", "int"),
            ("", "Numero", "int"),
            ("", "Capacidad", "int"),
            ("", "Ubicacion", "nvarchar(80)"),
            ("", "Activa", "bit"),
        ],
        ["IX: Numero", "Default: Activa = 1"],
    )

    platos = draw_table(
        draw,
        180,
        1040,
        565,
        "PLATOS",
        COLORS["platos"],
        [
            ("PK", "IdPlato", "int"),
            ("", "Nombre", "nvarchar(120)"),
            ("", "Descripcion", "nvarchar(320)"),
            ("", "Categoria", "nvarchar(60)"),
            ("", "Precio", "decimal(8,2)"),
            ("", "ImagenUrl", "nvarchar(220)"),
            ("", "Activo", "bit"),
        ],
        ["Default: Activo = 1"],
    )

    usuarios = draw_table(
        draw,
        950,
        1060,
        455,
        "USUARIOS",
        COLORS["usuarios"],
        [
            ("PK", "IdUsuario", "int"),
            ("UQ", "Usuario", "nvarchar(50)"),
            ("", "Password", "nvarchar(200)"),
            ("", "Rol", "nvarchar(30)"),
        ],
        ["Password almacenado con PBKDF2-SHA256"],
    )

    relation(
        draw,
        (clientes[2], 560),
        (reservas[0], 560),
        "1:N Cliente - Reserva",
        many_at="end",
        label_shift=(0, -18),
    )
    relation(
        draw,
        (mesas[0], 560),
        (reservas[2], 560),
        "1:N Mesa - Reserva",
        many_at="end",
        label_shift=(0, -18),
    )

    # Independent table notes.
    for box, text in [
        (platos, "Tabla independiente: catálogo gastronómico visible en la plataforma."),
        (usuarios, "Tabla independiente: credenciales y rol del acceso administrativo."),
    ]:
        x1, y1, x2, y2 = box
        draw.rounded_rectangle((x1, y2 + 22, x2, y2 + 74), radius=14, fill="#ffffff", outline="#cbd5e1", width=2)
        centered(draw, (x1 + 18, y2 + 28, x2 - 18, y2 + 68), text, F_SMALL, COLORS["muted"])

    draw_legend(draw)

    draw.text(
        (90, 1580),
        "Fuente: elaboración propia a partir de ReservationDbContext y migraciones de Entity Framework Core del proyecto Mikuy.",
        font=F_NOTE,
        fill=COLORS["muted"],
    )

    img.save(OUT, quality=96)
    print(OUT.resolve())


if __name__ == "__main__":
    main()
