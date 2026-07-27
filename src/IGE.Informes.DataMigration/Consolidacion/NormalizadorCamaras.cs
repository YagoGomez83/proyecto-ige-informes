using System.Text.RegularExpressions;

namespace IGE.Informes.DataMigration.Consolidacion;

/// <summary>
/// Correcciones de formato confirmadas por inspección directa de
/// docs/camaras.xlsx (ver docs/03-modelo-dominio.md, sección de
/// migración) — no reglas de negocio nuevas, solo errores de tipeo del
/// origen: "UR1" sin espacio (una sola fila, el resto usa "UR 1"…"UR 6")
/// y espacios dobles en nombres de Jurisdicción.
/// </summary>
public static class NormalizadorCamaras
{
    public static string? NormalizarUnidadRegional(string? valor)
    {
        if (valor is null)
        {
            return null;
        }

        var compactado = Regex.Replace(valor.Trim(), @"\s+", " ");
        return Regex.Replace(compactado, @"^UR(\d)$", "UR $1");
    }

    public static string? NormalizarTexto(string? valor) =>
        valor is null ? null : Regex.Replace(valor.Trim(), @"\s+", " ");

    /// <summary>
    /// Sigla de Monitoreo (columna del Excel) → nombre completo del Centro
    /// de Control de Cámaras, confirmado por el usuario (no inventado).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> SiglaANombreCentroControl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["CCCSL"] = "Centro de Control de Cámaras San Luis",
        ["CCCVM"] = "Centro de Control de Cámaras Villa Mercedes",
        ["CCCME"] = "Centro de Control de Cámaras Merlo",
        ["CCCJD"] = "Centro de Control de Cámaras Justo Daract",
    };
}
