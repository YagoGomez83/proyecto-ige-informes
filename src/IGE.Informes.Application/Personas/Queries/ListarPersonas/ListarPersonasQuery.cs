using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Personas.Queries.ListarPersonas;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarPersonasQuery(
    int Pagina = 1,
    int TamanioPagina = 50,
    bool? Identificada = null,
    RolPersona? Rol = null,
    OrdenPersonas Orden = OrdenPersonas.Estado) : IRequest<PagedResult<PersonaResumenDto>>;
