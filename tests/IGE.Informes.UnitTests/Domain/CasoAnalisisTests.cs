using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class CasoAnalisisTests
{
    private static readonly DateOnly Fecha = new(2026, 7, 21);
    private static readonly Guid DependenciaId = Guid.NewGuid();
    private static readonly Guid TipoIncidenteId = Guid.NewGuid();
    private static readonly Guid CreadorId = Guid.NewGuid();

    private static CasoAnalisis CrearCaso(string? nroLlamado911 = null) =>
        new(Fecha, DependenciaId, TipoIncidenteId, CreadorId, nroLlamado911);

    [Fact]
    public void Alta_queda_en_estado_Pendiente_y_asigna_al_creador_como_Creador()
    {
        var caso = CrearCaso();

        Assert.Equal(EstadoCaso.Pendiente, caso.Estado);
        Assert.Null(caso.Resultado);
        Assert.Single(caso.Analistas);
        Assert.Equal(CreadorId, caso.Analistas.Single().UsuarioId);
        Assert.Equal(RolCasoAnalista.Creador, caso.Analistas.Single().Rol);
    }

    [Fact]
    public void Alta_sin_numero_de_llamado_911_se_acepta()
    {
        var caso = CrearCaso(nroLlamado911: null);

        Assert.Null(caso.NroLlamado911);
    }

    [Theory]
    [InlineData("")]
    public void Alta_rechaza_dependencia_vacia(string _)
    {
        Assert.Throws<ArgumentException>(() =>
            new CasoAnalisis(Fecha, Guid.Empty, TipoIncidenteId, CreadorId));
    }

    [Fact]
    public void Alta_rechaza_tipo_incidente_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            new CasoAnalisis(Fecha, DependenciaId, Guid.Empty, CreadorId));
    }

    [Fact]
    public void CerrarConResultado_pasa_a_Cerrado_con_el_resultado_indicado()
    {
        var caso = CrearCaso();

        caso.CerrarConResultado(ResultadoCaso.Positivo);

        Assert.Equal(EstadoCaso.Cerrado, caso.Estado);
        Assert.Equal(ResultadoCaso.Positivo, caso.Resultado);
    }

    [Fact]
    public void AsignarAnalista_agrega_un_operador_nuevo()
    {
        var caso = CrearCaso();
        var operadorId = Guid.NewGuid();

        caso.AsignarAnalista(operadorId, RolCasoAnalista.Operador);

        Assert.Equal(2, caso.Analistas.Count);
        Assert.Contains(caso.Analistas, a => a.UsuarioId == operadorId && a.Rol == RolCasoAnalista.Operador);
    }

    [Fact]
    public void AsignarAnalista_no_duplica_si_el_usuario_ya_esta_asignado()
    {
        var caso = CrearCaso();

        caso.AsignarAnalista(CreadorId, RolCasoAnalista.Operador);

        Assert.Single(caso.Analistas);
    }

    [Fact]
    public void VincularVehiculoTexto_guarda_la_descripcion_libre()
    {
        var caso = CrearCaso();

        caso.VincularVehiculoTexto("Sedán oscuro, dominio incierto");

        Assert.Equal("Sedán oscuro, dominio incierto", caso.VehiculoInvolucradoTexto);
    }

    [Fact]
    public void VincularVehiculoTexto_rechaza_descripcion_vacia()
    {
        var caso = CrearCaso();

        Assert.Throws<ArgumentException>(() => caso.VincularVehiculoTexto(""));
    }

    [Fact]
    public void VincularCamaraTexto_acumula_referencias_sucesivas()
    {
        var caso = CrearCaso();

        caso.VincularCamaraTexto("SL 18");
        caso.VincularCamaraTexto("JK 51");

        Assert.Equal("SL 18; JK 51", caso.CamarasAnalizadasTexto);
    }

    // HU-21 · Borrado lógico de Informe, Caso de Análisis, Vehículo y
    // Persona (docs/epic-01-gestion-informes.md), Característica "Borrado
    // lógico de un Caso de Análisis": Eliminado/FechaEliminacion/Eliminar()
    // todavía no existen en el dominio — deben fallar en rojo (TDD) hasta
    // implementarse. A diferencia de Informe.Eliminar(), CasoAnalisis.Eliminar()
    // no tiene ninguna guarda por Estado (ver docs/03-modelo-dominio.md).

    [Fact]
    public void Eliminar_CasoEnEstadoPendiente_MarcaEliminadoYFechaEliminacion()
    {
        var caso = CrearCaso();

        caso.Eliminar();

        Assert.True(caso.Eliminado);
        Assert.NotNull(caso.FechaEliminacion);
    }

    [Fact]
    public void Eliminar_CasoCerrado_NoBloqueaLaEliminacion()
    {
        var caso = CrearCaso();
        caso.CerrarConResultado(ResultadoCaso.Positivo);

        caso.Eliminar();

        Assert.True(caso.Eliminado);
        Assert.NotNull(caso.FechaEliminacion);
        Assert.Equal(EstadoCaso.Cerrado, caso.Estado);
    }
}
