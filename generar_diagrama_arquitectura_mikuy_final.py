from pathlib import Path
from textwrap import wrap

from PIL import Image, ImageDraw, ImageFont


OUT = Path("docs/diagrama_arquitectura_mikuy_final.png")

W, H = 2400, 1350


COLORS = {
    "bg": "#f4f7fb",
    "ink": "#172033",
    "muted": "#536171",
    "line": "#536171",
    "panel": "#ffffff",
    "panel_alt": "#f9fbfd",
    "header": "#e8eef7",
    "blue": "#2f6fd6",
    "blue_soft": "#eaf2ff",
    "green": "#179b69",
    "green_soft": "#e9f8f1",
    "orange": "#d97706",
    "orange_soft": "#fff4df",
    "purple": "#6d5bd0",
    "purple_soft": "#f1efff",
    "teal": "#0f766e",
    "teal_soft": "#e8f7f5",
    "red": "#b45309",
}


def font(size, bold=False):
    base = Path(r"C:\Windows\Fonts")
    names = [
        "arialbd.ttf" if bold else "arial.ttf",
        "segoeuib.ttf" if bold else "segoeui.ttf",
        "calibrib.ttf" if bold else "calibri.ttf",
    ]
    for name in names:
        candidate = base / name
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


F_TITLE = font(50, True)
F_CAPTION = font(28)
F_PANEL_TITLE = font(27, True)
F_SECTION = font(25, True)
F_BODY = font(24)
F_SMALL = font(21)
F_TINY = font(18)
F_ARROW = font(21, True)


def text_size(draw, text, fnt):
    box = draw.textbbox((0, 0), text, font=fnt)
    return box[2] - box[0], box[3] - box[1]


def centered(draw, box, text, fnt, fill=COLORS["ink"], gap=6):
    x1, y1, x2, y2 = box
    lines = text.split("\n")
    heights = [text_size(draw, line, fnt)[1] for line in lines]
    total = sum(heights) + gap * (len(lines) - 1)
    y = y1 + (y2 - y1 - total) / 2
    for line, height in zip(lines, heights):
        width, _ = text_size(draw, line, fnt)
        draw.text((x1 + (x2 - x1 - width) / 2, y), line, font=fnt, fill=fill)
        y += height + gap


def draw_wrapped(draw, xy, text, fnt=F_BODY, fill=COLORS["ink"], line_gap=8, bullet=False):
    x, y, width, _ = xy
    current_y = y
    char_width = max(8, text_size(draw, "n", fnt)[0])
    max_chars = max(16, int(width / (char_width * 0.92)))
    for raw in text.split("\n"):
        if raw.strip() == "":
            current_y += int(fnt.size * 0.62)
            continue
        prefix = "• " if bullet else ""
        indent = 22 if bullet else 0
        lines = wrap(raw, max_chars)
        for idx, line in enumerate(lines):
            draw.text((x + (indent if idx else 0), current_y), (prefix if idx == 0 else "") + line, font=fnt, fill=fill)
            current_y += fnt.size + line_gap
    return current_y


def rounded(draw, box, fill, outline=COLORS["line"], width=3, radius=18):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def pill(draw, center, text, fill, outline, fnt=F_TINY):
    cx, cy = center
    tw, th = text_size(draw, text, fnt)
    pad_x, pad_y = 18, 8
    box = (cx - tw / 2 - pad_x, cy - th / 2 - pad_y, cx + tw / 2 + pad_x, cy + th / 2 + pad_y)
    draw.rounded_rectangle(box, radius=18, fill=fill, outline=outline, width=2)
    draw.text((cx - tw / 2, cy - th / 2 - 1), text, font=fnt, fill=COLORS["ink"])


def panel(draw, box, title, fill, accent, subtitle=None):
    rounded(draw, box, fill=fill, outline=COLORS["line"], width=3, radius=18)
    x1, y1, x2, _ = box
    draw.rounded_rectangle((x1, y1, x2, y1 + 88), radius=18, fill=COLORS["header"], outline=COLORS["line"], width=3)
    draw.rectangle((x1 + 3, y1 + 44, x2 - 3, y1 + 88), fill=COLORS["header"])
    draw.rectangle((x1, y1 + 84, x2, y1 + 88), fill=accent)
    centered(draw, (x1 + 18, y1 + 10, x2 - 18, y1 + 70), title, F_PANEL_TITLE)
    if subtitle:
        centered(draw, (x1 + 20, y1 + 101, x2 - 20, y1 + 136), subtitle, F_SMALL, COLORS["muted"])


def section(draw, x, y, title, body, accent, width=320):
    draw.rounded_rectangle((x, y, x + width, y + 44), radius=11, fill=accent, outline=accent, width=1)
    draw.text((x + 16, y + 9), title, font=F_SECTION, fill="#ffffff")
    return draw_wrapped(draw, (x, y + 58, width, 260), body, F_BODY, COLORS["ink"], bullet=True)


def arrow(draw, start, end, label=None, label_pos=0.5, color="#334155", two_way=False):
    sx, sy = start
    ex, ey = end
    draw.line((sx, sy, ex, ey), fill=color, width=5)
    def head_at(x, y, direction):
        head = 22
        if direction == "right":
            pts = [(x, y), (x - head, y - 13), (x - head, y + 13)]
        else:
            pts = [(x, y), (x + head, y - 13), (x + head, y + 13)]
        draw.polygon(pts, fill=color)

    head_at(ex, ey, "right" if ex >= sx else "left")
    if two_way:
        head_at(sx, sy, "left" if ex >= sx else "right")

    if label:
        lx = sx + (ex - sx) * label_pos
        ly = sy + (ey - sy) * label_pos
        pill(draw, (lx, ly - 35), label, "#ffffff", "#cbd5e1", F_ARROW)


def actor_card(draw, box, title, body, icon_kind):
    rounded(draw, box, fill=COLORS["panel"], outline=COLORS["line"], width=3, radius=18)
    x1, y1, x2, y2 = box
    centered(draw, (x1 + 16, y1 + 16, x2 - 16, y1 + 62), title, F_SECTION)
    icon_cx = x1 + 86
    if icon_kind == "users":
        cx, cy = icon_cx, y1 + 148
        draw.ellipse((cx - 25, cy - 64, cx + 25, cy - 14), fill="#ffd2aa", outline="#334155", width=3)
        draw.rounded_rectangle((cx - 52, cy - 14, cx + 52, cy + 58), radius=28, fill="#5aa2d9", outline="#334155", width=3)
        draw.rectangle((cx - 82, cy - 7, cx - 35, cy + 38), fill="#dbeafe", outline="#334155", width=3)
        draw.rectangle((cx + 35, cy - 7, cx + 82, cy + 38), fill="#dbeafe", outline="#334155", width=3)
    elif icon_kind == "browser":
        bx, by = x1 + 28, y1 + 105
        draw.rounded_rectangle((bx, by, bx + 140, by + 94), radius=14, fill="#ffffff", outline="#334155", width=3)
        draw.rectangle((bx, by, bx + 140, by + 28), fill="#dbeafe", outline="#334155", width=3)
        for i, c in enumerate(["#ef4444", "#f59e0b", "#22c55e"]):
            draw.ellipse((bx + 13 + i * 22, by + 9, bx + 25 + i * 22, by + 21), fill=c)
        draw.rectangle((bx + 24, by + 48, bx + 116, by + 78), fill="#edf6ff", outline="#60a5fa", width=3)
    elif icon_kind == "whatsapp":
        cx, cy = icon_cx, y1 + 148
        draw.ellipse((cx - 45, cy - 45, cx + 45, cy + 45), fill="#22c55e", outline="#15803d", width=4)
        draw.polygon([(cx - 23, cy + 34), (cx - 51, cy + 65), (cx - 10, cy + 45)], fill="#22c55e")
        draw.arc((cx - 23, cy - 23, cx + 24, cy + 26), 115, 310, fill="#ffffff", width=7)
    draw_wrapped(draw, (x1 + 178, y1 + 92, x2 - x1 - 198, y2 - y1 - 105), body, F_TINY, COLORS["muted"], bullet=False)


def draw_database(draw, box):
    x1, y1, x2, y2 = box
    panel(draw, box, "CAPA DE DATOS\nBASE DE DATOS", COLORS["panel"], COLORS["teal"])
    cx = (x1 + x2) / 2
    top = y1 + 190
    w = 210
    h = 250
    draw.ellipse((cx - w / 2, top, cx + w / 2, top + 58), fill="#f8fafc", outline=COLORS["teal"], width=6)
    draw.rectangle((cx - w / 2, top + 29, cx + w / 2, top + h), fill="#f8fafc", outline=COLORS["teal"], width=6)
    draw.ellipse((cx - w / 2, top + h - 31, cx + w / 2, top + h + 29), fill="#f8fafc", outline=COLORS["teal"], width=6)
    centered(draw, (cx - 92, top + 88, cx + 92, top + 168), "SQL\nServer", font(38, True), COLORS["teal"], gap=3)
    section(
        draw,
        x1 + 42,
        y1 + 520,
        "Tablas",
        "Clientes\nReservas\nMesas\nPlatos\nUsuarios",
        COLORS["teal"],
        width=x2 - x1 - 84,
    )
    section(
        draw,
        x1 + 42,
        y1 + 805,
        "Estados",
        "Pendiente\nConfirmada\nCancelada",
        COLORS["teal"],
        width=x2 - x1 - 84,
    )


def tech_icon(draw, kind, box):
    x1, y1, x2, y2 = box
    cx = (x1 + x2) / 2
    cy = (y1 + y2) / 2
    if kind == "dotnet":
        draw.ellipse((cx - 26, cy - 26, cx + 26, cy + 26), fill="#6d5bd0", outline="#4c3bb2", width=3)
        centered(draw, (cx - 26, cy - 19, cx + 26, cy + 20), ".NET", font(16, True), "#ffffff", gap=0)
    elif kind == "mvc":
        draw.rounded_rectangle((x1 + 9, y1 + 12, x2 - 9, y2 - 12), radius=10, fill="#eaf2ff", outline="#2f6fd6", width=3)
        draw.rectangle((x1 + 9, y1 + 12, x2 - 9, y1 + 30), fill="#2f6fd6")
        centered(draw, (x1 + 12, y1 + 34, x2 - 12, y2 - 10), "MVC", font(20, True), "#2f6fd6")
    elif kind == "bootstrap":
        draw.rounded_rectangle((cx - 29, cy - 29, cx + 29, cy + 29), radius=14, fill="#7952b3", outline="#563d7c", width=3)
        centered(draw, (cx - 24, cy - 26, cx + 24, cy + 25), "B", font(35, True), "#ffffff")
    elif kind == "ef":
        draw.ellipse((cx - 31, cy - 25, cx + 31, cy - 5), fill="#f1efff", outline="#6d5bd0", width=3)
        draw.rectangle((cx - 31, cy - 15, cx + 31, cy + 24), fill="#f1efff", outline="#6d5bd0", width=3)
        draw.ellipse((cx - 31, cy + 14, cx + 31, cy + 34), fill="#f1efff", outline="#6d5bd0", width=3)
        centered(draw, (cx - 32, cy - 12, cx + 32, cy + 23), "EF", font(22, True), "#6d5bd0")
    elif kind == "sql":
        draw.ellipse((cx - 32, cy - 26, cx + 32, cy - 5), fill="#e8f7f5", outline="#0f766e", width=3)
        draw.rectangle((cx - 32, cy - 16, cx + 32, cy + 25), fill="#e8f7f5", outline="#0f766e", width=3)
        draw.ellipse((cx - 32, cy + 15, cx + 32, cy + 36), fill="#e8f7f5", outline="#0f766e", width=3)
        centered(draw, (cx - 31, cy - 10, cx + 31, cy + 22), "SQL", font(19, True), "#0f766e")
    elif kind == "pbkdf2":
        draw.rounded_rectangle((cx - 30, cy - 8, cx + 30, cy + 32), radius=9, fill="#fff4df", outline="#d97706", width=3)
        draw.arc((cx - 20, cy - 35, cx + 20, cy + 8), 180, 360, fill="#d97706", width=5)
        centered(draw, (cx - 31, cy - 4, cx + 31, cy + 30), "#", font(26, True), "#d97706")
    elif kind == "whatsapp":
        draw.ellipse((cx - 29, cy - 29, cx + 29, cy + 29), fill="#22c55e", outline="#15803d", width=3)
        draw.polygon([(cx - 15, cy + 22), (cx - 34, cy + 43), (cx - 8, cy + 28)], fill="#22c55e")
        draw.arc((cx - 15, cy - 14, cx + 16, cy + 17), 115, 310, fill="#ffffff", width=5)


def draw_tech_card(draw, x, y, kind, title, subtitle, accent):
    card = (x, y, x + 238, y + 124)
    draw.rounded_rectangle(card, radius=18, fill="#ffffff", outline="#cbd5e1", width=2)
    draw.rounded_rectangle((x, y, x + 238, y + 11), radius=18, fill=accent, outline=accent, width=1)
    tech_icon(draw, kind, (x + 16, y + 24, x + 86, y + 96))
    draw.text((x + 96, y + 33), title, font=F_SMALL, fill=COLORS["ink"])
    draw_wrapped(draw, (x + 96, y + 62, 126, 48), subtitle, F_TINY, COLORS["muted"], line_gap=3)


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    img = Image.new("RGB", (W, H), COLORS["bg"])
    draw = ImageDraw.Draw(img)

    # Subtle background grid.
    for x in range(0, W, 80):
        draw.line((x, 0, x, H), fill="#edf2f7", width=1)
    for y in range(0, H, 80):
        draw.line((0, y, W, y), fill="#edf2f7", width=1)

    centered(draw, (0, 44, W, 112), "Diagrama de Arquitectura del Sistema Mikuy", F_TITLE)
    centered(
        draw,
        (0, 112, W, 152),
        "Plataforma web para gestión de reservas, mesas, catálogo gastronómico, panel administrativo y comunicación por WhatsApp",
        F_CAPTION,
        COLORS["muted"],
    )

    # Actors
    actor_card(
        draw,
        (60, 205, 365, 460),
        "CLIENTE",
        "Reserva, consulta su estado y obtiene comprobante.",
        "users",
    )
    actor_card(
        draw,
        (60, 535, 365, 790),
        "ADMINISTRADOR",
        "Administra reservas, mesas, platos y dashboard.",
        "browser",
    )
    actor_card(
        draw,
        (60, 865, 365, 1118),
        "INTEGRACIÓN\nEXTERNA",
        "Contacto mediante enlace wa.me con mensaje preparado.",
        "whatsapp",
    )

    # Main panels
    p1 = (455, 180, 825, 1135)
    p2 = (950, 180, 1320, 1135)
    p3 = (1445, 180, 1815, 1135)
    p4 = (1940, 180, 2280, 1135)

    panel(draw, p1, "CAPA DE PRESENTACIÓN\nASP.NET CORE MVC", COLORS["panel"], COLORS["blue"], "Reserva.Web")
    section(draw, 495, 350, "Interfaz", "Vistas Razor\nBootstrap y CSS\nFormularios MVC", COLORS["blue"], 290)
    section(draw, 495, 600, "Flujo público", "Wizard de reserva\nConsulta de disponibilidad\nConsulta de comprobante", COLORS["blue"], 290)
    section(draw, 495, 875, "Panel interno", "Dashboard\nCRUDs administrativos\nAcciones rápidas", COLORS["blue"], 290)

    panel(draw, p2, "CAPA DE APLICACIÓN\nLÓGICA WEB", COLORS["panel"], COLORS["orange"], "Controladores y servicios")
    section(draw, 990, 350, "Controladores", "ReservasController\nAdminController\nCuentaController\nMesasController\nPlatosController", COLORS["orange"], 290)
    section(draw, 990, 660, "Servicios", "Comprobante PDF\nNotificación WhatsApp\nAutenticación por cookies", COLORS["orange"], 290)
    section(draw, 990, 910, "Reglas", "Disponibilidad por horario\nBest-Fit de mesas\nValidación de estados", COLORS["orange"], 290)

    panel(draw, p3, "CAPA DE DOMINIO E\nINFRAESTRUCTURA", COLORS["panel"], COLORS["purple"], "Dominio e infraestructura")
    section(draw, 1485, 350, "Dominio", "Cliente\nReserva\nMesa\nPlato\nUsuario", COLORS["purple"], 290)
    section(draw, 1485, 635, "Persistencia", "Entity Framework Core\nReservationDbContext\nMigraciones\nDataSeeder", COLORS["purple"], 290)
    section(draw, 1485, 905, "Seguridad", "Hashing PBKDF2-SHA256\nSal e iteraciones\nComparación segura", COLORS["purple"], 290)

    draw_database(draw, p4)

    # Flow arrows
    arrow(draw, (365, 315), (455, 315))
    arrow(draw, (365, 650), (455, 650))
    arrow(draw, (365, 990), (455, 990))

    arrow(draw, (825, 420), (950, 420), "Peticiones", 0.5)
    arrow(draw, (950, 565), (825, 565), "Vistas / JSON", 0.5)

    arrow(draw, (1320, 420), (1445, 420), "Entidades", 0.5)
    arrow(draw, (1445, 565), (1320, 565), "Resultados", 0.5)

    arrow(draw, (1815, 420), (1940, 420), "Consultas SQL", 0.5)
    arrow(draw, (1940, 565), (1815, 565), "Datos SQL", 0.5)

    # Bottom technical strip.
    strip = (320, 1180, 2280, 1316)
    draw.rounded_rectangle(strip, radius=18, fill="#ffffff", outline="#cbd5e1", width=2)
    draw.text((345, 1218), "Tecnologías:", font=F_SECTION, fill=COLORS["ink"])
    draw_tech_card(draw, 520, 1188, "dotnet", "ASP.NET", "Core MVC", COLORS["blue"])
    draw_tech_card(draw, 770, 1188, "mvc", "Razor", "Vistas MVC", COLORS["blue"])
    draw_tech_card(draw, 1020, 1188, "bootstrap", "Bootstrap", "CSS y UI", "#7952b3")
    draw_tech_card(draw, 1270, 1188, "ef", "EF Core", "ORM", COLORS["purple"])
    draw_tech_card(draw, 1520, 1188, "sql", "SQL Server", "Base de datos", COLORS["teal"])
    draw_tech_card(draw, 1770, 1188, "pbkdf2", "PBKDF2", "Hash seguro", COLORS["orange"])
    draw_tech_card(draw, 2020, 1188, "whatsapp", "WhatsApp", "wa.me", COLORS["green"])

    draw.text(
        (72, 1300),
        "Fuente: elaboración propia con base en la estructura del proyecto Mikuy.",
        font=F_TINY,
        fill=COLORS["muted"],
    )

    img.save(OUT, quality=96)
    print(OUT.resolve())


if __name__ == "__main__":
    main()
