using System.IO.Compression;
using System.Text.RegularExpressions;

namespace IGE.Informes.DataMigration.Excel;

/// <summary>
/// ClosedXML no puede parsear archivos .xlsx con reglas de validación de
/// datos (dropdowns) mal formadas — algunos .xlsx generados/editados con
/// herramientas externas quedan con un &lt;dataValidation&gt; cuyo atributo
/// supera el límite de 255 caracteres que ClosedXML asume al cargar, y
/// tira ArgumentOutOfRangeException antes de poder leer ninguna celda.
/// Este saneador genera una copia temporal del archivo sin los nodos
/// &lt;dataValidations&gt; de cada hoja (no toca datos, solo quita las
/// reglas de validación/dropdowns) — el archivo original no se modifica.
/// </summary>
public static class SaneadorXlsx
{
    public static string GenerarCopiaSaneada(string rutaOriginal)
    {
        var rutaTemporal = Path.Combine(Path.GetTempPath(), $"ige-migracion-{Guid.NewGuid():N}.xlsx");
        File.Copy(rutaOriginal, rutaTemporal, overwrite: true);

        using var archivo = ZipFile.Open(rutaTemporal, ZipArchiveMode.Update);

        var hojasXml = archivo.Entries
            .Where(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
                && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var entrada in hojasXml)
        {
            QuitarDataValidations(entrada);
        }

        return rutaTemporal;
    }

    private static void QuitarDataValidations(ZipArchiveEntry entrada)
    {
        string contenido;
        using (var lector = new StreamReader(entrada.Open()))
        {
            contenido = lector.ReadToEnd();
        }

        var sinValidaciones = Regex.Replace(
            contenido,
            @"<dataValidations[^>]*>.*?</dataValidations>",
            string.Empty,
            RegexOptions.Singleline);

        if (sinValidaciones == contenido)
        {
            return;
        }

        var nombreEntrada = entrada.FullName;
        var archivo = entrada.Archive;
        entrada.Delete();

        var nuevaEntrada = archivo.CreateEntry(nombreEntrada);
        using var escritor = new StreamWriter(nuevaEntrada.Open());
        escritor.Write(sinValidaciones);
    }
}
