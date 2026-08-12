using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Informes.Queries.SugerirCausas;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// HU-02 · Editar / corregir metadatos de un informe (Épica 01), escenario
/// "Sugerir Causas existentes cuando la Pieza Sumarial no matchea ninguna".
/// La cobertura funcional de la similaridad de texto (pg_trgm/similarity())
/// vive en
/// tests/IGE.Informes.IntegrationTests/Informes/SugerirCausasQueryTests.cs
/// contra Postgres real — EF Core InMemory no traduce esa función, mismo
/// criterio que BuscarInformesQueryHandlerTests/BuscarInformesTextoLibreTests.
/// Acá solo se cubre lo que no depende del motor: que la Query exista con la
/// autorización esperada (cualquier rol autenticado del sistema puede pedir
/// sugerencias al editar un Informe, igual que EditarInformeCommand).
/// </summary>
public class SugerirCausasQueryTests
{
    [Fact]
    public void SugerirCausasQuery_DeclaraAutorizacionParaAnalistaSupervisorYAdmin()
    {
        var atributo = typeof(SugerirCausasQuery)
            .GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Contains(Roles.Analista, atributo.Roles);
        Assert.Contains(Roles.Supervisor, atributo.Roles);
        Assert.Contains(Roles.Admin, atributo.Roles);
    }
}
