using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Commands.CerrarCaso;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CerrarCasoCommand(Guid CasoId, ResultadoCaso Resultado) : IRequest;
