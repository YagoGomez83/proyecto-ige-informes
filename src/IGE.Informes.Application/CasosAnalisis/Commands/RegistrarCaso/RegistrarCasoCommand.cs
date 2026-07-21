using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Commands.RegistrarCaso;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record RegistrarCasoCommand(
    DateOnly Fecha,
    Guid DependenciaId,
    Guid TipoIncidenteId,
    string? NroLlamado911,
    string? Observaciones) : IRequest<Guid>;
