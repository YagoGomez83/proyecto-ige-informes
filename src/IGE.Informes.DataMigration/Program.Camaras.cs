using IGE.Informes.DataMigration.Consolidacion;
using IGE.Informes.DataMigration.Excel;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.DataMigration;

/// <summary>
/// Migración de docs/camaras.xlsx (1015 filas, hoja única) — Localidad,
/// CentroControlCamaras, Dependencia (Jurisdiccion=Comisaria + Unidad
/// Regional) y Camara. Invocar con el subcomando "camaras" (ver Program.cs
/// principal). No toca Vehiculos/Personas — esa migración es independiente
/// (ver Program.cs).
/// </summary>
public static class ProgramCamaras
{
    public static async Task<int> Ejecutar(string rutaExcel, string connectionString, bool confirmar)
    {
        Console.WriteLine($"Leyendo '{rutaExcel}'...");
        var filas = LectorExcelCamaras.LeerTodas(rutaExcel);
        Console.WriteLine($"  {filas.Count} filas leídas.");

        var localidades = filas.Select(f => NormalizadorCamaras.NormalizarTexto(f.Localidad))
            .Where(v => v is not null).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v).ToList();

        var siglasMonitoreo = filas.Select(f => NormalizadorCamaras.NormalizarTexto(f.Monitoreo))
            .Where(v => v is not null).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v).ToList();

        var unidadesRegionales = filas.Select(f => NormalizadorCamaras.NormalizarUnidadRegional(f.UnidadRegional))
            .Where(v => v is not null).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v).ToList();

        var jurisdicciones = filas.Select(f => NormalizadorCamaras.NormalizarTexto(f.Jurisdiccion))
            .Where(v => v is not null).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v).ToList();

        var siglaSinMapeo = siglasMonitoreo.Where(s => !NormalizadorCamaras.SiglaANombreCentroControl.ContainsKey(s)).ToList();

        Console.WriteLine();
        Console.WriteLine("=== Reporte de migración de Cámaras ===");
        Console.WriteLine($"Localidades distintas:          {localidades.Count}");
        Console.WriteLine($"Centros de Control (Monitoreo): {siglasMonitoreo.Count} ({string.Join(", ", siglasMonitoreo)})");
        Console.WriteLine($"Unidades Regionales distintas:  {unidadesRegionales.Count} ({string.Join(", ", unidadesRegionales)})");
        Console.WriteLine($"Jurisdicciones (Comisarías):    {jurisdicciones.Count}");
        Console.WriteLine();

        if (siglaSinMapeo.Count > 0)
        {
            Console.WriteLine($"ERROR: sigla(s) de Monitoreo sin nombre conocido: {string.Join(", ", siglaSinMapeo)}");
            Console.WriteLine("Agregar el mapeo en NormalizadorCamaras.SiglaANombreCentroControl antes de migrar.");
            return 1;
        }

        Console.WriteLine("Ejemplos (primeras 5 filas):");
        foreach (var ejemplo in filas.Take(5))
        {
            Console.WriteLine($"  - {ejemplo.Codigo} | {ejemplo.Ubicacion} | Localidad={ejemplo.Localidad} | " +
                $"Monitoreo={ejemplo.Monitoreo} | UR={NormalizadorCamaras.NormalizarUnidadRegional(ejemplo.UnidadRegional)} | " +
                $"Jurisdiccion={NormalizadorCamaras.NormalizarTexto(ejemplo.Jurisdiccion)}");
        }

        Console.WriteLine();

        if (!confirmar)
        {
            Console.WriteLine("Modo dry-run (no se persistió nada). Ejecutar con --confirmar para migrar de verdad.");
            return 0;
        }

        var currentUserService = new UsuarioMigracionService();
        var interceptor = new AuditLogInterceptor(currentUserService);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new AppDbContext(options);

        Console.WriteLine("Resolviendo/creando Localidades...");
        var localidadPorNombre = await ResolverOCrear(
            dbContext.Localidades, localidades, nombre => new Localidad(nombre), l => l.Nombre, l => l.Id);
        await dbContext.SaveChangesAsync();

        Console.WriteLine("Resolviendo/creando Centros de Control de Cámaras...");
        var centroPorSigla = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var sigla in siglasMonitoreo)
        {
            var existente = await dbContext.CentrosControlCamaras.FirstOrDefaultAsync(c => c.Sigla == sigla);
            if (existente is not null)
            {
                centroPorSigla[sigla] = existente.Id;
                continue;
            }

            var nuevo = new CentroControlCamaras(sigla, NormalizadorCamaras.SiglaANombreCentroControl[sigla]);
            dbContext.CentrosControlCamaras.Add(nuevo);
            centroPorSigla[sigla] = nuevo.Id;
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine("Resolviendo/creando Unidades Regionales (Dependencia.Tipo=UnidadRegional)...");
        var urPorNombre = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var nombreUr in unidadesRegionales)
        {
            var existente = await dbContext.Dependencias
                .FirstOrDefaultAsync(d => d.Nombre == nombreUr && d.Tipo == TipoDependencia.UnidadRegional);
            if (existente is not null)
            {
                urPorNombre[nombreUr] = existente.Id;
                continue;
            }

            var nueva = new Dependencia(nombreUr, TipoDependencia.UnidadRegional);
            dbContext.Dependencias.Add(nueva);
            urPorNombre[nombreUr] = nueva.Id;
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine("Resolviendo/creando Jurisdicciones (Dependencia.Tipo=Comisaria)...");
        var jurisdiccionPorNombre = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var fila in filas)
        {
            var nombreJurisdiccion = NormalizadorCamaras.NormalizarTexto(fila.Jurisdiccion);
            if (nombreJurisdiccion is null || jurisdiccionPorNombre.ContainsKey(nombreJurisdiccion))
            {
                continue;
            }

            var existente = await dbContext.Dependencias
                .FirstOrDefaultAsync(d => d.Nombre == nombreJurisdiccion && d.Tipo == TipoDependencia.Comisaria);
            Dependencia dependencia;
            if (existente is not null)
            {
                dependencia = existente;
            }
            else
            {
                dependencia = new Dependencia(nombreJurisdiccion, TipoDependencia.Comisaria);
                dbContext.Dependencias.Add(dependencia);
            }

            var nombreUr = NormalizadorCamaras.NormalizarUnidadRegional(fila.UnidadRegional);
            if (nombreUr is not null && dependencia.UnidadRegionalId is null)
            {
                dependencia.AsignarUnidadRegional(urPorNombre[nombreUr]);
            }

            jurisdiccionPorNombre[nombreJurisdiccion] = dependencia.Id;
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine($"Insertando {filas.Count} Camaras...");
        foreach (var fila in filas)
        {
            var camara = new Camara(fila.Codigo, TipoCamara.Domo, fila.Ubicacion);

            var nombreLocalidad = NormalizadorCamaras.NormalizarTexto(fila.Localidad);
            if (nombreLocalidad is not null)
            {
                camara.AsignarLocalidad(localidadPorNombre[nombreLocalidad]);
            }

            var siglaMonitoreo = NormalizadorCamaras.NormalizarTexto(fila.Monitoreo);
            if (siglaMonitoreo is not null)
            {
                camara.AsignarCentroControlCamaras(centroPorSigla[siglaMonitoreo]);
            }

            var nombreJurisdiccion = NormalizadorCamaras.NormalizarTexto(fila.Jurisdiccion);
            if (nombreJurisdiccion is not null)
            {
                camara.AsignarDependencia(jurisdiccionPorNombre[nombreJurisdiccion]);
            }

            dbContext.Camaras.Add(camara);
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine("Migración de Cámaras completada.");
        return 0;
    }

    private static async Task<Dictionary<string, Guid>> ResolverOCrear<TEntidad>(
        DbSet<TEntidad> dbSet,
        IReadOnlyCollection<string> nombres,
        Func<string, TEntidad> crear,
        Func<TEntidad, string> obtenerNombre,
        Func<TEntidad, Guid> obtenerId)
        where TEntidad : class
    {
        var existentes = await dbSet.ToListAsync();
        var resultado = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var nombre in nombres)
        {
            var existente = existentes.FirstOrDefault(e =>
                string.Equals(obtenerNombre(e), nombre, StringComparison.OrdinalIgnoreCase));

            if (existente is not null)
            {
                resultado[nombre] = obtenerId(existente);
                continue;
            }

            var nuevo = crear(nombre);
            dbSet.Add(nuevo);
            resultado[nombre] = obtenerId(nuevo);
        }

        return resultado;
    }
}
