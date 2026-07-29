using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.EditarInforme;

/// <summary>
/// Corrige metadatos de un Informe en Borrador (HU-02). Cada campo es
/// opcional: null significa "no tocar ese campo" — permite corregir solo
/// el que haga falta sin reenviar todo el Informe, mismo criterio que
/// ConfirmarCargaInformeCommand.Relato. La Causa es inmutable en el
/// dominio, así que editarla implica crear una Causa nueva y reasignar
/// Informe.CausaId.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record EditarInformeCommand(
    Guid InformeId,
    string? Relato,
    Guid? DependenciaDestinoId,
    string? CausaCaratula,
    string? CausaNroPiezaSumarial,
    string? CausaCircunscripcionJudicial,
    DateOnly? FechaAnalisis = null) : IRequest;
