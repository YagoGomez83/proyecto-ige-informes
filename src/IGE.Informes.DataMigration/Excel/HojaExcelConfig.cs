namespace IGE.Informes.DataMigration.Excel;

/// <summary>
/// Mapeo de columnas por letra para cada una de las 4 hojas del Excel que
/// tienen filas de datos reales — confirmado por inspección directa del
/// archivo (ver diagnóstico en memoria del proyecto). Las columnas no
/// tienen el mismo layout entre hojas ni encabezados consistentes, así que
/// se direccionan por letra en vez de por nombre.
/// </summary>
public sealed record HojaExcelConfig(
    string NombreHoja,
    char ColDominio,
    char ColMarcaModelo,
    char ColColor,
    char? ColMotivoOCategoria,
    char? ColEstado,
    char? ColObservacion,
    char? ColProcedimiento);

public static class HojasVehiculos
{
    public static readonly IReadOnlyCollection<HojaExcelConfig> Todas =
    [
        new("VEHICULOS SL", ColDominio: 'D', ColMarcaModelo: 'B', ColColor: 'C',
            ColMotivoOCategoria: 'F', ColEstado: 'E', ColObservacion: 'K', ColProcedimiento: 'M'),
        new("MOTOCICLETAS", ColDominio: 'C', ColMarcaModelo: 'B', ColColor: 'D',
            ColMotivoOCategoria: null, ColEstado: 'H', ColObservacion: 'I', ColProcedimiento: 'J'),
        new("Vehículos Vigentes", ColDominio: 'C', ColMarcaModelo: 'B', ColColor: 'D',
            ColMotivoOCategoria: null, ColEstado: 'H', ColObservacion: 'J', ColProcedimiento: 'K'),
        new("Robo de Cubiertas", ColDominio: 'C', ColMarcaModelo: 'B', ColColor: 'D',
            ColMotivoOCategoria: null, ColEstado: 'H', ColObservacion: 'I', ColProcedimiento: 'J'),
        // Todas las demás hojas del archivo (Vehiculo Robados, VEHICULOS
        // VM, Vehículos con Inhibidores, Vehículo Inv. Robo de cubiertas,
        // Vehículos involucrados en Narco, Pedidos especiales, Hoja 7/8/9)
        // tienen solo encabezados, sin ninguna fila de datos — confirmado
        // por inspección directa, se excluyen de la migración.
    ];

    /// <summary>
    /// Motivo/categoría (columna F de VEHICULOS SL, texto libre) → nombre
    /// de CategoriaAlerta del dominio. Solo estos 4 valores tienen
    /// equivalente — el resto (ESTAFA, ROBOS, MELLIZOS, PEDIDO SECUESTRO,
    /// ABUSO DE ARMA DE FUEGO, AMENAZAS) no mapea a ninguna categoría y
    /// queda solo como nota en Caracteristicas (decisión confirmada).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> MotivoACategoriaAlerta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ROBADO"] = "Robado",
        ["NARCOTRAFICO"] = "Narcotrafico",
        ["INHIBIDORES"] = "Inhibidores",
        ["ROBO DE CUBIERTAS"] = "RoboCubiertas",
    };
}
