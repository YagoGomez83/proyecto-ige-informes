using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.GenerarInformeDesdeCaso;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record GenerarInformeDesdeCasoCommand(
    Guid CasoAnalisisId,
    Guid DependenciaDestinoId,
    string CausaCaratula,
    string CausaNroPiezaSumarial,
    string CausaCircunscripcionJudicial) : IRequest<Guid>;
