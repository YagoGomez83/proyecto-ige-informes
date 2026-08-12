using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.DataMigration;

/// <summary>
/// Migración del catálogo histórico de carátulas de Causa (relevado de
/// docs/docuemntación-legacy/causes.csv — a pesar del nombre del archivo,
/// no es el catálogo de TipoIncidente, ver
/// docs/08-plan-implementacion.md "Deuda registrada" y
/// docs/03-modelo-dominio.md "Decisiones ya resueltas"). 82 filas activas
/// (excluidas 3 soft-deleted por deleted_at en el CSV origen), texto
/// normalizado (comillas tipográficas curly reemplazadas por rectas,
/// espacios finales recortados). Invocar con el subcomando "tipos-causa"
/// (ver Program.cs principal).
/// </summary>
public static class ProgramTiposCausa
{
    private static readonly string[] Catalogo =
    [
        "AV. DAÑOS A BIENES DEL ESTADO PROVINCIAL - DAÑOS",
        "AV. ROBO",
        "TAREA INVESTIGATIVA",
        "AV. ROBO CALIFICADO EN POBLADO Y EN BANDA POR EL USO DE ARMA DE FUEGO",
        "AV. TENENCIA ILEGAL DE ARMA DE FUEGO DE USO CIVIL",
        "AV. HURTO",
        "AV. HURTO CALIFICADO",
        "AV. ARREBATO",
        "AV. TENTATIVA DE HOMICIDIO",
        "AV. SOLICITUD DE PARADERO",
        "AV. ROBO CALIFICADO POR ESCALAMIENTO",
        "AV. ESTAFA",
        "AV. ROBO CALIFICADO",
        "AV. HURTO CALIFICADO DE VEHÍCULO",
        "AV. HURTO- ROBO EN CONSURSO REAL",
        "AV. LESIONES - AMENAZAS - RESISTENCIA Y ATENTADO CONTRA LA AUTORIDAD",
        "AV. ROBO CALIFICADO EN POBLADO Y EN BANDA",
        "AV. HURTO - ESTAFA",
        "AV. ROBO DE AUTOMOTOR",
        "AV. ROBO CALIFICADO DE AUTOMOTOR",
        "AV. HURTO DE MOTOVEHICULO",
        "AV. ROBO CALIFICADO POR EL USO DE ARMA DE FUEGO",
        "AV. ROBO CALIFICADO EN POBLADO Y EL BANDA - PRIVACION ILEGITIMA DE LA LIBERTAD",
        "AV. DAÑOS",
        "AV. LESIONES CULPOSAS EN ACCIDENTE DE TRANSITO",
        "AV. ABUSO SEXUAL",
        "AV ROBO DOBLEMENTE CALIFICADO POR EL USO DE ARMA DE FUEGO Y EN POBLADO Y EN BANDA-ABUSO SEXUAL SIMPLE-LESIONES-TODO EN CONCURSO REAL",
        "AV DAÑOS-ABUSO DE ARMA DE FUEGO",
        "AV SU DENUNCIA",
        "AV. HOMICIDIO",
        "AV. ROBO CALIFICADO CON USO DE ARMA DE FUEGO - ROBO CALIFICADO EN GRADO DE TENTATIVA EN CONCURSO REAL",
        "AV. ROBO EN GRADO DE TENTATIVA",
        "AV. MUERTE EN ACCIDENTE DE TRANSITO",
        "AV. ROBO REITERADO",
        "AV. ABUSO DE ARMA DE FUEGO, LESIONES LEVES Y DAÑOS EN CONCURSO REAL",
        "AV. HURTO EN GRADO DE TENTATIVA-LESIONES LEVES",
        "AV. SEXUAL SIMPLE",
        "AV. PROCEDENCIA DE RODADO",
        "AV. HURTO CALIFICADO POR ESCALAMIENTO",
        "AV. INFRACCIÓN LEY 23.737",
        "AV. ROBO CON PERFORACIÓN CON FRACTURA",
        "AV. ABUSO DE ARMA DE FUEGO - DAÑOS",
        "AV. ABUSO DE ARMA DE FUEGO EN CONCURSO REAL",
        "AV. HOMICIDIO EN GRADO DE TENTATIVA - ABUSO DE ARMA DE FUEGO LESIONES Y AMENAZAS EN CONCURSO REAL",
        "AV. ABUSO DE ARMA DE FUEGO",
        "ACTUACIONES COMPLEMENTARIAS",
        "AV. ROBO CALIFICADO POR DAÑOS EN LA SALUD A UN MENOR",
        "AV. INTIMIDACIÓN PÚBLICA",
        "AV. HOMICIDIO AGRAVADO POR EL USO DE ARMA EN GRADO DE TENTATIVA",
        "AV. INFRACCIÓN AL ART.239 DEL CODIGO PENAL ARGENTINO/S FUGA DEL SERVICIO PENITENCIARIO PROVINCIAL.",
        "AV. VIOLACIÓN DE DOMICILIO",
        "IMPUTADOS (DETENIDOS)",
        "AV. LESIONES Y AMENAZAS CON ARMA DE FUEGO",
        "AV. ENCUBRIMIENTO",
        "SOLICITUD DE COLABORACIÓN DE LA PROV. DE SAN JUAN",
        "AV. HURTO EN GRADO DE TENTATIVA, PROCEDENCIA DE AUTOMOTOR",
        "AV.  LESIONES GRAVISIMAS",
        "AV. LESIONES CALIFICADAS POR EL USO DE ARMA DE FUEGO-EN CONCURSO REAL-LESIONES-AMENAZAS-VIOLACION DE DOMICILIO",
        "AV. HURTO EN GRADO DE TENTATIVA",
        "AV. LESIONES AGRAVADAS POR EL VINCULO Y POR MEDIAR VIOLENCIA DE GENERO",
        "AV. HOMICIDIO CULPOSO EN ACCIDENTE DE TRANSITO",
        "AV. HOMICIDIO EN GRADO DE TENTATIVA",
        "TAREAS JUDICIALES DE CAMPO",
        "AV. HURTO CALIFICADO REITERADO",
        "AV. ROBO - DAÑOS",
        "AV. HURTO AGRAVADO POR ESCALAMIENTO",
        "ACTUACIONES COMPLEMENTARIAS RELACIONADAS AL LEGAJO JUDICIAL N° 8280029/23",
        "AV. ROBO SIMPLE",
        "AV. ROBO CALIFICADO EN BANDA Y EN POBLADO AGRAVADO POR LA PARTICIPACIÓN DE MENORES",
        "AV. HURTO CALIFICADO POR SER EN POBLADO Y EN BANDA",
        "AV. ROBO CALIFICADO POR EL USO DE ARMA BLANCA",
        "AV. COMUNICA SITUACIÓN-MANIFESTACIÓN POR RECLAMOS DE EMPLEADOS DE LA MUNICIPADAD DE SAN LUIS.",
        "AV. COMUNICO SITUACIÓN",
        "AV. COMUNICO SITUACIÓN-MANIFESTACIÓN POR RECLAMO EX EMPLEADOS DEL PLAN DE INCLUSIÓN SOCIAL",
        "AV. HOMICIDIO CALIFICADO POR EL VINCULO Y POR MEDIAR VIOLENCIA DE GENERO EN GRADO DE TENTATIVA",
        "AV. LESIONES DOLOSAS-ATENTADO Y RESISTENCIA A LA  AUTORIDAD EN CONCURSO REAL",
        "AV. LESIONES DOLOSAS - ATENTADO Y RESISTENCIA A LA  AUTORIDAD EN CONCURSO REAL",
        "AV. HURTO SIMPLE EN GRADO DE TENTATIVA",
        "AV. LESIONES CON ARMA BLANCA",
        "AV. ROBO CALIFICADO POR ESCALAMIENTO - PRIVACION ILEGITIMA DE LA LIBERTAD",
        "AV. LESIONES-AMENAZAS",
        "AV HURTO CALIFICADO EN GRADO DE TENTATIVA AGRABADO POR LA PARTICIPACION DE UN MENOR",
    ];

    public static async Task<int> Ejecutar(string connectionString, bool confirmar)
    {
        Console.WriteLine($"Carátulas en el catálogo a migrar: {Catalogo.Length}");
        Console.WriteLine();
        Console.WriteLine("Ejemplos (primeros 5):");
        foreach (var nombre in Catalogo.Take(5))
        {
            Console.WriteLine($"  - {nombre}");
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

        var nombresExistentes = await dbContext.TiposCausa
            .Select(t => t.Nombre)
            .ToListAsync();
        var existentesSet = new HashSet<string>(nombresExistentes, StringComparer.OrdinalIgnoreCase);

        var insertados = 0;
        var omitidos = 0;
        foreach (var nombre in Catalogo)
        {
            if (existentesSet.Contains(nombre))
            {
                omitidos++;
                continue;
            }

            dbContext.TiposCausa.Add(new TipoCausa(nombre));
            insertados++;
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine($"Migración completada: {insertados} Tipos de Causa nuevos, {omitidos} ya existentes (omitidos).");
        return 0;
    }
}
