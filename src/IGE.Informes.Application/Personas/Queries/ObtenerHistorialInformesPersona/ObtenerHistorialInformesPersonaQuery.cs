using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Personas.Queries.ObtenerHistorialInformesPersona;

/// <summary>
/// Ficha 360° de una Persona (HU-07, Épica 02) — todos los Informes donde
/// aparece, vía las Evidencias que la vinculan, ordenados cronológicamente.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ObtenerHistorialInformesPersonaQuery(Guid PersonaId)
    : IRequest<IReadOnlyCollection<InformeHistorialDto>>;
