using IGE.Informes.Application.CasosAnalisis.Queries.ObtenerTableroAnalitica;
using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.CasosAnalisis;

/// <summary>
/// HU-06 · Tablero de analítica de gestión (Épica 02), escenario "Exportar
/// reporte". Según ADR-006, la exportación es CSV generado en el servidor
/// con System.Text.StringBuilder (sin dependencia nueva tipo ClosedXML/
/// EPPlus), como un método de servicio en Application reutilizable por los
/// cuatro reportes de HU-06 (por Dependencia, TipoIncidente, Analista y
/// Resultado).
///
/// Todavía NO existe TableroAnaliticaCsvExporter en producción — estos
/// tests son la especificación ejecutable (deben fallar en rojo por
/// ausencia de implementación / error de compilación) hasta que se
/// implemente.
/// </summary>
public class TableroAnaliticaCsvExporterTests
{
    [Fact]
    public void ExportarPorDependencia_ConConteos_GeneraCsvConEncabezadoYUnaFilaPorDependencia()
    {
        var dependenciaA = Guid.NewGuid();
        var dependenciaB = Guid.NewGuid();

        var conteos = new List<ConteoPorDependenciaDto>
        {
            new(dependenciaA, 5),
            new(dependenciaB, 3),
        };

        var csv = TableroAnaliticaCsvExporter.ExportarPorDependencia(conteos);

        var lineas = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(3, lineas.Length); // encabezado + 2 filas
        Assert.Contains($"{dependenciaA},5", csv);
        Assert.Contains($"{dependenciaB},3", csv);
    }

    [Fact]
    public void ExportarPorTipoIncidente_ConConteos_GeneraCsvConEncabezadoYUnaFilaPorTipoIncidente()
    {
        var tipoRobo = Guid.NewGuid();

        var conteos = new List<ConteoPorTipoIncidenteDto> { new(tipoRobo, 7) };

        var csv = TableroAnaliticaCsvExporter.ExportarPorTipoIncidente(conteos);

        var lineas = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(2, lineas.Length);
        Assert.Contains($"{tipoRobo},7", csv);
    }

    [Fact]
    public void ExportarPorAnalista_ConConteos_GeneraCsvConEncabezadoYUnaFilaPorAnalista()
    {
        var analista = Guid.NewGuid();

        var conteos = new List<ConteoPorAnalistaDto> { new(analista, 4) };

        var csv = TableroAnaliticaCsvExporter.ExportarPorAnalista(conteos);

        var lineas = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(2, lineas.Length);
        Assert.Contains($"{analista},4", csv);
    }

    [Fact]
    public void ExportarPorResultado_ConConteos_GeneraCsvConEncabezadoYUnaFilaPorResultado()
    {
        var conteos = new List<ConteoPorResultadoDto>
        {
            new(ResultadoCaso.Positivo, 10),
            new(ResultadoCaso.Negativo, 2),
            new(ResultadoCaso.Revision, 1),
        };

        var csv = TableroAnaliticaCsvExporter.ExportarPorResultado(conteos);

        var lineas = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(4, lineas.Length);
        Assert.Contains("Positivo,10", csv);
        Assert.Contains("Negativo,2", csv);
        Assert.Contains("Revision,1", csv);
    }

    [Fact]
    public void ExportarPorDependencia_SinConteos_GeneraCsvSoloConEncabezado()
    {
        var csv = TableroAnaliticaCsvExporter.ExportarPorDependencia([]);

        var lineas = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Single(lineas);
    }
}
