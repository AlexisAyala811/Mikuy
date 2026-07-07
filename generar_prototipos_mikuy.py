from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT_DIR = Path("docs/prototipos")
W, H = 1600, 980


COLORS = {
    "bg": "#f6f3ee",
    "ink": "#1f2933",
    "muted": "#64748b",
    "line": "#d6d0c7",
    "panel": "#ffffff",
    "brand": "#8b3f1f",
    "brand_dark": "#5c2a18",
    "gold": "#d8a047",
    "green": "#15803d",
    "green_soft": "#e8f7ee",
    "blue": "#2563eb",
    "blue_soft": "#eef5ff",
    "red": "#b91c1c",
    "red_soft": "#fff1f1",
    "orange": "#c05621",
    "orange_soft": "#fff7ed",
    "gray_soft": "#f5f5f4",
}


def font(size, bold=False):
    base = Path(r"C:\Windows\Fonts")
    for name in [
        "arialbd.ttf" if bold else "arial.ttf",
        "segoeuib.ttf" if bold else "segoeui.ttf",
        "calibrib.ttf" if bold else "calibri.ttf",
    ]:
        p = base / name
        if p.exists():
            return ImageFont.truetype(str(p), size)
    return ImageFont.load_default()


F_TITLE = font(38, True)
F_H1 = font(34, True)
F_H2 = font(27, True)
F_BODY = font(22)
F_BODY_BOLD = font(22, True)
F_SMALL = font(18)
F_SMALL_BOLD = font(18, True)


def text_size(draw, value, fnt):
    box = draw.textbbox((0, 0), value, font=fnt)
    return box[2] - box[0], box[3] - box[1]


def centered(draw, box, value, fnt, fill=COLORS["ink"]):
    x1, y1, x2, y2 = box
    lines = value.split("\n")
    heights = [text_size(draw, line, fnt)[1] for line in lines]
    total = sum(heights) + 8 * (len(lines) - 1)
    y = y1 + (y2 - y1 - total) / 2
    for line, h in zip(lines, heights):
        w, _ = text_size(draw, line, fnt)
        draw.text((x1 + (x2 - x1 - w) / 2, y), line, font=fnt, fill=fill)
        y += h + 8


def rounded(draw, box, fill=COLORS["panel"], outline=COLORS["line"], width=2, radius=18):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def pill(draw, x, y, value, fill, color=None):
    color = color or COLORS["ink"]
    w, h = text_size(draw, value, F_SMALL_BOLD)
    rounded(draw, (x, y, x + w + 34, y + 38), fill=fill, outline=fill, width=1, radius=19)
    draw.text((x + 17, y + 9), value, font=F_SMALL_BOLD, fill=color)
    return w + 34


def button(draw, box, label, fill=COLORS["brand"], text_fill="#ffffff"):
    rounded(draw, box, fill=fill, outline=fill, width=1, radius=12)
    centered(draw, box, label, F_BODY_BOLD, text_fill)


def input_box(draw, box, label, value="", icon=None):
    x1, y1, x2, y2 = box
    draw.text((x1, y1 - 30), label, font=F_SMALL_BOLD, fill=COLORS["muted"])
    rounded(draw, box, fill="#ffffff", outline="#d8d3cb", width=2, radius=10)
    if icon:
        draw.text((x1 + 18, y1 + 17), icon, font=F_BODY_BOLD, fill=COLORS["brand"])
        tx = x1 + 54
    else:
        tx = x1 + 18
    draw.text((tx, y1 + 17), value, font=F_BODY, fill=COLORS["ink"])


def base(title):
    img = Image.new("RGB", (W, H), COLORS["bg"])
    draw = ImageDraw.Draw(img)
    # soft grid
    for x in range(0, W, 64):
        draw.line((x, 0, x, H), fill="#eee8df", width=1)
    for y in range(0, H, 64):
        draw.line((0, y, W, y), fill="#eee8df", width=1)
    draw.rectangle((0, 0, W, 92), fill=COLORS["brand_dark"])
    draw.text((46, 25), "Mikuy", font=F_TITLE, fill="#fff7ed")
    draw.text((180, 37), "Sistema web de reservas y gestión de mesas", font=F_BODY, fill="#efd6c6")
    centered(draw, (0, 112, W, 168), title, F_H1, COLORS["ink"])
    return img, draw


def browser_frame(draw, box):
    x1, y1, x2, y2 = box
    rounded(draw, box, fill="#ffffff", outline="#d1c7bb", width=2, radius=22)
    draw.rectangle((x1, y1, x2, y1 + 54), fill="#f1ede7")
    draw.rounded_rectangle((x1, y1, x2, y1 + 54), radius=22, outline="#d1c7bb", width=2)
    for i, c in enumerate(["#ef4444", "#f59e0b", "#22c55e"]):
        draw.ellipse((x1 + 24 + i * 27, y1 + 18, x1 + 39 + i * 27, y1 + 33), fill=c)
    rounded(draw, (x1 + 130, y1 + 14, x2 - 38, y1 + 39), fill="#ffffff", outline="#ddd6cc", width=1, radius=12)
    draw.text((x1 + 152, y1 + 18), "https://mikuy.pe/reservas", font=F_SMALL, fill=COLORS["muted"])


def mock_reserva_paso1():
    img, draw = base("Prototipo 1: Formulario de reserva - Paso 1")
    browser_frame(draw, (110, 190, 1490, 900))
    rounded(draw, (160, 280, 650, 830), fill="#fffaf4", outline="#ead8c4", radius=20)
    draw.text((200, 325), "Reserva tu mesa", font=F_H1, fill=COLORS["brand_dark"])
    draw.text((200, 375), "Selecciona fecha, hora y cantidad de personas.", font=F_BODY, fill=COLORS["muted"])
    input_box(draw, (200, 470, 570, 535), "Fecha", "26/07/2026")
    input_box(draw, (200, 595, 570, 660), "Hora", "08:00 p. m.")
    input_box(draw, (200, 720, 570, 785), "Personas", "4 comensales")
    rounded(draw, (720, 280, 1435, 830), fill="#ffffff", outline="#ead8c4", radius=20)
    draw.text((760, 325), "Disponibilidad sugerida", font=F_H2, fill=COLORS["ink"])
    for i, (hour, state, fill, color) in enumerate([
        ("07:00 p. m.", "Disponible", COLORS["green_soft"], COLORS["green"]),
        ("08:00 p. m.", "Mesa recomendada", COLORS["orange_soft"], COLORS["orange"]),
        ("09:00 p. m.", "Disponible", COLORS["green_soft"], COLORS["green"]),
    ]):
        y = 405 + i * 105
        rounded(draw, (765, y, 1388, y + 76), fill=fill, outline=fill, radius=14)
        draw.text((800, y + 23), hour, font=F_BODY_BOLD, fill=COLORS["ink"])
        draw.text((1055, y + 23), state, font=F_BODY_BOLD, fill=color)
    rounded(draw, (765, 720, 1130, 785), fill="#fff7ed", outline="#f3d5b5", radius=14)
    draw.text((792, 740), "Mesa asignada: M-04 / Capacidad 4", font=F_SMALL_BOLD, fill=COLORS["brand_dark"])
    button(draw, (1160, 720, 1390, 785), "Continuar")
    return img


def mock_reserva_paso2():
    img, draw = base("Prototipo 2: Datos del cliente y confirmación")
    browser_frame(draw, (110, 190, 1490, 900))
    rounded(draw, (160, 275, 820, 835), fill="#ffffff", outline="#ead8c4", radius=20)
    draw.text((205, 320), "Datos de contacto", font=F_H1, fill=COLORS["brand_dark"])
    input_box(draw, (205, 430, 760, 492), "Nombre completo", "Ana Quispe Huamán")
    input_box(draw, (205, 550, 760, 612), "Teléfono", "987654321")
    input_box(draw, (205, 670, 760, 732), "Correo electrónico", "ana.quispe@email.com")
    button(draw, (500, 760, 760, 815), "Confirmar reserva")
    rounded(draw, (880, 275, 1435, 835), fill="#fffaf4", outline="#ead8c4", radius=20)
    draw.text((925, 320), "Resumen de reserva", font=F_H2, fill=COLORS["ink"])
    rows = [
        ("Código", "MKY-20260726-081"),
        ("Fecha", "26/07/2026"),
        ("Hora", "08:00 p. m."),
        ("Mesa", "M-04"),
        ("Personas", "4"),
        ("Estado", "Pendiente"),
    ]
    y = 390
    for k, v in rows:
        draw.text((925, y), k, font=F_BODY_BOLD, fill=COLORS["muted"])
        draw.text((1130, y), v, font=F_BODY, fill=COLORS["ink"])
        y += 58
    pill(draw, 925, 745, "Enviar confirmación por WhatsApp", COLORS["green_soft"], COLORS["green"])
    return img


def mock_dashboard():
    img, draw = base("Prototipo 3: Dashboard administrativo")
    browser_frame(draw, (80, 180, 1520, 915))
    rounded(draw, (110, 245, 330, 880), fill=COLORS["brand_dark"], outline=COLORS["brand_dark"], radius=18)
    draw.text((145, 285), "Mikuy", font=F_H2, fill="#fff7ed")
    for i, item in enumerate(["Dashboard", "Reservas", "Mesas", "Platos", "Clientes"]):
        y = 355 + i * 72
        fill = COLORS["gold"] if i == 0 else COLORS["brand_dark"]
        rounded(draw, (132, y, 308, y + 46), fill=fill, outline=fill, radius=10)
        draw.text((155, y + 12), item, font=F_SMALL_BOLD, fill="#ffffff")
    draw.text((370, 250), "Panel operativo", font=F_H1, fill=COLORS["ink"])
    cards = [
        ("Pendientes", "8", COLORS["orange_soft"], COLORS["orange"]),
        ("Confirmadas", "21", COLORS["green_soft"], COLORS["green"]),
        ("Ocupación", "68%", COLORS["blue_soft"], COLORS["blue"]),
        ("Mesas activas", "12", "#f1f5f9", COLORS["ink"]),
    ]
    for i, (label, value, fill, color) in enumerate(cards):
        x = 370 + i * 270
        rounded(draw, (x, 320, x + 240, 430), fill=fill, outline=fill, radius=18)
        draw.text((x + 24, 345), label, font=F_SMALL_BOLD, fill=COLORS["muted"])
        draw.text((x + 24, 375), value, font=F_H1, fill=color)
    rounded(draw, (370, 470, 875, 840), fill="#ffffff", outline="#ddd6cc", radius=18)
    draw.text((410, 505), "Reservas pendientes", font=F_H2, fill=COLORS["ink"])
    for i, (name, hour, people) in enumerate([("Ana Quispe", "08:00 p. m.", "4"), ("Carlos Rojas", "08:30 p. m.", "2"), ("María Flores", "09:00 p. m.", "6")]):
        y = 570 + i * 78
        draw.line((410, y - 18, 835, y - 18), fill="#ece7df", width=2)
        draw.text((410, y), name, font=F_BODY_BOLD, fill=COLORS["ink"])
        draw.text((650, y), hour, font=F_BODY, fill=COLORS["muted"])
        pill(draw, 765, y - 5, f"{people} pers.", COLORS["orange_soft"], COLORS["orange"])
    rounded(draw, (920, 470, 1465, 840), fill="#ffffff", outline="#ddd6cc", radius=18)
    draw.text((960, 505), "Agenda del día", font=F_H2, fill=COLORS["ink"])
    for i, hour in enumerate(["07:00", "08:00", "09:00", "10:00"]):
        y = 570 + i * 56
        draw.text((960, y), hour, font=F_BODY_BOLD, fill=COLORS["muted"])
        rounded(draw, (1050, y - 8, 1420, y + 34), fill=COLORS["gray_soft"], outline="#e7e1d8", radius=10)
        draw.text((1072, y), "Mesa asignada / cliente", font=F_SMALL, fill=COLORS["ink"])
    return img


def mock_gestion_mesas():
    img, draw = base("Prototipo 4: Gestión de mesas y reservas")
    browser_frame(draw, (80, 180, 1520, 915))
    draw.text((135, 245), "Gestión de mesas", font=F_H1, fill=COLORS["ink"])
    button(draw, (1240, 240, 1455, 295), "Nueva mesa", COLORS["brand"])
    rounded(draw, (130, 335, 700, 850), fill="#ffffff", outline="#ddd6cc", radius=18)
    draw.text((170, 375), "Mesas disponibles", font=F_H2, fill=COLORS["ink"])
    for i in range(3):
        for j in range(3):
            x = 175 + j * 160
            y = 455 + i * 115
            active = not (i == 2 and j == 1)
            fill = COLORS["green_soft"] if active else COLORS["red_soft"]
            color = COLORS["green"] if active else COLORS["red"]
            rounded(draw, (x, y, x + 118, y + 80), fill=fill, outline=fill, radius=16)
            centered(draw, (x, y + 10, x + 118, y + 40), f"M-{i*3+j+1:02d}", F_BODY_BOLD, color)
            centered(draw, (x, y + 40, x + 118, y + 70), "4 pers." if j < 2 else "6 pers.", F_SMALL, COLORS["muted"])
    rounded(draw, (760, 335, 1455, 850), fill="#ffffff", outline="#ddd6cc", radius=18)
    draw.text((800, 375), "Reservas por mesa", font=F_H2, fill=COLORS["ink"])
    headers = ["Código", "Cliente", "Mesa", "Estado"]
    xs = [800, 980, 1190, 1310]
    for x, h in zip(xs, headers):
        draw.text((x, 440), h, font=F_SMALL_BOLD, fill=COLORS["muted"])
    data = [
        ("MKY-081", "Ana Quispe", "M-04", "Pendiente", COLORS["orange_soft"], COLORS["orange"]),
        ("MKY-082", "Carlos Rojas", "M-02", "Confirmada", COLORS["green_soft"], COLORS["green"]),
        ("MKY-083", "María Flores", "M-08", "Cancelada", COLORS["red_soft"], COLORS["red"]),
        ("MKY-084", "Luis Prado", "M-06", "Confirmada", COLORS["green_soft"], COLORS["green"]),
    ]
    y = 490
    for code, client, mesa, state, fill, color in data:
        draw.line((800, y - 18, 1415, y - 18), fill="#ece7df", width=2)
        draw.text((800, y), code, font=F_BODY, fill=COLORS["ink"])
        draw.text((980, y), client, font=F_BODY, fill=COLORS["ink"])
        draw.text((1190, y), mesa, font=F_BODY_BOLD, fill=COLORS["ink"])
        pill(draw, 1310, y - 5, state, fill, color)
        y += 78
    return img


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    prototypes = [
        ("prototipo_1_reserva_paso_1.png", mock_reserva_paso1()),
        ("prototipo_2_datos_confirmacion.png", mock_reserva_paso2()),
        ("prototipo_3_dashboard_administrativo.png", mock_dashboard()),
        ("prototipo_4_gestion_mesas_reservas.png", mock_gestion_mesas()),
    ]
    for name, img in prototypes:
        path = OUT_DIR / name
        img.save(path, quality=96)
        print(path.resolve())


if __name__ == "__main__":
    main()
