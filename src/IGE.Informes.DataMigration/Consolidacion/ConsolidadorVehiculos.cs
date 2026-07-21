using IGE.Informes.DataMigration.Excel;
using IGE.Informes.Domain.Entities;

namespace IGE.Informes.DataMigration.Consolidacion;

/// <summary>
/// Fusiona las filas crudas del Excel en Vehiculos consolidados por
/// Dominio. Reglas de negocio confirmadas explícitamente (no inventadas):
/// - Se agrupa por Dominio normalizado (mayúsculas, sin espacios/guiones)
///   — el Dominio persistido es el de la primera aparición, sin corregir
///   formato (nunca validar patente, ver docs/03-modelo-dominio.md).
/// - MarcaModelo se separa en Marca (primera palabra) / Modelo (resto) —
///   heurística porque el Excel no distingue las dos columnas.
/// - Color: de la primera fila del grupo; discrepancias con otras filas
///   del mismo Dominio se anotan en Caracteristicas, no se descartan.
/// - CategoriaAlerta: la hoja "Robo de Cubiertas" implica RoboCubiertas
///   para todas sus filas. En VEHICULOS SL, el motivo de la columna F
///   mapea a categoría solo si coincide con
///   HojasVehiculos.MotivoACategoriaAlerta (Robado/Narcotrafico/
///   Inhibidores/RoboCubiertas) — el resto de motivos (Estafa, Robos,
///   Mellizos, etc.) no tiene categoría equivalente y queda solo como
///   nota en Caracteristicas.
/// - Estado: en "Vehículos Vigentes", solo "Identificado" mapea a
///   EstadoVehiculo.Identificado; los otros 5 valores reales (Vigente,
///   Retirado, Detenido, Secuestrado, Sin Efecto) quedan como Vigente con
///   el valor original anotado en Caracteristicas. Sin dato → default
///   Vigente.
/// - AccionARealizar/AvisarA: no hay columna confiable con este dato en
///   las 4 hojas con datos reales — se aplica el default de negocio
///   directamente al construir el Vehiculo (ver Program.cs), no en este
///   consolidador.
/// - Filas sin Dominio: no se agrupan con nada — cada una es su propio
///   Vehiculo consolidado (no hay clave confiable para fusionar).
/// </summary>
public static class ConsolidadorVehiculos
{
    public static IReadOnlyCollection<VehiculoConsolidado> Consolidar(IReadOnlyCollection<FilaVehiculoExcel> filas)
    {
        var resultado = new List<VehiculoConsolidado>();

        var conDominio = filas.Where(f => f.Dominio is not null);
        var sinDominio = filas.Where(f => f.Dominio is null);

        var grupos = conDominio.GroupBy(f => NormalizarDominio(f.Dominio!));

        foreach (var grupo in grupos)
        {
            resultado.Add(ConsolidarGrupo(grupo.ToList()));
        }

        foreach (var fila in sinDominio)
        {
            resultado.Add(ConsolidarGrupo([fila]));
        }

        return resultado;
    }

    private static string NormalizarDominio(string dominio) =>
        dominio.ToUpperInvariant().Replace(" ", "").Replace("-", "");

    private static VehiculoConsolidado ConsolidarGrupo(IReadOnlyList<FilaVehiculoExcel> filasDelGrupo)
    {
        var primera = filasDelGrupo[0];
        var (marca, modelo) = SepararMarcaModelo(primera.MarcaModelo);
        var color = primera.Color ?? "Sin dato";

        var notas = new List<string>();

        foreach (var fila in filasDelGrupo.Skip(1))
        {
            var (marcaFila, modeloFila) = SepararMarcaModelo(fila.MarcaModelo);
            AnotarSiDiscrepa("Marca", marca, marcaFila, notas);
            AnotarSiDiscrepa("Modelo", modelo, modeloFila, notas);
            AnotarSiDiscrepa("Color", color, fila.Color, notas);
        }

        var categorias = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var fila in filasDelGrupo)
        {
            if (fila.HojaOrigen == "Robo de Cubiertas")
            {
                categorias.Add("RoboCubiertas");
            }

            if (fila.MotivoOCategoria is not null
                && HojasVehiculos.MotivoACategoriaAlerta.TryGetValue(fila.MotivoOCategoria, out var categoria))
            {
                categorias.Add(categoria);
            }
            else if (fila.MotivoOCategoria is not null)
            {
                notas.Add($"[Migración] Motivo original sin categoría equivalente: {fila.MotivoOCategoria}");
            }
        }

        var estado = EstadoVehiculo.Vigente;
        var filaVigentes = filasDelGrupo.FirstOrDefault(f => f.HojaOrigen == "Vehículos Vigentes");
        if (filaVigentes?.EstadoOriginal is not null)
        {
            if (string.Equals(filaVigentes.EstadoOriginal, "Identificado", StringComparison.OrdinalIgnoreCase))
            {
                estado = EstadoVehiculo.Identificado;
            }
            else if (!string.Equals(filaVigentes.EstadoOriginal, "Vigente", StringComparison.OrdinalIgnoreCase))
            {
                notas.Add($"[Migración] Estado original en Vehículos Vigentes: {filaVigentes.EstadoOriginal}");
            }
        }

        foreach (var observacion in filasDelGrupo.Select(f => f.Observacion).Where(o => o is not null).Distinct())
        {
            notas.Add(observacion!);
        }

        var hojasOrigen = filasDelGrupo.Select(f => f.HojaOrigen).Distinct().ToList();
        notas.Add($"[Migración] Origen: {string.Join(", ", hojasOrigen)}");

        return new VehiculoConsolidado(
            primera.Dominio,
            marca,
            modelo,
            color,
            string.Join(" | ", notas),
            estado,
            categorias.ToList(),
            hojasOrigen);
    }

    private static (string Marca, string Modelo) SepararMarcaModelo(string? marcaModelo)
    {
        if (string.IsNullOrWhiteSpace(marcaModelo))
        {
            return ("Sin dato", "Sin dato");
        }

        var partes = marcaModelo.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return partes.Length == 2 ? (partes[0], partes[1]) : (partes[0], "Sin dato");
    }

    private static void AnotarSiDiscrepa(string campo, string valorActual, string? valorNuevo, List<string> notas)
    {
        if (valorNuevo is not null && !string.Equals(valorActual, valorNuevo, StringComparison.OrdinalIgnoreCase))
        {
            notas.Add($"[Migración] Discrepancia en {campo}: '{valorNuevo}' en otra hoja (se usó '{valorActual}')");
        }
    }
}
