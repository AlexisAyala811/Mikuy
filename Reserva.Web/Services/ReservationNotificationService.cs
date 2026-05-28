using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Options;
using Reserva.Domain.Entities;
using Reserva.Web.Models;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Web.Services;

public sealed class ReservationNotificationService : IReservationNotificationService
{
    private readonly EmailSettings _emailSettings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ReservationNotificationService> _logger;

    public ReservationNotificationService(
        IOptions<EmailSettings> emailSettings,
        IWebHostEnvironment environment,
        ILogger<ReservationNotificationService> logger)
    {
        _emailSettings = emailSettings.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task NotifyStatusChangedAsync(
        ReservaEntity reserva,
        string previousStatus,
        CancellationToken cancellationToken = default)
    {
        if (reserva.Cliente is null ||
            string.Equals(previousStatus, reserva.Estado, StringComparison.OrdinalIgnoreCase) ||
            !IsCustomerNotificationStatus(reserva.Estado))
        {
            return;
        }

        if (!_emailSettings.Enabled)
        {
            _logger.LogInformation(
                "Email notification skipped for reservation {ReservationId}; email is disabled.",
                reserva.IdReserva);
            return;
        }

        if (string.IsNullOrWhiteSpace(reserva.Cliente.Correo))
        {
            _logger.LogWarning(
                "Email notification skipped for reservation {ReservationId}; customer has no email.",
                reserva.IdReserva);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
            Subject = GetEmailSubject(reserva),
            Body = GetPlainTextEmailBody(reserva),
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };

        message.To.Add(reserva.Cliente.Correo);
        AddHtmlBody(message, reserva);

        using var smtpClient = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
        {
            EnableSsl = _emailSettings.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_emailSettings.UserName))
        {
            smtpClient.Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password);
        }

        try
        {
            await smtpClient.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            _logger.LogError(
                ex,
                "Could not send reservation email for reservation {ReservationId}.",
                reserva.IdReserva);
        }
    }

    public string BuildWhatsAppUrl(ReservaEntity reserva)
    {
        var phone = NormalizePeruPhone(reserva.Cliente?.Telefono);
        var message = Uri.EscapeDataString(BuildWhatsAppMessage(reserva));

        return string.IsNullOrWhiteSpace(phone)
            ? $"https://wa.me/?text={message}"
            : $"https://wa.me/{phone}?text={message}";
    }

    public string BuildWhatsAppMessage(ReservaEntity reserva)
    {
        var customerName = reserva.Cliente?.Nombre ?? "cliente";
        var date = reserva.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var time = reserva.Hora.ToString("HH:mm", CultureInfo.InvariantCulture);
        var statusLine = reserva.Estado switch
        {
            EstadosReserva.Confirmada => "Tu reserva fue confirmada. La mesa quedara preparada para recibirte.",
            EstadosReserva.Cancelada => "Tu reserva fue cancelada. Si deseas, puedes solicitar un nuevo horario desde la web.",
            _ => $"Tu reserva se encuentra en estado {reserva.Estado.ToLowerInvariant()}."
        };
        var mesa = reserva.Mesa is null
            ? "Mesa asignada por el equipo Mikuy"
            : $"Mesa {reserva.Mesa.Numero} - {reserva.Mesa.Ubicacion}";
        var comentario = string.IsNullOrWhiteSpace(reserva.Comentario)
            ? "Sin comentarios adicionales"
            : reserva.Comentario.Trim();

        return new StringBuilder()
            .AppendLine($"Hola {customerName},")
            .AppendLine()
            .AppendLine(statusLine)
            .AppendLine()
            .AppendLine("Detalle de la reserva en Mikuy:")
            .AppendLine($"Codigo: {reserva.CodigoReserva}")
            .AppendLine($"Fecha: {date}")
            .AppendLine($"Hora: {time}")
            .AppendLine($"Personas: {reserva.CantidadPersonas}")
            .AppendLine($"Ubicacion: {mesa}")
            .AppendLine($"Comentario: {comentario}")
            .AppendLine()
            .AppendLine("Direccion: Huamanga, Ayacucho, Peru.")
            .AppendLine("Horario de atencion: lunes a domingo, 12:00 p. m. a 10:00 p. m.")
            .AppendLine("Te recomendamos llegar 10 minutos antes para acomodarte con calma.")
            .AppendLine()
            .AppendLine(reserva.Estado == EstadosReserva.Cancelada
                ? "Gracias por avisarnos. Te esperamos en una proxima visita a Mikuy."
                : "Gracias por elegir Mikuy. Te esperamos con los sabores tradicionales de Ayacucho.")
            .ToString();
    }

    public ReservationLookupResult BuildLookupResult(ReservaEntity reserva)
    {
        return new ReservationLookupResult
        {
            IdReserva = reserva.IdReserva,
            CodigoReserva = reserva.CodigoReserva,
            ClienteNombre = reserva.Cliente?.Nombre ?? string.Empty,
            ClienteCorreo = reserva.Cliente?.Correo ?? string.Empty,
            ClienteTelefono = reserva.Cliente?.Telefono ?? string.Empty,
            Fecha = reserva.Fecha,
            Hora = reserva.Hora,
            Estado = reserva.Estado,
            CantidadPersonas = reserva.CantidadPersonas,
            MesaDescripcion = reserva.Mesa is null ? string.Empty : $"Mesa {reserva.Mesa.Numero} - {reserva.Mesa.Ubicacion}",
            Comentario = reserva.Comentario,
            WhatsAppUrl = BuildWhatsAppUrl(reserva),
            CanCancel = CanCustomerCancel(reserva)
        };
    }

    private static bool IsCustomerNotificationStatus(string status)
    {
        return status is EstadosReserva.Confirmada or EstadosReserva.Cancelada;
    }

    private static string GetEmailSubject(ReservaEntity reserva)
    {
        return reserva.Estado switch
        {
            EstadosReserva.Confirmada => "Tu reserva en Mikuy fue confirmada",
            EstadosReserva.Cancelada => "Tu reserva en Mikuy fue cancelada",
            _ => $"Actualizacion de tu reserva en Mikuy: {reserva.Estado}"
        };
    }

    private void AddHtmlBody(MailMessage message, ReservaEntity reserva)
    {
        var heroPath = Path.Combine(_environment.WebRootPath, "img", "hero", "mikuy-ayacucho.png");
        var hasHero = File.Exists(heroPath);
        var html = GetHtmlEmailBody(reserva, hasHero);
        var htmlView = AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html);

        if (hasHero)
        {
            htmlView.LinkedResources.Add(new LinkedResource(heroPath, MediaTypeNames.Image.Png)
            {
                ContentId = "mikuyHero",
                TransferEncoding = TransferEncoding.Base64
            });
        }

        message.AlternateViews.Add(htmlView);
    }

    private static string GetPlainTextEmailBody(ReservaEntity reserva)
    {
        var message = new StringBuilder()
            .AppendLine($"Hola {reserva.Cliente?.Nombre ?? "cliente"},")
            .AppendLine()
            .AppendLine(reserva.Estado switch
            {
                EstadosReserva.Confirmada => "Tu reserva en Mikuy fue confirmada.",
                EstadosReserva.Cancelada => "Tu reserva en Mikuy fue cancelada.",
                _ => $"Tu reserva en Mikuy ahora esta en estado {reserva.Estado}."
            })
            .AppendLine()
            .AppendLine($"Fecha: {reserva.Fecha:dd/MM/yyyy}")
            .AppendLine($"Codigo: {reserva.CodigoReserva}")
            .AppendLine($"Hora: {reserva.Hora:HH:mm}")
            .AppendLine($"Personas: {reserva.CantidadPersonas}");

        if (reserva.Mesa is not null)
        {
            message.AppendLine($"Mesa: {reserva.Mesa.Numero} - {reserva.Mesa.Ubicacion}");
        }

        message
            .AppendLine()
            .AppendLine("Direccion: Huamanga, Ayacucho, Peru.")
            .AppendLine("Horario: lunes a domingo, 12:00 p. m. a 10:00 p. m.")
            .AppendLine("Gracias por elegir Mikuy.");

        return message.ToString();
    }

    private static string GetHtmlEmailBody(ReservaEntity reserva, bool includeHero)
    {
        var title = reserva.Estado == EstadosReserva.Confirmada
            ? "Tu reserva fue confirmada"
            : "Tu reserva fue cancelada";
        var intro = reserva.Estado == EstadosReserva.Confirmada
            ? "Hemos preparado el detalle de tu visita. Te esperamos con los sabores tradicionales de Ayacucho."
            : "Actualizamos el estado de tu reserva. Puedes solicitar un nuevo horario cuando gustes.";
        var statusColor = reserva.Estado == EstadosReserva.Confirmada ? "#1f7a4d" : "#9b2d1f";
        var mesa = reserva.Mesa is null
            ? "Mesa asignada por el equipo Mikuy"
            : $"Mesa {reserva.Mesa.Numero} - {reserva.Mesa.Ubicacion}";
        var comentario = string.IsNullOrWhiteSpace(reserva.Comentario)
            ? "Sin comentarios adicionales"
            : WebUtility.HtmlEncode(reserva.Comentario.Trim());
        var hero = includeHero
            ? "<img src=\"cid:mikuyHero\" alt=\"Mikuy Ayacucho\" style=\"width:100%;display:block;border-radius:0 0 18px 18px;max-height:260px;object-fit:cover;\" />"
            : string.Empty;

        return $$"""
            <!doctype html>
            <html lang="es">
            <body style="margin:0;background:#f7efe5;font-family:Arial,Helvetica,sans-serif;color:#24140f;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f7efe5;padding:28px 12px;">
                    <tr>
                        <td align="center">
                            <table role="presentation" width="640" cellspacing="0" cellpadding="0" style="max-width:640px;background:#fffaf2;border:1px solid #ead8c4;border-radius:20px;overflow:hidden;">
                                <tr>
                                    <td style="background:#2b1711;padding:24px 28px;color:#ffffff;">
                                        <div style="font-size:30px;font-weight:800;letter-spacing:.2px;">Mikuy</div>
                                        <div style="font-size:13px;color:#f2c56b;text-transform:uppercase;font-weight:700;margin-top:6px;">Reservas Huamanga</div>
                                    </td>
                                </tr>
                                <tr>
                                    <td>{{hero}}</td>
                                </tr>
                                <tr>
                                    <td style="padding:30px 28px 10px;">
                                        <div style="display:inline-block;background:{{statusColor}};color:#ffffff;border-radius:999px;padding:8px 14px;font-size:13px;font-weight:700;">{{WebUtility.HtmlEncode(reserva.Estado)}}</div>
                                        <h1 style="margin:18px 0 8px;font-size:30px;line-height:1.15;color:#24140f;">{{title}}</h1>
                                        <p style="margin:0;font-size:16px;line-height:1.6;color:#6e5146;">Hola {{WebUtility.HtmlEncode(reserva.Cliente?.Nombre ?? "cliente")}}, {{intro}}</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:18px 28px;">
                                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:separate;border-spacing:0 10px;">
                                            <tr>
                                                <td style="background:#fff3e1;border:1px solid #efd7b5;border-radius:14px;padding:16px;">
                                                    <strong style="display:block;color:#8f2e1d;font-size:13px;text-transform:uppercase;">Fecha</strong>
                                                    <span style="font-size:20px;font-weight:700;">{{reserva.Fecha:dd/MM/yyyy}}</span>
                                                </td>
                                                <td width="12"></td>
                                                <td style="background:#fff3e1;border:1px solid #efd7b5;border-radius:14px;padding:16px;">
                                                    <strong style="display:block;color:#8f2e1d;font-size:13px;text-transform:uppercase;">Hora</strong>
                                                    <span style="font-size:20px;font-weight:700;">{{reserva.Hora:HH:mm}}</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="background:#fff3e1;border:1px solid #efd7b5;border-radius:14px;padding:16px;">
                                                    <strong style="display:block;color:#8f2e1d;font-size:13px;text-transform:uppercase;">Personas</strong>
                                                    <span style="font-size:20px;font-weight:700;">{{reserva.CantidadPersonas}}</span>
                                                </td>
                                                <td width="12"></td>
                                                <td style="background:#fff3e1;border:1px solid #efd7b5;border-radius:14px;padding:16px;">
                                                    <strong style="display:block;color:#8f2e1d;font-size:13px;text-transform:uppercase;">Mesa</strong>
                                                    <span style="font-size:18px;font-weight:700;">{{WebUtility.HtmlEncode(mesa)}}</span>
                                                </td>
                                            </tr>
                                        </table>
                                        <div style="background:#ffffff;border:1px solid #ead8c4;border-radius:14px;padding:16px;margin-top:4px;">
                                            <strong style="display:block;color:#8f2e1d;font-size:13px;text-transform:uppercase;">Codigo de reserva</strong>
                                            <p style="margin:6px 0 0;color:#24140f;font-size:18px;font-weight:700;">{{WebUtility.HtmlEncode(reserva.CodigoReserva)}}</p>
                                        </div>
                                        <div style="background:#ffffff;border:1px solid #ead8c4;border-radius:14px;padding:16px;margin-top:4px;">
                                            <strong style="display:block;color:#8f2e1d;font-size:13px;text-transform:uppercase;">Comentario</strong>
                                            <p style="margin:6px 0 0;color:#4c332a;line-height:1.5;">{{comentario}}</p>
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:10px 28px 30px;">
                                        <p style="margin:0 0 10px;color:#4c332a;line-height:1.6;">Direccion: Huamanga, Ayacucho, Peru.</p>
                                        <p style="margin:0 0 10px;color:#4c332a;line-height:1.6;">Horario de atencion: lunes a domingo, 12:00 p. m. a 10:00 p. m.</p>
                                        <p style="margin:0;color:#4c332a;line-height:1.6;">Si tu reserva fue confirmada, te recomendamos llegar 10 minutos antes para acomodarte con calma.</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="background:#2b1711;color:#e8d7c8;padding:18px 28px;font-size:13px;line-height:1.5;">
                                        Mikuy - Sabores tradicionales de Ayacucho. Este mensaje fue generado por el sistema de reservas.
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }

    private static bool CanCustomerCancel(ReservaEntity reserva)
    {
        return reserva.Estado != EstadosReserva.Cancelada &&
            reserva.Fecha >= DateOnly.FromDateTime(DateTime.Today);
    }

    private static string NormalizePeruPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("51", StringComparison.Ordinal) && digits.Length >= 11)
        {
            return digits;
        }

        if (digits.Length == 9)
        {
            return $"51{digits}";
        }

        return digits;
    }
}
