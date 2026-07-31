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
}
