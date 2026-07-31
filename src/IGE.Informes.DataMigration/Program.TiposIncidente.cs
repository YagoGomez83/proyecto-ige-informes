using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.DataMigration;

/// <summary>
/// Migración de los códigos de incidente históricos (relevados de
/// docs/docuemntación-legacy/codigos.csv, exportado del sistema de despacho
/// de llamadas). Un TipoIncidente por código numérico de 3 dígitos (no por
/// cada subtipo de texto libre) — mismo criterio que ya usa el resto del
/// sistema para agrupar/analizar Casos. Invocar con el subcomando
/// "tipos-incidente" (ver Program.cs principal).
/// </summary>
public static class ProgramTiposIncidente
{
    // El código "107" (línea de emergencias médicas) no sigue el patrón
    // "CÓDIGO - CATEGORÍA - Subtipo" del resto del Excel de forma consistente
    // (subtipos como "Pedido de Ambulancia" o "Comunicacion interna" sin una
    // categoría común) — se carga como un único código con descripción
    // genérica, decisión validada explícitamente en vez de inventar una
    // categoría de las filas irregulares.
    private static readonly (string Codigo, string Descripcion)[] Catalogo =
    [
        ("000", "RECORRIDO POR JURISDICCION"),
        ("001", "ARTEFACTO EXPLOSIVO"),
        ("002", "ASALTO A MANO ARMADA"),
        ("003", "ATENTADO EXTREMISTA"),
        ("004", "PIRATA DEL ASFALTO"),
        ("005", "POLICIA HERIDO"),
        ("006", "ACCIDENTE CON HERIDOS"),
        ("007", "ACCIDENTE SIN HERIDOS"),
        ("008", "ALLANAMIENTO"),
        ("009", "MÓVIL NECESITA AYUDA"),
        ("010", "TIROTEO"),
        ("011", "SOLICITUD DE REFUERZO"),
        ("012", "PERSONA DEMORADA"),
        ("013", "VEHICULO ABANDONADO / HALLAZGO"),
        ("014", "MANIFESTACIÓN"),
        ("015", "PERSONA HERIDA"),
        ("016", "ESTUPEFACIENTES"),
        ("017", "OBITO"),
        ("018", "CONSIGNA POLICIAL"),
        ("019", "CONSUMO DE ALCOHOL"),
        ("020", "HALLAZGOS HUMANOS"),
        ("021", "EXHIBICION OBSENAS"),
        ("022", "DEMENTE"),
        ("024", "BULTO SOSPECHOSO"),
        ("025", "PERSONA SOSPECHOSA"),
        ("026", "DISTURBIOS"),
        ("028", "RESCATE"),
        ("029", "SUICIDIO"),
        ("079", "HOMICIDIO"),
        ("096", "CONTRAVENCION"),
        ("102", "NNyA"),
        ("104", "SERVICIO 104"),
        ("106", "ABANDONO DE PERSONA"),
        ("107", "SERVICIO DE EMERGENCIAS MEDICAS (C.E.)"),
        ("119", "DELITO CONTRA LA INTEGRIDAD SEXUAL"),
        ("130", "RAPTO"),
        ("149", "AMENAZA"),
        ("150", "VIOLACIÓN DE DOMICILIO"),
        ("162", "HURTO"),
        ("163", "ABIGEATO"),
        ("164", "ROBO"),
        ("183", "DAÑOS"),
        ("186", "INCENDIO"),
        ("190", "DESASTRE AÉREO"),
        ("191", "DESCARRILAMIENTO DE TREN"),
        ("212", "TRABAJO INFANTIL"),
        ("232", "TUMULTO"),
        ("237", "ATENTADO"),
        ("280", "EVASIÓN"),
        ("281", "VIOLENCIA DE GÉNERO"),
        ("282", "LLAMADA DE BROMA"),
        ("284", "ANIMALES EN VÍA PÚBLICA"),
        ("285", "SERVICIOS PÚBLICOS"),
        ("287", "PROBLEMAS FAMILIARES"),
        ("288", "PARADERO"),
        ("289", "ARREBATO"),
        ("290", "HALLAZGO DE ELEMENTOS"),
        ("291", "USURPACION"),
        ("292", "PROBLEMAS ENTRE VECINOS"),
        ("293", "EMERGENCIA NAUTICA"),
        ("294", "FUGA DE GAS"),
        ("295", "FUGA DE DETENIDOS"),
        ("296", "EMERGENCIA VIAL"),
        ("297", "GROOMING"),
        ("298", "CONSTE"),
        ("299", "SECUESTRO VEHICULAR"),
        ("300", "BOTÓN DE PÁNICO"),
        ("301", "OFICIAL DE JUSTICIA"),
        ("302", "PERSONA EN SITUACION DE VULNERABILIDAD"),
        ("303", "PERSONA DESORIENTADA O CONFUSA"),
        ("304", "BULLYING"),
        ("305", "SAN LUIS SOLIDARIO"),
        ("306", "ESTAFA"),
        ("307", "BIODIVERSIDAD"),
        ("309", "TOBILLERAS DUALES"),
        ("400", "TENTATIVA"),
        ("555", "COMUNICACIÓN INTERNA POLICIA"),
        ("600", "SISTEMA AUTOMATICO DE ALARMA"),
        ("601", "LLAMADA"),
        ("700", "INCUMPLIMIENTO PROTOCOLO DE TRANSPORTE"),
        ("701", "COVID-19"),
    ];

    public static async Task<int> Ejecutar(string connectionString, bool confirmar)
    {
        Console.WriteLine($"Códigos en el catálogo a migrar: {Catalogo.Length}");
        Console.WriteLine();
        Console.WriteLine("Ejemplos (primeros 5):");
        foreach (var (codigo, descripcion) in Catalogo.Take(5))
        {
            Console.WriteLine($"  - {codigo} | {descripcion}");
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

        var codigosExistentes = await dbContext.TiposIncidente
            .Select(t => t.Codigo)
            .ToListAsync();
        var existentesSet = new HashSet<string>(codigosExistentes, StringComparer.OrdinalIgnoreCase);

        var insertados = 0;
        var omitidos = 0;
        foreach (var (codigo, descripcion) in Catalogo)
        {
            if (existentesSet.Contains(codigo))
            {
                omitidos++;
                continue;
            }

            dbContext.TiposIncidente.Add(new TipoIncidente(codigo, descripcion));
            insertados++;
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine($"Migración completada: {insertados} Tipos de Incidente nuevos, {omitidos} ya existentes (omitidos).");
        return 0;
    }
}
