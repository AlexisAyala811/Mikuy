using AutoMapper;
using Reserva.Domain.Entities;
using Reserva.Web.DTOs;
using ReservaEntity = Reserva.Domain.Entities.Reserva;

namespace Reserva.Web.Mapping;

public sealed class ReservationMappingProfile : Profile
{
    public ReservationMappingProfile()
    {
        CreateMap<Cliente, ClienteDto>().ReverseMap();

        CreateMap<ReservaEntity, ReservaDto>()
            .ForMember(
                destination => destination.ClienteNombre,
                options => options.MapFrom(source => source.Cliente == null ? null : source.Cliente.Nombre))
            .ForMember(
                destination => destination.ClienteTelefono,
                options => options.MapFrom(source => source.Cliente == null ? null : source.Cliente.Telefono))
            .ForMember(
                destination => destination.MesaDescripcion,
                options => options.MapFrom(source => source.Mesa == null ? null : $"Mesa {source.Mesa.Numero} - {source.Mesa.Ubicacion}"));

        CreateMap<ReservaDto, ReservaEntity>()
            .ForMember(destination => destination.CodigoReserva, options => options.Ignore())
            .ForMember(destination => destination.Cliente, options => options.Ignore())
            .ForMember(destination => destination.Mesa, options => options.Ignore());
    }
}
