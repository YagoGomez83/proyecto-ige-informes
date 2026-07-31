using IGE.Informes.Application.CasosAnalisis.Queries.ListarCasos;
using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Queries.BuscarCasos;

/// <summary>
/// Búsqueda de Casos de Análisis por texto libre (Observaciones,
/// ElementoSustraido, VehiculoInvolucradoTexto, NroLlamado911) para la
/// búsqueda combinada de /busqueda — extensión de HU-05. Contains/ILIKE
/// simple sobre las notas de texto libre del Caso, no full-text search.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record BuscarCasosQuery(string TextoLibre) : IRequest<IReadOnlyCollection<CasoAnalisisResumenDto>>;
