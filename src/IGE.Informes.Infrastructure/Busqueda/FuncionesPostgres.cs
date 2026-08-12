using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Infrastructure.Busqueda;

/// <summary>
/// Mapeo de la función unaccent() de Postgres (extensión "unaccent",
/// habilitada en AppDbContext.OnModelCreating) para que la búsqueda
/// combinada (extensión de HU-05) ignore acentos: buscar "perez" debe
/// encontrar "Pérez". Solo puede llamarse dentro de una expresión LINQ
/// traducida a SQL — lanza si se ejecuta client-side.
/// </summary>
public static class FuncionesPostgres
{
    [DbFunction("unaccent", IsBuiltIn = false)]
    public static string Unaccent(string texto) =>
        throw new InvalidOperationException($"{nameof(Unaccent)} solo puede usarse dentro de una expresión LINQ traducida a SQL.");

    /// <summary>
    /// Mapeo de similarity() de Postgres (extensión "pg_trgm", habilitada
    /// en AppDbContext.OnModelCreating) — usado por SugerirCausasQueryHandler
    /// para sugerir Causas existentes con carátula parecida cuando el N° de
    /// Pieza Sumarial ingresado no matchea exacto con ninguna (HU-02, ver
    /// docs/03-modelo-dominio.md "Decisiones ya resueltas"). Devuelve un
    /// score de 0 (nada parecido) a 1 (idéntico) según trigramas
    /// compartidos entre los dos textos.
    /// </summary>
    [DbFunction("similarity", IsBuiltIn = false)]
    public static double Similarity(string texto1, string texto2) =>
        throw new InvalidOperationException($"{nameof(Similarity)} solo puede usarse dentro de una expresión LINQ traducida a SQL.");
}
