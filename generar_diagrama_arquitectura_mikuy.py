from pathlib import Path
from textwrap import wrap

from PIL import Image, ImageDraw, ImageFont


OUT = Path("docs/diagrama_arquitectura_mikuy.png")


def font(size, bold=False):
    base = Path(r"C:\Windows\Fonts")
    candidates = [
        base / ("arialbd.ttf" if bold else "arial.ttf"),
        base / ("calibrib.ttf" if bold else "calibri.ttf"),
        base / ("segoeuib.ttf" if bold else "segoeui.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


F_TITLE = font(31, True)
F_BOX_TITLE = font(20, True)
F_SUBTITLE = font(17, True)
F_BODY = font(17)
F_SMALL = font(14)
F_ARROW = font(15, True)


def rounded(draw, xy, fill, outline="#6b7280", width=2, radius=10):
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline, width=width)


def centered_text(draw, box, text, fnt, fill="#111827", spacing=4):
    x1, y1, x2, y2 = box
    lines = text.split("\n")
    total = sum(draw.textbbox((0, 0), line, font=fnt)[3] for line in lines) + spacing * (len(lines) - 1)
    y = y1 + ((y2 - y1) - total) / 2
    for line in lines:
        bbox = draw.textbbox((0, 0), line, font=fnt)
        x = x1 + ((x2 - x1) - (bbox[2] - bbox[0])) / 2
        draw.text((x, y), line, font=fnt, fill=fill)
        y += bbox[3] + spacing


def wrap_text(draw, text, x, y, width, fnt, fill="#111827", line_gap=4):
    current_y = y
    for paragraph in text.split("\n"):
        max_chars = max(12, int(width / 8.4))
        for line in wrap(paragraph, max_chars) or [""]:
            draw.text((x, current_y), line, font=fnt, fill=fill)
            current_y += fnt.size + line_gap
    return current_y


def arrow(draw, start, end, label_top=None, label_bottom=None, color="#374151"):
    sx, sy = start
    ex, ey = end
    draw.line((sx, sy, ex, ey), fill=color, width=4)
    head = 15
    if ex >= sx:
        pts = [(ex, ey), (ex - head, ey - head * 0.65), (ex - head, ey + head * 0.65)]
    else:
        pts = [(ex, ey), (ex + head, ey - head * 0.65), (ex + head, ey + head * 0.65)]
    draw.polygon(pts, fill=color)
    mid_x = (sx + ex) / 2
    if label_top:
        centered_text(draw, (mid_x - 88, sy - 46, mid_x + 88, sy - 7), label_top, F_ARROW, "#1f2937", 1)
    if label_bottom:
        centered_text(draw, (mid_x - 88, sy + 8, mid_x + 88, sy + 47), label_bottom, F_ARROW, "#1f2937", 1)


def draw_user_icon(draw, cx, cy):
    draw.ellipse((cx - 18, cy - 43, cx + 18, cy - 7), fill="#f5c39b", outline="#374151", width=2)
    draw.rounded_rectangle((cx - 34, cy - 7, cx + 34, cy + 43), radius=18, fill="#4f9ed8", outline="#374151", width=2)
    draw.rectangle((cx - 76, cy - 2, cx - 42, cy + 31), fill="#dbeafe", outline="#374151", width=2)
    draw.rectangle((cx + 42, cy - 2, cx + 76, cy + 31), fill="#dbeafe", outline="#374151", width=2)
    draw.line((cx - 58, cy + 33, cx - 35, cy + 33), fill="#374151", width=2)
    draw.line((cx + 35, cy + 33, cx + 58, cy + 33), fill="#374151", width=2)


def draw_browser_icon(draw, x, y):
    draw.rounded_rectangle((x, y, x + 120, y + 78), radius=8, fill="#ffffff", outline="#374151", width=2)
    draw.rectangle((x, y, x + 120, y + 20), fill="#dbeafe", outline="#374151", width=2)
    for i, color in enumerate(["#ef4444", "#f59e0b", "#22c55e"]):
        draw.ellipse((x + 9 + i * 17, y + 7, x + 18 + i * 17, y + 16), fill=color)
    draw.rectangle((x + 16, y + 34, x + 104, y + 61), fill="#e0f2fe", outline="#60a5fa", width=2)


def draw_whatsapp_icon(draw, x, y):
    draw.rounded_rectangle((x, y, x + 130, y + 78), radius=10, fill="#ecfdf5", outline="#374151", width=2)
    draw.ellipse((x + 22, y + 15, x + 69, y + 62), fill="#22c55e", outline="#15803d", width=2)
    draw.polygon([(x + 33, y + 57), (x + 24, y + 70), (x + 45, y + 61)], fill="#22c55e")
    draw.arc((x + 35, y + 28, x + 56, y + 52), 120, 300, fill="#ffffff", width=4)
    draw.text((x + 77, y + 20), "API\nWhatsApp", font=F_SMALL, fill="#14532d")


def draw_db(draw, x, y):
    draw.ellipse((x, y, x + 150, y + 42), fill="#f8fafc", outline="#0f766e", width=4)
    draw.rectangle((x, y + 21, x + 150, y + 142), fill="#f8fafc", outline="#0f766e", width=4)
    draw.ellipse((x, y + 120, x + 150, y + 162), fill="#f8fafc", outline="#0f766e", width=4)
    draw.arc((x, y + 66, x + 150, y + 108), 0, 180, fill="#7dd3fc", width=3)
    centered_text(draw, (x + 13, y + 54, x + 137, y + 112), "SQL\nServer", F_BOX_TITLE, "#0f766e", 2)


def box_with_text(draw, xy, title, body, fill):
    rounded(draw, xy, fill=fill)
    x1, y1, x2, y2 = xy
    draw.rectangle((x1, y1, x2, y1 + 54), fill="#ffffff", outline="#6b7280", width=2)
    centered_text(draw, (x1 + 10, y1 + 5, x2 - 10, y1 + 49), title, F_BOX_TITLE, "#111827")
    wrap_text(draw, body, x1 + 18, y1 + 74, x2 - x1 - 36, F_BODY)


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    img = Image.new("RGB", (1600, 820), "#eef4fb")
    draw = ImageDraw.Draw(img)

    draw.text((410, 22), "Diagrama de Arquitectura del Sistema Mikuy", font=F_TITLE, fill="#111827")

    # Actor area
    rounded(draw, (35, 74, 210, 205), fill="#ffffff")
    centered_text(draw, (44, 87, 201, 122), "CLIENTE\n(USUARIO MIKUY)", F_SUBTITLE)
    draw_user_icon(draw, 122, 156)
    draw_browser_icon(draw, 60, 265)
    centered_text(draw, (45, 350, 205, 385), "Navegador web", F_BODY)
    draw_whatsapp_icon(draw, 55, 485)

    # Layer boxes
    box_with_text(
        draw,
        (285, 88, 565, 700),
        "CAPA DE PRESENTACION\n(FRONTEND MVC)",
        "ASP.NET Core MVC\n\nVistas Razor\nReservar, Consultar,\nDashboard y CRUDs\n\nBootstrap / CSS\nComponentes UI\n\nreservas.js\nWizard de reserva\nConsulta de disponibilidad\nValidacion de campos",
        "#f8fafc",
    )
    box_with_text(
        draw,
        (685, 88, 965, 700),
        "CAPA DE APLICACION\n(LOGICA WEB)",
        "Controladores MVC\nReservasController\nAdminController\nCuentaController\nMesasController\nPlatosController\nClientesController\n\nServicios\nComprobante PDF\nNotificaciones WhatsApp\n\nReglas operativas\nDisponibilidad y Best-Fit\nAutenticacion por cookies\nValidacion de estados",
        "#ffffff",
    )

    box_with_text(
        draw,
        (1085, 88, 1325, 700),
        "CAPA DE DOMINIO E\nINFRAESTRUCTURA",
        "Reserva.Domain\nCliente\nReserva\nMesa\nPlato\nUsuario\n\nReserva.Infrastructure\nEntity Framework Core\nReservationDbContext\nMigraciones\nDataSeeder\n\nSeguridad\nHashing PBKDF2-SHA256\nDatos semilla",
        "#f8fafc",
    )

    rounded(draw, (1425, 88, 1570, 700), fill="#ffffff")
    centered_text(draw, (1434, 99, 1561, 153), "CAPA DE DATOS\n(BASE DE DATOS)", F_BOX_TITLE)
    draw_db(draw, 1423, 210)
    wrap_text(
        draw,
        "Tablas relacionales\nClientes\nReservas\nMesas\nPlatos\nUsuarios\n\nEstados de reserva\nPendiente\nConfirmada\nCancelada",
        1441,
        415,
        116,
        F_BODY,
    )

    # Arrows
    arrow(draw, (210, 140), (285, 140))
    arrow(draw, (210, 305), (285, 305))
    arrow(draw, (565, 275), (685, 275), "Peticiones HTTP\nJSON / Formularios")
    arrow(draw, (685, 405), (565, 405), "Respuestas HTTP\nVistas / JSON")
    arrow(draw, (965, 275), (1085, 275), "Entidades y reglas")
    arrow(draw, (1085, 405), (965, 405), "Resultados de negocio")
    arrow(draw, (1325, 275), (1425, 275), "Consultas SQL")
    arrow(draw, (1425, 405), (1325, 405), "Datos SQL")
    arrow(draw, (185, 525), (285, 525))

    # Footer labels
    draw.text((40, 760), "Proyecto Mikuy: plataforma web para gestion de reservas, mesas, platos, dashboard administrativo y comunicacion por WhatsApp.", font=F_SMALL, fill="#374151")

    img.save(OUT, quality=95)
    print(OUT.resolve())


if __name__ == "__main__":
    main()
