using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.ListarMigracionesPendientes;

public sealed record MigracionPendienteDto(
    Guid Id,
    string IdRegistro,
    string? CausaCaratula,
    string? PiezaSumarial,
    Guid DependenciaDestinoId);

/// <summary>
/// Alimenta /informes/migrar/pendientes (HU-04) — PDFs históricos cuyo ID
/// Registro se reconoció pero la Fecha de Análisis no, pendientes de que
/// el Administrador la complete (ver MigracionPendiente en el Domain).
/// </summary>
[Autorizar(Roles.Admin)]
public sealed record ListarMigracionesPendientesQuery : IRequest<IReadOnlyCollection<MigracionPendienteDto>>;
