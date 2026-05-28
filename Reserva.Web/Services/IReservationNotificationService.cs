using Reserva.Domain.Entities;
using Reserva.Web.Models;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Web.Services;

public interface IReservationNotificationService
{
    Task NotifyStatusChangedAsync(
        ReservaEntity reserva,
        string previousStatus,
        CancellationToken cancellationToken = default);

    string BuildWhatsAppUrl(ReservaEntity reserva);

    string BuildWhatsAppMessage(ReservaEntity reserva);

    ReservationLookupResult BuildLookupResult(ReservaEntity reserva);
}
