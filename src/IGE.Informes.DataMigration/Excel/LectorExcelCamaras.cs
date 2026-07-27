using ClosedXML.Excel;

namespace IGE.Informes.DataMigration.Excel;

/// <summary>
/// Lee la hoja única de docs/camaras.xlsx. Encabezados en fila 1 (ID,
/// Ubicacion, Localidad, Monitoreo, Unidad Regional, Jurisdiccion); se lee
/// por posición de columna (A-F) porque es más robusto que buscar por
/// nombre de encabezado si alguna celda de encabezado trae espacios extra.
/// </summary>
public static class LectorExcelCamaras
{
    public static IReadOnlyCollection<FilaCamaraExcel> LeerTodas(string rutaArchivo)
    {
        using var workbook = new XLWorkbook(rutaArchivo);
        var hoja = workbook.Worksheets.First();

        var filas = new List<FilaCamaraExcel>();
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;

        for (var numeroFila = 2; numeroFila <= ultimaFila; numeroFila++)
        {
            var fila = hoja.Row(numeroFila);

            var codigo = Valor(fila, 'A');
            if (codigo is null)
            {
                continue;
            }

            filas.Add(new FilaCamaraExcel(
                codigo,
                Valor(fila, 'B'),
                Valor(fila, 'C'),
                Valor(fila, 'D'),
                Valor(fila, 'E'),
                Valor(fila, 'F')));
        }

        return filas;
    }

    private static string? Valor(IXLRow fila, char columna)
    {
        var valor = fila.Cell(columna.ToString()).GetString().Trim();
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }
}
