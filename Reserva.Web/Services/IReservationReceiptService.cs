using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Web.Services;

public interface IReservationReceiptService
{
    byte[] BuildReceiptPdf(ReservaEntity reserva);
}
