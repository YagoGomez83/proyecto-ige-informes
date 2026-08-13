using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarVehiculosQuery(
    int Pagina = 1,
    int TamanioPagina = 50,
    EstadoVehiculo? Estado = null,
    OrdenVehiculos Orden = OrdenVehiculos.Estado) : IRequest<PagedResult<VehiculoResumenDto>>;
