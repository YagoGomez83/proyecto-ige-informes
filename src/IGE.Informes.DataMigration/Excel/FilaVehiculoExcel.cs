namespace IGE.Informes.DataMigration.Excel;

/// <summary>
/// Fila cruda de una de las 4 hojas del Excel con datos reales
/// (VEHICULOS SL, MOTOCICLETAS, Vehículos Vigentes, Robo de Cubiertas —
/// las demás hojas del archivo solo tienen encabezados, sin filas, ver
/// memoria del proyecto). MarcaModelo viene combinado en una sola columna
/// del Excel, sin separación real entre marca y modelo.
/// </summary>
public sealed record FilaVehiculoExcel(
    string HojaOrigen,
    string? Dominio,
    string? MarcaModelo,
    string? Color,
    string? MotivoOCategoria,
    string? EstadoOriginal,
    string? Observacion,
    string? Procedimiento);
