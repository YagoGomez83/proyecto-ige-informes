using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.ListarMigracionesPendientes;

public sealed record MigracionPendienteDto(
    Guid Id,
    string? IdRegistro,
    string? CausaCaratula,
    string? PiezaSumarial,
    Guid DependenciaDestinoId,
    string PdfUrl);

/// <summary>
/// Alimenta /informes/migrar/pendientes (HU-04) — PDFs históricos a los
/// que el parser no les reconoció el ID Registro y/o la Fecha de
/// Análisis, pendientes de que el Administrador complete lo que falte
/// (ver MigracionPendiente en el Domain). IdRegistro null significa que
/// también hay que completarlo, no solo la fecha. PdfUrl es una URL
/// prefirmada de corta expiración (mismo criterio que
/// ObtenerInformePorIdQueryHandler) para poder leer la fecha/ID directo
/// del PDF original en vez de completarlos a ciegas.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarMigracionesPendientesQuery : IRequest<IReadOnlyCollection<MigracionPendienteDto>>;
