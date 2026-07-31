using IGE.Informes.Application.CasosAnalisis.Queries.BuscarCasos;

namespace IGE.Informes.UnitTests.CasosAnalisis;

/// <summary>
/// El Handler de esta Query vive en Infrastructure (EF.Functions.ILike, no
/// traduce en EF Core InMemory) — se prueba contra Postgres real en
/// tests/IGE.Informes.IntegrationTests/Busqueda/BuscarCasosQueryHandlerTests.cs.
/// Acá solo el Validator, que no depende del motor de base de datos.
/// </summary>
public class BuscarCasosQueryValidatorTests
{
    [Fact]
    public void Rechaza_texto_libre_vacio()
    {
        var validator = new BuscarCasosQueryValidator();

        var resultado = validator.Validate(new BuscarCasosQuery(""));

        Assert.False(resultado.IsValid);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    public void Rechaza_texto_libre_menor_a_tres_caracteres(string textoLibre)
    {
        var validator = new BuscarCasosQueryValidator();

        var resultado = validator.Validate(new BuscarCasosQuery(textoLibre));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Acepta_texto_libre_de_tres_caracteres_o_mas()
    {
        var validator = new BuscarCasosQueryValidator();

        var resultado = validator.Validate(new BuscarCasosQuery("robo"));

        Assert.True(resultado.IsValid);
    }
}
