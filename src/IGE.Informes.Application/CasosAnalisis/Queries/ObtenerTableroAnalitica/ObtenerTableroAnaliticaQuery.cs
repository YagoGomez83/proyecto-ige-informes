using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ObtenerTableroAnalitica;

public sealed record ConteoPorDependenciaDto(Guid DependenciaId, int Cantidad);

public sealed record ConteoPorTipoIncidenteDto(Guid TipoIncidenteId, int Cantidad);

public sealed record ConteoPorAnalistaDto(Guid AnalistaUsuarioId, int Cantidad);

public sealed record ConteoPorResultadoDto(ResultadoCaso Resultado, int Cantidad);

public sealed record TableroAnaliticaDto(
    IReadOnlyCollection<ConteoPorDependenciaDto> PorDependencia,
    IReadOnlyCollection<ConteoPorTipoIncidenteDto> PorTipoIncidente,
    IReadOnlyCollection<ConteoPorAnalistaDto> PorAnalista,
    IReadOnlyCollection<ConteoPorResultadoDto> PorResultado);

/// <summary>
/// Tablero de analítica de gestión (HU-06, Épica 02) — cuenta CasoAnalisis
/// (no Informe, que es opcional y no tiene Resultado) por Dependencia
/// (jurisdicción del llamado), TipoIncidente, Analista (vía CasoAnalista,
/// cualquier rol) y Resultado, en un rango de fechas opcional. Ver nota de
/// decisión en docs/epic-02-busqueda-analitica.md.
/// </summary>
[Autorizar(Roles.Supervisor, Roles.Admin)]
public sealed record ObtenerTableroAnaliticaQuery(
    DateOnly? FechaDesde = null,
    DateOnly? FechaHasta = null) : IRequest<TableroAnaliticaDto>;
