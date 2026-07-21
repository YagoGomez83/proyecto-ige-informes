using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Commands.VincularVehiculoTexto;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record VincularVehiculoTextoCommand(Guid CasoId, string DescripcionLibre) : IRequest;
