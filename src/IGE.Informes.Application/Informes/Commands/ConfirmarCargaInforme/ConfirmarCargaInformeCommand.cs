using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.ConfirmarCargaInforme;

public sealed record EvidenciaConfirmadaDto(
    int NumeroImagen,
    Guid? CamaraId,
    DateTime? FechaHoraCaptura,
    string? Descripcion);

/// <summary>
/// Persiste el Informe (Borrador) + sus Evidencia a partir de los datos
/// ya revisados/corregidos por el Analista en la pantalla de confirmación
/// (HU-01) — nunca los datos crudos del parser sin pasar por esa revisión.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ConfirmarCargaInformeCommand(
    byte[] ContenidoPdf,
    string NombreArchivo,
    string IdRegistro,
    DateOnly FechaAnalisis,
    Guid CasoAnalisisId,
    Guid DependenciaDestinoId,
    string? CausaCaratula,
    string? CausaNroPiezaSumarial,
    string? CausaCircunscripcionJudicial,
    string? Relato,
    bool ReemplazarSiExiste,
    IReadOnlyCollection<EvidenciaConfirmadaDto> Evidencias) : IRequest<Guid>;
