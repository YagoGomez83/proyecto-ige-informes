using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.DataMigration;
using IGE.Informes.DataMigration.Consolidacion;
using IGE.Informes.DataMigration.Excel;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

if (args.Length > 0 && string.Equals(args[0], "camaras", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3 || !File.Exists(args[1]))
    {
        Console.WriteLine("Uso: dotnet run -- camaras <ruta-al-excel.xlsx> <connection-string> [--confirmar]");
        return 1;
    }

    return await ProgramCamaras.Ejecutar(args[1], args[2], args.Contains("--confirmar"));
}

if (args.Length > 0 && string.Equals(args[0], "tipos-incidente", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.WriteLine("Uso: dotnet run -- tipos-incidente <connection-string> [--confirmar]");
        return 1;
    }

    return await ProgramTiposIncidente.Ejecutar(args[1], args.Contains("--confirmar"));
}

if (args.Length > 0 && string.Equals(args[0], "tipos-causa", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.WriteLine("Uso: dotnet run -- tipos-causa <connection-string> [--confirmar]");
        return 1;
    }

    return await ProgramTiposCausa.Ejecutar(args[1], args.Contains("--confirmar"));
}

if (args.Length > 0 && string.Equals(args[0], "limpiar-relatos", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.WriteLine("Uso: dotnet run -- limpiar-relatos <connection-string> [--confirmar]");
        return 1;
    }

    return await ProgramLimpiarRelatos.Ejecutar(args[1], args.Contains("--confirmar"));
}

if (args.Length == 0 || !File.Exists(args[0]))
{
    Console.WriteLine("Uso: dotnet run -- <ruta-al-excel.xlsx> <connection-string> [--confirmar]   (migración de Vehículos)");
    Console.WriteLine("     dotnet run -- camaras <ruta-al-excel.xlsx> <connection-string> [--confirmar]   (migración de Cámaras)");
    Console.WriteLine("     dotnet run -- tipos-incidente <connection-string> [--confirmar]   (migración de Tipos de Incidente)");
    Console.WriteLine("     dotnet run -- tipos-causa <connection-string> [--confirmar]   (migración de Tipos de Causa)");
    Console.WriteLine("     dotnet run -- limpiar-relatos <connection-string> [--confirmar]   (limpieza de Relatos con ruido de paginación)");
    Console.WriteLine("Sin --confirmar, corre en modo dry-run: solo muestra el reporte, no persiste nada.");
    return 1;
}

var rutaExcel = args[0];
var connectionString = args.Length > 1 ? args[1] : null;
var confirmar = args.Contains("--confirmar");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Falta la connection string como segundo argumento.");
    return 1;
}

const string accionARealizarDefault = "Identificar";
const string avisarADefault = "Sin dato migración";

Console.WriteLine($"Leyendo '{rutaExcel}'...");
var filas = LectorExcelVehiculos.LeerTodas(rutaExcel);
Console.WriteLine($"  {filas.Count} filas con datos leídas de {HojasVehiculos.Todas.Count} hojas.");

var consolidadosLeidos = ConsolidadorVehiculos.Consolidar(filas);
Console.WriteLine($"  Consolidados en {consolidadosLeidos.Count} Vehiculos únicos.");

// El Excel de origen es una exportación recurrente de la misma planilla
// maestra (re-descargada periódicamente) — no un lote nuevo aislado. Sin
// este filtro, cada corrida re-insertaría como Vehiculo nuevo cualquier
// Dominio ya migrado antes, generando duplicados en cada re-importación
// (confirmado 2026-08-05: el archivo trae 1080+59+687+237 filas crudas,
// muchas más que los Vehiculos ya cargados en Fase 2). Se compara por
// Dominio normalizado — el mismo Dominio aparece con espacios/guiones
// distintos entre hojas y entre corridas.
static string NormalizarDominio(string dominio) =>
    dominio.ToUpperInvariant().Replace(" ", "").Replace("-", "");

var currentUserServiceTemp = new UsuarioMigracionService();
var optionsLectura = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .Options;

HashSet<string> dominiosExistentes;
await using (var dbContextLectura = new AppDbContext(optionsLectura))
{
    dominiosExistentes = (await dbContextLectura.Vehiculos
            .Where(v => v.Dominio != null)
            .Select(v => v.Dominio!)
            .ToListAsync())
        .Select(NormalizarDominio)
        .ToHashSet();
}

Console.WriteLine($"  {dominiosExistentes.Count} Dominios ya existentes en la base.");

var yaExistentes = consolidadosLeidos
    .Where(v => v.Dominio is not null && dominiosExistentes.Contains(NormalizarDominio(v.Dominio)))
    .ToList();
var consolidados = consolidadosLeidos
    .Where(v => v.Dominio is null || !dominiosExistentes.Contains(NormalizarDominio(v.Dominio)))
    .ToList();

var conDominio = consolidados.Count(v => v.Dominio is not null);
var sinDominio = consolidados.Count - conDominio;
var identificados = consolidados.Count(v => v.Estado == EstadoVehiculo.Identificado);
var categoriasUsadas = consolidados.SelectMany(v => v.CategoriasAlerta).Distinct().OrderBy(c => c).ToList();

Console.WriteLine();
Console.WriteLine("=== Reporte de migración ===");
Console.WriteLine($"Vehículos ya existentes (omitidos): {yaExistentes.Count}");
Console.WriteLine($"Vehículos nuevos a insertar:         {consolidados.Count}");
Console.WriteLine($"  Con Dominio:      {conDominio}");
Console.WriteLine($"  Sin Dominio:      {sinDominio}");
Console.WriteLine($"  Marcados Identificado:      {identificados}");
Console.WriteLine($"Categorías de alerta usadas: {string.Join(", ", categoriasUsadas)}");
Console.WriteLine($"AccionARealizar/AvisarA por defecto: {accionARealizarDefault} / \"{avisarADefault}\"");
Console.WriteLine();
Console.WriteLine("Ejemplos de Vehículos NUEVOS a insertar (primeros 5):");
foreach (var ejemplo in consolidados.Take(5))
{
    Console.WriteLine($"  - {ejemplo.Dominio ?? "(sin dominio)"} | {ejemplo.Marca} {ejemplo.Modelo} | {ejemplo.Color} | Estado={ejemplo.Estado} | Categorías=[{string.Join(",", ejemplo.CategoriasAlerta)}]");
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

Console.WriteLine("Resolviendo/creando CategoriaAlerta necesarias...");
var categoriasPorNombre = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
foreach (var nombreCategoria in categoriasUsadas)
{
    var existente = await dbContext.CategoriasAlerta.FirstOrDefaultAsync(c => c.Nombre == nombreCategoria);
    if (existente is not null)
    {
        categoriasPorNombre[nombreCategoria] = existente.Id;
        continue;
    }

    var nueva = new CategoriaAlerta(nombreCategoria);
    dbContext.CategoriasAlerta.Add(nueva);
    categoriasPorNombre[nombreCategoria] = nueva.Id;
}

await dbContext.SaveChangesAsync();

const int maxCaracteristicas = 2000;
const int maxDominio = 20;
const int maxMarcaModelo = 100;
const int maxColor = 50;
var truncados = 0;

Console.WriteLine($"Insertando {consolidados.Count} Vehiculos...");
foreach (var consolidado in consolidados)
{
    var caracteristicas = consolidado.Caracteristicas;
    if (caracteristicas is not null && caracteristicas.Length > maxCaracteristicas)
    {
        caracteristicas = caracteristicas[..(maxCaracteristicas - 15)] + " [...truncado]";
        truncados++;
    }

    // Si el Dominio real no entra en la columna (dato de otra naturaleza
    // colado en esa celda del Excel — ej. un número de expediente), se
    // guarda completo en Caracteristicas y el Dominio queda null en vez
    // de truncarlo (truncar una patente la haría un dato incorrecto).
    var dominio = consolidado.Dominio;
    if (dominio is not null && dominio.Length > maxDominio)
    {
        caracteristicas = $"[Migración] Dominio original (no entraba en el campo): {dominio} | {caracteristicas}";
        if (caracteristicas.Length > maxCaracteristicas)
        {
            caracteristicas = caracteristicas[..(maxCaracteristicas - 15)] + " [...truncado]";
        }

        dominio = null;
        truncados++;
    }

    var marca = TruncarSiExcede(consolidado.Marca, maxMarcaModelo);
    var modelo = TruncarSiExcede(consolidado.Modelo, maxMarcaModelo);
    var color = TruncarSiExcede(consolidado.Color, maxColor);

    var vehiculo = new Vehiculo(
        marca,
        modelo,
        color,
        CertezaDominio.Incierto,
        AccionARealizar.Identificar,
        avisarADefault,
        dominio,
        caracteristicas);

    if (consolidado.Estado == EstadoVehiculo.Identificado)
    {
        vehiculo.MarcarIdentificado();
    }

    foreach (var nombreCategoria in consolidado.CategoriasAlerta)
    {
        vehiculo.AsignarCategoriaAlerta(categoriasPorNombre[nombreCategoria]);
    }

    dbContext.Vehiculos.Add(vehiculo);
}

await dbContext.SaveChangesAsync();

if (truncados > 0)
{
    Console.WriteLine($"Advertencia: {truncados} Vehiculo(s) tenían Caracteristicas más largas que 2000 caracteres y se truncaron.");
}

Console.WriteLine("Migración completada.");
return 0;

static string TruncarSiExcede(string valor, int maxLength) =>
    valor.Length > maxLength ? valor[..maxLength] : valor;
