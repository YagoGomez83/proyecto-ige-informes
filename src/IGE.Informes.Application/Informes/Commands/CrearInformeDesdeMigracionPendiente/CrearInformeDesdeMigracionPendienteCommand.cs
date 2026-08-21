using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.CrearInformeDesdeMigracionPendiente;

/// <summary>
/// Completa una MigracionPendiente (HU-04) con el ID Registro y/o la
/// Fecha de Análisis que el parser no pudo reconocer — crea el Informe
/// real con los datos ya extraídos y borra la MigracionPendiente.
/// <paramref name="IdRegistro"/> es obligatorio solo si la
/// MigracionPendiente todavía no tenía uno propio (el Handler valida
/// eso, no este record); si ya lo tenía, este valor se ignora. Ver
/// docs/03-modelo-dominio.md, "Decisiones ya resueltas".
/// <paramref name="CausaId"/> es opcional: si el usuario eligió una
/// Causa sugerida en la UI, el Handler la usa directamente en vez de
/// intentar el auto-match por Pieza Sumarial exacta (ver
/// CrearInformeDesdeMigracionPendienteCommandHandler).
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CrearInformeDesdeMigracionPendienteCommand(
    Guid MigracionPendienteId, DateOnly FechaAnalisis, string? IdRegistro = null, Guid? CausaId = null)
    : IRequest<Guid>;
