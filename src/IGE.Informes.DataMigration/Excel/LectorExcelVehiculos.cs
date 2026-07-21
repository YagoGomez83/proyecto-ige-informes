using ClosedXML.Excel;

namespace IGE.Informes.DataMigration.Excel;

/// <summary>
/// Lee las 4 hojas con datos reales de "Relevamiento Dominios cargados
/// Hik Central.xlsx" (ver HojasVehiculos) y las normaliza a
/// FilaVehiculoExcel. Las columnas se leen por letra porque el layout y
/// los encabezados no son consistentes entre hojas.
/// </summary>
public static class LectorExcelVehiculos
{
    public static IReadOnlyCollection<FilaVehiculoExcel> LeerTodas(string rutaArchivo)
    {
        var rutaSaneada = SaneadorXlsx.GenerarCopiaSaneada(rutaArchivo);

        try
        {
            using var workbook = new XLWorkbook(rutaSaneada);
            return LeerWorkbook(workbook);
        }
        finally
        {
            File.Delete(rutaSaneada);
        }
    }

    private static IReadOnlyCollection<FilaVehiculoExcel> LeerWorkbook(XLWorkbook workbook)
    {
        var filas = new List<FilaVehiculoExcel>();

        foreach (var config in HojasVehiculos.Todas)
        {
            if (!workbook.Worksheets.TryGetWorksheet(config.NombreHoja, out var hoja))
            {
                throw new InvalidOperationException(
                    $"No se encontró la hoja '{config.NombreHoja}' en el archivo. Revisar si el Excel cambió de estructura.");
            }

            filas.AddRange(LeerHoja(hoja, config));
        }

        return filas;
    }

    private static IEnumerable<FilaVehiculoExcel> LeerHoja(IXLWorksheet hoja, HojaExcelConfig config)
    {
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;

        for (var numeroFila = 2; numeroFila <= ultimaFila; numeroFila++)
        {
            var fila = hoja.Row(numeroFila);

            var marcaModelo = Valor(fila, config.ColMarcaModelo);
            var dominio = Valor(fila, config.ColDominio);

            if (string.IsNullOrWhiteSpace(marcaModelo) && string.IsNullOrWhiteSpace(dominio))
            {
                continue;
            }

            yield return new FilaVehiculoExcel(
                config.NombreHoja,
                dominio,
                marcaModelo,
                Valor(fila, config.ColColor),
                config.ColMotivoOCategoria is char colMotivo ? Valor(fila, colMotivo) : null,
                config.ColEstado is char colEstado ? Valor(fila, colEstado) : null,
                config.ColObservacion is char colObs ? Valor(fila, colObs) : null,
                config.ColProcedimiento is char colProc ? Valor(fila, colProc) : null);
        }
    }

    private static string? Valor(IXLRow fila, char columna)
    {
        var valor = fila.Cell(columna.ToString()).GetString().Trim();
        return string.IsNullOrWhiteSpace(valor) || valor == "-" ? null : valor;
    }
}
