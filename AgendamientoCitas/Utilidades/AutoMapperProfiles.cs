using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AutoMapper;

namespace AgendamientoCitas.Mapping;

public sealed class AgendamientoProfile : Profile
{
    public AgendamientoProfile()
    {
        CreateMap<ClienteCrearDTO, Cliente>();
        CreateMap<ClienteModificarDTO, Cliente>();
        CreateMap<Cliente, ClienteConsultarDTO>();

        CreateMap<ServicioCrearDTO, Servicio>();
        CreateMap<ServicioModificarDTO, Servicio>();
        CreateMap<Servicio, ServicioConsultarDTO>();

        CreateMap<CitaCrearDTO, Cita>()
            .ForMember(destination => destination.Estado, options => options.MapFrom(source => source.Estado ?? EstadoCita.Programada));
        CreateMap<CitaModificarDTO, Cita>()
            .ForMember(destination => destination.Estado, options => options.MapFrom(source => source.Estado ?? EstadoCita.Programada));
        CreateMap<Cita, CitaConsultarDTO>();

        CreateMap<IngresoCrearDTO, Ingreso>();
        CreateMap<IngresoModificarDTO, Ingreso>();
        CreateMap<Ingreso, IngresoConsultarDTO>();

        CreateMap<GastoCrearDTO, Gasto>();
        CreateMap<GastoModificarDTO, Gasto>();
        CreateMap<Gasto, GastoConsultarDTO>();
    }
}
