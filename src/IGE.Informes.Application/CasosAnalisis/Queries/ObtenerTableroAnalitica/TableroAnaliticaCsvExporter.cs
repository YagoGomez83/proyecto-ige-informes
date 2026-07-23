using System.Globalization;
using System.Text;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ObtenerTableroAnalitica;

/// <summary>
/// Exportación a CSV del tablero de analítica (HU-06, ADR-006) — generado
/// con StringBuilder en el servidor, sin dependencia de ClosedXML/EPPlus.
/// </summary>
public static class TableroAnaliticaCsvExporter
{
    public static string ExportarPorDependencia(IReadOnlyCollection<ConteoPorDependenciaDto> conteos)
        => Exportar("DependenciaId,Cantidad", conteos, c => $"{c.DependenciaId},{c.Cantidad.ToString(CultureInfo.InvariantCulture)}");

    public static string ExportarPorTipoIncidente(IReadOnlyCollection<ConteoPorTipoIncidenteDto> conteos)
        => Exportar("TipoIncidenteId,Cantidad", conteos, c => $"{c.TipoIncidenteId},{c.Cantidad.ToString(CultureInfo.InvariantCulture)}");

    public static string ExportarPorAnalista(IReadOnlyCollection<ConteoPorAnalistaDto> conteos)
        => Exportar("AnalistaUsuarioId,Cantidad", conteos, c => $"{c.AnalistaUsuarioId},{c.Cantidad.ToString(CultureInfo.InvariantCulture)}");

    public static string ExportarPorResultado(IReadOnlyCollection<ConteoPorResultadoDto> conteos)
        => Exportar("Resultado,Cantidad", conteos, c => $"{c.Resultado},{c.Cantidad.ToString(CultureInfo.InvariantCulture)}");

    private static string Exportar<T>(string encabezado, IReadOnlyCollection<T> conteos, Func<T, string> formatearFila)
    {
        var sb = new StringBuilder();
        sb.AppendLine(encabezado);

        foreach (var conteo in conteos)
        {
            sb.AppendLine(formatearFila(conteo));
        }

        return sb.ToString();
    }
}
