using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.ListarCausasAutoAsignadas;

public sealed record InformeConCausaAutoAsignadaDto(
    Guid InformeId,
    string IdRegistro,
    string CausaCaratula,
    string? CausaNroPiezaSumarial,
    DateTime FechaAutoAsignacion);

/// <summary>
/// Alimenta /informes/causas-auto-asignadas (Admin) — Informes migrados
/// cuya Causa se vinculó automáticamente por matching de N° de Pieza
/// Sumarial (ver CausaMatcher), sin que ningún humano la haya visto ni
/// confirmado en el momento, a diferencia de la edición manual (HU-02).
/// Existe para poder revisar en un solo lugar todas esas vinculaciones
/// después de una migración masiva, en vez de abrir cada Informe uno por
/// uno (hallazgo del security-reviewer: colisión accidental de N° de
/// Pieza Sumarial vincularía en silencio al expediente equivocado).
/// </summary>
[Autorizar(Roles.Admin)]
public sealed record ListarCausasAutoAsignadasQuery : IRequest<IReadOnlyCollection<InformeConCausaAutoAsignadaDto>>;
