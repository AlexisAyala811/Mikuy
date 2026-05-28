using System.Globalization;
using System.Text;
using Reserva.Domain.Entities;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Web.Services;

public sealed class ReservationReceiptService : IReservationReceiptService
{
    private const double PageWidth = 612;
    private const double PageHeight = 792;

    public byte[] BuildReceiptPdf(ReservaEntity reserva)
    {
        var content = BuildPageContent(reserva);
        return BuildPdf(content);
    }

    private static string BuildPageContent(ReservaEntity reserva)
    {
        var builder = new StringBuilder();
        var statusColor = reserva.Estado switch
        {
            EstadosReserva.Confirmada => (R: 0.12, G: 0.48, B: 0.30),
            EstadosReserva.Cancelada => (R: 0.61, G: 0.18, B: 0.12),
            _ => (R: 0.79, G: 0.48, B: 0.12)
        };
        var mesa = reserva.Mesa is null
            ? "Mesa por asignar"
            : $"Mesa {reserva.Mesa.Numero} - {reserva.Mesa.Ubicacion}";
        var comentario = string.IsNullOrWhiteSpace(reserva.Comentario)
            ? "Sin comentario adicional."
            : reserva.Comentario.Trim();

        DrawBackground(builder);
        DrawHeader(builder, reserva);
        DrawVisualBand(builder, statusColor);
        DrawTitle(builder, reserva, statusColor);

        DrawCard(builder, 54, 292, 238, 168, "Datos del cliente");
        DrawInfoRow(builder, "Cliente", reserva.Cliente?.Nombre ?? "Cliente", 72, 414);
        DrawInfoRow(builder, "Correo", reserva.Cliente?.Correo ?? "No registrado", 72, 372);
        DrawInfoRow(builder, "Telefono", reserva.Cliente?.Telefono ?? "No registrado", 72, 330);

        DrawCard(builder, 320, 292, 238, 168, "Detalle de reserva");
        DrawInfoRow(builder, "Fecha", reserva.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 338, 414);
        DrawInfoRow(builder, "Hora", reserva.Hora.ToString("HH:mm", CultureInfo.InvariantCulture), 338, 372);
        DrawInfoRow(builder, "Personas", reserva.CantidadPersonas.ToString(CultureInfo.InvariantCulture), 338, 330);

        DrawCard(builder, 54, 170, 504, 86, "Mesa y comentario");
        DrawText(builder, mesa, 72, 218, 13, "F2", 0.17, 0.11, 0.09);
        DrawWrappedText(builder, comentario, 72, 193, 456, 11, "F1", 0.42, 0.31, 0.27);

        DrawFooter(builder);
        return builder.ToString();
    }

    private static void DrawBackground(StringBuilder builder)
    {
        Rect(builder, 0, 0, PageWidth, PageHeight, 0.98, 0.94, 0.88);
        Rect(builder, 34, 40, 544, 712, 1, 0.99, 0.96);
        StrokeRect(builder, 34, 40, 544, 712, 0.91, 0.83, 0.74);
        Rect(builder, 34, 680, 544, 72, 0.17, 0.09, 0.07);
    }

    private static void DrawHeader(StringBuilder builder, ReservaEntity reserva)
    {
        DrawText(builder, "Mikuy", 58, 716, 34, "F2", 1, 1, 1);
        DrawText(builder, "Reservas Huamanga", 60, 694, 11, "F1", 0.96, 0.78, 0.42);

        Rect(builder, 395, 704, 142, 26, 0.98, 0.94, 0.86);
        DrawText(builder, reserva.CodigoReserva, 408, 712, 11, "F2", 0.55, 0.18, 0.10);
    }

    private static void DrawVisualBand(StringBuilder builder, (double R, double G, double B) statusColor)
    {
        Rect(builder, 54, 570, 504, 82, 0.55, 0.18, 0.10);
        Rect(builder, 54, 570, 168, 82, 0.20, 0.33, 0.16);
        Rect(builder, 222, 570, 168, 82, 0.84, 0.51, 0.17);
        Rect(builder, 390, 570, 168, 82, statusColor.R, statusColor.G, statusColor.B);

        DrawText(builder, "Sabores tradicionales", 76, 620, 18, "F2", 1, 0.96, 0.88);
        DrawText(builder, "Ayacucho en la mesa", 76, 596, 12, "F1", 1, 0.96, 0.88);
        DrawText(builder, "Reserva", 410, 620, 18, "F2", 1, 1, 1);
        DrawText(builder, "Mikuy", 410, 598, 12, "F1", 1, 1, 1);

        StrokeRect(builder, 54, 570, 504, 82, 0.77, 0.62, 0.45);
        Rect(builder, 76, 580, 34, 7, 0.98, 0.94, 0.86);
        Rect(builder, 116, 580, 34, 7, 0.98, 0.94, 0.86);
        Rect(builder, 156, 580, 34, 7, 0.98, 0.94, 0.86);
    }

    private static void DrawTitle(StringBuilder builder, ReservaEntity reserva, (double R, double G, double B) statusColor)
    {
        DrawText(builder, "Comprobante de reserva", 54, 526, 23, "F2", 0.17, 0.11, 0.09);
        DrawText(builder, "Presente este codigo al llegar o uselo para consultar su reserva.", 54, 500, 10, "F1", 0.45, 0.35, 0.31);

        Rect(builder, 424, 506, 134, 28, statusColor.R, statusColor.G, statusColor.B);
        DrawText(builder, reserva.Estado, 446, 515, 12, "F2", 1, 1, 1);
    }

    private static void DrawCard(StringBuilder builder, double x, double y, double width, double height, string title)
    {
        Rect(builder, x, y, width, height, 1, 0.96, 0.90);
        StrokeRect(builder, x, y, width, height, 0.91, 0.82, 0.72);
        DrawText(builder, title, x + 18, y + height - 28, 12, "F2", 0.55, 0.18, 0.10);
    }

    private static void DrawInfoRow(StringBuilder builder, string label, string value, double x, double y)
    {
        DrawText(builder, label.ToUpperInvariant(), x, y, 8, "F2", 0.55, 0.18, 0.10);
        DrawWrappedText(builder, value, x, y - 16, 196, 12, "F1", 0.13, 0.08, 0.06);
    }

    private static void DrawFooter(StringBuilder builder)
    {
        Rect(builder, 54, 84, 504, 70, 0.17, 0.09, 0.07);
        DrawText(builder, "Mikuy - Sabores tradicionales de Ayacucho", 76, 124, 13, "F2", 1, 0.96, 0.88);
        DrawText(builder, "Direccion: Huamanga, Ayacucho, Peru", 76, 106, 10, "F1", 0.93, 0.84, 0.76);
        DrawText(builder, "Horario: lunes a domingo, 12:00 p. m. a 10:00 p. m.", 76, 91, 10, "F1", 0.93, 0.84, 0.76);
    }

    private static void DrawWrappedText(
        StringBuilder builder,
        string value,
        double x,
        double y,
        double maxWidth,
        int size,
        string font,
        double r,
        double g,
        double b)
    {
        var currentLine = new StringBuilder();
        var currentY = y;
        var maxCharacters = Math.Max(18, (int)(maxWidth / (size * 0.52)));

        foreach (var word in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (currentLine.Length + word.Length + 1 > maxCharacters)
            {
                DrawText(builder, currentLine.ToString(), x, currentY, size, font, r, g, b);
                currentLine.Clear();
                currentY -= size + 5;
            }

            if (currentLine.Length > 0)
            {
                currentLine.Append(' ');
            }

            currentLine.Append(word);
        }

        if (currentLine.Length > 0)
        {
            DrawText(builder, currentLine.ToString(), x, currentY, size, font, r, g, b);
        }
    }

    private static void DrawText(
        StringBuilder builder,
        string text,
        double x,
        double y,
        int size,
        string font,
        double r,
        double g,
        double b)
    {
        builder
            .AppendLine("BT")
            .AppendLine($"{r:0.###} {g:0.###} {b:0.###} rg")
            .AppendLine($"/{font} {size} Tf")
            .AppendLine($"{x:0.##} {y:0.##} Td")
            .Append('(')
            .Append(EscapePdfText(ToPdfText(text)))
            .AppendLine(") Tj")
            .AppendLine("ET");
    }

    private static void Rect(StringBuilder builder, double x, double y, double width, double height, double r, double g, double b)
    {
        builder
            .AppendLine("q")
            .AppendLine($"{r:0.###} {g:0.###} {b:0.###} rg")
            .AppendLine($"{x:0.##} {y:0.##} {width:0.##} {height:0.##} re f")
            .AppendLine("Q");
    }

    private static void StrokeRect(StringBuilder builder, double x, double y, double width, double height, double r, double g, double b)
    {
        builder
            .AppendLine("q")
            .AppendLine($"{r:0.###} {g:0.###} {b:0.###} RG")
            .AppendLine($"{x:0.##} {y:0.##} {width:0.##} {height:0.##} re S")
            .AppendLine("Q");
    }

    private static byte[] BuildPdf(string pageContent)
    {
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
            $"<< /Length {Encoding.Latin1.GetByteCount(pageContent)} >>\nstream\n{pageContent}endstream"
        };

        using var stream = new MemoryStream();
        Write(stream, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };

        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            Write(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xrefPosition = stream.Position;
        Write(stream, $"xref\n0 {objects.Length + 1}\n");
        Write(stream, "0000000000 65535 f \n");

        foreach (var offset in offsets.Skip(1))
        {
            Write(stream, $"{offset:0000000000} 00000 n \n");
        }

        Write(stream, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");
        return stream.ToArray();
    }

    private static string ToPdfText(string value)
    {
        return value
            .Replace("á", "a", StringComparison.Ordinal)
            .Replace("é", "e", StringComparison.Ordinal)
            .Replace("í", "i", StringComparison.Ordinal)
            .Replace("ó", "o", StringComparison.Ordinal)
            .Replace("ú", "u", StringComparison.Ordinal)
            .Replace("Á", "A", StringComparison.Ordinal)
            .Replace("É", "E", StringComparison.Ordinal)
            .Replace("Í", "I", StringComparison.Ordinal)
            .Replace("Ó", "O", StringComparison.Ordinal)
            .Replace("Ú", "U", StringComparison.Ordinal)
            .Replace("ñ", "n", StringComparison.Ordinal)
            .Replace("Ñ", "N", StringComparison.Ordinal);
    }

    private static string EscapePdfText(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static void Write(Stream stream, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
