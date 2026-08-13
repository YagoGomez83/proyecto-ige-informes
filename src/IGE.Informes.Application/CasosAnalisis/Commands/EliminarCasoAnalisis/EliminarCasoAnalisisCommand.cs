using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Commands.EliminarCasoAnalisis;

[Autorizar(Roles.Supervisor, Roles.Admin)]
public sealed record EliminarCasoAnalisisCommand(Guid CasoId) : IRequest;
