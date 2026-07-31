using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.PdfParsing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.DataMigration;

/// <summary>
/// Limpieza puntual del Relato de Informes migrados sin el PDF original
/// guardado (no se puede re-parsear desde el archivo) cuyo texto quedó
/// contaminado con "Página X de Y" y contenido posterior a las imágenes —
/// bug de InformePdfParser.ExtraerRelato ya corregido, pero que no corrige
/// retroactivamente lo ya persistido. Solo toca el campo Relato, nada más
/// del Informe. Invocar con el subcomando "limpiar-relatos".
/// </summary>
public static class ProgramLimpiarRelatos
{
    public static async Task<int> Ejecutar(string connectionString, bool confirmar)
    {
        var currentUserService = new UsuarioMigracionService();
        var interceptor = new AuditLogInterceptor(currentUserService);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new AppDbContext(options);

        var candidatos = await dbContext.Informes
            .Where(i => i.Relato != null && i.PdfPath == null)
            .ToListAsync();

        Console.WriteLine($"Informes sin PDF original con Relato cargado: {candidatos.Count}");
        Console.WriteLine();

        var aLimpiar = new List<(Informe Informe, string RelatoActual, string? RelatoLimpio)>();
        foreach (var informe in candidatos)
        {
            var limpio = InformePdfParser.LimpiarRelatoExistente(informe.Relato!);
            if (limpio != informe.Relato)
            {
                aLimpiar.Add((informe, informe.Relato!, limpio));
            }
        }

        Console.WriteLine($"Informes cuyo Relato cambia tras la limpieza: {aLimpiar.Count}");
        Console.WriteLine();

        // No se imprime el contenido del Relato (ni "antes" ni "después"):
        // el texto contaminado que motiva esta limpieza puede incluir datos
        // personales de terceros no vinculados al Informe (ej. titular real
        // de un vehículo con DNI/dirección, ver skill pdf-informe-parser —
        // hallazgo del security-reviewer). Solo se informa cuánto texto se
        // recorta, suficiente para verificar el efecto sin exponer el dato.
        foreach (var (informe, relatoActual, relatoLimpio) in aLimpiar)
        {
            var caracteresRemovidos = relatoActual.Length - (relatoLimpio?.Length ?? 0);
            Console.WriteLine($"{informe.IdRegistro} (Estado: {informe.Estado}): {relatoActual.Length} -> {relatoLimpio?.Length ?? 0} caracteres ({caracteresRemovidos} removidos).");
        }

        Console.WriteLine();

        if (!confirmar)
        {
            Console.WriteLine("Modo dry-run (no se persistió nada). Ejecutar con --confirmar para aplicar la limpieza.");
            return 0;
        }

        var omitidosPorEstado = 0;
        foreach (var (informe, _, relatoLimpio) in aLimpiar)
        {
            if (informe.Estado == EstadoInforme.Publicado)
            {
                // CompletarRelato lanza si el Informe está Publicado
                // (inmutable por diseño) — se omite en vez de fallar toda
                // la corrida, ninguno de los 14 conocidos está en este
                // estado, pero el chequeo cubre corridas futuras.
                omitidosPorEstado++;
                continue;
            }

            informe.CompletarRelato(relatoLimpio ?? string.Empty);
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine($"Limpieza aplicada a {aLimpiar.Count - omitidosPorEstado} Informe(s).");
        if (omitidosPorEstado > 0)
        {
            Console.WriteLine($"Omitidos por estar Publicado (inmutable): {omitidosPorEstado}.");
        }

        return 0;
    }
}
