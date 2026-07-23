using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class DependenciaTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_datos()
    {
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);

        Assert.NotEqual(Guid.Empty, dependencia.Id);
        Assert.Equal("Comisaría 2°", dependencia.Nombre);
        Assert.Equal(TipoDependencia.Comisaria, dependencia.Tipo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_rechaza_nombre_vacio_o_en_blanco(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Dependencia(nombre, TipoDependencia.Fiscalia));
    }

    [Fact]
    public void AsignarBarrio_agrega_el_barrio_a_la_jurisdiccion()
    {
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var barrioId = Guid.NewGuid();

        dependencia.AsignarBarrio(barrioId);

        Assert.Contains(barrioId, dependencia.BarrioIds);
    }

    [Fact]
    public void AsignarBarrio_es_idempotente()
    {
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var barrioId = Guid.NewGuid();

        dependencia.AsignarBarrio(barrioId);
        dependencia.AsignarBarrio(barrioId);

        Assert.Single(dependencia.BarrioIds);
    }

    [Fact]
    public void AsignarBarrio_rechaza_id_vacio()
    {
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);

        Assert.Throws<ArgumentException>(() => dependencia.AsignarBarrio(Guid.Empty));
    }

    [Fact]
    public void QuitarBarrio_remueve_el_barrio_de_la_jurisdiccion()
    {
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var barrioId = Guid.NewGuid();
        dependencia.AsignarBarrio(barrioId);

        dependencia.QuitarBarrio(barrioId);

        Assert.Empty(dependencia.BarrioIds);
    }

    [Fact]
    public void AsignarBarrio_no_esta_restringido_por_tipo_de_dependencia()
    {
        // Ver docs/01-glosario-dominio.md: no toda Dependencia tiene
        // jurisdicción geográfica real, pero el Domain no lo restringe —
        // la UI simplemente no lo exige para Fiscalía/Juzgado.
        var fiscalia = new Dependencia("Fiscalía N°3", TipoDependencia.Fiscalia);
        var barrioId = Guid.NewGuid();

        fiscalia.AsignarBarrio(barrioId);

        Assert.Contains(barrioId, fiscalia.BarrioIds);
    }
}
