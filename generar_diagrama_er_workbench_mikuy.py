from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path("docs/diagrama_er_workbench_mikuy.png")
W, H = 1800, 1300


def font(size, bold=False):
    base = Path(r"C:\Windows\Fonts")
    for name in [
        "arialbd.ttf" if bold else "arial.ttf",
        "segoeuib.ttf" if bold else "segoeui.ttf",
        "calibrib.ttf" if bold else "calibri.ttf",
    ]:
        path = base / name
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


F_TITLE = font(34, True)
F_TABLE = font(19, True)
F_FIELD = font(17)
F_FIELD_BOLD = font(17, True)
F_SMALL = font(14)

BG = "#fbfbf8"
GRID_MINOR = "#e8e6df"
GRID_MAJOR = "#d7d5cd"
HEADER = "#8fb8d0"
HEADER_DARK = "#6f9fba"
BODY = "#eef2f2"
BORDER = "#8b9aa3"
INK = "#1f2933"
MUTED = "#5b6670"
PK = "#f3b327"
FK = "#dc5f55"
COL = "#4bbfc3"
IDX = "#9aa3aa"


def text(draw, xy, value, fnt=F_FIELD, fill=INK):
    draw.text(xy, value, font=fnt, fill=fill)


def centered(draw, box, value, fnt, fill=INK):
    x1, y1, x2, y2 = box
    b = draw.textbbox((0, 0), value, font=fnt)
    draw.text((x1 + (x2 - x1 - (b[2] - b[0])) / 2, y1 + (y2 - y1 - (b[3] - b[1])) / 2), value, font=fnt, fill=fill)


def draw_grid(draw):
    for x in range(0, W, 16):
        draw.line((x, 0, x, H), fill=GRID_MINOR, width=1)
    for y in range(0, H, 16):
        draw.line((0, y, W, y), fill=GRID_MINOR, width=1)
    for x in range(0, W, 80):
        draw.line((x, 0, x, H), fill=GRID_MAJOR, width=1)
    for y in range(0, H, 80):
        draw.line((0, y, W, y), fill=GRID_MAJOR, width=1)


def icon_key(draw, x, y, color):
    draw.ellipse((x, y + 5, x + 10, y + 15), fill=color, outline="#8a6a00")
    draw.rectangle((x + 9, y + 9, x + 24, y + 12), fill=color)
    draw.rectangle((x + 18, y + 12, x + 21, y + 17), fill=color)


def icon_diamond(draw, x, y, color):
    draw.polygon([(x + 8, y + 3), (x + 16, y + 11), (x + 8, y + 19), (x, y + 11)], fill=color, outline="#64748b")


def draw_table(draw, x, y, w, name, rows, indexes=None):
    row_h = 29
    header_h = 38
    idx_h = 30
    h = header_h + row_h * len(rows) + idx_h
    draw.rounded_rectangle((x, y, x + w, y + h), radius=8, fill=BODY, outline=BORDER, width=2)
    draw.rounded_rectangle((x, y, x + w, y + header_h), radius=8, fill=HEADER, outline=BORDER, width=2)
    draw.rectangle((x, y + header_h - 8, x + w, y + header_h), fill=HEADER)
    draw.rectangle((x + w - 28, y, x + w, y + header_h), fill=HEADER_DARK)
    draw.polygon([(x + w - 20, y + 15), (x + w - 10, y + 15), (x + w - 15, y + 24)], fill="#315262")
    draw.rectangle((x + 10, y + 11, x + 23, y + 24), fill="#c7ddff", outline="#4f6fad")
    text(draw, (x + 31, y + 8), name, F_TABLE)

    cy = y + header_h
    for marker, field, typ in rows:
        draw.line((x, cy, x + w, cy), fill="#dde3e6", width=1)
        if marker == "PK":
            icon_key(draw, x + 12, cy + 5, PK)
        elif marker == "FK":
            icon_diamond(draw, x + 14, cy + 5, FK)
        elif marker == "UQ":
            icon_diamond(draw, x + 14, cy + 5, "#8b5cf6")
        else:
            icon_diamond(draw, x + 14, cy + 5, COL)
        text(draw, (x + 43, cy + 5), field, F_FIELD_BOLD if marker in {"PK", "FK"} else F_FIELD)
        b = draw.textbbox((0, 0), typ, font=F_FIELD)
        text(draw, (x + w - 12 - (b[2] - b[0]), cy + 5), typ, F_FIELD, MUTED)
        cy += row_h

    draw.rectangle((x, y + h - idx_h, x + w, y + h), fill="#c7ced3", outline=BORDER, width=1)
    text(draw, (x + 12, y + h - 24), "Indexes", F_SMALL, "#ffffff")
    return {"x": x, "y": y, "w": w, "h": h, "box": (x, y, x + w, y + h)}


def anchor(table, side, dy=0):
    x, y, w, h = table["x"], table["y"], table["w"], table["h"]
    if side == "left":
        return (x, y + h / 2 + dy)
    if side == "right":
        return (x + w, y + h / 2 + dy)
    if side == "top":
        return (x + w / 2, y + dy)
    return (x + w / 2, y + h + dy)


def relation(draw, p1, p2, dashed=False, via=None):
    color = "#3f454a"
    width = 3
    if via:
        pts = [p1] + via + [p2]
    else:
        mid = ((p1[0] + p2[0]) / 2, p1[1])
        mid2 = ((p1[0] + p2[0]) / 2, p2[1])
        pts = [p1, mid, mid2, p2]
    for a, b in zip(pts, pts[1:]):
        if dashed:
            draw_dashed(draw, a, b, color, width)
        else:
            draw.line((a[0], a[1], b[0], b[1]), fill=color, width=width)
    crow(draw, p2, "left" if p2[0] < p1[0] else "right")
    one(draw, p1)


def draw_dashed(draw, a, b, color, width):
    x1, y1 = a
    x2, y2 = b
    length = ((x2 - x1) ** 2 + (y2 - y1) ** 2) ** 0.5
    if length == 0:
        return
    dash = 18
    gap = 10
    steps = int(length / (dash + gap)) + 1
    ux = (x2 - x1) / length
    uy = (y2 - y1) / length
    pos = 0
    for _ in range(steps):
        sx = x1 + ux * pos
        sy = y1 + uy * pos
        ex = x1 + ux * min(pos + dash, length)
        ey = y1 + uy * min(pos + dash, length)
        draw.line((sx, sy, ex, ey), fill=color, width=width)
        pos += dash + gap


def one(draw, p):
    x, y = p
    draw.line((x, y - 16, x, y + 16), fill="#3f454a", width=3)
    draw.line((x + 9, y - 16, x + 9, y + 16), fill="#3f454a", width=3)


def crow(draw, p, direction):
    x, y = p
    if direction == "left":
        draw.line((x, y, x - 26, y - 17), fill="#3f454a", width=3)
        draw.line((x, y, x - 26, y), fill="#3f454a", width=3)
        draw.line((x, y, x - 26, y + 17), fill="#3f454a", width=3)
    else:
        draw.line((x, y, x + 26, y - 17), fill="#3f454a", width=3)
        draw.line((x, y, x + 26, y), fill="#3f454a", width=3)
        draw.line((x, y, x + 26, y + 17), fill="#3f454a", width=3)


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    img = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(img)
    draw_grid(draw)

    centered(draw, (0, 35, W, 88), "Diagrama Entidad-Relación de SQL Server - Proyecto Mikuy", F_TITLE)
    centered(draw, (0, 86, W, 122), "Modelo físico de tablas, claves primarias, claves foráneas e índices principales", font(22), MUTED)

    clientes = draw_table(
        draw,
        115,
        230,
        380,
        "clientes",
        [
            ("PK", "id_cliente", "INT"),
            ("", "nombre", "NVARCHAR(100)"),
            ("", "telefono", "NVARCHAR(20)"),
            ("UQ", "correo", "NVARCHAR(150)"),
        ],
    )

    reservas = draw_table(
        draw,
        690,
        300,
        480,
        "reservas",
        [
            ("PK", "id_reserva", "INT"),
            ("UQ", "codigo_reserva", "NVARCHAR(24)"),
            ("", "fecha", "DATE"),
            ("", "hora", "TIME(0)"),
            ("", "estado", "NVARCHAR(30)"),
            ("FK", "cliente_id_cliente", "INT"),
            ("FK", "id_mesa", "INT"),
            ("", "cantidad_personas", "INT"),
            ("", "comentario", "NVARCHAR(300)"),
        ],
    )

    mesas = draw_table(
        draw,
        1385,
        250,
        360,
        "mesas",
        [
            ("PK", "id_mesa", "INT"),
            ("", "numero", "INT"),
            ("", "capacidad", "INT"),
            ("", "ubicacion", "NVARCHAR(80)"),
            ("", "activa", "BIT"),
        ],
    )

    platos = draw_table(
        draw,
        160,
        820,
        435,
        "platos",
        [
            ("PK", "id_plato", "INT"),
            ("", "nombre", "NVARCHAR(120)"),
            ("", "descripcion", "NVARCHAR(320)"),
            ("", "categoria", "NVARCHAR(60)"),
            ("", "precio", "DECIMAL(8,2)"),
            ("", "imagen_url", "NVARCHAR(220)"),
            ("", "activo", "BIT"),
        ],
    )

    usuarios = draw_table(
        draw,
        760,
        850,
        390,
        "usuarios",
        [
            ("PK", "id_usuario", "INT"),
            ("UQ", "usuario", "NVARCHAR(50)"),
            ("", "password", "NVARCHAR(200)"),
            ("", "rol", "NVARCHAR(30)"),
        ],
    )

    estados = draw_table(
        draw,
        1350,
        875,
        390,
        "estado_reserva",
        [
            ("PK", "estado", "NVARCHAR(30)"),
            ("", "pendiente", "VALOR"),
            ("", "confirmada", "VALOR"),
            ("", "cancelada", "VALOR"),
        ],
    )

    relation(draw, anchor(clientes, "right", -8), anchor(reservas, "left", -80), dashed=False)
    relation(draw, anchor(mesas, "left", -5), anchor(reservas, "right", -24), dashed=False)
    relation(
        draw,
        anchor(estados, "top", 0),
        (reservas["x"] + reservas["w"] - 85, reservas["y"] + 38 + 29 * 5 - 15),
        dashed=True,
        via=[(estados["x"] + estados["w"] / 2, 735), (reservas["x"] + reservas["w"] - 85, 735)],
    )

    # Notes box, similar to a DB diagram annotation.
    note_x, note_y = 1195, 1140
    draw.rounded_rectangle((note_x, note_y, note_x + 550, note_y + 98), radius=8, fill="#fffef7", outline="#b8b2a4", width=2)
    text(draw, (note_x + 18, note_y + 16), "Restricciones principales", F_TABLE)
    text(draw, (note_x + 18, note_y + 48), "UX_Reservas_Mesa_Fecha_Hora: evita doble reserva activa.", F_FIELD, MUTED)
    text(draw, (note_x + 18, note_y + 73), "CK_Reservas_Estado: Pendiente, Confirmada o Cancelada.", F_FIELD, MUTED)

    # Legend.
    lx, ly = 95, 1168
    draw.rounded_rectangle((lx, ly, lx + 760, ly + 76), radius=8, fill="#ffffff", outline="#cbd5d9", width=2)
    text(draw, (lx + 18, ly + 16), "Leyenda:", F_TABLE)
    icon_key(draw, lx + 128, ly + 18, PK)
    text(draw, (lx + 160, ly + 19), "PK", F_FIELD_BOLD)
    icon_diamond(draw, lx + 230, ly + 18, FK)
    text(draw, (lx + 255, ly + 19), "FK", F_FIELD_BOLD)
    icon_diamond(draw, lx + 320, ly + 18, "#8b5cf6")
    text(draw, (lx + 345, ly + 19), "UQ", F_FIELD_BOLD)
    draw.line((lx + 430, ly + 33, lx + 510, ly + 33), fill="#3f454a", width=3)
    text(draw, (lx + 525, ly + 19), "Relación física", F_FIELD)

    text(draw, (95, 1260), "Fuente: elaboración propia a partir del ReservationDbContext y las migraciones EF Core del proyecto Mikuy.", F_SMALL, MUTED)
    img.save(OUT, quality=96)
    print(OUT.resolve())


if __name__ == "__main__":
    main()
