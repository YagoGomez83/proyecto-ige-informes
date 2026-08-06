using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.ListarMigracionesPendientes;

public sealed record MigracionPendienteDto(
    Guid Id,
    string? IdRegistro,
    string? CausaCaratula,
    string? PiezaSumarial,
    Guid DependenciaDestinoId);

/// <summary>
/// Alimenta /informes/migrar/pendientes (HU-04) — PDFs históricos a los
/// que el parser no les reconoció el ID Registro y/o la Fecha de
/// Análisis, pendientes de que el Administrador complete lo que falte
/// (ver MigracionPendiente en el Domain). IdRegistro null significa que
/// también hay que completarlo, no solo la fecha.
/// </summary>
[Autorizar(Roles.Admin)]
public sealed record ListarMigracionesPendientesQuery : IRequest<IReadOnlyCollection<MigracionPendienteDto>>;
