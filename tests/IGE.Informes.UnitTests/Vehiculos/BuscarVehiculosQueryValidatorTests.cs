using IGE.Informes.Application.Vehiculos.Queries.BuscarVehiculos;

namespace IGE.Informes.UnitTests.Vehiculos;

/// <summary>
/// El Handler de esta Query vive en Infrastructure (EF.Functions.ILike, no
/// traduce en EF Core InMemory) — se prueba contra Postgres real en
/// tests/IGE.Informes.IntegrationTests/Busqueda/BuscarVehiculosQueryHandlerTests.cs.
/// Acá solo el Validator, que no depende del motor de base de datos.
/// </summary>
public class BuscarVehiculosQueryValidatorTests
{
    [Fact]
    public void Rechaza_texto_libre_vacio()
    {
        var validator = new BuscarVehiculosQueryValidator();

        var resultado = validator.Validate(new BuscarVehiculosQuery(""));

        Assert.False(resultado.IsValid);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    public void Rechaza_texto_libre_menor_a_tres_caracteres(string textoLibre)
    {
        var validator = new BuscarVehiculosQueryValidator();

        var resultado = validator.Validate(new BuscarVehiculosQuery(textoLibre));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Acepta_texto_libre_de_tres_caracteres_o_mas()
    {
        var validator = new BuscarVehiculosQueryValidator();

        var resultado = validator.Validate(new BuscarVehiculosQuery("IAK"));

        Assert.True(resultado.IsValid);
    }
}
