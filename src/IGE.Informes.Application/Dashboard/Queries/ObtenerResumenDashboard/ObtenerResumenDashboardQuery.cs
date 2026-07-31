using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Dashboard.Queries.ObtenerResumenDashboard;

public sealed record ResumenDashboardDto(
    int CasosAbiertos,
    int VehiculosConAlertaVigente,
    int InformesEnBorrador,
    int InformesPublicados);

/// <summary>
/// KPIs livianos para el Home ("/"), visibles a los 3 roles — sin filtros ni
/// gráficos, a diferencia del Tablero de Analítica (HU-06, restringido a
/// Supervisor/Admin). Solo conteos simples sobre reglas ya existentes del
/// dominio, sin lógica de negocio nueva.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ObtenerResumenDashboardQuery : IRequest<ResumenDashboardDto>;

