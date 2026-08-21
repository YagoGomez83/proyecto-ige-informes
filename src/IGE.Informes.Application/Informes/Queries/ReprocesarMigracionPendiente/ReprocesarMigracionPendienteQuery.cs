using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.ReprocesarMigracionPendiente;

public sealed record ReprocesarMigracionPendienteResultDto(string? IdRegistro, DateOnly? FechaAnalisis);

/// <summary>
/// Vuelve a leer el PDF ya guardado de una MigracionPendiente (HU-04) y lo
/// re-parsea con la versión actual de InformePdfParser — no persiste nada,
/// solo devuelve lo que el parser logre reconocer ahora para precargar el
/// formulario de /informes/migrar/pendientes. Existe porque el parser se
/// sigue corrigiendo con el tiempo (ver casos reales de formato de fecha en
/// docs/06-seguridad-amenazas.md) y las MigracionesPendientes ya creadas no
/// se reprocesan solas: sin este mecanismo, cada fix del parser solo
/// beneficia a archivos subidos después, dejando el trabajo manual de
/// completar las ya existentes sin aprovecharlo.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ReprocesarMigracionPendienteQuery(Guid MigracionPendienteId) : IRequest<ReprocesarMigracionPendienteResultDto>;
