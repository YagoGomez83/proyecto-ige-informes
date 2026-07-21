using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.DataMigration.Consolidacion;
using IGE.Informes.DataMigration.Excel;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

if (args.Length == 0 || !File.Exists(args[0]))
{
    Console.WriteLine("Uso: dotnet run -- <ruta-al-excel.xlsx> <connection-string> [--confirmar]");
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

var consolidados = ConsolidadorVehiculos.Consolidar(filas);
Console.WriteLine($"  Consolidados en {consolidados.Count} Vehiculos únicos.");

var conDominio = consolidados.Count(v => v.Dominio is not null);
var sinDominio = consolidados.Count - conDominio;
var identificados = consolidados.Count(v => v.Estado == EstadoVehiculo.Identificado);
var categoriasUsadas = consolidados.SelectMany(v => v.CategoriasAlerta).Distinct().OrderBy(c => c).ToList();

Console.WriteLine();
Console.WriteLine("=== Reporte de migración ===");
Console.WriteLine($"Vehículos con Dominio:      {conDominio}");
Console.WriteLine($"Vehículos sin Dominio:      {sinDominio}");
Console.WriteLine($"Marcados Identificado:      {identificados}");
Console.WriteLine($"Categorías de alerta usadas: {string.Join(", ", categoriasUsadas)}");
Console.WriteLine($"AccionARealizar/AvisarA por defecto: {accionARealizarDefault} / \"{avisarADefault}\"");
Console.WriteLine();
Console.WriteLine("Ejemplos (primeros 5):");
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

sealed class UsuarioMigracionService : ICurrentUserService
{
    // Guid reservado y documentado como "usuario de sistema / migración
    // histórica" — nunca corresponde a una cuenta real de Identity.
    public static readonly Guid UsuarioMigracionId = new("00000000-0000-0000-0000-000000000001");

    public Guid? UsuarioId => UsuarioMigracionId;
    public IReadOnlyCollection<string> Roles => ["Admin"];
}
