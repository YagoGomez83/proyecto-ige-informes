using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class VehiculoTests
{
    private static Vehiculo CrearVehiculo(string? dominio = null) => new(
        "Volkswagen",
        "Gol",
        "Gris",
        CertezaDominio.Incierto,
        AccionARealizar.Detener,
        "Comisaría 2°",
        TipoVehiculo.Auto,
        dominio);

    [Fact]
    public void Alta_nace_en_estado_Vigente()
    {
        var vehiculo = CrearVehiculo();

        Assert.Equal(EstadoVehiculo.Vigente, vehiculo.Estado);
    }

    [Fact]
    public void Alta_sin_dominio_confirmado_se_acepta_dado_que_es_nullable()
    {
        var vehiculo = CrearVehiculo(dominio: null);

        Assert.Null(vehiculo.Dominio);
    }

    [Fact]
    public void Alta_acepta_dominio_con_formato_no_estandar()
    {
        var vehiculo = CrearVehiculo("IAK 79-6");

        Assert.Equal("IAK 79-6", vehiculo.Dominio);
    }

    [Theory]
    [InlineData("", "Gol", "Gris", "Comisaría 2°")]
    [InlineData("Volkswagen", "", "Gris", "Comisaría 2°")]
    [InlineData("Volkswagen", "Gol", "", "Comisaría 2°")]
    [InlineData("Volkswagen", "Gol", "Gris", "")]
    public void Alta_rechaza_campos_obligatorios_vacios(string marca, string modelo, string color, string avisarA)
    {
        Assert.Throws<ArgumentException>(() => new Vehiculo(
            marca, modelo, color, CertezaDominio.Incierto, AccionARealizar.Detener, avisarA, TipoVehiculo.Auto));
    }

    [Fact]
    public void Alta_con_TipoVehiculo_Moto_sin_Cilindrada_lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Vehiculo(
            "Honda", "Wave", "Roja", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°", TipoVehiculo.Moto));
    }

    [Fact]
    public void Alta_con_TipoVehiculo_Moto_y_Cilindrada_no_lanza()
    {
        var vehiculo = new Vehiculo(
            "Honda", "Wave", "Roja", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°",
            TipoVehiculo.Moto, cilindrada: "110cc");

        Assert.Equal(TipoVehiculo.Moto, vehiculo.TipoVehiculo);
        Assert.Equal("110cc", vehiculo.Cilindrada);
    }

    [Fact]
    public void Alta_con_TipoVehiculo_distinto_de_Moto_ignora_Cilindrada()
    {
        var vehiculo = CrearVehiculo();

        Assert.Equal(TipoVehiculo.Auto, vehiculo.TipoVehiculo);
        Assert.Null(vehiculo.Cilindrada);
    }

    [Fact]
    public void MarcarIdentificado_cambia_el_estado()
    {
        var vehiculo = CrearVehiculo();

        vehiculo.MarcarIdentificado();

        Assert.Equal(EstadoVehiculo.Identificado, vehiculo.Estado);
    }

    [Fact]
    public void MarcarVigente_vuelve_a_Vigente_y_limpia_la_fecha_de_baja()
    {
        var vehiculo = CrearVehiculo();
        vehiculo.MarcarIdentificado();
        vehiculo.DarDeBaja(new DateOnly(2026, 7, 21));

        vehiculo.MarcarVigente();

        Assert.Equal(EstadoVehiculo.Vigente, vehiculo.Estado);
        Assert.Null(vehiculo.FechaBaja);
    }

    [Fact]
    public void DarDeBaja_no_modifica_el_Estado()
    {
        var vehiculo = CrearVehiculo();

        vehiculo.DarDeBaja(new DateOnly(2026, 7, 21));

        Assert.Equal(EstadoVehiculo.Vigente, vehiculo.Estado);
        Assert.Equal(new DateOnly(2026, 7, 21), vehiculo.FechaBaja);
    }

    [Fact]
    public void AsignarCategoriaAlerta_permite_multiples_categorias_simultaneas()
    {
        var vehiculo = CrearVehiculo();
        var robado = Guid.NewGuid();
        var narcotrafico = Guid.NewGuid();

        vehiculo.AsignarCategoriaAlerta(robado);
        vehiculo.AsignarCategoriaAlerta(narcotrafico);

        Assert.Equal(2, vehiculo.CategoriasAlertaIds.Count);
        Assert.Contains(robado, vehiculo.CategoriasAlertaIds);
        Assert.Contains(narcotrafico, vehiculo.CategoriasAlertaIds);
    }

    [Fact]
    public void AsignarCategoriaAlerta_no_duplica_la_misma_categoria()
    {
        var vehiculo = CrearVehiculo();
        var robado = Guid.NewGuid();

        vehiculo.AsignarCategoriaAlerta(robado);
        vehiculo.AsignarCategoriaAlerta(robado);

        Assert.Single(vehiculo.CategoriasAlertaIds);
    }

    [Fact]
    public void QuitarCategoriaAlerta_la_remueve_si_estaba_asignada()
    {
        var vehiculo = CrearVehiculo();
        var robado = Guid.NewGuid();
        vehiculo.AsignarCategoriaAlerta(robado);

        vehiculo.QuitarCategoriaAlerta(robado);

        Assert.Empty(vehiculo.CategoriasAlertaIds);
    }

    // HU-21 · Borrado lógico de Informe, Caso de Análisis, Vehículo y
    // Persona (docs/epic-01-gestion-informes.md), Característica "Borrado
    // lógico de un Vehículo": Eliminado/FechaEliminacion/Eliminar() todavía
    // no existen en el dominio — deben fallar en rojo (TDD) hasta
    // implementarse. Distinto de FechaBaja/DarDeBaja (fin de vigilancia
    // activa, no oculta el registro) — ver docs/03-modelo-dominio.md.

    [Fact]
    public void Eliminar_Vehiculo_MarcaEliminadoYFechaEliminacion()
    {
        var vehiculo = CrearVehiculo();

        vehiculo.Eliminar();

        Assert.True(vehiculo.Eliminado);
        Assert.NotNull(vehiculo.FechaEliminacion);
    }

    [Fact]
    public void Eliminar_NoModificaFechaBajaNiEstado_SonConceptosIndependientes()
    {
        var vehiculo = CrearVehiculo();
        vehiculo.DarDeBaja(new DateOnly(2026, 7, 21));

        vehiculo.Eliminar();

        Assert.True(vehiculo.Eliminado);
        Assert.Equal(new DateOnly(2026, 7, 21), vehiculo.FechaBaja);
        Assert.Equal(EstadoVehiculo.Vigente, vehiculo.Estado);
    }
}
