using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.CrearInformeDesdeMigracionPendiente;

/// <summary>
/// Completa una MigracionPendiente (HU-04) con la Fecha de Análisis que
/// el parser no pudo reconocer — crea el Informe real con los datos ya
/// extraídos y borra la MigracionPendiente. Ver
/// docs/03-modelo-dominio.md, "Decisiones ya resueltas".
/// </summary>
[Autorizar(Roles.Admin)]
public sealed record CrearInformeDesdeMigracionPendienteCommand(Guid MigracionPendienteId, DateOnly FechaAnalisis)
    : IRequest<Guid>;
