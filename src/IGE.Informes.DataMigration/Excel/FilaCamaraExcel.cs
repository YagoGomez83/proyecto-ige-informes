namespace IGE.Informes.DataMigration.Excel;

/// <summary>
/// Fila cruda de docs/camaras.xlsx (hoja única, columnas ID, Ubicacion,
/// Localidad, Monitoreo, Unidad Regional, Jurisdiccion — ver
/// docs/03-modelo-dominio.md, sección de migración).
/// </summary>
public sealed record FilaCamaraExcel(
    string Codigo,
    string? Ubicacion,
    string? Localidad,
    string? Monitoreo,
    string? UnidadRegional,
    string? Jurisdiccion);
